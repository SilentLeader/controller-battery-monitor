using System;
using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace ControllerMonitor.Services;

public class LocalizationService : INotifyPropertyChanged
{
    private static LocalizationService? _instance;
    private readonly ResourceManager _resourceManager;
    private CultureInfo _currentCulture;

    public static LocalizationService Instance => _instance ??= new LocalizationService();

    public event PropertyChangedEventHandler? PropertyChanged;

    private LocalizationService()
    {
        _resourceManager = new ResourceManager("ControllerMonitor.Resources.Strings", typeof(LocalizationService).Assembly);
        _currentCulture = CultureInfo.CurrentUICulture;
    }

    public CultureInfo CurrentCulture
    {
        get => _currentCulture;
        set
        {
            if (_currentCulture != value)
            {
                _currentCulture = value;
                CultureInfo.CurrentUICulture = value;
                OnPropertyChanged(nameof(CurrentCulture));
                OnPropertyChanged("Item[]"); // Notify that all indexed properties have changed
            }
        }
    }

    public string this[string key]
    {
        get
        {
            try
            {
                return _resourceManager.GetString(key, _currentCulture) ?? key;
            }
            catch
            {
                return key;
            }
        }
    }

    public void SetLanguage(string languageCode)
    {
        CultureInfo culture = languageCode.ToLower() switch
        {
            "auto" => CultureInfo.InstalledUICulture,
            "en" => new CultureInfo("en"),
            "hu" => new CultureInfo("hu"),
            "es" => new CultureInfo("es"),
            "de" => new CultureInfo("de"),
            "fr" => new CultureInfo("fr"),
            "pt-br" => new CultureInfo("pt-BR"),
            _ => CultureInfo.InstalledUICulture
        };

        CurrentCulture = culture;
    }

    public static string GetLanguageCodeFromSetting(string? setting)
    {
        if (string.IsNullOrEmpty(setting))
            return "auto";

        return setting.ToLower() switch
        {
            "auto" => "auto",
            "english" => "en",
            "magyar (hungarian)" => "hu",
            "magyar" => "hu",
            "hungarian" => "hu",
            "español (spanish)" => "es",
            "español" => "es",
            "spanish" => "es",
            "deutsch (german)" => "de",
            "deutsch" => "de",
            "german" => "de",
            "français (french)" => "fr",
            "français" => "fr",
            "french" => "fr",
            "português (portuguese)" => "pt-br",
            "português" => "pt-br",
            "portuguese" => "pt-br",
            _ => "auto"
        };
    }

    public static string GetSettingFromLanguageCode(string code)
    {
        return code.ToLower() switch
        {
            "auto" => "Auto",
            "en" => "English",
            "hu" => "Magyar (Hungarian)",
            "es" => "Español (Spanish)",
            "de" => "Deutsch (German)",
            "fr" => "Français (French)",
            "pt-br" => "Português (Portuguese)",
            _ => "Auto"
        };
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
