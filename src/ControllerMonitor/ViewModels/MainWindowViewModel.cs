using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Timers;
using Avalonia;
using Avalonia.Styling;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using ControllerMonitor.Interfaces;
using ControllerMonitor.Models;
using ControllerMonitor.Services;
using ControllerMonitor.ValueObjects;
using Microsoft.Extensions.Logging;

namespace ControllerMonitor.ViewModels;

public partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly IBatteryMonitorService _batteryService;
    private readonly ISettingsService _settingsService;
    private readonly INotificationService _notificationService;
    private Timer _settingsSaveDebounceTimer;
    private BatteryLevel _previousBatteryLevel = BatteryLevel.Unknown;

    private bool _previousIsConnected = false;
    private readonly ILogger<MainWindowViewModel>? _logger;

    [ObservableProperty]
    private ControllerInfoViewModel controllerInfo = new();

    [ObservableProperty]
    private SettingsViewModel settings;

    [ObservableProperty]
    private string appName;

    [ObservableProperty]
    private string appDescription;

    [ObservableProperty]
    private string appVersion;

    [ObservableProperty]
    private ThemeVariant? themeVariant;

    private bool disposedValue;

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

        // Initialize localized app info
        AppName = LocalizationService.Instance["App_Name"];
        AppDescription = LocalizationService.Instance["App_Description"];

        // Subscribe to culture changes to update app info
        LocalizationService.Instance.PropertyChanged += LocalizationChanged;

        // Initialize theme variant
        if (Application.Current != null)
        {
            themeVariant = Application.Current.ActualThemeVariant;
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;

            // Apply initial theme setting
            ApplyThemeSetting(Settings.Theme);
        }

        // Apply initial language setting
        ApplyLanguageSetting(Settings.Language);

        _settingsSaveDebounceTimer = new Timer(500)
        {
            AutoReset = false
        };
        _settingsSaveDebounceTimer.Elapsed += OnSettingsSaveDebounceTimerElapsed;

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
                    ControllerInfo.BatteryInfo.ModelName = initialInfo.ModelName;
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed initialize battery info");
            }
        });
    }

    private async void OnSettingsSaveDebounceTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        var settings = await Dispatcher.UIThread.InvokeAsync(() => Settings.ToSettingsData());
        await _settingsService.SaveSettingsAsync(settings);
    }

    private void LocalizationChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LocalizationService.CurrentCulture))
        {
            AppName = LocalizationService.Instance["App_Name"];
            AppDescription = LocalizationService.Instance["App_Description"];
        }
    }

    private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.Theme))
        {
            // Apply theme change immediately
            ApplyThemeSetting(Settings.Theme);
        }
        else if (e.PropertyName == nameof(Settings.Language))
        {
            // Apply language change immediately
            ApplyLanguageSetting(Settings.Language);
        }
        // Debounced auto-save settings on change
        _settingsSaveDebounceTimer.Stop();
        _settingsSaveDebounceTimer.Start();
    }

    private async void OnBatteryInfoChanged(object? sender, BatteryInfo batteryInfo)
    {
        if (batteryInfo == null) return;

        // Capture previous state locally to make notification decisions off the UI thread
        var prevIsConnected = _previousIsConnected;
        var prevBatteryLevel = _previousBatteryLevel;

        // Use a local reference to settings for thread-safety of lookups
        var settings = Settings;

        

        // Check for low battery notification
        if (prevBatteryLevel != BatteryLevel.Low && batteryInfo.Level == BatteryLevel.Low && !batteryInfo.IsCharging && settings.NotifyOnBatteryLow)
        {
            await _notificationService.ShowSystemNotificationAsync("Low Battery", "Controller battery is low.", NotificationPriority.High, expirationTime: 10);
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
            ControllerInfo.BatteryInfo.ModelName = batteryInfo.ModelName;            
        });

        // Check for controller connection/disconnection notifications (safe to call off UI thread because NotificationService posts to UI thread)
        if (prevIsConnected != batteryInfo.IsConnected)
        {
            var controllerName = ControllerInfo.BatteryInfo.GetControllerDisplayName();

            if (batteryInfo.IsConnected && settings.NotifyOnControllerConnected)
            {
                await _notificationService.ShowSystemNotificationAsync("Controller Connected", $"{controllerName} has been connected.", expirationTime: 3);
            }
            else if (!batteryInfo.IsConnected && settings.NotifyOnControllerDisconnected)
            {
                await _notificationService.ShowSystemNotificationAsync("Controller Disconnected", "Controller has been disconnected.", expirationTime: 3);
            }
        }
    }

    private async void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        if (Application.Current != null)
        {
            await Dispatcher.UIThread.InvokeAsync(() => ThemeVariant = Application.Current.ActualThemeVariant);
        }
    }

    private static void ApplyThemeSetting(string theme)
    {
        if (Application.Current == null) return;

        Application.Current.RequestedThemeVariant = theme switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            "Auto" => ThemeVariant.Default,
            _ => ThemeVariant.Default
        };
    }

    private static void ApplyLanguageSetting(string language)
    {
        var languageCode = LocalizationService.GetLanguageCodeFromSetting(language);
        LocalizationService.Instance.SetLanguage(languageCode);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _settingsSaveDebounceTimer.Stop();
                _settingsSaveDebounceTimer.Dispose();
                if (Application.Current != null)
                {
                    Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
                }
                _batteryService.BatteryInfoChanged -= OnBatteryInfoChanged;
                Settings.PropertyChanged -= Settings_PropertyChanged;
                LocalizationService.Instance.PropertyChanged -= LocalizationChanged;
                _settingsSaveDebounceTimer.Elapsed -= OnSettingsSaveDebounceTimerElapsed;
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
