using CommunityToolkit.Mvvm.ComponentModel;

namespace XboxBatteryMonitor.ViewModels;

public partial class ControllerInfoViewModel : ObservableObject
{
    [ObservableProperty]
    private string name = "Xbox Controller";

    [ObservableProperty]
    private BatteryInfoViewModel batteryInfo = new();
}