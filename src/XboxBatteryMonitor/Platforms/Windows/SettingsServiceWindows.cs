using System;
using System.IO;
using Microsoft.Extensions.Logging;
using XboxBatteryMonitor.Services;
using XboxBatteryMonitor.Interfaces;

namespace XboxBatteryMonitor.Platforms.Windows
{
    public class SettingsServiceWindows(ILogger<SettingsServiceBase> logger) : SettingsServiceBase(logger)
    {
        protected override string GetSettingsFolderPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ControllerMonitor");
        }
    }
}