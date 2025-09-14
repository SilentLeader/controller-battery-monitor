using System;
using System.IO;
using System.Threading.Tasks;
using ControllerMonitor.ViewModels;
using ControllerMonitor.Services;
using ControllerMonitor.Interfaces;
using Microsoft.Extensions.Logging;

#if LINUX
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using BatteryLevel = ControllerMonitor.UPower.ValueObjects.BatteryLevel;
using ControllerMonitor.UPower.Services;
using ControllerMonitor.UPower.Models;
using ControllerMonitor.UPower.ValueObjects;
using ControllerMonitor.UPower.Exceptions;
using UPowerBatteryState = ControllerMonitor.UPower.ValueObjects.BatteryState;
#endif
using ControllerBatteryLevel = ControllerMonitor.ValueObjects.BatteryLevel;

namespace ControllerMonitor.Platforms.Linux;

public class BatteryMonitorLinux : BatteryMonitorServiceBase
{
    private readonly IServiceProvider? _serviceProvider;
    
#if LINUX
    private UPowerBatteryProvider? _upowerProvider;
    private bool _upowerInitialized;
    private bool _upowerAvailable;
#endif

    public BatteryMonitorLinux(ISettingsService settingsService, ILogger<IBatteryMonitorService> logger)
        : base(settingsService, logger)
    {
    }
    
    public BatteryMonitorLinux(ISettingsService settingsService, ILogger<IBatteryMonitorService> logger, IServiceProvider serviceProvider)
        : base(settingsService, logger)
    {
        _serviceProvider = serviceProvider;
    }
    private const string PowerSupplyPath = "/sys/class/power_supply/";
    private const string PowerSupplyModelName = "POWER_SUPPLY_MODEL_NAME=";
    private const string PowerSupplyCapacity = "POWER_SUPPLY_CAPACITY=";
    private const string PowerSupplyCapacityLevel = "POWER_SUPPLY_CAPACITY_LEVEL=";
    private const string PowerSupplyStatus = "POWER_SUPPLY_STATUS=";

    public override async Task<BatteryInfoViewModel> GetBatteryInfoAsync()
    {
        try
        {
            // Dual-tier battery detection: UPower first, then sysfs fallback
            
            // Tier 1: Try UPower (primary detection mechanism)
#if LINUX
            var upowerResult = await TryGetBatteryFromUPowerAsync();
            if (upowerResult != null)
            {
                _logger.LogDebug("Battery info retrieved via UPower: {Model} - {Percentage}%",
                    upowerResult.ModelName, upowerResult.Capacity);
                return upowerResult;
            }
            
            _logger.LogDebug("UPower detection failed, falling back to sysfs");
#endif
            
            // Tier 2: Fallback to sysfs (existing implementation)
            var sysfsResult = await GetBatteryFromSysfsAsync();
            if (sysfsResult != null)
            {
                _logger.LogDebug("Battery info retrieved via sysfs: {Model} - {Percentage}%",
                    sysfsResult.ModelName, sysfsResult.Capacity);
                return sysfsResult;
            }
            
            _logger.LogDebug("No battery devices detected via UPower or sysfs");
            return new BatteryInfoViewModel { IsConnected = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get battery info failed");
            
            // Try fallback to sysfs if primary method fails
            try
            {
                var fallbackResult = await GetBatteryFromSysfsAsync();
                if (fallbackResult != null)
                {
                    _logger.LogInformation("Fallback to sysfs successful after primary method failed");
                    return fallbackResult;
                }
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "Fallback to sysfs also failed");
            }
            
            return new BatteryInfoViewModel { IsConnected = false };
        }
    }

#if LINUX
    /// <summary>
    /// Attempts to get battery information using UPower (primary detection mechanism)
    /// </summary>
    private async Task<BatteryInfoViewModel?> TryGetBatteryFromUPowerAsync()
    {
        try
        {
            // Initialize UPower provider if not already done
            if (!_upowerInitialized)
            {
                await InitializeUPowerAsync();
            }
            
            if (!_upowerAvailable || _upowerProvider == null)
            {
                return null;
            }
            
            // Get all battery devices from UPower
            var devices = await _upowerProvider.GetBatteryDevicesAsync();
            
            // Prioritize gaming controllers
            var gamingDevice = devices.FirstOrDefault(d => d.IsGamingController);
            if (gamingDevice != null)
            {
                return ConvertUPowerToViewModel(gamingDevice);
            }
            
            // Fallback to any battery device (for broader compatibility)
            var anyBatteryDevice = devices.FirstOrDefault(d =>
                d.Type == DeviceType.Battery ||
                d.Type == DeviceType.Mouse ||
                d.Type == DeviceType.Keyboard ||
                d.Type == DeviceType.Headset);
                
            if (anyBatteryDevice != null)
            {
                return ConvertUPowerToViewModel(anyBatteryDevice);
            }
            
            return null;
        }
        catch (UPowerException ex)
        {
            _logger.LogWarning(ex, "UPower battery detection failed: {Message}",
                UPowerExceptionHelpers.GetUserFriendlyMessage(ex));
            
            // Mark UPower as unavailable for this session
            _upowerAvailable = false;
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Unexpected error in UPower battery detection");
            return null;
        }
    }
    
    /// <summary>
    /// Initializes the UPower provider lazily
    /// </summary>
    private async Task InitializeUPowerAsync()
    {
        if (_upowerInitialized)
            return;
            
        try
        {
            _upowerProvider = _serviceProvider?.GetService<UPowerBatteryProvider>();
            
            if (_upowerProvider != null)
            {
                _upowerAvailable = await _upowerProvider.IsAvailableAsync();
                
                if (_upowerAvailable)
                {   
                    _logger.LogInformation("UPower integration initialized successfully");
                }
                else
                {
                    _logger.LogInformation("UPower is not available on this system, using sysfs fallback");
                }
            }
            else
            {
                _logger.LogError("UPower provider not registered in DI container");
                _upowerAvailable = false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize UPower integration, falling back to sysfs");
            _upowerAvailable = false;
        }
        finally
        {
            _upowerInitialized = true;
        }
    }
        
    /// <summary>
    /// Converts UPower BatteryDevice to BatteryInfoViewModel
    /// </summary>
    private static BatteryInfoViewModel ConvertUPowerToViewModel(BatteryDevice device)
    {
        return new BatteryInfoViewModel
        {
            Level = ConvertUPowerBatteryLevelToBatteryLevel(device.BatteryLevelCurrent) ?? ConvertUPowerPercentageToBatteryLevel(device.Percentage),
            Capacity = (int)Math.Round(device.Percentage),
            IsCharging = device.State == UPowerBatteryState.Charging,
            IsConnected = true,
            ModelName = device.DisplayName
        };
    }
    
    /// <summary>
    /// Converts UPower percentage to ControllerMonitor BatteryLevel
    /// </summary>
    private static ControllerBatteryLevel ConvertUPowerPercentageToBatteryLevel(double percentage)
    {
        return percentage switch
        {
            >= 90 => ControllerBatteryLevel.Full,
            >= 60 => ControllerBatteryLevel.High,
            >= 30 => ControllerBatteryLevel.Normal,
            >= 10 => ControllerBatteryLevel.Low,
            > 0 => ControllerBatteryLevel.Low,
            0 => ControllerBatteryLevel.Empty,
            _ => ControllerBatteryLevel.Unknown
        };
    }

    /// <summary>
    /// Converts UPower battery level to ControllerMonitor BatteryLevel
    /// </summary>
    private static ControllerBatteryLevel? ConvertUPowerBatteryLevelToBatteryLevel(BatteryLevel? batteryLevel)
    {
        return batteryLevel switch
        {
            BatteryLevel.Full => ControllerBatteryLevel.Full,
            BatteryLevel.High => ControllerBatteryLevel.High,
            BatteryLevel.Normal => ControllerBatteryLevel.Normal,
            BatteryLevel.Low => ControllerBatteryLevel.Low,
            BatteryLevel.Unknown => ControllerBatteryLevel.Unknown,
            _ => null
        };
    }
#endif

    /// <summary>
    /// Gets battery information using sysfs (fallback mechanism)
    /// </summary>
    private async Task<BatteryInfoViewModel?> GetBatteryFromSysfsAsync()
    {
        try
        {
            var deviceInfo = FindXboxBatteryDevice();
            if (deviceInfo == null)
            {
                return null;
            }

            var (level, isCharging, capacity) = await ReadBatteryInfoAsync(deviceInfo.Value.devicePath);
            return new BatteryInfoViewModel
            {
                Level = level,
                Capacity = capacity,
                IsCharging = isCharging,
                IsConnected = true,
                ModelName = deviceInfo.Value.modelName
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Sysfs battery detection failed");
            return null;
        }
    }

    private static (string devicePath, string modelName)? FindXboxBatteryDevice()
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
                        return (devicePath, model);
                    }
                }
            }
        }
        return null;
    }

    private static async Task<(ControllerBatteryLevel level, bool isCharging, int? capacity)> ReadBatteryInfoAsync(string devicePath)
    {
        var ueventPath = Path.Combine(devicePath, "uevent");
        var lines = await File.ReadAllLinesAsync(ueventPath);
        int? capacity = null;
        var level = ControllerBatteryLevel.Unknown;
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

    private static ControllerBatteryLevel ConvertBatteryLevel(string levelRaw)
    {
        return levelRaw.ToLower() switch
        {
            "full" => ControllerBatteryLevel.Full,
            "high" => ControllerBatteryLevel.High,
            "normal" => ControllerBatteryLevel.Normal,
            "low" => ControllerBatteryLevel.Low,
            "empty" => ControllerBatteryLevel.Empty,
            _ => ControllerBatteryLevel.Unknown
        };
    }

    private static ControllerBatteryLevel ConvertCapacityToBatteryLevel(int? capacity)
    {
        return capacity switch
        {
            >= 90 => ControllerBatteryLevel.Full,
            >= 60 => ControllerBatteryLevel.High,
            >= 30 => ControllerBatteryLevel.Normal,
            >= 10 => ControllerBatteryLevel.Low,
            > 0 => ControllerBatteryLevel.Low,
            0 => ControllerBatteryLevel.Empty,
            _ => ControllerBatteryLevel.Unknown
        };
    }
}
