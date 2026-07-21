using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using DLSS_Swapper.Avalonia.ViewModels;

namespace DLSS_Swapper.Avalonia.Views;

public partial class MainWindow : Window
{
    private static bool _skipAddGameDisclaimers;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }

    private void OnWindowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        FocusManager?.ClearFocus();
    }

    private async void OnFilterClick(object? sender, RoutedEventArgs e)
    {
        var filterWin = new FilterWindow();
        await filterWin.ShowDialog(this);
    }

    private async void OnAddGameClick(object? sender, RoutedEventArgs e)
    {
        if (_skipAddGameDisclaimers)
        {
            await OpenGameFolderPickerAsync();
            return;
        }

        var step1 = new AddGameStep1Window();
        await step1.ShowDialog(this);

        if (step1.UserProceeded)
        {
            if (step1.DontShowAgain)
            {
                _skipAddGameDisclaimers = true;
            }

            var step2 = new AddGameStep2Window();
            await step2.ShowDialog(this);

            if (step2.UserProceeded)
            {
                await OpenGameFolderPickerAsync();
            }
        }
    }

    private async Task OpenGameFolderPickerAsync()
    {
        var storage = StorageProvider;
        if (storage != null)
        {
            var result = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Game Directory",
                AllowMultiple = false
            });

            if (result.Count > 0)
            {
                var path = result[0].Path.LocalPath;
                if (DataContext is MainWindowViewModel vm)
                {
                    vm.GameGridViewModel.AddManualGameFolder(path);
                }
            }
        }
    }
}
