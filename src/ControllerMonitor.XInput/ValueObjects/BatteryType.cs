namespace ControllerMonitor.XInput.ValueObjects;

/// <summary>
/// XInput battery types
/// </summary>
public static class BatteryType
{
    public const byte BATTERY_TYPE_DISCONNECTED = 0x00;
    public const byte BATTERY_TYPE_WIRED = 0x01;
    public const byte BATTERY_TYPE_ALKALINE = 0x02;
    public const byte BATTERY_TYPE_NIMH = 0x03;
    public const byte BATTERY_TYPE_UNKNOWN = 0xFF;
}