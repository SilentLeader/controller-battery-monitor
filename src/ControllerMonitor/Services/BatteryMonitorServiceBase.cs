using System;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.Logging;
using ControllerMonitor.Models;
using ControllerMonitor.ViewModels;

namespace ControllerMonitor.Services;

using ControllerMonitor.Interfaces;

public abstract class BatteryMonitorServiceBase : IBatteryMonitorService, IDisposable
{
    public event EventHandler<BatteryInfoViewModel?>? BatteryInfoChanged;
    protected readonly ISettingsService _settingsService;
    protected readonly ILogger<IBatteryMonitorService> _logger;
    private Timer? _monitoringTimer;
    private BatteryInfoViewModel? _previousBatteryInfo;
    private bool disposedValue;
    protected object LockObject = new();

    public BatteryMonitorServiceBase(ISettingsService settingsService, ILogger<IBatteryMonitorService> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
        _settingsService.SettingsChanged += SettingsChanged;
    }

    public abstract Task<BatteryInfoViewModel> GetBatteryInfoAsync();

    public void StartMonitoring()
    {
        var settings = _settingsService.GetSettings();
        var updateFreq = settings.UpdateFrequencySeconds < 1 ? 1 : settings.UpdateFrequencySeconds;
        _monitoringTimer = new Timer(TimeSpan.FromSeconds(updateFreq));
        _monitoringTimer.Elapsed += async (obj, arg) =>
        {
            try
            {
                var currentInfo = await GetBatteryInfoAsync();
                lock (LockObject)
                {
                    if (HasBatteryInfoChanged(_previousBatteryInfo, currentInfo))
                    {
                        _previousBatteryInfo = currentInfo;
                        BatteryInfoChanged?.Invoke(this, currentInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Periodic state check error");
            }
        };
        _monitoringTimer.Start();
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _monitoringTimer?.Stop();
                _monitoringTimer?.Dispose();
                _monitoringTimer = null;
                _previousBatteryInfo = null;
                _settingsService.SettingsChanged -= SettingsChanged;
            }

            disposedValue = true;
        }
    }
    
    private void SettingsChanged(object? sender, Settings settings)
    {
        if ((_monitoringTimer?.Interval) == (settings.UpdateFrequencySeconds * 1000))
        {
            return;
        }
        _monitoringTimer?.Stop();
        _monitoringTimer?.Dispose();
        StartMonitoring();
    }

    private static bool HasBatteryInfoChanged(BatteryInfoViewModel? previous, BatteryInfoViewModel? current)
    {
        if (previous == null || current == null) return true;
        return previous.IsConnected != current.IsConnected ||
               previous.Level != current.Level ||
               previous.IsCharging != current.IsCharging ||
               !Equals(previous.Capacity, current.Capacity) ||
               previous.ModelName != current.ModelName;
    }
}
