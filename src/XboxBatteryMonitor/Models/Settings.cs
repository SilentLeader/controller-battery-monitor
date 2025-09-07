using CommunityToolkit.Mvvm.ComponentModel;

namespace XboxBatteryMonitor.Models;

public partial class Settings : ObservableObject
{
    private SettingsData _data;

    public Settings(SettingsData? data = null)
    {
        _data = data ?? new SettingsData();
        // Initialize observable properties from data
        WindowX = _data.WindowX;
        WindowY = _data.WindowY;
        WindowWidth = _data.WindowWidth;
        WindowHeight = _data.WindowHeight;
        StartMinimized = _data.StartMinimized;
        UpdateFrequencySeconds = _data.UpdateFrequencySeconds;
        HideTrayIconWhenDisconnected = _data.HideTrayIconWhenDisconnected;
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
    private int updateFrequencySeconds;

    [ObservableProperty]
    private bool hideTrayIconWhenDisconnected;

    // Method to convert back to SettingsData for serialization
    public SettingsData ToSettingsData()
    {
        return new SettingsData
        {
            WindowX = WindowX,
            WindowY = WindowY,
            WindowWidth = WindowWidth,
            WindowHeight = WindowHeight,
            StartMinimized = StartMinimized,
            UpdateFrequencySeconds = UpdateFrequencySeconds,
            HideTrayIconWhenDisconnected = HideTrayIconWhenDisconnected
        };
    }
}