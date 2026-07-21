using Avalonia.Controls;
using Avalonia.Input;
using DLSS_Swapper.Avalonia.ViewModels;

namespace DLSS_Swapper.Avalonia.Views;

public partial class GameGridView : UserControl
{
    public GameGridView()
    {
        InitializeComponent();
        DataContext = new GameGridViewModel();
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
