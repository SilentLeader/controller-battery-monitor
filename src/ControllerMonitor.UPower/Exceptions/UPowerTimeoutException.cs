namespace ControllerMonitor.UPower.Exceptions;

/// <summary>
/// Exception thrown when operation times out
/// </summary>
public sealed class UPowerTimeoutException : UPowerException
{
    public TimeSpan Timeout { get; }
    public string? Operation { get; }
    
    public UPowerTimeoutException(string operation, TimeSpan timeout) 
        : base($"Operation '{operation}' timed out after {timeout.TotalMilliseconds}ms")
    {
        Operation = operation;
        Timeout = timeout;
    }
    
    public UPowerTimeoutException(string message, TimeSpan timeout, Exception innerException) 
        : base(message, innerException)
    {
        Timeout = timeout;
    }
}
