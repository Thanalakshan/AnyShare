using System;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

namespace AnyShareWindows.Services;

public class AdbService
{
    public async Task<bool> IsDeviceConnected()
    {
        var output = await RunAdb("devices");
        return output.Split('\n').Any(line => line.Contains("\tdevice"));
    }

    public async Task SetupClipboardBridge()
    {
        await RunAdb("forward tcp:18765 tcp:8765");
    }

    public async Task SetupNetworkBridge()
    {
        await RunAdb("forward tcp:18888 tcp:8888");
    }

    private static async Task<string> RunAdb(string args)
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
        if (process == null) return "";

        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        return output;
    }
}