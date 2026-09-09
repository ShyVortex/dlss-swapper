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
    public string ManifestPath { get; set; } = string.Empty;
    public long ManifestLastWriteTimeUtcTicks { get; set; }
}

public struct GameDllVersions
{
    public string DLSSVersion { get; set; } = "Not found";
    public string DLSSGVersion { get; set; } = "Not found";
    public string DLSSDVersion { get; set; } = "Not found";
    public string Fsr31Dx12Version { get; set; } = "Not found";
    public string Fsr31VkVersion { get; set; } = "Not found";
    public string XessVersion { get; set; } = "Not found";
    public string XessDx11Version { get; set; } = "Not found";
    public string XessFgVersion { get; set; } = "Not found";
    public string XellVersion { get; set; } = "Not found";

    public GameDllVersions() { }
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

    private static bool IsExcludedSoftware(string name, string installDir)
    {
        var lowerName = name.ToLowerInvariant();
        var lowerDir = installDir.ToLowerInvariant();

        foreach (var keyword in ExcludedKeywords)
        {
            if (keyword.Contains(' '))
            {
                if (lowerName.Contains(keyword) || lowerDir.Contains(keyword))
                    return true;
            }
            else
            {
                if (Regex.IsMatch(lowerName, $@"\b{Regex.Escape(keyword)}\b") ||
                    Regex.IsMatch(lowerDir, $@"\b{Regex.Escape(keyword)}\b"))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public bool IsLauncherInstalled()
    {
        return GetSteamInstallPath() != null || GetSteamLibraryDirectories().Count > 0;
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
        var activeSteamPaths = PossibleSteamPaths.Where(Directory.Exists).ToList();

        foreach (var steamPath in activeSteamPaths)
        {
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
                                if (rawPath.EndsWith("steamapps", StringComparison.OrdinalIgnoreCase))
                                {
                                    AddNormalizedDirectory(libraries, rawPath);
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Ignore unparseable VDF files
                    }
                }
            }
        }

        // Fallback mount scanning: Discover Steam libraries across all storage mounts
        var mountRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var topMountParents = new[] { "/mnt", "/media", "/run/media" };
        foreach (var top in topMountParents)
        {
            if (Directory.Exists(top))
            {
                mountRoots.Add(top);
                try
                {
                    foreach (var sub in Directory.GetDirectories(top))
                    {
                        mountRoots.Add(sub);
                        try
                        {
                            foreach (var subsub in Directory.GetDirectories(sub))
                            {
                                mountRoots.Add(subsub);
                            }
                        }
                        catch { }
                    }
                }
                catch { }
            }
        }

        if (File.Exists("/proc/mounts"))
        {
            try
            {
                foreach (var line in File.ReadAllLines("/proc/mounts"))
                {
                    var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        var target = parts[1];
                        if (target.StartsWith("/mnt", StringComparison.OrdinalIgnoreCase) ||
                            target.StartsWith("/media", StringComparison.OrdinalIgnoreCase) ||
                            target.StartsWith("/run/media", StringComparison.OrdinalIgnoreCase) ||
                            target.StartsWith("/data", StringComparison.OrdinalIgnoreCase) ||
                            target.StartsWith("/games", StringComparison.OrdinalIgnoreCase) ||
                            target.StartsWith("/disks", StringComparison.OrdinalIgnoreCase))
                        {
                            if (Directory.Exists(target)) mountRoots.Add(target);
                        }
                    }
                }
            }
            catch { }
        }

        var candidateSteamFolderNames = new[] { "SteamLibrary", "steamlibrary", "Steam", "steam" };
        foreach (var root in mountRoots)
        {
            foreach (var folderName in candidateSteamFolderNames)
            {
                var targetDir = Path.Combine(root, folderName, "steamapps");
                if (Directory.Exists(targetDir))
                {
                    try
                    {
                        if (Directory.EnumerateFiles(targetDir, "appmanifest_*.acf").Any())
                        {
                            AddNormalizedDirectory(libraries, targetDir);
                        }
                    }
                    catch { }
                }
            }

            var directSteamapps = Path.Combine(root, "steamapps");
            if (Directory.Exists(directSteamapps))
            {
                try
                {
                    if (Directory.EnumerateFiles(directSteamapps, "appmanifest_*.acf").Any())
                    {
                        AddNormalizedDirectory(libraries, directSteamapps);
                    }
                }
                catch { }
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

    public List<DiscoveredGameInfo> ScanInstalledGames(Dictionary<string, ScannedGameCacheEntry>? cache = null, bool forceRescan = false)
    {
        var games = new List<DiscoveredGameInfo>();
        var scannedAppIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var scannedInstallPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var steamPath = GetSteamInstallPath() ?? string.Empty;
        var libraryDirectories = GetSteamLibraryDirectories();
        if (libraryDirectories.Count == 0 && string.IsNullOrEmpty(steamPath)) return games;

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

                    var manifestInfo = new FileInfo(manifestFile);
                    var manifestTicks = manifestInfo.LastWriteTimeUtc.Ticks;

                    var content = File.ReadAllText(manifestFile);
                    var nameMatch = Regex.Match(content, @"""name""\s+""([^""]+)""", RegexOptions.IgnoreCase);
                    var dirMatch = Regex.Match(content, @"""installdir""\s+""([^""]+)""", RegexOptions.IgnoreCase);

                    if (nameMatch.Success && dirMatch.Success)
                    {
                        var gameName = nameMatch.Groups[1].Value;
                        var installDir = dirMatch.Groups[1].Value;

                        // Dynamic exclusion: filter out software, compatibility layers, and runtimes by keyword
                        if (IsExcludedSoftware(gameName, installDir))
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

                        // Delta cache lookup: If manifest timestamp is unchanged, directory exists, and cached DLLs are valid, reuse cached info
                        if (!forceRescan && cache != null && cache.TryGetValue(appId, out var cachedEntry))
                        {
                            bool hasAnyValidDll = cachedEntry.DllMap != null && cachedEntry.DllMap.Values.Any(v =>
                                !string.IsNullOrWhiteSpace(v) &&
                                !string.Equals(v, "Not found", StringComparison.OrdinalIgnoreCase) &&
                                !string.Equals(v, "N/A", StringComparison.OrdinalIgnoreCase));

                            if (hasAnyValidDll && cachedEntry.ManifestLastWriteTimeUtcTicks == manifestTicks && Directory.Exists(normalizedFullPath))
                            {
                                games.Add(new DiscoveredGameInfo
                                {
                                    AppId = appId,
                                    Name = gameName,
                                    InstallPath = normalizedFullPath,
                                    Launcher = "Steam",
                                    DLSSVersion = cachedEntry.DllMap.GetValueOrDefault("dlss", "Not found"),
                                    DLSSGVersion = cachedEntry.DllMap.GetValueOrDefault("dlss_g", "Not found"),
                                    DLSSDVersion = cachedEntry.DllMap.GetValueOrDefault("dlss_d", "Not found"),
                                    Fsr31Dx12Version = cachedEntry.DllMap.GetValueOrDefault("fsr_31_dx12", "Not found"),
                                    Fsr31VkVersion = cachedEntry.DllMap.GetValueOrDefault("fsr_31_vk", "Not found"),
                                    XessVersion = cachedEntry.DllMap.GetValueOrDefault("xess", "Not found"),
                                    XessDx11Version = cachedEntry.DllMap.GetValueOrDefault("xess_dx11", "Not found"),
                                    XessFgVersion = cachedEntry.DllMap.GetValueOrDefault("xess_fg", "Not found"),
                                    XellVersion = cachedEntry.DllMap.GetValueOrDefault("xell", "Not found"),
                                    CoverImagePath = !string.IsNullOrEmpty(cachedEntry.CoverImagePath) && File.Exists(cachedEntry.CoverImagePath) ? cachedEntry.CoverImagePath : ResolveCoverImage(steamPath, appId),
                                    ManifestPath = manifestFile,
                                    ManifestLastWriteTimeUtcTicks = manifestTicks
                                });
                                continue;
                            }
                        }

                        var coverImage = ResolveCoverImage(steamPath, appId);
                        var dlls = ScanAllGameDlls(normalizedFullPath);

                        games.Add(new DiscoveredGameInfo
                        {
                            AppId = appId,
                            Name = gameName,
                            InstallPath = normalizedFullPath,
                            Launcher = "Steam",
                            DLSSVersion = dlls.DLSSVersion,
                            DLSSGVersion = dlls.DLSSGVersion,
                            DLSSDVersion = dlls.DLSSDVersion,
                            Fsr31Dx12Version = dlls.Fsr31Dx12Version,
                            Fsr31VkVersion = dlls.Fsr31VkVersion,
                            XessVersion = dlls.XessVersion,
                            XessDx11Version = dlls.XessDx11Version,
                            XessFgVersion = dlls.XessFgVersion,
                            XellVersion = dlls.XellVersion,
                            CoverImagePath = coverImage,
                            ManifestPath = manifestFile,
                            ManifestLastWriteTimeUtcTicks = manifestTicks
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

    private static HashSet<string>? _cachedDevHashes;
    private static readonly object _hashLock = new();

    private static HashSet<string> GetCachedDevHashes()
    {
        if (_cachedDevHashes != null) return _cachedDevHashes;
        lock (_hashLock)
        {
            if (_cachedDevHashes != null) return _cachedDevHashes;
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
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

                        foreach (var r in allRecords)
                        {
                            if (r.IsDevFile && !string.IsNullOrEmpty(r.Md5Hash))
                            {
                                set.Add(r.Md5Hash);
                            }
                        }
                    }
                }
            }
            catch { }
            _cachedDevHashes = set;
            return _cachedDevHashes;
        }
    }

    private bool IsDebugDll(Stream stream, string filePath)
    {
        try
        {
            // Method 1: MD5 hash match against manifest dev records using cached memory set
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                using var md5 = System.Security.Cryptography.MD5.Create();
                using var fileStream = File.OpenRead(filePath);
                var hashBytes = md5.ComputeHash(fileStream);
                var hashHex = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

                var devHashes = GetCachedDevHashes();
                if (devHashes.Contains(hashHex))
                {
                    return true;
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

    public GameDllVersions ScanAllGameDlls(string gameDirectory)
    {
        var result = new GameDllVersions();
        if (string.IsNullOrEmpty(gameDirectory) || !Directory.Exists(gameDirectory)) return result;

        try
        {
            var foundDllPaths = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            var excludedFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "content", "paks", "audio", "sound", "sounds", "video", "movies", "textures",
                "shaders", "saves", "cache", "__pycache__", ".git", "logs", "screenshots", "docs", "manual",
                "locale", "locales", "localization", "translations"
            };

            var queue = new Queue<(string Dir, int Depth)>();
            queue.Enqueue((gameDirectory, 0));

            while (queue.Count > 0)
            {
                var (currentDir, depth) = queue.Dequeue();

                try
                {
                    var files = Directory.GetFiles(currentDir, "*.dll");
                    foreach (var file in files)
                    {
                        var name = Path.GetFileName(file);
                        if (!foundDllPaths.TryGetValue(name, out var list))
                        {
                            list = new List<string>();
                            foundDllPaths[name] = list;
                        }
                        list.Add(file);
                    }

                    if (depth < 12)
                    {
                        foreach (var subDir in Directory.GetDirectories(currentDir))
                        {
                            var folderName = Path.GetFileName(subDir);
                            if (!excludedFolderNames.Contains(folderName) && !folderName.StartsWith("."))
                            {
                                queue.Enqueue((subDir, depth + 1));
                            }
                        }
                    }
                }
                catch { }
            }

            // 1. DLSS
            if (foundDllPaths.TryGetValue("nvngx_dlss.dll", out var dlssPaths))
                result.DLSSVersion = ExtractDllVersionSafe(dlssPaths);

            // 2. DLSSG
            if (foundDllPaths.TryGetValue("nvngx_dlssg.dll", out var dlssgPaths))
                result.DLSSGVersion = ExtractDllVersionSafe(dlssgPaths);

            // 3. DLSSD
            if (foundDllPaths.TryGetValue("nvngx_dlssd.dll", out var dlssdPaths))
                result.DLSSDVersion = ExtractDllVersionSafe(dlssdPaths);

            // 4. FSR 3.1 DX12
            var fsr12Candidates = new List<string>();
            if (foundDllPaths.TryGetValue("amd_fidelityfx_dx12.dll", out var f1)) fsr12Candidates.AddRange(f1);
            if (foundDllPaths.TryGetValue("ffx_fsr31_x64.dll", out var f2)) fsr12Candidates.AddRange(f2);
            if (foundDllPaths.TryGetValue("ffx_fsr31_dx12_x64.dll", out var f3)) fsr12Candidates.AddRange(f3);
            if (fsr12Candidates.Count > 0)
                result.Fsr31Dx12Version = ExtractDllVersionSafe(fsr12Candidates);

            // 5. FSR 3.1 VK
            var fsrVkCandidates = new List<string>();
            if (foundDllPaths.TryGetValue("amd_fidelityfx_vk.dll", out var fv1)) fsrVkCandidates.AddRange(fv1);
            if (foundDllPaths.TryGetValue("ffx_fsr31_vk_x64.dll", out var fv2)) fsrVkCandidates.AddRange(fv2);
            if (fsrVkCandidates.Count > 0)
                result.Fsr31VkVersion = ExtractDllVersionSafe(fsrVkCandidates);

            // 6. XeSS
            if (foundDllPaths.TryGetValue("libxess.dll", out var xessPaths))
                result.XessVersion = ExtractDllVersionSafe(xessPaths);

            // 7. XeSS DX11
            if (foundDllPaths.TryGetValue("libxess_dx11.dll", out var xessDx11Paths))
                result.XessDx11Version = ExtractDllVersionSafe(xessDx11Paths);

            // 8. XeSS FG
            if (foundDllPaths.TryGetValue("libxess_fg.dll", out var xessFgPaths))
                result.XessFgVersion = ExtractDllVersionSafe(xessFgPaths);

            // 9. XeLL
            if (foundDllPaths.TryGetValue("libxell.dll", out var xellPaths))
                result.XellVersion = ExtractDllVersionSafe(xellPaths);
        }
        catch { }

        return result;
    }

    private string ExtractDllVersionSafe(IEnumerable<string> filePaths)
    {
        string? firstInstalled = null;
        foreach (var filePath in filePaths)
        {
            var ver = ExtractDllVersionFromFile(filePath);
            if (!string.IsNullOrEmpty(ver) && ver != "Unknown") return ver;
            firstInstalled ??= "Installed";
        }
        return firstInstalled ?? "Not found";
    }

    private string ExtractDllVersionSafe(string filePath)
    {
        var ver = ExtractDllVersionFromFile(filePath);
        if (!string.IsNullOrEmpty(ver) && ver != "Unknown") return ver;
        return "Installed";
    }

    public string ScanDllVersion(string gameDirectory, params string[] dllFilenames)
    {
        if (string.IsNullOrEmpty(gameDirectory) || !Directory.Exists(gameDirectory)) return "N/A";

        try
        {
            var dlls = ScanAllGameDlls(gameDirectory);
            foreach (var filename in dllFilenames)
            {
                var lower = filename.ToLowerInvariant();
                if (lower.Contains("dlssg")) return dlls.DLSSGVersion;
                if (lower.Contains("dlssd")) return dlls.DLSSDVersion;
                if (lower.Contains("dlss")) return dlls.DLSSVersion;
                if (lower.Contains("fidelityfx_dx12") || lower.Contains("fsr31_x64") || lower.Contains("fsr31_dx12")) return dlls.Fsr31Dx12Version;
                if (lower.Contains("fidelityfx_vk") || lower.Contains("fsr31_vk")) return dlls.Fsr31VkVersion;
                if (lower.Contains("xess_dx11")) return dlls.XessDx11Version;
                if (lower.Contains("xess_fg")) return dlls.XessFgVersion;
                if (lower.Contains("xess")) return dlls.XessVersion;
                if (lower.Contains("xell")) return dlls.XellVersion;
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
