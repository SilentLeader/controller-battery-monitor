namespace ControllerMonitor.UPower.ValueObjects;

/// <summary>
/// Battery charging/discharging states
/// </summary>
public enum BatteryState
{
    Unknown = 0,
    Charging = 1,
    Discharging = 2,
    Empty = 3,
    FullyCharged = 4,
    PendingCharge = 5,
    PendingDischarge = 6
}