namespace ControllerMonitor.UPower.Exceptions;

/// <summary>
/// Exception thrown when native library loading fails
/// </summary>
public sealed class UPowerLibraryLoadException : UPowerException
{
    public UPowerLibraryLoadException(string message) : base(message) { }
    
    public UPowerLibraryLoadException(string message, Exception innerException) 
        : base(message, innerException) { }
}
