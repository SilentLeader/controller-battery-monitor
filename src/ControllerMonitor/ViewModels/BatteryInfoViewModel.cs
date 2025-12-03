using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using ControllerMonitor.Services;
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

    public CultureInfo CultureInfo => LocalizationService.Instance.CurrentCulture;

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

    public string GetControllerDisplayName()
    {
        if (IsConnected != true)
            return "Unknown Controller";
            
        return !string.IsNullOrWhiteSpace(ModelName)
            ? ModelName
            : "Unknown Controller";
    }
}