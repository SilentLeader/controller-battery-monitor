using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using ControllerMonitor.Interfaces;
using AvaloniaNotificationType = Avalonia.Controls.Notifications.NotificationType;

namespace ControllerMonitor.Services;

public abstract class NotificationServiceBase : INotificationService
{
    private WindowNotificationManager? _notificationManager;

    public virtual void Initialize(Window window)
    {
        _notificationManager = new WindowNotificationManager(window);
    }

    public virtual Task ShowNotificationAsync(string title, string message, ValueObjects.NotificationType type = ValueObjects.NotificationType.Normal)
    {
        var avaloniaType = type switch
        {
            ValueObjects.NotificationType.High => AvaloniaNotificationType.Warning,
            ValueObjects.NotificationType.Low => AvaloniaNotificationType.Success,
            _ => AvaloniaNotificationType.Information
        };

        Dispatcher.UIThread.Post(() => _notificationManager?.Show(new Notification(title, message, avaloniaType)));
        return Task.CompletedTask;
    }

    public abstract Task ShowSystemNotificationAsync(string title, string message, ValueObjects.NotificationType type = ValueObjects.NotificationType.Normal, DateTimeOffset? expirationTime = null);
}