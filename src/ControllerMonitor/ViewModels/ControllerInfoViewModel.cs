using CommunityToolkit.Mvvm.ComponentModel;

namespace ControllerMonitor.ViewModels;

public partial class ControllerInfoViewModel : ObservableObject
{
    [ObservableProperty]
    private BatteryInfoViewModel batteryInfo = new();
}