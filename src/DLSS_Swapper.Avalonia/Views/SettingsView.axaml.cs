using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;

namespace DLSS_Swapper.Avalonia.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private void OnThemeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem item)
        {
            var themeName = item.Content?.ToString();
            if (Application.Current != null)
            {
                if (themeName == "Light")
                {
                    Application.Current.RequestedThemeVariant = ThemeVariant.Light;
                }
                else if (themeName == "Dark")
                {
                    Application.Current.RequestedThemeVariant = ThemeVariant.Dark;
                }
                else
                {
                    Application.Current.RequestedThemeVariant = ThemeVariant.Default;
                }
            }
        }
    }
}
