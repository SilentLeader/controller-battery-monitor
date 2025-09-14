using ControllerMonitor.UPower.Models;
using ControllerMonitor.UPower.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ControllerMonitor.UPower.Extensions;

/// <summary>
/// Extension methods for configuring UPower services in dependency injection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds UPower services to the service collection with default configuration
    /// </summary>
    public static IServiceCollection AddUPower(this IServiceCollection services)
    {
        return services.AddUPower(_ => { });
    }
    
    /// <summary>
    /// Adds UPower services to the service collection with custom configuration
    /// </summary>
    public static IServiceCollection AddUPower(this IServiceCollection services, Action<UPowerConfiguration> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);
        
        // Configure options
        services.Configure(configureOptions);
        services.AddSingleton<IValidateOptions<UPowerConfiguration>, UPowerConfigurationValidator>();
        
        return services.AddUPowerCore();
    }
    
    /// <summary>
    /// Adds UPower services to the service collection with configuration binding
    /// </summary>
    public static IServiceCollection AddUPower(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        
        // Bind configuration
        services.Configure<UPowerConfiguration>(configuration.GetSection(UPowerConfiguration.SectionName));
        services.AddSingleton<IValidateOptions<UPowerConfiguration>, UPowerConfigurationValidator>();
        
        return services.AddUPowerCore();
    }
    
    /// <summary>
    /// Adds UPower services to the service collection with both configuration binding and custom setup
    /// </summary>
    public static IServiceCollection AddUPower(this IServiceCollection services, IConfiguration configuration, Action<UPowerConfiguration> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(configureOptions);
        
        // Bind configuration first, then apply custom configuration
        services.Configure<UPowerConfiguration>(configuration.GetSection(UPowerConfiguration.SectionName));
        services.Configure(configureOptions);
        services.AddSingleton<IValidateOptions<UPowerConfiguration>, UPowerConfigurationValidator>();
        
        return services.AddUPowerCore();
    }
    
    /// <summary>
    /// Core UPower service registration
    /// </summary>
    private static IServiceCollection AddUPowerCore(this IServiceCollection services)
    {
        // Ensure logging is available
        services.TryAddSingleton<ILoggerFactory, LoggerFactory>();
        
        // Register core services
        services.TryAddSingleton<UPowerPropertyConverter>();
        services.TryAddSingleton<ProviderHealthMonitor>();
        services.TryAddSingleton<UPowerClient>();
        services.TryAddSingleton<UPowerEventMonitor>();
        
        // Register the main battery provider
        services.TryAddSingleton<UPowerBatteryProvider>();
        services.TryAddSingleton<IBatteryDataProvider>(provider => provider.GetRequiredService<UPowerBatteryProvider>());
        
        // Register as a hosted service for automatic startup/shutdown
        services.TryAddSingleton<UPowerHostedService>();
        services.AddHostedService(provider => provider.GetRequiredService<UPowerHostedService>());
        
        return services;
    }
    
    /// <summary>
    /// Configures UPower for gaming device monitoring (controllers, mice, etc.)
    /// </summary>
    public static IServiceCollection ConfigureUPowerForGaming(this IServiceCollection services)
    {
        return services.Configure<UPowerConfiguration>(config =>
        {
            config.DeviceFilter.IncludeGamingDevices = true;
            config.DeviceFilter.IncludeMouseDevices = true;
            config.DeviceFilter.IncludeKeyboardDevices = true;
            config.DeviceFilter.IncludeHeadsetDevices = true;
            config.DeviceFilter.IncludeTabletDevices = false;
            config.DeviceFilter.IncludePhoneDevices = false;
            config.DeviceFilter.IncludeLaptopBatteries = false;
            
            config.EnableEventMonitoring = true;
            config.EventDebounceInterval = TimeSpan.FromMilliseconds(100);
            config.DefaultPollingInterval = TimeSpan.FromSeconds(2);
        });
    }
    
    /// <summary>
    /// Configures UPower for low-latency monitoring (for real-time applications)
    /// </summary>
    public static IServiceCollection ConfigureUPowerForLowLatency(this IServiceCollection services)
    {
        return services.Configure<UPowerConfiguration>(config =>
        {
            config.EnableEventMonitoring = true;
            config.EventDebounceInterval = TimeSpan.FromMilliseconds(50);
            config.DefaultPollingInterval = TimeSpan.FromSeconds(1);
            config.OperationTimeout = TimeSpan.FromSeconds(5);
            config.Advanced.EnablePropertyCaching = true;
            config.Advanced.PropertyCacheExpiry = TimeSpan.FromMilliseconds(500);
        });
    }
    
    /// <summary>
    /// Configures UPower for resource-constrained environments
    /// </summary>
    public static IServiceCollection ConfigureUPowerForLowResource(this IServiceCollection services)
    {
        return services.Configure<UPowerConfiguration>(config =>
        {
            config.EnableEventMonitoring = false; // Reduce resource usage
            config.DefaultPollingInterval = TimeSpan.FromSeconds(10);
            config.OperationTimeout = TimeSpan.FromSeconds(30);
            config.Advanced.MaxConcurrentDeviceQueries = 1;
            config.Advanced.EnablePropertyCaching = true;
            config.Advanced.PropertyCacheExpiry = TimeSpan.FromSeconds(5);
            config.Advanced.GCCollectionInterval = TimeSpan.FromMinutes(10);
        });
    }
    
    /// <summary>
    /// Adds debugging and diagnostic features
    /// </summary>
    public static IServiceCollection AddUPowerDiagnostics(this IServiceCollection services)
    {
        return services.Configure<UPowerConfiguration>(config =>
        {
            config.Advanced.EnablePerformanceMetrics = true;
            config.Advanced.EnableNativeCallLogging = true;
            config.Advanced.EnableDataValidation = true;
        });
    }
    
    /// <summary>
    /// Validates the UPower configuration and logs any issues
    /// </summary>
    public static IServiceCollection ValidateUPowerConfiguration(this IServiceCollection services)
    {
        return services.AddOptions<UPowerConfiguration>()
            .ValidateOnStart()
            .Services;
    }
}

/// <summary>
/// Hosted service for managing UPower lifecycle
/// </summary>
internal sealed class UPowerHostedService : IHostedService
{
    private readonly ILogger<UPowerHostedService> _logger;
    private readonly UPowerBatteryProvider _batteryProvider;
    private readonly IOptionsMonitor<UPowerConfiguration> _configurationMonitor;
    
    public UPowerHostedService(
        ILogger<UPowerHostedService> logger,
        UPowerBatteryProvider batteryProvider,
        IOptionsMonitor<UPowerConfiguration> configurationMonitor)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _batteryProvider = batteryProvider ?? throw new ArgumentNullException(nameof(batteryProvider));
        _configurationMonitor = configurationMonitor ?? throw new ArgumentNullException(nameof(configurationMonitor));
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting UPower hosted service");
            
            // Check if UPower is available
            if (!await _batteryProvider.IsAvailableAsync(cancellationToken))
            {
                _logger.LogWarning("UPower is not available on this system");
                return;
            }
            
            // Start monitoring
            await _batteryProvider.StartMonitoringAsync(cancellationToken);
            
            _logger.LogInformation("UPower hosted service started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start UPower hosted service");
            // Don't rethrow - allow the application to continue without UPower
        }
    }
    
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Stopping UPower hosted service");
            
            await _batteryProvider.StopMonitoringAsync(cancellationToken);
            
            _logger.LogInformation("UPower hosted service stopped");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error stopping UPower hosted service");
        }
    }
}

/// <summary>
/// Builder extensions for fluent configuration
/// </summary>
public static class UPowerConfigurationBuilder
{
    /// <summary>
    /// Creates a configuration builder for UPower
    /// </summary>
    public static UPowerConfigurationFluentBuilder CreateBuilder()
    {
        return new UPowerConfigurationFluentBuilder();
    }
}

/// <summary>
/// Fluent builder for UPower configuration
/// </summary>
public sealed class UPowerConfigurationFluentBuilder
{
    private readonly UPowerConfiguration _configuration = new();
    
    /// <summary>
    /// Sets the default polling interval
    /// </summary>
    public UPowerConfigurationFluentBuilder WithPollingInterval(TimeSpan interval)
    {
        _configuration.DefaultPollingInterval = interval;
        return this;
    }
    
    /// <summary>
    /// Sets the operation timeout
    /// </summary>
    public UPowerConfigurationFluentBuilder WithOperationTimeout(TimeSpan timeout)
    {
        _configuration.OperationTimeout = timeout;
        return this;
    }
    
    /// <summary>
    /// Enables or disables event monitoring
    /// </summary>
    public UPowerConfigurationFluentBuilder WithEventMonitoring(bool enabled = true)
    {
        _configuration.EnableEventMonitoring = enabled;
        return this;
    }
    
    /// <summary>
    /// Sets the event debounce interval
    /// </summary>
    public UPowerConfigurationFluentBuilder WithEventDebouncing(TimeSpan interval)
    {
        _configuration.EventDebounceInterval = interval;
        return this;
    }
    
    /// <summary>
    /// Configures device filtering
    /// </summary>
    public UPowerConfigurationFluentBuilder WithDeviceFilter(Action<DeviceTypeFilter> configure)
    {
        configure(_configuration.DeviceFilter);
        return this;
    }
    
    /// <summary>
    /// Configures retry behavior
    /// </summary>
    public UPowerConfigurationFluentBuilder WithRetryPolicy(int maxAttempts, TimeSpan baseDelay, TimeSpan maxDelay)
    {
        _configuration.MaxRetryAttempts = maxAttempts;
        _configuration.RetryBaseDelay = baseDelay;
        _configuration.RetryMaxDelay = maxDelay;
        return this;
    }
    
    /// <summary>
    /// Configures health checking
    /// </summary>
    public UPowerConfigurationFluentBuilder WithHealthCheck(int failureThreshold, TimeSpan recoveryInterval)
    {
        _configuration.HealthCheckFailureThreshold = failureThreshold;
        _configuration.HealthCheckRecoveryInterval = recoveryInterval;
        return this;
    }
    
    /// <summary>
    /// Enables performance monitoring
    /// </summary>
    public UPowerConfigurationFluentBuilder WithPerformanceMonitoring(bool enabled = true)
    {
        _configuration.Advanced.EnablePerformanceMetrics = enabled;
        return this;
    }
    
    /// <summary>
    /// Enables data validation
    /// </summary>
    public UPowerConfigurationFluentBuilder WithDataValidation(bool enabled = true)
    {
        _configuration.Advanced.EnableDataValidation = enabled;
        return this;
    }
    
    /// <summary>
    /// Configures property caching
    /// </summary>
    public UPowerConfigurationFluentBuilder WithPropertyCaching(bool enabled, TimeSpan? expiry = null)
    {
        _configuration.Advanced.EnablePropertyCaching = enabled;
        if (expiry.HasValue)
        {
            _configuration.Advanced.PropertyCacheExpiry = expiry.Value;
        }
        return this;
    }
    
    /// <summary>
    /// Builds the configuration
    /// </summary>
    public UPowerConfiguration Build()
    {
        return _configuration;
    }
    
    /// <summary>
    /// Applies the configuration to a service collection
    /// </summary>
    public IServiceCollection ApplyTo(IServiceCollection services)
    {
        return services.AddUPower(_configuration => { });
    }
}