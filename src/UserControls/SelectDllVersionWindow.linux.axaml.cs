using Avalonia.Controls;
using Avalonia.Interactivity;
using DLSS_Swapper.Avalonia.ViewModels;

namespace DLSS_Swapper.Avalonia.Views;

public partial class SelectDllVersionWindow : Window
{
    public SelectDllVersionWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(System.EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is SelectDllVersionViewModel vm)
        {
            vm.CloseWindowAction = Close;
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
