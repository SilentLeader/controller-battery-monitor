using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace XboxBatteryMonitor.Models;

public partial class Settings : ObservableObject
{
    [ObservableProperty]
    [JsonPropertyName("windowX")]
    private double windowX = -1;

    [ObservableProperty]
    [JsonPropertyName("windowY")]
    private double windowY = -1;

    [ObservableProperty]
    [JsonPropertyName("windowWidth")]
    private double windowWidth = 640;

    [ObservableProperty]
    [JsonPropertyName("windowHeight")]
    private double windowHeight = 450;

    [ObservableProperty]
    [JsonPropertyName("startMinimized")]
    private bool startMinimized = true;

    [ObservableProperty]
    [JsonPropertyName("updateFrequencySeconds")]
    private int updateFrequencySeconds = 5;

    [ObservableProperty]
    [JsonPropertyName("hideTrayIconWhenDisconnected")]
    private bool hideTrayIconWhenDisconnected = false;
}