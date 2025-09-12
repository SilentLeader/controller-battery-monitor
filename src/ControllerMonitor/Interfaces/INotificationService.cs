using System.Threading.Tasks;
using Avalonia.Controls;

namespace ControllerMonitor.Interfaces;

public interface INotificationService
{
    void Initialize(Window window);
    Task ShowNotificationAsync(string title, string message);
}
