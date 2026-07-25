using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DLSS_Swapper.Core.Services;

public class LinuxGameLauncherService
{
    public static bool IsProtonAutogenInstalled()
    {
        var localBin = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin", "proton-autogen");
        if (File.Exists(localBin)) return true;

        var usrBin = "/usr/bin/proton-autogen";
        if (File.Exists(usrBin)) return true;

        var usrLocalBin = "/usr/local/bin/proton-autogen";
        if (File.Exists(usrLocalBin)) return true;

        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "which",
                Arguments = "proton-autogen",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            proc?.WaitForExit(1000);
            return proc?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> InstallProtonAutogenAsync()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = "-c \"curl -fsSL https://raw.githubusercontent.com/N3oRay/proton-autogen/main/install.sh | bash\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc != null)
            {
                await proc.WaitForExitAsync();
                return proc.ExitCode == 0 || IsProtonAutogenInstalled();
            }
        }
        catch
        {
        }
        return IsProtonAutogenInstalled();
    }

    public static bool LaunchGame(string libraryName, string appId, string installPath, Func<Task<bool>>? confirmProtonAutogenInstallPrompt = null)
    {
        if (string.Equals(libraryName, "Steam", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrEmpty(appId))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "xdg-open",
                    Arguments = $"steam://run/{appId}",
                    UseShellExecute = true
                });
                return true;
            }
        }
        else if (string.Equals(libraryName, "Heroic", StringComparison.OrdinalIgnoreCase))
        {
            var launchUri = !string.IsNullOrEmpty(appId) ? $"heroic://launch/{appId}" : "heroic";
            Process.Start(new ProcessStartInfo
            {
                FileName = "xdg-open",
                Arguments = launchUri,
                UseShellExecute = true
            });
            return true;
        }

        // Manual game launch via proton-autogen
        _ = Task.Run(async () =>
        {
            if (!IsProtonAutogenInstalled())
            {
                if (confirmProtonAutogenInstallPrompt != null)
                {
                    var userApproved = await confirmProtonAutogenInstallPrompt();
                    if (userApproved)
                    {
                        var installed = await InstallProtonAutogenAsync();
                        if (!installed)
                        {
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }

            // Find target executable in installPath
            var targetExe = FindMainExecutable(installPath);
            if (string.IsNullOrEmpty(targetExe)) return;

            var protonAutogenCmd = IsProtonAutogenInstalled() ? "proton-autogen" : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin", "proton-autogen");

            Process.Start(new ProcessStartInfo
            {
                FileName = protonAutogenCmd,
                Arguments = $"\"{targetExe}\"",
                UseShellExecute = true
            });
        });

        return true;
    }

    private static string? FindMainExecutable(string installPath)
    {
        if (string.IsNullOrEmpty(installPath)) return null;

        if (File.Exists(installPath) && installPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            return installPath;
        }

        if (Directory.Exists(installPath))
        {
            var exeFiles = Directory.GetFiles(installPath, "*.exe", SearchOption.AllDirectories)
                .Where(f => !f.Contains("CrashReport", StringComparison.OrdinalIgnoreCase) &&
                            !f.Contains("Unins", StringComparison.OrdinalIgnoreCase) &&
                            !f.Contains("UnityCrashHandler", StringComparison.OrdinalIgnoreCase) &&
                            !f.Contains("DXSETUP", StringComparison.OrdinalIgnoreCase) &&
                            !f.Contains("vcredist", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (exeFiles.Count > 0)
            {
                // Prefer root executables first
                var rootExe = exeFiles.FirstOrDefault(f => Path.GetDirectoryName(f) == installPath);
                return rootExe ?? exeFiles[0];
            }
        }

        return null;
    }
}
