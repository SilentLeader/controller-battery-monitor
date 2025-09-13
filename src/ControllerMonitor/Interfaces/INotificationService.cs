using System.Threading.Tasks;
using Avalonia.Controls;
using ControllerMonitor.ValueObjects;

namespace ControllerMonitor.Interfaces;

public interface INotificationService
{
    void Initialize(Window window);
    Task ShowNotificationAsync(string title, string message, NotificationPriority type = NotificationPriority.Normal);

    /// <summary>
    /// Show system notification
    /// </summary>
    /// <param name="title">Title</param>
    /// <param name="message">Message</param>
    /// <param name="priority">Priority</param>
    /// <param name="expirationTime">Expiration time in seconds</param>
    /// <returns></returns>
    Task ShowSystemNotificationAsync(string title, string message, NotificationPriority priority = NotificationPriority.Normal, int? expirationTime = null);
}
