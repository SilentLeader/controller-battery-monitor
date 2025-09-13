using CommunityToolkit.Mvvm.ComponentModel;
using ControllerMonitor.ValueObjects;

namespace ControllerMonitor.ViewModels;

public partial class BatteryInfoViewModel : ObservableObject
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