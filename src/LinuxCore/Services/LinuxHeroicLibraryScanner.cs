using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DLSS_Swapper.Core.Interfaces;

namespace DLSS_Swapper.Core.Services;

public class LinuxHeroicLibraryScanner : IGameLibraryScanner
{
    private static readonly string[] PossibleHeroicConfigDirs = new[]
    {
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "heroic"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".var", "app", "com.heroicgameslauncher.hgl", "config", "heroic"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "snap", "heroic", "current", ".config", "heroic")
    };

    private record HeroicGameMetadata(string AppId, string Title, string CoverUrl, string? InstallPath, string Runner);

    public bool IsLauncherInstalled()
    {
        return PossibleHeroicConfigDirs.Any(Directory.Exists) || DiscoverHeroicGameDirectories().Count > 0;
    }

    public Task<List<string>> DiscoverGamePathsAsync(bool forceNeedsProcessing)
    {
        var discoveredPaths = new List<string>();
        foreach (var dir in PossibleHeroicConfigDirs)
        {
            if (Directory.Exists(dir))
            {
                discoveredPaths.Add(dir);
            }
        }
        return Task.FromResult(discoveredPaths);
    }

    public List<DiscoveredGameInfo> ScanInstalledGames()
    {
        var games = new List<DiscoveredGameInfo>();
        var scannedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var steamScanner = new LinuxSteamLibraryScanner();

        var activeConfigDirs = PossibleHeroicConfigDirs.Where(Directory.Exists).ToList();
        var metadataMap = new Dictionary<string, HeroicGameMetadata>(StringComparer.OrdinalIgnoreCase);

        foreach (var configDir in activeConfigDirs)
        {
            LoadHeroicMetadata(configDir, metadataMap);
        }

        // 1. Scan GOG installed games (gog_store/installed.json)
        foreach (var configDir in activeConfigDirs)
        {
            var gogInstalledJson = Path.Combine(configDir, "gog_store", "installed.json");
            if (File.Exists(gogInstalledJson))
            {
                try
                {
                    var jsonText = File.ReadAllText(gogInstalledJson);
                    using var doc = JsonDocument.Parse(jsonText);

                    if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("installed", out var installedArray) && installedArray.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var elem in installedArray.EnumerateArray())
                        {
                            AddHeroicGameFromElement(elem, "GOG", metadataMap, activeConfigDirs, steamScanner, scannedPaths, games);
                        }
                    }
                    else if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var elem in doc.RootElement.EnumerateArray())
                        {
                            AddHeroicGameFromElement(elem, "GOG", metadataMap, activeConfigDirs, steamScanner, scannedPaths, games);
                        }
                    }
                    else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var prop in doc.RootElement.EnumerateObject())
                        {
                            AddHeroicGameFromElement(prop.Value, "GOG", metadataMap, activeConfigDirs, steamScanner, scannedPaths, games, prop.Name);
                        }
                    }
                }
                catch (Exception ex)
                {
                    DLSS_Swapper.Logger.Warning($"Failed to parse GOG installed.json: {ex.Message}");
                }
            }
        }

        // 2. Scan Legendary (Epic) installed games
        foreach (var configDir in activeConfigDirs)
        {
            var legPaths = new[]
            {
                Path.Combine(configDir, "legendaryConfig", "legendary", "installed.json"),
                Path.Combine(configDir, "legendary", "installed.json")
            };

            foreach (var legInstalledJson in legPaths)
            {
                if (File.Exists(legInstalledJson))
                {
                    try
                    {
                        var jsonText = File.ReadAllText(legInstalledJson);
                        using var doc = JsonDocument.Parse(jsonText);
                        if (doc.RootElement.ValueKind == JsonValueKind.Object)
                        {
                            foreach (var prop in doc.RootElement.EnumerateObject())
                            {
                                AddHeroicGameFromElement(prop.Value, "Epic", metadataMap, activeConfigDirs, steamScanner, scannedPaths, games, prop.Name);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        DLSS_Swapper.Logger.Warning($"Failed to parse Legendary installed.json: {ex.Message}");
                    }
                }
            }
        }

        // 3. Scan Nile (Amazon Prime) installed games (nile_store/installed.json)
        foreach (var configDir in activeConfigDirs)
        {
            var nileInstalledJson = Path.Combine(configDir, "nile_store", "installed.json");
            if (File.Exists(nileInstalledJson))
            {
                try
                {
                    var jsonText = File.ReadAllText(nileInstalledJson);
                    using var doc = JsonDocument.Parse(jsonText);
                    if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("installed", out var arr) && arr.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var elem in arr.EnumerateArray())
                        {
                            AddHeroicGameFromElement(elem, "Amazon", metadataMap, activeConfigDirs, steamScanner, scannedPaths, games);
                        }
                    }
                    else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var prop in doc.RootElement.EnumerateObject())
                        {
                            AddHeroicGameFromElement(prop.Value, "Amazon", metadataMap, activeConfigDirs, steamScanner, scannedPaths, games, prop.Name);
                        }
                    }
                }
                catch { }
            }
        }

        // 4. Scan Sideloaded apps (sideload_apps/installed.json)
        foreach (var configDir in activeConfigDirs)
        {
            var sideloadJson = Path.Combine(configDir, "sideload_apps", "installed.json");
            if (File.Exists(sideloadJson))
            {
                try
                {
                    var jsonText = File.ReadAllText(sideloadJson);
                    using var doc = JsonDocument.Parse(jsonText);
                    if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("installed", out var arr) && arr.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var elem in arr.EnumerateArray())
                        {
                            AddHeroicGameFromElement(elem, "Sideload", metadataMap, activeConfigDirs, steamScanner, scannedPaths, games);
                        }
                    }
                    else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var prop in doc.RootElement.EnumerateObject())
                        {
                            AddHeroicGameFromElement(prop.Value, "Sideload", metadataMap, activeConfigDirs, steamScanner, scannedPaths, games, prop.Name);
                        }
                    }
                }
                catch { }
            }
        }

        // 5. Check metadata library items marked is_installed or with valid install_path
        foreach (var (appId, meta) in metadataMap)
        {
            if (!string.IsNullOrEmpty(meta.InstallPath) && Directory.Exists(meta.InstallPath))
            {
                var canonical = Path.GetFullPath(meta.InstallPath);
                if (!scannedPaths.Contains(canonical))
                {
                    scannedPaths.Add(canonical);
                    var coverImage = ResolveHeroicCoverImage(appId, meta.Title, canonical, meta.CoverUrl, activeConfigDirs);
                    games.Add(CreateDiscoveredGame(appId, meta.Title, canonical, steamScanner, coverImage));
                }
            }
        }

        // 6. Scan external drives and mount locations for any Heroic game directories
        var heroicDirectories = DiscoverHeroicGameDirectories();
        foreach (var heroicDir in heroicDirectories)
        {
            if (!Directory.Exists(heroicDir)) continue;

            try
            {
                foreach (var gameDir in Directory.GetDirectories(heroicDir))
                {
                    var canonical = Path.GetFullPath(gameDir);
                    if (scannedPaths.Contains(canonical)) continue;

                    var folderName = Path.GetFileName(gameDir);
                    // Filter out container directories, prefixes, and system paths
                    var excludedFolderNames = new[] { "Heroic", "heroic", "Games", "Prefixes", "drive_c", "default", "dosdevices", "compatdata" };
                    if (excludedFolderNames.Any(ex => folderName.Equals(ex, StringComparison.OrdinalIgnoreCase)) ||
                        folderName.StartsWith("."))
                    {
                        continue;
                    }

                    // Check if folder contains executables or DLLs
                    bool containsGameContent = false;
                    try
                    {
                        containsGameContent = Directory.EnumerateFiles(gameDir, "*.*", SearchOption.AllDirectories)
                            .Take(200)
                            .Any(f => f.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                                      f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                                      f.EndsWith(".x86_64", StringComparison.OrdinalIgnoreCase) ||
                                      f.EndsWith(".bin", StringComparison.OrdinalIgnoreCase));
                    }
                    catch
                    {
                        // Ignore permission or file enumeration issues
                    }

                    if (!containsGameContent) continue;

                    scannedPaths.Add(canonical);

                    // Try to match metadata by folder name or title
                    var matchingMeta = metadataMap.Values.FirstOrDefault(m =>
                        folderName.Equals(m.Title, StringComparison.OrdinalIgnoreCase) ||
                        folderName.Replace(" ", "").Equals(m.Title.Replace(" ", ""), StringComparison.OrdinalIgnoreCase) ||
                        (m.InstallPath != null && Path.GetFileName(m.InstallPath).Equals(folderName, StringComparison.OrdinalIgnoreCase)));

                    string appId = matchingMeta?.AppId ?? $"heroic_{folderName}";
                    string title = matchingMeta?.Title ?? folderName;
                    string? coverUrl = matchingMeta?.CoverUrl;

                    var coverImage = ResolveHeroicCoverImage(appId, title, canonical, coverUrl, activeConfigDirs);
                    games.Add(CreateDiscoveredGame(appId, title, canonical, steamScanner, coverImage));
                }
            }
            catch (Exception ex)
            {
                DLSS_Swapper.Logger.Warning($"Failed scanning Heroic directory {heroicDir}: {ex.Message}");
            }
        }

        DLSS_Swapper.Logger.Info($"Scanned {games.Count} installed Heroic games.");
        return games;
    }

    public List<string> DiscoverHeroicGameDirectories()
    {
        var dirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1. Check default install path from Heroic config.json
        foreach (var configDir in PossibleHeroicConfigDirs)
        {
            var configJson = Path.Combine(configDir, "config.json");
            if (File.Exists(configJson))
            {
                try
                {
                    var jsonText = File.ReadAllText(configJson);
                    using var doc = JsonDocument.Parse(jsonText);
                    if (doc.RootElement.TryGetProperty("defaultSettings", out var settings) &&
                        settings.TryGetProperty("defaultInstallPath", out var pathProp))
                    {
                        var p = pathProp.GetString();
                        if (!string.IsNullOrEmpty(p) && Directory.Exists(p))
                        {
                            dirs.Add(Path.GetFullPath(p));
                        }
                    }
                }
                catch { }
            }
        }

        // 2. Check standard Home locations
        var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var homeCandidates = new[]
        {
            Path.Combine(userHome, "Games", "Heroic"),
            Path.Combine(userHome, "Heroic"),
            Path.Combine(userHome, "heroic")
        };
        foreach (var c in homeCandidates)
        {
            if (Directory.Exists(c)) dirs.Add(Path.GetFullPath(c));
        }

        // 3. Scan external drives and mount points (/mnt, /media, /run/media, /proc/mounts)
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

        // Read /proc/mounts
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

        // Search for folders named Heroic / heroic in all detected mount points
        var heroicFolderNames = new[] { "Heroic", "heroic", "Heroic Games", "HeroicGames", "GOG Games", "Epic Games" };
        foreach (var root in mountRoots)
        {
            foreach (var folderName in heroicFolderNames)
            {
                var targetDir = Path.Combine(root, folderName);
                if (Directory.Exists(targetDir))
                {
                    dirs.Add(Path.GetFullPath(targetDir));
                }
            }
        }

        return dirs.ToList();
    }

    private void AddHeroicGameFromElement(JsonElement elem, string defaultRunner, Dictionary<string, HeroicGameMetadata> metadataMap, List<string> activeConfigDirs, LinuxSteamLibraryScanner steamScanner, HashSet<string> scannedPaths, List<DiscoveredGameInfo> games, string fallbackAppId = "")
    {
        string appId = fallbackAppId;
        if (elem.TryGetProperty("appName", out var an)) appId = an.GetString() ?? appId;
        else if (elem.TryGetProperty("app_name", out var an2)) appId = an2.GetString() ?? appId;
        else if (elem.TryGetProperty("id", out var id)) appId = id.GetString() ?? appId;

        string title = "";
        if (elem.TryGetProperty("title", out var t)) title = t.GetString() ?? "";
        else if (elem.TryGetProperty("app_title", out var at)) title = at.GetString() ?? "";

        string installPath = "";
        if (elem.TryGetProperty("install_path", out var p)) installPath = p.GetString() ?? "";
        else if (elem.TryGetProperty("installPath", out var p2)) installPath = p2.GetString() ?? "";
        else if (elem.TryGetProperty("path", out var p3)) installPath = p3.GetString() ?? "";
        else if (elem.TryGetProperty("install", out var instElem) && instElem.TryGetProperty("install_path", out var p4)) installPath = p4.GetString() ?? "";

        if (string.IsNullOrEmpty(installPath) || !Directory.Exists(installPath))
        {
            return;
        }

        var canonical = Path.GetFullPath(installPath);
        if (scannedPaths.Contains(canonical))
        {
            return;
        }

        // If title is missing, lookup in metadata map
        if (string.IsNullOrEmpty(title) && metadataMap.TryGetValue(appId, out var meta))
        {
            title = meta.Title;
        }
        if (string.IsNullOrEmpty(title))
        {
            title = Path.GetFileName(canonical);
        }

        string? coverUrl = null;
        if (metadataMap.TryGetValue(appId, out var m2))
        {
            coverUrl = m2.CoverUrl;
        }
        else if (elem.TryGetProperty("art_cover", out var ac))
        {
            coverUrl = ac.GetString();
        }
        else if (elem.TryGetProperty("image", out var img))
        {
            coverUrl = img.GetString();
        }

        scannedPaths.Add(canonical);
        var coverImage = ResolveHeroicCoverImage(appId, title, canonical, coverUrl, activeConfigDirs);
        games.Add(CreateDiscoveredGame(appId, title, canonical, steamScanner, coverImage));
    }

    private void LoadHeroicMetadata(string configDir, Dictionary<string, HeroicGameMetadata> metadataMap)
    {
        // 1. GOG library cache
        var gogLibJson = Path.Combine(configDir, "store_cache", "gog_library.json");
        if (File.Exists(gogLibJson))
        {
            try
            {
                var json = File.ReadAllText(gogLibJson);
                using var doc = JsonDocument.Parse(json);
                JsonElement gamesArray = default;
                if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("games", out var gArr) && gArr.ValueKind == JsonValueKind.Array)
                {
                    gamesArray = gArr;
                }
                else if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    gamesArray = doc.RootElement;
                }

                if (gamesArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in gamesArray.EnumerateArray())
                    {
                        var appName = item.TryGetProperty("app_name", out var an) ? an.GetString() : null;
                        if (string.IsNullOrEmpty(appName)) continue;

                        var title = item.TryGetProperty("title", out var t) ? t.GetString() ?? appName : appName;
                        string? coverUrl = null;
                        if (item.TryGetProperty("art_cover", out var ac)) coverUrl = ac.GetString();
                        if (string.IsNullOrEmpty(coverUrl) && item.TryGetProperty("art_square", out var asq)) coverUrl = asq.GetString();

                        string? installPath = null;
                        if (item.TryGetProperty("install", out var inst) && inst.TryGetProperty("install_path", out var ip))
                        {
                            installPath = ip.GetString();
                        }

                        metadataMap[appName] = new HeroicGameMetadata(appName, title, coverUrl ?? "", installPath, "gog");
                    }
                }
            }
            catch { }
        }

        // 2. Legendary library cache
        var legLibJson = Path.Combine(configDir, "store_cache", "legendary_library.json");
        if (File.Exists(legLibJson))
        {
            try
            {
                var json = File.ReadAllText(legLibJson);
                using var doc = JsonDocument.Parse(json);
                JsonElement libArray = default;
                if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("library", out var lArr) && lArr.ValueKind == JsonValueKind.Array)
                {
                    libArray = lArr;
                }
                else if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    libArray = doc.RootElement;
                }

                if (libArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in libArray.EnumerateArray())
                    {
                        var appName = item.TryGetProperty("app_name", out var an) ? an.GetString() : null;
                        if (string.IsNullOrEmpty(appName)) continue;

                        var title = item.TryGetProperty("title", out var t) ? t.GetString() ?? appName : appName;
                        string? coverUrl = null;
                        if (item.TryGetProperty("art_cover", out var ac)) coverUrl = ac.GetString();
                        if (string.IsNullOrEmpty(coverUrl) && item.TryGetProperty("art_square", out var asq)) coverUrl = asq.GetString();

                        string? installPath = null;
                        if (item.TryGetProperty("install", out var inst) && inst.TryGetProperty("install_path", out var ip))
                        {
                            installPath = ip.GetString();
                        }

                        metadataMap[appName] = new HeroicGameMetadata(appName, title, coverUrl ?? "", installPath, "legendary");
                    }
                }
            }
            catch { }
        }

        // 3. Legendary individual metadata files (metadata/*.json)
        var metadataDirs = new[]
        {
            Path.Combine(configDir, "legendaryConfig", "legendary", "metadata"),
            Path.Combine(configDir, "legendary", "metadata")
        };

        foreach (var mDir in metadataDirs)
        {
            if (Directory.Exists(mDir))
            {
                try
                {
                    foreach (var mFile in Directory.GetFiles(mDir, "*.json"))
                    {
                        try
                        {
                            var json = File.ReadAllText(mFile);
                            using var doc = JsonDocument.Parse(json);
                            var root = doc.RootElement;
                            var appName = root.TryGetProperty("app_name", out var an) ? an.GetString() : Path.GetFileNameWithoutExtension(mFile);
                            if (string.IsNullOrEmpty(appName)) continue;

                            var title = root.TryGetProperty("app_title", out var at) ? at.GetString() ?? appName : appName;
                            string? coverUrl = null;

                            if (root.TryGetProperty("metadata", out var metaElem) && metaElem.TryGetProperty("keyImages", out var keyImgs) && keyImgs.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var img in keyImgs.EnumerateArray())
                                {
                                    var type = img.TryGetProperty("type", out var ty) ? ty.GetString() : null;
                                    var url = img.TryGetProperty("url", out var u) ? u.GetString() : null;
                                    if (type == "DieselGameBoxTall" && !string.IsNullOrEmpty(url))
                                    {
                                        coverUrl = url;
                                        break;
                                    }
                                    if (type == "DieselGameBox" && string.IsNullOrEmpty(coverUrl) && !string.IsNullOrEmpty(url))
                                    {
                                        coverUrl = url;
                                    }
                                }
                            }

                            if (!metadataMap.TryGetValue(appName, out var existingMeta) || string.IsNullOrEmpty(existingMeta.CoverUrl))
                            {
                                metadataMap[appName] = new HeroicGameMetadata(appName, title, coverUrl ?? "", null, "legendary");
                            }
                        }
                        catch { }
                    }
                }
                catch { }
            }
        }
    }

    private string ResolveHeroicCoverImage(string appId, string title, string installPath, string? coverUrl, List<string> activeConfigDirs)
    {
        // 1. If we have a cover URL from GOG/Epic/Nile store metadata
        if (!string.IsNullOrEmpty(coverUrl))
        {
            // Check if Heroic cached this image locally in images-cache/
            var sha256Hex = ComputeSha256(coverUrl);
            foreach (var configDir in activeConfigDirs)
            {
                var cachedImagePath = Path.Combine(configDir, "images-cache", sha256Hex);
                if (File.Exists(cachedImagePath))
                {
                    return cachedImagePath;
                }
            }

            // Return the online cover URL (ImageHelper will download, cache, and display it)
            return coverUrl;
        }

        // 2. Check Heroic icons folder for local app/game icons
        foreach (var configDir in activeConfigDirs)
        {
            var iconPath = Path.Combine(configDir, "icons", $"{appId}.png");
            if (File.Exists(iconPath)) return iconPath;

            var titleIconPath = Path.Combine(configDir, "icons", $"{title}.png");
            if (File.Exists(titleIconPath)) return titleIconPath;
        }

        // 3. Check game installation folder for standard cover/boxart files
        if (!string.IsNullOrEmpty(installPath) && Directory.Exists(installPath))
        {
            var localCandidates = new[]
            {
                Path.Combine(installPath, "library_600x900.jpg"),
                Path.Combine(installPath, "library_capsule.jpg"),
                Path.Combine(installPath, "cover.jpg"),
                Path.Combine(installPath, "cover.png"),
                Path.Combine(installPath, "poster.jpg"),
                Path.Combine(installPath, "poster.png"),
                Path.Combine(installPath, "boxart.jpg"),
                Path.Combine(installPath, "boxart.png")
            };

            foreach (var candidate in localCandidates)
            {
                if (File.Exists(candidate)) return candidate;
            }
        }

        return string.Empty;
    }

    private static string ComputeSha256(string rawData)
    {
        using var sha256 = SHA256.Create();
        byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }

    private DiscoveredGameInfo CreateDiscoveredGame(string appId, string title, string installPath, LinuxSteamLibraryScanner steamScanner, string coverImage)
    {
        return new DiscoveredGameInfo
        {
            AppId = appId,
            Name = title,
            InstallPath = installPath,
            Launcher = "Heroic",
            DLSSVersion = steamScanner.ScanDllVersion(installPath, "nvngx_dlss.dll"),
            DLSSGVersion = steamScanner.ScanDllVersion(installPath, "nvngx_dlssg.dll"),
            DLSSDVersion = steamScanner.ScanDllVersion(installPath, "nvngx_dlssd.dll"),
            Fsr31Dx12Version = steamScanner.ScanDllVersion(installPath, "amd_fidelityfx_dx12.dll", "ffx_fsr31_x64.dll", "ffx_fsr31_dx12_x64.dll"),
            Fsr31VkVersion = steamScanner.ScanDllVersion(installPath, "amd_fidelityfx_vk.dll", "ffx_fsr31_vk_x64.dll"),
            XessVersion = steamScanner.ScanDllVersion(installPath, "libxess.dll"),
            XessDx11Version = steamScanner.ScanDllVersion(installPath, "libxess_dx11.dll"),
            XessFgVersion = steamScanner.ScanDllVersion(installPath, "libxess_fg.dll"),
            XellVersion = steamScanner.ScanDllVersion(installPath, "libxell.dll"),
            CoverImagePath = coverImage
        };
    }
}
