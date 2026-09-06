using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using ControllerMonitor.Interfaces;
using ControllerMonitor.ViewModels;

namespace ControllerMonitor.Windows;

public partial class MainWindow : Window
{
    private MainWindowViewModel? _viewModel;
    private INotificationService? _notificationService;

    private bool _isShutdown = false;

    // Set right before an intentional Close() triggered by minimizing to tray, so
    // MainWindow_Closing lets it through instead of cancelling it back to Minimized.
    private bool _isClosingToTray = false;

    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainWindowViewModel viewModel, INotificationService notificationService)
    {
        InitializeComponent();

        PropertyChanged += MainWindow_PropertyChanged;
        Closing += MainWindow_Closing;
        Closed += MainWindow_Closed;

        _viewModel = viewModel;
        DataContext = _viewModel;

        _notificationService = notificationService;
        notificationService.Initialize(this);

        // Apply settings
        Position = new PixelPoint((int)viewModel.Settings.WindowX, (int)viewModel.Settings.WindowY);
        Width = viewModel.Settings.WindowWidth;
        Height = viewModel.Settings.WindowHeight;

        // Validate position is within screen bounds or if not set (default -1)
        if(!IsPositionValid(viewModel.Settings.WindowX, viewModel.Settings.WindowY))
        {
            // Center on primary screen
            CenterOnPrimaryScreen();
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

    public void PrepareForShutdown()
    {
        _isShutdown = true;
    }

    private void CenterOnPrimaryScreen()
    {
        var primary = Screens.Primary;
        if (primary != null)
        {
            Position = new PixelPoint(
                (int)(primary.Bounds.X + (primary.Bounds.Width - Width) / 2),
                (int)(primary.Bounds.Y + (primary.Bounds.Height - Height) / 2)
            );
        }
    }

    private bool IsPositionValid(double windowX, double windowY)
    {
        bool validPosition = windowX != -1 && windowY != -1;
        if (validPosition)
        {
            foreach (var screen in Screens.All)
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

        return validPosition;
        
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

    private void MainWindow_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == WindowStateProperty
            && WindowState == WindowState.Minimized
            && _viewModel!.Settings.MinimizeToTray)
        {
            ShowInTaskbar = false;
            Hide();

            Dispatcher.UIThread.Post(() =>
            {
                // Skip if the user reopened the window before this ran.
                if (!IsVisible)
                {
                    _isClosingToTray = true;
                    Close();
                }
            }, DispatcherPriority.Background);
        }
    }

    private void MainWindow_Closing(object? sender, WindowClosingEventArgs e)
    {
        if (!_isShutdown && !_isClosingToTray && WindowState != WindowState.Minimized)
        {
            e.Cancel = true;
            WindowState = WindowState.Minimized;
        }
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        // Release the notification manager's reference to this window so the whole
        // visual tree becomes eligible for garbage collection once closed.
        _notificationService?.Initialize(null);
        _notificationService = null;
    }
}