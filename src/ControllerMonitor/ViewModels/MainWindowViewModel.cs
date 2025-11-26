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
using ControllerMonitor.Services;
using ControllerMonitor.ValueObjects;
using Microsoft.Extensions.Logging;

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
    private string appName;

    [ObservableProperty]
    private string appDescription;

    [ObservableProperty]
    private string appVersion;

    [ObservableProperty]
    private ThemeVariant? themeVariant;

    private bool disposedValue;

    /// <summary>
    /// Gets a value indicating whether the current platform is Linux
    /// </summary>
    public bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

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
        LocalizationService.Instance.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(LocalizationService.CurrentCulture))
            {
                AppName = LocalizationService.Instance["App_Name"];
                AppDescription = LocalizationService.Instance["App_Description"];
            }
        };

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
                    ControllerInfo.BatteryInfo.ModelName = initialInfo.ModelName;

                    // Set initial controller name with fallback logic
                    ControllerInfo.Name = GetControllerDisplayName(initialInfo);
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed initialize battery info");
            }
        });
    }

    private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.UpdateFrequencySeconds))
        {
        }
        else if (e.PropertyName == nameof(Settings.Theme))
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
            var controllerName = GetControllerDisplayName(batteryInfo);
            
            if (batteryInfo.IsConnected && settings.NotifyOnControllerConnected)
            {
                await _notificationService.ShowSystemNotificationAsync("Controller Connected", $"{controllerName} has been connected.", expirationTime: 3);
            }
            else if (!batteryInfo.IsConnected && settings.NotifyOnControllerDisconnected)
            {
                await _notificationService.ShowSystemNotificationAsync("Controller Disconnected", $"Controller has been disconnected.", expirationTime: 3);
            }
        }

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

            // Update controller name with fallback logic
            ControllerInfo.Name = GetControllerDisplayName(batteryInfo);
        });
    }

    private async void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        if (Application.Current != null)
        {
            await Dispatcher.UIThread.InvokeAsync(() => ThemeVariant = Application.Current.ActualThemeVariant);
        }
    }

    private void ApplyThemeSetting(string theme)
    {
        if (Application.Current == null) return;

        var themeVariant = theme switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            "Auto" => ThemeVariant.Default,
            _ => ThemeVariant.Default
        };

        Application.Current.RequestedThemeVariant = themeVariant;
    }

    private void ApplyLanguageSetting(string language)
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
                _debounceTimer.Stop();
                _debounceTimer.Dispose();
                if (Application.Current != null)
                {
                    Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
                }
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

    private static string GetControllerDisplayName(BatteryInfoViewModel batteryInfo)
    {
        if (batteryInfo?.IsConnected != true)
            return LocalizationService.Instance["Controller_Unknown"];

        return !string.IsNullOrWhiteSpace(batteryInfo.ModelName)
            ? batteryInfo.ModelName
            : LocalizationService.Instance["Controller_Unknown"];
    }
}
