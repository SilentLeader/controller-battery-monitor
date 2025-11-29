using System;
using System.Threading.Tasks;
using ControllerMonitor.Models;

namespace ControllerMonitor.Interfaces;

public interface IBatteryMonitorService
{
    Task<BatteryInfo> GetBatteryInfoAsync();

    event EventHandler<BatteryInfo>? BatteryInfoChanged;

    void StartMonitoring();
}
