using Avalonia.Data.Converters;
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

            var isConnected = values[0] as bool? ?? true;
            var hideTrayIconWhenDisconnected = values[1] as bool? ?? false;

            // Show tray icon if connected OR if we don't want to hide when disconnected
            return isConnected || !hideTrayIconWhenDisconnected;
        }
    }
}