using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using ControllerMonitor.Services;
using ControllerMonitor.ViewModels;

namespace ControllerMonitor.Converters
{
    public class BatteryInfoToControllerNameConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values[0] is not BatteryInfoViewModel batteryInfo)
                return values[0]?.ToString() ?? string.Empty;

            if (batteryInfo?.IsConnected != true)
                return LocalizationService.Instance["Controller_Unknown"];

            return !string.IsNullOrWhiteSpace(batteryInfo.ModelName)
                ? batteryInfo.ModelName
                : LocalizationService.Instance["Controller_Unknown"];
        }
    }
}