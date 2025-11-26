using System;
using System.Globalization;
using Avalonia.Data.Converters;
using ControllerMonitor.Services;

namespace ControllerMonitor.Converters;

public class LocalizeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is string key)
        {
            return LocalizationService.Instance[key];
        }
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
