using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using DLSS_Swapper.Avalonia.ViewModels;
using DLSS_Swapper.Core.Services;

namespace DLSS_Swapper.Avalonia.Views;

public partial class GameDetailsWindow : Window
{
    public GameCardItem? SelectedGame { get; }

    public GameDetailsWindow() : this(new GameCardItem())
    {
    }

    public GameDetailsWindow(GameCardItem game)
    {
        InitializeComponent();
        SelectedGame = game;
        DataContext = game;
        game.LoadPresets();
        UpdateTranslations();
        LoadDevOptions();
        LinuxLanguageService.Instance.OnLanguageChanged += UpdateTranslations;
    }

    private void UpdateTranslations()
    {
        Title = DLSS_Swapper.Helpers.ResourceHelper.GetString("GamesPage_Title", "Game Details");
        AddCustomCoverButton.Content = DLSS_Swapper.Helpers.ResourceHelper.GetString("GamePage_AddCustomCover", "Add Custom Cover");
        NameLabelTextBlock.Text = DLSS_Swapper.Helpers.ResourceHelper.GetString("General_Name", "Name");
        InstallPathLabelTextBlock.Text = DLSS_Swapper.Helpers.ResourceHelper.GetString("GamePage_InstallPath", "Install path");
        DlssLabelTextBlock.Text = DLSS_Swapper.Helpers.ResourceHelper.GetString("General_Name_DLSS", "DLSS");
        DlssPresetLabelTextBlock.Text = DLSS_Swapper.Helpers.ResourceHelper.GetString("GamePage_DLSSPreset", "DLSS Preset");
        var tooltip = DLSS_Swapper.Helpers.ResourceHelper.GetString("GamePage_DLSSPresetInfo_Tooltip", "Setting a DLSS preset does not guarantee that the preset is used.");
        ToolTip.SetTip(DlssPresetInfoIcon, tooltip);
        DlssRrLabelTextBlock.Text = DLSS_Swapper.Helpers.ResourceHelper.GetString("General_Name_DLSS_D", "DLSS Ray Reconstruction");
        DlssRrPresetLabelTextBlock.Text = DLSS_Swapper.Helpers.ResourceHelper.GetString("GamePage_DLSSDPreset", "DLSS RR Preset");
        ToolTip.SetTip(DlssRrPresetInfoIcon, tooltip);
        DlssFgLabelTextBlock.Text = DLSS_Swapper.Helpers.ResourceHelper.GetString("General_Name_DLSS_G", "DLSS Frame Generation");
        DlssFgPresetLabelTextBlock.Text = DLSS_Swapper.Helpers.ResourceHelper.GetString("GamePage_DLSSGPreset", "DLSS FG Preset");
        ToolTip.SetTip(DlssFgPresetInfoIcon, tooltip);
        FsrDx12LabelTextBlock.Text = DLSS_Swapper.Helpers.ResourceHelper.GetString("General_Name_FSR_31_DX12", "FSR 3.1 DirectX 12");
        XessLabelTextBlock.Text = DLSS_Swapper.Helpers.ResourceHelper.GetString("General_Name_XeSS", "XeSS");
        XessDx11LabelTextBlock.Text = DLSS_Swapper.Helpers.ResourceHelper.GetString("General_Name_XeSS_DX11", "XeSS (DX11)");
        XellLabelTextBlock.Text = DLSS_Swapper.Helpers.ResourceHelper.GetString("General_Name_XeLL", "XeLL");
        ToolTip.SetTip(PlayButton, DLSS_Swapper.Helpers.ResourceHelper.GetString("GamePage_Launch", "Launch"));
        ToolTip.SetTip(NotesButton, DLSS_Swapper.Helpers.ResourceHelper.GetString("GamePage_Notes", "Notes"));
        ToolTip.SetTip(HistoryButton, DLSS_Swapper.Helpers.ResourceHelper.GetString("GamePage_History", "History"));
        ToolTip.SetTip(FavouriteButton, DLSS_Swapper.Helpers.ResourceHelper.GetString("GamePage_Favorited", "Favorited"));
        ToolTip.SetTip(RefreshButton, DLSS_Swapper.Helpers.ResourceHelper.GetString("General_Refresh", "Refresh"));
        CloseButton.Content = DLSS_Swapper.Helpers.ResourceHelper.GetString("General_Close", "Close");
        RefreshLoadingOverlayTextBlock.Text = DLSS_Swapper.Helpers.ResourceHelper.GetString("GamesPage_ReloadingGame", "Refreshing game details...");
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void OnNotesClick(object? sender, RoutedEventArgs e)
    {
        if (SelectedGame == null) return;
        var dialog = new GameNotesWindow(SelectedGame.GameId, SelectedGame.Name);
        await dialog.ShowDialog(this);
    }

    private async void OnHistoryClick(object? sender, RoutedEventArgs e)
    {
        if (SelectedGame == null) return;
        var dialog = new GameHistoryWindow(SelectedGame.GameId, SelectedGame.Name);
        await dialog.ShowDialog(this);
    }

    private async void OnRefreshClick(object? sender, RoutedEventArgs e)
    {
        if (SelectedGame == null || string.IsNullOrEmpty(SelectedGame.InstallPath)) return;

        RefreshLoadingOverlay.IsVisible = true;
        try
        {
            var installPath = SelectedGame.InstallPath;
            await System.Threading.Tasks.Task.Run(() =>
            {
                var scanner = new LinuxSteamLibraryScanner();
                var dlss = scanner.ScanDllVersion(installPath, "nvngx_dlss.dll");
                var dlssg = scanner.ScanDllVersion(installPath, "nvngx_dlssg.dll");
                var dlssd = scanner.ScanDllVersion(installPath, "nvngx_dlssd.dll");
                var fsrDx12 = scanner.ScanDllVersion(installPath, "amd_fidelityfx_dx12.dll", "ffx_fsr31_x64.dll", "ffx_fsr31_dx12_x64.dll");
                var fsrVk = scanner.ScanDllVersion(installPath, "amd_fidelityfx_vk.dll", "ffx_fsr31_vk_x64.dll");
                var xess = scanner.ScanDllVersion(installPath, "libxess.dll");
                var xessDx11 = scanner.ScanDllVersion(installPath, "libxess_dx11.dll");
                var xessFg = scanner.ScanDllVersion(installPath, "libxess_fg.dll");
                var xell = scanner.ScanDllVersion(installPath, "libxell.dll");

                global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    SelectedGame.DLSSVersion = dlss;
                    SelectedGame.DLSSGVersion = dlssg;
                    SelectedGame.DLSSDVersion = dlssd;
                    SelectedGame.Fsr31Dx12Version = fsrDx12;
                    SelectedGame.Fsr31VkVersion = fsrVk;
                    SelectedGame.XessVersion = xess;
                    SelectedGame.XessDx11Version = xessDx11;
                    SelectedGame.XessFgVersion = xessFg;
                    SelectedGame.XellVersion = xell;
                    SelectedGame.LoadPresets();
                });
            });

            // Ensure smooth visual transition
            await System.Threading.Tasks.Task.Delay(300);
        }
        finally
        {
            RefreshLoadingOverlay.IsVisible = false;
        }
    }

    private void OnPlayClick(object? sender, RoutedEventArgs e)
    {
        if (SelectedGame == null) return;

        LinuxGameLauncherService.LaunchGame(
            SelectedGame.LibraryName,
            SelectedGame.AppId,
            SelectedGame.InstallPath,
            async () =>
            {
                // Show prompt dialog asking if user wants to install proton-autogen
                var confirmWindow = new Window
                {
                    Title = "Install Proton Autogen",
                    Width = 420,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    CanResize = false,
                    Background = global::Avalonia.Media.Brush.Parse("#242424")
                };

                bool result = false;
                var text = new TextBlock
                {
                    Text = "Proton Autogen is required to launch manually added games without Steam.\n\nWould you like to install proton-autogen now?",
                    Foreground = global::Avalonia.Media.Brushes.White,
                    FontSize = 13,
                    TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
                    Margin = new global::Avalonia.Thickness(20)
                };

                var yesBtn = new Button { Content = "Yes, Install", Width = 110, Height = 34, Margin = new global::Avalonia.Thickness(5), Background = global::Avalonia.Media.Brush.Parse("#E85A24"), Foreground = global::Avalonia.Media.Brushes.White };
                var noBtn = new Button { Content = "Cancel", Width = 90, Height = 34, Margin = new global::Avalonia.Thickness(5), Background = global::Avalonia.Media.Brush.Parse("#383838"), Foreground = global::Avalonia.Media.Brushes.White };

                yesBtn.Click += (_, _) => { result = true; confirmWindow.Close(); };
                noBtn.Click += (_, _) => { result = false; confirmWindow.Close(); };

                var btnStack = new StackPanel { Orientation = global::Avalonia.Layout.Orientation.Horizontal, HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Right, Margin = new global::Avalonia.Thickness(15) };
                btnStack.Children.Add(yesBtn);
                btnStack.Children.Add(noBtn);

                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = new global::Avalonia.Controls.GridLength(1, global::Avalonia.Controls.GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition { Height = global::Avalonia.Controls.GridLength.Auto });

                Grid.SetRow(text, 0);
                Grid.SetRow(btnStack, 1);
                grid.Children.Add(text);
                grid.Children.Add(btnStack);

                confirmWindow.Content = grid;
                await confirmWindow.ShowDialog(this);
                return result;
            }
        );
    }

    private void OnFavouriteClick(object? sender, RoutedEventArgs e)
    {
        if (SelectedGame != null)
        {
            SelectedGame.IsFavourite = !SelectedGame.IsFavourite;
        }
    }

    private void OnDlssClick(object? sender, RoutedEventArgs e) => OpenVersionPicker("dlss", SelectedGame?.DLSSVersion);
    private void OnDlssRrClick(object? sender, RoutedEventArgs e) => OpenVersionPicker("dlss_d", SelectedGame?.DLSSDVersion);
    private void OnDlssFgClick(object? sender, RoutedEventArgs e) => OpenVersionPicker("dlss_g", SelectedGame?.DLSSGVersion);
    private void OnFsrDx12Click(object? sender, RoutedEventArgs e) => OpenVersionPicker("fsr_31_dx12", SelectedGame?.Fsr31Dx12Version);
    private void OnFsrVkClick(object? sender, RoutedEventArgs e) => OpenVersionPicker("fsr_31_vk", SelectedGame?.Fsr31VkVersion);
    private void OnXessClick(object? sender, RoutedEventArgs e) => OpenVersionPicker("xess", SelectedGame?.XessVersion);
    private void OnXessDx11Click(object? sender, RoutedEventArgs e) => OpenVersionPicker("xess_dx11", SelectedGame?.XessDx11Version);
    private void OnXessFgClick(object? sender, RoutedEventArgs e) => OpenVersionPicker("xess_fg", SelectedGame?.XessFgVersion);
    private void OnXellClick(object? sender, RoutedEventArgs e) => OpenVersionPicker("xell", SelectedGame?.XellVersion);

    private async void OpenVersionPicker(string categoryType, string? currentVersion)
    {
        if (SelectedGame == null || string.IsNullOrEmpty(SelectedGame.InstallPath)) return;
        if (string.IsNullOrEmpty(currentVersion) || currentVersion == "Not found" || currentVersion == "N/A") return;

        var possibleFilenames = LibraryStorageService.GetPossibleDllFilenamesForType(categoryType);
        var targetDllPath = string.Empty;

        try
        {
            if (Directory.Exists(SelectedGame.InstallPath))
            {
                foreach (var fname in possibleFilenames)
                {
                    var files = Directory.GetFiles(SelectedGame.InstallPath, fname, SearchOption.AllDirectories);
                    if (files.Length > 0)
                    {
                        targetDllPath = files[0];
                        break;
                    }
                }
            }
        }
        catch
        {
        }

        if (string.IsNullOrEmpty(targetDllPath))
        {
            targetDllPath = Path.Combine(SelectedGame.InstallPath, possibleFilenames[0]);
        }

        var storageService = new LibraryStorageService();
        var vm = new SelectDllVersionViewModel(categoryType, targetDllPath, currentVersion, storageService);

        var dialog = new DLLPickerControl
        {
            DataContext = vm
        };

        await dialog.ShowDialog(this);

        if (!string.IsNullOrEmpty(vm.ResultSwappedPath) && File.Exists(vm.ResultSwappedPath))
        {
            var scanner = new LinuxSteamLibraryScanner();
            var newVer = scanner.ExtractDllVersionFromFile(vm.ResultSwappedPath);
            if (!string.IsNullOrEmpty(newVer))
            {
                UpdateGameVersion(categoryType, newVer);
                var historyService = new GameHistoryService();
                var assetType = GetGameAssetTypeForCategory(categoryType);
                _ = historyService.AddEventAsync(SelectedGame.GameId, DLSS_Swapper.Data.GameHistoryEventType.DLLSwapped, assetType, newVer, vm.ResultSwappedPath);
            }
        }
    }

    private void UpdateGameVersion(string categoryType, string newVer)
    {
        if (SelectedGame == null) return;
        switch (categoryType.ToLowerInvariant())
        {
            case "dlss": SelectedGame.DLSSVersion = newVer; break;
            case "dlss_g": SelectedGame.DLSSGVersion = newVer; break;
            case "dlss_d": SelectedGame.DLSSDVersion = newVer; break;
            case "fsr_31_dx12": SelectedGame.Fsr31Dx12Version = newVer; break;
            case "fsr_31_vk": SelectedGame.Fsr31VkVersion = newVer; break;
            case "xess": SelectedGame.XessVersion = newVer; break;
            case "xess_dx11": SelectedGame.XessDx11Version = newVer; break;
            case "xess_fg": SelectedGame.XessFgVersion = newVer; break;
            case "xell": SelectedGame.XellVersion = newVer; break;
        }
    }

    private static DLSS_Swapper.Data.GameAssetType GetGameAssetTypeForCategory(string category)
    {
        return category.ToLowerInvariant() switch
        {
            "dlss" => DLSS_Swapper.Data.GameAssetType.DLSS,
            "dlss_g" => DLSS_Swapper.Data.GameAssetType.DLSS_G,
            "dlss_d" => DLSS_Swapper.Data.GameAssetType.DLSS_D,
            "fsr_31_dx12" => DLSS_Swapper.Data.GameAssetType.FSR_31_DX12,
            "fsr_31_vk" => DLSS_Swapper.Data.GameAssetType.FSR_31_VK,
            "xess" => DLSS_Swapper.Data.GameAssetType.XeSS,
            "xess_dx11" => DLSS_Swapper.Data.GameAssetType.XeSS_DX11,
            "xess_fg" => DLSS_Swapper.Data.GameAssetType.XeSS_FG,
            "xell" => DLSS_Swapper.Data.GameAssetType.XeLL,
            _ => DLSS_Swapper.Data.GameAssetType.Unknown
        };
    }

    private bool _isInitializingDevOptions = false;

    private string? GetUserRegPath()
    {
        if (SelectedGame == null) return null;

        if (!string.IsNullOrEmpty(SelectedGame.AppId))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var candidates = new[]
            {
                Path.Combine(home, ".steam", "steam", "steamapps", "compatdata", SelectedGame.AppId, "pfx", "user.reg"),
                Path.Combine(home, ".local", "share", "Steam", "steamapps", "compatdata", SelectedGame.AppId, "pfx", "user.reg"),
                Path.Combine(home, ".var", "app", "com.valvesoftware.Steam", "data", "Steam", "steamapps", "compatdata", SelectedGame.AppId, "pfx", "user.reg")
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate)) return candidate;
            }
        }

        if (!string.IsNullOrEmpty(SelectedGame.InstallPath))
        {
            var pfxUserReg = Path.Combine(SelectedGame.InstallPath, "pfx", "user.reg");
            if (File.Exists(pfxUserReg)) return pfxUserReg;

            var parentPfx = Path.Combine(Directory.GetParent(SelectedGame.InstallPath)?.FullName ?? string.Empty, "pfx", "user.reg");
            if (File.Exists(parentPfx)) return parentPfx;
        }

        return null;
    }

    private void LoadDevOptions()
    {
        _isInitializingDevOptions = true;
        try
        {
            var regPath = GetUserRegPath();
            if (regPath != null && File.Exists(regPath))
            {
                var options = DLSS_Swapper.LinuxCore.Services.ProtonRegistryService.ReadDeveloperOptions(regPath);

                if (IndicatorComboBox != null && IndicatorComboBox.Items != null)
                {
                    foreach (var rawItem in IndicatorComboBox.Items)
                    {
                        if (rawItem is ComboBoxItem item && item.Tag?.ToString() == options.IndicatorOption.ToString())
                        {
                            IndicatorComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }

                if (EnableLoggingToFileCheckBox != null)
                    EnableLoggingToFileCheckBox.IsChecked = options.EnableLoggingToFile;

                if (VerboseLoggingCheckBox != null)
                    VerboseLoggingCheckBox.IsChecked = options.VerboseLogging;

                if (EnableLoggingToConsoleCheckBox != null)
                    EnableLoggingToConsoleCheckBox.IsChecked = options.EnableLoggingToConsole;
            }
            else if (IndicatorComboBox != null)
            {
                IndicatorComboBox.SelectedIndex = 0;
            }
        }
        catch { }
        finally
        {
            _isInitializingDevOptions = false;
        }
    }

    private void OnDevOptionChanged(object? sender, RoutedEventArgs e)
    {
        if (_isInitializingDevOptions || SelectedGame == null) return;

        var regPath = GetUserRegPath();
        if (regPath == null || !File.Exists(regPath)) return;

        int indicator = 0;
        if (IndicatorComboBox?.SelectedItem is ComboBoxItem item && int.TryParse(item.Tag?.ToString(), out var opt))
        {
            indicator = opt;
        }

        var options = new DLSS_Swapper.LinuxCore.Services.NgxDeveloperOptions
        {
            IndicatorOption = indicator,
            EnableLoggingToFile = EnableLoggingToFileCheckBox?.IsChecked == true,
            VerboseLogging = VerboseLoggingCheckBox?.IsChecked == true,
            EnableLoggingToConsole = EnableLoggingToConsoleCheckBox?.IsChecked == true
        };

        DLSS_Swapper.LinuxCore.Services.ProtonRegistryService.WriteDeveloperOptions(regPath, options);
    }
}
