using System;
using System.IO;
using System.Text.Json;
using XboxBatteryMonitor.Models;
using XboxBatteryMonitor.ViewModels;

namespace XboxBatteryMonitor.Services;

public class SettingsService
{
    private readonly string _settingsFilePath;
    private readonly SettingsJsonContext _jsonContext;

    public SettingsService()
    {
        var appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".xboxbatterymonitor");
        Directory.CreateDirectory(appDataDir);
        _settingsFilePath = Path.Combine(appDataDir, "settings.json");

        _jsonContext = new SettingsJsonContext(new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    public SettingsViewModel LoadSettings()
    {
        if (!File.Exists(_settingsFilePath))
        {
            return new SettingsViewModel();
        }

        var json = File.ReadAllText(_settingsFilePath);
        var data = JsonSerializer.Deserialize(json, _jsonContext.SettingsData) ?? new SettingsData();
        return new SettingsViewModel(data);
    }

    public void SaveSettings(SettingsViewModel settings)
    {
        var data = settings.ToSettingsData();
        var json = JsonSerializer.Serialize(data, _jsonContext.SettingsData);
        File.WriteAllText(_settingsFilePath, json);
    }
}