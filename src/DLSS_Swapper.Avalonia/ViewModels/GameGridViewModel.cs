using System.Collections.ObjectModel;
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
    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isLoading = false;

    public ObservableCollection<GameCardItem> SteamGames { get; } = new();

    public GameGridViewModel()
    {
        ScanRealGames();
    }

    public void ScanRealGames()
    {
        SteamGames.Clear();

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
                SteamGames.Add(card);
                _ = card.LoadCoverAsync(); // Asynchronously load poster artwork
            }
        }

        // Fallback sample games if no real games are installed in Steam directory yet
        if (SteamGames.Count == 0)
        {
            LoadSampleGames();
        }
    }

    private string GetColorForGame(string name)
    {
        var hash = name.GetHashCode();
        var colors = new[] { "#2D3436", "#636E72", "#2C3E50", "#34495E", "#16A085", "#D35400", "#C0392B", "#8E44AD" };
        return colors[System.Math.Abs(hash) % colors.Length];
    }

    private void LoadSampleGames()
    {
        SteamGames.Add(new GameCardItem { Name = "Cyberpunk 2077", DLSSVersion = "v310.1", LibraryName = "Steam", CoverColor = "#F1C40F" });
        SteamGames.Add(new GameCardItem { Name = "Battlefield 6", DLSSVersion = "v310.4", LibraryName = "Steam", CoverColor = "#2D3436" });
        SteamGames.Add(new GameCardItem { Name = "Company of Heroes 3", DLSSVersion = "N/A", LibraryName = "Steam", CoverColor = "#636E72" });
        SteamGames.Add(new GameCardItem { Name = "Escape Simulator", DLSSVersion = "N/A", LibraryName = "Steam", CoverColor = "#E67E22" });
        SteamGames.Add(new GameCardItem { Name = "Escape Simulator 2", DLSSVersion = "v3.7.20", LibraryName = "Steam", CoverColor = "#D35400" });
        SteamGames.Add(new GameCardItem { Name = "Lossless Scaling", DLSSVersion = "N/A", LibraryName = "Steam", CoverColor = "#3498DB" });
        SteamGames.Add(new GameCardItem { Name = "Once Human", DLSSVersion = "v310.4", LibraryName = "Steam", CoverColor = "#2C3E50" });
        SteamGames.Add(new GameCardItem { Name = "The Bureau: XCOM Declassified", DLSSVersion = "N/A", LibraryName = "Steam", CoverColor = "#34495E" });
        SteamGames.Add(new GameCardItem { Name = "Rainbow Six Siege X", DLSSVersion = "v3.7.10", LibraryName = "Steam", CoverColor = "#16A085" });
    }

    [RelayCommand]
    private async Task RefreshGamesAsync()
    {
        IsLoading = true;
        await Task.Delay(300);
        ScanRealGames();
        IsLoading = false;
    }
}
