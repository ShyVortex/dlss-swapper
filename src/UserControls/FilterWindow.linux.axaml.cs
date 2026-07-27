using Avalonia.Controls;
using Avalonia.Interactivity;
using DLSS_Swapper.Core.Services;
using DLSS_Swapper.Helpers;

namespace DLSS_Swapper.Avalonia.Views;

public partial class FilterWindow : Window
{
    public bool Applied { get; private set; }

    public bool HideNoSwappableItems => HideNoSwappableCheckBox.IsChecked == true;
    public bool ShowHiddenGames => ShowHiddenCheckBox.IsChecked == true;
    public bool GroupByLibrary => GroupLibraryCheckBox.IsChecked == true;

    public FilterWindow()
    {
        InitializeComponent();
        UpdateTranslations();
        LinuxLanguageService.Instance.OnLanguageChanged += UpdateTranslations;
    }

    public FilterWindow(bool hideNoSwappable, bool showHidden, bool groupByLibrary) : this()
    {
        HideNoSwappableCheckBox.IsChecked = hideNoSwappable;
        ShowHiddenCheckBox.IsChecked = showHidden;
        GroupLibraryCheckBox.IsChecked = groupByLibrary;
    }

    private void UpdateTranslations()
    {
        Title = ResourceHelper.GetString("General_Filter", "Filter");
        TitleTextBlock.Text = ResourceHelper.GetString("General_Filter", "Filter");
        OptionsHeaderTextBlock.Text = ResourceHelper.GetString("General_Options", "Options") + ":";
        HideNoSwappableCheckBox.Content = ResourceHelper.GetString("GamesPage_HideGamesWithNoSwappableItems", "Hide games with no swappable items");
        ShowHiddenCheckBox.Content = ResourceHelper.GetString("GamesPage_ShowHiddenGamesText", "Show hidden games (defaults to off on launch)");
        GroupingHeaderTextBlock.Text = ResourceHelper.GetString("GamesPage_Grouping", "Grouping") + ":";
        GroupLibraryCheckBox.Content = ResourceHelper.GetString("GamesPage_GroupGamesFromTheSameLibraryTogether", "Group games from the same library together");
        ApplyButton.Content = ResourceHelper.GetString("General_Apply", "Apply");
        CancelButton.Content = ResourceHelper.GetString("General_Cancel", "Cancel");
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        Applied = true;
        Close();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Applied = false;
        Close();
    }
}
