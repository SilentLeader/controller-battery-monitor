using System.Text.Json.Serialization;

namespace ControllerMonitor.Models;

public class Settings
{
    [JsonPropertyName("windowX")]
    public double WindowX { get; set; } = -1;

    [JsonPropertyName("windowY")]
    public double WindowY { get; set; } = -1;

    [JsonPropertyName("windowWidth")]
    public double WindowWidth { get; set; } = 640;

    [JsonPropertyName("windowHeight")]
    public double WindowHeight { get; set; } = 450;

    [JsonPropertyName("startMinimized")]
    public bool StartMinimized { get; set; } = true;

    [JsonPropertyName("minimizeToTray")]
    public bool MinimizeToTray { get; set; } = true;

    [JsonPropertyName("updateFrequencySeconds")]
    public int UpdateFrequencySeconds { get; set; } = 5;

    [JsonPropertyName("hideTrayIconWhenDisconnected")]
    public bool HideTrayIconWhenDisconnected { get; set; } = false;

    [JsonPropertyName("notifyOnControllerConnected")]
    public bool NotifyOnControllerConnected { get; set; } = true;

    [JsonPropertyName("notifyOnControllerDisconnected")]
    public bool NotifyOnControllerDisconnected { get; set; } = true;

    [JsonPropertyName("notifyOnBatteryLow")]
    public bool NotifyOnBatteryLow { get; set; } = true;

    [JsonPropertyName("theme")]
    public string Theme { get; set; } = "Auto";

    [JsonPropertyName("language")]
    public string? Language { get; set; } = "Auto";

    public Settings ShallowCopy()
    {
        return (Settings)MemberwiseClone();
    }
}
