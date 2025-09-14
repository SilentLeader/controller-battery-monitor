using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using ControllerMonitor.ValueObjects;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;

namespace ControllerMonitor.Converters
{
    public class BatteryLevelToIconConverter : IMultiValueConverter
    {
        private static readonly ConcurrentDictionary<Uri, WindowIcon> _iconCache = new();
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count < 3) return null;

            var level = values[0] as BatteryLevel? ?? BatteryLevel.Unknown;
            var status = values[1] as ConnectionStatus? ?? ConnectionStatus.Disconnected;
            var themeVariant = values[2] as ThemeVariant ?? Application.Current?.ActualThemeVariant;

            var iconName = GetIconName(level, status);
            var theme = themeVariant == ThemeVariant.Dark ? "dark" : "light";
            var uri = new Uri($"avares://ControllerMonitor/Assets/icons/{theme}/{iconName}.png");
            
            return GetOrCreateWindowIcon(uri, theme);
        }

        private static WindowIcon GetOrCreateWindowIcon(Uri uri, string theme)
        {
            return _iconCache.GetOrAdd(uri, (iconUri) =>
            {
                try
                {
                    using var stream = AssetLoader.Open(iconUri);
                    var bitmap = new Bitmap(stream);
                    return new WindowIcon(bitmap);
                }
                catch
                {
                    // Fallback to a default icon if loading fails
                    var fallbackUri = new Uri($"avares://ControllerMonitor/Assets/icons/{theme}/battery_unknown.png");
                    
                    // Try to get fallback from cache first, or create it
                    return _iconCache.GetOrAdd(fallbackUri, (fallbackIconUri) =>
                    {
                        try
                        {
                            using var fallbackStream = AssetLoader.Open(fallbackIconUri);
                            var fallbackBitmap = new Bitmap(fallbackStream);
                            return new WindowIcon(fallbackBitmap);
                        }
                        catch
                        {
                            // If even fallback fails, return null
                            return null!;
                        }
                    });
                }
            });
        }

        private static string GetIconName(BatteryLevel level, ConnectionStatus status)
        {
            return status switch
            {
                ConnectionStatus.Disconnected => "battery_disconnected",
                ConnectionStatus.Charging => "battery_charging",
                ConnectionStatus.Connected => level switch
                {
                    BatteryLevel.Full => "battery_full",
                    BatteryLevel.High => "battery_high",
                    BatteryLevel.Normal => "battery_normal",
                    BatteryLevel.Low => "battery_low",
                    BatteryLevel.Empty => "battery_empty",
                    _ => "battery_unknown"
                },
                _ => "battery_unknown"
            };
        }
    }
}