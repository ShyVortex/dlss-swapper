using System.Collections.Generic;

namespace DLSS_Swapper.Core.Models;

public class DlssPresetItem
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty; // e.g. "0", "1", "10", "16777215"
    public string EnvironmentValue { get; set; } = string.Empty;

    public DlssPresetItem() { }

    public DlssPresetItem(string name, string value, string? envVal = null)
    {
        Name = name;
        Value = value;
        EnvironmentValue = envVal ?? value;
    }

    public override string ToString() => Name;

    public static List<DlssPresetItem> GetSrPresetOptions()
    {
        return new List<DlssPresetItem>
        {
            new DlssPresetItem("Default", "0"),
            new DlssPresetItem("Preset A", "1"),
            new DlssPresetItem("Preset B", "2"),
            new DlssPresetItem("Preset C", "3"),
            new DlssPresetItem("Preset D", "4"),
            new DlssPresetItem("Preset E", "5"),
            new DlssPresetItem("Preset F", "6"),
            new DlssPresetItem("Preset J (DLSS 4)", "10"),
            new DlssPresetItem("Preset K (DLSS 4)", "11"),
            new DlssPresetItem("Preset L (DLSS 4.5)", "12"),
            new DlssPresetItem("Preset M (DLSS 4.5)", "13"),
            new DlssPresetItem("NVIDIA recommended", "16777215", "16777215")
        };
    }

    public static List<DlssPresetItem> GetRrPresetOptions()
    {
        return new List<DlssPresetItem>
        {
            new DlssPresetItem("Default", "0"),
            new DlssPresetItem("Preset D", "4"),
            new DlssPresetItem("Preset E", "5"),
            new DlssPresetItem("NVIDIA recommended", "16777215", "16777215")
        };
    }

    public static List<DlssPresetItem> GetFgPresetOptions()
    {
        return new List<DlssPresetItem>
        {
            new DlssPresetItem("Default", "0"),
            new DlssPresetItem("Preset A", "1"),
            new DlssPresetItem("Preset B", "2"),
            new DlssPresetItem("NVIDIA recommended", "16777214", "16777214")
        };
    }
}
