using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace AnyShareWindows.Services;

public class NetworkSharingService
{
    private const int SmoothingWindowSize = 3;
    private const string WindowsProxy = "127.0.0.1:18888";
    private const string InternetSettingsPath =
        @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";
    private const string ProxyBackupPath = @"Software\AnyShare\ProxyBackup";
    private const int ListenPort = 18888;
    private const int AdbForwardPort = 18889;

    [DllImport("wininet.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);

    private const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
    private const int INTERNET_OPTION_REFRESH = 37;

    private static void RefreshProxySettings()
    {
        InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
        InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
    }

    public static async Task<bool> TestTunnelConnection()
    {
        try
        {
            using var client = new TcpClient();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));

            await client.ConnectAsync(
                IPAddress.Loopback,
                AdbForwardPort,
                timeout.Token
            );

            await Task.Delay(150, timeout.Token);

            var closedByRemote =
                client.Client.Poll(0, SelectMode.SelectRead) &&
                client.Client.Available == 0;

            return client.Connected && !closedByRemote;
        }
        catch
        {
            return false;
        }
    }

    private TcpListener? _listener;
    private CancellationTokenSource? _cts;

    private long _downloadBytes;
    private long _uploadBytes;
    private long _lastDownloadBytes;
    private long _lastUploadBytes;
    private DateTime _lastSpeedTime = DateTime.Now;
    private readonly System.Collections.Generic.Queue<(long Download, long Upload)>
        _speedSamples = new();

    public long CurrentDownloadSpeed { get; private set; }
    public long CurrentUploadSpeed { get; private set; }

    public bool IsProxyRunning => _listener != null;

    public void StartLocalProxy()
    {
        if (_listener != null) return;

        _cts = new CancellationTokenSource();
        _listener = new TcpListener(IPAddress.Loopback, ListenPort);
        _listener.Start();

        _ = AcceptLoop(_cts.Token);
    }

    public void StopLocalProxy()
    {
        try { _cts?.Cancel(); } catch { }
        try { _listener?.Stop(); } catch { }

        _listener = null;
        _cts = null;

        CurrentDownloadSpeed = 0;
        CurrentUploadSpeed = 0;

        _downloadBytes = 0;
        _uploadBytes = 0;
        _lastDownloadBytes = 0;
        _lastUploadBytes = 0;
        _lastSpeedTime = DateTime.Now;
        _speedSamples.Clear();
    }

    public void UpdateSpeed()
    {
        var now = DateTime.Now;
        var seconds = Math.Max((now - _lastSpeedTime).TotalSeconds, 1);

        var download = (long)((_downloadBytes - _lastDownloadBytes) / seconds);
        var upload = (long)((_uploadBytes - _lastUploadBytes) / seconds);

        _speedSamples.Enqueue((download, upload));

        while (_speedSamples.Count > SmoothingWindowSize)
            _speedSamples.Dequeue();

        CurrentDownloadSpeed = (long)_speedSamples.Average(sample => sample.Download);
        CurrentUploadSpeed = (long)_speedSamples.Average(sample => sample.Upload);

        _lastDownloadBytes = _downloadBytes;
        _lastUploadBytes = _uploadBytes;
        _lastSpeedTime = now;
    }

    public void EnableWindowsProxy()
    {
        StartLocalProxy();

        using var key = Registry.CurrentUser.OpenSubKey(
            InternetSettingsPath,
            true
        );

        if (key == null) return;

        CaptureProxySettings(key);

        key.SetValue("ProxyEnable", 1);
        key.SetValue("ProxyServer", WindowsProxy);
        key.SetValue("ProxyOverride", "<local>;localhost;127.*");

        RefreshProxySettings();
    }

    public void DisableWindowsProxy()
    {
        StopLocalProxy();

        using var key = Registry.CurrentUser.OpenSubKey(
            InternetSettingsPath,
            true
        );

        if (key == null) return;

        if (!RestoreProxySettings(key))
        {
            var currentProxy = key.GetValue("ProxyServer") as string;

            if (!string.Equals(currentProxy, WindowsProxy, StringComparison.OrdinalIgnoreCase))
                return;

            key.SetValue("ProxyEnable", 0);
            key.DeleteValue("ProxyServer", false);
        }

        RefreshProxySettings();
    }

    private static void CaptureProxySettings(RegistryKey internetSettings)
    {
        using var backup = Registry.CurrentUser.CreateSubKey(ProxyBackupPath, true);
        if (backup == null || backup.GetValue("Captured") is not null)
            return;

        BackupValue(internetSettings, backup, "ProxyEnable");
        BackupValue(internetSettings, backup, "ProxyServer");
        BackupValue(internetSettings, backup, "ProxyOverride");
        BackupValue(internetSettings, backup, "AutoConfigURL");
        backup.SetValue("Captured", 1, RegistryValueKind.DWord);
    }

    private static bool RestoreProxySettings(RegistryKey internetSettings)
    {
        using var backup = Registry.CurrentUser.OpenSubKey(ProxyBackupPath, false);
        if (backup?.GetValue("Captured") is null)
            return false;

        RestoreValue(internetSettings, backup, "ProxyEnable");
        RestoreValue(internetSettings, backup, "ProxyServer");
        RestoreValue(internetSettings, backup, "ProxyOverride");
        RestoreValue(internetSettings, backup, "AutoConfigURL");

        backup.Close();
        Registry.CurrentUser.DeleteSubKeyTree(ProxyBackupPath, false);
        return true;
    }

    private static void BackupValue(
        RegistryKey source,
        RegistryKey backup,
        string valueName)
    {
        var value = source.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
        backup.SetValue($"{valueName}Exists", value is null ? 0 : 1, RegistryValueKind.DWord);

        if (value is not null)
            backup.SetValue(valueName, value, source.GetValueKind(valueName));
    }

    private static void RestoreValue(
        RegistryKey target,
        RegistryKey backup,
        string valueName)
    {
        var existed = Convert.ToInt32(backup.GetValue($"{valueName}Exists", 0)) == 1;

        if (!existed)
        {
            target.DeleteValue(valueName, false);
            return;
        }

        var value = backup.GetValue(
            valueName,
            null,
            RegistryValueOptions.DoNotExpandEnvironmentNames
        );

        if (value is not null)
            target.SetValue(valueName, value, backup.GetValueKind(valueName));
    }

    private async Task AcceptLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested && _listener != null)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync(token);
                _ = HandleClient(client, token);
            }
            catch
            {
            }
        }
    }

    private async Task HandleClient(TcpClient client, CancellationToken token)
    {
        TcpClient? adb = null;

        try
        {
            adb = new TcpClient();
            await adb.ConnectAsync(IPAddress.Loopback, AdbForwardPort, token);

            var clientStream = client.GetStream();
            var adbStream = adb.GetStream();

            var uploadTask = Pipe(clientStream, adbStream, true, token);
            var downloadTask = Pipe(adbStream, clientStream, false, token);

            await Task.WhenAny(uploadTask, downloadTask);
        }
        catch
        {
        }
        finally
        {
            try { client.Close(); } catch { }
            try { adb?.Close(); } catch { }
        }
    }

    private async Task Pipe(NetworkStream input, NetworkStream output, bool upload, CancellationToken token)
    {
        var buffer = new byte[32 * 1024];

        while (!token.IsCancellationRequested)
        {
            var read = await input.ReadAsync(buffer, token);
            if (read <= 0) break;

            await output.WriteAsync(buffer.AsMemory(0, read), token);
            await output.FlushAsync(token);

            if (upload)
                _uploadBytes += read;
            else
                _downloadBytes += read;
        }
    }
}
