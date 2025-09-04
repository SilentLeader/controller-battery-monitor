using System.Threading.Tasks;
using Avalonia.Controls;

namespace XboxBatteryMonitor.Services;

public interface INotificationService
{
    void Initialize(Window window);
    Task ShowNotificationAsync(string title, string message);
}