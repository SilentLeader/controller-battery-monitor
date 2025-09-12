// Example usage of the enhanced system notification service with urgency levels

using ControllerMonitor.Interfaces;

public class NotificationExample
{
    private readonly INotificationService _notificationService;

    public NotificationExample(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task DemonstrateNotificationTypesAsync()
    {
        // Information notification - normal urgency
        await _notificationService.ShowSystemNotificationAsync(
            "Controller Status", 
            "Controller battery level is normal",
            NotificationType.Information
        );

        // Success notification - low urgency (less intrusive)
        await _notificationService.ShowSystemNotificationAsync(
            "Controller Connected", 
            "Xbox controller connected successfully",
            NotificationType.Success
        );

        // Warning notification - normal urgency
        await _notificationService.ShowSystemNotificationAsync(
            "Low Battery Warning", 
            "Controller battery is getting low (25%)",
            NotificationType.Warning
        );

        // Error notification - critical urgency (stays visible longer, alarm sound on Windows)
        await _notificationService.ShowSystemNotificationAsync(
            "Critical Battery Alert", 
            "Controller battery is critically low (5%)!",
            NotificationType.Error
        );
    }
}