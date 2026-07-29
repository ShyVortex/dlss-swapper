using Avalonia.Controls;
using Avalonia.Interactivity;
using DLSS_Swapper.Avalonia.ViewModels;
using DLSS_Swapper.Core.Services;
using DLSS_Swapper.Helpers;

namespace DLSS_Swapper.Avalonia.Views;

public partial class DLLPickerControl : Window
{
    public DLLPickerControl()
    {
        InitializeComponent();
        UpdateTranslations();
        LinuxLanguageService.Instance.OnLanguageChanged += UpdateTranslations;
    }

    private void UpdateTranslations()
    {
        OriginalLabelTextBlock.Text = ResourceHelper.GetString("GamePage_OriginalDll", "Original");
        CurrentLabelTextBlock.Text = ResourceHelper.GetString("GamePage_CurrentDll", "Current");
        ToolTip.SetTip(ResetToOriginalButton, ResourceHelper.GetString("GamePage_RestoreOriginalDll", "Restore original DLL"));
        ToolTip.SetTip(OpenFolderButton, ResourceHelper.GetString("GamePage_OpenDllLocation", "Open DLL location"));
        SwapButton.Content = ResourceHelper.GetString("General_Swap", "Swap");
        CancelButton.Content = ResourceHelper.GetString("General_Cancel", "Cancel");
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
