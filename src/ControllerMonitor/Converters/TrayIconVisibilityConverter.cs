using Avalonia.Data.Converters;
using ControllerMonitor.ValueObjects;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace ControllerMonitor.Converters
{
    public class TrayIconVisibilityConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count < 2) return true;

            var status = values[0] as ConnectionStatus? ?? ConnectionStatus.Disconnected;
            var hideTrayIconWhenDisconnected = values[1] as bool? ?? false;

            // Show tray icon if connected/charging OR if we don't want to hide when disconnected
            bool isConnected = status != ConnectionStatus.Disconnected;
            return isConnected || !hideTrayIconWhenDisconnected;
        }
    }
}