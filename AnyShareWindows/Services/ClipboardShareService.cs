using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AnyShareWindows.Services;

public class ClipboardShareService
{
    private readonly HttpClient _http = new(
        new HttpClientHandler
        {
            UseProxy = false
        })
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    public async Task<bool> SetupAdbBridge()
    {
        if (!await RunAdb("forward tcp:18765 tcp:8765"))
            return false;

        return await RunAdbContains(
            "forward --list",
            "tcp:18765",
            "tcp:8765"
        );
    }

    public async Task<bool> SendWindowsClipboardToAndroid()
    {
        try
        {
            if (!Clipboard.ContainsText())
                return false;

            var text = Clipboard.GetText();

            if (string.IsNullOrWhiteSpace(text))
                return false;

            var encoded = Uri.EscapeDataString(text);

            var json = JsonSerializer.Serialize(
                new
                {
                    text = encoded
                });

            using var response = await _http.PostAsync(
                "http://127.0.0.1:18765/clipboard/windows/send",
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json"));

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ReceiveAndroidClipboardToWindows()
    {
        try
        {
            string? response = null;

            for (var attempt = 0; attempt < 3 && response == null; attempt++)
            {
                try
                {
                    response = await _http.GetStringAsync(
                        "http://127.0.0.1:18765/clipboard/android/last");
                }
                catch when (attempt < 2)
                {
                    await Task.Delay(250);
                }
            }

            if (response == null)
                return false;

            using var doc = JsonDocument.Parse(response);

            if (!doc.RootElement.TryGetProperty("text", out var textProperty))
                return false;

            var encoded = textProperty.GetString() ?? "";
            var text = Uri.UnescapeDataString(encoded);

            if (string.IsNullOrWhiteSpace(text))
                return false;

            Clipboard.SetText(text);
            return true;
        }
        catch
        {
            return false;
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

            if (process == null)
                return false;

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(true); } catch { }
                return false;
            }

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> RunAdbContains(
        string args,
        params string[] expectedValues)
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
                return false;

            var outputTask = process.StandardOutput.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(true); } catch { }
                return false;
            }

            if (process.ExitCode != 0)
                return false;

            var output = await outputTask;
            return expectedValues.All(value =>
                output.Contains(value, StringComparison.Ordinal));
        }
        catch
        {
            return false;
        }
    }
}
