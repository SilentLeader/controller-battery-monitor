using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using ControllerMonitor.Models;

namespace ControllerMonitor.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private Settings _data;

    public SettingsViewModel(Settings? data = null)
    {
        _data = data ?? new Settings();
        // Initialize observable properties from data
        WindowX = _data.WindowX;
        WindowY = _data.WindowY;
        WindowWidth = _data.WindowWidth;
        WindowHeight = _data.WindowHeight;
        StartMinimized = _data.StartMinimized;
        MinimizeToTray = _data.MinimizeToTray;
        UpdateFrequencySeconds = _data.UpdateFrequencySeconds;
        HideTrayIconWhenDisconnected = _data.HideTrayIconWhenDisconnected;        
        NotifyOnControllerConnected = _data.NotifyOnControllerConnected;
        NotifyOnControllerDisconnected = _data.NotifyOnControllerDisconnected;
        NotifyOnBatteryLow = _data.NotifyOnBatteryLow;
        Theme = _data.Theme;
        Language = _data.Language ?? "Auto";
    }

    [ObservableProperty]
    private double windowX;

    [ObservableProperty]
    private double windowY;

    [ObservableProperty]
    private double windowWidth;

    [ObservableProperty]
    private double windowHeight;

    [ObservableProperty]
    private bool startMinimized;

    [ObservableProperty]
    private bool minimizeToTray;

    [ObservableProperty]
    private int updateFrequencySeconds;

    [ObservableProperty]
    private bool hideTrayIconWhenDisconnected;

    [ObservableProperty]
    private bool notifyOnControllerConnected;

    [ObservableProperty]
    private bool notifyOnControllerDisconnected;

    [ObservableProperty]
    private bool notifyOnBatteryLow;

    [ObservableProperty]
    private string theme;

    [ObservableProperty]
    private string language = "Auto";

    // Method to convert back to SettingsData for serialization
    public Settings ToSettingsData()
    {
        return new Settings
        {
            WindowX = WindowX,
            WindowY = WindowY,
            WindowWidth = WindowWidth,
            WindowHeight = WindowHeight,
            StartMinimized = StartMinimized,
            MinimizeToTray = MinimizeToTray,
            UpdateFrequencySeconds = UpdateFrequencySeconds,
            HideTrayIconWhenDisconnected = HideTrayIconWhenDisconnected,
            NotifyOnControllerConnected = NotifyOnControllerConnected,
            NotifyOnControllerDisconnected = NotifyOnControllerDisconnected,
            NotifyOnBatteryLow = NotifyOnBatteryLow,
            Theme = Theme,
            Language = Language
        };
    }
}