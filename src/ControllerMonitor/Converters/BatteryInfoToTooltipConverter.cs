using Avalonia.Data.Converters;
using ControllerMonitor.ValueObjects;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace ControllerMonitor.Converters
{
    public class BatteryInfoToTooltipConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count < 3) return "Controller Monitor";
    
            var level = values[0] as BatteryLevel? ?? BatteryLevel.Unknown;
            var isCharging = values[1] as bool? ?? false;
            var isConnected = values[2] as bool? ?? false;
            var modelName = values.Count > 3 ? values[3] as string : null;
    
            string status = isCharging ? "Charging" : "Not Charging";
            string connection = isConnected ? "Connected" : "Disconnected";
            string controllerName = !string.IsNullOrWhiteSpace(modelName) ? modelName : "Unknown Controller";
    
            return $"{controllerName} - Battery: {level} - {status} - {connection}";
        }
    }
}