using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DLSS_Swapper.Core.Models;

public class ManifestModel
{
    [JsonPropertyName("dlss")]
    public List<DllRecordModel> Dlss { get; set; } = new();

    [JsonPropertyName("dlss_g")]
    public List<DllRecordModel> DlssG { get; set; } = new();

    [JsonPropertyName("dlss_d")]
    public List<DllRecordModel> DlssD { get; set; } = new();

    [JsonPropertyName("fsr_31_dx12")]
    public List<DllRecordModel> Fsr31Dx12 { get; set; } = new();

    [JsonPropertyName("fsr_31_vk")]
    public List<DllRecordModel> Fsr31Vk { get; set; } = new();

    [JsonPropertyName("xess")]
    public List<DllRecordModel> Xess { get; set; } = new();

    [JsonPropertyName("xess_dx11")]
    public List<DllRecordModel> XessDx11 { get; set; } = new();

    [JsonPropertyName("xess_fg")]
    public List<DllRecordModel> XessFg { get; set; } = new();

    [JsonPropertyName("xell")]
    public List<DllRecordModel> Xell { get; set; } = new();

    public List<DllRecordModel> GetRecordsForCategory(string category)
    {
        return category.ToLowerInvariant() switch
        {
            "dlss" => Dlss ?? new(),
            "dlss_g" => DlssG ?? new(),
            "dlss_d" => DlssD ?? new(),
            "fsr_31_dx12" => Fsr31Dx12 ?? new(),
            "fsr_31_vk" => Fsr31Vk ?? new(),
            "xess" => Xess ?? new(),
            "xess_dx11" => XessDx11 ?? new(),
            "xess_fg" => XessFg ?? new(),
            "xell" => Xell ?? new(),
            _ => new()
        };
    }
}

public class DllRecordModel
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("version_number")]
    public ulong VersionNumber { get; set; }

    [JsonPropertyName("internal_name")]
    public string InternalName { get; set; } = string.Empty;

    [JsonPropertyName("additional_label")]
    public string AdditionalLabel { get; set; } = string.Empty;

    [JsonPropertyName("md5_hash")]
    public string Md5Hash { get; set; } = string.Empty;

    [JsonPropertyName("zip_md5_hash")]
    public string ZipMd5Hash { get; set; } = string.Empty;

    [JsonPropertyName("download_url")]
    public string DownloadUrl { get; set; } = string.Empty;

    [JsonPropertyName("file_description")]
    public string FileDescription { get; set; } = string.Empty;

    [JsonPropertyName("is_dev_file")]
    public bool IsDevFile { get; set; }

    [JsonPropertyName("file_size")]
    public long FileSize { get; set; }

    [JsonPropertyName("zip_file_size")]
    public long ZipFileSize { get; set; }

    public string DisplayVersion
    {
        get
        {
            if (!string.IsNullOrEmpty(Version))
            {
                var v = Version.Trim();
                while (v.EndsWith(".0"))
                {
                    v = v.Substring(0, v.Length - 2);
                }
                return v.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? v : "v" + v;
            }
            return "v1.0";
        }
    }

    public string DisplayName => IsDevFile ? $"{DisplayVersion} (Debug)" : DisplayVersion;
}
