using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLSS_Swapper.Core.Models;
using DLSS_Swapper.Core.Services;

namespace DLSS_Swapper.Avalonia.ViewModels;

public partial class LibraryCategoryItem : ObservableObject
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    [ObservableProperty]
    private bool _isSelected;
}

public partial class DllRecordCardItem : ObservableObject
{
    public string CategoryKey { get; set; } = string.Empty;
    public DllRecordModel Record { get; set; } = new();

    public string VersionText => Record.DisplayName;

    [ObservableProperty]
    private bool _isDownloaded;

    [ObservableProperty]
    private bool _isDownloading;

    [ObservableProperty]
    private double _downloadProgress;

    [ObservableProperty]
    private bool _hasError;
}

public partial class LibraryViewModel : ObservableObject
{
    private readonly LibraryStorageService _storageService = new();
    private ManifestModel? _manifest;

    public ObservableCollection<LibraryCategoryItem> Categories { get; } = new();
    public ObservableCollection<DllRecordCardItem> VisibleRecords { get; } = new();

    [ObservableProperty]
    private LibraryCategoryItem? _selectedCategory;

    [ObservableProperty]
    private bool _isLoading = true;

    public LibraryViewModel()
    {
        InitializeCategories();
        _ = LoadManifestAndRecordsAsync();
    }

    private void InitializeCategories()
    {
        var items = new List<LibraryCategoryItem>
        {
            new() { Key = "dlss", Name = "DLSS", IsSelected = true },
            new() { Key = "dlss_g", Name = "DLSS Frame Generation" },
            new() { Key = "dlss_d", Name = "DLSS Ray Reconstruction" },
            new() { Key = "fsr_31_dx12", Name = "FSR 3.1 DirectX 12" },
            new() { Key = "fsr_31_vk", Name = "FSR 3.1 Vulkan" },
            new() { Key = "xess", Name = "XeSS" },
            new() { Key = "xess_dx11", Name = "XeSS (DX11)" },
            new() { Key = "xess_fg", Name = "XeSS Frame Generation" },
            new() { Key = "xell", Name = "XeLL" }
        };

        foreach (var c in items)
        {
            Categories.Add(c);
        }

        SelectedCategory = Categories.First();
    }

    [RelayCommand]
    private void SelectCategory(LibraryCategoryItem category)
    {
        if (category == null || category == SelectedCategory) return;

        foreach (var c in Categories)
        {
            c.IsSelected = (c == category);
        }

        SelectedCategory = category;
        UpdateVisibleRecords();
    }

    public async Task LoadManifestAndRecordsAsync()
    {
        IsLoading = true;
        _manifest = await _storageService.LoadManifestAsync();
        UpdateVisibleRecords();
        IsLoading = false;
    }

    private void UpdateVisibleRecords()
    {
        VisibleRecords.Clear();
        if (_manifest == null || SelectedCategory == null) return;

        List<DllRecordModel> records = SelectedCategory.Key switch
        {
            "dlss" => _manifest.Dlss,
            "dlss_g" => _manifest.DlssG,
            "dlss_d" => _manifest.DlssD,
            "fsr_31_dx12" => _manifest.Fsr31Dx12,
            "fsr_31_vk" => _manifest.Fsr31Vk,
            "xess" => _manifest.Xess,
            "xess_dx11" => _manifest.XessDx11,
            "xess_fg" => _manifest.XessFg,
            "xell" => _manifest.Xell,
            _ => _manifest.Dlss
        };

        var sortedRecords = records
            .OrderByDescending(r => r.VersionNumber)
            .ThenByDescending(r => r.Version);

        foreach (var r in sortedRecords)
        {
            var card = new DllRecordCardItem
            {
                CategoryKey = SelectedCategory.Key,
                Record = r,
                IsDownloaded = _storageService.IsDownloaded(SelectedCategory.Key, r)
            };
            VisibleRecords.Add(card);
        }
    }

    [RelayCommand]
    public async Task DownloadRecordAsync(DllRecordCardItem card)
    {
        if (card == null || card.IsDownloaded || card.IsDownloading) return;

        card.IsDownloading = true;
        card.HasError = false;
        card.DownloadProgress = 0;

        var success = await _storageService.DownloadAndExtractAsync(card.CategoryKey, card.Record, p =>
        {
            card.DownloadProgress = p;
        });

        card.IsDownloading = false;
        card.IsDownloaded = success;
        card.HasError = !success;
    }

    [RelayCommand]
    public void DeleteRecord(DllRecordCardItem card)
    {
        if (card == null || !card.IsDownloaded) return;

        var success = _storageService.DeleteRecord(card.CategoryKey, card.Record);
        if (success)
        {
            card.IsDownloaded = false;
        }
    }

    [RelayCommand]
    public void OpenDownloadedFolder(DllRecordCardItem card)
    {
        if (card == null || !card.IsDownloaded) return;
        var folder = _storageService.GetExpectedRecordFolder(card.CategoryKey, card.Record);
        if (Directory.Exists(folder))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = folder,
                UseShellExecute = true
            });
        }
    }

    [RelayCommand]
    public async Task DownloadLatestAsync()
    {
        var latest = VisibleRecords.FirstOrDefault(r => !r.IsDownloaded && !r.IsDownloading);
        if (latest != null)
        {
            await DownloadRecordAsync(latest);
        }
    }

    [RelayCommand]
    public void ExportAll()
    {
        var folder = LibraryStorageService.DllsFolder;
        if (Directory.Exists(folder))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = folder,
                UseShellExecute = true
            });
        }
    }

    [RelayCommand]
    public void Import()
    {
        var folder = LibraryStorageService.DllsFolder;
        if (Directory.Exists(folder))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = folder,
                UseShellExecute = true
            });
        }
    }

    [RelayCommand]
    public async Task RefreshManifestAsync()
    {
        await LoadManifestAndRecordsAsync();
    }
}
