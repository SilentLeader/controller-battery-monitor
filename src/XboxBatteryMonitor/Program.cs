using Avalonia;
using System;
using XboxBatteryMonitor.Services;

namespace XboxBatteryMonitor;

static class Program
{
    public static SingleInstanceService? SingleInstanceService => _singleInstanceService;
    private static SingleInstanceService? _singleInstanceService;

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        using (_singleInstanceService = new SingleInstanceService())
        {
            if (!_singleInstanceService.TryAcquireSingleInstance())
            {
                // Another instance is running, bring it to front
                _singleInstanceService.BringExistingWindowToFront();
                return;
            }

            // This is the first instance, proceed with the application
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}