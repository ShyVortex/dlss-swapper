using CommunityToolkit.Mvvm.ComponentModel;

namespace DLSS_Swapper.Avalonia.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private GameGridViewModel _gameGridViewModel = new();
}
