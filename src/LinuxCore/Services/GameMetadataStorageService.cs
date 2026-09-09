using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DLSS_Swapper.Core.Services;

public class GameMetadataStorageService
{
    public static string StorageFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DLSS Swapper");

    private static string FavouritesFilePath => Path.Combine(StorageFolder, "favourites.json");

    public GameMetadataStorageService()
    {
        Directory.CreateDirectory(StorageFolder);
    }

    public HashSet<string> LoadFavourites()
    {
        try
        {
            if (File.Exists(FavouritesFilePath))
            {
                var json = File.ReadAllText(FavouritesFilePath);
                var list = JsonSerializer.Deserialize<List<string>>(json);
                if (list != null)
                {
                    return new HashSet<string>(list, StringComparer.OrdinalIgnoreCase);
                }
            }
        }
        catch
        {
        }
        return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    public void SaveFavourites(IEnumerable<string> favouriteIds)
    {
        try
        {
            var list = new List<string>(favouriteIds);
            var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FavouritesFilePath, json);
        }
        catch
        {
        }
    }

    private static string GetNotesDirectory()
    {
        var dir = Path.Combine(StorageFolder, "notes");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static string GetNoteFilePath(string gameId)
    {
        // Sanitize filename
        var safeId = string.Join("_", gameId.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(GetNotesDirectory(), $"{safeId}.txt");
    }

    public string LoadNote(string gameId)
    {
        if (string.IsNullOrEmpty(gameId)) return string.Empty;
        try
        {
            var p = GetNoteFilePath(gameId);
            if (File.Exists(p))
            {
                return File.ReadAllText(p);
            }
        }
        catch
        {
        }
        return string.Empty;
    }

    public void SaveNote(string gameId, string noteText)
    {
        if (string.IsNullOrEmpty(gameId)) return;
        try
        {
            var p = GetNoteFilePath(gameId);
            if (string.IsNullOrWhiteSpace(noteText))
            {
                if (File.Exists(p)) File.Delete(p);
            }
            else
            {
                File.WriteAllText(p, noteText);
            }
        }
        catch
        {
        }
    }

    public void DeleteGameMetadata(string gameId)
    {
        if (string.IsNullOrEmpty(gameId)) return;
        try
        {
            var noteFile = GetNoteFilePath(gameId);
            if (File.Exists(noteFile))
            {
                File.Delete(noteFile);
            }
        }
        catch
        {
        }
    }

    private static string ManualGamesFilePath => Path.Combine(StorageFolder, "manual_games.json");

    public List<ManualGameRecord> LoadManualGames()
    {
        try
        {
            if (File.Exists(ManualGamesFilePath))
            {
                var json = File.ReadAllText(ManualGamesFilePath);
                var list = JsonSerializer.Deserialize<List<ManualGameRecord>>(json);
                if (list != null)
                {
                    return list;
                }
            }
        }
        catch
        {
        }
        return new List<ManualGameRecord>();
    }

    public void SaveManualGames(IEnumerable<ManualGameRecord> games)
    {
        try
        {
            var list = new List<ManualGameRecord>(games);
            var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ManualGamesFilePath, json);
        }
        catch
        {
        }
    }

    public void AddManualGame(ManualGameRecord game)
    {
        if (string.IsNullOrEmpty(game.InstallPath)) return;
        var list = LoadManualGames();
        list.RemoveAll(x => string.Equals(x.InstallPath, game.InstallPath, StringComparison.OrdinalIgnoreCase));
        list.Add(game);
        SaveManualGames(list);
    }

    public void AddManualGame(string name, string installPath, string? coverImagePath = null)
    {
        AddManualGame(new ManualGameRecord
        {
            Name = name,
            InstallPath = installPath,
            CoverImagePath = coverImagePath
        });
    }

    public void RemoveManualGame(string installPath)
    {
        if (string.IsNullOrEmpty(installPath)) return;
        var list = LoadManualGames();
        list.RemoveAll(x => string.Equals(x.InstallPath, installPath, StringComparison.OrdinalIgnoreCase));
        SaveManualGames(list);
    }
    private static string ScannedGamesCacheFilePath => Path.Combine(StorageFolder, "games_cache.json");

    public List<ScannedGameCacheEntry> LoadScannedGamesCache()
    {
        try
        {
            if (File.Exists(ScannedGamesCacheFilePath))
            {
                var json = File.ReadAllText(ScannedGamesCacheFilePath);
                var list = JsonSerializer.Deserialize<List<ScannedGameCacheEntry>>(json);
                if (list != null)
                {
                    return list;
                }
            }
        }
        catch
        {
        }
        return new List<ScannedGameCacheEntry>();
    }

    public void SaveScannedGamesCache(IEnumerable<ScannedGameCacheEntry> entries)
    {
        try
        {
            var list = new List<ScannedGameCacheEntry>(entries);
            var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ScannedGamesCacheFilePath, json);
        }
        catch
        {
        }
    }
}

public class ScannedGameCacheEntry
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Launcher { get; set; } = string.Empty;
    public string InstallPath { get; set; } = string.Empty;
    public string ManifestPath { get; set; } = string.Empty;
    public long ManifestLastWriteTimeUtcTicks { get; set; }
    public string? CoverImagePath { get; set; }
    public string? CoverColorHex { get; set; }
    public bool IsManualGame { get; set; }
    public Dictionary<string, string> DllMap { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class ManualGameRecord
{
    public string Name { get; set; } = string.Empty;
    public string InstallPath { get; set; } = string.Empty;
    public string? CoverImagePath { get; set; }
}
