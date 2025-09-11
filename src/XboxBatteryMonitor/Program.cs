using Avalonia;
using System;
using XboxBatteryMonitor.Services;
using Serilog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XboxBatteryMonitor.ViewModels;
using XboxBatteryMonitor.Windows;

namespace XboxBatteryMonitor;

static class Program
{
    public static SingleInstanceService? SingleInstanceService => _singleInstanceService;
    private static SingleInstanceService? _singleInstanceService;
    public static IServiceProvider? ServiceProvider { get; private set; }

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
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

        // Register platform battery service via factory so DI can resolve it
        services.AddSingleton(_ => BatteryMonitorFactory.CreatePlatformService());

        // Register notification service implementation
        services.AddSingleton<INotificationService, NotificationService>();

        // Load settings into a singleton SettingsViewModel so all consumers share same instance
        services.AddSingleton(sp => sp.GetRequiredService<SettingsService>().LoadSettings());

        // Register viewmodel and window so they can be resolved from DI
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

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