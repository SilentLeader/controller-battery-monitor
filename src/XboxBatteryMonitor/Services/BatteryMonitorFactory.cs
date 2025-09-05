using System.Runtime.InteropServices;
using XboxBatteryMonitor.Platforms.Linux;
using XboxBatteryMonitor.Platforms.Windows;

namespace XboxBatteryMonitor.Services;

public static class BatteryMonitorFactory
{
    public static IBatteryMonitorService CreatePlatformService()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return new BatteryMonitorLinux();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new BatteryMonitorWindows();
        }
        else
        {
            // Fallback for other platforms - could be macOS or other
            return new BatteryMonitorWindows(); // Use Windows implementation as fallback
        }
    }
}