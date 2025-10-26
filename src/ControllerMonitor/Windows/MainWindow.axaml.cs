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
    
    private bool _isShutdown = false;

    public MainWindow() : this(Program.ServiceProvider!.GetRequiredService<MainWindowViewModel>(), Program.ServiceProvider!.GetRequiredService<INotificationService>())
    {
    }

    public MainWindow(MainWindowViewModel viewModel, INotificationService notificationService)
    {
        InitializeComponent();

        PropertyChanged += MainWindow_PropertyChanged;
        Opened += MainWindow_Opened;
        Closing += MainWindow_Closing;

        _viewModel = viewModel;
        DataContext = _viewModel;

        notificationService.Initialize(this);

        // Apply settings
        Position = new PixelPoint((int)viewModel.Settings.WindowX, (int)viewModel.Settings.WindowY);
        Width = viewModel.Settings.WindowWidth;
        Height = viewModel.Settings.WindowHeight;

        // Validate position is within screen bounds or if not set (default -1)
        var screens = Screens;
        bool validPosition = viewModel.Settings.WindowX != -1 && viewModel.Settings.WindowY != -1;
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
        if (_viewModel!.Settings.StartMinimized)
        {
            WindowState = WindowState.Minimized;
        }
    }

    private void MainWindow_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == WindowStateProperty && WindowState == WindowState.Minimized && _viewModel!.Settings.MinimizeToTray)
        {
            ShowInTaskbar = false;
        }
    }

    private void MainWindow_Closing(object? sender, WindowClosingEventArgs e)
    {
        if (!_isShutdown && WindowState != WindowState.Minimized)
        {
            e.Cancel = true;
            WindowState = WindowState.Minimized;
        }
    }

    public void PrepareForShutdown()
    {
        _isShutdown = true;
    }
}