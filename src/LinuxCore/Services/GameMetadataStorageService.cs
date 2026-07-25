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
}
