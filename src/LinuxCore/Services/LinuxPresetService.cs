using System;
using System.IO;
using System.Text.RegularExpressions;
using DLSS_Swapper.Core.Models;

namespace DLSS_Swapper.Core.Services;

public class LinuxPresetService
{
    private const string ENV_SR_KEY1 = "DXVK_NVAPI_SET_NGX_DLSS_SR_OVERRIDE_RENDER_PRESET_SELECTION";
    private const string ENV_SR_KEY2 = "DXVK_NVAPI_DRS_NGX_DLSS_SR_OVERRIDE_RENDER_PRESET_SELECTION";

    private const string ENV_RR_KEY1 = "DXVK_NVAPI_SET_NGX_DLSS_RR_OVERRIDE_RENDER_PRESET_SELECTION";
    private const string ENV_RR_KEY2 = "DXVK_NVAPI_DRS_NGX_DLSS_RR_OVERRIDE_RENDER_PRESET_SELECTION";

    private const string ENV_FG_KEY1 = "DXVK_NVAPI_SET_NGX_DLSS_FG_OVERRIDE_RENDER_PRESET_SELECTION";
    private const string ENV_FG_KEY2 = "DXVK_NVAPI_DRS_NGX_DLSS_FG_OVERRIDE_RENDER_PRESET_SELECTION";

    private static readonly Lazy<LinuxPresetService> _instance = new(() => new LinuxPresetService());
    public static LinuxPresetService Instance => _instance.Value;

    public List<DlssPresetItem> GetDlssSrPresets() => DlssPresetItem.GetSrPresetOptions();
    public List<DlssPresetItem> GetDlssRrPresets() => DlssPresetItem.GetRrPresetOptions();
    public List<DlssPresetItem> GetDlssFgPresets() => DlssPresetItem.GetFgPresetOptions();

    public bool IsSteamRunning()
    {
        try
        {
            var processes = System.Diagnostics.Process.GetProcesses();
            return processes.Any(p =>
            {
                try
                {
                    var name = p.ProcessName.ToLowerInvariant();
                    return name == "steam" || name == "steamwebhelper" || name.Contains("valvesoftware.steam");
                }
                catch
                {
                    return false;
                }
            });
        }
        catch
        {
            return false;
        }
    }

    public PresetSelectionState ReadGamePresets(string appId)
    {
        var result = new PresetSelectionState();
        if (string.IsNullOrEmpty(appId)) return result;

        var localConfigPath = FindSteamLocalConfigVdfPath();
        if (string.IsNullOrEmpty(localConfigPath) || !File.Exists(localConfigPath)) return result;

        try
        {
            var content = File.ReadAllText(localConfigPath);
            var launchOptions = ExtractLaunchOptionsForApp(content, appId);
            if (!string.IsNullOrEmpty(launchOptions))
            {
                result.SrPresetValue = ExtractEnvValue(launchOptions, ENV_SR_KEY1) ?? ExtractEnvValue(launchOptions, ENV_SR_KEY2);
                result.RrPresetValue = ExtractEnvValue(launchOptions, ENV_RR_KEY1) ?? ExtractEnvValue(launchOptions, ENV_RR_KEY2);
                result.FgPresetValue = ExtractEnvValue(launchOptions, ENV_FG_KEY1) ?? ExtractEnvValue(launchOptions, ENV_FG_KEY2);
            }
        }
        catch
        {
            // Fall back to default
        }

        return result;
    }

    public bool SaveGamePresets(string appId, string? srValue, string? rrValue, string? fgValue)
    {
        if (string.IsNullOrEmpty(appId)) return false;

        var localConfigPath = FindSteamLocalConfigVdfPath();
        if (string.IsNullOrEmpty(localConfigPath) || !File.Exists(localConfigPath)) return false;

        try
        {
            var content = File.ReadAllText(localConfigPath);
            var currentOptions = ExtractLaunchOptionsForApp(content, appId) ?? string.Empty;

            var newOptions = UpdateLaunchOptionsString(currentOptions, srValue, rrValue, fgValue);
            var updatedContent = InjectLaunchOptionsForApp(content, appId, newOptions);

            File.WriteAllText(localConfigPath, updatedContent);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private string UpdateLaunchOptionsString(string existingOptions, string? srValue, string? rrValue, string? fgValue)
    {
        var opts = existingOptions ?? string.Empty;

        // Strip out any existing DXVK_NVAPI preset environment variables first
        opts = Regex.Replace(opts, $@"{Regex.Escape(ENV_SR_KEY1)}=\S+", string.Empty);
        opts = Regex.Replace(opts, $@"{Regex.Escape(ENV_SR_KEY2)}=\S+", string.Empty);
        opts = Regex.Replace(opts, $@"{Regex.Escape(ENV_RR_KEY1)}=\S+", string.Empty);
        opts = Regex.Replace(opts, $@"{Regex.Escape(ENV_RR_KEY2)}=\S+", string.Empty);
        opts = Regex.Replace(opts, $@"{Regex.Escape(ENV_FG_KEY1)}=\S+", string.Empty);
        opts = Regex.Replace(opts, $@"{Regex.Escape(ENV_FG_KEY2)}=\S+", string.Empty);

        opts = Regex.Replace(opts, @"\s+", " ").Trim();
        if (opts == "%command%") opts = string.Empty;

        // Now build new prefix string for non-default values
        var newEnvs = new System.Collections.Generic.List<string>();

        if (!string.IsNullOrEmpty(srValue) && srValue != "0")
        {
            newEnvs.Add($"{ENV_SR_KEY1}={srValue}");
            newEnvs.Add($"{ENV_SR_KEY2}={srValue}");
        }

        if (!string.IsNullOrEmpty(rrValue) && rrValue != "0")
        {
            newEnvs.Add($"{ENV_RR_KEY1}={rrValue}");
            newEnvs.Add($"{ENV_RR_KEY2}={rrValue}");
        }

        if (!string.IsNullOrEmpty(fgValue) && fgValue != "0")
        {
            newEnvs.Add($"{ENV_FG_KEY1}={fgValue}");
            newEnvs.Add($"{ENV_FG_KEY2}={fgValue}");
        }

        if (newEnvs.Count == 0)
        {
            return opts; // Return original options minus our env vars
        }

        var prefix = string.Join(" ", newEnvs);

        if (string.IsNullOrEmpty(opts))
        {
            return $"{prefix} %command%";
        }
        else if (opts.Contains("%command%"))
        {
            var idx = opts.IndexOf("%command%", StringComparison.Ordinal);
            return opts.Insert(idx, $"{prefix} ");
        }
        else
        {
            return $"{prefix} {opts}";
        }
    }

    private string? ExtractEnvValue(string launchOptions, string envKey)
    {
        var match = Regex.Match(launchOptions, $@"{Regex.Escape(envKey)}=(\S+)");
        return match.Success ? match.Groups[1].Value : null;
    }

    private string? ExtractLaunchOptionsForApp(string vdfContent, string appId)
    {
        var appBlock = FindAppBlock(vdfContent, appId);
        if (string.IsNullOrEmpty(appBlock)) return null;

        var launchOptMatch = Regex.Match(appBlock, @"""LaunchOptions""\s*""([^""]*)""", RegexOptions.IgnoreCase);
        return launchOptMatch.Success ? launchOptMatch.Groups[1].Value : null;
    }

    private string InjectLaunchOptionsForApp(string vdfContent, string appId, string newLaunchOptions)
    {
        var appsMatch = Regex.Match(vdfContent, @"""UserLocalConfigStore"".*?""Software"".*?""Valve"".*?""Steam"".*?""apps""\s*\{", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (!appsMatch.Success) return vdfContent;

        var appsIndex = appsMatch.Index;
        var appsSub = vdfContent.Substring(appsIndex);
        var pattern = $@"""{Regex.Escape(appId)}""\s*\{{\s*(?:[^{{}}]|(?<open>\{{)|(?<-open>\}}))*(?(open)(?!))\}}";
        var match = Regex.Match(appsSub, pattern, RegexOptions.Singleline);

        if (match.Success)
        {
            var absoluteIndex = appsIndex + match.Index;
            var appBlock = match.Value;

            // Infer the indentation level from the app block's position in the file
            // Look at the line containing the appId to determine the block's indentation
            var blockAbsolutePos = absoluteIndex;
            var precedingText = vdfContent.Substring(0, blockAbsolutePos);
            var lastNl = precedingText.LastIndexOf('\n');
            var blockLineStart = lastNl >= 0 ? precedingText.Substring(lastNl + 1) : "";
            var blockIndentMatch = Regex.Match(blockLineStart, @"^(\t*)");
            var blockIndent = blockIndentMatch.Success ? blockIndentMatch.Groups[1].Value : "\t\t\t\t\t";
            // Keys inside the block are one tab deeper than the block itself
            var keyIndent = blockIndent + "\t";

            if (Regex.IsMatch(appBlock, @"""LaunchOptions""\s*""[^""]*""", RegexOptions.IgnoreCase))
            {
                string newAppBlock;
                if (string.IsNullOrEmpty(newLaunchOptions))
                {
                    // Remove LaunchOptions key-value pair including the preceding newline+indentation
                    newAppBlock = Regex.Replace(appBlock, @"\n\t*""LaunchOptions""\s*""[^""]*""", string.Empty, RegexOptions.IgnoreCase);
                }
                else
                {
                    // Replace only the value portion of the LaunchOptions key, preserving existing indentation
                    newAppBlock = Regex.Replace(appBlock, @"(""LaunchOptions""\s*)""[^""]*""",
                        "$1\"" + newLaunchOptions + "\"", RegexOptions.IgnoreCase);
                }
                return vdfContent.Remove(absoluteIndex, appBlock.Length).Insert(absoluteIndex, newAppBlock);
            }
            else
            {
                if (!string.IsNullOrEmpty(newLaunchOptions))
                {
                    // Insert LaunchOptions before closing brace, using inferred indentation
                    var insertPos = appBlock.LastIndexOf('}');
                    if (insertPos != -1)
                    {
                        // The text before } is typically "\n\t\t\t\t\t" (brace-level indent).
                        // We insert a new line for LaunchOptions before that trailing indent.
                        var insertion = $"\n{keyIndent}\"LaunchOptions\"\t\t\"{newLaunchOptions}\"\n{blockIndent}";
                        var newAppBlock = appBlock.Insert(insertPos, insertion);
                        return vdfContent.Remove(absoluteIndex, appBlock.Length).Insert(absoluteIndex, newAppBlock);
                    }
                }
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(newLaunchOptions))
            {
                // App block doesn't exist yet — create it after the apps opening brace
                // Infer indentation from the apps header match
                var headerInVdf = vdfContent.Substring(0, appsMatch.Index);
                var lastNewline = headerInVdf.LastIndexOf('\n');
                var appsLineStart = lastNewline >= 0 ? headerInVdf.Substring(lastNewline + 1) : "";
                var appsIndentMatch = Regex.Match(appsLineStart, @"^(\t*)");
                var appsIndent = appsIndentMatch.Success ? appsIndentMatch.Groups[1].Value : "\t\t\t\t";
                var appIndent = appsIndent + "\t";
                var keyIndent = appIndent + "\t";

                var openBraceIndex = appsMatch.Index + appsMatch.Length - 1;
                var newAppEntry = $"\n{appIndent}\"{appId}\"\n{appIndent}{{\n{keyIndent}\"LaunchOptions\"\t\t\"{newLaunchOptions}\"\n{appIndent}}}";
                return vdfContent.Insert(openBraceIndex + 1, newAppEntry);
            }
        }

        return vdfContent;
    }

    private string? FindAppBlock(string vdfContent, string appId)
    {
        var appsMatch = Regex.Match(vdfContent, @"""UserLocalConfigStore"".*?""Software"".*?""Valve"".*?""Steam"".*?""apps""\s*\{", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (!appsMatch.Success) return null;

        var appsSub = vdfContent.Substring(appsMatch.Index);
        var pattern = $@"""{Regex.Escape(appId)}""\s*\{{\s*(?:[^{{}}]|(?<open>\{{)|(?<-open>\}}))*(?(open)(?!))\}}";
        var match = Regex.Match(appsSub, pattern, RegexOptions.Singleline);
        return match.Success ? match.Value : null;
    }

    private string? FindSteamLocalConfigVdfPath()
    {
        var scanner = new LinuxSteamLibraryScanner();
        var steamPath = scanner.GetSteamInstallPath();
        if (string.IsNullOrEmpty(steamPath) || !Directory.Exists(steamPath)) return null;

        var userdataDir = Path.Combine(steamPath, "userdata");
        if (!Directory.Exists(userdataDir)) return null;

        var userSubdirs = Directory.GetDirectories(userdataDir);
        foreach (var userDir in userSubdirs)
        {
            var dirName = Path.GetFileName(userDir);
            if (dirName == "0" || !long.TryParse(dirName, out _)) continue;

            var localConfigPath = Path.Combine(userDir, "config", "localconfig.vdf");
            if (File.Exists(localConfigPath))
            {
                return localConfigPath;
            }
        }

        return null;
    }
}

public class PresetSelectionState
{
    public string? SrPresetValue { get; set; }
    public string? RrPresetValue { get; set; }
    public string? FgPresetValue { get; set; }
}
