using System;
using System.IO;
using System.Text.Json;
using XboxBatteryMonitor.Models;
using XboxBatteryMonitor.ViewModels;
using Microsoft.Extensions.Logging;

namespace XboxBatteryMonitor.Services;

public class SettingsService
{
    private readonly ILogger<SettingsService> _logger;
    private readonly string _settingsFilePath;
    private readonly SettingsJsonContext _jsonContext;

    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;
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
        _logger.LogInformation("Loading settings from {Path}", _settingsFilePath);
        if (!File.Exists(_settingsFilePath))
        {
            _logger.LogInformation("Settings file does not exist, returning default settings");
            return new SettingsViewModel();
        }

        var json = File.ReadAllText(_settingsFilePath);
        var data = JsonSerializer.Deserialize(json, _jsonContext.SettingsData) ?? new SettingsData();
        _logger.LogInformation("Settings loaded successfully");
        return new SettingsViewModel(data);
    }

    public void SaveSettings(SettingsViewModel settings)
    {
        _logger.LogInformation("Saving settings to {Path}", _settingsFilePath);
        var data = settings.ToSettingsData();
        var json = JsonSerializer.Serialize(data, _jsonContext.SettingsData);
        File.WriteAllText(_settingsFilePath, json);
        _logger.LogInformation("Settings saved successfully");
    }
}