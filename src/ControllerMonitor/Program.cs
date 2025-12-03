using Avalonia;
using System;
using System.Linq;
using ControllerMonitor.Services;
using ControllerMonitor.Interfaces;
using Serilog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ControllerMonitor.ViewModels;
using ControllerMonitor.Windows;
using System.Runtime.InteropServices;
#if LINUX
using ControllerMonitor.Platforms.Linux;
using ControllerMonitor.UPower.Extensions;
#endif
#if WINDOWS
using ControllerMonitor.Platforms.Windows;
using ControllerMonitor.XInput.Interfaces;
using ControllerMonitor.XInput.Services;
#endif

namespace ControllerMonitor;

static class Program
{
    private const string LogLevelParamName = "--log-level=";

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    public static int Main(string[] args)
    {
        // Configure Serilog
        Log.Logger = BuildLogger(args);

        // Set up dependency injection
        var serviceProvider = ConfigureServices();
        try
        {
            using (var singleInstanceService = serviceProvider.GetRequiredService<SingleInstanceService>())
            {
                if (!singleInstanceService.TryAcquireSingleInstance())
                {
                    // Another instance is running, bring it to front
                    singleInstanceService.BringExistingWindowToFront();
                    return 2;
                }

                var settingsService = serviceProvider.GetRequiredService<ISettingsService>();
                settingsService.LoadSettings();
                var batteryMonitorService = serviceProvider.GetRequiredService<IBatteryMonitorService>();
                batteryMonitorService.StartMonitoring();

                // This is the first instance, proceed with the application
                BuildAvaloniaApp(serviceProvider)
                    .StartWithClassicDesktopLifetime(args);
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Fatal(ex, "Fatal error");
            return 1;
        }

        return 0;
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp(IServiceProvider serviceProvider)
        => AppBuilder.Configure(() => serviceProvider.GetRequiredService<App>())
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    private static Serilog.Core.Logger BuildLogger(string[] args)
    {
        var loggerConfig = new LoggerConfiguration()
            .WriteTo.Console();
        
        if (args.Length > 0 && args.Any(x => x.StartsWith(LogLevelParamName, StringComparison.InvariantCultureIgnoreCase)))
        {
            var logLevelParam = args.First(x => x.StartsWith(LogLevelParamName, StringComparison.InvariantCultureIgnoreCase));
            var logLevel = logLevelParam[LogLevelParamName.Length..];
            switch (logLevel.ToLower())
            {
                case "verbose":
                    loggerConfig.MinimumLevel.Verbose();
                    break;
                case "debug":
                    loggerConfig.MinimumLevel.Debug();
                    break;
                case "warning":
                    loggerConfig.MinimumLevel.Warning();
                    break;
                case "error":
                    loggerConfig.MinimumLevel.Error();
                    break;
                case "fatal":
                    loggerConfig.MinimumLevel.Fatal();
                    break;
                default:
                    loggerConfig.MinimumLevel.Information();
                    break;
            }
        }

        return loggerConfig.CreateLogger();
    }

    private static ServiceProvider ConfigureServices()
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
#if WINDOWS
            services.AddSingleton<IXInputService, XInputService>();
            services.AddSingleton<IBatteryMonitorService, BatteryMonitorWindows>();
            services.AddSingleton<ISettingsService, SettingsServiceWindows>();
            services.AddSingleton<INotificationService, NotificationServiceWindows>();
#endif
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
#if LINUX
            services.AddSingleton<IBatteryMonitorService, BatteryMonitorLinux>();
            services.AddSingleton<ISettingsService, SettingsServiceLinux>();
            services.AddSingleton<INotificationService, NotificationServiceLinux>();
            services.AddUPower();
#endif
        }

        // Register viewmodel and window so they can be resolved from DI
        services.AddTransient(s => new SettingsViewModel(s.GetRequiredService<ISettingsService>().GetSettings()));
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();
        services.AddSingleton<AppViewModel>();
        services.AddSingleton<App>();

        return services.BuildServiceProvider();
    }


}