using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace DLSS_Swapper.LinuxCore.Services;

public class NgxDeveloperOptions
{
    public int IndicatorOption { get; set; } = 0; // 0=None, 1=Debug DLL only, 1024=All DLLs
    public bool EnableLoggingToFile { get; set; } = false;
    public bool VerboseLogging { get; set; } = false;
    public bool EnableLoggingToConsole { get; set; } = false;
}

public static class ProtonRegistryService
{
    private const string TargetSection = "[Software\\\\NVIDIA Corporation\\\\Global\\\\NGXCore]";

    public static NgxDeveloperOptions ReadDeveloperOptions(string userRegPath)
    {
        var options = new NgxDeveloperOptions();
        if (string.IsNullOrEmpty(userRegPath) || !File.Exists(userRegPath))
        {
            return options;
        }

        try
        {
            var lines = File.ReadAllLines(userRegPath);
            bool inSection = false;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("["))
                {
                    inSection = string.Equals(trimmed.Split(' ')[0], TargetSection, StringComparison.OrdinalIgnoreCase);
                    continue;
                }

                if (inSection)
                {
                    if (trimmed.StartsWith("\"ShowOnScreenIndicator\"=", StringComparison.OrdinalIgnoreCase))
                    {
                        options.IndicatorOption = ParseDword(trimmed);
                    }
                    else if (trimmed.StartsWith("\"EnableLoggingToFile\"=", StringComparison.OrdinalIgnoreCase))
                    {
                        options.EnableLoggingToFile = ParseDword(trimmed) != 0;
                    }
                    else if (trimmed.StartsWith("\"VerboseLogging\"=", StringComparison.OrdinalIgnoreCase))
                    {
                        options.VerboseLogging = ParseDword(trimmed) != 0;
                    }
                    else if (trimmed.StartsWith("\"EnableLoggingToConsoleWindow\"=", StringComparison.OrdinalIgnoreCase))
                    {
                        options.EnableLoggingToConsole = ParseDword(trimmed) != 0;
                    }
                }
            }
        }
        catch { }

        return options;
    }

    public static void WriteDeveloperOptions(string userRegPath, NgxDeveloperOptions options)
    {
        if (string.IsNullOrEmpty(userRegPath) || !File.Exists(userRegPath))
        {
            return;
        }

        try
        {
            var lines = new List<string>(File.ReadAllLines(userRegPath));
            int sectionStartIndex = -1;
            int sectionEndIndex = -1;

            for (int i = 0; i < lines.Count; i++)
            {
                var trimmed = lines[i].Trim();
                if (trimmed.StartsWith("["))
                {
                    if (string.Equals(trimmed.Split(' ')[0], TargetSection, StringComparison.OrdinalIgnoreCase))
                    {
                        sectionStartIndex = i;
                    }
                    else if (sectionStartIndex != -1)
                    {
                        sectionEndIndex = i;
                        break;
                    }
                }
            }

            var newKeys = new List<string>
            {
                $"\"ShowOnScreenIndicator\"=dword:{options.IndicatorOption:x8}",
                $"\"EnableLoggingToFile\"=dword:{(options.EnableLoggingToFile ? 1 : 0):x8}",
                $"\"VerboseLogging\"=dword:{(options.VerboseLogging ? 1 : 0):x8}",
                $"\"EnableLoggingToConsoleWindow\"=dword:{(options.EnableLoggingToConsole ? 1 : 0):x8}"
            };

            if (sectionStartIndex != -1)
            {
                int end = sectionEndIndex != -1 ? sectionEndIndex : lines.Count;
                // Filter out old NGX keys
                var existingNonNgx = new List<string>();
                for (int i = sectionStartIndex + 1; i < end; i++)
                {
                    var l = lines[i].Trim();
                    if (!l.StartsWith("\"ShowOnScreenIndicator\"=", StringComparison.OrdinalIgnoreCase) &&
                        !l.StartsWith("\"EnableLoggingToFile\"=", StringComparison.OrdinalIgnoreCase) &&
                        !l.StartsWith("\"VerboseLogging\"=", StringComparison.OrdinalIgnoreCase) &&
                        !l.StartsWith("\"EnableLoggingToConsoleWindow\"=", StringComparison.OrdinalIgnoreCase))
                    {
                        existingNonNgx.Add(lines[i]);
                    }
                }

                lines.RemoveRange(sectionStartIndex + 1, end - (sectionStartIndex + 1));
                lines.InsertRange(sectionStartIndex + 1, existingNonNgx);
                lines.InsertRange(sectionStartIndex + 1 + existingNonNgx.Count, newKeys);
            }
            else
            {
                // Section does not exist, append at the end
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                lines.Add("");
                lines.Add($"{TargetSection} {timestamp}");
                lines.AddRange(newKeys);
            }

            File.WriteAllLines(userRegPath, lines, Encoding.UTF8);
        }
        catch { }
    }

    private static int ParseDword(string line)
    {
        var parts = line.Split('=');
        if (parts.Length < 2) return 0;
        var valStr = parts[1].Trim();
        if (valStr.StartsWith("dword:", StringComparison.OrdinalIgnoreCase))
        {
            var hex = valStr.Substring("dword:".Length);
            if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var val))
            {
                return val;
            }
        }
        return 0;
    }
}
