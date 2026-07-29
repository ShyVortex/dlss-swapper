using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DLSS_Swapper.Core.Interfaces;

namespace DLSS_Swapper.Core.Services;

public class LinuxHeroicLibraryScanner : IGameLibraryScanner
{
    private static readonly string HeroicConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".config",
        "heroic"
    );

    public bool IsLauncherInstalled()
    {
        return Directory.Exists(HeroicConfigDir);
    }

    public Task<List<string>> DiscoverGamePathsAsync(bool forceNeedsProcessing)
    {
        var discoveredConfigFiles = new List<string>();

        if (!IsLauncherInstalled())
        {
            return Task.FromResult(discoveredConfigFiles);
        }

        var gogInstalledJson = Path.Combine(HeroicConfigDir, "gog_store", "installed.json");
        if (File.Exists(gogInstalledJson))
        {
            discoveredConfigFiles.Add(gogInstalledJson);
        }

        var legendaryInstalledJson = Path.Combine(HeroicConfigDir, "legendaryConfig", "legendary", "installed.json");
        if (File.Exists(legendaryInstalledJson))
        {
            discoveredConfigFiles.Add(legendaryInstalledJson);
        }

        return Task.FromResult(discoveredConfigFiles);
    }

    public List<DiscoveredGameInfo> ScanInstalledGames()
    {
        var games = new List<DiscoveredGameInfo>();
        if (!IsLauncherInstalled()) return games;

        var steamScanner = new LinuxSteamLibraryScanner();

        // 1. GOG Store in Heroic
        var gogInstalledJson = Path.Combine(HeroicConfigDir, "gog_store", "installed.json");
        if (File.Exists(gogInstalledJson))
        {
            try
            {
                var jsonText = File.ReadAllText(gogInstalledJson);
                using var doc = System.Text.Json.JsonDocument.Parse(jsonText);
                if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        var elem = prop.Value;
                        string title = elem.TryGetProperty("title", out var t) ? t.GetString() ?? prop.Name : prop.Name;
                        string installPath = elem.TryGetProperty("install_path", out var p) ? p.GetString() ?? string.Empty : string.Empty;

                        if (!string.IsNullOrEmpty(installPath) && Directory.Exists(installPath))
                        {
                            var info = new DiscoveredGameInfo
                            {
                                AppId = prop.Name,
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
                                XellVersion = steamScanner.ScanDllVersion(installPath, "libxell.dll")
                            };
                            games.Add(info);
                        }
                    }
                }
            }
            catch { }
        }

        // 2. Legendary (Epic) Store in Heroic
        var legendaryInstalledJson = Path.Combine(HeroicConfigDir, "legendaryConfig", "legendary", "installed.json");
        if (File.Exists(legendaryInstalledJson))
        {
            try
            {
                var jsonText = File.ReadAllText(legendaryInstalledJson);
                using var doc = System.Text.Json.JsonDocument.Parse(jsonText);
                if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        var elem = prop.Value;
                        string title = elem.TryGetProperty("title", out var t) ? t.GetString() ?? prop.Name : prop.Name;
                        string installPath = elem.TryGetProperty("install_path", out var p) ? p.GetString() ?? string.Empty : string.Empty;

                        if (!string.IsNullOrEmpty(installPath) && Directory.Exists(installPath))
                        {
                            var info = new DiscoveredGameInfo
                            {
                                AppId = prop.Name,
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
                                XellVersion = steamScanner.ScanDllVersion(installPath, "libxell.dll")
                            };
                            games.Add(info);
                        }
                    }
                }
            }
            catch { }
        }

        DLSS_Swapper.Logger.Info($"Scanned {games.Count} installed Heroic games.");
        return games;
    }
}
