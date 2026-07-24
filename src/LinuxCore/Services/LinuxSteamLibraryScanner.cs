using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public string DLSSVersion { get; set; } = "Not found";
    public string DLSSGVersion { get; set; } = "Not found";
    public string DLSSDVersion { get; set; } = "Not found";
    public string Fsr31Dx12Version { get; set; } = "Not found";
    public string Fsr31VkVersion { get; set; } = "Not found";
    public string XessVersion { get; set; } = "Not found";
    public string XessDx11Version { get; set; } = "Not found";
    public string XessFgVersion { get; set; } = "Not found";
    public string XellVersion { get; set; } = "Not found";
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

    // Dynamic keyword exclusion list for tools, compatibility runtimes, and non-game software
    private static readonly string[] ExcludedKeywords = new[]
    {
        "proton", "steam linux runtime", "steamvr", "obs studio", "steamworks",
        "soundtrack", "sdk", "server", "tool", "shader pre-caching", "directx",
        "dotnet", "vulkan", "redistributables", "common redist", "runtime",
        "compatibility", "controller config", "easy anti-cheat", "battleye"
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
        var foundPaths = GetSteamLibraryDirectories();
        return Task.FromResult(foundPaths);
    }

    /// <summary>
    /// Returns all configured Steam steamapps directories across internal and external storage mounts.
    /// Deduplicates symlinked paths (e.g. ~/.steam/steam -> ~/.local/share/Steam).
    /// </summary>
    public List<string> GetSteamLibraryDirectories()
    {
        var libraries = new List<string>();
        var steamPath = GetSteamInstallPath();
        if (string.IsNullOrEmpty(steamPath)) return libraries;

        var mainSteamApps = Path.Combine(steamPath, "steamapps");
        AddNormalizedDirectory(libraries, mainSteamApps);

        var possibleVdfPaths = new[]
        {
            Path.Combine(mainSteamApps, "libraryfolders.vdf"),
            Path.Combine(steamPath, "config", "libraryfolders.vdf")
        };

        foreach (var vdfPath in possibleVdfPaths)
        {
            if (File.Exists(vdfPath))
            {
                try
                {
                    var content = File.ReadAllText(vdfPath);
                    var matches = Regex.Matches(content, @"""path""\s+""([^""]+)""", RegexOptions.IgnoreCase);
                    foreach (Match match in matches)
                    {
                        if (match.Success)
                        {
                            var rawPath = match.Groups[1].Value.Replace(@"\\", @"/");
                            var steamAppsSubDir = Path.Combine(rawPath, "steamapps");
                            AddNormalizedDirectory(libraries, steamAppsSubDir);
                        }
                    }
                }
                catch
                {
                    // Ignore unparseable VDF files
                }
            }
        }

        return libraries;
    }

    private void AddNormalizedDirectory(List<string> list, string dirPath)
    {
        if (!Directory.Exists(dirPath)) return;

        try
        {
            var canonicalPath = Path.GetFullPath(dirPath);
            if (!list.Any(existing => string.Equals(Path.GetFullPath(existing), canonicalPath, StringComparison.OrdinalIgnoreCase)))
            {
                list.Add(canonicalPath);
            }
        }
        catch
        {
            if (!list.Contains(dirPath, StringComparer.OrdinalIgnoreCase))
            {
                list.Add(dirPath);
            }
        }
    }

    public List<DiscoveredGameInfo> ScanInstalledGames()
    {
        var games = new List<DiscoveredGameInfo>();
        var scannedAppIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var scannedInstallPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var steamPath = GetSteamInstallPath();
        if (string.IsNullOrEmpty(steamPath)) return games;

        var libraryDirectories = GetSteamLibraryDirectories();

        foreach (var steamAppsDir in libraryDirectories)
        {
            if (!Directory.Exists(steamAppsDir)) continue;

            foreach (var manifestFile in Directory.GetFiles(steamAppsDir, "appmanifest_*.acf"))
            {
                try
                {
                    var filename = Path.GetFileNameWithoutExtension(manifestFile);
                    var appId = filename.Replace("appmanifest_", "");

                    if (scannedAppIds.Contains(appId))
                    {
                        continue; // Skip duplicate AppID
                    }

                    var content = File.ReadAllText(manifestFile);
                    var nameMatch = Regex.Match(content, @"""name""\s+""([^""]+)""", RegexOptions.IgnoreCase);
                    var dirMatch = Regex.Match(content, @"""installdir""\s+""([^""]+)""", RegexOptions.IgnoreCase);

                    if (nameMatch.Success && dirMatch.Success)
                    {
                        var gameName = nameMatch.Groups[1].Value;
                        var installDir = dirMatch.Groups[1].Value;

                        var lowerName = gameName.ToLowerInvariant();
                        var lowerDir = installDir.ToLowerInvariant();

                        // Dynamic exclusion: filter out software, compatibility layers, and runtimes by keyword
                        if (ExcludedKeywords.Any(k => lowerName.Contains(k) || lowerDir.Contains(k)))
                        {
                            continue;
                        }

                        var fullPath = Path.Combine(steamAppsDir, "common", installDir);
                        if (!Directory.Exists(fullPath)) continue;

                        var normalizedFullPath = Path.GetFullPath(fullPath);
                        if (scannedInstallPaths.Contains(normalizedFullPath))
                        {
                            continue; // Skip duplicate physical install path
                        }

                        scannedAppIds.Add(appId);
                        scannedInstallPaths.Add(normalizedFullPath);

                        var coverImage = ResolveCoverImage(steamPath, appId);

                        games.Add(new DiscoveredGameInfo
                        {
                            AppId = appId,
                            Name = gameName,
                            InstallPath = normalizedFullPath,
                            Launcher = "Steam",
                            DLSSVersion = ScanDllVersion(normalizedFullPath, "nvngx_dlss.dll"),
                            DLSSGVersion = ScanDllVersion(normalizedFullPath, "nvngx_dlssg.dll"),
                            DLSSDVersion = ScanDllVersion(normalizedFullPath, "nvngx_dlssd.dll"),
                            Fsr31Dx12Version = ScanDllVersion(normalizedFullPath, "amd_fidelityfx_dx12.dll", "ffx_fsr31_x64.dll", "ffx_fsr31_dx12_x64.dll"),
                            Fsr31VkVersion = ScanDllVersion(normalizedFullPath, "amd_fidelityfx_vk.dll", "ffx_fsr31_vk_x64.dll"),
                            XessVersion = ScanDllVersion(normalizedFullPath, "libxess.dll"),
                            XessDx11Version = ScanDllVersion(normalizedFullPath, "libxess_dx11.dll"),
                            XessFgVersion = ScanDllVersion(normalizedFullPath, "libxess_fg.dll"),
                            XellVersion = ScanDllVersion(normalizedFullPath, "libxell.dll"),
                            CoverImagePath = coverImage
                        });
                    }
                }
                catch
                {
                    // Skip unreadable manifest files
                }
            }
        }

        return games;
    }

    private string ResolveCoverImage(string steamPath, string appId)
    {
        // Check local Steam librarycache
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

        // Steam CDN online image fallback
        return $"https://shared.cloudflare.steamstatic.com/store_item_assets/steam/apps/{appId}/library_600x900.jpg";
    }

    public string ExtractDllVersionFromFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return string.Empty;

        try
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] pattern = System.Text.Encoding.Unicode.GetBytes("FileVersion\0");
                long patternOffset = FindBytes(stream, pattern);
                if (patternOffset != -1)
                {
                    long startOffset = patternOffset - 6;
                    if (startOffset >= 0)
                    {
                        stream.Position = startOffset + 2;
                        int wValueLength = stream.ReadByte() | (stream.ReadByte() << 8);

                        if (wValueLength > 0 && wValueLength < 64)
                        {
                            stream.Position = patternOffset + 26;
                            byte[] valBytes = new byte[wValueLength * 2];
                            int read = stream.Read(valBytes, 0, valBytes.Length);
                            if (read == valBytes.Length)
                            {
                                var versionRaw = System.Text.Encoding.Unicode.GetString(valBytes).TrimEnd('\0').Trim();
                                var version = versionRaw.Replace(',', '.').Replace(" ", "");

                                while (version.EndsWith(".0"))
                                {
                                    version = version.Substring(0, version.Length - 2);
                                }

                                if (!string.IsNullOrEmpty(version))
                                {
                                    if (!version.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                                    {
                                        version = "v" + version;
                                    }
                                    return version;
                                }
                            }
                        }
                    }
                }
            }
        }
        catch
        {
        }
        return "Unknown";
    }

    public string ScanDllVersion(string gameDirectory, params string[] dllFilenames)
    {
        if (string.IsNullOrEmpty(gameDirectory) || !Directory.Exists(gameDirectory)) return "N/A";

        try
        {
            foreach (var filename in dllFilenames)
            {
                var files = Directory.GetFiles(gameDirectory, filename, SearchOption.AllDirectories);
                if (files.Length > 0)
                {
                    var ver = ExtractDllVersionFromFile(files[0]);
                    if (!string.IsNullOrEmpty(ver) && ver != "Unknown")
                    {
                        return ver;
                    }
                    return "Installed";
                }
            }
        }
        catch
        {
        }
        return "N/A";
    }

    private static long FindBytes(Stream stream, byte[] pattern)
    {
        int patternLength = pattern.Length;
        int bufSize = 4096;
        byte[] buffer = new byte[bufSize];
        int bytesRead;
        long streamPos = 0;
        int matched = 0;

        while ((bytesRead = stream.Read(buffer, 0, bufSize)) > 0)
        {
            for (int i = 0; i < bytesRead; i++)
            {
                if (buffer[i] == pattern[matched])
                {
                    matched++;
                    if (matched == patternLength)
                    {
                        return streamPos + i - patternLength + 1;
                    }
                }
                else
                {
                    matched = (buffer[i] == pattern[0]) ? 1 : 0;
                }
            }
            streamPos += bytesRead;
        }
        return -1;
    }
}
