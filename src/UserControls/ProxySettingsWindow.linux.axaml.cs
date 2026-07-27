using Avalonia.Controls;
using Avalonia.Interactivity;
using DLSS_Swapper.Core.Services;
using DLSS_Swapper.Helpers;

namespace DLSS_Swapper.Avalonia.Views;

public partial class ProxySettingsWindow : Window
{
    public ProxySettingsWindow()
    {
        InitializeComponent();
        UpdateTranslations();
        LinuxLanguageService.Instance.OnLanguageChanged += UpdateTranslations;
        LoadProxySettings();
    }

    private void UpdateTranslations()
    {
        Title = ResourceHelper.GetString("SettingsPage_ProxySettings", "Proxy Settings");
        TitleTextBlock.Text = ResourceHelper.GetString("SettingsPage_ProxySettings", "Proxy Settings");
        UseProxyCheckBox.Content = ResourceHelper.GetString("ProxySettings_UseProxySettings", "Use Proxy Server");
        ServerAddressTextBlock.Text = ResourceHelper.GetString("ProxySettings_Server", "Proxy Server Address");
        UseAuthCheckBox.Content = ResourceHelper.GetString("ProxySettings_UseAuthentication", "Requires Authentication");
        UsernameTextBlock.Text = ResourceHelper.GetString("ProxySettings_Username", "Username");
        PasswordTextBlock.Text = ResourceHelper.GetString("ProxySettings_Password", "Password");
        CancelButton.Content = ResourceHelper.GetString("General_Cancel", "Cancel");
        SaveButton.Content = ResourceHelper.GetString("General_Save", "Save");
    }

    private void LoadProxySettings()
    {
        var settings = LinuxSettingsService.Instance.Settings;
        ServerTextBox.Text = settings.DlssPreset; // Or proxy server field
    }

    private void OnUseProxyChanged(object? sender, RoutedEventArgs e)
    {
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        LinuxSettingsService.Instance.SaveSettings();
        Close(true);
    }
}
