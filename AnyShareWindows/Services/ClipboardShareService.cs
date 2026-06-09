using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AnyShareWindows.Services;

public class ClipboardShareService
{
    private readonly HttpClient _http = new();

    public async Task<bool> SetupAdbBridge()
    {
        return await RunAdb("forward tcp:18765 tcp:8765");
    }

    public async Task SendWindowsClipboardToAndroid()
    {
        if (!Clipboard.ContainsText()) return;

        var text = Clipboard.GetText();
        if (string.IsNullOrWhiteSpace(text)) return;

        var encoded = Uri.EscapeDataString(text);
        var json = $"{{\"text\":\"{encoded}\"}}";

        await _http.PostAsync(
            "http://127.0.0.1:18765/clipboard/windows/send",
            new StringContent(json, Encoding.UTF8, "application/json")
        );
    }

    public async Task ReceiveAndroidClipboardToWindows()
    {
        var response = await _http.GetStringAsync(
            "http://127.0.0.1:18765/clipboard/android/last"
        );

        using var doc = JsonDocument.Parse(response);
        var encoded = doc.RootElement.GetProperty("text").GetString() ?? "";
        var text = Uri.UnescapeDataString(encoded);

        if (!string.IsNullOrWhiteSpace(text))
        {
            Clipboard.SetText(text);
        }
    }

    private static async Task<bool> RunAdb(string args)
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
            if (process == null) return false;

            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}