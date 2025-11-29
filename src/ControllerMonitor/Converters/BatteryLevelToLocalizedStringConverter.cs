using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using ControllerMonitor.Services;
using ControllerMonitor.ValueObjects;

namespace ControllerMonitor.Converters;

public class BatteryLevelToLocalizedStringConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values[0] is not BatteryLevel level)
            return values[0]?.ToString() ?? string.Empty;

        var loc = LocalizationService.Instance;

        return level switch
        {
            BatteryLevel.Unknown => loc["BatteryLevel_Unknown"],
            BatteryLevel.Empty => loc["BatteryLevel_Empty"],
            BatteryLevel.Low => loc["BatteryLevel_Low"],
            BatteryLevel.Normal => loc["BatteryLevel_Normal"],
            BatteryLevel.High => loc["BatteryLevel_High"],
            BatteryLevel.Full => loc["BatteryLevel_Full"],
            _ => loc["BatteryLevel_Unknown"]
        };
    }
}
