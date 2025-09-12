#if WINDOWS
using System;
using System.Runtime.InteropServices;
#endif
using System.Threading.Tasks;
using XboxBatteryMonitor.ViewModels;
using XboxBatteryMonitor.Services;
using Microsoft.Extensions.Logging;

namespace XboxBatteryMonitor.Platforms.Windows;

public class BatteryMonitorWindows(ISettingsService settingsService, ILogger<IBatteryMonitorService> logger) : BatteryMonitorServiceBase(settingsService, logger)
{
    public override async Task<BatteryInfoViewModel> GetBatteryInfoAsync()
    {
        var batteryInfo = new BatteryInfoViewModel { IsConnected = false };

        await Task.Run(() => {
#if WINDOWS
            try
            {
                // Check up to 4 XInput controllers
                for (uint i = 0; i < 4; i++)
                {
                    var state = new XInputState();
                    uint result = XInputGetState(i, ref state);
                    
                    if (result == 0) // ERROR_SUCCESS
                    {
                        // Controller is connected, get battery info
                        var batteryInfoXInput = new XInputBatteryInformation();
                        uint batteryResult = XInputGetBatteryInformation(i, BatteryDeviceType.BATTERY_DEVTYPE_GAMEPAD, ref batteryInfoXInput);
                        
                        if (batteryResult == 0)
                        {
                            batteryInfo.IsConnected = true;
                            batteryInfo.Level = ConvertBatteryLevel(batteryInfoXInput.BatteryLevel);
                            batteryInfo.IsCharging = batteryInfoXInput.BatteryType == BatteryType.BATTERY_TYPE_WIRED;
                            batteryInfo.Capacity = null; // XInput doesn't provide percentage
                            
                            // Return info for first connected controller
                            break;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Handle any errors gracefully
                batteryInfo.IsConnected = false;
            }
#endif
        });

        return batteryInfo;
    }

#if WINDOWS
    // XInput P/Invoke declarations
    [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
    private static extern uint XInputGetState(uint dwUserIndex, ref XInputState pState);

    [DllImport("xinput1_4.dll", EntryPoint = "XInputGetBatteryInformation")]
    private static extern uint XInputGetBatteryInformation(uint dwUserIndex, byte devType, ref XInputBatteryInformation pBatteryInformation);

    [StructLayout(LayoutKind.Sequential)]
    private struct XInputState
    {
        public uint dwPacketNumber;
        public XInputGamepad Gamepad;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XInputGamepad
    {
        public ushort wButtons;
        public byte bLeftTrigger;
        public byte bRightTrigger;
        public short sThumbLX;
        public short sThumbLY;
        public short sThumbRX;
        public short sThumbRY;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XInputBatteryInformation
    {
        public byte BatteryType;
        public byte BatteryLevel;
    }

    private static class BatteryDeviceType
    {
        public const byte BATTERY_DEVTYPE_GAMEPAD = 0x00;
        public const byte BATTERY_DEVTYPE_HEADSET = 0x01;
    }

    private static class BatteryType
    {
        public const byte BATTERY_TYPE_DISCONNECTED = 0x00;
        public const byte BATTERY_TYPE_WIRED = 0x01;
        public const byte BATTERY_TYPE_ALKALINE = 0x02;
        public const byte BATTERY_TYPE_NIMH = 0x03;
        public const byte BATTERY_TYPE_UNKNOWN = 0xFF;
    }

    private static class BatteryLevel
    {
        public const byte BATTERY_LEVEL_EMPTY = 0x00;
        public const byte BATTERY_LEVEL_LOW = 0x01;
        public const byte BATTERY_LEVEL_MEDIUM = 0x02;
        public const byte BATTERY_LEVEL_FULL = 0x03;
    }

    private static ValueObjects.BatteryLevel ConvertBatteryLevel(byte xInputLevel)
    {
        return xInputLevel switch
        {
            BatteryLevel.BATTERY_LEVEL_EMPTY => ValueObjects.BatteryLevel.Empty,
            BatteryLevel.BATTERY_LEVEL_LOW => ValueObjects.BatteryLevel.Low,
            BatteryLevel.BATTERY_LEVEL_MEDIUM => ValueObjects.BatteryLevel.Normal,
            BatteryLevel.BATTERY_LEVEL_FULL => ValueObjects.BatteryLevel.Full,
            _ => ValueObjects.BatteryLevel.Unknown
        };
    }
#endif
}