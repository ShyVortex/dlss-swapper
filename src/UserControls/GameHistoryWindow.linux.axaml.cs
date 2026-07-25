using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DLSS_Swapper.Avalonia.Views;

public partial class GameHistoryWindow : Window
{
    public GameHistoryWindow()
    {
        InitializeComponent();
    }

    public GameHistoryWindow(string gameId, string gameName) : this()
    {
        TitleTextBlock.Text = $"History - {gameName}";
        HistoryControl.LoadHistory(gameId);
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
