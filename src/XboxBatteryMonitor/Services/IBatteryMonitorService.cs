using System.Threading.Tasks;
using XboxBatteryMonitor.Models;

namespace XboxBatteryMonitor.Services;

public interface IBatteryMonitorService
{
    Task<BatteryInfo> GetBatteryInfoAsync();
}
