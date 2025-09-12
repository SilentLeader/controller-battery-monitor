#if LINUX
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ControllerMonitor.Services;

namespace ControllerMonitor.Platforms.Linux;

public class NotificationServiceLinux : NotificationServiceBase
{
    public override async Task ShowSystemNotificationAsync(string title, string message, Interfaces.NotificationType type = Interfaces.NotificationType.Information)
    {
        try
        {
            // Map NotificationType to notify-send urgency levels
            var urgency = type switch
            {
                Interfaces.NotificationType.Error => "critical",
                Interfaces.NotificationType.Warning => "normal", 
                Interfaces.NotificationType.Success => "low",
                Interfaces.NotificationType.Information => "normal",
                _ => "normal"
            };

            // Use notify-send for Linux desktop notifications with urgency level
            var processInfo = new ProcessStartInfo
            {
                FileName = "notify-send",
                Arguments = $"--app-name=\"Controller Monitor\" --icon=input-gaming --urgency={urgency} \"{title}\" \"{message}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
            }
        }
        catch (Exception)
        {
            // Fallback to in-app notification if notify-send fails
            await ShowNotificationAsync(title, message, type);
        }
    }
}
#endif