using Avalonia.Controls;
using Avalonia.Interactivity;
using DLSS_Swapper.Core.Services;
using DLSS_Swapper.Helpers;

namespace DLSS_Swapper.Avalonia.Views;

public partial class GameHistoryWindow : Window
{
    private string _gameName = string.Empty;

    public GameHistoryWindow()
    {
        InitializeComponent();
        UpdateTranslations();
        LinuxLanguageService.Instance.OnLanguageChanged += UpdateTranslations;
    }

    public GameHistoryWindow(string gameId, string gameName) : this()
    {
        _gameName = gameName;
        UpdateTranslations();
        HistoryControl.LoadHistory(gameId);
    }

    private void UpdateTranslations()
    {
        var historyTitle = ResourceHelper.GetString("GamePage_History", "History");
        Title = string.IsNullOrEmpty(_gameName) ? historyTitle : $"{historyTitle} - {_gameName}";
        TitleTextBlock.Text = Title;
        CloseButton.Content = ResourceHelper.GetString("General_Close", "Close");
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
