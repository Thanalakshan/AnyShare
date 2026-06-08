using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace AnyShareWindows.Services;

public class SettingsService
{
    private readonly string _settingsPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public SettingsService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AnyShare"
        );

        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }

        _settingsPath = Path.Combine(appDataPath, "settings.json");
        _jsonOptions = new JsonSerializerOptions { WriteIndented = true };
    }

    /// <summary>
    /// Load settings from disk, or return defaults if not found
    /// </summary>
    public AppState LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<AppState>(json, _jsonOptions);
                return settings ?? GetDefaultSettings();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
        }

        return GetDefaultSettings();
    }

    /// <summary>
    /// Save settings to disk (synchronous)
    /// </summary>
    public void SaveSettings(AppState settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Save settings to disk (asynchronous)
    /// </summary>
    public async Task SaveSettingsAsync(AppState settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            await File.WriteAllTextAsync(_settingsPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
        }
    }

    private AppState GetDefaultSettings()
    {
        return new AppState
        {
            NetworkSpeedMonitor = false,
            NetworkSharing = false,
            ClipboardSharing = false,
            OpenAtStartup = false,
            CurrentSpeed = "0 KB/s",
            TodayUsage = "0 MB"
        };
    }
}
