using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using ControllerMonitor.ValueObjects;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace ControllerMonitor.Converters
{
    public class BatteryLevelToIconConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count < 3) return null;

            var level = values[0] as BatteryLevel? ?? BatteryLevel.Unknown;
            var isCharging = values[1] as bool? ?? false;
            var isConnected = values[2] as bool? ?? false;
            var themeVariant = values[3] as ThemeVariant ?? Application.Current?.ActualThemeVariant;

            var iconName = GetIconName(level, isCharging, isConnected);
            var theme = themeVariant == ThemeVariant.Dark ? "dark" : "light";
            var uri = new Uri($"avares://ControllerMonitor/Assets/icons/{theme}/{iconName}.png");
            
            try
            {
                using var stream = AssetLoader.Open(uri);
                var bitmap = new Bitmap(stream);
                return new WindowIcon(bitmap);
            }
            catch
            {
                // Fallback to a default icon if loading fails
                var fallbackUri = new Uri($"avares://ControllerMonitor/Assets/icons/{theme}/battery_unknown.png");
                using var fallbackStream = AssetLoader.Open(fallbackUri);
                var fallbackBitmap = new Bitmap(fallbackStream);
                return new WindowIcon(fallbackBitmap);
            }
        }

        private static string GetIconName(BatteryLevel level, bool isCharging, bool isConnected)
        {
            if (!isConnected)
            {
                return "battery_disconnected";
            }

            if (isCharging)
            {
                return "battery_charging";
            }

            return level switch
            {
                BatteryLevel.Full => "battery_full",
                BatteryLevel.High => "battery_high",
                BatteryLevel.Normal => "battery_normal",
                BatteryLevel.Low => "battery_low",
                BatteryLevel.Empty => "battery_empty",
                _ => "battery_unknown"
            };
        }
    }
}