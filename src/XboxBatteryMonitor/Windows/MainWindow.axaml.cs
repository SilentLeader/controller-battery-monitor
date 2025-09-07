using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using System;
using System.ComponentModel;
using XboxBatteryMonitor.Services;
using XboxBatteryMonitor.ValueObjects;
using XboxBatteryMonitor.ViewModels;
using Avalonia.Styling;
using Avalonia.Media.Imaging;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

namespace XboxBatteryMonitor.Windows;

public partial class MainWindow : Window
{
    private TrayIcon? _trayIcon;
    private MainWindowViewModel? _viewModel;
    private NativeMenuItem? _statusMenuItem;
    private SettingsViewModel _settings;
    private bool _isShutdown = false;

    public MainWindow() : this(new SettingsViewModel())
    {
    }

    public MainWindow(SettingsViewModel settings)
    {
        _settings = settings;
        InitializeComponent();

        PropertyChanged += MainWindow_PropertyChanged;
        Opened += MainWindow_Opened;
        Closing += MainWindow_Closing;

        UpdateWindowIcon();

        // Use factory to create platform-specific service
        var service = BatteryMonitorFactory.CreatePlatformService();
        var notificationService = new NotificationService();
        _viewModel = new MainWindowViewModel(service, settings, notificationService);
        DataContext = _viewModel;

        notificationService.Initialize(this);

        // Apply settings
        Position = new PixelPoint((int)settings.WindowX, (int)settings.WindowY);
        Width = settings.WindowWidth;
        Height = settings.WindowHeight;

        // Validate position is within screen bounds or if not set (default -1)
        var screens = Screens;
        bool validPosition = settings.WindowX != -1 && settings.WindowY != -1;
        if (validPosition)
        {
            foreach (var screen in screens.All)
            {
                if (Position.X >= screen.Bounds.X && Position.X + Width <= screen.Bounds.X + screen.Bounds.Width &&
                    Position.Y >= screen.Bounds.Y && Position.Y + Height <= screen.Bounds.Y + screen.Bounds.Height)
                {
                    validPosition = true;
                    break;
                }
                else
                {
                    validPosition = false;
                }
            }
        }
        if (!validPosition)
        {
            // Center on primary screen
            var primary = screens.Primary;
            if (primary != null)
            {
                Position = new PixelPoint(
                    (int)(primary.Bounds.X + (primary.Bounds.Width - Width) / 2),
                    (int)(primary.Bounds.Y + (primary.Bounds.Height - Height) / 2)
                );
            }
        }


        // Setup tray icon
        _trayIcon = new TrayIcon();
        _trayIcon.ToolTipText = "Xbox Battery Monitor";
        _trayIcon.IsVisible = true;
        _trayIcon.Clicked += (sender, e) =>
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        };

        // Setup tray menu
        _statusMenuItem = new NativeMenuItem { Header = "Xbox Battery Monitor", IsEnabled = false };
        var openItem = new NativeMenuItem { Header = "Open Main Window" };
        openItem.Click += (sender, e) =>
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        };
        var exitItem = new NativeMenuItem { Header = "Exit" };
        exitItem.Click += (sender, e) =>
        {
            _trayIcon?.Dispose();
            _isShutdown = true;
            Close();
            _viewModel.Dispose();
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
            {
                desktopLifetime.Shutdown();
            }
        };
        var menu = new NativeMenu();
        menu.Items.Add(_statusMenuItem);
        menu.Items.Add(openItem);
        menu.Items.Add(exitItem);
        _trayIcon.Menu = menu;

        // Attach to battery info changes
        _viewModel.ControllerInfo.BatteryInfo.PropertyChanged += BatteryInfo_PropertyChanged;

        // Attach to theme changes
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += (s, e) => { UpdateTrayIcon(); UpdateWindowIcon(); };
        }

        // Initial update
        UpdateTrayIcon();

        // Attach to window events for saving position/size
        PositionChanged += MainWindow_PositionChanged;
        SizeChanged += MainWindow_SizeChanged;
        Closed += MainWindow_Closed;
    }

    private void MainWindow_PositionChanged(object? sender, EventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.Settings.WindowX = Position.X;
            _viewModel.Settings.WindowY = Position.Y;
            _viewModel.SaveSettingsCommand.Execute(null);
        }
    }

    private void MainWindow_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.Settings.WindowWidth = Width;
            _viewModel.Settings.WindowHeight = Height;
            _viewModel.SaveSettingsCommand.Execute(null);
        }
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.SaveSettingsCommand.Execute(null);
        }
    }

    private void MainWindow_Opened(object? sender, EventArgs e)
    {
        if (_settings.StartMinimized)
        {
            WindowState = WindowState.Minimized;
        }
    }

    private void MainWindow_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Window.WindowStateProperty)
        {
            ShowInTaskbar = WindowState != WindowState.Minimized;
        }
    }

    private void MainWindow_Closing(object? sender, WindowClosingEventArgs e)
    {
        if (!_isShutdown && WindowState != WindowState.Minimized)
        {
            e.Cancel = true;
            WindowState = WindowState.Minimized;
            Hide();
        }
    }

    private void BatteryInfo_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(() => UpdateTrayIcon());
    }

    private void UpdateTrayIcon()
    {
        if (_trayIcon == null || _viewModel == null) return;

        var battery = _viewModel.ControllerInfo.BatteryInfo;
        string status = battery.IsCharging ? "Charging" : "Not Charging";
        string connection = battery.IsConnected ? "Connected" : "Disconnected";
        _trayIcon.ToolTipText = $"Xbox Controller - Battery: {battery.Level} - {status} - {connection}";
        if (_statusMenuItem != null)
        {
            _statusMenuItem.Header = _trayIcon.ToolTipText;
        }

        // Load and set icon from resources
        var themeVariant = Application.Current?.ActualThemeVariant;
        var theme = themeVariant == ThemeVariant.Dark ? "dark" : "light";
        var iconName = GetIconName(battery.Level, battery.IsCharging, battery.IsConnected);
        var uri = new Uri($"avares://XboxBatteryMonitor/Assets/icons/{theme}/{iconName}.png");
        using var stream = AssetLoader.Open(uri);
        var bitmap = new Bitmap(stream);
        _trayIcon.Icon = new WindowIcon(bitmap);

        // Handle tray icon visibility based on settings
        if (_viewModel != null && _viewModel.Settings.HideTrayIconWhenDisconnected && !battery.IsConnected)
        {
            _trayIcon.IsVisible = false;
        }
        else
        {
            _trayIcon.IsVisible = true;
        }
    }

    private void UpdateWindowIcon()
    {
        var themeVariant = Application.Current?.ActualThemeVariant;
        var theme = themeVariant == ThemeVariant.Dark ? "dark" : "light";
        var uri = new Uri($"avares://XboxBatteryMonitor/Assets/icons/{theme}/battery_normal.png");
        using var stream = AssetLoader.Open(uri);
        var bitmap = new Bitmap(stream);
        Icon = new WindowIcon(bitmap);
    }

    private static string GetIconName(BatteryLevel level, bool isCharging, bool isConnected)
    {
        if (!isConnected)
        {
            return "battery_disconnected";
        }

        if (isCharging)
        {
            return "battery_charging";
        }

        return level switch
        {
            BatteryLevel.Full => "battery_full",
            BatteryLevel.High => "battery_high",
            BatteryLevel.Normal => "battery_normal",
            BatteryLevel.Low => "battery_low",
            BatteryLevel.Empty => "battery_empty",
            _ => "battery_unknown"
        };
    }
}