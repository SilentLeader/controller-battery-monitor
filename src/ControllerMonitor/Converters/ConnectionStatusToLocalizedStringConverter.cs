using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using ControllerMonitor.Services;
using ControllerMonitor.ValueObjects;

namespace ControllerMonitor.Converters;

public class ConnectionStatusToLocalizedStringConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values[0] is not ConnectionStatus status)
            return values[0]?.ToString() ?? string.Empty;

        var loc = LocalizationService.Instance;

        return status switch
        {
            ConnectionStatus.Disconnected => loc["ConnectionStatus_Disconnected"],
            ConnectionStatus.Connected => loc["ConnectionStatus_Connected"],
            ConnectionStatus.Charging => loc["ConnectionStatus_Charging"],
            _ => loc["ConnectionStatus_Unknown"]
        };
    }
}
