namespace ControllerMonitor.UPower.Exceptions;

/// <summary>
/// Exception thrown when device property access fails
/// </summary>
public sealed class UPowerDeviceException(string message, string? devicePath, Exception innerException) : UPowerException(message, innerException)
{
    public string? DevicePath { get; } = devicePath;
}
