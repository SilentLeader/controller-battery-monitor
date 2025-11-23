namespace ControllerMonitor.UPower.Native;

/// <summary>
/// Exception thrown when a SafeHandle operation fails
/// </summary>
public class SafeHandleException : Exception
{
    public SafeHandleException(string message) : base(message) { }
}
