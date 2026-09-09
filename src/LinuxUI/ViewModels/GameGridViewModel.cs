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
    public string LibraryName { get; set; } = string.Empty;
    public string InstallPath { get; set; } = string.Empty;
    public string CoverImagePath { get; set; } = string.Empty;
    public string CoverColor { get; set; } = "#2C2C2C";

    public string GameId => !string.IsNullOrEmpty(AppId) ? AppId : (InstallPath ?? Name);

    [ObservableProperty] private bool _isFavourite;

    public Action<GameCardItem>? OnFavouriteToggled { get; set; }
    public Action<GameCardItem>? OnManualGameRemoved { get; set; }

    partial void OnIsFavouriteChanged(bool value)
    {
        OnFavouriteToggled?.Invoke(this);
    }

    [ObservableProperty] private string _dLSSVersion = "N/A";
    [ObservableProperty] private string _dLSSGVersion = "N/A";
    [ObservableProperty] private string _dLSSDVersion = "N/A";
    [ObservableProperty] private string _fsr31Dx12Version = "N/A";
    [ObservableProperty] private string _fsr31VkVersion = "N/A";
    [ObservableProperty] private string _xessVersion = "N/A";
    [ObservableProperty] private string _xessDx11Version = "N/A";
    [ObservableProperty] private string _xessFgVersion = "N/A";
    [ObservableProperty] private string _xellVersion = "N/A";

    private static bool IsValidDll(string? ver)
    {
        return !string.IsNullOrWhiteSpace(ver) &&
               !string.Equals(ver, "N/A", StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(ver, "Not found", StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(ver, "None", StringComparison.OrdinalIgnoreCase);
    }

    public bool HasDLSS => IsValidDll(DLSSVersion);
    public bool HasDLSSG => IsValidDll(DLSSGVersion);
    public bool HasDLSSD => IsValidDll(DLSSDVersion);
    public bool HasFsr31Dx12 => IsValidDll(Fsr31Dx12Version);
    public bool HasFsr31Vk => IsValidDll(Fsr31VkVersion);
    public bool HasXeSS => IsValidDll(XessVersion);
    public bool HasXeSSDx11 => IsValidDll(XessDx11Version);
    public bool HasXeSSFg => IsValidDll(XessFgVersion);
    public bool HasXeLL => IsValidDll(XellVersion);

    public bool HasAnySwappableItem => HasDLSS || HasDLSSG || HasDLSSD || HasFsr31Dx12 || HasFsr31Vk || HasXeSS || HasXeSSDx11 || HasXeSSFg || HasXeLL;

    public bool IsManualGame => string.Equals(LibraryName, "Manually Added", StringComparison.OrdinalIgnoreCase) || string.Equals(LibraryName, "Manual", StringComparison.OrdinalIgnoreCase);

    public List<DLSS_Swapper.Core.Models.DlssPresetItem> SrPresetOptions { get; } = DLSS_Swapper.Core.Models.DlssPresetItem.GetSrPresetOptions();
    public List<DLSS_Swapper.Core.Models.DlssPresetItem> RrPresetOptions { get; } = DLSS_Swapper.Core.Models.DlssPresetItem.GetRrPresetOptions();
    public List<DLSS_Swapper.Core.Models.DlssPresetItem> FgPresetOptions { get; } = DLSS_Swapper.Core.Models.DlssPresetItem.GetFgPresetOptions();

    [ObservableProperty] private DLSS_Swapper.Core.Models.DlssPresetItem? _selectedDlssPresetOption;
    [ObservableProperty] private DLSS_Swapper.Core.Models.DlssPresetItem? _selectedDlssRrPresetOption;
    [ObservableProperty] private DLSS_Swapper.Core.Models.DlssPresetItem? _selectedDlssFgPresetOption;

    private bool _isInitializingPresets = false;
    private readonly LinuxPresetService _presetService = new LinuxPresetService();

    public bool IsSteamRunning => _presetService.IsSteamRunning();
    public bool IsHeroicRunning => _presetService.IsHeroicRunning();

    public bool IsLauncherRunning
    {
        get
        {
            if (string.Equals(LibraryName, "Heroic", StringComparison.OrdinalIgnoreCase))
                return IsHeroicRunning;
            if (string.Equals(LibraryName, "Steam", StringComparison.OrdinalIgnoreCase))
                return IsSteamRunning;
            return false;
        }
    }

    public string LauncherRunningWarning
    {
        get
        {
            if (string.Equals(LibraryName, "Heroic", StringComparison.OrdinalIgnoreCase))
                return "Heroic Games Launcher is currently running. Close Heroic before changing presets to ensure launch options persist.";
            return "Steam is currently running. Close Steam before changing presets to ensure launch options persist.";
        }
    }

    public void LoadPresets()
    {
        if (string.IsNullOrEmpty(AppId)) return;

        _isInitializingPresets = true;
        try
        {
            var state = string.Equals(LibraryName, "Heroic", StringComparison.OrdinalIgnoreCase)
                ? _presetService.ReadHeroicGamePresets(AppId)
                : _presetService.ReadGamePresets(AppId);

            SelectedDlssPresetOption = (!string.IsNullOrEmpty(state.SrPresetValue))
                ? (SrPresetOptions.FirstOrDefault(x => x.EnvironmentValue == state.SrPresetValue) ?? SrPresetOptions[0])
                : SrPresetOptions[0];

            SelectedDlssRrPresetOption = (!string.IsNullOrEmpty(state.RrPresetValue))
                ? (RrPresetOptions.FirstOrDefault(x => x.EnvironmentValue == state.RrPresetValue) ?? RrPresetOptions[0])
                : RrPresetOptions[0];

            SelectedDlssFgPresetOption = (!string.IsNullOrEmpty(state.FgPresetValue))
                ? (FgPresetOptions.FirstOrDefault(x => x.EnvironmentValue == state.FgPresetValue) ?? FgPresetOptions[0])
                : FgPresetOptions[0];

            // Log initial detected DLLs
            var history = new GameHistoryService();
            if (HasDLSS) _ = history.LogDetectedDllAsync(GameId, DLSS_Swapper.Data.GameAssetType.DLSS, DLSSVersion);
            if (HasDLSSG) _ = history.LogDetectedDllAsync(GameId, DLSS_Swapper.Data.GameAssetType.DLSS_G, DLSSGVersion);
            if (HasDLSSD) _ = history.LogDetectedDllAsync(GameId, DLSS_Swapper.Data.GameAssetType.DLSS_D, DLSSDVersion);
            if (HasFsr31Dx12) _ = history.LogDetectedDllAsync(GameId, DLSS_Swapper.Data.GameAssetType.FSR_31_DX12, Fsr31Dx12Version);
            if (HasFsr31Vk) _ = history.LogDetectedDllAsync(GameId, DLSS_Swapper.Data.GameAssetType.FSR_31_VK, Fsr31VkVersion);
            if (HasXeSS) _ = history.LogDetectedDllAsync(GameId, DLSS_Swapper.Data.GameAssetType.XeSS, XessVersion);
            if (HasXeSSDx11) _ = history.LogDetectedDllAsync(GameId, DLSS_Swapper.Data.GameAssetType.XeSS_DX11, XessDx11Version);
            if (HasXeSSFg) _ = history.LogDetectedDllAsync(GameId, DLSS_Swapper.Data.GameAssetType.XeSS_FG, XessFgVersion);
            if (HasXeLL) _ = history.LogDetectedDllAsync(GameId, DLSS_Swapper.Data.GameAssetType.XeLL, XellVersion);
        }
        finally
        {
            _isInitializingPresets = false;
        }
    }

    partial void OnSelectedDlssPresetOptionChanged(DLSS_Swapper.Core.Models.DlssPresetItem? value) => SavePresetsIfReady();
    partial void OnSelectedDlssRrPresetOptionChanged(DLSS_Swapper.Core.Models.DlssPresetItem? value) => SavePresetsIfReady();
    partial void OnSelectedDlssFgPresetOptionChanged(DLSS_Swapper.Core.Models.DlssPresetItem? value) => SavePresetsIfReady();

    private void SavePresetsIfReady()
    {
        if (_isInitializingPresets || string.IsNullOrEmpty(AppId)) return;

        if (string.Equals(LibraryName, "Heroic", StringComparison.OrdinalIgnoreCase))
        {
            _presetService.SaveHeroicGamePresets(
                AppId,
                SelectedDlssPresetOption?.EnvironmentValue,
                SelectedDlssRrPresetOption?.EnvironmentValue,
                SelectedDlssFgPresetOption?.EnvironmentValue
            );
        }
        else
        {
            _presetService.SaveGamePresets(
                AppId,
                SelectedDlssPresetOption?.EnvironmentValue,
                SelectedDlssRrPresetOption?.EnvironmentValue,
                SelectedDlssFgPresetOption?.EnvironmentValue
            );
        }
    }

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
    private readonly GameMetadataStorageService _metadataService = new();
    private HashSet<string> _favouriteIds = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private bool _hasGames = false;

    [ObservableProperty]
    private bool _hasFavourites = false;

    [ObservableProperty]
    private bool _isGridView = true;

    public ObservableCollection<GameCardItem> FavouriteGames { get; } = new();
    public ObservableCollection<GameLibraryGroupViewModel> LibraryGroups { get; } = new();
    public ObservableCollection<GameCardItem> SteamGames { get; } = new();

    public GameGridViewModel()
    {
        var settings = LinuxSettingsService.Instance.Settings;
        _isGridView = settings.IsGridView;
        _hideNoSwappableItems = settings.HideNoSwappableItems;
        _showHiddenGames = settings.ShowHiddenGames;
        _groupByLibrary = settings.GroupByLibrary;

        _favouriteIds = _metadataService.LoadFavourites();
        LoadFromCacheImmediately();

        LinuxSettingsService.Instance.OnSettingsChanged += () =>
        {
            global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                FilterGames();
            });
        };
    }

    private void LoadFromCacheImmediately()
    {
        try
        {
            var cachedEntries = _metadataService.LoadScannedGamesCache(out bool isCacheOutdated);
            if (cachedEntries != null && cachedEntries.Count > 0)
            {
                var cards = new List<GameCardItem>();
                foreach (var entry in cachedEntries)
                {
                    if (string.IsNullOrEmpty(entry.InstallPath) || !Directory.Exists(entry.InstallPath))
                        continue;

                    var card = new GameCardItem
                    {
                        AppId = entry.Id,
                        Name = entry.Title,
                        LibraryName = entry.Launcher,
                        InstallPath = entry.InstallPath,
                        CoverImagePath = entry.CoverImagePath ?? string.Empty,
                        CoverColor = !string.IsNullOrEmpty(entry.CoverColorHex) ? entry.CoverColorHex : GetColorForGame(entry.Title),
                        DLSSVersion = entry.DllMap.GetValueOrDefault("dlss", "N/A"),
                        DLSSGVersion = entry.DllMap.GetValueOrDefault("dlss_g", "N/A"),
                        DLSSDVersion = entry.DllMap.GetValueOrDefault("dlss_d", "N/A"),
                        Fsr31Dx12Version = entry.DllMap.GetValueOrDefault("fsr_31_dx12", "N/A"),
                        Fsr31VkVersion = entry.DllMap.GetValueOrDefault("fsr_31_vk", "N/A"),
                        XessVersion = entry.DllMap.GetValueOrDefault("xess", "N/A"),
                        XessDx11Version = entry.DllMap.GetValueOrDefault("xess_dx11", "N/A"),
                        XessFgVersion = entry.DllMap.GetValueOrDefault("xess_fg", "N/A"),
                        XellVersion = entry.DllMap.GetValueOrDefault("xell", "N/A")
                    };
                    card.IsFavourite = _favouriteIds.Contains(card.GameId);
                    card.OnFavouriteToggled = OnCardFavouriteToggled;
                    card.OnManualGameRemoved = OnCardManualGameRemoved;
                    cards.Add(card);
                }

                _allDiscoveredGames.Clear();
                _allDiscoveredGames.AddRange(cards);
                foreach (var card in _allDiscoveredGames)
                {
                    _ = card.LoadCoverAsync();
                }
                FilterGames();
            }

            // Immediately scan in background; if cache is outdated or missing, force a full rescan
            _ = ScanRealGamesAsync(isManualRefresh: isCacheOutdated);
        }
        catch
        {
            _ = ScanRealGamesAsync(isManualRefresh: true);
        }
    }

    partial void OnIsGridViewChanged(bool value)
    {
        LinuxSettingsService.Instance.Settings.IsGridView = value;
        LinuxSettingsService.Instance.SaveSettings();
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
        if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath)) return;

        // Check if already added
        if (_allDiscoveredGames.Any(x => string.Equals(x.InstallPath, folderPath, StringComparison.OrdinalIgnoreCase)))
            return;

        var folderName = Path.GetFileName(folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var steamScanner = new LinuxSteamLibraryScanner();
        var card = CreateManualGameCard(folderPath, folderName, null, steamScanner);

        _metadataService.AddManualGame(folderName, folderPath);
        _allDiscoveredGames.Add(card);
        FilterGames();
    }

    private GameCardItem CreateManualGameCard(string folderPath, string folderName, string? savedCoverPath, LinuxSteamLibraryScanner scanner)
    {
        string? cover = savedCoverPath;
        if (string.IsNullOrEmpty(cover) || !File.Exists(cover))
        {
            // Auto-detect standard cover files
            var candidateCovers = new[]
            {
                Path.Combine(folderPath, "cover.jpg"),
                Path.Combine(folderPath, "cover.png"),
                Path.Combine(folderPath, "poster.jpg"),
                Path.Combine(folderPath, "poster.png"),
                Path.Combine(folderPath, "boxart.jpg"),
                Path.Combine(folderPath, "boxart.png"),
                Path.Combine(folderPath, "library_600x900.jpg")
            };
            foreach (var p in candidateCovers)
            {
                if (File.Exists(p))
                {
                    cover = p;
                    break;
                }
            }
        }

        var dlls = scanner.ScanAllGameDlls(folderPath);
        var card = new GameCardItem
        {
            AppId = $"manual_{folderName.Replace(" ", "_")}",
            Name = folderName,
            DLSSVersion = dlls.DLSSVersion,
            DLSSGVersion = dlls.DLSSGVersion,
            DLSSDVersion = dlls.DLSSDVersion,
            Fsr31Dx12Version = dlls.Fsr31Dx12Version,
            Fsr31VkVersion = dlls.Fsr31VkVersion,
            XessVersion = dlls.XessVersion,
            XessDx11Version = dlls.XessDx11Version,
            XessFgVersion = dlls.XessFgVersion,
            XellVersion = dlls.XellVersion,
            LibraryName = "Manually Added",
            InstallPath = folderPath,
            CoverImagePath = cover ?? string.Empty,
            CoverColor = GetColorForGame(folderName)
        };
        card.OnFavouriteToggled = OnCardFavouriteToggled;
        card.OnManualGameRemoved = OnCardManualGameRemoved;
        _ = card.LoadCoverAsync();
        return card;
    }

    private void OnCardManualGameRemoved(GameCardItem card)
    {
        RemoveManualGame(card);
    }

    public void AddGameFolder(string folderPath)
    {
        AddManualGameFolder(folderPath);
    }

    public void RemoveManualGame(GameCardItem card)
    {
        if (card == null || string.IsNullOrEmpty(card.InstallPath)) return;
        _metadataService.RemoveManualGame(card.InstallPath);
        _allDiscoveredGames.RemoveAll(x => string.Equals(x.InstallPath, card.InstallPath, StringComparison.OrdinalIgnoreCase) || x == card);
        FilterGames();
    }

    public async Task ScanRealGamesAsync(bool isManualRefresh = false)
    {
        if (isManualRefresh || _allDiscoveredGames.Count == 0)
        {
            IsLoading = true;
        }

        try
        {
            var discoveredData = await Task.Run(() =>
            {
                var cards = new List<GameCardItem>();
                var cacheEntriesToSave = new List<ScannedGameCacheEntry>();
                var favIds = _metadataService.LoadFavourites();
                var existingCache = _metadataService.LoadScannedGamesCache(out bool isCacheOutdated)
                    .ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

                bool forceFullRescan = isManualRefresh || isCacheOutdated;
                var steamScanner = new LinuxSteamLibraryScanner();

                // 1. Steam Games
                if (steamScanner.IsLauncherInstalled())
                {
                    var realGames = steamScanner.ScanInstalledGames(existingCache, forceRescan: forceFullRescan);
                    foreach (var g in realGames)
                    {
                        var card = new GameCardItem
                        {
                            AppId = g.AppId,
                            Name = g.Name,
                            DLSSVersion = g.DLSSVersion,
                            DLSSGVersion = g.DLSSGVersion,
                            DLSSDVersion = g.DLSSDVersion,
                            Fsr31Dx12Version = g.Fsr31Dx12Version,
                            Fsr31VkVersion = g.Fsr31VkVersion,
                            XessVersion = g.XessVersion,
                            XessDx11Version = g.XessDx11Version,
                            XessFgVersion = g.XessFgVersion,
                            XellVersion = g.XellVersion,
                            LibraryName = "Steam",
                            InstallPath = g.InstallPath,
                            CoverImagePath = g.CoverImagePath,
                            CoverColor = GetColorForGame(g.Name)
                        };
                        card.IsFavourite = favIds.Contains(card.GameId);
                        card.OnFavouriteToggled = OnCardFavouriteToggled;
                        cards.Add(card);

                        cacheEntriesToSave.Add(new ScannedGameCacheEntry
                        {
                            Id = g.AppId,
                            Title = g.Name,
                            Launcher = "Steam",
                            InstallPath = g.InstallPath,
                            ManifestPath = g.ManifestPath,
                            ManifestLastWriteTimeUtcTicks = g.ManifestLastWriteTimeUtcTicks,
                            CoverImagePath = g.CoverImagePath,
                            CoverColorHex = card.CoverColor,
                            IsManualGame = false,
                            DllMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                            {
                                ["dlss"] = g.DLSSVersion,
                                ["dlss_g"] = g.DLSSGVersion,
                                ["dlss_d"] = g.DLSSDVersion,
                                ["fsr_31_dx12"] = g.Fsr31Dx12Version,
                                ["fsr_31_vk"] = g.Fsr31VkVersion,
                                ["xess"] = g.XessVersion,
                                ["xess_dx11"] = g.XessDx11Version,
                                ["xess_fg"] = g.XessFgVersion,
                                ["xell"] = g.XellVersion
                            }
                        });
                    }
                }

                // 2. Heroic Games
                var heroicScanner = new LinuxHeroicLibraryScanner();
                if (heroicScanner.IsLauncherInstalled())
                {
                    var heroicGames = heroicScanner.ScanInstalledGames(existingCache, forceRescan: forceFullRescan);
                    foreach (var g in heroicGames)
                    {
                        var card = new GameCardItem
                        {
                            AppId = g.AppId,
                            Name = g.Name,
                            DLSSVersion = g.DLSSVersion,
                            DLSSGVersion = g.DLSSGVersion,
                            DLSSDVersion = g.DLSSDVersion,
                            Fsr31Dx12Version = g.Fsr31Dx12Version,
                            Fsr31VkVersion = g.Fsr31VkVersion,
                            XessVersion = g.XessVersion,
                            XessDx11Version = g.XessDx11Version,
                            XessFgVersion = g.XessFgVersion,
                            XellVersion = g.XellVersion,
                            LibraryName = "Heroic",
                            InstallPath = g.InstallPath,
                            CoverImagePath = g.CoverImagePath,
                            CoverColor = GetColorForGame(g.Name)
                        };
                        card.IsFavourite = favIds.Contains(card.GameId);
                        card.OnFavouriteToggled = OnCardFavouriteToggled;
                        cards.Add(card);

                        cacheEntriesToSave.Add(new ScannedGameCacheEntry
                        {
                            Id = g.AppId,
                            Title = g.Name,
                            Launcher = "Heroic",
                            InstallPath = g.InstallPath,
                            ManifestPath = g.ManifestPath,
                            ManifestLastWriteTimeUtcTicks = g.ManifestLastWriteTimeUtcTicks,
                            CoverImagePath = g.CoverImagePath,
                            CoverColorHex = card.CoverColor,
                            IsManualGame = false,
                            DllMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                            {
                                ["dlss"] = g.DLSSVersion,
                                ["dlss_g"] = g.DLSSGVersion,
                                ["dlss_d"] = g.DLSSDVersion,
                                ["fsr_31_dx12"] = g.Fsr31Dx12Version,
                                ["fsr_31_vk"] = g.Fsr31VkVersion,
                                ["xess"] = g.XessVersion,
                                ["xess_dx11"] = g.XessDx11Version,
                                ["xess_fg"] = g.XessFgVersion,
                                ["xell"] = g.XellVersion
                            }
                        });
                    }
                }

                // 3. Manually Added Games
                var manualRecords = _metadataService.LoadManualGames();
                foreach (var record in manualRecords)
                {
                    if (string.IsNullOrEmpty(record.InstallPath) || !Directory.Exists(record.InstallPath))
                        continue;

                    if (cards.Any(x => string.Equals(x.InstallPath, record.InstallPath, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    var manualAppId = $"manual_{record.Name.Replace(" ", "_")}";
                    GameCardItem card;

                    if (!forceFullRescan && existingCache.TryGetValue(manualAppId, out var cachedManual) && Directory.Exists(record.InstallPath))
                    {
                        card = new GameCardItem
                        {
                            AppId = manualAppId,
                            Name = record.Name,
                            DLSSVersion = cachedManual.DllMap.GetValueOrDefault("dlss", "N/A"),
                            DLSSGVersion = cachedManual.DllMap.GetValueOrDefault("dlss_g", "N/A"),
                            DLSSDVersion = cachedManual.DllMap.GetValueOrDefault("dlss_d", "N/A"),
                            Fsr31Dx12Version = cachedManual.DllMap.GetValueOrDefault("fsr_31_dx12", "N/A"),
                            Fsr31VkVersion = cachedManual.DllMap.GetValueOrDefault("fsr_31_vk", "N/A"),
                            XessVersion = cachedManual.DllMap.GetValueOrDefault("xess", "N/A"),
                            XessDx11Version = cachedManual.DllMap.GetValueOrDefault("xess_dx11", "N/A"),
                            XessFgVersion = cachedManual.DllMap.GetValueOrDefault("xess_fg", "N/A"),
                            XellVersion = cachedManual.DllMap.GetValueOrDefault("xell", "N/A"),
                            LibraryName = "Manually Added",
                            InstallPath = record.InstallPath,
                            CoverImagePath = !string.IsNullOrEmpty(record.CoverImagePath) ? record.CoverImagePath : (cachedManual.CoverImagePath ?? string.Empty),
                            CoverColor = GetColorForGame(record.Name)
                        };
                    }
                    else
                    {
                        card = CreateManualGameCard(record.InstallPath, record.Name, record.CoverImagePath, steamScanner);
                    }

                    card.IsFavourite = favIds.Contains(card.GameId);
                    card.OnFavouriteToggled = OnCardFavouriteToggled;
                    card.OnManualGameRemoved = OnCardManualGameRemoved;
                    cards.Add(card);

                    cacheEntriesToSave.Add(new ScannedGameCacheEntry
                    {
                        Id = card.AppId,
                        Title = card.Name,
                        Launcher = "Manually Added",
                        InstallPath = card.InstallPath,
                        CoverImagePath = card.CoverImagePath,
                        CoverColorHex = card.CoverColor,
                        IsManualGame = true,
                        DllMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["dlss"] = card.DLSSVersion,
                            ["dlss_g"] = card.DLSSGVersion,
                            ["dlss_d"] = card.DLSSDVersion,
                            ["fsr_31_dx12"] = card.Fsr31Dx12Version,
                            ["fsr_31_vk"] = card.Fsr31VkVersion,
                            ["xess"] = card.XessVersion,
                            ["xess_dx11"] = card.XessDx11Version,
                            ["xess_fg"] = card.XessFgVersion,
                            ["xell"] = card.XellVersion
                        }
                    });
                }

                _metadataService.SaveScannedGamesCache(cacheEntriesToSave);
                return (Cards: cards, Favourites: favIds);
            });

            global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _allDiscoveredGames.Clear();
                _favouriteIds = discoveredData.Favourites;
                _allDiscoveredGames.AddRange(discoveredData.Cards);

                foreach (var card in _allDiscoveredGames)
                {
                    _ = card.LoadCoverAsync();
                }

                FilterGames();
                IsLoading = false;
            });
        }
        catch
        {
            IsLoading = false;
        }
    }

    public void ScanRealGames()
    {
        _ = ScanRealGamesAsync(isManualRefresh: true);
    }

    private void OnCardFavouriteToggled(GameCardItem card)
    {
        if (card.IsFavourite)
        {
            _favouriteIds.Add(card.GameId);
        }
        else
        {
            _favouriteIds.Remove(card.GameId);
        }
        _metadataService.SaveFavourites(_favouriteIds);
        FilterGames();
    }

    [ObservableProperty]
    private bool _hideNoSwappableItems = false;

    [ObservableProperty]
    private bool _showHiddenGames = false;

    [ObservableProperty]
    private bool _groupByLibrary = true;

    partial void OnHideNoSwappableItemsChanged(bool value)
    {
        FilterGames();
    }

    partial void OnShowHiddenGamesChanged(bool value)
    {
        FilterGames();
    }

    partial void OnGroupByLibraryChanged(bool value)
    {
        FilterGames();
    }

    public void ApplyFilters()
    {
        var settings = LinuxSettingsService.Instance.Settings;
        settings.HideNoSwappableItems = HideNoSwappableItems;
        settings.ShowHiddenGames = ShowHiddenGames;
        settings.GroupByLibrary = GroupByLibrary;
        LinuxSettingsService.Instance.SaveSettings();

        FilterGames();
    }

    private void FilterGames()
    {
        FavouriteGames.Clear();
        LibraryGroups.Clear();
        SteamGames.Clear();

        var settings = LinuxSettingsService.Instance.Settings;
        var query = SearchText?.Trim() ?? string.Empty;
        var matches = _allDiscoveredGames.AsEnumerable();

        // Filter by Game Library toggles in Settings
        matches = matches.Where(g =>
        {
            if (string.Equals(g.LibraryName, "Steam", StringComparison.OrdinalIgnoreCase))
                return settings.EnableSteam;
            if (string.Equals(g.LibraryName, "Heroic", StringComparison.OrdinalIgnoreCase))
                return settings.EnableHeroic;
            if (string.Equals(g.LibraryName, "Manual", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(g.LibraryName, "Manually Added", StringComparison.OrdinalIgnoreCase))
                return settings.EnableManuallyAdded;
            return true;
        });

        if (!string.IsNullOrEmpty(query))
        {
            matches = matches.Where(g => g.Name.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        if (HideNoSwappableItems)
        {
            matches = matches.Where(g => g.HasAnySwappableItem);
        }

        var matchList = matches.ToList();

        foreach (var g in matchList)
        {
            if (g.IsFavourite)
            {
                FavouriteGames.Add(g);
            }
            SteamGames.Add(g);
        }

        if (GroupByLibrary)
        {
            // Defined order of known launchers
            var knownLibraries = new[] { "Steam", "Heroic", "Manually Added" };

            foreach (var libName in knownLibraries)
            {
                var groupGames = matchList.Where(g =>
                    string.Equals(g.LibraryName, libName, StringComparison.OrdinalIgnoreCase) ||
                    (libName == "Manually Added" && string.Equals(g.LibraryName, "Manual", StringComparison.OrdinalIgnoreCase))
                ).ToList();

                if (groupGames.Count > 0)
                {
                    var group = new GameLibraryGroupViewModel(libName, libName, showHeader: true);
                    foreach (var game in groupGames)
                    {
                        group.Games.Add(game);
                    }
                    LibraryGroups.Add(group);
                }
            }

            // Next add any other libraries (e.g. Lutris, Ubisoft, GOG, Epic if added in future)
            var otherLibraries = matchList
                .Select(g => g.LibraryName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(name => !knownLibraries.Any(k => string.Equals(k, name, StringComparison.OrdinalIgnoreCase)) &&
                               !string.Equals(name, "Manual", StringComparison.OrdinalIgnoreCase));

            foreach (var libName in otherLibraries)
            {
                var groupGames = matchList.Where(g => string.Equals(g.LibraryName, libName, StringComparison.OrdinalIgnoreCase)).ToList();
                if (groupGames.Count > 0)
                {
                    var group = new GameLibraryGroupViewModel(libName, libName, showHeader: true);
                    foreach (var game in groupGames)
                    {
                        group.Games.Add(game);
                    }
                    LibraryGroups.Add(group);
                }
            }
        }
        else
        {
            if (matchList.Count > 0)
            {
                var group = new GameLibraryGroupViewModel(string.Empty, "All", showHeader: false);
                foreach (var game in matchList)
                {
                    group.Games.Add(game);
                }
                LibraryGroups.Add(group);
            }
        }

        HasFavourites = FavouriteGames.Count > 0;
        HasGames = LibraryGroups.Any(g => g.HasGames) || HasFavourites;
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
        await ScanRealGamesAsync(isManualRefresh: true);
    }
}
