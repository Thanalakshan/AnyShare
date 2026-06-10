using System;
using System.IO;
using Microsoft.Win32;

namespace AnyShareWindows.Services;

public class StartupService
{
    private const string AppName = "AnyShare";

#pragma warning disable CA1416
    public bool IsStartupEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Run",
            false);

        return key?.GetValue(AppName) is string;
    }

    public void SetStartup(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Run",
            true);

        if (key == null) return;

        if (enabled)
        {
            var executablePath = GetExecutablePath();

            if (string.IsNullOrWhiteSpace(executablePath))
                return;

            key.SetValue(AppName, $"\"{executablePath}\"");
        }
        else
        {
            key.DeleteValue(AppName, false);
        }
    }

    private static string GetExecutablePath()
    {
        if (!string.IsNullOrWhiteSpace(Environment.ProcessPath))
            return Environment.ProcessPath;

        var commandLinePath = Environment.GetCommandLineArgs()[0];

        if (string.IsNullOrWhiteSpace(commandLinePath))
            return "";

        return Path.GetFullPath(commandLinePath);
    }
#pragma warning restore CA1416
}