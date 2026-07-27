using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DLSS_Swapper.Core.Services;

public enum LinuxAppTheme
{
    Default,
    Light,
    Dark
}

public enum LinuxLoggingLevel
{
    Off,
    Verbose,
    Debug,
    Info,
    Warning,
    Error
}

public class LinuxSettingsData
{
    public LinuxAppTheme AppTheme { get; set; } = LinuxAppTheme.Default;
    public string AccentColor { get; set; } = "#E85A24";
    public string Language { get; set; } = "en-US";
    public bool EnableSteam { get; set; } = true;
    public bool EnableHeroic { get; set; } = true;
    public bool EnableGog { get; set; } = true;
    public bool EnableEpic { get; set; } = true;
    public bool EnableManuallyAdded { get; set; } = true;
    public List<string> IgnoredPaths { get; set; } = new();
    public string DlssPreset { get; set; } = "Default";
    public string DlssDPreset { get; set; } = "Default";
    public string DlssGPreset { get; set; } = "Default";
    public int IndicatorOption { get; set; } = 0;
    public bool EnableLoggingToFile { get; set; } = false;
    public bool VerboseLogging { get; set; } = false;
    public bool EnableLoggingToConsole { get; set; } = false;
    public bool AllowUntrusted { get; set; } = false;
    public bool AllowDebugDlls { get; set; } = false;
    public bool OnlyShowDownloadedDlls { get; set; } = false;
    public LinuxLoggingLevel LoggingLevel { get; set; } = LinuxLoggingLevel.Error;
}

public class LinuxSettingsService
{
    private static readonly Lazy<LinuxSettingsService> _instance = new(() => new LinuxSettingsService());
    public static LinuxSettingsService Instance => _instance.Value;

    private readonly string _settingsFilePath;
    public LinuxSettingsData Settings { get; private set; }

    public LinuxSettingsService()
    {
        var configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config",
            "dlss-swapper"
        );

        Directory.CreateDirectory(configDir);
        _settingsFilePath = Path.Combine(configDir, "settings.json");
        Settings = LoadSettings();
    }

    private LinuxSettingsData LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                var data = JsonSerializer.Deserialize<LinuxSettingsData>(json);
                if (data != null)
                    return data;
            }
        }
        catch { }

        return new LinuxSettingsData();
    }

    public void SaveSettings()
    {
        try
        {
            var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFilePath, json);
        }
        catch { }
    }
}
