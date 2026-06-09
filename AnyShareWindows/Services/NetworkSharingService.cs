using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace AnyShareWindows.Services;

public class NetworkSharingService
{
    private const string WindowsProxy = "127.0.0.1:18888";
    private const int ListenPort = 18888;
    private const int AdbForwardPort = 18889;

    private TcpListener? _listener;
    private CancellationTokenSource? _cts;

    private long _downloadBytes;
    private long _uploadBytes;
    private long _lastDownloadBytes;
    private long _lastUploadBytes;
    private DateTime _lastSpeedTime = DateTime.Now;

    public long CurrentDownloadSpeed { get; private set; }
    public long CurrentUploadSpeed { get; private set; }

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
    }

    public void UpdateSpeed()
    {
        var now = DateTime.Now;
        var seconds = Math.Max((now - _lastSpeedTime).TotalSeconds, 1);

        CurrentDownloadSpeed = (long)((_downloadBytes - _lastDownloadBytes) / seconds);
        CurrentUploadSpeed = (long)((_uploadBytes - _lastUploadBytes) / seconds);

        _lastDownloadBytes = _downloadBytes;
        _lastUploadBytes = _uploadBytes;
        _lastSpeedTime = now;
    }

    public void EnableWindowsProxy()
    {
        StartLocalProxy();

        using var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Internet Settings",
            true
        );

        if (key == null) return;

        key.SetValue("ProxyEnable", 1);
        key.SetValue("ProxyServer", WindowsProxy);
    }

    public void DisableWindowsProxy()
    {
        StopLocalProxy();

        using var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Internet Settings",
            true
        );

        if (key == null) return;

        key.SetValue("ProxyEnable", 0);
        key.DeleteValue("ProxyServer", false);
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