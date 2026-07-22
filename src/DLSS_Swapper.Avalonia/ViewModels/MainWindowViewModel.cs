using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DLSS_Swapper.Avalonia.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private GameGridViewModel _gameGridViewModel = new();

    [ObservableProperty]
    private LibraryViewModel _libraryViewModel = new();

    [ObservableProperty]
    private bool _isGamesPageVisible = true;

    [ObservableProperty]
    private bool _isLibraryPageVisible = false;

    [ObservableProperty]
    private bool _isSettingsPageVisible = false;

    [RelayCommand]
    private void NavigateToGames()
    {
        IsGamesPageVisible = true;
        IsLibraryPageVisible = false;
        IsSettingsPageVisible = false;
    }

    [RelayCommand]
    private void NavigateToLibrary()
    {
        IsGamesPageVisible = false;
        IsLibraryPageVisible = true;
        IsSettingsPageVisible = false;
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        IsGamesPageVisible = false;
        IsLibraryPageVisible = false;
        IsSettingsPageVisible = true;
    }
}
