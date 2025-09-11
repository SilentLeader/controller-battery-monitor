using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using XboxBatteryMonitor.Services;
using XboxBatteryMonitor.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace XboxBatteryMonitor
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Resolve main window from DI container
                var mainWindow = Program.ServiceProvider!.GetRequiredService<MainWindow>();
                desktop.MainWindow = mainWindow;

                // Set the MainWindow in the single instance service for window activation
                if (Program.SingleInstanceService != null)
                {
                    Program.SingleInstanceService.SetMainWindow(mainWindow);
                }
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}