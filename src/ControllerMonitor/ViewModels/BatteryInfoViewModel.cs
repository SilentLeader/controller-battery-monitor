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

    [ObservableProperty]
    private string? modelName;

    public ConnectionStatus Status =>
        !IsConnected ? ConnectionStatus.Disconnected :
        IsCharging ? ConnectionStatus.Charging :
        ConnectionStatus.Connected;

    partial void OnIsChargingChanged(bool value)
    {
        OnPropertyChanged(nameof(Status));
    }

    partial void OnIsConnectedChanged(bool value)
    {
        OnPropertyChanged(nameof(Status));
    }
}