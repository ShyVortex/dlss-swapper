using Avalonia.Controls;
using Avalonia.Interactivity;
using DLSS_Swapper.Core.Services;
using DLSS_Swapper.Helpers;

namespace DLSS_Swapper.Avalonia.Views;

public partial class AddGameStep2Window : Window
{
    public bool UserProceeded { get; private set; }

    public AddGameStep2Window()
    {
        InitializeComponent();
        UpdateTranslations();
        LinuxLanguageService.Instance.OnLanguageChanged += UpdateTranslations;
    }

    private void UpdateTranslations()
    {
        Title = ResourceHelper.GetString("GamesPage_ManuallyAdding_AnotherNoteTitle", "Another note for manually adding games");
        TitleTextBlock.Text = ResourceHelper.GetString("GamesPage_ManuallyAdding_AnotherNoteTitle", "Another note for manually adding games");
        AddGameButton.Content = ResourceHelper.GetString("GamesPage_AddGame", "Add Game");
        CloseButton.Content = ResourceHelper.GetString("General_Close", "Close");
    }

    private void OnAddGameClick(object? sender, RoutedEventArgs e)
    {
        UserProceeded = true;
        Close();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        UserProceeded = false;
        Close();
    }
}
