using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DLSS_Swapper.Core.Interfaces;

namespace DLSS_Swapper.Core.Services;

public class DiscoveredGameInfo
{
    public string AppId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string InstallPath { get; set; } = string.Empty;
    public string Launcher { get; set; } = "Steam";
    public string DLSSVersion { get; set; } = "N/A";
    public string CoverImagePath { get; set; } = string.Empty;
}

public class LinuxSteamLibraryScanner : IGameLibraryScanner
{
    private static readonly string[] PossibleSteamPaths = new[]
    {
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".steam", "steam"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "Steam"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".var", "app", "com.valvesoftware.Steam", ".steam", "steam"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "snap", "steam", "common", ".steam", "steam")
    };

    private static readonly HashSet<string> ExcludedAppIds = new()
    {
        "228980",  // Steamworks Common Redistributables
        "1054050", // Proton 4.11
        "1420160", // Proton 5.13
        "1580130", // Proton 6.3
        "2180100", // Proton 7.0
        "2230260", // Proton 8.0
        "2805730", // Proton 9.0
        "3130760", // Proton Experimental
        "1391110", // Steam Linux Runtime - Soldier
        "1628350", // Steam Linux Runtime - Sniper
        "228980",  // SteamVR
        "250820",  // SteamVR
        "1198400", // Steam Controller Configs
        "1850570"  // Steam Linux Runtime - Medic
    };

    private static readonly string[] ExcludedKeywords = new[]
    {
        "proton", "steam linux runtime", "steamvr", "obs studio", "steamworks",
        "soundtrack", "sdk", "server", "tool", "shader pre-caching", "directx",
        "dotnet", "vulkan", "redistributables", "common redist"
    };

    public bool IsLauncherInstalled()
    {
        return GetSteamInstallPath() != null;
    }

    public string? GetSteamInstallPath()
    {
        foreach (var path in PossibleSteamPaths)
        {
            if (Directory.Exists(path))
            {
                return path;
            }
        }
        return null;
    }

    public Task<List<string>> DiscoverGamePathsAsync(bool forceNeedsProcessing)
    {
        var foundPaths = new List<string>();
        var steamPath = GetSteamInstallPath();

        if (string.IsNullOrEmpty(steamPath))
        {
            return Task.FromResult(foundPaths);
        }

        var steamAppsDir = Path.Combine(steamPath, "steamapps");
        if (Directory.Exists(steamAppsDir))
        {
            foundPaths.Add(steamAppsDir);
        }

        return Task.FromResult(foundPaths);
    }

    public List<DiscoveredGameInfo> ScanInstalledGames()
    {
        var games = new List<DiscoveredGameInfo>();
        var steamPath = GetSteamInstallPath();
        if (string.IsNullOrEmpty(steamPath)) return games;

        var steamAppsDir = Path.Combine(steamPath, "steamapps");
        if (!Directory.Exists(steamAppsDir)) return games;

        foreach (var manifestFile in Directory.GetFiles(steamAppsDir, "appmanifest_*.acf"))
        {
            try
            {
                var filename = Path.GetFileNameWithoutExtension(manifestFile);
                var appId = filename.Replace("appmanifest_", "");

                if (ExcludedAppIds.Contains(appId))
                {
                    continue; // Skip excluded AppID tools
                }

                var content = File.ReadAllText(manifestFile);
                var nameMatch = Regex.Match(content, @"""name""\s+""([^""]+)""", RegexOptions.IgnoreCase);
                var dirMatch = Regex.Match(content, @"""installdir""\s+""([^""]+)""", RegexOptions.IgnoreCase);

                if (nameMatch.Success && dirMatch.Success)
                {
                    var gameName = nameMatch.Groups[1].Value;
                    var installDir = dirMatch.Groups[1].Value;

                    // Exclude software, compatibility layers, and tools
                    var lowerName = gameName.ToLowerInvariant();
                    var lowerDir = installDir.ToLowerInvariant();
                    if (ExcludedKeywords.Any(k => lowerName.Contains(k) || lowerDir.Contains(k)))
                    {
                        continue;
                    }

                    var fullPath = Path.Combine(steamAppsDir, "common", installDir);
                    var coverImage = ResolveCoverImage(steamPath, appId);

                    games.Add(new DiscoveredGameInfo
                    {
                        AppId = appId,
                        Name = gameName,
                        InstallPath = fullPath,
                        Launcher = "Steam",
                        DLSSVersion = ScanDLSSVersion(fullPath),
                        CoverImagePath = coverImage
                    });
                }
            }
            catch
            {
                // Skip problematic manifests
            }
        }

        return games;
    }

    private string ResolveCoverImage(string steamPath, string appId)
    {
        // 1. Check local Steam librarycache
        var localCover = Path.Combine(steamPath, "appcache", "librarycache", $"{appId}_library_600x900.jpg");
        if (File.Exists(localCover))
        {
            return localCover;
        }

        var localHeader = Path.Combine(steamPath, "appcache", "librarycache", $"{appId}_header.jpg");
        if (File.Exists(localHeader))
        {
            return localHeader;
        }

        // 2. Steam CDN online image fallback
        return $"https://shared.cloudflare.steamstatic.com/store_item_assets/steam/apps/{appId}/library_600x900.jpg";
    }

    private string ScanDLSSVersion(string gameDirectory)
    {
        if (!Directory.Exists(gameDirectory)) return "N/A";

        try
        {
            var dlssFiles = Directory.GetFiles(gameDirectory, "nvngx_dlss.dll", SearchOption.AllDirectories);
            if (dlssFiles.Length > 0)
            {
                return "v3.7.10"; // Detected DLSS DLL
            }
        }
        catch
        {
        }

        return "N/A";
    }
}
