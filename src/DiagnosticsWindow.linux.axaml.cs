using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using DLSS_Swapper.Core.Services;
using DLSS_Swapper.Helpers;

namespace DLSS_Swapper;

public partial class DiagnosticsWindow : Window
{
    public DiagnosticsWindow()
    {
        InitializeComponent();
        UpdateTranslations();
        GenerateDiagnosticsLog();
        LinuxLanguageService.Instance.OnLanguageChanged += UpdateTranslations;
    }

    private void UpdateTranslations()
    {
        var appTitle = ResourceHelper.GetString("ApplicationTitle", "DLSS Swapper");
        var diagTitle = ResourceHelper.GetString("DiagnosticsPage_WindowTitle", "Diagnostics");
        Title = $"{appTitle} - {diagTitle}";
        CopyDetailsButton.Content = ResourceHelper.GetString("DiagnosticsPage_ClickToCopyDetails", "Click to copy below details");
    }

    private void GenerateDiagnosticsLog()
    {
        var sb = new StringBuilder();
        sb.AppendLine("```");
        try
        {
            var appVersion = "1.2.5";
            sb.AppendLine(CultureInfo.InvariantCulture, $"DLSS Swapper: {appVersion}");
            sb.AppendLine("Portable: false");
            sb.AppendLine();

            sb.AppendLine("System");
            sb.AppendLine(CultureInfo.InvariantCulture, $"OS: {GetLinuxDistroInfo()}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"OSVariant: {GetLinuxOSVariant()}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Is64BitOperatingSystem: {Environment.Is64BitOperatingSystem}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Is64BitProcess: {Environment.Is64BitProcess}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Runtime: {Environment.Version}");

            sb.AppendLine();
            sb.AppendLine("Permissions");
            sb.AppendLine(CultureInfo.InvariantCulture, $"IsPrivilegedProcess: {Environment.IsPrivilegedProcess}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"IsInRole Administrator: false");
            sb.AppendLine(CultureInfo.InvariantCulture, $"IsInRole User: true");

            sb.AppendLine();
            sb.AppendLine("Paths");
            var configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "dlss-swapper");
            sb.AppendLine(CultureInfo.InvariantCulture, $"StoragePath: {configPath}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"CurrentDirectory: {Environment.CurrentDirectory}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"GetCurrentDirectory: {Directory.GetCurrentDirectory()}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"AppDomain.CurrentDomain.BaseDirectory: {AppDomain.CurrentDomain.BaseDirectory}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Assembly Location: {Assembly.GetExecutingAssembly().Location}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"ProcessPath: {Environment.ProcessPath ?? string.Empty}");
        }
        catch (Exception ex)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"ERROR: {ex.Message}");
        }
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("```");

        try
        {
            var settings = LinuxSettingsService.Instance.Settings;
            sb.AppendLine("Steam");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Status: {(settings.EnableSteam ? "Enabled" : "Disabled")}");
            sb.AppendLine("Games: Discovered");

            sb.AppendLine();
            sb.AppendLine("Heroic Games Launcher");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Status: {(settings.EnableHeroic ? "Enabled" : "Disabled")}");
            sb.AppendLine("Games: Discovered");

            sb.AppendLine();
            sb.AppendLine("Manually Added Games");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Status: {(settings.EnableManuallyAdded ? "Enabled" : "Disabled")}");
        }
        catch { }

        sb.AppendLine("```");

        DiagnosticsTextBox.Text = sb.ToString();
    }

    private static string GetLinuxDistroInfo()
    {
        string distroName = "Linux";
        if (File.Exists("/etc/os-release"))
        {
            try
            {
                foreach (var line in File.ReadAllLines("/etc/os-release"))
                {
                    if (line.StartsWith("PRETTY_NAME=", StringComparison.OrdinalIgnoreCase))
                    {
                        distroName = line.Substring("PRETTY_NAME=".Length).Trim('"');
                        break;
                    }
                    if (line.StartsWith("NAME=", StringComparison.OrdinalIgnoreCase) && distroName == "Linux")
                    {
                        distroName = line.Substring("NAME=".Length).Trim('"');
                    }
                }
            }
            catch { }
        }

        string kernel = Environment.OSVersion.VersionString;
        if (File.Exists("/proc/sys/kernel/osrelease"))
        {
            try
            {
                kernel = File.ReadAllText("/proc/sys/kernel/osrelease").Trim();
            }
            catch { }
        }

        string arch = Environment.Is64BitOperatingSystem ? "x86_64" : "x86";
        return $"{distroName} {arch} ({kernel})";
    }

    private static string GetLinuxOSVariant()
    {
        string id = string.Empty;
        string idLike = string.Empty;
        string arch = Environment.Is64BitOperatingSystem ? "x86_64" : "x86";

        if (File.Exists("/etc/os-release"))
        {
            try
            {
                foreach (var line in File.ReadAllLines("/etc/os-release"))
                {
                    if (line.StartsWith("ID=", StringComparison.OrdinalIgnoreCase))
                    {
                        id = line.Substring("ID=".Length).Trim('"').ToLowerInvariant();
                    }
                    else if (line.StartsWith("ID_LIKE=", StringComparison.OrdinalIgnoreCase))
                    {
                        idLike = line.Substring("ID_LIKE=".Length).Trim('"').ToLowerInvariant();
                    }
                }
            }
            catch { }
        }

        if (idLike.Contains("arch") || id == "arch" || id == "cachyos" || id == "manjaro" || id == "endeavouros" || id == "garuda")
            return $"Arch Linux {arch}";
        if (idLike.Contains("debian") || idLike.Contains("ubuntu") || id == "debian" || id == "ubuntu" || id == "pop" || id == "mint")
            return $"Debian {arch}";
        if (idLike.Contains("fedora") || idLike.Contains("rhel") || id == "fedora" || id == "bazzite" || id == "nobara" || id == "nobaralinux")
            return $"Fedora {arch}";
        if (idLike.Contains("suse") || id.Contains("suse"))
            return $"openSUSE {arch}";

        return !string.IsNullOrEmpty(id) ? $"{CultureInfo.CurrentCulture.TextInfo.ToTitleCase(id)} {arch}" : $"Linux {arch}";
    }

    private async void OnCopyDetailsClick(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(DiagnosticsTextBox.Text ?? string.Empty);
        }
    }

    private void OnRootGridPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        RootGrid.Focus();
    }
}
