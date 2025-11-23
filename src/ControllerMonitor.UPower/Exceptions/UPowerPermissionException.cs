namespace ControllerMonitor.UPower.Exceptions;

/// <summary>
/// Exception thrown when there are permission issues accessing UPower
/// </summary>
public sealed class UPowerPermissionException(string message, Exception innerException) : UPowerException(message, innerException)
{
}
