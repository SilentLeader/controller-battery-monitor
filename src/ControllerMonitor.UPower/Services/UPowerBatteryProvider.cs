using ControllerMonitor.UPower.Models;
using Microsoft.Extensions.Logging;

namespace ControllerMonitor.UPower.Services;

/// <summary>
/// UPower implementation of IBatteryDataProvider
/// </summary>
public sealed partial class UPowerBatteryProvider(
    ILogger<UPowerBatteryProvider> logger,
    UPowerClient client) : IBatteryDataProvider
{
    private readonly ILogger<UPowerBatteryProvider> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly UPowerClient _client = client ?? throw new ArgumentNullException(nameof(client));
    
    
    /// <summary>
    /// Checks if UPower is available on this system
    /// </summary>
    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.InitializeAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "UPower availability check failed");
            return false;
        }
    }
    
    /// <summary>
    /// Gets all battery devices from UPower
    /// </summary>
    public async Task<IReadOnlyList<BatteryDevice>> GetBatteryDevicesAsync(CancellationToken cancellationToken = default)
    {
        var devices = await _client.GetDevicesAsync(cancellationToken);

        LogRetrievedDevices(_logger, devices.Count);

        return devices;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Retrieved {Count} battery devices from UPower")]
    private static partial void LogRetrievedDevices(ILogger logger, int count);
}

