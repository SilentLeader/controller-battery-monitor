using ControllerMonitor.XInput.ValueObjects;

namespace ControllerMonitor.XInput.Models;

/// <summary>
/// Represents XInput controller battery information
/// </summary>
public class ControllerBatteryInfo
{
    public uint ControllerIndex { get; set; }
    public bool IsConnected { get; set; }
    public byte BatteryType { get; set; }
    public byte BatteryLevel { get; set; }
    public bool IsWired => BatteryType == ValueObjects.BatteryType.BATTERY_TYPE_WIRED;
    public string? ModelName { get; set; } = "Generic controller";
}