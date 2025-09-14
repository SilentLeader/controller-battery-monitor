namespace ControllerMonitor.UPower.Exceptions;

/// <summary>
/// Exception thrown when native library loading fails
/// </summary>
public sealed class UPowerLibraryLoadException : UPowerException
{
    public string? LibraryPath { get; }
    public string[]? AttemptedPaths { get; }
    
    public UPowerLibraryLoadException() 
        : base("Failed to load UPower native library") { }
    
    public UPowerLibraryLoadException(string message) : base(message) { }
    
    public UPowerLibraryLoadException(string message, string? libraryPath) : base(message)
    {
        LibraryPath = libraryPath;
    }
    
    public UPowerLibraryLoadException(string message, string[]? attemptedPaths) : base(message)
    {
        AttemptedPaths = attemptedPaths;
    }
    
    public UPowerLibraryLoadException(string message, Exception innerException) 
        : base(message, innerException) { }
}
