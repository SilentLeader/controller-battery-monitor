using System;
using System.IO;
using System.Threading.Tasks;
using XboxBatteryMonitor.ViewModels;
using XboxBatteryMonitor.Services;
using XboxBatteryMonitor.ValueObjects;
using Microsoft.Extensions.Logging;

namespace XboxBatteryMonitor.Platforms.Linux;

public class BatteryMonitorLinux(ISettingsService settingsService, ILogger<IBatteryMonitorService> logger) : BatteryMonitorServiceBase(settingsService, logger)
{
    private const string PowerSupplyPath = "/sys/class/power_supply/";
    private const string PowerSupplyModelName = "POWER_SUPPLY_MODEL_NAME=";
    private const string PowerSupplyCapacity = "POWER_SUPPLY_CAPACITY=";
    private const string PowerSupplyCapacityLevel = "POWER_SUPPLY_CAPACITY_LEVEL=";
    private const string PowerSupplyStatus = "POWER_SUPPLY_STATUS=";

    public override async Task<BatteryInfoViewModel> GetBatteryInfoAsync()
    {
        try
        {
            var devicePath = FindXboxBatteryDevice();
            if (string.IsNullOrEmpty(devicePath))
            {
                return new BatteryInfoViewModel { IsConnected = false };
            }

            var (level, isCharging, capacity) = await ReadBatteryInfoAsync(devicePath);
            return new BatteryInfoViewModel
            {
                Level = level,
                Capacity = capacity,
                IsCharging = isCharging,
                IsConnected = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get battery info failed");
            throw;
        }
    }

    private static string? FindXboxBatteryDevice()
    {
        if (!Directory.Exists(PowerSupplyPath))
            return null;

        var entries = Directory.GetDirectories(PowerSupplyPath);
        foreach (var devicePath in entries)
        {
            var typePath = Path.Combine(devicePath, "type");
            if (!File.Exists(typePath))
                continue;

            var deviceType = File.ReadAllText(typePath).Trim();
            if (deviceType != "Battery")
                continue;

            var ueventPath = Path.Combine(devicePath, "uevent");
            if (!File.Exists(ueventPath))
                continue;

            var lines = File.ReadAllLines(ueventPath);
            foreach (var line in lines)
            {
                if (line.StartsWith(PowerSupplyModelName))
                {
                    var model = line.Substring(PowerSupplyModelName.Length);
                    if (model.Contains("controller", StringComparison.CurrentCultureIgnoreCase))
                    {
                        return devicePath;
                    }
                }
            }
        }
        return null;
    }

    private static async Task<(BatteryLevel level, bool isCharging, int? capacity)> ReadBatteryInfoAsync(string devicePath)
    {
        var ueventPath = Path.Combine(devicePath, "uevent");
        var lines = await File.ReadAllLinesAsync(ueventPath);
        int? capacity = null;
        var level = BatteryLevel.Unknown;
        bool isCharging = false;
        foreach (var line in lines)
        {
            if (line.StartsWith(PowerSupplyCapacity))
            {
                var capacityRaw = line[PowerSupplyCapacity.Length..];
                if (int.TryParse(capacityRaw, out int convertedCapacity))
                {
                    capacity = convertedCapacity;
                    level = ConvertCapacityToBatteryLevel(capacity);
                }
            }

            if (line.StartsWith(PowerSupplyCapacityLevel))
            {
                level = ConvertBatteryLevel(line[PowerSupplyCapacityLevel.Length..]);
            }

            if (line.StartsWith(PowerSupplyStatus))
            {
                var status = line[PowerSupplyStatus.Length..].ToLower();
                isCharging = status == "charging";
            }
        }
        return (level, isCharging, capacity);
    }

    private static BatteryLevel ConvertBatteryLevel(string levelRaw)
    {
        return levelRaw.ToLower() switch
        {
            "full" => BatteryLevel.Full,
            "high" => BatteryLevel.High,
            "normal" => BatteryLevel.Normal,
            "low" => BatteryLevel.Low,
            "empty" => BatteryLevel.Empty,
            _ => BatteryLevel.Unknown
        };
    }

    private static BatteryLevel ConvertCapacityToBatteryLevel(int? capacity)
    {
        return capacity switch
        {
            >= 90 => BatteryLevel.Full,
            >= 60 => BatteryLevel.High,
            >= 30 => BatteryLevel.Normal,
            >= 10 => BatteryLevel.Low,
            > 0 => BatteryLevel.Low,
            0 => BatteryLevel.Empty,
            _ => BatteryLevel.Unknown
        };
    }
}
