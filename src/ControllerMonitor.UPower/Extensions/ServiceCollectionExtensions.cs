using ControllerMonitor.UPower.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ControllerMonitor.UPower.Extensions;

/// <summary>
/// Extension methods for configuring UPower services in dependency injection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds UPower services to the service collection with default configuration.
    /// </summary>
    public static IServiceCollection AddUPower(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register core services
        services.TryAddSingleton<UPowerPropertyConverter>();
        services.TryAddSingleton<UPowerClient>();
        
        // Register the main battery provider
        services.TryAddSingleton<UPowerBatteryProvider>();
        services.TryAddSingleton<IBatteryDataProvider>(provider => provider.GetRequiredService<UPowerBatteryProvider>());
        
        return services;
    }
    
}

