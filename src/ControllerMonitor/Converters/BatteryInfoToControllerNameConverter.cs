using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using ControllerMonitor.Services;

namespace ControllerMonitor.Converters
{
    public class BatteryInfoToControllerNameConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count < 2 )
            {
                return string.Empty;
            }

            if(values[0] is not bool isConnected)
            {
                return string.Empty;
            }

            var modelName = values[1]?.ToString();

            if (isConnected != true)
                return LocalizationService.Instance["Controller_Unknown"];

            return !string.IsNullOrWhiteSpace(modelName)
                ? modelName
                : LocalizationService.Instance["Controller_Unknown"];
        }
    }
}