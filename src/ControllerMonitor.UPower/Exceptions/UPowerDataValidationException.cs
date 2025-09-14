namespace ControllerMonitor.UPower.Exceptions;

/// <summary>
/// Exception thrown when data validation fails
/// </summary>
public sealed class UPowerDataValidationException : UPowerException
{
    public string? PropertyName { get; }
    public object? InvalidValue { get; }
    
    public UPowerDataValidationException() 
        : base("Data validation failed") { }
    
    public UPowerDataValidationException(string message) : base(message) { }
    
    public UPowerDataValidationException(string message, string? propertyName) : base(message)
    {
        PropertyName = propertyName;
    }
    
    public UPowerDataValidationException(string message, string? propertyName, object? invalidValue) 
        : base(message)
    {
        PropertyName = propertyName;
        InvalidValue = invalidValue;
    }
    
    public UPowerDataValidationException(string message, Exception innerException) 
        : base(message, innerException) { }
}
