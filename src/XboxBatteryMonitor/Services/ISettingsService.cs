using System;
using System.Threading.Tasks;
using XboxBatteryMonitor.Models;

namespace XboxBatteryMonitor.Services;

public interface ISettingsService
{
    event EventHandler<Settings>? SettingsChanged;

    Settings GetSettings();
    void LoadSettings();
    void SaveSettings(Settings? settings);
    Task SaveSettingsAsync(Settings? settings);
}
