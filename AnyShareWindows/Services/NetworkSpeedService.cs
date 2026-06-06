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

    public string GetCurrentSpeed()
    {
        var interfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n =>
                n.OperationalStatus == OperationalStatus.Up &&
                n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .ToList();

        long received = 0;
        long sent = 0;

        foreach (var item in interfaces)
        {
            var stats = item.GetIPv4Statistics();
            received += stats.BytesReceived;
            sent += stats.BytesSent;
        }

        var now = DateTime.Now;
        var seconds = Math.Max((now - _lastTime).TotalSeconds, 1);

        var diff = Math.Max((received - _lastReceived) + (sent - _lastSent), 0);
        TodayBytes += diff;

        _lastReceived = received;
        _lastSent = sent;
        _lastTime = now;

        return FormatSpeed(diff / seconds);
    }

    public string GetTodayUsage()
    {
        return FormatBytes(TodayBytes);
    }

    private static string FormatSpeed(double bytesPerSecond)
    {
        if (bytesPerSecond >= 1024 * 1024)
            return $"{bytesPerSecond / 1024 / 1024:0.0} MB/s";

        if (bytesPerSecond >= 1024)
            return $"{bytesPerSecond / 1024:0} KB/s";

        return $"{bytesPerSecond:0} B/s";
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes >= 1024L * 1024L * 1024L)
            return $"{bytes / 1024.0 / 1024.0 / 1024.0:0.00} GB";

        if (bytes >= 1024L * 1024L)
            return $"{bytes / 1024.0 / 1024.0:0.00} MB";

        if (bytes >= 1024L)
            return $"{bytes / 1024.0:0.0} KB";

        return $"{bytes} B";
    }
}