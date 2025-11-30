using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using ControllerMonitor.Windows;
using Microsoft.Extensions.Logging;

namespace ControllerMonitor.Services;

public class SingleInstanceService(ILogger<SingleInstanceService> logger) : IDisposable
{
    private static readonly string LockFileName = Path.Combine(Path.GetTempPath(), $"{Environment.UserName}_ControllerMonitorSingleInstance.lock");
    private static readonly string PipeName = $"{Environment.UserName}_ControllerMonitorPipe";

    private readonly ILogger<SingleInstanceService> _logger = logger;
    private FileStream? _lockFile;
    private Task? _pipeTask;
    private CancellationTokenSource? _cancellationTokenSource;
    private MainWindow? _mainWindow;
    private bool disposedValue;

    public bool IsFirstInstance { get; private set; }

    public void SetMainWindow(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
    }

    public bool TryAcquireSingleInstance()
    {
        _logger.LogTrace("Attempting to acquire single instance lock: {LockFileName}", LockFileName);
        
        try
        {
            // Try to create and lock the file exclusively
            _lockFile = new FileStream(LockFileName, FileMode.Create, FileAccess.Write, FileShare.None);
            
            // Write process ID to the lock file
            using (var writer = new StreamWriter(_lockFile, leaveOpen: true))
            {
                writer.WriteLine(Environment.ProcessId);
                writer.Flush();
            }
            _lockFile.Position = 0;
            
            IsFirstInstance = true;
            _logger.LogDebug("This is the first instance, starting pipe server");
            StartPipeServer();
        }
        catch (IOException ex) when (ex.HResult == -2147024864 || ex.HResult == 11) // File is being used by another process
        {
            _logger.LogDebug("Lock file is in use by another instance");
            IsFirstInstance = false;
            
            // Try to read the process ID from the lock file to verify it's still running
            try
            {
                if (File.Exists(LockFileName))
                {
                    var lockContent = File.ReadAllText(LockFileName).Trim();
                    if (int.TryParse(lockContent, out int processId))
                    {
                        try
                        {
                            var process = System.Diagnostics.Process.GetProcessById(processId);
                            if (process.HasExited)
                            {
                                _logger.LogDebug("Previous instance with PID {ProcessId} has exited, retrying lock acquisition", processId);
                                File.Delete(LockFileName);
                                return TryAcquireSingleInstance(); // Retry
                            }
                            else
                            {
                                _logger.LogInformation("Another instance is running with PID {ProcessId}", processId);
                            }
                        }
                        catch (ArgumentException)
                        {
                            // Process doesn't exist, cleanup and retry
                            _logger.LogDebug("Process {ProcessId} from lock file no longer exists, cleaning up", processId);
                            File.Delete(LockFileName);
                            return TryAcquireSingleInstance(); // Retry
                        }
                    }
                }
            }
            catch (Exception cleanupEx)
            {
                _logger.LogWarning(cleanupEx, "Failed to cleanup stale lock file");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire lock file");
            IsFirstInstance = false;
        }

        return IsFirstInstance;
    }

    public void BringExistingWindowToFront()
    {
        if (!IsFirstInstance)
        {
            _logger.LogDebug("Attempting to bring existing window to front via pipe: {PipeName}", PipeName);
            try
            {
                using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                {
                    pipeClient.Connect(1000); // 1 second timeout
                    using (StreamWriter writer = new StreamWriter(pipeClient))
                    {
                        writer.WriteLine("SHOW");
                        writer.Flush();
                    }
                }
                _logger.LogDebug("Successfully sent show command to existing instance");
                Thread.Sleep(200);
            }
            catch
            {
                _logger.LogWarning("Failed to connect to existing instance pipe");
            }
        }
    }

    private void StartPipeServer()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _pipeTask = PipeServerLoop(_cancellationTokenSource.Token);
    }

    private async Task PipeServerLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                using (var pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.In))
                {
                    await pipeServer.WaitForConnectionAsync(token);
                    using (var reader = new StreamReader(pipeServer))
                    {
                        string? message = reader.ReadLine();
                        if (message == "SHOW")
                        {
                            _logger.LogDebug("Received SHOW command, bringing window to front");
                            Dispatcher.UIThread.Invoke(() =>
                            {
                                if (_mainWindow != null)
                                {
                                    _mainWindow.Show();
                                    _mainWindow.WindowState = WindowState.Normal;
                                    _mainWindow.Activate();
                                }
                            });
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Single instance pipe server cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Single instance pipe server warning");
            }
        }

        _logger.LogDebug("Single instance pipe server down");
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _logger.LogDebug("Disposing SingleInstanceService");
        
                // Stop the pipe server first
                _cancellationTokenSource?.Cancel();
                
                try
                {
                    _pipeTask?.Wait(TimeSpan.FromSeconds(5)); // Wait up to 5 seconds for pipe server to stop
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Exception while waiting for pipe server to stop");
                }
                
                // Release the lock file if we own it
                if (_lockFile != null)
                {
                    try
                    {
                        _lockFile.Dispose();
                        _lockFile = null;
                        
                        if (IsFirstInstance && File.Exists(LockFileName))
                        {
                            File.Delete(LockFileName);
                            _logger.LogDebug("Released lock file");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Exception while releasing lock file");
                    }
                }
                
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                _pipeTask = null;
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}