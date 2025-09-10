using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using System.Timers;
using System.ComponentModel;
using XboxBatteryMonitor.ViewModels;
using XboxBatteryMonitor.Services;
using XboxBatteryMonitor.ValueObjects;
using System;
using System.Reflection;
using Avalonia.Threading;

namespace XboxBatteryMonitor.ViewModels;

public partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly IBatteryMonitorService _batteryService;
    private readonly SettingsService _settingsService;
    private readonly INotificationService _notificationService;
    private Timer _debounceTimer;
    private Timer _timer;
    private BatteryLevel _previousBatteryLevel = BatteryLevel.Unknown;
    private bool _previousIsCharging = false;
    private bool _previousIsConnected = false;

    [ObservableProperty]
    private ControllerInfoViewModel controllerInfo = new();

    [ObservableProperty]
    private SettingsViewModel settings;

    [ObservableProperty]
    private string appName = "Xbox Battery Monitor";

    [ObservableProperty]
    private string appDescription = "A simple application to monitor Xbox controller battery levels.";

    [ObservableProperty]
    private string appVersion;

    private bool disposedValue;

    public bool IsCapacityVisible => ControllerInfo.BatteryInfo.Capacity != null;

    public MainWindowViewModel(IBatteryMonitorService batteryService, SettingsViewModel settings, INotificationService notificationService, SettingsService settingsService)
    {
        _batteryService = batteryService;
        _settingsService = settingsService;
        _notificationService = notificationService;
        Settings = settings;
        Settings.PropertyChanged += Settings_PropertyChanged;
        AppVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0";
        _ = UpdateBatteryInfoAsync();

        _timer = new Timer(settings.UpdateFrequencySeconds * 1000);
        _debounceTimer = new Timer(500);
        _debounceTimer.AutoReset = false;
        _debounceTimer.Elapsed += (s, e) => Dispatcher.UIThread.Post(() => _settingsService.SaveSettings(Settings));
        _timer.Elapsed += async (s, e) => await UpdateBatteryInfoAsync();
        _timer.Start();
    }

    private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.UpdateFrequencySeconds))
        {
            _timer.Stop();
            _timer.Interval = Settings.UpdateFrequencySeconds * 1000;
            _timer.Start();
        }
        // Debounced auto-save settings on change
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    [RelayCommand]
    private async Task UpdateBatteryInfoAsync()
    {
        var batteryInfo = await _batteryService.GetBatteryInfoAsync();

        // Check for controller connection/disconnection notifications
        if (_previousIsConnected != batteryInfo.IsConnected)
        {
            if (batteryInfo.IsConnected && Settings.NotifyOnControllerConnected)
            {
                await _notificationService.ShowNotificationAsync("Controller Connected", "Xbox controller has been connected.");
            }
            else if (!batteryInfo.IsConnected && Settings.NotifyOnControllerDisconnected)
            {
                await _notificationService.ShowNotificationAsync("Controller Disconnected", "Xbox controller has been disconnected.");
            }
        }

        // Check for low battery notification
        if (_previousBatteryLevel != BatteryLevel.Low && batteryInfo.Level == BatteryLevel.Low && !batteryInfo.IsCharging && Settings.NotifyOnBatteryLow)
        {
            await _notificationService.ShowNotificationAsync("Low Battery", "Controller battery is low and not charging.");
        }

        // Update previous state
        _previousBatteryLevel = batteryInfo.Level;
        _previousIsCharging = batteryInfo.IsCharging;
        _previousIsConnected = batteryInfo.IsConnected;

        ControllerInfo.BatteryInfo.Level = batteryInfo.Level;
        ControllerInfo.BatteryInfo.Capacity = batteryInfo.Capacity;
        ControllerInfo.BatteryInfo.IsCharging = batteryInfo.IsCharging;
        ControllerInfo.BatteryInfo.IsConnected = batteryInfo.IsConnected;
    }

    [RelayCommand]
    private void SaveSettings()
    {
        _settingsService.SaveSettings(Settings);
    }

                
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _timer.Stop();
                _timer.Dispose();
                _debounceTimer.Stop();
                _debounceTimer.Dispose();
                
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
