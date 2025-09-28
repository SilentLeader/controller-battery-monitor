using ControllerMonitor.XInput.ValueObjects;

namespace ControllerMonitor.XInput.Helpers;


/// <summary>
/// Helper class for converting XInput battery information to application battery levels
/// </summary>
public static class BatteryLevelConverter
{
    /// <summary>
    /// Converts XInput battery level to application battery level
    /// </summary>
    /// <param name="xInputLevel">XInput battery level</param>
    /// <returns>Application battery level</returns>
    public static XInputBatteryLevel ConvertBatteryLevel(byte xInputLevel)
    {
        return xInputLevel switch
        {
            XInputBatteryLevelConstants.BATTERY_LEVEL_EMPTY => XInputBatteryLevel.Empty,
            XInputBatteryLevelConstants.BATTERY_LEVEL_LOW => XInputBatteryLevel.Low,
            XInputBatteryLevelConstants.BATTERY_LEVEL_MEDIUM => XInputBatteryLevel.Normal,
            XInputBatteryLevelConstants.BATTERY_LEVEL_FULL => XInputBatteryLevel.Full,
            _ => XInputBatteryLevel.Unknown
        };
    }
}