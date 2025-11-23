namespace ControllerMonitor.UPower.Exceptions;

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
            UPowerTimeoutException =>
                "Battery information request timed out. The system may be under heavy load.",
            _ => "Unknown error"
        };
    }
}