using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ControllerMonitor.Interfaces;
using ControllerMonitor.Services;
using ControllerMonitor.ViewModels;
using ControllerMonitor.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace ControllerMonitor
{
    public partial class App(IServiceProvider serviceProvider) : Application()
    {
        private AppViewModel? _viewModel;

        private MainWindow? _mainWindow;
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private bool _isShuttingDown;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Set shutdown mode to OnExplicitShutdown to allow the application to run in background
                desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                _viewModel = _serviceProvider.GetRequiredService<AppViewModel>();

                _serviceProvider.GetRequiredService<MainWindowViewModel>();

                var singleInstanceService = _serviceProvider.GetRequiredService<SingleInstanceService>();
                singleInstanceService.SetShowMainWindowCallback(ShowMainWindow);

                if (_viewModel != null)
                {
                    DataContext = _viewModel;
                }

                var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
                if (!settingsService.GetSettings().StartMinimized)
                {
                    ShowMainWindow();
                }

                // Handle system shutdown to allow proper logout
                desktop.ShutdownRequested += (sender, e) =>
                {
                    CleanupAndShutdown(desktop);
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        public void ShowMainWindow_Click(object sender, EventArgs args) => ShowMainWindow();

        private void ShowMainWindow()
        {
            if (_mainWindow == null)
            {
                _mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                _mainWindow.Closed += MainWindow_Closed;

                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.MainWindow = _mainWindow;
                }
            }

            _mainWindow.ShowInTaskbar = true;
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Show();

            _mainWindow.Activate();
            _mainWindow.Focus();
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            if (_mainWindow != null)
            {
                _mainWindow.Closed -= MainWindow_Closed;
            }
            _mainWindow = null;

            if (!_isShuttingDown)
            {
                // Free up resources after window is closed
                Task.Run(() => GC.Collect(2, GCCollectionMode.Aggressive, blocking: true, compacting: true));
            }
        }

        public void ExitApplication_Click(object sender, EventArgs args)
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                CleanupAndShutdown(desktop);
            }
        }

        private void CleanupAndShutdown(IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (_isShuttingDown)
            {
                return;
            }
            _isShuttingDown = true;

            // Prepare main window for shutdown and close
            try
            {
                _mainWindow?.PrepareForShutdown();
                _mainWindow?.Close();
            }
            catch
            {
                // Ignore exceptions during shutdown
            }

            // Shutdown application
            desktop.Shutdown();
        }
    }
}