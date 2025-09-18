using ControllerMonitor.UPower.ValueObjects;

namespace ControllerMonitor.UPower.Models;

/// <summary>
/// Strongly-typed model representing a UPower battery device
/// </summary>
public sealed record BatteryDevice
{
    /// <summary>
    /// Device object path in D-Bus
    /// </summary>
    public string ObjectPath { get; init; } = string.Empty;
    
    /// <summary>
    /// Device vendor/manufacturer
    /// </summary>
    public string Vendor { get; init; } = string.Empty;
    
    /// <summary>
    /// Device model name
    /// </summary>
    public string Model { get; init; } = string.Empty;
    
    /// <summary>
    /// Device serial number
    /// </summary>
    public string Serial { get; init; } = string.Empty;
    
    /// <summary>
    /// Type of the device
    /// </summary>
    public DeviceType Type { get; init; } = DeviceType.Unknown;
    
    /// <summary>
    /// Battery energy in Wh (watt-hours)
    /// </summary>
    public double Energy { get; init; }
    
    /// <summary>
    /// Battery energy when empty in Wh
    /// </summary>
    public double EnergyEmpty { get; init; }
    
    /// <summary>
    /// Battery energy when full in Wh
    /// </summary>
    public double EnergyFull { get; init; }
    
    /// <summary>
    /// Current voltage in V (volts)
    /// </summary>
    public double Voltage { get; init; }
    
    /// <summary>
    /// Current charge percentage (0-100)
    /// </summary>
    public double Percentage { get; init; }
    
    /// <summary>
    /// Current battery state
    /// </summary>
    public BatteryState State { get; init; } = BatteryState.Unknown;
    
    /// <summary>
    /// Battery technology type
    /// </summary>
    public BatteryTechnology Technology { get; init; } = BatteryTechnology.Unknown;
    
    /// <summary>
    /// Current battery level
    /// </summary>
    public BatteryLevel? BatteryLevelCurrent { get; init; } = null;
    
    /// <summary>
    /// Battery health percentage (0-100, where 100 is perfect health)
    /// </summary>
    public double Capacity { get; init; }
    
    /// <summary>
    /// Whether the battery is rechargeable
    /// </summary>
    public bool IsRechargeable { get; init; }
    
    /// <summary>
    /// Timestamp when this information was last updated
    /// </summary>
    public DateTimeOffset UpdateTime { get; init; }
    
    /// <summary>
    /// Gets a user-friendly display name for this device
    /// </summary>
    public string DisplayName => !string.IsNullOrEmpty(Model) ? $"{Model}{(!string.IsNullOrEmpty(Serial) ? $" [{Serial}]" : string.Empty)}" :
                                !string.IsNullOrEmpty(Vendor) ? Vendor :
                                Type.ToString();
    
    /// <summary>
    /// Gets whether this appears to be a gaming controller
    /// </summary>
    public bool IsGamingController => Type == DeviceType.GamingInput ||
                                     IsGamingControllerByName(DisplayName);
    
    private static bool IsGamingControllerByName(string name)
    {
        var lowerName = name.ToLowerInvariant();
        var gamingKeywords = new[]
        {
            "xbox", "controller", "wireless controller",
            "dualshock", "dualsense", "playstation",
            "gamepad", "gaming", "pro controller"
        };
        
        return gamingKeywords.Any(lowerName.Contains);
    }
}