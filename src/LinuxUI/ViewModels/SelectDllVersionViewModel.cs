using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLSS_Swapper.Core.Models;
using DLSS_Swapper.Core.Services;

namespace DLSS_Swapper.Avalonia.ViewModels;

public partial class DllVersionItemViewModel : ObservableObject
{
    private readonly LibraryStorageService _storageService;

    public string VersionName { get; set; } = string.Empty;
    public DllRecordModel Record { get; set; } = new();
    public string CategoryType { get; set; } = string.Empty;

    [ObservableProperty]
    private bool _isDownloaded;

    [ObservableProperty]
    private bool _isDownloading;

    [ObservableProperty]
    private double _downloadProgress;

    public Action? OnDownloadCompletedAction { get; set; }

    public DllVersionItemViewModel(DllRecordModel record, string categoryType, LibraryStorageService storageService)
    {
        Record = record;
        CategoryType = categoryType;
        _storageService = storageService;
        VersionName = record.DisplayName;
        IsDownloaded = storageService.IsDownloaded(categoryType, record);
    }

    [RelayCommand]
    public async Task DownloadAsync()
    {
        if (IsDownloading) return;
        IsDownloading = true;
        DownloadProgress = 0;
        try
        {
            await _storageService.DownloadAndExtractAsync(CategoryType, Record, p =>
            {
                DownloadProgress = p;
            });

            IsDownloaded = _storageService.IsDownloaded(CategoryType, Record);
            if (IsDownloaded)
            {
                OnDownloadCompletedAction?.Invoke();
            }
        }
        finally
        {
            IsDownloading = false;
        }
    }
}

public partial class SelectDllVersionViewModel : ObservableObject
{
    private readonly LibraryStorageService _storageService;
    private readonly string _categoryType;
    private readonly string _gameDllPath;

    [ObservableProperty]
    private string _title = "Select DLL version";

    [ObservableProperty]
    private string _originalVersionText = "Unknown";

    [ObservableProperty]
    private string _currentVersionText = "Not found";

    [ObservableProperty]
    private DllVersionItemViewModel? _selectedVersionItem;

    [ObservableProperty]
    private bool _canSwap;

    public ObservableCollection<DllVersionItemViewModel> Versions { get; } = new();

    public string? ResultSwappedPath { get; private set; }

    public Action? CloseWindowAction { get; set; }

    public SelectDllVersionViewModel(string categoryType, string gameDllPath, string currentVersion, LibraryStorageService storageService)
    {
        _categoryType = categoryType;
        _gameDllPath = gameDllPath;
        _storageService = storageService;

        Title = $"Select {GetDisplayNameForCategory(categoryType)} version";
        var formattedCurrent = FormatVersionString(currentVersion);
        CurrentVersionText = formattedCurrent;

        var backupPath = gameDllPath + ".bak";
        if (File.Exists(backupPath))
        {
            var scanner = new LinuxSteamLibraryScanner();
            var origVer = scanner.ExtractDllVersionFromFile(backupPath);
            OriginalVersionText = FormatVersionString(origVer);
        }
        else
        {
            OriginalVersionText = formattedCurrent;
        }

        _ = LoadVersionsAsync();
    }

    private static string FormatVersionString(string? version)
    {
        if (string.IsNullOrEmpty(version) || version == "Not found" || version == "N/A") return "Not found";
        var v = version.Trim();
        while (v.EndsWith(".0"))
        {
            v = v.Substring(0, v.Length - 2);
        }
        return v.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? v : "v" + v;
    }

    partial void OnSelectedVersionItemChanged(DllVersionItemViewModel? value)
    {
        UpdateCanSwap();
    }

    private string GetDisplayNameForCategory(string category)
    {
        return category.ToLowerInvariant() switch
        {
            "dlss" => "DLSS",
            "dlss_g" => "DLSS Frame Generation",
            "dlss_d" => "DLSS Ray Reconstruction",
            "fsr_31_dx12" => "FSR 3.1 DirectX 12",
            "fsr_31_vk" => "FSR 3.1 Vulkan",
            "xess" => "XeSS",
            "xess_dx11" => "XeSS (DX11)",
            "xess_fg" => "XeSS Frame Generation",
            "xell" => "XeLL",
            _ => category.ToUpper()
        };
    }

    private async Task LoadVersionsAsync()
    {
        var manifest = await _storageService.LoadManifestAsync();
        if (manifest == null) return;

        var records = manifest.GetRecordsForCategory(_categoryType)
            .OrderByDescending(r => r.VersionNumber)
            .ThenByDescending(r => r.Version);

        Versions.Clear();

        foreach (var record in records)
        {
            var item = new DllVersionItemViewModel(record, _categoryType, _storageService);
            item.OnDownloadCompletedAction = () =>
            {
                SelectedVersionItem = item;
                UpdateCanSwap();
            };
            Versions.Add(item);

            if (string.Equals(item.VersionName, CurrentVersionText, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(record.DisplayVersion, CurrentVersionText, StringComparison.OrdinalIgnoreCase))
            {
                SelectedVersionItem = item;
            }
        }

        UpdateCanSwap();
    }

    private void UpdateCanSwap()
    {
        if (SelectedVersionItem == null || !SelectedVersionItem.IsDownloaded)
        {
            CanSwap = false;
            return;
        }

        // Disable Swap if the selected version is identical to the currently installed version
        bool isSameVersion = string.Equals(SelectedVersionItem.VersionName, CurrentVersionText, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(SelectedVersionItem.Record.DisplayVersion, CurrentVersionText, StringComparison.OrdinalIgnoreCase);

        CanSwap = !isSameVersion;
    }

    [RelayCommand]
    private void ResetToOriginal()
    {
        var backupPath = _gameDllPath + ".bak";
        if (File.Exists(backupPath) && !string.IsNullOrEmpty(_gameDllPath))
        {
            try
            {
                File.Copy(backupPath, _gameDllPath, overwrite: true);
                File.Delete(backupPath);
                ResultSwappedPath = _gameDllPath;
                CloseWindowAction?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to restore backup: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private void OpenFolder()
    {
        if (string.IsNullOrEmpty(_gameDllPath)) return;
        var dir = Path.GetDirectoryName(_gameDllPath);
        if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = dir,
                UseShellExecute = true
            });
        }
    }

    [RelayCommand]
    private void Swap()
    {
        if (SelectedVersionItem == null || !SelectedVersionItem.IsDownloaded || string.IsNullOrEmpty(_gameDllPath)) return;

        try
        {
            var sourceDll = _storageService.GetExpectedDllPath(_categoryType, SelectedVersionItem.Record);
            if (!File.Exists(sourceDll)) return;

            var backupPath = _gameDllPath + ".bak";
            if (!File.Exists(backupPath) && File.Exists(_gameDllPath))
            {
                File.Copy(_gameDllPath, backupPath);
            }

            File.Copy(sourceDll, _gameDllPath, overwrite: true);

            ResultSwappedPath = _gameDllPath;
            CloseWindowAction?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to swap DLL: {ex.Message}");
        }
    }
}
