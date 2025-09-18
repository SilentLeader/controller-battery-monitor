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

public class SingleInstanceService : IDisposable
{
    private static readonly string LockFileName = Path.Combine(Path.GetTempPath(), $"{Environment.UserName}_ControllerMonitorSingleInstance.lock");
    private static readonly string PipeName = $"{Environment.UserName}_ControllerMonitorPipe";

    private readonly ILogger<SingleInstanceService> _logger;
    private FileStream? _lockFile;
    private Task? _pipeTask;
    private CancellationTokenSource? _cancellationTokenSource;
    private MainWindow? _mainWindow;

    public bool IsFirstInstance { get; private set; }

    public SingleInstanceService(ILogger<SingleInstanceService> logger)
    {
        _logger = logger;
    }

    public void SetMainWindow(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
    }

    public bool TryAcquireSingleInstance()
    {
        _logger.LogInformation("Attempting to acquire single instance lock: {LockFileName}", LockFileName);
        
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
            _logger.LogInformation("This is the first instance, starting pipe server");
            StartPipeServer();
        }
        catch (IOException ex) when (ex.HResult == -2147024864) // File is being used by another process
        {
            _logger.LogInformation("Lock file is in use by another instance");
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
                                _logger.LogInformation("Previous instance with PID {ProcessId} has exited, retrying lock acquisition", processId);
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
                            _logger.LogInformation("Process {ProcessId} from lock file no longer exists, cleaning up", processId);
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
            _logger.LogInformation("Attempting to bring existing window to front via pipe: {PipeName}", PipeName);
            try
            {
                using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                {
                    pipeClient.Connect(1000); // 1 second timeout
                    using (StreamWriter writer = new StreamWriter(pipeClient))
                    {
                        writer.WriteLine("SHOW");
                    }
                }
                _logger.LogInformation("Successfully sent show command to existing instance");
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
                    using (StreamReader reader = new StreamReader(pipeServer))
                    {
                        string? message = reader.ReadLine();
                        if (message == "SHOW")
                        {
                            _logger.LogInformation("Received SHOW command, bringing window to front");
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
                _logger.LogInformation("Pipe server cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Pipe server warning");
            }
        }

        _logger.LogInformation("Pipe server down");
    }

    public void Dispose()
    {
        _logger.LogInformation("Disposing SingleInstanceService");
        
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
                    _logger.LogInformation("Released lock file");
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
}