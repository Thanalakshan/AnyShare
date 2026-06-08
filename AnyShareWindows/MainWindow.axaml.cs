using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using AnyShareWindows.Services;

namespace AnyShareWindows;

public partial class MainWindow : Window
{
    private readonly NetworkSpeedService _networkSpeed = new();
    private readonly AdbService _adb = new();
    private readonly StartupService _startup = new();
    private readonly SettingsService _settings = new();
    private readonly UsageHistoryService _usageHistory = new();

    private readonly DispatcherTimer _timer = new();
    private bool _historyExpanded = false;

    public MainWindow()
    {
        InitializeComponent();

        // Load saved settings
        LoadSettings();

        // Speed toggle - click card to toggle
        SpeedCard.Click += (_, _) => { SpeedToggle.IsChecked = !SpeedToggle.IsChecked; };
        SpeedToggle.IsCheckedChanged += (_, _) => 
        { 
            SpeedDetailsPanel.IsVisible = SpeedToggle.IsChecked ?? false;
            SaveSettings();
            if (SpeedToggle.IsChecked == false)
            {
                _historyExpanded = false;
                HistoryPanel.IsVisible = false;
                HistoryArrow.Text = "▼";
            }
        };

        // Reset button
        ResetButton.Click += (_, _) => 
        {
            _networkSpeed.ResetTodayUsage();
            _usageHistory.ResetToday();
            TodayUsageText.Text = "0 MB";
        };

        // History toggle
        HistoryToggleButton.Click += (_, _) => ToggleHistory();

        // Sharing toggle - click card to toggle
        SharingCard.Click += async (_, _) => 
        { 
            if (SharingToggle.IsChecked != true)
            {
                SharingToggle.IsChecked = !SharingToggle.IsChecked;
            }
            else
            {
                SharingToggle.IsChecked = false;
                await OnNetworkSharingChanged();
            }
        };
        SharingToggle.IsCheckedChanged += async (_, _) => 
        { 
            SaveSettings();
            await OnNetworkSharingChanged();
        };

        // Clipboard toggle - click card to toggle
        ClipboardCard.Click += async (_, _) => 
        { 
            if (ClipboardToggle.IsChecked != true)
            {
                ClipboardToggle.IsChecked = !ClipboardToggle.IsChecked;
            }
            else
            {
                ClipboardToggle.IsChecked = false;
                await OnClipboardChanged();
            }
        };
        ClipboardToggle.IsCheckedChanged += async (_, _) => 
        { 
            SaveSettings();
            await OnClipboardChanged();
        };

        // Startup toggle - click card to toggle
        StartupCard.Click += (_, _) => { StartupToggle.IsChecked = !StartupToggle.IsChecked; };
        StartupToggle.IsCheckedChanged += (_, _) =>
        {
            _startup.SetStartup(StartupToggle.IsChecked == true);
            SaveSettings();
        };

        // Save settings before closing
        Closing += (_, _) => SaveSettings();

        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (_, _) =>
        {
            if (SpeedToggle.IsChecked == true)
            {
                var speed = _networkSpeed.GetCurrentSpeed();
                DownloadSpeedText.Text = _networkSpeed.GetCurrentDownloadSpeed();
                UploadSpeedText.Text = _networkSpeed.GetCurrentUploadSpeed();
                TodayUsageText.Text = _networkSpeed.GetTodayUsage();
                NetworkSourceText.Text = _networkSpeed.NetworkSource;

                // Update history service
                _usageHistory.UpdateTodayUsage(_networkSpeed.CurrentDownloadSpeed + _networkSpeed.CurrentUploadSpeed, _networkSpeed.NetworkSource);
            }
        };
        _timer.Start();

        // Generate history UI
        UpdateHistoryDisplay();
    }

    private void LoadSettings()
    {
        var settings = _settings.LoadSettings();
        
        SpeedToggle.IsChecked = settings.NetworkSpeedMonitor;
        SharingToggle.IsChecked = settings.NetworkSharing;
        ClipboardToggle.IsChecked = settings.ClipboardSharing;
        StartupToggle.IsChecked = settings.OpenAtStartup;

        // Show speed details if enabled
        SpeedDetailsPanel.IsVisible = settings.NetworkSpeedMonitor;
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
    }

    private void UpdateHistoryDisplay()
    {
        HistoryItemsPanel.Children.Clear();

        var history = _usageHistory.GetLast7DaysHistory();

        if (history.Count == 0)
        {
            var emptyText = new TextBlock
            {
                Text = "No history yet",
                Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#6B7280")),
                FontSize = 12
            };
            HistoryItemsPanel.Children.Add(emptyText);
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
                Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#6B7280")),
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
                Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("White")),
                FontWeight = Avalonia.Media.FontWeight.Bold,
                FontSize = 11
            };

            var sourceText = new TextBlock
            {
                Text = $"({record.NetworkSource})",
                Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#3A86FF")),
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

    private async Task OnNetworkSharingChanged()
    {
        if (SharingToggle.IsChecked == true)
        {
            if (!await _adb.IsDeviceConnected())
            {
                SharingToggle.IsChecked = false;
                return;
            }

            await _adb.SetupNetworkBridge();
        }
    }

    private async Task OnClipboardChanged()
    {
        if (ClipboardToggle.IsChecked == true)
        {
            if (!await _adb.IsDeviceConnected())
            {
                ClipboardToggle.IsChecked = false;
                return;
            }

            await _adb.SetupClipboardBridge();
        }
    }

    public void UpdateTrayIcon(string uploadSpeed, string downloadSpeed)
    {
        // This will be called from App.cs to update tray tooltip
    }
}