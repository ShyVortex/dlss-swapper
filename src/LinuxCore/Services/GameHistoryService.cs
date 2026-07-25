using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace DLSS_Swapper.Core.Services;

public class GameHistoryItem
{
    public string EventTime { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
}

public class GameHistoryService
{
    public static string StorageFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DLSS Swapper");

    private static string GetHistoryDirectory()
    {
        var dir = Path.Combine(StorageFolder, "history");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static string GetHistoryFilePath(string gameId)
    {
        var safeId = string.Join("_", gameId.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(GetHistoryDirectory(), $"{safeId}.json");
    }

    public List<GameHistoryItem> LoadHistory(string gameId)
    {
        if (string.IsNullOrEmpty(gameId)) return new List<GameHistoryItem>();

        try
        {
            var path = GetHistoryFilePath(gameId);
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var items = JsonSerializer.Deserialize<List<GameHistoryItem>>(json);
                if (items != null)
                {
                    return items.OrderByDescending(x => x.EventTime).ToList();
                }
            }
        }
        catch
        {
        }

        return new List<GameHistoryItem>();
    }

    public void AddEvent(string gameId, string eventType, string assetType, string version)
    {
        if (string.IsNullOrEmpty(gameId)) return;

        try
        {
            var history = LoadHistory(gameId);

            // Avoid logging exact duplicates back-to-back within the same second
            var timeStr = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            var duplicate = history.FirstOrDefault(h => h.EventTime == timeStr && h.EventType == eventType && h.AssetType == assetType && h.Version == version);
            if (duplicate != null) return;

            history.Insert(0, new GameHistoryItem
            {
                EventTime = timeStr,
                EventType = eventType,
                AssetType = assetType,
                Version = version
            });

            var json = JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(GetHistoryFilePath(gameId), json);
        }
        catch
        {
        }
    }

    public void LogDetectedDlls(string gameId, string assetType, string version)
    {
        if (string.IsNullOrEmpty(version) || version == "N/A" || version == "Not found") return;
        AddEvent(gameId, "DLL detected", assetType, version);
    }
}
