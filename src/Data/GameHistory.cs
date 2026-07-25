using System;
using SQLite;

namespace DLSS_Swapper.Data;

public enum GameHistoryEventType
{
    Unknown,
    DLLSwapped,
    DLLReset,
    DLLDetected,
    DLLChangedExternally,
    DLLBackupRemoved,
}

[Table("game_history")]
public class GameHistory
{
    [Indexed]
    [Column("game_id")]
    public string GameId { get; set; } = string.Empty;

    [Column("event_type")]
    public GameHistoryEventType EventType { get; set; } = GameHistoryEventType.Unknown;

    [Column("asset_type")]
    public GameAssetType? AssetType { get; set; }

    [Column("asset_path")]
    public string? AssetPath { get; set; }

    [Ignore]
    public string AssetTypeName
    {
        get
        {
            return AssetType.Value switch
            {
                GameAssetType.DLSS => "DLSS",
                GameAssetType.DLSS_G => "DLSS Frame Generation",
                GameAssetType.DLSS_D => "DLSS Ray Reconstruction",
                GameAssetType.FSR_31_DX12 => "FSR 3.1 DirectX 12",
                GameAssetType.FSR_31_VK => "FSR 3.1 Vulkan",
                GameAssetType.XeSS => "XeSS",
                GameAssetType.XeSS_DX11 => "XeSS (DX11)",
                GameAssetType.XeSS_FG => "XeSS Frame Generation",
                GameAssetType.XeLL => "XeLL",
                _ => AssetType.Value.ToString()
            };
        }
    }

    [Column("event_time")]
    public DateTime EventTime { get; set; } = DateTime.MinValue;

    [Column("asset_version")]
    public string? AssetVersion { get; set; }
}
