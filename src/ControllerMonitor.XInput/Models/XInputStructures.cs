using System.Runtime.InteropServices;

namespace ControllerMonitor.XInput.Models;

/// <summary>
/// XInput battery information structure
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct XInputBatteryInformation
{
    public byte BatteryType;
    public byte BatteryLevel;
}

/// <summary>
/// XInput gamepad state structure
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct XInputState
{
    public uint dwPacketNumber;
    public XInputGamepad Gamepad;
}

/// <summary>
/// XInput gamepad structure
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct XInputGamepad
{
    public ushort wButtons;
    public byte bLeftTrigger;
    public byte bRightTrigger;
    public short sThumbLX;
    public short sThumbLY;
    public short sThumbRX;
    public short sThumbRY;
}