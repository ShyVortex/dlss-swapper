using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DLSS_Swapper.Data.NVIDIA;
using DLSS_Swapper.Core.Services;
using DLSS_Swapper.Helpers;

namespace DLSS_Swapper.Avalonia.Views;

public class NvidiaModelRowItem
{
    public bool IsChecked { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
    public string AssetTypeDisplay { get; set; } = string.Empty;
    public string VersionDisplay { get; set; } = string.Empty;
    public string SizeDisplay { get; set; } = string.Empty;
    public string StatusDisplay { get; set; } = string.Empty;

    public string DownloadUrl { get; set; } = string.Empty;
    public string LocalFilePath { get; set; } = string.Empty;
    public string CategoryKey { get; set; } = "dlss";
}

public partial class NGXModelImporter : Window
{
    private readonly bool _isDriverImport;
    public ObservableCollection<NvidiaModelRowItem> Items { get; } = new();
    public List<NvidiaModelRowItem> SelectedItems { get; private set; } = new();

    public NGXModelImporter() : this(true)
    {
    }

    public NGXModelImporter(bool isDriverImport)
    {
        _isDriverImport = isDriverImport;
        InitializeComponent();
        ModelsDataGrid.ItemsSource = Items;
        UpdateTranslations();
        LinuxLanguageService.Instance.OnLanguageChanged += UpdateTranslations;

        _ = LoadModelsAsync();
    }

    private void UpdateTranslations()
    {
        Title = _isDriverImport ? ResourceHelper.GetString("LibraryPage_ImportFromNVIDIADriver", "Import from NVIDIA driver") : ResourceHelper.GetString("LibraryPage_DownloadFromNVIDIA", "Download from NVIDIA");
        ActionButton.Content = _isDriverImport ? ResourceHelper.GetString("General_Import", "Import") : ResourceHelper.GetString("General_Download", "Download");
        CloseButton.Content = ResourceHelper.GetString("General_Close", "Close");
        if (ModelsDataGrid != null && ModelsDataGrid.Columns.Count >= 5)
        {
            ModelsDataGrid.Columns[1].Header = ResourceHelper.GetString("General_Name", "Asset Type");
            ModelsDataGrid.Columns[2].Header = ResourceHelper.GetString("General_Version", "Version");
            ModelsDataGrid.Columns[3].Header = ResourceHelper.GetString("General_Size", "Size");
            ModelsDataGrid.Columns[4].Header = ResourceHelper.GetString("General_Status", "Status");
        }
    }

    private async Task LoadModelsAsync()
    {
        LoadingBorder.IsVisible = true;
        LoadingTextBlock.Text = _isDriverImport 
            ? ResourceHelper.GetString("LibraryPage_Importing", "Scanning system driver NGX models...") 
            : ResourceHelper.GetString("LibraryPage_FetchingFileList", "Fetching file list...");

        try
        {
            if (_isDriverImport)
            {
                await ScanDriverModelsAsync();
            }
            else
            {
                await FetchServerModelsAsync();
            }
        }
        catch (Exception ex)
        {
            LoadingTextBlock.Text = $"Error: {ex.Message}";
            await Task.Delay(1500);
        }
        finally
        {
            LoadingBorder.IsVisible = false;
        }
    }

    private async Task ScanDriverModelsAsync()
    {
        await Task.Run(() =>
        {
            // Parse standard Linux driver paths and Proton/Wine caches
            var searchPaths = new[]
            {
                "/usr/lib/x86_64-linux-gnu",
                "/usr/lib64",
                "/usr/lib",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local/share/Steam"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".var/app/com.valvesoftware.Steam")
            };

            var foundFiles = new List<string>();
            foreach (var path in searchPaths)
            {
                if (Directory.Exists(path))
                {
                    try
                    {
                        var dlls = Directory.GetFiles(path, "nvngx_dlss*.dll", SearchOption.AllDirectories);
                        foundFiles.AddRange(dlls);
                    }
                    catch
                    {
                    }
                }
            }

            var seenVersions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in foundFiles.Distinct())
            {
                var fi = new FileInfo(file);
                var name = fi.Name.ToLowerInvariant();
                var catKey = name.Contains("dlssg") ? "dlss_g" : name.Contains("dlssd") ? "dlss_d" : "dlss";
                var assetDisplay = catKey switch
                {
                    "dlss_g" => "DLSS Frame Generation",
                    "dlss_d" => "DLSS Ray Reconstruction",
                    _ => "DLSS"
                };

                string versionDisplay;
                var peVersion = DLSS_Swapper.Core.Helpers.PeVersionReader.GetFileVersion(file);
                if (peVersion != null)
                {
                    versionDisplay = peVersion.ToString();
                }
                else
                {
                    versionDisplay = fi.Name;
                }

                // Deduplicate by category + version — each game ships a copy of the same DLL
                var dedupeKey = $"{catKey}|{versionDisplay}";
                if (!seenVersions.Add(dedupeKey))
                    continue;

                global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    Items.Add(new NvidiaModelRowItem
                    {
                        IsChecked = true,
                        AssetTypeDisplay = assetDisplay,
                        VersionDisplay = versionDisplay,
                        SizeDisplay = $"{fi.Length / (1024.0 * 1024.0):F2} MB",
                        StatusDisplay = "Available",
                        LocalFilePath = file,
                        CategoryKey = catKey
                    });
                });
            }
        });
    }

    private async Task FetchServerModelsAsync()
    {
        using var client = new HttpClient();
        var xmlStream = await client.GetStreamAsync("https://ngx.download.nvidia.com");
        var serializer = new XmlSerializer(typeof(ListBucketResult));
        var listResult = serializer.Deserialize(xmlStream) as ListBucketResult;

        if (listResult?.Contents != null)
        {
            var regex = new Regex(@"^d6e9b45e-d4f6-4a84-a460-bf61decae3e8\/(?<asset_type>dlss|dlssg|dlssd)\/versions\/(?<version_packed>\d*)\/files\/160_E658700\.bin$", RegexOptions.IgnoreCase);

            var entries = new List<(long versionInt, NvidiaModelRowItem item)>();

            foreach (var content in listResult.Contents)
            {
                if (content == null || content.Size == 0 || !content.Key.EndsWith("files/160_E658700.bin", StringComparison.OrdinalIgnoreCase))
                    continue;

                var match = regex.Match(content.Key);
                if (!match.Success) continue;

                var assetTypeStr = match.Groups["asset_type"].Value.ToLowerInvariant();
                var catKey = assetTypeStr switch
                {
                    "dlssg" => "dlss_g",
                    "dlssd" => "dlss_d",
                    _ => "dlss"
                };
                var assetDisplay = catKey switch
                {
                    "dlss_g" => "DLSS Frame Generation",
                    "dlss_d" => "DLSS Ray Reconstruction",
                    _ => "DLSS"
                };

                if (long.TryParse(match.Groups["version_packed"].Value, out var versionInt))
                {
                    var major = (versionInt >> 16) & 0xFFFF;
                    var minor = (versionInt >> 8) & 0xFF;
                    var build = versionInt & 0xFF;
                    var versionStr = $"{major}.{minor}.{build}.0";

                    entries.Add((versionInt, new NvidiaModelRowItem
                    {
                        IsChecked = true,
                        AssetTypeDisplay = assetDisplay,
                        VersionDisplay = versionStr,
                        SizeDisplay = $"{content.Size / (1024.0 * 1024.0):F2} MB",
                        StatusDisplay = "Ready to download",
                        DownloadUrl = $"https://ngx.download.nvidia.com/{content.Key}",
                        CategoryKey = catKey
                    }));
                }
            }

            // Sort by asset type then by version descending (latest first)
            var sorted = entries
                .OrderBy(e => e.item.CategoryKey)
                .ThenByDescending(e => e.versionInt)
                .Select(e => e.item);

            global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                foreach (var item in sorted)
                {
                    Items.Add(item);
                }
            });
        }
    }

    private void OnActionButtonClick(object? sender, RoutedEventArgs e)
    {
        SelectedItems = Items.Where(x => x.IsChecked).ToList();
        Close();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        SelectedItems.Clear();
        Close();
    }
}
