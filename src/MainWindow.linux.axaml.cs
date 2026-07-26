using System;
using System.Linq;
using System.Threading.Tasks;
using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Input;
using global::Avalonia.Interactivity;
using global::Avalonia.Layout;
using global::Avalonia.Media;
using global::Avalonia.Platform.Storage;
using DLSS_Swapper.Avalonia.ViewModels;

namespace DLSS_Swapper.Avalonia.Views;

public partial class MainWindow : Window
{
    private static bool _skipAddGameDisclaimers;

    public MainWindow()
    {
        InitializeComponent();
        var vm = new MainWindowViewModel();
        DataContext = vm;

        vm.LibraryViewModel.OpenFilePickerAsync = OpenLibraryFilePickerAsync;
        vm.LibraryViewModel.SaveFilePickerAsync = SaveLibraryFilePickerAsync;
        vm.LibraryViewModel.ShowMessageDialogAsync = ShowMessageDialogAsync;
        vm.LibraryViewModel.ExportWithProgressAsync = ExportWithProgressAsync;
        vm.LibraryViewModel.DownloadBatchWithProgressAsync = DownloadBatchWithProgressAsync;
        vm.LibraryViewModel.OpenNvidiaImportDialogAsync = OpenNvidiaImportDialogAsync;

        Opened += OnWindowOpened;
    }

    private async Task<System.Collections.Generic.List<NvidiaModelRowItem>> OpenNvidiaImportDialogAsync(bool isDriverImport)
    {
        var win = new ImportNvidiaWindow(isDriverImport);
        await win.ShowDialog(this);
        return win.SelectedItems;
    }

    private async Task<System.Collections.Generic.IReadOnlyList<string>> OpenLibraryFilePickerAsync()
    {
        var storage = StorageProvider;
        if (storage != null)
        {
            var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select DLL or ZIP file to import",
                AllowMultiple = true,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("DLL or ZIP archives") { Patterns = new[] { "*.dll", "*.zip" } },
                    new FilePickerFileType("DLL files") { Patterns = new[] { "*.dll" } },
                    new FilePickerFileType("ZIP archives") { Patterns = new[] { "*.zip" } }
                }
            });

            return files.Select(f => f.Path.LocalPath).ToList();
        }

        return System.Array.Empty<string>();
    }

    private async Task<string?> SaveLibraryFilePickerAsync(string defaultFileName)
    {
        var storage = StorageProvider;
        if (storage != null)
        {
            var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export All DLLs to ZIP",
                SuggestedFileName = defaultFileName,
                DefaultExtension = "zip",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("ZIP archives (*.zip)") { Patterns = new[] { "*.zip" } }
                }
            });

            return file?.Path.LocalPath;
        }

        return null;
    }

    private async Task ShowMessageDialogAsync(string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 460,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Background = Brush.Parse("#242424")
        };

        var border = new Border
        {
            Padding = new Thickness(24),
            Background = Brush.Parse("#242424"),
            CornerRadius = new CornerRadius(8)
        };

        var stack = new StackPanel { Spacing = 16 };
        var titleText = new TextBlock
        {
            Text = title,
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brushes.White
        };

        var msgText = new TextBlock
        {
            Text = message,
            FontSize = 13,
            Foreground = Brush.Parse("#DDDDDD"),
            TextWrapping = TextWrapping.Wrap
        };

        var okBtn = new Button
        {
            Content = "OK",
            HorizontalAlignment = HorizontalAlignment.Right,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Width = 100,
            Height = 36,
            Background = Brush.Parse("#E85A24"),
            Foreground = Brushes.White,
            FontWeight = FontWeight.SemiBold,
            CornerRadius = new CornerRadius(4),
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        okBtn.Click += (s, e) => dialog.Close();

        stack.Children.Add(titleText);
        stack.Children.Add(msgText);
        stack.Children.Add(okBtn);
        border.Child = stack;
        dialog.Content = border;

        await dialog.ShowDialog(this);
    }

    private async Task<(bool Success, int ExportedCount, string ErrorMessage)> ExportWithProgressAsync(string zipPath)
    {
        var dialog = new Window
        {
            Title = "Exporting DLLs...",
            Width = 420,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Background = Brush.Parse("#242424")
        };

        var border = new Border
        {
            Padding = new Thickness(24),
            Background = Brush.Parse("#242424"),
            CornerRadius = new CornerRadius(8)
        };

        var stack = new StackPanel { Spacing = 16 };

        var titleText = new TextBlock
        {
            Text = "Exporting DLLs...",
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brushes.White
        };

        var progressBar = new ProgressBar
        {
            Height = 8,
            IsIndeterminate = true,
            Minimum = 0,
            Maximum = 100,
            Foreground = Brush.Parse("#E85A24"),
            Background = Brush.Parse("#333333"),
            CornerRadius = new CornerRadius(4)
        };

        var statusText = new TextBlock
        {
            Text = "Preparing export archive...",
            FontSize = 13,
            Foreground = Brush.Parse("#AAAAAA")
        };

        stack.Children.Add(titleText);
        stack.Children.Add(progressBar);
        stack.Children.Add(statusText);
        border.Child = stack;
        dialog.Content = border;

        (bool Success, int ExportedCount, string ErrorMessage) result = (false, 0, string.Empty);

        var storageService = new DLSS_Swapper.Core.Services.LibraryStorageService();

        var dialogTask = dialog.ShowDialog(this);

        result = await storageService.ExportAllToZipAsync(zipPath, (current, total) =>
        {
            global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (total > 0)
                {
                    progressBar.IsIndeterminate = false;
                    progressBar.Maximum = total;
                    progressBar.Value = current;
                    statusText.Text = $"Exported {current} of {total} file(s)...";
                }
            });
        });

        dialog.Close();
        await dialogTask;

        return result;
    }

    private async Task DownloadBatchWithProgressAsync(System.Collections.Generic.List<(string CategoryKey, DLSS_Swapper.Core.Models.DllRecordModel Record, string CategoryName)> items)
    {
        if (items == null || items.Count == 0) return;

        var dialog = new Window
        {
            Title = "Downloading Latest DLLs...",
            Width = 440,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Background = Brush.Parse("#242424")
        };

        var border = new Border
        {
            Padding = new Thickness(24),
            Background = Brush.Parse("#242424"),
            CornerRadius = new CornerRadius(8)
        };

        var stack = new StackPanel { Spacing = 16 };

        var titleText = new TextBlock
        {
            Text = "Downloading Latest DLLs...",
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brushes.White
        };

        var progressBar = new ProgressBar
        {
            Height = 8,
            Minimum = 0,
            Maximum = items.Count,
            Value = 0,
            Foreground = Brush.Parse("#E85A24"),
            Background = Brush.Parse("#333333"),
            CornerRadius = new CornerRadius(4)
        };

        var statusText = new TextBlock
        {
            Text = $"Preparing to download {items.Count} file(s)...",
            FontSize = 13,
            Foreground = Brush.Parse("#AAAAAA")
        };

        stack.Children.Add(titleText);
        stack.Children.Add(progressBar);
        stack.Children.Add(statusText);
        border.Child = stack;
        dialog.Content = border;

        var storageService = new DLSS_Swapper.Core.Services.LibraryStorageService();

        var dialogTask = dialog.ShowDialog(this);

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            int currentStep = i + 1;

            global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                progressBar.Value = i;
                statusText.Text = $"Downloading {currentStep} of {items.Count}: {item.CategoryName} (v{item.Record.Version})...";
            });

            await storageService.DownloadAndExtractAsync(item.CategoryKey, item.Record, p =>
            {
                global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    progressBar.Value = i + (p / 100.0);
                });
            });
        }

        global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            progressBar.Value = items.Count;
            statusText.Text = "All downloads complete!";
        });

        await Task.Delay(400);

        dialog.Close();
        await dialogTask;
    }

    private async void OnWindowOpened(object? sender, EventArgs e)
    {
        await Task.Delay(50);
        if (Screens.Primary is { } primaryScreen)
        {
            var workArea = primaryScreen.WorkingArea;
            var scaling = primaryScreen.Scaling > 0 ? primaryScreen.Scaling : DesktopScaling;

            // Compute logical dimensions of the usable area (excludes taskbars/panels)
            double screenLogicalWidth = workArea.Width / scaling;
            double screenLogicalHeight = workArea.Height / scaling;

            double winWidth = Bounds.Width > 0 ? Bounds.Width : Width;
            double winHeight = Bounds.Height > 0 ? Bounds.Height : Height;

            // Target DIP coordinates relative to primary screen working area origin
            double targetLogicalX = (workArea.X / scaling) + (screenLogicalWidth - winWidth) / 2.0;
            double targetLogicalY = (workArea.Y / scaling) + (screenLogicalHeight - winHeight) / 2.0;

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
