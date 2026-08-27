using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DLSS_Swapper.Core.Interfaces;
using DLSS_Swapper.Core.Models;

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

        DLSS_Swapper.Logger.Info($"Scanned {games.Count} installed Steam games.");
        return games;
    }

    private string ResolveCoverImage(string steamPath, string appId)
    {
        var candidateRoots = new List<string>();
        if (!string.IsNullOrEmpty(steamPath) && Directory.Exists(steamPath))
        {
            candidateRoots.Add(steamPath);
        }
        foreach (var p in PossibleSteamPaths)
        {
            if (Directory.Exists(p) && !candidateRoots.Contains(p))
            {
                candidateRoots.Add(p);
            }
        }

        foreach (var root in candidateRoots)
        {
            // 1. Check user custom portrait grid artwork (userdata/{userId}/config/grid/)
            var userdataPath = Path.Combine(root, "userdata");
            if (Directory.Exists(userdataPath))
            {
                try
                {
                    foreach (var userDir in Directory.GetDirectories(userdataPath))
                    {
                        var gridPath = Path.Combine(userDir, "config", "grid");
                        if (!Directory.Exists(gridPath)) continue;

                        var customCandidates = new[]
                        {
                            Path.Combine(gridPath, $"{appId}p.png"),
                            Path.Combine(gridPath, $"{appId}p.jpg"),
                            Path.Combine(gridPath, $"{appId}_600x900.jpg"),
                            Path.Combine(gridPath, $"{appId}_600x900.png")
                        };

                        foreach (var candidate in customCandidates)
                        {
                            if (File.Exists(candidate)) return candidate;
                        }
                    }
                }
                catch
                {
                    // Ignore filesystem errors in userdata
                }
            }

            // 2. Check local Steam librarycache for vertical portrait covers (600x900 / capsule)
            var libraryCachePath = Path.Combine(root, "appcache", "librarycache");
            if (Directory.Exists(libraryCachePath))
            {
                // 2a. Legacy flat file (e.g. {appId}_library_600x900.jpg)
                var localCover = Path.Combine(libraryCachePath, $"{appId}_library_600x900.jpg");
                if (File.Exists(localCover)) return localCover;

                // 2b. Modern Steam directory structure (appcache/librarycache/{appId}/...)
                var appCacheDir = Path.Combine(libraryCachePath, appId);
                if (Directory.Exists(appCacheDir))
                {
                    try
                    {
                        // Direct file inside folder (e.g. 1174180/library_600x900.jpg)
                        var direct600x900 = Path.Combine(appCacheDir, "library_600x900.jpg");
                        if (File.Exists(direct600x900)) return direct600x900;

                        // Recursive search for 600x900
                        var direct600x900Files = Directory.GetFiles(appCacheDir, "library_600x900.jpg", SearchOption.AllDirectories);
                        if (direct600x900Files.Length > 0) return direct600x900Files[0];

                        // Steam vertical capsule cover (e.g. {appId}/{hash}/library_capsule.jpg)
                        var capsuleFiles = Directory.GetFiles(appCacheDir, "library_capsule.jpg", SearchOption.AllDirectories);
                        if (capsuleFiles.Length > 0) return capsuleFiles[0];

                        var any600x900 = Directory.GetFiles(appCacheDir, "*600x900*.jpg", SearchOption.AllDirectories);
                        if (any600x900.Length > 0) return any600x900[0];
                    }
                    catch
                    {
                        // Ignore directory search errors
                    }
                }
            }
        }

        // 3. Steam CDN online portrait image fallback (shared CDN 600x900 capsule)
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

                                    bool isDebug = IsDebugDll(stream, filePath);
                                    if (isDebug)
                                    {
                                        version += " (Debug)";
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

    private bool IsDebugDll(Stream stream, string filePath)
    {
        try
        {
            // Method 1: MD5 hash match against manifest dev records
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                using var md5 = System.Security.Cryptography.MD5.Create();
                using var fileStream = File.OpenRead(filePath);
                var hashBytes = md5.ComputeHash(fileStream);
                var hashHex = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

                var storageService = new LibraryStorageService();
                var manifestPath = Path.Combine(LibraryStorageService.StorageFolder, "json", "manifest.json");
                if (File.Exists(manifestPath))
                {
                    var json = File.ReadAllText(manifestPath);
                    var manifest = System.Text.Json.JsonSerializer.Deserialize<ManifestModel>(json);
                    if (manifest != null)
                    {
                        var allRecords = new List<DllRecordModel>();
                        allRecords.AddRange(manifest.Dlss ?? new());
                        allRecords.AddRange(manifest.DlssG ?? new());
                        allRecords.AddRange(manifest.DlssD ?? new());

                        var matchedRecord = allRecords.FirstOrDefault(r => string.Equals(r.Md5Hash, hashHex, StringComparison.OrdinalIgnoreCase));
                        if (matchedRecord != null)
                        {
                            return matchedRecord.IsDevFile;
                        }
                    }
                }
            }

            // Method 2: PE String table inspection (OriginalFilename / FileDescription)
            byte[] origFilenamePattern = System.Text.Encoding.Unicode.GetBytes("OriginalFilename\0");
            long origOffset = FindBytes(stream, origFilenamePattern);
            if (origOffset != -1)
            {
                stream.Position = origOffset + 26;
                byte[] nameBytes = new byte[128];
                int read = stream.Read(nameBytes, 0, nameBytes.Length);
                if (read > 0)
                {
                    var origName = System.Text.Encoding.Unicode.GetString(nameBytes).TrimEnd('\0').Trim();
                    if (origName.Contains("_dbg", StringComparison.OrdinalIgnoreCase) ||
                        origName.Contains("_dev", StringComparison.OrdinalIgnoreCase) ||
                        origName.Contains("debug", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
        }
        catch
        {
        }
        return false;
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
