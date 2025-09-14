namespace ControllerMonitor.UPower.Exceptions;

/// <summary>
/// Exception thrown when there are D-Bus connectivity issues
/// </summary>
public sealed class UPowerDBusException : UPowerException
{
    public string? DBusError { get; }
    
    public UPowerDBusException() 
        : base("D-Bus connectivity error occurred") { }
    
    public UPowerDBusException(string message) : base(message) { }
    
    public UPowerDBusException(string message, string? dbusError) : base(message) 
    {
        DBusError = dbusError;
    }
    
    public UPowerDBusException(string message, Exception innerException) 
        : base(message, innerException) { }
    
    public UPowerDBusException(string message, string? dbusError, Exception innerException) 
        : base(message, innerException) 
    {
        DBusError = dbusError;
    }
}
