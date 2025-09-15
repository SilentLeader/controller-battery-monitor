using Avalonia.Data.Converters;
using ControllerMonitor.ValueObjects;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;

namespace ControllerMonitor.Converters
{
    public class TrayIconVisibilityConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count < 2) return true;

            var status = values[0] as ConnectionStatus? ?? ConnectionStatus.Disconnected;
            var hideTrayIconWhenDisconnected = values[1] as bool? ?? false;

            // Workaround for Avalonia bug #19332 on Linux
            // Always show tray icon on Linux to avoid DBus disposal race condition
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return true;
            }

            // On Windows, honor the hide setting
            bool isConnected = status != ConnectionStatus.Disconnected;
            return isConnected || !hideTrayIconWhenDisconnected;
        }

        public object[]? ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}