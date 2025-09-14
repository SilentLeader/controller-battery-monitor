namespace ControllerMonitor.UPower.Exceptions;

/// <summary>
/// Exception thrown when there are permission issues accessing UPower
/// </summary>
public sealed class UPowerPermissionException : UPowerException
{
    public string? RequiredPermission { get; }
    
    public UPowerPermissionException() 
        : base("Insufficient permissions to access UPower") { }
    
    public UPowerPermissionException(string message) : base(message) { }
    
    public UPowerPermissionException(string message, string? requiredPermission) : base(message)
    {
        RequiredPermission = requiredPermission;
    }
    
    public UPowerPermissionException(string message, Exception innerException) 
        : base(message, innerException) { }
}
