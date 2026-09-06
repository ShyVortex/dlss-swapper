using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DLSS_Swapper.Avalonia.ViewModels;

public partial class GameLibraryGroupViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _libraryName = string.Empty;

    [ObservableProperty]
    private bool _showHeader = true;

    public ObservableCollection<GameCardItem> Games { get; } = new();

    public bool HasGames => Games.Count > 0;

    public GameLibraryGroupViewModel(string title, string libraryName, bool showHeader = true)
    {
        Title = title;
        LibraryName = libraryName;
        ShowHeader = showHeader;
    }
}
