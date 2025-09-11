using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
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

    public async Task<SettingsViewModel> LoadSettingsAsync()
    {
        _logger.LogInformation("Loading settings from {Path}", _settingsFilePath);
        if (!File.Exists(_settingsFilePath))
        {
            _logger.LogInformation("Settings file does not exist, returning default settings");
            return new SettingsViewModel();
        }

        try
        {
            var json = await File.ReadAllTextAsync(_settingsFilePath).ConfigureAwait(false);
            var data = JsonSerializer.Deserialize(json, _jsonContext.SettingsData) ?? new SettingsData();
            _logger.LogInformation("Settings loaded successfully");
            return new SettingsViewModel(data);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load settings, returning defaults");
            return new SettingsViewModel();
        }
    }

    public async Task SaveSettingsAsync(SettingsViewModel settings)
    {
        _logger.LogInformation("Saving settings to {Path}", _settingsFilePath);
        try
        {
            var data = settings.ToSettingsData();
            var json = JsonSerializer.Serialize(data, _jsonContext.SettingsData);
            await File.WriteAllTextAsync(_settingsFilePath, json).ConfigureAwait(false);
            _logger.LogInformation("Settings saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save settings");
        }
    }

    // Keep synchronous wrappers for compatibility
    public SettingsViewModel LoadSettings()
    {
        return LoadSettingsAsync().GetAwaiter().GetResult();
    }

    public void SaveSettings(SettingsViewModel settings)
    {
        SaveSettingsAsync(settings).GetAwaiter().GetResult();
    }
}