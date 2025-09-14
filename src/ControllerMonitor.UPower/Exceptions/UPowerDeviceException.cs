namespace ControllerMonitor.UPower.Exceptions;

/// <summary>
/// Exception thrown when device property access fails
/// </summary>
public sealed class UPowerDeviceException : UPowerException
{
    public string? DevicePath { get; }
    public string? PropertyName { get; }
    
    public UPowerDeviceException() 
        : base("Device operation failed") { }
    
    public UPowerDeviceException(string message) : base(message) { }
    
    public UPowerDeviceException(string message, string? devicePath) : base(message)
    {
        DevicePath = devicePath;
    }
    
    public UPowerDeviceException(string message, string? devicePath, string? propertyName) 
        : base(message)
    {
        DevicePath = devicePath;
        PropertyName = propertyName;
    }
    
    public UPowerDeviceException(string message, Exception innerException) 
        : base(message, innerException) { }
    
    public UPowerDeviceException(string message, string? devicePath, Exception innerException) 
        : base(message, innerException)
    {
        DevicePath = devicePath;
    }
}
