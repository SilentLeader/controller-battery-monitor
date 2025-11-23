namespace ControllerMonitor.UPower.Exceptions;

/// <summary>
/// Exception thrown when UPower daemon is not available or not running
/// </summary>
public sealed class UPowerDaemonUnavailableException(string message) : UPowerException(message)
{
}
