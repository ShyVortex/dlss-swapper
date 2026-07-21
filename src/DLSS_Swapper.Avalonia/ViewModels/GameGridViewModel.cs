using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLSS_Swapper.Avalonia.Helpers;
using DLSS_Swapper.Core.Services;

namespace DLSS_Swapper.Avalonia.ViewModels;

public partial class GameCardItem : ObservableObject
{
    public string AppId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DLSSVersion { get; set; } = string.Empty;
    public string LibraryName { get; set; } = string.Empty;
    public string InstallPath { get; set; } = string.Empty;
    public string CoverImagePath { get; set; } = string.Empty;
    public string CoverColor { get; set; } = "#2C2C2C";

    [ObservableProperty]
    private Bitmap? _coverBitmap;

    public async Task LoadCoverAsync()
    {
        if (!string.IsNullOrEmpty(CoverImagePath) && CoverBitmap == null)
        {
            CoverBitmap = await ImageHelper.LoadBitmapAsync(CoverImagePath);
        }
    }
}

public partial class GameGridViewModel : ObservableObject
{
    private readonly List<GameCardItem> _allDiscoveredGames = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private bool _hasGames = false;

    [ObservableProperty]
    private bool _isGridView = true;

    public ObservableCollection<GameCardItem> SteamGames { get; } = new();

    public GameGridViewModel()
    {
        ScanRealGames();
    }

    partial void OnSearchTextChanged(string value)
    {
        FilterGames();
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
    }

    [RelayCommand]
    private void ToggleViewMode()
    {
        IsGridView = !IsGridView;
    }

    [RelayCommand]
    private void SetGridView()
    {
        IsGridView = true;
    }

    [RelayCommand]
    private void SetListView()
    {
        IsGridView = false;
    }

    public void AddManualGameFolder(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath)) return;
        var folderName = Path.GetFileName(folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrEmpty(folderName)) folderName = folderPath;

        var dlssVer = "N/A";
        try
        {
            var files = Directory.GetFiles(folderPath, "nvngx_dlss.dll", SearchOption.AllDirectories);
            if (files.Length > 0) dlssVer = "v3.7.10";
        }
        catch { }

        var card = new GameCardItem
        {
            Name = folderName,
            DLSSVersion = dlssVer,
            LibraryName = "Manual",
            InstallPath = folderPath,
            CoverColor = GetColorForGame(folderName)
        };

        _allDiscoveredGames.Add(card);
        FilterGames();
    }

    public void ScanRealGames()
    {
        _allDiscoveredGames.Clear();

        var steamScanner = new LinuxSteamLibraryScanner();
        if (steamScanner.IsLauncherInstalled())
        {
            var realGames = steamScanner.ScanInstalledGames();
            foreach (var g in realGames)
            {
                var card = new GameCardItem
                {
                    AppId = g.AppId,
                    Name = g.Name,
                    DLSSVersion = g.DLSSVersion,
                    LibraryName = g.Launcher,
                    InstallPath = g.InstallPath,
                    CoverImagePath = g.CoverImagePath,
                    CoverColor = GetColorForGame(g.Name)
                };
                _allDiscoveredGames.Add(card);
                _ = card.LoadCoverAsync(); // Asynchronously load poster artwork
            }
        }

        FilterGames();
    }

    private void FilterGames()
    {
        SteamGames.Clear();
        var query = SearchText?.Trim() ?? string.Empty;
        var matches = string.IsNullOrEmpty(query)
            ? _allDiscoveredGames
            : _allDiscoveredGames.Where(g => g.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

        foreach (var g in matches)
        {
            SteamGames.Add(g);
        }

        HasGames = SteamGames.Count > 0;
    }

    private string GetColorForGame(string name)
    {
        var hash = name.GetHashCode();
        var colors = new[] { "#2D3436", "#636E72", "#2C3E50", "#34495E", "#16A085", "#D35400", "#C0392B", "#8E44AD" };
        return colors[Math.Abs(hash) % colors.Length];
    }

    [RelayCommand]
    public async Task RefreshGamesAsync()
    {
        IsLoading = true;
        await Task.Delay(600); // Visible refresh indicator duration
        ScanRealGames();
        IsLoading = false;
    }
}
