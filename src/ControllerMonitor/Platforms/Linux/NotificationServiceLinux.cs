#if LINUX
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ControllerMonitor.Interfaces;
using ControllerMonitor.Services;
using ControllerMonitor.ValueObjects;
using Microsoft.Extensions.Logging;

namespace ControllerMonitor.Platforms.Linux;

public class NotificationServiceLinux(ILogger<INotificationService> logger) : NotificationServiceBase
{
    public override async Task ShowSystemNotificationAsync(string title, string message, NotificationPriority type = NotificationPriority.Normal, int? expirationTime = 10)
    {
        try
        {
            // Map NotificationType to notify-send urgency levels
            var urgency = type switch
            {
                NotificationPriority.High => "critical",
                NotificationPriority.Normal => "normal",
                NotificationPriority.Low => "low",
                _ => "normal"
            };

            // Build the notify-send arguments
            var arguments = $"--app-name=\"Controller Monitor\" --icon=input-gaming --urgency={urgency}";
            var now = DateTimeOffset.Now;
            var timeoutMs = (expirationTime ?? 10) * 1000;
            
            // Ensure timeout is positive (at least 1ms)
            if (timeoutMs > 0)
            {
                arguments += $" --expire-time={timeoutMs}";
            }
            
            arguments += $" \"{title}\" \"{message}\"";

            // Use notify-send for Linux desktop notifications with urgency level and optional expiration
            var processInfo = new ProcessStartInfo
            {
                FileName = "notify-send",
                Arguments = arguments,
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
        catch (Exception ex)
        {
            logger.LogWarning(ex, "System notification failed");
            // Fallback to in-app notification if notify-send fails
            await ShowNotificationAsync(title, message, type);
        }
    }
}
#endif