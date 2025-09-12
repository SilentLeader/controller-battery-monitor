using System.Threading.Tasks;
using Avalonia.Controls;

namespace XboxBatteryMonitor.Interfaces;

public interface INotificationService
{
    void Initialize(Window window);
    Task ShowNotificationAsync(string title, string message);
}
