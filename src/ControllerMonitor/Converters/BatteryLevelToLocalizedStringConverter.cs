using System;
using System.Globalization;
using Avalonia.Data.Converters;
using ControllerMonitor.Services;
using ControllerMonitor.ValueObjects;

namespace ControllerMonitor.Converters;

public class BatteryLevelToLocalizedStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not BatteryLevel level)
            return value?.ToString() ?? string.Empty;

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

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
