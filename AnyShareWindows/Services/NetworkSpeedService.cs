using System;
using System.Linq;
using System.Net.NetworkInformation;

namespace AnyShareWindows.Services;

public class NetworkSpeedService
{
    private long _lastReceived;
    private long _lastSent;
    private DateTime _lastTime = DateTime.Now;

    public long TodayBytes { get; private set; }
    public long CurrentDownloadSpeed { get; private set; }
    public long CurrentUploadSpeed { get; private set; }
    public string NetworkSource { get; private set; } = "Unknown";
    public long TodayDownloadBytes { get; private set; }
    public long TodayUploadBytes { get; private set; }

    public string GetCurrentSpeed()
    {
        var interfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n =>
                n.OperationalStatus == OperationalStatus.Up &&
                n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                n.NetworkInterfaceType != NetworkInterfaceType.Tunnel &&
                n.GetIPv4Statistics().BytesReceived > 0)
            .ToList();

        long received = 0;
        long sent = 0;
        NetworkSource = "Not connected";

        foreach (var item in interfaces)
        {
            var stats = item.GetIPv4Statistics();

            received += stats.BytesReceived;
            sent += stats.BytesSent;

            var properties = item.GetIPProperties();
            var hasGateway = properties.GatewayAddresses.Any(g =>
                g.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

            if (!hasGateway)
                continue;

            if (item.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                NetworkSource = "WiFi";
            else if (item.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                NetworkSource = "Ethernet";
        }

        var now = DateTime.Now;
        var seconds = Math.Max((now - _lastTime).TotalSeconds, 0.001);

        var diffReceived = Math.Max(received - _lastReceived, 0);
        var diffSent = Math.Max(sent - _lastSent, 0);
        var diffTotal = diffReceived + diffSent;

        TodayDownloadBytes += diffReceived;
        TodayUploadBytes += diffSent;
        TodayBytes = TodayDownloadBytes + TodayUploadBytes;

        // Windows NetworkInterface gives BYTES, not bits.
        CurrentDownloadSpeed = (long)(diffReceived / seconds);
        CurrentUploadSpeed = (long)(diffSent / seconds);

        _lastReceived = received;
        _lastSent = sent;
        _lastTime = now;

        return FormatSpeed(CurrentDownloadSpeed + CurrentUploadSpeed);
    }

    public string GetTodayDownloadUsage()
    {
        return FormatBytes(TodayDownloadBytes);
    }

    public string GetTodayUploadUsage()
    {
        return FormatBytes(TodayUploadBytes);
    }

    public string GetCurrentDownloadSpeed()
    {
        return FormatSpeed(CurrentDownloadSpeed);
    }

    public string GetCurrentUploadSpeed()
    {
        return FormatSpeed(CurrentUploadSpeed);
    }

    public string GetTodayUsage()
    {
        return FormatBytes(TodayBytes);
    }

    public static string FormatSpeed(double bytesPerSecond)
    {
        const double KB = 1024.0;
        const double MB = KB * 1024.0;
        const double GB = MB * 1024.0;

        if (bytesPerSecond >= GB)
        {
            return $"{bytesPerSecond / GB:0.0} GB/s";
        }

        if (bytesPerSecond >= MB)
        {
            var mb = bytesPerSecond / MB;

            if (mb >= 10)
                return $"{Math.Round(mb):0} MB/s";

            return $"{mb:0.0} MB/s";
        }

        var kb = bytesPerSecond / KB;

        return $"{Math.Round(kb):0} KB/s";
    }

    public static string FormatBytes(long bytes)
    {
        const double KB = 1024.0;
        const double MB = KB * 1024.0;
        const double GB = MB * 1024.0;

        if (bytes >= GB)
            return $"{bytes / GB:0.00} GB";

        if (bytes >= MB)
            return $"{bytes / MB:0.00} MB";

        if (bytes >= KB)
            return $"{bytes / KB:0.0} KB";

        return $"{bytes} B";
    }

    public void ResetTodayUsage()
    {
        TodayBytes = 0;
        TodayDownloadBytes = 0;
        TodayUploadBytes = 0;

        var interfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n =>
                n.OperationalStatus == OperationalStatus.Up &&
                n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                n.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
            .ToList();

        _lastReceived = interfaces.Sum(i => i.GetIPv4Statistics().BytesReceived);
        _lastSent = interfaces.Sum(i => i.GetIPv4Statistics().BytesSent);
        _lastTime = DateTime.Now;
    }
}