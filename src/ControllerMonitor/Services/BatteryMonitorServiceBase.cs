using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ControllerMonitor.Models;

namespace ControllerMonitor.Services;

using System.Threading;
using ControllerMonitor.Interfaces;

public abstract class BatteryMonitorServiceBase : IBatteryMonitorService, IDisposable
{
    public event EventHandler<BatteryInfo>? BatteryInfoChanged;
    protected readonly ISettingsService _settingsService;
    protected readonly ILogger<IBatteryMonitorService> _logger;
    private System.Timers.Timer? _monitoringTimer;
    private BatteryInfo _previousBatteryInfo = new();
    private DateTime? _lastUpdate;

    // Update frequency
    private TimeSpan _updateFreq = TimeSpan.FromSeconds(1);
    private bool disposedValue;
    private static readonly SemaphoreSlim _lockObject = new(1, 1);

    public BatteryMonitorServiceBase(ISettingsService settingsService, ILogger<IBatteryMonitorService> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
        _settingsService.SettingsChanged += SettingsChanged;
    }

    public async Task<BatteryInfo> GetBatteryInfoAsync()
    {
        await UpdateBatteryInfo();
        return _previousBatteryInfo;
    }

    protected abstract Task<BatteryInfo> GetBatteryInfoInternalAsync();

    public void StartMonitoring()
    {
        var settings = _settingsService.GetSettings();
        var updateFreq = settings.UpdateFrequencySeconds < 1 ? 1 : settings.UpdateFrequencySeconds;
        _updateFreq = TimeSpan.FromSeconds(updateFreq);
        _monitoringTimer = new System.Timers.Timer(_updateFreq);
        _monitoringTimer.Elapsed += async (obj, arg) =>
        {
            await UpdateBatteryInfo(true);
        };
        _monitoringTimer.Start();
    }

    private async Task UpdateBatteryInfo(bool force = false)
    {
        await _lockObject.WaitAsync();
        try
        {   
            if(!force 
                && _lastUpdate != null 
                && (DateTime.Now - _lastUpdate) < _updateFreq)
            {
                return;
            }

            var currentInfo = await GetBatteryInfoInternalAsync();     
            if (_previousBatteryInfo != currentInfo)
            {
                _previousBatteryInfo = currentInfo;
                BatteryInfoChanged?.Invoke(this, currentInfo);
            }
            _lastUpdate = DateTime.Now;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Periodic state check error");
        }
        finally
        {
            _lockObject.Release();
        }
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
}
