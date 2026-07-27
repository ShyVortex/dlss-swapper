using Avalonia.Controls;
using Avalonia.Input;
using DLSS_Swapper.Avalonia.ViewModels;

namespace DLSS_Swapper.Avalonia.Views;

public partial class GameGridView : UserControl
{
    public GameGridView()
    {
        InitializeComponent();
        UpdateTranslations();
        DLSS_Swapper.Core.Services.LinuxLanguageService.Instance.OnLanguageChanged += UpdateTranslations;
    }

    private void UpdateTranslations()
    {
        FavouritesHeaderTextBlock.Text = DLSS_Swapper.Helpers.ResourceHelper.GetString("GamesPage_Favourites", "Favourites");
    }

    private void OnCardPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control control && control.DataContext is GameCardItem game)
        {
            var window = TopLevel.GetTopLevel(this) as Window;
            var dialog = new GameDetailsWindow(game);
            if (window != null)
            {
                dialog.ShowDialog(window);
            }
            else
            {
                dialog.Show();
            }
        }
    }
}
