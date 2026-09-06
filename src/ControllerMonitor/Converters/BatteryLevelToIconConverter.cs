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

namespace ControllerMonitor.Converters;

public class BatteryLevelToIconConverter : IMultiValueConverter
{
    private const int IconDecodeWidth = 64;

    private static readonly ConcurrentDictionary<Uri, WindowIcon> IconCache = new();
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 3) return null;

        var level = values[0] as BatteryLevel? ?? BatteryLevel.Unknown;
        var status = values[1] as ConnectionStatus? ?? ConnectionStatus.Disconnected;
        var themeVariant = values[2] as ThemeVariant ?? Application.Current?.ActualThemeVariant;
        
        // Check if we have hideTrayIconWhenDisconnected setting as 4th parameter
        var hideTrayIconWhenDisconnected = values.Count > 3 && (values[3] as bool? ?? false);
        
        // If controller is disconnected and we should hide tray icon, return null
        if (status == ConnectionStatus.Disconnected && hideTrayIconWhenDisconnected)
        {
            return null;
        }

        var iconName = GetIconName(level, status);
        var theme = themeVariant == ThemeVariant.Dark ? "dark" : "light";
        var uri = new Uri($"avares://ControllerMonitor/Assets/icons/{theme}/{iconName}.png");
        
        return GetOrCreateWindowIcon(uri, theme);
    }

    private static WindowIcon GetOrCreateWindowIcon(Uri uri, string theme)
    {
        return IconCache.GetOrAdd(uri, (iconUri) =>
        {
            try
            {
                using var stream = AssetLoader.Open(iconUri);
                using var bitmap = Bitmap.DecodeToWidth(stream, IconDecodeWidth, BitmapInterpolationMode.HighQuality);
                return new WindowIcon(bitmap);
            }
            catch
            {
                // Fallback to a default icon if loading fails
                var fallbackUri = new Uri($"avares://ControllerMonitor/Assets/icons/{theme}/battery_unknown.png");

                // Try to get fallback from cache first, or create it
                return IconCache.GetOrAdd(fallbackUri, (fallbackIconUri) =>
                {
                    try
                    {
                        using var fallbackStream = AssetLoader.Open(fallbackIconUri);
                        using var fallbackBitmap = Bitmap.DecodeToWidth(fallbackStream, IconDecodeWidth, BitmapInterpolationMode.HighQuality);
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