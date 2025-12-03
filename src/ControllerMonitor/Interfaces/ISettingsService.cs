using System;
using System.Threading.Tasks;
using ControllerMonitor.Models;

namespace ControllerMonitor.Interfaces;

public interface ISettingsService
{
    event EventHandler<Settings>? SettingsChanged;

    Settings GetSettings();
    
    void LoadSettings();
    
    Task SaveSettingsAsync(Settings? settings);
}
