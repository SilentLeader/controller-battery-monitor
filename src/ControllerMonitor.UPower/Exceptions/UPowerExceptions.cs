namespace ControllerMonitor.UPower.Exceptions;

/// <summary>
/// Base exception for all UPower-related errors
/// </summary>
public abstract class UPowerException : Exception
{
    protected UPowerException(string message) : base(message) { }
    protected UPowerException(string message, Exception innerException) : base(message, innerException) { }
}
