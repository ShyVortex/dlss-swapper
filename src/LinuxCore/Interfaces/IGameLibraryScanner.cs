using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DLSS_Swapper.Core.Interfaces;

public interface IGameLibraryScanner
{
    /// <summary>
    /// Scans for installed games for the associated library launcher.
    /// </summary>
    /// <param name="forceNeedsProcessing">Whether to force re-processing cached games.</param>
    /// <returns>A list of discovered games or paths.</returns>
    Task<List<string>> DiscoverGamePathsAsync(bool forceNeedsProcessing);

    /// <summary>
    /// Checks if the game library launcher is installed on the current operating system.
    /// </summary>
    bool IsLauncherInstalled();
}
