using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ControllerMonitor.Models;
using Microsoft.Extensions.Logging;

namespace ControllerMonitor.Services;

using ControllerMonitor.Interfaces;

public abstract class SettingsServiceBase : ISettingsService
{
    private readonly ILogger<SettingsServiceBase> _logger;
    private readonly string _settingsFilePath;
    private readonly SettingsJsonContext _jsonContext;

    private Settings _settings = new();

    public event EventHandler<Settings>? SettingsChanged;

    public SettingsServiceBase(ILogger<SettingsServiceBase> logger)
    {
        _logger = logger;
        var configDir = GetSettingsFolderPath();
        Directory.CreateDirectory(configDir);
        _settingsFilePath = Path.Combine(configDir, "settings.json");

        _jsonContext = new SettingsJsonContext(new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    public void LoadSettings()
    {
        _logger.LogInformation("Loading settings from {Path}", _settingsFilePath);
        if (!File.Exists(_settingsFilePath))
        {
            _logger.LogInformation("Settings file does not exist");
            return;
        }

        try
        {
            var json = File.ReadAllText(_settingsFilePath);
            _settings = JsonSerializer.Deserialize(json, _jsonContext.Settings) ?? _settings;
            _logger.LogInformation("Settings loaded successfully");
            SettingsChanged?.Invoke(this, GetSettings());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load settings, using defaults");
        }
    }

    public async Task SaveSettingsAsync(Settings? settings)
    {
        _logger.LogInformation("Saving settings to {Path}", _settingsFilePath);
        try
        {
            var json = JsonSerializer.Serialize(settings ?? _settings, _jsonContext.Settings);
            await File.WriteAllTextAsync(_settingsFilePath, json).ConfigureAwait(false);
            if (settings != null)
            {
                _settings = settings;
                SettingsChanged?.Invoke(this, GetSettings());
            }
            _logger.LogInformation("Settings saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save settings");
        }
    }

    public void SaveSettings(Settings? settings)
    {
        SaveSettingsAsync(settings).GetAwaiter().GetResult();
    }

    public Settings GetSettings()
    {
        return _settings.ShallowCopy();
    }

    protected abstract string GetSettingsFolderPath();
}