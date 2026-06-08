using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AnyShareWindows.Services;

public class DailyUsageRecord
{
    public DateTime Date { get; set; }
    public long BytesUsed { get; set; }
    public string NetworkSource { get; set; } = "Unknown"; // "WiFi", "Ethernet", "AnyShare"
}

public class UsageHistoryService
{
    private readonly string _historyPath;
    private readonly JsonSerializerOptions _jsonOptions;
    private List<DailyUsageRecord> _history = new();

    public UsageHistoryService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AnyShare"
        );

        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }

        _historyPath = Path.Combine(appDataPath, "usage_history.json");
        _jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        LoadHistory();
    }

    /// <summary>
    /// Load usage history from disk
    /// </summary>
    private void LoadHistory()
    {
        try
        {
            if (File.Exists(_historyPath))
            {
                var json = File.ReadAllText(_historyPath);
                _history = JsonSerializer.Deserialize<List<DailyUsageRecord>>(json, _jsonOptions) ?? new();
                // Remove records older than 7 days
                var sevenDaysAgo = DateTime.Now.AddDays(-7);
                _history = _history.Where(r => r.Date >= sevenDaysAgo).ToList();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading history: {ex.Message}");
            _history = new();
        }
    }

    /// <summary>
    /// Get today's usage record or create new one
    /// </summary>
    public DailyUsageRecord GetTodayRecord()
    {
        var today = DateTime.Now.Date;
        var record = _history.FirstOrDefault(r => r.Date.Date == today);

        if (record == null)
        {
            record = new DailyUsageRecord { Date = today, BytesUsed = 0 };
            _history.Add(record);
        }

        return record;
    }

    /// <summary>
    /// Update today's usage and network source
    /// </summary>
    public void UpdateTodayUsage(long additionalBytes, string networkSource)
    {
        var record = GetTodayRecord();
        record.BytesUsed += additionalBytes;
        record.NetworkSource = networkSource;
        SaveHistory();
    }

    /// <summary>
    /// Get 7-day history (sorted by date, most recent first)
    /// </summary>
    public List<DailyUsageRecord> GetLast7DaysHistory()
    {
        return _history
            .OrderByDescending(r => r.Date)
            .Take(7)
            .ToList();
    }

    /// <summary>
    /// Reset today's usage to zero
    /// </summary>
    public void ResetToday()
    {
        var record = GetTodayRecord();
        record.BytesUsed = 0;
        SaveHistory();
    }

    /// <summary>
    /// Reset all history
    /// </summary>
    public void ResetAll()
    {
        _history.Clear();
        SaveHistory();
    }

    /// <summary>
    /// Save history to disk
    /// </summary>
    private void SaveHistory()
    {
        try
        {
            // Keep only last 7 days
            var sevenDaysAgo = DateTime.Now.AddDays(-7);
            _history = _history.Where(r => r.Date >= sevenDaysAgo).ToList();

            var json = JsonSerializer.Serialize(_history, _jsonOptions);
            File.WriteAllText(_historyPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving history: {ex.Message}");
        }
    }
}
