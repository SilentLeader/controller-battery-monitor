#if WINDOWS
using ControllerMonitor.Interfaces;
using ControllerMonitor.Services;
using ControllerMonitor.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Threading.Tasks;
using Windows.UI.Notifications;

namespace ControllerMonitor.Platforms.Windows;

public class NotificationServiceWindows(ILogger<INotificationService> logger) : NotificationServiceBase
{
    public override async Task ShowSystemNotificationAsync(string title, string message, NotificationPriority type = NotificationPriority.Normal, int? expirationTime = null)
    {
        try
        {
            new ToastContentBuilder()
                .AddText(title)
                .AddText(message)
                .Show(toast => 
                {
                    toast.ExpirationTime = DateTime.Now.AddSeconds(expirationTime ?? 10);
                    toast.Priority = type == NotificationPriority.High ? ToastNotificationPriority.High : ToastNotificationPriority.Default;
                });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "System notification failed");
            // Fallback to in-app notification if toast fails
            await ShowNotificationAsync(title, message, type);
        }
    }
}
#endif