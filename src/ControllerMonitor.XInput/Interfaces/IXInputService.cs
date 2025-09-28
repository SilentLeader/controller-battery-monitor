using ControllerMonitor.XInput.Models;

namespace ControllerMonitor.XInput.Interfaces;

/// <summary>
/// Interface for XInput service operations
/// </summary>
public interface IXInputService
{
    /// <summary>
    /// Gets battery information for the first connected controller
    /// </summary>
    /// <returns>Controller battery info or null if no controller is connected</returns>
    Task<ControllerBatteryInfo?> GetFirstControllerBatteryInfoAsync();

    /// <summary>
    /// Gets battery information for a specific controller by index
    /// </summary>
    /// <param name="controllerIndex">Controller index (0-3)</param>
    /// <returns>Controller battery info or null if controller is not connected</returns>
    Task<ControllerBatteryInfo?> GetControllerBatteryInfoAsync(uint controllerIndex);

    /// <summary>
    /// Gets battery information for all connected controllers
    /// </summary>
    /// <returns>List of connected controllers with their battery info</returns>
    Task<IEnumerable<ControllerBatteryInfo>> GetAllControllersBatteryInfoAsync();
}