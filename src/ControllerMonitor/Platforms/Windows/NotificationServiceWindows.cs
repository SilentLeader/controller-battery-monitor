#if WINDOWS
using System;
using System.Threading.Tasks;
using ControllerMonitor.Services;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace ControllerMonitor.Platforms.Windows;

public class NotificationServiceWindows : NotificationServiceBase
{
    private const string APP_ID = "ControllerMonitor";

    public override async Task ShowSystemNotificationAsync(string title, string message, Interfaces.NotificationType type = Interfaces.NotificationType.Information)
    {
        try
        {
            // Create toast notification using Windows Runtime API
            var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
            
            var stringElements = toastXml.GetElementsByTagName("text");
            stringElements[0].AppendChild(toastXml.CreateTextNode(title));
            stringElements[1].AppendChild(toastXml.CreateTextNode(message));

            // Configure audio based on notification type
            var audioElement = toastXml.CreateElement("audio");
            var audioSrc = type switch
            {
                Interfaces.NotificationType.Error => "ms-winsoundevent:Notification.Looping.Alarm",
                Interfaces.NotificationType.Warning => "ms-winsoundevent:Notification.Default",
                Interfaces.NotificationType.Success => "ms-winsoundevent:Notification.SMS",
                Interfaces.NotificationType.Information => "ms-winsoundevent:Notification.Default",
                _ => "ms-winsoundevent:Notification.Default"
            };
            
            audioElement.SetAttribute("src", audioSrc);
            toastXml.DocumentElement?.AppendChild(audioElement);

            var toast = new ToastNotification(toastXml);
            
            // Set priority for critical notifications
            if (type == Interfaces.NotificationType.Error)
            {
                toast.Priority = ToastNotificationPriority.High;
            }
            
            ToastNotificationManager.CreateToastNotifier(APP_ID).Show(toast);
            
            await Task.CompletedTask;
        }
        catch (Exception)
        {
            // Fallback to in-app notification if toast fails
            await ShowNotificationAsync(title, message, type);
        }
    }
}
#endif