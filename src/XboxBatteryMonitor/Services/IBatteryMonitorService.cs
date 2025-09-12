using System;
using System.Threading.Tasks;
using XboxBatteryMonitor.ViewModels;

namespace XboxBatteryMonitor.Services;

public interface IBatteryMonitorService
{
    Task<BatteryInfoViewModel> GetBatteryInfoAsync();

    event EventHandler<BatteryInfoViewModel?>? BatteryInfoChanged;

    void StartMonitoring();
}
