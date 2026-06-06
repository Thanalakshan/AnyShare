using System;
using Microsoft.Win32;

namespace AnyShareWindows.Services;

public class StartupService
{
    private const string AppName = "AnyShare";

    public void SetStartup(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Run",
            true);

        if (key == null) return;

        if (enabled)
        {
            key.SetValue(AppName, Environment.ProcessPath ?? "");
        }
        else
        {
            key.DeleteValue(AppName, false);
        }
    }
}