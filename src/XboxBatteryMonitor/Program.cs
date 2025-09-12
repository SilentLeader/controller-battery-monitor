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
        ConfigureServices();

        using (_singleInstanceService = ServiceProvider!.GetRequiredService<SingleInstanceService>())
        {
            if (!_singleInstanceService.TryAcquireSingleInstance())
            {
                // Another instance is running, bring it to front
                _singleInstanceService.BringExistingWindowToFront();
                return;
            }

            var settingsService = ServiceProvider!.GetRequiredService<ISettingsService>();            
            settingsService.LoadSettings();
            var batteryMonitorService = ServiceProvider!.GetRequiredService<IBatteryMonitorService>();
            batteryMonitorService.StartMonitoring();

            // This is the first instance, proceed with the application
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
    }

    private static void ConfigureServices()
    {
        var services = new ServiceCollection();
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog();
        });
        services.AddSingleton<SingleInstanceService>();

        // Platfrom specific service registration
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            services.AddSingleton<IBatteryMonitorService, BatteryMonitorWindows>();
            services.AddSingleton<ISettingsService, SettingsServiceWindows>();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            services.AddSingleton<IBatteryMonitorService, BatteryMonitorLinux>();
            services.AddSingleton<ISettingsService, SettingsServiceLinux>();
        }

        // Register notification service implementation
        services.AddSingleton<INotificationService, NotificationService>();

        // Register viewmodel and window so they can be resolved from DI
        services.AddTransient(s => new SettingsViewModel(s.GetRequiredService<ISettingsService>().GetSettings()));
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        ServiceProvider = services.BuildServiceProvider();
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}