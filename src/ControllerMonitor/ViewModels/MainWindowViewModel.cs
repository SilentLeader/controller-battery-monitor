using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using ControllerMonitor.Services;
using ControllerMonitor.Interfaces;
using ControllerMonitor.ValueObjects;

namespace ControllerMonitor.ViewModels;

public partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly IBatteryMonitorService _batteryService;
    private readonly ISettingsService _settingsService;
    private readonly INotificationService _notificationService;
    private System.Timers.Timer _debounceTimer;
    private BatteryLevel _previousBatteryLevel = BatteryLevel.Unknown;
    
    private bool _previousIsConnected = false;
    private readonly ILogger<MainWindowViewModel>? _logger;

    [ObservableProperty]
    private ControllerInfoViewModel controllerInfo = new();

    [ObservableProperty]
    private SettingsViewModel settings;

    [ObservableProperty]
    private string appName = "Controller Monitor";

    [ObservableProperty]
    private string appDescription = "A simple application to monitor game controller battery levels.";

    [ObservableProperty]
    private string appVersion;

    private bool disposedValue;

    public bool IsCapacityVisible => ControllerInfo.BatteryInfo.Capacity != null;

    public MainWindowViewModel(
        IBatteryMonitorService batteryService,
        SettingsViewModel settings,
        INotificationService notificationService,
        ISettingsService settingsService,
        ILogger<MainWindowViewModel>? logger = null)
    {
        _batteryService = batteryService;
        _settingsService = settingsService;
        _notificationService = notificationService;
        _logger = logger;
        Settings = settings;
        Settings.PropertyChanged += Settings_PropertyChanged;
        AppVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0";

        _debounceTimer = new Timer(500)
        {
            AutoReset = false
        };
        _debounceTimer.Elapsed += (s, e) => Dispatcher.UIThread.Post(() => _settingsService.SaveSettings(Settings.ToSettingsData()));

        _batteryService.BatteryInfoChanged += OnBatteryInfoChanged;
        _ = Task.Run(async () =>
        {
            try
            {
                var initialInfo = await _batteryService.GetBatteryInfoAsync();
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _previousBatteryLevel = initialInfo.Level;
                    _previousIsConnected = initialInfo.IsConnected;

                    ControllerInfo.BatteryInfo.Level = initialInfo.Level;
                    ControllerInfo.BatteryInfo.Capacity = initialInfo.Capacity;
                    ControllerInfo.BatteryInfo.IsCharging = initialInfo.IsCharging;
                    ControllerInfo.BatteryInfo.IsConnected = initialInfo.IsConnected;
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to start monitoring");
            }
        });
    }

    private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.UpdateFrequencySeconds))
        {
        }
        // Debounced auto-save settings on change
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private async void OnBatteryInfoChanged(object? sender, BatteryInfoViewModel? batteryInfo)
    {
        if (batteryInfo == null) return;

        // Capture previous state locally to make notification decisions off the UI thread
        var prevIsConnected = _previousIsConnected;
        var prevBatteryLevel = _previousBatteryLevel;

        // Use a local reference to settings for thread-safety of lookups
        var settings = Settings;

        // Check for controller connection/disconnection notifications (safe to call off UI thread because NotificationService posts to UI thread)
        if (prevIsConnected != batteryInfo.IsConnected)
        {
            if (batteryInfo.IsConnected && settings.NotifyOnControllerConnected)
            {
                await _notificationService.ShowSystemNotificationAsync("Controller Connected", "Xbox controller has been connected.", expirationTime: 3);
            }
            else if (!batteryInfo.IsConnected && settings.NotifyOnControllerDisconnected)
            {
                await _notificationService.ShowSystemNotificationAsync("Controller Disconnected", "Xbox controller has been disconnected.", expirationTime: 3);
            }
        }

        // Check for low battery notification
        if (prevBatteryLevel != BatteryLevel.Low && batteryInfo.Level == BatteryLevel.Low && !batteryInfo.IsCharging && settings.NotifyOnBatteryLow)
        {
            await _notificationService.ShowSystemNotificationAsync("Low Battery", "Controller battery is low and not charging.", NotificationPriority.High, expirationTime: 10);
        }

        // Update previous state and view-model properties on the UI thread to avoid affinity violations
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _previousBatteryLevel = batteryInfo.Level;
            _previousIsConnected = batteryInfo.IsConnected;

            ControllerInfo.BatteryInfo.Level = batteryInfo.Level;
            ControllerInfo.BatteryInfo.Capacity = batteryInfo.Capacity;
            ControllerInfo.BatteryInfo.IsCharging = batteryInfo.IsCharging;
            ControllerInfo.BatteryInfo.IsConnected = batteryInfo.IsConnected;
        });
    }
                
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _debounceTimer.Stop();
                _debounceTimer.Dispose();
                _ = Task.Run(() =>
                {
                    try
                    {
                        _batteryService.BatteryInfoChanged -= OnBatteryInfoChanged;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Failed to stop monitoring");
                    }
                });
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
