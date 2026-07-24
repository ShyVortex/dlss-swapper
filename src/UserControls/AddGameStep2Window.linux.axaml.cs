using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DLSS_Swapper.Avalonia.Views;

public partial class AddGameStep2Window : Window
{
    public bool UserProceeded { get; private set; } = false;

    public AddGameStep2Window()
    {
        InitializeComponent();
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
