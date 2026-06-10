using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace AnyShareWindows.Services;

public class NetworkSpeedService
{
    private long _lastReceived;
    private long _lastSent;
    private DateTime _lastTime = DateTime.Now;

    private bool _baselineSet;
    private bool _externalMode = false;

    public long TodayBytes { get; private set; }
    public long TodayDownloadBytes { get; private set; }
    public long TodayUploadBytes { get; private set; }

    public long CurrentDownloadSpeed { get; private set; }
    public long CurrentUploadSpeed { get; private set; }

    public string NetworkSource { get; private set; } = "Unknown";

    public event Action? Updated;

    public string GetCurrentSpeed()
    {
        if (_externalMode)
        {
            var extNow = DateTime.Now;
            var extSeconds = Math.Max((extNow - _lastTime).TotalSeconds, 0.001);

            TodayDownloadBytes += (long)(CurrentDownloadSpeed * extSeconds);
            TodayUploadBytes += (long)(CurrentUploadSpeed * extSeconds);
            TodayBytes = TodayDownloadBytes + TodayUploadBytes;

            _lastTime = extNow;

            var externalTotal = FormatSpeed(CurrentDownloadSpeed + CurrentUploadSpeed);
            Updated?.Invoke();
            return externalTotal;
        }

        var interfaces = GetActiveInterfaces();

        long received = 0;
        long sent = 0;

        foreach (var item in interfaces)
        {
            var stats = item.GetIPv4Statistics();
            received += stats.BytesReceived;
            sent += stats.BytesSent;
        }

        NetworkSource = DetectNetworkSource();

        var now = DateTime.Now;

        if (!_baselineSet)
        {
            _lastReceived = received;
            _lastSent = sent;
            _lastTime = now;
            _baselineSet = true;
            CurrentDownloadSpeed = 0;
            CurrentUploadSpeed = 0;
            var baselineTotal = FormatSpeed(0);
            Updated?.Invoke();
            return baselineTotal;
        }

        var seconds = Math.Max((now - _lastTime).TotalSeconds, 0.001);

        var diffReceived = Math.Max(received - _lastReceived, 0);
        var diffSent = Math.Max(sent - _lastSent, 0);

        TodayDownloadBytes += diffReceived;
        TodayUploadBytes += diffSent;
        TodayBytes = TodayDownloadBytes + TodayUploadBytes;

        CurrentDownloadSpeed = (long)(diffReceived / seconds);
        CurrentUploadSpeed = (long)(diffSent / seconds);

        _lastReceived = received;
        _lastSent = sent;
        _lastTime = now;

        var total = FormatSpeed(CurrentDownloadSpeed + CurrentUploadSpeed);
        Updated?.Invoke();
        return total;
    }

    public void SetExternalSpeed(
        long downloadBytesPerSecond,
        long uploadBytesPerSecond,
        string source)
    {
        _externalMode = true;
        CurrentDownloadSpeed = downloadBytesPerSecond;
        CurrentUploadSpeed = uploadBytesPerSecond;
        NetworkSource = source;
    }

    public void ClearExternalSpeed()
    {
        if (!_externalMode)
            return;

        _externalMode = false;
        _baselineSet = false;
        CurrentDownloadSpeed = 0;
        CurrentUploadSpeed = 0;
        NetworkSource = "Not connected";
    }

    private static string DetectNetworkSource()
    {
        var connected = GetConnectedInterfaces();

        if (connected.Any(i => i.NetworkInterfaceType == NetworkInterfaceType.Ethernet))
            return "Ethernet";

        if (connected.Any(i =>
                i.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                IsLikelyWifiAdapter(i)))
            return "WiFi";

        return "Not connected";
    }

    private static List<NetworkInterface> GetActiveInterfaces()
    {
        return GetConnectedInterfaces();
    }

    private static List<NetworkInterface> GetConnectedInterfaces()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(n =>
                n.OperationalStatus == OperationalStatus.Up &&
                n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                n.NetworkInterfaceType != NetworkInterfaceType.Tunnel &&
                HasIpv4Connectivity(n))
            .ToList();
    }

    private static bool HasIpv4Connectivity(NetworkInterface networkInterface)
    {
        var properties = networkInterface.GetIPProperties();

        if (properties.GatewayAddresses.Any(g =>
                g.Address.AddressFamily == AddressFamily.InterNetwork))
        {
            return true;
        }

        return properties.UnicastAddresses.Any(a =>
            a.Address.AddressFamily == AddressFamily.InterNetwork &&
            HasUsableIpv4Address(a.Address));
    }

    private static bool HasUsableIpv4Address(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
            return false;

        var bytes = address.GetAddressBytes();

        // Exclude APIPA / link-local auto addresses.
        return !(bytes[0] == 169 && bytes[1] == 254);
    }

    private static bool IsLikelyWifiAdapter(NetworkInterface networkInterface)
    {
        var description = networkInterface.Description;

        return description.Contains("Wi-Fi", StringComparison.OrdinalIgnoreCase) ||
               description.Contains("WiFi", StringComparison.OrdinalIgnoreCase) ||
               description.Contains("Wireless", StringComparison.OrdinalIgnoreCase) ||
               description.Contains("802.11", StringComparison.OrdinalIgnoreCase);
    }

    public string GetCurrentDownloadSpeed()
    {
        return FormatSpeed(CurrentDownloadSpeed);
    }

    public string GetCurrentUploadSpeed()
    {
        return FormatSpeed(CurrentUploadSpeed);
    }

    public string GetTodayDownloadUsage()
    {
        return FormatBytes(TodayDownloadBytes);
    }

    public string GetTodayUploadUsage()
    {
        return FormatBytes(TodayUploadBytes);
    }

    public string GetTodayUsage()
    {
        return FormatBytes(TodayBytes);
    }

    public void ResetTodayUsage()
    {
        TodayBytes = 0;
        TodayDownloadBytes = 0;
        TodayUploadBytes = 0;

        var interfaces = GetActiveInterfaces();

        _lastReceived = interfaces.Sum(i => i.GetIPv4Statistics().BytesReceived);
        _lastSent = interfaces.Sum(i => i.GetIPv4Statistics().BytesSent);
        _lastTime = DateTime.Now;
        _baselineSet = true;
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
}