using Avalonia.Data.Converters;
using ControllerMonitor.Services;
using ControllerMonitor.ValueObjects;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace ControllerMonitor.Converters;

public class BatteryInfoToTooltipConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var loc = LocalizationService.Instance;

        if (values.Count < 2) return loc["App_Name"];

        var level = values[0] as BatteryLevel? ?? BatteryLevel.Unknown;
        var status = values[1] as ConnectionStatus? ?? ConnectionStatus.Disconnected;
        var modelName = values.Count > 2 ? values[2] as string : null;

        // Check if we have hideTrayIconWhenDisconnected setting as 4th parameter
        var hideTrayIconWhenDisconnected = values.Count > 3 ? values[3] as bool? ?? false : false;

        // If controller is disconnected and we should hide tray icon, return default tooltip
        if (status == ConnectionStatus.Disconnected && hideTrayIconWhenDisconnected)
        {
            return loc["App_Name"];
        }

        string statusText = status switch
        {
            ConnectionStatus.Disconnected => loc["ConnectionStatus_Disconnected"],
            ConnectionStatus.Connected => loc["ConnectionStatus_Connected"],
            ConnectionStatus.Charging => loc["ConnectionStatus_Charging"],
            _ => loc["ConnectionStatus_Unknown"]
        };

        string levelText = level switch
        {
            BatteryLevel.Unknown => loc["BatteryLevel_Unknown"],
            BatteryLevel.Empty => loc["BatteryLevel_Empty"],
            BatteryLevel.Low => loc["BatteryLevel_Low"],
            BatteryLevel.Normal => loc["BatteryLevel_Normal"],
            BatteryLevel.High => loc["BatteryLevel_High"],
            BatteryLevel.Full => loc["BatteryLevel_Full"],
            _ => loc["BatteryLevel_Unknown"]
        };

        string controllerName = !string.IsNullOrWhiteSpace(modelName) ? modelName : loc["Controller_Unknown"];

        return status == ConnectionStatus.Disconnected
            ? loc["Controller_Disconnected"]
            : string.Format(loc["Controller_TooltipFormat"], controllerName, levelText, statusText);
    }
}