using Avalonia.Controls;
using Avalonia.Interactivity;
using DLSS_Swapper.Avalonia.ViewModels;

namespace DLSS_Swapper.Avalonia.Views;

public partial class GameDetailsWindow : Window
{
    public GameCardItem? SelectedGame { get; }

    public GameDetailsWindow() : this(new GameCardItem { Name = "PRAGMATA", InstallPath = @"C:\Program Files (x86)\Steam\steamapps\common\PRAGMATA", DLSSVersion = "v310.6" })
    {
    }

    public GameDetailsWindow(GameCardItem game)
    {
        InitializeComponent();
        SelectedGame = game;
        DataContext = game;
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
