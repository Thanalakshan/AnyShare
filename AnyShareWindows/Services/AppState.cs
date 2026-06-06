namespace AnyShareWindows.Services;

public class AppState
{
    public bool NetworkSpeedMonitor { get; set; }
    public bool NetworkSharing { get; set; }
    public bool ClipboardSharing { get; set; }
    public bool OpenAtStartup { get; set; }

    public string CurrentSpeed { get; set; } = "0 KB/s";
    public string TodayUsage { get; set; } = "0 MB";
}