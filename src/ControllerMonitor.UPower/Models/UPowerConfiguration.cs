using ControllerMonitor.UPower.ValueObjects;
using Microsoft.Extensions.Options;

namespace ControllerMonitor.UPower.Models;

/// <summary>
/// Configuration options for UPower integration
/// </summary>
public sealed class UPowerConfiguration
{
    public const string SectionName = "UPower";
    
    /// <summary>
    /// Default polling interval when event monitoring is unavailable
    /// </summary>
    public TimeSpan DefaultPollingInterval { get; set; } = TimeSpan.FromSeconds(5);
    
    /// <summary>
    /// Timeout for individual UPower operations
    /// </summary>
    public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(10);
    
    /// <summary>
    /// Maximum number of retry attempts for failed operations
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;
    
    /// <summary>
    /// Base delay for exponential backoff retry strategy
    /// </summary>
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromMilliseconds(500);
    
    /// <summary>
    /// Maximum delay for exponential backoff retry strategy
    /// </summary>
    public TimeSpan RetryMaxDelay { get; set; } = TimeSpan.FromSeconds(5);
    
    /// <summary>
    /// Number of consecutive failures before marking provider as unhealthy
    /// </summary>
    public int HealthCheckFailureThreshold { get; set; } = 3;
    
    /// <summary>
    /// Interval for health check recovery attempts
    /// </summary>
    public TimeSpan HealthCheckRecoveryInterval { get; set; } = TimeSpan.FromMinutes(2);
    
    /// <summary>
    /// Whether to enable event-driven monitoring
    /// </summary>
    public bool EnableEventMonitoring { get; set; } = true;
    
    /// <summary>
    /// Debouncing interval for rapid events
    /// </summary>
    public TimeSpan EventDebounceInterval { get; set; } = TimeSpan.FromMilliseconds(200);
    
    /// <summary>
    /// Maximum number of events to queue before dropping oldest events
    /// </summary>
    public int MaxEventQueueSize { get; set; } = 100;
    
    /// <summary>
    /// Whether to prefer UPower over sysfs for battery monitoring
    /// </summary>
    public bool PreferUPowerOverSysfs { get; set; } = false;
    
    /// <summary>
    /// Filter for device types to monitor
    /// </summary>
    public DeviceTypeFilter DeviceFilter { get; set; } = new();
    
    /// <summary>
    /// Library loading configuration
    /// </summary>
    public LibraryConfiguration Library { get; set; } = new();
    
    /// <summary>
    /// Advanced configuration options
    /// </summary>
    public AdvancedConfiguration Advanced { get; set; } = new();
}

/// <summary>
/// Configuration for filtering device types
/// </summary>
public sealed class DeviceTypeFilter
{
    /// <summary>
    /// Whether to monitor gaming input devices (controllers)
    /// </summary>
    public bool IncludeGamingDevices { get; set; } = true;
    
    /// <summary>
    /// Whether to monitor mouse devices
    /// </summary>
    public bool IncludeMouseDevices { get; set; } = true;
    
    /// <summary>
    /// Whether to monitor keyboard devices
    /// </summary>
    public bool IncludeKeyboardDevices { get; set; } = true;
    
    /// <summary>
    /// Whether to monitor headset devices
    /// </summary>
    public bool IncludeHeadsetDevices { get; set; } = true;
    
    /// <summary>
    /// Whether to monitor tablet/stylus devices
    /// </summary>
    public bool IncludeTabletDevices { get; set; } = false;
    
    /// <summary>
    /// Whether to monitor phone devices
    /// </summary>
    public bool IncludePhoneDevices { get; set; } = false;
    
    /// <summary>
    /// Whether to monitor laptop batteries
    /// </summary>
    public bool IncludeLaptopBatteries { get; set; } = false;
    
    /// <summary>
    /// Custom device types to include
    /// </summary>
    public HashSet<DeviceType> CustomIncludedTypes { get; set; } = new();
    
    /// <summary>
    /// Device types to explicitly exclude
    /// </summary>
    public HashSet<DeviceType> ExcludedTypes { get; set; } = new();
    
    /// <summary>
    /// Vendor/model name patterns to include (case-insensitive)
    /// </summary>
    public List<string> IncludePatterns { get; set; } = new();
    
    /// <summary>
    /// Vendor/model name patterns to exclude (case-insensitive)
    /// </summary>
    public List<string> ExcludePatterns { get; set; } = new();
    
    /// <summary>
    /// Checks if a device should be included based on the filter settings
    /// </summary>
    public bool ShouldIncludeDevice(BatteryDevice device)
    {
        // Check explicit exclusions first
        if (ExcludedTypes.Contains(device.Type))
            return false;
            
        // Check exclude patterns
        var displayName = device.DisplayName.ToLowerInvariant();
        if (ExcludePatterns.Any(pattern => displayName.Contains(pattern.ToLowerInvariant())))
            return false;
        
        // Check include patterns
        if (IncludePatterns.Any() && 
            IncludePatterns.Any(pattern => displayName.Contains(pattern.ToLowerInvariant())))
            return true;
        
        // Check custom included types
        if (CustomIncludedTypes.Contains(device.Type))
            return true;
        
        // Check standard device types
        return device.Type switch
        {
            DeviceType.GamingInput => IncludeGamingDevices,
            DeviceType.Mouse => IncludeMouseDevices,
            DeviceType.Keyboard => IncludeKeyboardDevices,
            DeviceType.Headset => IncludeHeadsetDevices,
            DeviceType.Headphones => IncludeHeadsetDevices,
            DeviceType.Tablet => IncludeTabletDevices,
            DeviceType.Pen => IncludeTabletDevices,
            DeviceType.Phone => IncludePhoneDevices,
            DeviceType.Battery => IncludeLaptopBatteries,
            _ => false
        };
    }
}

/// <summary>
/// Configuration for native library loading
/// </summary>
public sealed class LibraryConfiguration
{
    /// <summary>
    /// Custom library paths to try in order
    /// </summary>
    public List<string> CustomLibraryPaths { get; set; } = new();
    
    /// <summary>
    /// Whether to allow fallback to system library search
    /// </summary>
    public bool AllowSystemLibrarySearch { get; set; } = true;
    
    /// <summary>
    /// Timeout for library loading attempts
    /// </summary>
    public TimeSpan LibraryLoadTimeout { get; set; } = TimeSpan.FromSeconds(5);
    
    /// <summary>
    /// Whether to verify library functionality after loading
    /// </summary>
    public bool VerifyLibraryAfterLoad { get; set; } = true;
    
    /// <summary>
    /// Minimum required UPower version (if available)
    /// </summary>
    public Version? MinimumUPowerVersion { get; set; }
}

/// <summary>
/// Advanced configuration options
/// </summary>
public sealed class AdvancedConfiguration
{
    /// <summary>
    /// Maximum number of concurrent device queries
    /// </summary>
    public int MaxConcurrentDeviceQueries { get; set; } = 3;
    
    /// <summary>
    /// Whether to cache device properties between queries
    /// </summary>
    public bool EnablePropertyCaching { get; set; } = true;
    
    /// <summary>
    /// Cache expiry time for device properties
    /// </summary>
    public TimeSpan PropertyCacheExpiry { get; set; } = TimeSpan.FromSeconds(1);
    
    /// <summary>
    /// Whether to enable detailed performance metrics
    /// </summary>
    public bool EnablePerformanceMetrics { get; set; } = false;
    
    /// <summary>
    /// Whether to enable debug logging for native calls
    /// </summary>
    public bool EnableNativeCallLogging { get; set; } = false;
    
    /// <summary>
    /// GC collection interval for managing native resources
    /// </summary>
    public TimeSpan GCCollectionInterval { get; set; } = TimeSpan.FromMinutes(5);
    
    /// <summary>
    /// Whether to validate device data consistency
    /// </summary>
    public bool EnableDataValidation { get; set; } = true;
    
    /// <summary>
    /// Custom signal names to monitor (advanced users only)
    /// </summary>
    public List<string> CustomSignalNames { get; set; } = new();
}

/// <summary>
/// Configuration validator for UPowerConfiguration
/// </summary>
public sealed class UPowerConfigurationValidator : IValidateOptions<UPowerConfiguration>
{
    public ValidateOptionsResult Validate(string? name, UPowerConfiguration options)
    {
        var failures = new List<string>();
        
        if (options.DefaultPollingInterval < TimeSpan.FromMilliseconds(100))
            failures.Add("DefaultPollingInterval must be at least 100ms");
            
        if (options.DefaultPollingInterval > TimeSpan.FromMinutes(10))
            failures.Add("DefaultPollingInterval must not exceed 10 minutes");
        
        if (options.OperationTimeout < TimeSpan.FromMilliseconds(500))
            failures.Add("OperationTimeout must be at least 500ms");
            
        if (options.MaxRetryAttempts < 0 || options.MaxRetryAttempts > 10)
            failures.Add("MaxRetryAttempts must be between 0 and 10");
        
        if (options.HealthCheckFailureThreshold < 1)
            failures.Add("HealthCheckFailureThreshold must be at least 1");
            
        if (options.EventDebounceInterval < TimeSpan.Zero)
            failures.Add("EventDebounceInterval cannot be negative");
            
        if (options.MaxEventQueueSize < 1)
            failures.Add("MaxEventQueueSize must be at least 1");
        
        if (options.Advanced.MaxConcurrentDeviceQueries < 1 || 
            options.Advanced.MaxConcurrentDeviceQueries > 20)
            failures.Add("MaxConcurrentDeviceQueries must be between 1 and 20");
            
        if (options.Advanced.PropertyCacheExpiry < TimeSpan.Zero)
            failures.Add("PropertyCacheExpiry cannot be negative");
        
        return failures.Any() 
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}