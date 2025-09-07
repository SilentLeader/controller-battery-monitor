using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using System.Timers;
using System.ComponentModel;
using XboxBatteryMonitor.Models;
using XboxBatteryMonitor.Services;
using XboxBatteryMonitor.ValueObjects;
using System;

namespace XboxBatteryMonitor.ViewModels;

public partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly IBatteryMonitorService _batteryService;
    private readonly SettingsService _settingsService;
    private readonly INotificationService _notificationService;
    private Timer _timer;
    private BatteryLevel _previousBatteryLevel = BatteryLevel.Unknown;
    private bool _previousIsCharging = false;

    [ObservableProperty]
    private ControllerInfo controllerInfo = new();

    [ObservableProperty]
    private SettingsViewModel settings;
    private bool disposedValue;

    public bool IsCapacityVisible => ControllerInfo.BatteryInfo.Capacity != null;

    public MainWindowViewModel(IBatteryMonitorService batteryService, SettingsViewModel settings, INotificationService notificationService)
    {
        _batteryService = batteryService;
        _settingsService = new SettingsService();
        _notificationService = notificationService;
        Settings = settings;
        Settings.PropertyChanged += Settings_PropertyChanged;
        _ = UpdateBatteryInfoAsync();

        _timer = new Timer(settings.UpdateFrequencySeconds * 1000);
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
        // Auto-save settings on change
        _settingsService.SaveSettings(Settings);
    }

    [RelayCommand]
    private async Task UpdateBatteryInfoAsync()
    {
        var batteryInfo = await _batteryService.GetBatteryInfoAsync();

        // Check for low battery notification
        if (_previousBatteryLevel != BatteryLevel.Low && batteryInfo.Level == BatteryLevel.Low && !batteryInfo.IsCharging)
        {
            await _notificationService.ShowNotificationAsync("Low Battery", "Controller battery is low and not charging.");
        }

        // Update previous state
        _previousBatteryLevel = batteryInfo.Level;
        _previousIsCharging = batteryInfo.IsCharging;

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
