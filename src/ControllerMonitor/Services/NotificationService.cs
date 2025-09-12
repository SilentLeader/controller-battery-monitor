using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;

namespace ControllerMonitor.Services;

using ControllerMonitor.Interfaces;

public class NotificationService : INotificationService
{
    private WindowNotificationManager? _notificationManager;

    public void Initialize(Window window)
    {
        _notificationManager = new WindowNotificationManager(window);
    }

    public Task ShowNotificationAsync(string title, string message)
    {
        Dispatcher.UIThread.Post(() => _notificationManager?.Show(new Notification(title, message, NotificationType.Warning)));
        return Task.CompletedTask;
    }
}