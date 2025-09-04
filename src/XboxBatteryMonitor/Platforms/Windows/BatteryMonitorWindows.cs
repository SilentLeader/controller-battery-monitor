using System.Threading.Tasks;
using XboxBatteryMonitor.Models;
using XboxBatteryMonitor.Services;

namespace XboxBatteryMonitor.Platforms.Windows;

public class BatteryMonitorWindows : IBatteryMonitorService
{
    public async Task<BatteryInfo> GetBatteryInfoAsync()
    {
        var batteryInfo = new BatteryInfo { IsConnected = false };

        await Task.Run(() => {
#if WINDOWS
            try
            {
                // Use GameInput on Windows
                using var gameInput = new Microsoft.GameInput.GameInput();

                // Get connected devices
                var devices = gameInput.GetDevices();
                foreach (var device in devices)
                {
                    // Check if it's an Xbox controller (optional filter)
                    if (device.DeviceInfo.DeviceFamily == Microsoft.GameInput.GameInputDeviceFamily.XboxOne ||
                        device.DeviceInfo.DeviceFamily == Microsoft.GameInput.GameInputDeviceFamily.Xbox360)
                    {
                        // Get battery state
                        var batteryState = device.GetBatteryState();

                        batteryInfo.IsConnected = true;
                        batteryInfo.Level = batteryState.Level switch
                        {
                            Microsoft.GameInput.GameInputBatteryLevel.Empty => BatteryLevel.Empty,
                            Microsoft.GameInput.GameInputBatteryLevel.Low => BatteryLevel.Low,
                            Microsoft.GameInput.GameInputBatteryLevel.Medium => BatteryLevel.Normal,
                            Microsoft.GameInput.GameInputBatteryLevel.Full => BatteryLevel.Full,
                            _ => BatteryLevel.Unknown
                        };
                        batteryInfo.IsCharging = batteryState.IsCharging;

                        // If available, set capacity (percentage); otherwise null
                        batteryInfo.Capacity = batteryState.CapacityPercentage.HasValue
                            ? (int?)batteryState.CapacityPercentage.Value : null;

                        // Return for first matching controller
                        break;
                    }
                }
            }
            catch (Exception)
            {
                // Handle errors (e.g., API not available on older Windows)
                batteryInfo.IsConnected = false;
            }
#endif
        });

        return batteryInfo;
    }
}