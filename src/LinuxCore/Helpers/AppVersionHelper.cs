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
                                 ?? new Version(1, 2, 6, 0);
            }
            return _cachedVersion;
        }
    }

    public static string GetVersionString(int fieldCount = 3)
    {
        var ver = Version;
        if (fieldCount == 3)
        {
            return $"{ver.Major}.{ver.Minor}.{Math.Max(0, ver.Build)}";
        }
        else if (fieldCount == 2)
        {
            return $"{ver.Major}.{ver.Minor}";
        }
        return $"{ver.Major}.{ver.Minor}.{Math.Max(0, ver.Build)}.{Math.Max(0, ver.Revision)}";
    }

    public static string GetUserAgent()
    {
        return $"Mozilla/5.0 (X11; Linux x86_64) DLSS-Swapper/{GetVersionString(3)}";
    }
}
