using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using ControllerMonitor.ValueObjects;

namespace ControllerMonitor.Interfaces;

public interface INotificationService
{
    void Initialize(Window window);
    Task ShowNotificationAsync(string title, string message, NotificationType type = NotificationType.Normal);
    Task ShowSystemNotificationAsync(string title, string message, NotificationType type = NotificationType.Normal, DateTimeOffset? expirationTime = null);
}
