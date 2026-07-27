using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using DLSS_Swapper.Core.Services;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.LinuxUI.Services;

namespace DLSS_Swapper.Avalonia.Views;

public partial class SettingsView : UserControl
{
    private bool _isInitializing = true;
    public ObservableCollection<string> IgnoredPaths { get; } = new ObservableCollection<string>();

    public SettingsView()
    {
        InitializeComponent();
        IgnoredPathsItemsControl.ItemsSource = IgnoredPaths;
        LoadSettings();
        UpdateTranslations();
        LinuxLanguageService.Instance.OnLanguageChanged += UpdateTranslations;
    }

    private void UpdateTranslations()
    {
        HeaderTitleTextBlock.Text = ResourceHelper.GetString("SettingsPage_Title");
        ThemeTitleTextBlock.Text = ResourceHelper.GetString("SettingsPage_ThemeMode");
        AccentColorTitleTextBlock.Text = ResourceHelper.GetString("SettingsPage_AccentColor", "Accent Color");
        GameLibrariesTitleTextBlock.Text = ResourceHelper.GetString("SettingsPage_GameLibraries");
        IgnoredPathsTitleTextBlock.Text = ResourceHelper.GetString("SettingsPage_IgnoredPaths");
        AddIgnoredPathButton.Content = ResourceHelper.GetString("SettingsPage_AddIgnoredPath");
        DlssOptionsTitleTextBlock.Text = ResourceHelper.GetString("SettingsPage_DLSSOptions");
        GlobalSrTitleTextBlock.Text = ResourceHelper.GetString("SettingsPage_DLSSOptions_GlobalPreset", "Global Super Resolution preset");
        GlobalRrTitleTextBlock.Text = ResourceHelper.GetString("SettingsPage_DLSSOptions_GlobalRayReconstructionPreset", "Global Ray Reconstruction preset");
        GlobalFgTitleTextBlock.Text = ResourceHelper.GetString("SettingsPage_DLSSOptions_GlobalFrameGenerationPreset", "Global Frame Generation preset");
        DlssDeveloperTitleTextBlock.Text = ResourceHelper.GetString("SettingsPage_DLSSDeveloperOptions");
        OnScreenIndicatorTitleTextBlock.Text = ResourceHelper.GetString("SettingsPage_DLSSDeveloperOptions_ShowOnScreenIndicator");
        EnableLoggingToFileCheckBox.Content = ResourceHelper.GetString("SettingsPage_DLSSDeveloperOptions_EnableLoggingToFile");
        VerboseLoggingCheckBox.Content = ResourceHelper.GetString("SettingsPage_DLSSDeveloperOptions_VerboseLogging");
        EnableLoggingToConsoleCheckBox.Content = ResourceHelper.GetString("SettingsPage_DLSSDeveloperOptions_EnableLoggingToConsoleWindow");
        AllowUntrustedTitleTextBlock.Text = ResourceHelper.GetString("SettingsPage_SettingsAllowUntrusted", "Allow Untrusted");
        AllowUntrustedCaptionTextBlock.Text = ResourceHelper.GetString("SettingsPage_AllowUntrustedInfo");
        AllowDebugDllsTitleTextBlock.Text = ResourceHelper.GetString("SettingsPage_AllowDebugDlls");
        AllowDebugDllsCaptionTextBlock.Text = ResourceHelper.GetString("SettingsPage_AllowDebugDllsInfo");
        OnlyShowDownloadedTitleTextBlock.Text = ResourceHelper.GetString("SettingsPage_ShowOnlyDownloadedDlls");
        OnlyShowDownloadedCaptionTextBlock.Text = ResourceHelper.GetString("SettingsPage_AppliesOnlyToDllPickerNotLibrary");
        NetworkingTitleTextBlock.Text = ResourceHelper.GetString("SettingsPage_Networking", "Networking");
        ProxySettingsButton.Content = ResourceHelper.GetString("SettingsPage_ProxySettings");
        LoggingTitleTextBlock.Text = ResourceHelper.GetString("SettingsPage_Logging");
        CurrentLogFileLabelTextBlock.Text = ResourceHelper.GetString("SettingsPage_YourCurrentLogFile");
        LanguageTitleTextBlock.Text = ResourceHelper.GetString("SettingsPage_Language");
        OpenTranslationToolboxButton.Content = ResourceHelper.GetString("SettingsPage_OpenTranslationToolbox");
    }

    private void LoadSettings()
    {
        _isInitializing = true;

        try
        {
            var settings = LinuxSettingsService.Instance.Settings;

            // 1. Theme Mode
            if (settings.AppTheme == LinuxAppTheme.Light)
                LightThemeRadioButton.IsChecked = true;
            else if (settings.AppTheme == LinuxAppTheme.Dark)
                DarkThemeRadioButton.IsChecked = true;
            else
                DefaultThemeRadioButton.IsChecked = true;

            AccentColorHexTextBox.Text = settings.AccentColor;
            LinuxAccentColorService.Instance.ApplyAccentColor(settings.AccentColor);

            // 2. Game Library Toggles
            SteamToggle.IsChecked = settings.EnableSteam;
            HeroicToggle.IsChecked = settings.EnableHeroic;
            GogToggle.IsChecked = settings.EnableGog;
            EpicToggle.IsChecked = settings.EnableEpic;
            ManuallyAddedToggle.IsChecked = settings.EnableManuallyAdded;

            // 3. Ignored Paths
            IgnoredPaths.Clear();
            if (settings.IgnoredPaths != null)
            {
                foreach (var p in settings.IgnoredPaths)
                {
                    IgnoredPaths.Add(p);
                }
            }

            // 4. DLSS Presets
            var srPresets = LinuxPresetService.Instance.GetDlssSrPresets();
            DlssPresetComboBox.ItemsSource = srPresets;
            DlssPresetComboBox.SelectedItem = srPresets.FirstOrDefault(x => x.Name == settings.DlssPreset || x.Value == settings.DlssPreset) ?? srPresets.FirstOrDefault();

            var rrPresets = LinuxPresetService.Instance.GetDlssRrPresets();
            DlssDPresetComboBox.ItemsSource = rrPresets;
            DlssDPresetComboBox.SelectedItem = rrPresets.FirstOrDefault(x => x.Name == settings.DlssDPreset || x.Value == settings.DlssDPreset) ?? rrPresets.FirstOrDefault();

            var fgPresets = LinuxPresetService.Instance.GetDlssFgPresets();
            DlssGPresetComboBox.ItemsSource = fgPresets;
            DlssGPresetComboBox.SelectedItem = fgPresets.FirstOrDefault(x => x.Name == settings.DlssGPreset || x.Value == settings.DlssGPreset) ?? fgPresets.FirstOrDefault();

            // 5. Developer Options
            SelectComboBoxItemByTag(IndicatorComboBox, settings.IndicatorOption.ToString());
            EnableLoggingToFileCheckBox.IsChecked = settings.EnableLoggingToFile;
            VerboseLoggingCheckBox.IsChecked = settings.VerboseLogging;
            EnableLoggingToConsoleCheckBox.IsChecked = settings.EnableLoggingToConsole;

            // 6. Toggles
            AllowUntrustedToggle.IsChecked = settings.AllowUntrusted;
            AllowDebugDllsToggle.IsChecked = settings.AllowDebugDlls;
            OnlyShowDownloadedDllsToggle.IsChecked = settings.OnlyShowDownloadedDlls;

            // 7. Logging Level
            SelectComboBoxItemByTag(LoggingLevelComboBox, settings.LoggingLevel.ToString());

            // 8. Log File Path
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".config",
                "dlss-swapper",
                "logs"
            );
            Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, $"dlss_swapper_{DateTime.Now:yyyyMMdd}.log");
            CurrentLogFileTextBlock.Text = logPath;

            // 9. Language Selection
            var languages = LinuxLanguageService.Instance.GetAvailableLanguages();
            LanguageComboBox.ItemsSource = languages.Select(x => x.Value).ToList();
            var currentLangPair = languages.FirstOrDefault(x => x.Key == settings.Language);
            LanguageComboBox.SelectedItem = currentLangPair.Value ?? languages[7].Value; // Default en-US

            // 10. About metadata
            VersionTextBlock.Text = "1.2.5";
            BuildDateTextBlock.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        }
        catch
        {
            // Fallback defaults
        }
        finally
        {
            _isInitializing = false;
        }
    }

    private void SelectComboBoxItemByTag(ComboBox comboBox, string tagValue)
    {
        if (comboBox == null || string.IsNullOrEmpty(tagValue)) return;
        foreach (ComboBoxItem item in comboBox.Items)
        {
            if (item.Tag?.ToString() == tagValue || item.Content?.ToString() == tagValue)
            {
                comboBox.SelectedItem = item;
                return;
            }
        }
        if (comboBox.ItemCount > 0)
            comboBox.SelectedIndex = 0;
    }

    private void Save()
    {
        if (_isInitializing) return;
        LinuxSettingsService.Instance.SaveSettings();
    }

    private void OnThemeRadioButtonChecked(object? sender, RoutedEventArgs e)
    {
        if (_isInitializing || Application.Current == null) return;

        var settings = LinuxSettingsService.Instance.Settings;

        if (LightThemeRadioButton.IsChecked == true)
        {
            Application.Current.RequestedThemeVariant = ThemeVariant.Light;
            settings.AppTheme = LinuxAppTheme.Light;
        }
        else if (DarkThemeRadioButton.IsChecked == true)
        {
            Application.Current.RequestedThemeVariant = ThemeVariant.Dark;
            settings.AppTheme = LinuxAppTheme.Dark;
        }
        else
        {
            Application.Current.RequestedThemeVariant = ThemeVariant.Default;
            settings.AppTheme = LinuxAppTheme.Default;
        }

        Save();
    }

    private void OnAccentColorSwatchClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string hex)
        {
            AccentColorHexTextBox.Text = hex;
            LinuxAccentColorService.Instance.ApplyAccentColor(hex);
        }
    }

    private void OnAccentColorHexTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_isInitializing) return;
        var hex = AccentColorHexTextBox.Text;
        if (!string.IsNullOrWhiteSpace(hex) && (hex.Length == 7 || hex.Length == 9) && hex.StartsWith('#'))
        {
            LinuxAccentColorService.Instance.ApplyAccentColor(hex);
        }
    }

    private void OnGameLibraryToggleChanged(object? sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        var settings = LinuxSettingsService.Instance.Settings;
        settings.EnableSteam = SteamToggle.IsChecked == true;
        settings.EnableHeroic = HeroicToggle.IsChecked == true;
        settings.EnableGog = GogToggle.IsChecked == true;
        settings.EnableEpic = EpicToggle.IsChecked == true;
        settings.EnableManuallyAdded = ManuallyAddedToggle.IsChecked == true;
        Save();
    }

    private async void OnAddIgnoredPathClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider != null)
        {
            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Ignored Path Folder",
                AllowMultiple = false
            });

            if (folders != null && folders.Count > 0)
            {
                var path = folders[0].Path.LocalPath;
                if (!IgnoredPaths.Contains(path))
                {
                    IgnoredPaths.Add(path);
                    LinuxSettingsService.Instance.Settings.IgnoredPaths = IgnoredPaths.ToList();
                    Save();
                }
            }
        }
    }

    private void OnRemoveIgnoredPathClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string path)
        {
            IgnoredPaths.Remove(path);
            LinuxSettingsService.Instance.Settings.IgnoredPaths = IgnoredPaths.ToList();
            Save();
        }
    }

    private void OnDlssPresetSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        if (DlssPresetComboBox.SelectedItem is DLSS_Swapper.Core.Models.DlssPresetItem item)
        {
            LinuxSettingsService.Instance.Settings.DlssPreset = item.Name;
            Save();
        }
    }

    private void OnDlssDPresetSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        if (DlssDPresetComboBox.SelectedItem is DLSS_Swapper.Core.Models.DlssPresetItem item)
        {
            LinuxSettingsService.Instance.Settings.DlssDPreset = item.Name;
            Save();
        }
    }

    private void OnDlssGPresetSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        if (DlssGPresetComboBox.SelectedItem is DLSS_Swapper.Core.Models.DlssPresetItem item)
        {
            LinuxSettingsService.Instance.Settings.DlssGPreset = item.Name;
            Save();
        }
    }

    private void OnDlssPresetInfoClick(object? sender, RoutedEventArgs e)
    {
        OpenUrl("https://github.com/beeradmoore/dlss-swapper/wiki/DLSS-Presets");
    }

    private void OnIndicatorSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        if (IndicatorComboBox.SelectedItem is ComboBoxItem item && int.TryParse(item.Tag?.ToString(), out var option))
        {
            LinuxSettingsService.Instance.Settings.IndicatorOption = option;
            Save();
        }
    }

    private void OnDlssLoggingChanged(object? sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        var settings = LinuxSettingsService.Instance.Settings;
        settings.EnableLoggingToFile = EnableLoggingToFileCheckBox.IsChecked == true;
        settings.VerboseLogging = VerboseLoggingCheckBox.IsChecked == true;
        settings.EnableLoggingToConsole = EnableLoggingToConsoleCheckBox.IsChecked == true;
        Save();
    }

    private void OnAllowUntrustedChanged(object? sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        LinuxSettingsService.Instance.Settings.AllowUntrusted = AllowUntrustedToggle.IsChecked == true;
        Save();
    }

    private void OnAllowDebugDllsChanged(object? sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        LinuxSettingsService.Instance.Settings.AllowDebugDlls = AllowDebugDllsToggle.IsChecked == true;
        Save();
    }

    private void OnOnlyShowDownloadedDllsChanged(object? sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        LinuxSettingsService.Instance.Settings.OnlyShowDownloadedDlls = OnlyShowDownloadedDllsToggle.IsChecked == true;
        Save();
    }


    private void OnLoggingLevelSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        if (LoggingLevelComboBox.SelectedItem is ComboBoxItem item && item.Tag is string levelStr)
        {
            if (Enum.TryParse<LinuxLoggingLevel>(levelStr, out var level))
            {
                LinuxSettingsService.Instance.Settings.LoggingLevel = level;
                Save();
            }
        }
    }

    private void OnOpenLogFileClick(object? sender, RoutedEventArgs e)
    {
        var logPath = CurrentLogFileTextBlock.Text;
        if (!string.IsNullOrWhiteSpace(logPath))
        {
            var dir = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
                OpenUrl(File.Exists(logPath) ? logPath : dir);
            }
        }
    }

    private void OnOpenTranslationToolboxClick(object? sender, RoutedEventArgs e)
    {
        OpenUrl("https://github.com/beeradmoore/dlss-swapper/wiki/Translating");
    }

    private void OnLanguageSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        if (LanguageComboBox.SelectedItem is string selectedName)
        {
            var languages = LinuxLanguageService.Instance.GetAvailableLanguages();
            var pair = languages.FirstOrDefault(x => x.Value == selectedName);
            if (!string.IsNullOrEmpty(pair.Key))
            {
                LinuxLanguageService.Instance.ChangeLanguage(pair.Key);
            }
        }
    }

    private async void OnCheckForUpdatesClick(object? sender, RoutedEventArgs e)
    {
        UpdateProgressBar.IsVisible = true;
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "DLSS-Swapper-Linux");
            var response = await client.GetStringAsync("https://api.github.com/repos/beeradmoore/dlss-swapper/releases/latest");
            using var doc = JsonDocument.Parse(response);
            var tagName = doc.RootElement.GetProperty("tag_name").GetString();
            UpdateProgressBar.IsVisible = false;

            await ShowDialogAsync("Check for Updates", $"Latest release on GitHub is {tagName}. You are running version 1.2.5.");
        }
        catch
        {
            UpdateProgressBar.IsVisible = false;
            await ShowDialogAsync("Check for Updates", "No new updates available or could not reach GitHub.");
        }
    }

    private void OnGitHubLinkClick(object? sender, RoutedEventArgs e) => OpenUrl("https://github.com/beeradmoore/dlss-swapper");
    private void OnTwitterLinkClick(object? sender, RoutedEventArgs e) => OpenUrl("https://twitter.com/dlss_swapper");
    private void OnRedditLinkClick(object? sender, RoutedEventArgs e) => OpenUrl("https://www.reddit.com/r/DLSS_Swapper/");
    private void OnGitHubIssuesLinkClick(object? sender, RoutedEventArgs e) => OpenUrl("https://github.com/beeradmoore/dlss-swapper/issues");
    private void OnTroubleshootingGuideClick(object? sender, RoutedEventArgs e) => OpenUrl("https://github.com/beeradmoore/dlss-swapper/wiki/Troubleshooting");

    private async void OnProxySettingsClick(object? sender, RoutedEventArgs e)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        var proxyWin = new ProxySettingsWindow();
        if (window != null)
            await proxyWin.ShowDialog(window);
        else
            proxyWin.Show();
    }

    private async void OnAcknowledgementsClick(object? sender, RoutedEventArgs e)
    {
        await ShowDialogAsync("Acknowledgements", 
            "DLSS Swapper is made possible by open-source libraries & contributions:\n\n" +
            "• NVIDIA DLSS / Streamline / XeSS / FSR SDKs\n" +
            "• Avalonia UI Framework\n" +
            "• Serilog & SQLite-net\n" +
            "• ValveKeyValue & CommunityToolkit\n" +
            "• Open-source community translators & contributors.");
    }

    private async void OnNetworkTesterClick(object? sender, RoutedEventArgs e)
    {
        UpdateProgressBar.IsVisible = true;
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var ping = await client.GetAsync("https://api.github.com");
            UpdateProgressBar.IsVisible = false;
            await ShowDialogAsync("Network Tester", $"Network connectivity test successful!\nStatus code: {(int)ping.StatusCode} ({ping.StatusCode})");
        }
        catch (Exception ex)
        {
            UpdateProgressBar.IsVisible = false;
            await ShowDialogAsync("Network Tester", $"Network connectivity test failed:\n{ex.Message}");
        }
    }

    private async void OnDiagnosticsClick(object? sender, RoutedEventArgs e)
    {
        var settings = LinuxSettingsService.Instance.Settings;
        var diag = $"DLSS Swapper Linux Diagnostics\n" +
                   $"App Version: 1.2.5\n" +
                   $"OS: {Environment.OSVersion.VersionString}\n" +
                   $"Runtime: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}\n" +
                   $"Theme: {settings.AppTheme}\n" +
                   $"Steam Enabled: {settings.EnableSteam}\n" +
                   $"Heroic Enabled: {settings.EnableHeroic}\n" +
                   $"Ignored Paths Count: {settings.IgnoredPaths?.Count ?? 0}";

        await ShowDialogAsync("Diagnostics", diag);
    }

    private async Task ShowDialogAsync(string title, string message)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        var dialog = new Window
        {
            Title = title,
            Width = 440,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Background = Brush.Parse("#242424")
        };

        var okButton = new Button
        {
            Content = "Okay",
            Padding = new Thickness(20, 8),
            HorizontalAlignment = HorizontalAlignment.Right,
            Background = LinuxAccentColorService.Instance.CurrentAccentBrush,
            Foreground = Brushes.Black,
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(4),
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        okButton.Click += (_, _) => dialog.Close();

        dialog.Content = new Border
        {
            Padding = new Thickness(24),
            Child = new StackPanel
            {
                Spacing = 16,
                Children =
                {
                    new TextBlock { Text = title, FontSize = 18, FontWeight = FontWeight.Bold, Foreground = Brushes.White },
                    new TextBlock { Text = message, FontSize = 14, Foreground = Brush.Parse("#CCCCCC"), TextWrapping = TextWrapping.Wrap },
                    okButton
                }
            }
        };

        if (window != null)
            await dialog.ShowDialog(window);
        else
            dialog.Show();
    }

    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch { }
    }
}
