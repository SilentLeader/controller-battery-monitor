namespace ControllerMonitor.XInput.ValueObjects;

/// <summary>
/// XInput battery levels
/// </summary>
public static class XInputBatteryLevelConstants
{
    public const byte BATTERY_LEVEL_EMPTY = 0x00;
    public const byte BATTERY_LEVEL_LOW = 0x01;
    public const byte BATTERY_LEVEL_MEDIUM = 0x02;
    public const byte BATTERY_LEVEL_FULL = 0x03;
}