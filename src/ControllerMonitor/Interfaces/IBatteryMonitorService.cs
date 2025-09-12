using System;
using System.Threading.Tasks;
using ControllerMonitor.ViewModels;

namespace ControllerMonitor.Interfaces;

public interface IBatteryMonitorService
{
    Task<BatteryInfoViewModel> GetBatteryInfoAsync();

    event EventHandler<BatteryInfoViewModel?>? BatteryInfoChanged;

    void StartMonitoring();
}
