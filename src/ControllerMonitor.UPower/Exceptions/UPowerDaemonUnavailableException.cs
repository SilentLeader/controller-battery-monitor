namespace ControllerMonitor.UPower.Exceptions;

/// <summary>
/// Exception thrown when UPower daemon is not available or not running
/// </summary>
public sealed class UPowerDaemonUnavailableException : UPowerException
{
    public UPowerDaemonUnavailableException() 
        : base("UPower daemon is not available or not running") { }
    
    public UPowerDaemonUnavailableException(string message) : base(message) { }
    
    public UPowerDaemonUnavailableException(string message, Exception innerException) 
        : base(message, innerException) { }
}
