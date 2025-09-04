using System.Threading.Tasks;
using XboxBatteryMonitor.Services;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;

namespace XboxBatteryMonitor.Services;

public class NotificationService : INotificationService
{
    private WindowNotificationManager? _notificationManager;

    public void Initialize(Window window)
    {
        _notificationManager = new WindowNotificationManager(window);
    }

    public Task ShowNotificationAsync(string title, string message)
    {
        _notificationManager?.Show(new Notification(title, message, NotificationType.Warning));
        return Task.CompletedTask;
    }
}