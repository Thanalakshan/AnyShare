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

    private readonly DispatcherTimer _timer = new();

    public MainWindow()
    {
        InitializeComponent();

        SpeedToggle.IsCheckedChanged += (_, _) => { };
        SharingToggle.IsCheckedChanged += async (_, _) => await OnNetworkSharingChanged();
        ClipboardToggle.IsCheckedChanged += async (_, _) => await OnClipboardChanged();
        StartupToggle.IsCheckedChanged += (_, _) =>
        {
            _startup.SetStartup(StartupToggle.IsChecked == true);
        };

        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (_, _) =>
        {
            if (SpeedToggle.IsChecked == true)
            {
                var speed = _networkSpeed.GetCurrentSpeed();
                SpeedText.Text = $"Speed: {speed}";
                UsageText.Text = $"Usage: {_networkSpeed.GetTodayUsage()}";
            }
        };
        _timer.Start();
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
}