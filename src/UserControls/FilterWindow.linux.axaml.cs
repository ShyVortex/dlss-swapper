using Avalonia.Controls;
using Avalonia.Interactivity;

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
    }

    public FilterWindow(bool hideNoSwappable, bool showHidden, bool groupByLibrary) : this()
    {
        HideNoSwappableCheckBox.IsChecked = hideNoSwappable;
        ShowHiddenCheckBox.IsChecked = showHidden;
        GroupLibraryCheckBox.IsChecked = groupByLibrary;
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
