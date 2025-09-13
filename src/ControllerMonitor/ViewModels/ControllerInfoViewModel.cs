using CommunityToolkit.Mvvm.ComponentModel;

namespace ControllerMonitor.ViewModels;

public partial class ControllerInfoViewModel : ObservableObject
{
    [ObservableProperty]
    private string name = "Game Controller";

    [ObservableProperty]
    private BatteryInfoViewModel batteryInfo = new();
}