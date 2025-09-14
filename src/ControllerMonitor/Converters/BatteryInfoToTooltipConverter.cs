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
            if (values.Count < 2) return "Controller Monitor";
    
            var level = values[0] as BatteryLevel? ?? BatteryLevel.Unknown;
            var status = values[1] as ConnectionStatus? ?? ConnectionStatus.Disconnected;
            var modelName = values.Count > 2 ? values[2] as string : null;
    
            string statusText = status switch
            {
                ConnectionStatus.Disconnected => "Disconnected",
                ConnectionStatus.Connected => "Connected",
                ConnectionStatus.Charging => "Charging",
                _ => "Unknown"
            };
            
            string controllerName = !string.IsNullOrWhiteSpace(modelName) ? modelName : "Unknown Controller";
    
            return $"{controllerName} - Battery: {level} - Status: {statusText}";
        }
    }
}