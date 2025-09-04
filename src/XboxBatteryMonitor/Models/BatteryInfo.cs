using CommunityToolkit.Mvvm.ComponentModel;
using XboxBatteryMonitor.ValueObjects;

namespace XboxBatteryMonitor.Models;

public partial class BatteryInfo : ObservableObject
{
    [ObservableProperty]
    private BatteryLevel level = BatteryLevel.Unknown;

    [ObservableProperty]
    private int? capacity;

    [ObservableProperty]
    private bool isCharging;

    [ObservableProperty]
    private bool isConnected;
}
