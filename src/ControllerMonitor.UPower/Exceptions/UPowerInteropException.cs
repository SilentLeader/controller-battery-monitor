namespace ControllerMonitor.UPower.Exceptions;

/// <summary>
/// Exception thrown when P/Invoke marshalling fails
/// </summary>
public sealed class UPowerInteropException(string message, Exception innerException) : UPowerException(message, innerException)
{
}
