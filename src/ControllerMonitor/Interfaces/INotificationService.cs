using System.Threading.Tasks;
using Avalonia.Controls;

namespace ControllerMonitor.Interfaces;

public interface INotificationService
{
    void Initialize(Window window);
    Task ShowNotificationAsync(string title, string message, NotificationType type = NotificationType.Information);
    Task ShowSystemNotificationAsync(string title, string message, NotificationType type = NotificationType.Information);
}

public enum NotificationType
{
    Information,
    Warning,
    Error,
    Success
}
