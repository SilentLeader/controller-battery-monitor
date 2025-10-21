using Avalonia;
using Avalonia.Controls;
using System;
using ControllerMonitor.Interfaces;
using ControllerMonitor.ViewModels;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;

namespace ControllerMonitor.Windows;

public partial class MainWindow : Window
{
    private MainWindowViewModel? _viewModel;
    private SettingsViewModel _settings;
    private bool _isShutdown = false;

    public MainWindow() : this(Program.ServiceProvider!.GetRequiredService<MainWindowViewModel>(), new SettingsViewModel(), Program.ServiceProvider!.GetRequiredService<INotificationService>())
    {
    }

    public MainWindow(MainWindowViewModel viewModel, SettingsViewModel settings, INotificationService notificationService)
    {
        _settings = settings;
        InitializeComponent();

        PropertyChanged += MainWindow_PropertyChanged;
        Opened += MainWindow_Opened;
        Closing += MainWindow_Closing;

        _viewModel = viewModel;
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

        // Handle system shutdown to allow proper logout
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
        {
            desktopLifetime.ShutdownRequested += (sender, e) => _isShutdown = true;
        }

        // Attach to window events for saving position/size
        PositionChanged += MainWindow_PositionChanged;
        SizeChanged += MainWindow_SizeChanged;
    }

    private void MainWindow_PositionChanged(object? sender, EventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.Settings.WindowX = Position.X;
            _viewModel.Settings.WindowY = Position.Y;
        }
    }

    private void MainWindow_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.Settings.WindowWidth = Width;
            _viewModel.Settings.WindowHeight = Height;
        }
    }

    private void MainWindow_Opened(object? sender, EventArgs e)
    {
        if (_settings.StartMinimized && !_settings.StartClosed)
        {
            WindowState = WindowState.Minimized;
        }
        else if (_settings.StartClosed)
        {
            WindowState = WindowState.Minimized;
            Hide();
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

    public void PrepareForShutdown()
    {
        _isShutdown = true;
    }
}