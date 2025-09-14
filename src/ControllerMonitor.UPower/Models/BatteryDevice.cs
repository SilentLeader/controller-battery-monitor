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
    /// Native system path to the device
    /// </summary>
    public string NativePath { get; init; } = string.Empty;
    
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
    /// Whether this device can supply power to the system
    /// </summary>
    public bool PowerSupply { get; init; }
    
    /// <summary>
    /// Whether the device has historical data available
    /// </summary>
    public bool HasHistory { get; init; }
    
    /// <summary>
    /// Whether the device has statistical data available
    /// </summary>
    public bool HasStatistics { get; init; }
    
    /// <summary>
    /// Whether the device is currently online/present
    /// </summary>
    public bool Online { get; init; }
    
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
    /// Battery design energy when full in Wh
    /// </summary>
    public double EnergyFullDesign { get; init; }
    
    /// <summary>
    /// Current energy consumption/charging rate in W (watts)
    /// </summary>
    public double EnergyRate { get; init; }
    
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
    /// Current warning level
    /// </summary>
    public BatteryLevel WarningLevel { get; init; } = BatteryLevel.Unknown;

    /// <summary>
    /// Current battery level
    /// </summary>
    public BatteryLevel? BatteryLevelCurrent { get; init; } = null;
    
    /// <summary>
    /// Estimated time until empty in seconds (0 if unknown)
    /// </summary>
    public long TimeToEmpty { get; init; }
    
    /// <summary>
    /// Estimated time until full in seconds (0 if unknown)
    /// </summary>
    public long TimeToFull { get; init; }
    
    /// <summary>
    /// Battery health percentage (0-100, where 100 is perfect health)
    /// </summary>
    public double Capacity { get; init; }
    
    /// <summary>
    /// Temperature in degrees Celsius
    /// </summary>
    public double Temperature { get; init; }
    
    /// <summary>
    /// Whether the battery is present in the device
    /// </summary>
    public bool IsPresent { get; init; }
    
    /// <summary>
    /// Whether the battery is rechargeable
    /// </summary>
    public bool IsRechargeable { get; init; }
    
    /// <summary>
    /// Icon name for the current battery state
    /// </summary>
    public string IconName { get; init; } = string.Empty;
    
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
    
    /// <summary>
    /// Gets the estimated time remaining formatted as a string
    /// </summary>
    public string TimeRemainingFormatted
    {
        get
        {
            var seconds = State == BatteryState.Charging ? TimeToFull : TimeToEmpty;
            if (seconds <= 0) return "Unknown";
            
            var timeSpan = TimeSpan.FromSeconds(seconds);
            if (timeSpan.TotalHours >= 1)
                return $"{timeSpan.Hours}h {timeSpan.Minutes}m";
            else
                return $"{timeSpan.Minutes}m";
        }
    }
    
    private static bool IsGamingControllerByName(string name)
    {
        var lowerName = name.ToLowerInvariant();
        var gamingKeywords = new[]
        {
            "xbox", "controller", "wireless controller",
            "dualshock", "dualsense", "playstation",
            "gamepad", "gaming", "pro controller"
        };
        
        return gamingKeywords.Any(keyword => lowerName.Contains(keyword));
    }
    
    public override string ToString()
    {
        return $"{DisplayName}: {Percentage:F1}% ({State})";
    }
}