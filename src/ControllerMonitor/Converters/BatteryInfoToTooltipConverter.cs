using Avalonia.Data.Converters;
using ControllerMonitor.Services;
using ControllerMonitor.ValueObjects;
using ControllerMonitor.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace ControllerMonitor.Converters;

public class BatteryInfoToTooltipConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var loc = LocalizationService.Instance;

        if (values.Count < 2) 
        {
            return loc["App_Name"];
        }

        if (values[0] is not BatteryInfoViewModel batteryInfo) 
        {
            return loc["App_Name"];
        }

        // Check if we have hideTrayIconWhenDisconnected setting as 4th parameter
        var hideTrayIconWhenDisconnected = values.Count > 4 && (values[4] as bool? ?? false);

        // If controller is disconnected and we should hide tray icon, return default tooltip
        if (batteryInfo.Status == ConnectionStatus.Disconnected && hideTrayIconWhenDisconnected)
        {
            return loc["App_Name"];
        }

        string statusText = batteryInfo.Status switch
        {
            ConnectionStatus.Disconnected => loc["ConnectionStatus_Disconnected"],
            ConnectionStatus.Connected => loc["ConnectionStatus_Connected"],
            ConnectionStatus.Charging => loc["ConnectionStatus_Charging"],
            _ => loc["ConnectionStatus_Unknown"]
        };

        string levelText = batteryInfo.Level switch
        {
            BatteryLevel.Unknown => loc["BatteryLevel_Unknown"],
            BatteryLevel.Empty => loc["BatteryLevel_Empty"],
            BatteryLevel.Low => loc["BatteryLevel_Low"],
            BatteryLevel.Normal => loc["BatteryLevel_Normal"],
            BatteryLevel.High => loc["BatteryLevel_High"],
            BatteryLevel.Full => loc["BatteryLevel_Full"],
            _ => loc["BatteryLevel_Unknown"]
        };

        var modelName = batteryInfo.GetControllerDisplayName();

        string controllerName = !string.IsNullOrWhiteSpace(modelName) ? modelName : loc["Controller_Unknown"];

        return batteryInfo.Status == ConnectionStatus.Disconnected
            ? loc["Controller_Disconnected"]
            : string.Format(loc["Controller_TooltipFormat"], controllerName, levelText, statusText);
    }

    
}