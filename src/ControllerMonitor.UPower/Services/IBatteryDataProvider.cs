using ControllerMonitor.UPower.Models;

namespace ControllerMonitor.UPower.Services;

/// <summary>
/// Interface for battery data providers with health monitoring and retry logic
/// </summary>
public interface IBatteryDataProvider
{
    /// <summary>
    /// Checks if the provider is available on this system
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all battery devices from this provider
    /// </summary>
    Task<IReadOnlyList<BatteryDevice>> GetBatteryDevicesAsync(CancellationToken cancellationToken = default);
}

