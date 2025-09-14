namespace ControllerMonitor.UPower.Exceptions;

/// <summary>
/// Exception thrown when operation is cancelled
/// </summary>
public sealed class UPowerOperationCancelledException : UPowerException
{
    public string? Operation { get; }
    
    public UPowerOperationCancelledException() 
        : base("Operation was cancelled") { }
    
    public UPowerOperationCancelledException(string message) : base(message) { }
    
    public UPowerOperationCancelledException(string operation, CancellationToken cancellationToken) 
        : base($"Operation '{operation}' was cancelled")
    {
        Operation = operation;
    }
    
    public UPowerOperationCancelledException(string message, Exception innerException) 
        : base(message, innerException) { }
}
