using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using XboxBatteryMonitor.Windows;
using Microsoft.Extensions.Logging;

namespace XboxBatteryMonitor.Services;

public class SingleInstanceService : IDisposable
{
    private static readonly string MutexName = $"{Environment.UserName}_XboxBatteryMonitorSingleInstance";
    private static readonly string PipeName = $"{Environment.UserName}_XboxBatteryMonitorPipe";

    private readonly ILogger<SingleInstanceService> _logger;
    private Mutex? _mutex;
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
        _logger.LogInformation("Attempting to acquire single instance mutex: {MutexName}", MutexName);
        _mutex = new Mutex(true, MutexName, out bool createdNew);
        IsFirstInstance = createdNew;

        if (IsFirstInstance)
        {
            _logger.LogInformation("This is the first instance, starting pipe server");
            StartPipeServer();
        }
        else
        {
            _logger.LogInformation("Another instance is already running");
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
            catch
            {
                // ignore
            }
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _pipeTask?.Wait();
        _mutex?.Dispose();
    }
}