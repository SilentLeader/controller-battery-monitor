using ControllerMonitor.UPower.Exceptions;
using ControllerMonitor.UPower.Models;
using ControllerMonitor.UPower.Native;
using ControllerMonitor.UPower.ValueObjects;
using Microsoft.Extensions.Logging;

namespace ControllerMonitor.UPower.Services;

/// <summary>
/// Service for converting UPower device properties from native GVariant types
/// </summary>
public sealed class UPowerPropertyConverter
{
    private readonly ILogger<UPowerPropertyConverter> _logger;
    
    public UPowerPropertyConverter(ILogger<UPowerPropertyConverter> logger)
    {
        _logger = logger;
    }
    
    /// <summary>
    /// Extracts all battery device properties from a UPower device handle
    /// </summary>
    public BatteryDevice ExtractBatteryDevice(SafeUPowerDeviceHandle deviceHandle, string objectPath)
    {
        try
        {
            return deviceHandle.UseHandle(devicePtr =>
            {
                var device = new BatteryDevice
                {
                    ObjectPath = objectPath,
                    NativePath = GetStringProperty(devicePtr, "native-path") ?? "",
                    Vendor = GetStringProperty(devicePtr, "vendor") ?? "",
                    Model = GetStringProperty(devicePtr, "model") ?? "",
                    Serial = GetStringProperty(devicePtr, "serial") ?? "",
                    Type = GetDeviceType(devicePtr),
                    PowerSupply = GetBooleanProperty(devicePtr, "power-supply"),
                    HasHistory = GetBooleanProperty(devicePtr, "has-history"),
                    HasStatistics = GetBooleanProperty(devicePtr, "has-statistics"),
                    Online = GetBooleanProperty(devicePtr, "online"),
                    Energy = GetDoubleProperty(devicePtr, "energy"),
                    EnergyEmpty = GetDoubleProperty(devicePtr, "energy-empty"),
                    EnergyFull = GetDoubleProperty(devicePtr, "energy-full"),
                    EnergyFullDesign = GetDoubleProperty(devicePtr, "energy-full-design"),
                    EnergyRate = GetDoubleProperty(devicePtr, "energy-rate"),
                    Voltage = GetDoubleProperty(devicePtr, "voltage"),
                    Percentage = GetDoubleProperty(devicePtr, "percentage"),
                    State = GetBatteryState(devicePtr),
                    Technology = GetBatteryTechnology(devicePtr),
                    WarningLevel = GetWarningLevel(devicePtr),
                    BatteryLevelCurrent = GetBatteryLevel(devicePtr),
                    TimeToEmpty = GetInt64Property(devicePtr, "time-to-empty"),
                    TimeToFull = GetInt64Property(devicePtr, "time-to-full"),
                    Capacity = GetDoubleProperty(devicePtr, "capacity"),
                    Temperature = GetDoubleProperty(devicePtr, "temperature"),
                    IsPresent = GetBooleanProperty(devicePtr, "is-present"),
                    IsRechargeable = GetBooleanProperty(devicePtr, "is-rechargeable"),
                    IconName = GetStringProperty(devicePtr, "icon-name") ?? "",
                    UpdateTime = DateTimeOffset.FromUnixTimeSeconds((long)GetUInt64Property(devicePtr, "update-time"))
                };
                
                _logger.LogDebug("Extracted device properties for {ObjectPath}: {Model} ({Type}) - {Percentage}%", 
                    objectPath, device.Model, device.Type, device.Percentage);
                
                return device;
            });
        }
        catch (Exception ex)
        {
            throw new UPowerDeviceException($"Failed to extract properties for device {objectPath}", objectPath, ex);
        }
    }
    
    /// <summary>
    /// Gets a string property from the device
    /// </summary>
    private string? GetStringProperty(IntPtr devicePtr, string propertyName)
    {
        try
        {
            return UPowerNative.GetObjectStringProperty(devicePtr, propertyName);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get string property {PropertyName}", propertyName);
            return null;
        }
    }
    
    /// <summary>
    /// Gets a double property from the device
    /// </summary>
    private double GetDoubleProperty(IntPtr devicePtr, string propertyName)
    {
        try
        {
            double value = UPowerNative.GetObjectDoubleProperty(devicePtr, propertyName);
            return double.IsNaN(value) || double.IsInfinity(value) ? 0.0 : value;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get double property {PropertyName}", propertyName);
            return 0.0;
        }
    }
    
    /// <summary>
    /// Gets an integer property from the device
    /// </summary>
    private int GetInt32Property(IntPtr devicePtr, string propertyName)
    {
        try
        {
            return UPowerNative.GetObjectIntProperty(devicePtr, propertyName);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get int32 property {PropertyName}", propertyName);
            return 0;
        }
    }
    
    /// <summary>
    /// Gets a 64-bit integer property from the device
    /// </summary>
    private long GetInt64Property(IntPtr devicePtr, string propertyName)
    {
        try
        {
            return UPowerNative.GetObjectInt64Property(devicePtr, propertyName);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get int64 property {PropertyName}", propertyName);
            return 0L;
        }
    }
    
    /// <summary>
    /// Gets a 32-bit unsigned integer property from the device
    /// </summary>
    private uint GetUInt32Property(IntPtr devicePtr, string propertyName)
    {
        try
        {
            return UPowerNative.GetObjectUIntProperty(devicePtr, propertyName);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get uint32 property {PropertyName}", propertyName);
            return 0;
        }
    }
    
    /// <summary>
    /// Gets a 64-bit unsigned integer property from the device
    /// </summary>
    private ulong GetUInt64Property(IntPtr devicePtr, string propertyName)
    {
        try
        {
            return UPowerNative.GetObjectUInt64Property(devicePtr, propertyName);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get uint64 property {PropertyName}", propertyName);
            return 0UL;
        }
    }
    
    /// <summary>
    /// Gets a boolean property from the device
    /// </summary>
    private bool GetBooleanProperty(IntPtr devicePtr, string propertyName)
    {
        try
        {
            return UPowerNative.GetObjectBooleanProperty(devicePtr, propertyName);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get boolean property {PropertyName}", propertyName);
            return false;
        }
    }
    
    /// <summary>
    /// Gets the device type and converts it to the managed enum
    /// </summary>
    private DeviceType GetDeviceType(IntPtr devicePtr)
    {
        try
        {
            var typeValue = GetUInt32Property(devicePtr, "kind");
            if (Enum.IsDefined(typeof(DeviceType), (int)typeValue))
            {
                return (DeviceType)typeValue;
            }
            
            _logger.LogDebug("Unknown device type value: {TypeValue}", typeValue);
            return DeviceType.Unknown;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get device type");
            return DeviceType.Unknown;
        }
    }
    
    /// <summary>
    /// Gets the battery state and converts it to the managed enum
    /// </summary>
    private BatteryState GetBatteryState(IntPtr devicePtr)
    {
        try
        {
            var stateValue = GetUInt32Property(devicePtr, "state");
            if (Enum.IsDefined(typeof(BatteryState), (int)stateValue))
            {
                return (BatteryState)stateValue;
            }
            
            _logger.LogDebug("Unknown battery state value: {StateValue}", stateValue);
            return BatteryState.Unknown;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get battery state");
            return BatteryState.Unknown;
        }
    }
    
    /// <summary>
    /// Gets the battery technology and converts it to the managed enum
    /// </summary>
    private BatteryTechnology GetBatteryTechnology(IntPtr devicePtr)
    {
        try
        {
            var technologyValue = GetUInt32Property(devicePtr, "technology");
            if (Enum.IsDefined(typeof(BatteryTechnology), (int)technologyValue))
            {
                return (BatteryTechnology)technologyValue;
            }
            
            _logger.LogDebug("Unknown battery technology value: {TechnologyValue}", technologyValue);
            return BatteryTechnology.Unknown;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get battery technology");
            return BatteryTechnology.Unknown;
        }
    }
    
    /// <summary>
    /// Gets the battery level and converts it to the managed enum
    /// </summary>
    private BatteryLevel? GetBatteryLevel(IntPtr devicePtr)
    {
        try
        {
            var levelValue = GetUInt32Property(devicePtr, "battery-level");
            if (Enum.IsDefined(typeof(BatteryLevel), (int)levelValue))
            {
                return (BatteryLevel)levelValue;
            }
            
            _logger.LogDebug("Unknown battery level value: {LevelValue}", levelValue);
            return BatteryLevel.Unknown;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get battery level");
            return null;
        }
    }
    
    /// <summary>
    /// Gets the battery warning level and converts it to the managed enum
    /// </summary>
    private BatteryLevel GetWarningLevel(IntPtr devicePtr)
    {
        try
        {
            var levelValue = GetUInt32Property(devicePtr, "warning-level");
            if (Enum.IsDefined(typeof(BatteryLevel), (int)levelValue))
            {
                return (BatteryLevel)levelValue;
            }
            
            _logger.LogDebug("Unknown warning level value: {LevelValue}", levelValue);
            return BatteryLevel.Unknown;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get warning level");
            return BatteryLevel.Unknown;
        }
    }
    
    /// <summary>
    /// Validates device properties for consistency and completeness
    /// </summary>
    public bool ValidateDeviceProperties(BatteryDevice device)
    {
        var issues = new List<string>();

        // Validate percentage
        if (device.Percentage < 0 || device.Percentage > 100)
        {
            issues.Add($"Invalid percentage: {device.Percentage}");
        }

        // Validate energy values
        if (device.Energy < 0 || device.EnergyFull < 0 || device.EnergyEmpty < 0)
        {
            issues.Add("Negative energy values detected");
        }

        if (device.EnergyFull > 0 && device.Energy > device.EnergyFull)
        {
            issues.Add($"Energy ({device.Energy}) exceeds EnergyFull ({device.EnergyFull})");
        }

        // Validate voltage
        if (device.Voltage < 0 || device.Voltage > 50) // Reasonable voltage range
        {
            issues.Add($"Suspicious voltage value: {device.Voltage}V");
        }

        // Validate temperature
        if (device.Temperature < -40 || device.Temperature > 100) // Reasonable temperature range
        {
            issues.Add($"Suspicious temperature value: {device.Temperature}°C");
        }

        // Validate time values
        if (device.TimeToEmpty < 0 || device.TimeToFull < 0)
        {
            issues.Add("Negative time values detected");
        }

        // Validate state consistency
        if (device.State == BatteryState.Charging && device.TimeToFull == 0)
        {
            issues.Add("Charging state but no time-to-full estimate");
        }
        
        if (issues.Any())
        {
            _logger.LogWarning("Device validation issues for {ObjectPath}: {Issues}",
                device.ObjectPath, string.Join(", ", issues));

            // Only fail validation for critical issues
            return !issues.Any(issue => issue.Contains("Invalid percentage") ||
                                       issue.Contains("Negative energy"));
        }

        return true;
    }
    
    /// <summary>
    /// Sanitizes device properties to ensure reasonable values
    /// </summary>
    public BatteryDevice SanitizeDeviceProperties(BatteryDevice device)
    {
        return device with
        {
            Percentage = Math.Clamp(device.Percentage, 0, 100),
            Energy = Math.Max(0, device.Energy),
            EnergyFull = Math.Max(0, device.EnergyFull),
            EnergyEmpty = Math.Max(0, device.EnergyEmpty),
            EnergyRate = Math.Max(0, device.EnergyRate),
            Voltage = Math.Max(0, device.Voltage),
            Capacity = Math.Clamp(device.Capacity, 0, 100),
            TimeToEmpty = Math.Max(0, device.TimeToEmpty),
            TimeToFull = Math.Max(0, device.TimeToFull),
            Temperature = Math.Clamp(device.Temperature, -40, 100)
        };
    }
}