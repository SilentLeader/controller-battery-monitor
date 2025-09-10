using Avalonia;
using System;
using XboxBatteryMonitor.Services;
using Serilog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace XboxBatteryMonitor;

static class Program
{
    public static SingleInstanceService? SingleInstanceService => _singleInstanceService;
    private static SingleInstanceService? _singleInstanceService;
    public static IServiceProvider? ServiceProvider { get; private set; }

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        // Set up dependency injection
        var services = new ServiceCollection();
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog();
        });
        services.AddSingleton<SettingsService>();
        services.AddSingleton<SingleInstanceService>();
        // Add other services as needed
        ServiceProvider = services.BuildServiceProvider();

        using (_singleInstanceService = ServiceProvider.GetRequiredService<SingleInstanceService>())
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