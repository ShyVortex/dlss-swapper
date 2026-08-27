using System;
using System.Reflection;

namespace DLSS_Swapper.LinuxCore.Helpers;

public static class AppVersionHelper
{
    private static Version? _cachedVersion;

    public static Version Version
    {
        get
        {
            if (_cachedVersion == null)
            {
                _cachedVersion = Assembly.GetEntryAssembly()?.GetName().Version
                                 ?? Assembly.GetExecutingAssembly().GetName().Version
                                 ?? new Version(1, 2, 6, 1);
            }
            return _cachedVersion;
        }
    }

    public static string GetVersionString()
    {
        var version = Version;
        if (version.Build <= 0 && version.Revision <= 0)
        {
            return $"{version.Major}.{version.Minor}";
        }
        else if (version.Revision <= 0)
        {
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }
        return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }

    public static string GetUserAgent()
    {
        return $"Mozilla/5.0 (X11; Linux x86_64) DLSS-Swapper/{GetVersionString()}";
    }
}
