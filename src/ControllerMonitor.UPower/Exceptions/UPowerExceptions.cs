namespace ControllerMonitor.UPower.Exceptions;

/// <summary>
/// Base exception for all UPower-related errors
/// </summary>
public abstract class UPowerException : Exception
{
    protected UPowerException(string message) : base(message) { }
    protected UPowerException(string message, Exception innerException) : base(message, innerException) { }
}

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

/// <summary>
/// Exception thrown when device enumeration fails
/// </summary>
public sealed class UPowerDeviceEnumerationException : UPowerException
{
    public int? DeviceCount { get; }
    
    public UPowerDeviceEnumerationException() 
        : base("Failed to enumerate UPower devices") { }
    
    public UPowerDeviceEnumerationException(string message) : base(message) { }
    
    public UPowerDeviceEnumerationException(string message, int deviceCount) : base(message)
    {
        DeviceCount = deviceCount;
    }
    
    public UPowerDeviceEnumerationException(string message, Exception innerException) 
        : base(message, innerException) { }
}

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

/// <summary>
/// Exception thrown when configuration validation fails
/// </summary>
public sealed class UPowerConfigurationException : UPowerException
{
    public string? ConfigurationSection { get; }
    public string[]? ValidationErrors { get; }
    
    public UPowerConfigurationException() 
        : base("UPower configuration validation failed") { }
    
    public UPowerConfigurationException(string message) : base(message) { }
    
    public UPowerConfigurationException(string message, string? configurationSection) : base(message)
    {
        ConfigurationSection = configurationSection;
    }
    
    public UPowerConfigurationException(string message, string[]? validationErrors) : base(message)
    {
        ValidationErrors = validationErrors;
    }
    
    public UPowerConfigurationException(string message, Exception innerException) 
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when operation times out
/// </summary>
public sealed class UPowerTimeoutException : UPowerException
{
    public TimeSpan Timeout { get; }
    public string? Operation { get; }
    
    public UPowerTimeoutException(TimeSpan timeout) 
        : base($"Operation timed out after {timeout.TotalMilliseconds}ms")
    {
        Timeout = timeout;
    }
    
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

/// <summary>
/// Exception thrown when event monitoring fails
/// </summary>
public sealed class UPowerEventMonitoringException : UPowerException
{
    public string? EventType { get; }
    
    public UPowerEventMonitoringException() 
        : base("Event monitoring failed") { }
    
    public UPowerEventMonitoringException(string message) : base(message) { }
    
    public UPowerEventMonitoringException(string message, string? eventType) : base(message)
    {
        EventType = eventType;
    }
    
    public UPowerEventMonitoringException(string message, Exception innerException) 
        : base(message, innerException) { }
}

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

/// <summary>
/// Utility methods for exception handling
/// </summary>
public static class UPowerExceptionHelpers
{
    /// <summary>
    /// Wraps an exception with appropriate UPower exception type
    /// </summary>
    public static UPowerException WrapException(Exception exception, string? operation = null)
    {
        return exception switch
        {
            UPowerException upowerEx => upowerEx,
            TimeoutException timeoutEx => new UPowerTimeoutException(
                operation ?? "Unknown", TimeSpan.FromMilliseconds(0), timeoutEx),
            OperationCanceledException cancelEx => new UPowerOperationCancelledException(
                operation ?? "Unknown operation was cancelled", cancelEx),
            UnauthorizedAccessException authEx => new UPowerPermissionException(
                "Access denied: " + authEx.Message, authEx),
            DllNotFoundException dllEx => new UPowerLibraryLoadException(
                "UPower library not found: " + dllEx.Message, dllEx),
            EntryPointNotFoundException entryEx => new UPowerInteropException(
                "Function not found in UPower library: " + entryEx.Message, entryEx),
            _ => new UPowerInteropException("Unexpected error: " + exception.Message, exception)
        };
    }
    
    /// <summary>
    /// Determines if an exception indicates UPower is unavailable
    /// </summary>
    public static bool IsUPowerUnavailable(Exception exception)
    {
        return exception is UPowerDaemonUnavailableException ||
               exception is UPowerLibraryLoadException ||
               exception is DllNotFoundException ||
               (exception is UPowerDBusException dbusEx && 
                dbusEx.Message.Contains("service not available", StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// Determines if an exception is recoverable
    /// </summary>
    public static bool IsRecoverableException(Exception exception)
    {
        return exception switch
        {
            UPowerTimeoutException => true,
            UPowerDeviceException => true,
            UPowerInteropException interopEx when !interopEx.Message.Contains("library", StringComparison.OrdinalIgnoreCase) => true,
            UPowerDBusException dbusEx when !dbusEx.Message.Contains("service not available", StringComparison.OrdinalIgnoreCase) => true,
            _ => false
        };
    }
    
    /// <summary>
    /// Gets user-friendly error message for an exception
    /// </summary>
    public static string GetUserFriendlyMessage(Exception exception)
    {
        return exception switch
        {
            UPowerDaemonUnavailableException => 
                "UPower service is not available. Please ensure UPower is installed and running.",
            UPowerLibraryLoadException => 
                "Could not load UPower library. Please ensure libupower-glib is installed.",
            UPowerPermissionException => 
                "Insufficient permissions to access battery information. You may need to run with elevated privileges or configure PolicyKit.",
            UPowerDBusException => 
                "Communication error with system services. Please check D-Bus is running.",
            UPowerTimeoutException => 
                "Battery information request timed out. The system may be under heavy load.",
            UPowerDeviceEnumerationException => 
                "Could not enumerate battery devices. Some devices may not be accessible.",
            _ => "An unexpected error occurred while accessing battery information."
        };
    }
}