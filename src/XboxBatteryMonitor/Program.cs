using Avalonia;
using System;
using XboxBatteryMonitor.Services;
using Serilog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XboxBatteryMonitor.ViewModels;
using XboxBatteryMonitor.Windows;
using System.Runtime.InteropServices;
using XboxBatteryMonitor.Platforms.Windows;
using XboxBatteryMonitor.Platforms.Linux;

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
        ServiceProvider = RegisterServices();

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

    private static ServiceProvider RegisterServices()
    {
        var services = new ServiceCollection();
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog();
        });
        services.AddSingleton<SettingsService>();
        services.AddSingleton<SingleInstanceService>();

        // Platfrom specific service registration
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            services.AddSingleton<IBatteryMonitorService, BatteryMonitorWindows>();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            services.AddSingleton<IBatteryMonitorService, BatteryMonitorLinux>();
        }

        // Register notification service implementation
        services.AddSingleton<INotificationService, NotificationService>();

        // Load settings into a singleton SettingsViewModel so all consumers share same instance
        services.AddSingleton(sp => sp.GetRequiredService<SettingsService>().LoadSettings());

        // Register viewmodel and window so they can be resolved from DI
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}