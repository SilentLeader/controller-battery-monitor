using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace ControllerMonitor.Converters
{
    /// <summary>
    /// Converts a battery capacity value to a boolean visibility state.
    /// Returns true if capacity is not null, false otherwise.
    /// </summary>
    public class CapacityToVisibilityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Return true if capacity has a value (not null), false otherwise
            return value is int;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("CapacityToVisibilityConverter does not support two-way binding.");
        }
    }
}