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

        Opened += OnWindowOpened;
    }

    private async void OnWindowOpened(object? sender, EventArgs e)
    {
        await Task.Delay(50);
        if (Screens.Primary is { } primaryScreen)
        {
            var bounds = primaryScreen.Bounds;
            var scaling = primaryScreen.Scaling > 0 ? primaryScreen.Scaling : DesktopScaling;

            // Compute logical width of primary monitor
            double screenLogicalWidth = bounds.Width / scaling;
            double screenLogicalHeight = bounds.Height / scaling;

            double winWidth = Bounds.Width > 0 ? Bounds.Width : Width;
            double winHeight = Bounds.Height > 0 ? Bounds.Height : Height;

            // Target DIP coordinates relative to primary screen origin
            double targetLogicalX = (bounds.X / scaling) + (screenLogicalWidth - winWidth) / 2.0;
            double targetLogicalY = (bounds.Y / scaling) + (screenLogicalHeight - winHeight) / 2.0;

            // Convert logical target coordinates to physical PixelPoint
            int pixelX = (int)(targetLogicalX * scaling);
            int pixelY = (int)(targetLogicalY * scaling);

            Position = new global::Avalonia.PixelPoint(pixelX, pixelY);
        }
    }

    private void OnWindowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        FocusManager?.ClearFocus();
    }

    private async void OnFilterClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            var gridVm = vm.GameGridViewModel;
            var filterWin = new FilterWindow(gridVm.HideNoSwappableItems, gridVm.ShowHiddenGames, gridVm.GroupByLibrary);
            await filterWin.ShowDialog(this);

            if (filterWin.Applied)
            {
                gridVm.HideNoSwappableItems = filterWin.HideNoSwappableItems;
                gridVm.ShowHiddenGames = filterWin.ShowHiddenGames;
                gridVm.GroupByLibrary = filterWin.GroupByLibrary;
                gridVm.ApplyFilters();
            }
        }
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
