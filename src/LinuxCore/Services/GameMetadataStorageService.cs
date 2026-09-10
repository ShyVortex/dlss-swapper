using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace DLSS_Swapper.Core.Services;

public class GameMetadataStorageService
{
    public static string StorageFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DLSS Swapper");

    public static string CustomCoversDirectory
    {
        get
        {
            var dir = Path.Combine(StorageFolder, "custom_covers");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return dir;
        }
    }

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
    public const int CurrentCacheVersion = 2;
    private static string ScannedGamesCacheFilePath => Path.Combine(StorageFolder, "games_cache.json");

    public List<ScannedGameCacheEntry> LoadScannedGamesCache()
    {
        return LoadScannedGamesCache(out _);
    }

    public List<ScannedGameCacheEntry> LoadScannedGamesCache(out bool isCacheOutdated)
    {
        isCacheOutdated = false;
        try
        {
            if (File.Exists(ScannedGamesCacheFilePath))
            {
                var json = File.ReadAllText(ScannedGamesCacheFilePath);
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    var version = doc.RootElement.TryGetProperty("Version", out var vProp) ? vProp.GetInt32() : 1;
                    if (version < CurrentCacheVersion)
                    {
                        isCacheOutdated = true;
                    }

                    if (doc.RootElement.TryGetProperty("Entries", out var entriesProp))
                    {
                        var list = JsonSerializer.Deserialize<List<ScannedGameCacheEntry>>(entriesProp.GetRawText());
                        return list ?? new List<ScannedGameCacheEntry>();
                    }
                }
                else if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    // Version 1 legacy format
                    isCacheOutdated = true;
                    var list = JsonSerializer.Deserialize<List<ScannedGameCacheEntry>>(json);
                    return list ?? new List<ScannedGameCacheEntry>();
                }
            }
            else
            {
                isCacheOutdated = true;
            }
        }
        catch
        {
            isCacheOutdated = true;
        }
        return new List<ScannedGameCacheEntry>();
    }

    public void SaveScannedGamesCache(IEnumerable<ScannedGameCacheEntry> entries)
    {
        try
        {
            var container = new ScannedGamesCacheContainer
            {
                Version = CurrentCacheVersion,
                Entries = new List<ScannedGameCacheEntry>(entries)
            };
            var json = JsonSerializer.Serialize(container, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ScannedGamesCacheFilePath, json);
        }
        catch
        {
        }
    }
    
    public static string GetSafeGameId(string gameId)
    {
        return string.Join("_", gameId.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
    }

    public static string? GetCustomCoverPath(string gameId)
    {
        if (string.IsNullOrWhiteSpace(gameId)) return null;

        var safeId = GetSafeGameId(gameId);
        var dir = CustomCoversDirectory;

        var candidates = new[]
        {
            Path.Combine(dir, $"{safeId}_custom_400_600.png"),
            Path.Combine(dir, $"{safeId}.png"),
            Path.Combine(dir, $"{safeId}.jpg"),
            Path.Combine(dir, $"{safeId}.jpeg"),
            Path.Combine(dir, $"{safeId}.webp")
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    public static bool HasCustomCover(string gameId)
    {
        return !string.IsNullOrEmpty(GetCustomCoverPath(gameId));
    }

    public async Task<string> SaveCustomCoverAsync(string gameId, string sourceFilePath)
    {
        if (string.IsNullOrWhiteSpace(gameId)) throw new ArgumentException("gameId cannot be empty", nameof(gameId));
        if (!File.Exists(sourceFilePath)) throw new FileNotFoundException("Source image not found", sourceFilePath);

        var safeId = GetSafeGameId(gameId);
        var destPath = Path.Combine(CustomCoversDirectory, $"{safeId}_custom_400_600.png");

        try
        {
            using var inStream = File.OpenRead(sourceFilePath);
            using var image = await SixLabors.ImageSharp.Image.LoadAsync(inStream).ConfigureAwait(false);
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new SixLabors.ImageSharp.Size(400, 600),
                Mode = ResizeMode.Crop
            }));
            await image.SaveAsPngAsync(destPath).ConfigureAwait(false);
        }
        catch
        {
            // Fallback to direct file copy if ImageSharp cannot decode/process
            File.Copy(sourceFilePath, destPath, true);
        }

        // Clean up any legacy or other extension files for this safeId
        var candidates = new[]
        {
            Path.Combine(CustomCoversDirectory, $"{safeId}.png"),
            Path.Combine(CustomCoversDirectory, $"{safeId}.jpg"),
            Path.Combine(CustomCoversDirectory, $"{safeId}.jpeg"),
            Path.Combine(CustomCoversDirectory, $"{safeId}.webp")
        };
        foreach (var c in candidates)
        {
            if (!string.Equals(c, destPath, StringComparison.OrdinalIgnoreCase) && File.Exists(c))
            {
                try { File.Delete(c); } catch { }
            }
        }

        return destPath;
    }

    public void DeleteCustomCover(string gameId)
    {
        if (string.IsNullOrWhiteSpace(gameId)) return;

        var safeId = GetSafeGameId(gameId);
        var dir = CustomCoversDirectory;
        if (!Directory.Exists(dir)) return;

        try
        {
            var files = Directory.GetFiles(dir, $"{safeId}*");
            foreach (var file in files)
            {
                try { File.Delete(file); } catch { }
            }
        }
        catch
        {
        }
    }

    public void UpdateGameCoverInCache(string gameId, string? newCoverPath, string? defaultCoverPath = null)
    {
        try
        {
            var cache = LoadScannedGamesCache(out _);
            bool updated = false;

            foreach (var entry in cache)
            {
                if (string.Equals(entry.Id, gameId, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(entry.InstallPath, gameId, StringComparison.OrdinalIgnoreCase))
                {
                    entry.CoverImagePath = newCoverPath;
                    if (!string.IsNullOrEmpty(defaultCoverPath))
                    {
                        entry.DefaultCoverImagePath = defaultCoverPath;
                    }
                    updated = true;
                    break;
                }
            }

            if (updated)
            {
                SaveScannedGamesCache(cache);
            }

            // Also update manual games if applicable
            var manualGames = LoadManualGames();
            bool manualUpdated = false;
            foreach (var mg in manualGames)
            {
                if (string.Equals(mg.InstallPath, gameId, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(mg.Name, gameId, StringComparison.OrdinalIgnoreCase))
                {
                    mg.CoverImagePath = newCoverPath;
                    manualUpdated = true;
                    break;
                }
            }

            if (manualUpdated)
            {
                SaveManualGames(manualGames);
            }
        }
        catch
        {
        }
    }
}

public class ScannedGamesCacheContainer
{
    public int Version { get; set; } = GameMetadataStorageService.CurrentCacheVersion;
    public List<ScannedGameCacheEntry> Entries { get; set; } = new();
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
    public string? DefaultCoverImagePath { get; set; }
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
