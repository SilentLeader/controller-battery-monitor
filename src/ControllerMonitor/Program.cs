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

    public static IServiceProvider? ServiceProvider { get; private set; }

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    public static int Main(string[] args)
    {
        // Configure Serilog
        var loggerconfig = new LoggerConfiguration()
            .WriteTo.Console();
        SetLogLevel(args, loggerconfig);

        Log.Logger = loggerconfig.CreateLogger();

        // Set up dependency injection
        ConfigureServices();
        try
        {
            using (var singleInstanceService = ServiceProvider!.GetRequiredService<SingleInstanceService>())
            {
                if (!singleInstanceService.TryAcquireSingleInstance())
                {
                    // Another instance is running, bring it to front
                    singleInstanceService.BringExistingWindowToFront();
                    return 2;
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
        catch (Exception ex)
        {
            Log.Logger.Fatal(ex, "Fatal error");
            return 1;
        }

        return 0;
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    private static void SetLogLevel(string[] args, LoggerConfiguration loggerconfig)
    {
        if (args.Length > 0 && args.Any(x => x.StartsWith(LogLevelParamName, StringComparison.InvariantCultureIgnoreCase)))
        {
            var logLevelParam = args.First(x => x.StartsWith(LogLevelParamName, StringComparison.InvariantCultureIgnoreCase));
            var logLevel = logLevelParam[LogLevelParamName.Length..];
            switch (logLevel.ToLower())
            {
                case "verbose":
                    loggerconfig.MinimumLevel.Verbose();
                    break;
                case "debug":
                    loggerconfig.MinimumLevel.Debug();
                    break;
                case "warning":
                    loggerconfig.MinimumLevel.Warning();
                    break;
                case "error":
                    loggerconfig.MinimumLevel.Error();
                    break;
                case "fatal":
                    loggerconfig.MinimumLevel.Fatal();
                    break;
                default:
                    loggerconfig.MinimumLevel.Information();
                    break;
            }
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

        ServiceProvider = services.BuildServiceProvider();
    }


}