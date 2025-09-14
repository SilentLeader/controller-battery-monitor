namespace ControllerMonitor.UPower.ValueObjects;

/// <summary>
/// Battery warning levels
/// </summary>
public enum BatteryLevel
{
    Unknown,
    none,
    Discharging,
    Low,
    Critical,
    Action,
    Normal,
    High,
    Full,
    Last,
}