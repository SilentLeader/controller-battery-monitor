using System;
using System.IO;
using Microsoft.Extensions.Logging;
using ControllerMonitor.Services;

namespace ControllerMonitor.Platforms.Windows
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