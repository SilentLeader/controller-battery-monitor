using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Styling;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using ControllerMonitor.Interfaces;
using ControllerMonitor.Models;
using Microsoft.Extensions.Logging;

namespace ControllerMonitor.ViewModels;

public partial class AppViewModel : ObservableObject, IDisposable
{
    [ObservableProperty]
    private ControllerInfoViewModel controllerInfo = new();

    [ObservableProperty]
    private SettingsViewModel _settings;

    [ObservableProperty]
    private ThemeVariant? themeVariant;

    private bool disposedValue;
    private readonly IBatteryMonitorService _batteryService;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<AppViewModel> _logger;

    public AppViewModel(
        IBatteryMonitorService batteryService,
        SettingsViewModel settings,
        ISettingsService settingsService,
        ILogger<AppViewModel> logger)
    {
        _batteryService = batteryService;
        _settings = settings;
        _settingsService = settingsService;
        _logger = logger;
        if (Application.Current != null)
        {
            themeVariant = Application.Current.ActualThemeVariant;
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }

        _batteryService.BatteryInfoChanged += OnBatteryInfoChanged;
        _settingsService.SettingsChanged += OnSettingsChanged;

        _ = InitializeBatteryInfoAsync();
    }

    private async Task InitializeBatteryInfoAsync()
    {
        try
            {
                var initialInfo = await _batteryService.GetBatteryInfoAsync();
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ControllerInfo.BatteryInfo.Level = initialInfo.Level;
                    ControllerInfo.BatteryInfo.Capacity = initialInfo.Capacity;
                    ControllerInfo.BatteryInfo.IsCharging = initialInfo.IsCharging;
                    ControllerInfo.BatteryInfo.IsConnected = initialInfo.IsConnected;
                    ControllerInfo.BatteryInfo.ModelName = initialInfo.ModelName;

                    // Set initial controller name with fallback logic
                    ControllerInfo.Name = GetControllerDisplayName(initialInfo);
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed initialize battery info");
            }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _batteryService.BatteryInfoChanged -= OnBatteryInfoChanged;
                _settingsService.SettingsChanged -= OnSettingsChanged;
                if (Application.Current != null)
                {
                    Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
                }
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

    private async void OnBatteryInfoChanged(object? sender, BatteryInfoViewModel? batteryInfo)
    {
        if (batteryInfo == null) return;

        // Update previous state and view-model properties on the UI thread to avoid affinity violations
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            ControllerInfo.BatteryInfo.Level = batteryInfo.Level;
            ControllerInfo.BatteryInfo.Capacity = batteryInfo.Capacity;
            ControllerInfo.BatteryInfo.IsCharging = batteryInfo.IsCharging;
            ControllerInfo.BatteryInfo.IsConnected = batteryInfo.IsConnected;
            ControllerInfo.BatteryInfo.ModelName = batteryInfo.ModelName;

            // Set initial controller name with fallback logic
            ControllerInfo.Name = GetControllerDisplayName(batteryInfo);
        });
    }

    private void OnSettingsChanged(object? sender, Settings e)
    {
        Settings = new SettingsViewModel(_settingsService.GetSettings());
    }

    private async void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        if (Application.Current != null)
        {
            await Dispatcher.UIThread.InvokeAsync(() => ThemeVariant = Application.Current.ActualThemeVariant);
        }
    }
    
    private static string GetControllerDisplayName(BatteryInfoViewModel batteryInfo)
    {
        if (batteryInfo?.IsConnected != true)
            return "Unknown Controller";
            
        return !string.IsNullOrWhiteSpace(batteryInfo.ModelName)
            ? batteryInfo.ModelName
            : "Unknown Controller";
    }
}
