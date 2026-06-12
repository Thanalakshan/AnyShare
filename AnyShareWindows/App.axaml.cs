using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;
using AnyShareWindows.Services;

namespace AnyShareWindows;

public partial class App : Application
{
    private MainWindow? _mainWindow;
    private TrayIcon? _trayIcon;
    private bool _allowExit = false;

    private SettingsService? _settings;
    private DispatcherTimer? _trayUpdateTimer;

    public static NetworkSpeedService NetworkSpeed { get; private set; } = new();
    public static TaskbarWidgetWindow? SpeedWidget { get; private set; }

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

            _mainWindow = new MainWindow
            {
                ShowActivated = false
            };

            _mainWindow.Closing += (_, e) =>
            {
                if (!_allowExit)
                {
                    e.Cancel = true;
                    _mainWindow.Hide();
                }
            };

            CreateTrayIcon();

            SpeedWidget = new TaskbarWidgetWindow();
            SpeedWidget.OpenRequested += OpenWindow;
            SpeedWidget.Hide();

            StartTrayUpdateTimer();

            desktop.ShutdownRequested += (_, _) =>
            {
                CleanupNetworkSharing();
                _trayUpdateTimer?.Stop();
                SpeedWidget?.Close();
                _trayIcon?.Dispose();
            };
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
            IsVisible = true,
            Icon = new WindowIcon(
                AssetLoader.Open(new Uri("avares://AnyShareWindows/Assets/icon/icon.ico"))
            )
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
            if (_trayIcon == null || _settings == null)
                return;

            var settings = _settings.LoadSettings();

            if (settings.NetworkSpeedMonitor)
            {
                _mainWindow?.RefreshNetworkSharingSpeedIfActive();
                NetworkSpeed.GetCurrentSpeed();

                var download = NetworkSpeed.GetCurrentDownloadSpeed();
                var upload = NetworkSpeed.GetCurrentUploadSpeed();

                _trayIcon.ToolTipText = $"AnyShare\n↓ {download}\n↑ {upload}";

                SpeedWidget?.UpdateSpeed(upload, download);

                if (SpeedWidget?.IsVisible == false)
                {
                    SpeedWidget.Show();
                }
            }
            else
            {
                _trayIcon.ToolTipText = "AnyShare";

                if (SpeedWidget?.IsVisible == true)
                {
                    SpeedWidget.Hide();
                }
            }
        };

        _trayUpdateTimer.Start();
    }

    private void OpenWindow()
    {
        _mainWindow ??= new MainWindow();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = _mainWindow;

        _mainWindow.ShowActivated = true;
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Show();
        _mainWindow.Activate();
    }

    public void ShutdownApplication()
    {
        _allowExit = true;

        CleanupNetworkSharing();

        _trayUpdateTimer?.Stop();
        SpeedWidget?.Close();
        _trayIcon?.Dispose();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    private void ExitApp()
    {
        ShutdownApplication();
    }

    private static void CleanupNetworkSharing()
    {
        try
        {
            new NetworkSharingService().DisableWindowsProxy();
        }
        catch
        {
        }
    }
}
