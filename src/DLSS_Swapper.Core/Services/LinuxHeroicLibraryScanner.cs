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
}
