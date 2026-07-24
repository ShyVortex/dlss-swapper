using System;
using DLSS_Swapper.Core.Interfaces;

namespace DLSS_Swapper.Core.Services;

public class LinuxThemeService : IThemeService
{
    public event EventHandler<AppThemeMode>? ThemeChanged;
    public event EventHandler<bool>? ContrastChanged;

    public AppThemeMode CurrentTheme { get; private set; } = AppThemeMode.Dark;
    public bool IsHighContrast => false;

    public LinuxThemeService()
    {
        DetectLinuxTheme();
    }

    private void DetectLinuxTheme()
    {
        var gtkTheme = Environment.GetEnvironmentVariable("GTK_THEME")?.ToLowerInvariant();
        if (!string.IsNullOrEmpty(gtkTheme) && gtkTheme.Contains("light"))
        {
            CurrentTheme = AppThemeMode.Light;
        }
        else
        {
            CurrentTheme = AppThemeMode.Dark;
        }
    }

    public void StartWatching()
    {
        // On Linux, notify initial theme state
        ThemeChanged?.Invoke(this, CurrentTheme);
        ContrastChanged?.Invoke(this, false);
    }

    public void StopWatching()
    {
    }
}
