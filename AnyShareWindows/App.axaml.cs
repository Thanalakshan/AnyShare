using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using AnyShareWindows.Services;

namespace AnyShareWindows;

public partial class App : Application
{
    private MainWindow? _mainWindow;
    private TrayIcon? _trayIcon;
    private bool _allowExit = false;
    private SettingsService? _settings;
    private NetworkSpeedService? _networkSpeed;
    private DispatcherTimer? _trayUpdateTimer;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _settings = new SettingsService();
            _networkSpeed = new NetworkSpeedService();
            _mainWindow = new MainWindow();
            desktop.MainWindow = _mainWindow;

            _mainWindow.Closing += (_, e) =>
            {
                if (!_allowExit)
                {
                    e.Cancel = true;
                    _mainWindow.Hide();
                }
            };

            // Save settings before app actually exits
            desktop.ShutdownRequested += (_, _) =>
            {
                _trayUpdateTimer?.Stop();
                _trayIcon?.Dispose();
            };

            CreateTrayIcon();
            StartTrayUpdateTimer();

            _mainWindow.Hide();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void CreateTrayIcon()
    {
        var menu = new NativeMenu();

        var openItem = new NativeMenuItem("Open AnyShare");
        openItem.Click += (_, _) => OpenWindow();

        var exitItem = new NativeMenuItem("Exit");
        exitItem.Click += (_, _) => ExitApp();

        menu.Items.Add(openItem);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(exitItem);

        _trayIcon = new TrayIcon
        {
            ToolTipText = "AnyShare",
            Menu = menu,
            IsVisible = true
        };

        _trayIcon.Clicked += (_, _) => OpenWindow();
    }

    private void StartTrayUpdateTimer()
    {
        _trayUpdateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        _trayUpdateTimer.Tick += (_, _) =>
        {
            if (_networkSpeed != null && _trayIcon != null)
            {
                var settings = _settings?.LoadSettings();
                if (settings?.NetworkSpeedMonitor == true)
                {
                    var download = _networkSpeed.GetCurrentDownloadSpeed();
                    var upload = _networkSpeed.GetCurrentUploadSpeed();
                    _trayIcon.ToolTipText = $"AnyShare\n↓ {download} | ↑ {upload}";
                }
                else
                {
                    _trayIcon.ToolTipText = "AnyShare";
                }
            }
        };

        _trayUpdateTimer.Start();
    }

    private void OpenWindow()
    {
        if (_mainWindow == null)
        {
            _mainWindow = new MainWindow();
        }

        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    private void ExitApp()
    {
        _allowExit = true;
        _trayUpdateTimer?.Stop();
        _trayIcon?.Dispose();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}