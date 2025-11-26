using System;
using System.Globalization;
using Avalonia.Data.Converters;
using ControllerMonitor.Services;
using ControllerMonitor.ValueObjects;

namespace ControllerMonitor.Converters;

public class ConnectionStatusToLocalizedStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ConnectionStatus status)
            return value?.ToString() ?? string.Empty;

        var loc = LocalizationService.Instance;

        return status switch
        {
            ConnectionStatus.Disconnected => loc["ConnectionStatus_Disconnected"],
            ConnectionStatus.Connected => loc["ConnectionStatus_Connected"],
            ConnectionStatus.Charging => loc["ConnectionStatus_Charging"],
            _ => loc["ConnectionStatus_Unknown"]
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
