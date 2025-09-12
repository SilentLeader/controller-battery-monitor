using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ControllerMonitor.ViewModels;
using ControllerMonitor.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace ControllerMonitor
{
    public partial class App : Application
    {   
        public AppViewModel? _viewModel;

        private MainWindow? _mainWindow;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Resolve services from DI container
                _mainWindow = Program.ServiceProvider!.GetRequiredService<MainWindow>();
                _viewModel = Program.ServiceProvider!.GetRequiredService<AppViewModel>();

                desktop.MainWindow = _mainWindow;

                // Set the MainWindow in the single instance service for window activation
                if (Program.SingleInstanceService != null)
                {
                    Program.SingleInstanceService.SetMainWindow(_mainWindow);
                }

                // Set DataContext at Application level for bindings to work
                if (_viewModel != null)
                {
                    DataContext = _viewModel;
                }

                // Handle system shutdown to allow proper logout
                desktop.ShutdownRequested += (sender, e) =>
                {
                    CleanupAndShutdown(desktop);
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        public void ShowMainWindow_Click(object sender, EventArgs args)
        {
            if (_mainWindow != null)
            {
                _mainWindow.Show();
                _mainWindow.WindowState = WindowState.Normal;
                _mainWindow.Activate();
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