namespace ControllerMonitor.UPower.Exceptions;

/// <summary>
/// Exception thrown when P/Invoke marshalling fails
/// </summary>
public sealed class UPowerInteropException : UPowerException
{
    public string? FunctionName { get; }
    public object[]? Parameters { get; }
    
    public UPowerInteropException() 
        : base("P/Invoke interop operation failed") { }
    
    public UPowerInteropException(string message) : base(message) { }
    
    public UPowerInteropException(string message, string? functionName) : base(message)
    {
        FunctionName = functionName;
    }
    
    public UPowerInteropException(string message, string? functionName, object[]? parameters) 
        : base(message)
    {
        FunctionName = functionName;
        Parameters = parameters;
    }
    
    public UPowerInteropException(string message, Exception innerException) 
        : base(message, innerException) { }
    
    public UPowerInteropException(string message, string? functionName, Exception innerException) 
        : base(message, innerException)
    {
        FunctionName = functionName;
    }
}
