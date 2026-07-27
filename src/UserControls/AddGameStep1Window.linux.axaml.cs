using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DLSS_Swapper.Core.Services;
using DLSS_Swapper.Helpers;

namespace DLSS_Swapper.Avalonia.Views;

public partial class AddGameStep1Window : Window
{
    public bool UserProceeded { get; private set; }
    public bool DontShowAgain => DontShowCheckBox?.IsChecked == true;

    public AddGameStep1Window()
    {
        InitializeComponent();
        UpdateTranslations();
        LinuxLanguageService.Instance.OnLanguageChanged += UpdateTranslations;
    }

    private void UpdateTranslations()
    {
        Title = ResourceHelper.GetString("GamesPage_ManuallyAdding_NoteTitle", "Note for manually adding games");
        TitleTextBlock.Text = ResourceHelper.GetString("GamesPage_ManuallyAdding_NoteTitle", "Note for manually adding games");
        MessageBodyTextBlock.Text = ResourceHelper.GetString("GamesPage_ManuallyAdding_NoteMessage", "DLSS Swapper should find games from your installed game libraries automatically. If your game is not listed there may be a few settings preventing it. Please check:");
        CheckItem1TextBlock.Text = "- " + ResourceHelper.GetString("GamesPage_HideGamesWithNoSwappableItems", "Games list filter is not set to \"Hide games with no swappable items\"");
        CheckItem2TextBlock.Text = "- " + ResourceHelper.GetString("SettingsPage_GameLibraries", "Specific game library is enabled in settings");
        DontShowCheckBox.Content = ResourceHelper.GetString("General_DontShowAgain", "Don't show again");
        AddGameButton.Content = ResourceHelper.GetString("GamesPage_AddGame", "Add Game");
        ReportIssueButton.Content = ResourceHelper.GetString("General_ReportIssue", "Report issue");
        CancelButton.Content = ResourceHelper.GetString("General_Cancel", "Cancel");
    }

    private void OnAddGameClick(object? sender, RoutedEventArgs e)
    {
        UserProceeded = true;
        Close();
    }

    private void OnReportIssueClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/beeradmoore/dlss-swapper/issues",
                UseShellExecute = true
            });
        }
        catch
        {
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        UserProceeded = false;
        Close();
    }
}
