using CommunityToolkit.Mvvm.ComponentModel;

namespace XboxBatteryMonitor.Models;

public partial class ControllerInfo : ObservableObject
{
    [ObservableProperty]
    private string name = "Xbox Controller";

    [ObservableProperty]
    private BatteryInfo batteryInfo = new();
}
