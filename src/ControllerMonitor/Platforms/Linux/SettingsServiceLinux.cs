using System;
using System.IO;
using Microsoft.Extensions.Logging;
using ControllerMonitor.Services;

namespace ControllerMonitor.Platforms.Linux
{
    public class SettingsServiceLinux(ILogger<SettingsServiceBase> logger) : SettingsServiceBase(logger)
    {
        protected override string GetSettingsFolderPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".config", ".controller-monitor");
        }
    }
}