using ControllerMonitor.UPower.Models;

namespace ControllerMonitor.UPower.Services;

/// <summary>
/// Interface for battery data providers with health monitoring and retry logic
/// </summary>
public interface IBatteryDataProvider
{
    /// <summary>
    /// Unique name of this provider
    /// </summary>
    string ProviderName { get; }
    
    /// <summary>
    /// Priority of this provider (lower values = higher priority)
    /// </summary>
    int Priority { get; }
    
    /// <summary>
    /// Whether this provider is currently healthy and available
    /// </summary>
    bool IsHealthy { get; }
    
    /// <summary>
    /// Checks if the provider is available on this system
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all battery devices from this provider
    /// </summary>
    Task<IReadOnlyList<BatteryDevice>> GetBatteryDevicesAsync(CancellationToken cancellationToken = default);
}

