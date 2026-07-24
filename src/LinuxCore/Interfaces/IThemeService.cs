using System;

namespace DLSS_Swapper.Core.Interfaces;

public enum AppThemeMode
{
    Default = 0,
    Light = 1,
    Dark = 2,
    HighContrast = 3
}

public interface IThemeService
{
    event EventHandler<AppThemeMode>? ThemeChanged;
    event EventHandler<bool>? ContrastChanged;

    AppThemeMode CurrentTheme { get; }
    bool IsHighContrast { get; }

    void StartWatching();
    void StopWatching();
}
