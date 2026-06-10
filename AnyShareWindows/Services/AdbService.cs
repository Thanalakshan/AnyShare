using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace AnyShareWindows.Services;

public class AdbService
{
    public async Task<bool> IsDeviceConnected()
    {
        var output = await RunAdb("devices");

        return output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Any(line =>
                line.Contains("\tdevice") &&
                !line.StartsWith("List of devices"));
    }

    public async Task<bool> SetupClipboardBridge()
    {
        await RunAdb("forward tcp:18765 tcp:8765");
        return true;
    }

    public async Task<bool> RemoveClipboardBridge()
    {
        await RunAdb("forward --remove tcp:18765");
        return true;
    }

    public async Task<bool> IsNetworkBridgeForwarded()
    {
        var output = await RunAdb("forward --list");

        return output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Any(line =>
                line.Contains("tcp:18889", StringComparison.Ordinal) &&
                line.Contains("tcp:8888", StringComparison.Ordinal));
    }

    public async Task<bool> IsAndroidNetworkProxyRunning()
    {
        var output = await RunAdb("shell dumpsys activity services");

        return output.Contains("NetworkProxyService", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<bool> SetupNetworkBridge()
    {
        await RunAdb("forward tcp:18889 tcp:8888");
        return true;
    }

    public async Task<bool> RemoveNetworkBridge()
    {
        await RunAdb("forward --remove tcp:18889");
        return true;
    }

    public async Task<bool> RemoveAllForwards()
    {
        await RunAdb("forward --remove-all");
        return true;
    }

    public async Task<bool> RestartServer()
    {
        await RunAdb("kill-server");
        await RunAdb("start-server");
        return true;
    }

    public async Task<string> GetDeviceName()
    {
        var name = await RunAdb("shell getprop ro.product.model");

        return string.IsNullOrWhiteSpace(name)
            ? "Android Device"
            : name.Trim();
    }

    public async Task<bool> IsVpnActive()
    {
        var output = await RunAdb("shell dumpsys connectivity");

        return output.Contains("VPN", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string> RunAdb(string args)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "adb",
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);

            if (process == null)
                return "";

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            return string.IsNullOrWhiteSpace(output) ? error : output;
        }
        catch
        {
            return "";
        }
    }
}