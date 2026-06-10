using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using AnyShareWindows.Services;

namespace AnyShareWindows;

public partial class MainWindow : Window
{
    private readonly AdbService _adb = new();
    private readonly StartupService _startup = new();
    private readonly SettingsService _settings = new();
    private readonly UsageHistoryService _usageHistory = new();
    private readonly ClipboardShareService _clipboardShare = new();
    private readonly NetworkSharingService _networkSharing = new();
    private readonly DispatcherTimer _networkSharingTimer = new();

    private bool _historyExpanded = false;
    private bool _isUpdatingToggle = false;
    private bool _anyShareTunnelActive;

    public MainWindow()
    {
        InitializeComponent();

        LoadSettings();

        App.NetworkSpeed.Updated += OnNetworkSpeedUpdated;

        _networkSharingTimer.Interval = TimeSpan.FromSeconds(2);
        _networkSharingTimer.Tick += async (_, _) =>
        {
            if (SharingToggle.IsChecked == true)
            {
                await AutoConnectNetworkSharing();
                await UpdateAnyShareTunnelState();
            }
            else
            {
                _anyShareTunnelActive = false;
            }
        };
        _networkSharingTimer.Start();

        SpeedCard.Click += (_, _) =>
        {
            SpeedToggle.IsChecked = !(SpeedToggle.IsChecked ?? false);
        };

        SpeedToggle.IsCheckedChanged += (_, _) =>
        {
            if (_isUpdatingToggle) return;

            SpeedDetailsPanel.IsVisible = SpeedToggle.IsChecked ?? false;

            if (SpeedToggle.IsChecked == true)
            {
                App.SpeedWidget?.Show();
            }
            else
            {
                App.SpeedWidget?.Hide();
                _historyExpanded = false;
                HistoryPanel.IsVisible = false;
                HistoryArrow.Text = "▼";
            }

            SaveSettings();
        };

        ResetButton.Click += (_, _) =>
        {
            App.NetworkSpeed.ResetTodayUsage();
            _usageHistory.ResetToday();

            DownloadUsageText.Text = "0 MB";
            UploadUsageText.Text = "0 MB";
            TodayUsageText.Text = "0 MB";

            UpdateHistoryDisplay();
        };

        HistoryToggleButton.Click += (_, _) => ToggleHistory();

        SharingCard.Click += (_, _) =>
        {
            SharingToggle.IsChecked = !(SharingToggle.IsChecked ?? false);
        };

        SharingToggle.IsCheckedChanged += async (_, _) =>
        {
            if (_isUpdatingToggle) return;

            await OnNetworkSharingChanged();
            SaveSettings();
        };

        ClipboardCard.Click += (_, _) =>
        {
            ClipboardToggle.IsChecked = !(ClipboardToggle.IsChecked ?? false);
        };

        ClipboardToggle.IsCheckedChanged += async (_, _) =>
        {
            if (_isUpdatingToggle) return;

            await OnClipboardChanged();
            SaveSettings();
        };

        SendClipboardButton.Click += async (_, _) =>
        {
            await _clipboardShare.SetupAdbBridge();
            await _clipboardShare.SendWindowsClipboardToAndroid();
        };

        ReceiveClipboardButton.Click += async (_, _) =>
        {
            await _clipboardShare.SetupAdbBridge();
            await _clipboardShare.ReceiveAndroidClipboardToWindows();
        };

        StartupCard.Click += (_, _) =>
        {
            StartupToggle.IsChecked = !(StartupToggle.IsChecked ?? false);
        };

        StartupToggle.IsCheckedChanged += (_, _) =>
        {
            if (_isUpdatingToggle) return;

            _startup.SetStartup(StartupToggle.IsChecked == true);
            SaveSettings();
        };

        Closing += (_, _) => SaveSettings();

        UpdateHistoryDisplay();
    }

    public void RefreshNetworkSharingSpeedIfActive()
    {
        if (SharingToggle.IsChecked != true || !_anyShareTunnelActive)
            return;

        _networkSharing.UpdateSpeed();
        App.NetworkSpeed.SetExternalSpeed(
            _networkSharing.CurrentDownloadSpeed,
            _networkSharing.CurrentUploadSpeed,
            "AnyShare"
        );
    }

    private async Task UpdateAnyShareTunnelState()
    {
        var wasActive = _anyShareTunnelActive;

        if (SharingToggle.IsChecked != true)
        {
            _anyShareTunnelActive = false;

            if (wasActive)
                App.NetworkSpeed.ClearExternalSpeed();

            return;
        }

        var deviceConnected = await _adb.IsDeviceConnected();
        var bridgeForwarded = deviceConnected && await _adb.IsNetworkBridgeForwarded();
        var androidProxyRunning = deviceConnected && await _adb.IsAndroidNetworkProxyRunning();

        _anyShareTunnelActive =
            deviceConnected &&
            bridgeForwarded &&
            androidProxyRunning &&
            _networkSharing.IsProxyRunning;

        if (wasActive && !_anyShareTunnelActive)
            App.NetworkSpeed.ClearExternalSpeed();
    }

    private void LoadSettings()
    {
        var settings = _settings.LoadSettings();

        _isUpdatingToggle = true;

        SpeedToggle.IsChecked = settings.NetworkSpeedMonitor;
        SharingToggle.IsChecked = settings.NetworkSharing;
        ClipboardToggle.IsChecked = settings.ClipboardSharing;
        StartupToggle.IsChecked = settings.OpenAtStartup;

        SpeedDetailsPanel.IsVisible = settings.NetworkSpeedMonitor;
        ClipboardActionsPanel.IsVisible = settings.ClipboardSharing;

        if (settings.NetworkSpeedMonitor)
        {
            App.SpeedWidget?.Show();
        }
        else
        {
            App.SpeedWidget?.Hide();
        }

        _isUpdatingToggle = false;
    }

    private void SaveSettings()
    {
        var settings = new AppState
        {
            NetworkSpeedMonitor = SpeedToggle.IsChecked ?? false,
            NetworkSharing = SharingToggle.IsChecked ?? false,
            ClipboardSharing = ClipboardToggle.IsChecked ?? false,
            OpenAtStartup = StartupToggle.IsChecked ?? false,
            CurrentSpeed = DownloadSpeedText.Text ?? "0 KB/s",
            TodayUsage = TodayUsageText.Text ?? "0 MB"
        };

        _settings.SaveSettings(settings);
    }

    private void ToggleHistory()
    {
        _historyExpanded = !_historyExpanded;
        HistoryPanel.IsVisible = _historyExpanded;
        HistoryArrow.Text = _historyExpanded ? "▲" : "▼";

        if (_historyExpanded)
        {
            UpdateHistoryDisplay();
        }
    }

    private void UpdateHistoryDisplay()
    {
        HistoryItemsPanel.Children.Clear();

        var history = _usageHistory.GetLast7DaysHistory();

        if (history.Count == 0)
        {
            HistoryItemsPanel.Children.Add(new TextBlock
            {
                Text = "No history yet",
                Foreground = new Avalonia.Media.SolidColorBrush(
                    Avalonia.Media.Color.Parse("#6B7280")
                ),
                FontSize = 12
            });

            return;
        }

        foreach (var record in history)
        {
            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                RowDefinitions = new RowDefinitions("Auto"),
                Margin = new Avalonia.Thickness(0, 4, 0, 4),
                ColumnSpacing = 8
            };

            var dateText = new TextBlock
            {
                Text = $"{record.Date:MMM dd, yyyy}",
                Foreground = new Avalonia.Media.SolidColorBrush(
                    Avalonia.Media.Color.Parse("#6B7280")
                ),
                FontSize = 11
            };

            var usageStack = new StackPanel
            {
                Spacing = 2,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
            };

            var usageText = new TextBlock
            {
                Text = NetworkSpeedService.FormatBytes(record.BytesUsed),
                Foreground = new Avalonia.Media.SolidColorBrush(
                    Avalonia.Media.Color.Parse("White")
                ),
                FontWeight = Avalonia.Media.FontWeight.Bold,
                FontSize = 11
            };

            var sourceText = new TextBlock
            {
                Text = $"({record.NetworkSource})",
                Foreground = new Avalonia.Media.SolidColorBrush(
                    Avalonia.Media.Color.Parse("#3A86FF")
                ),
                FontSize = 10
            };

            usageStack.Children.Add(usageText);
            usageStack.Children.Add(sourceText);

            grid.Children.Add(dateText);
            Grid.SetColumn(usageStack, 1);
            grid.Children.Add(usageStack);

            HistoryItemsPanel.Children.Add(grid);
        }
    }

    private void OnNetworkSpeedUpdated()
    {
        if (SpeedToggle.IsChecked != true)
            return;

        DownloadSpeedText.Text = App.NetworkSpeed.GetCurrentDownloadSpeed();
        UploadSpeedText.Text = App.NetworkSpeed.GetCurrentUploadSpeed();
        DownloadUsageText.Text = App.NetworkSpeed.GetTodayDownloadUsage();
        UploadUsageText.Text = App.NetworkSpeed.GetTodayUploadUsage();
        TodayUsageText.Text = App.NetworkSpeed.GetTodayUsage();
        NetworkSourceText.Text = App.NetworkSpeed.NetworkSource;

        _usageHistory.UpdateTodayUsage(
            App.NetworkSpeed.CurrentDownloadSpeed + App.NetworkSpeed.CurrentUploadSpeed,
            App.NetworkSpeed.NetworkSource
        );

        if (_historyExpanded)
        {
            UpdateHistoryDisplay();
        }
    }

    private async Task OnNetworkSharingChanged()
    {
        if (SharingToggle.IsChecked == true)
        {
            await AutoConnectNetworkSharing();
            await UpdateAnyShareTunnelState();
        }
        else
        {
            _anyShareTunnelActive = false;
            App.NetworkSpeed.ClearExternalSpeed();
            await _adb.RemoveNetworkBridge();
            _networkSharing.DisableWindowsProxy();
        }
    }

    private async Task AutoConnectNetworkSharing()
    {
        var connected = await _adb.IsDeviceConnected();

        if (!connected)
        {
            _networkSharing.DisableWindowsProxy();
            return;
        }

        var bridgeOk = await _adb.SetupNetworkBridge();

        if (bridgeOk)
        {
            _networkSharing.EnableWindowsProxy();
        }
        else
        {
            _networkSharing.DisableWindowsProxy();
        }
    }

    private async Task OnClipboardChanged()
    {
        ClipboardActionsPanel.IsVisible =
            ClipboardToggle.IsChecked == true;

        if (ClipboardToggle.IsChecked == true)
        {
            if (!await _adb.IsDeviceConnected())
            {
                _isUpdatingToggle = true;
                ClipboardToggle.IsChecked = false;
                ClipboardActionsPanel.IsVisible = false;
                _isUpdatingToggle = false;
                return;
            }

            await _adb.SetupClipboardBridge();
            await _clipboardShare.SetupAdbBridge();
        }
    }

    public void UpdateTrayIcon(string uploadSpeed, string downloadSpeed)
    {
    }
}