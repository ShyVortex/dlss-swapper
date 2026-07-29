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
        CheckForUpdatesButton.Content = ResourceHelper.GetString("SettingsPage_SettingsCheckForUpdates", "Check for updates");
        AboutTitleTextBlock.Text = ResourceHelper.GetString("SettingsPage_About", "About");
        AcknowledgementsButtonTextBlock.Text = ResourceHelper.GetString("SettingsPage_OpenAcknowledgements", "Acknowledgements");
        GiveFeedbackTitleTextBlock.Text = ResourceHelper.GetString("SettingsPage_GiveFeedback", "Give Feedback");
        var feedbackMsg = ResourceHelper.GetString("SettingsPage_GiveFeedbackInfo", "You can suggest a feature or report a problem on the {0}.");
        GiveFeedbackInfoTextBlock.Text = feedbackMsg.Contains("{0}") ? feedbackMsg.Replace("{0}", "").Trim() : feedbackMsg;
        TroubleshootingTitleTextBlock.Text = ResourceHelper.GetString("SettingsPage_Troubleshooting", "Troubleshooting");
        TroubleshootingGuideButtonTextBlock.Text = ResourceHelper.GetString("SettingsPage_GeneralTroubleshootingGuide", "General troubleshooting guide");
        NetworkTesterButtonTextBlock.Text = ResourceHelper.GetString("SettingsPage_OpenNetworkTester", "Network Tester");
        DiagnosticsButtonTextBlock.Text = ResourceHelper.GetString("SettingsPage_OpenDiagnostics", "Diagnostics");
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

            // 4. Toggles
            AllowUntrustedToggle.IsChecked = settings.AllowUntrusted;
            AllowDebugDllsToggle.IsChecked = settings.AllowDebugDlls;
            OnlyShowDownloadedDllsToggle.IsChecked = settings.OnlyShowDownloadedDlls;

            // 5. Logging Level
            SelectComboBoxItemByTag(LoggingLevelComboBox, settings.LoggingLevel.ToString());

            // 6. Log File Path
            CurrentLogFileTextBlock.Text = Logger.GetCurrentLogPath();

            // 7. Language Selection
            var languages = LinuxLanguageService.Instance.GetAvailableLanguages();
            LanguageComboBox.ItemsSource = languages.Select(x => x.Value).ToList();
            var currentLangPair = languages.FirstOrDefault(x => x.Key == settings.Language);
            LanguageComboBox.SelectedItem = currentLangPair.Value ?? languages[7].Value; // Default en-US

            // 8. About metadata
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
                Logger.ChangeLoggingLevel((LoggingLevel)(int)level);
                Logger.Info($"Logging level set to {level}");
            }
        }
    }

    private void OnOpenLogFileClick(object? sender, RoutedEventArgs e)
    {
        var logPath = Logger.GetCurrentLogPath();
        var dir = Logger.LogDirectory;
        Directory.CreateDirectory(dir);

        if (!File.Exists(logPath))
        {
            Logger.Info("Log file initialized by user click.");
        }

        OpenUrl(File.Exists(logPath) ? logPath : dir);
    }

    private async void OnOpenTranslationToolboxClick(object? sender, RoutedEventArgs e)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        var toolboxWin = new DLSS_Swapper.TranslationToolboxWindow();
        if (window != null)
            await toolboxWin.ShowDialog(window);
        else
            toolboxWin.Show();
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
        var title = DLSS_Swapper.Helpers.ResourceHelper.GetString("SettingsPage_SettingsCheckForUpdates", "Check for Updates");
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "DLSS-Swapper-Linux");
            var response = await client.GetStringAsync("https://api.github.com/repos/beeradmoore/dlss-swapper/releases/latest");
            using var doc = JsonDocument.Parse(response);
            var tagName = doc.RootElement.GetProperty("tag_name").GetString();
            UpdateProgressBar.IsVisible = false;

            var noUpdatesText = DLSS_Swapper.Helpers.ResourceHelper.GetString("SettingsPage_NoNewUpdatesAvailable", "No new updates available");
            await ShowDialogAsync(title, $"{noUpdatesText} ({tagName}).");
        }
        catch
        {
            UpdateProgressBar.IsVisible = false;
            var noUpdatesText = DLSS_Swapper.Helpers.ResourceHelper.GetString("SettingsPage_NoNewUpdatesAvailable", "No new updates available");
            await ShowDialogAsync(title, noUpdatesText);
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
        var proxyWin = new ProxySettingsControl();
        if (window != null)
            await proxyWin.ShowDialog(window);
        else
            proxyWin.Show();
    }

    private async void OnAcknowledgementsClick(object? sender, RoutedEventArgs e)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        var ackWin = new DLSS_Swapper.Pages.AcknowledgementsPage();
        if (window != null)
            await ackWin.ShowDialog(window);
        else
            ackWin.Show();
    }

    private async void OnNetworkTesterClick(object? sender, RoutedEventArgs e)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        var netWin = new DLSS_Swapper.NetworkTesterWindow();
        if (window != null)
            await netWin.ShowDialog(window);
        else
            netWin.Show();
    }

    private async void OnDiagnosticsClick(object? sender, RoutedEventArgs e)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        var diagWin = new DLSS_Swapper.DiagnosticsWindow();
        if (window != null)
            await diagWin.ShowDialog(window);
        else
            diagWin.Show();
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
