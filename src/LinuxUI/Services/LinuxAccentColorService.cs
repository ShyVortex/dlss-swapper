using System;
using Avalonia;
using Avalonia.Media;
using DLSS_Swapper.Core.Services;

namespace DLSS_Swapper.LinuxUI.Services;

public class LinuxAccentColorService
{
    private static readonly Lazy<LinuxAccentColorService> _instance = new(() => new LinuxAccentColorService());
    public static LinuxAccentColorService Instance => _instance.Value;

    public string CurrentAccentColorHex { get; private set; } = "#E85A24";
    public IBrush CurrentAccentBrush { get; private set; } = new SolidColorBrush(Color.Parse("#E85A24"));

    public void ApplyAccentColor(string hexColor)
    {
        if (string.IsNullOrWhiteSpace(hexColor))
            hexColor = "#E85A24";

        if (!hexColor.StartsWith('#'))
            hexColor = "#" + hexColor;

        try
        {
            var color = Color.Parse(hexColor);
            CurrentAccentColorHex = hexColor;

            var brush = new SolidColorBrush(color);
            CurrentAccentBrush = brush;

            if (Application.Current != null)
            {
                Application.Current.Resources["SystemAccentColor"] = color;
                Application.Current.Resources["SystemControlHighlightListAccentLowBrush"] = brush;
                Application.Current.Resources["SystemControlHighlightListAccentMediumBrush"] = brush;
                Application.Current.Resources["SystemControlHighlightAccentBrush"] = brush;
                Application.Current.Resources["AccentButtonBackground"] = brush;
                Application.Current.Resources["ProgressBarForeground"] = brush;
                Application.Current.Resources["ToggleSwitchFillOn"] = brush;
                Application.Current.Resources["CheckBoxCheckBackgroundStrokeChecked"] = brush;
                Application.Current.Resources["AppAccentBrush"] = brush;
            }

            LinuxSettingsService.Instance.Settings.AccentColor = hexColor;
            LinuxSettingsService.Instance.SaveSettings();
        }
        catch
        {
            // Invalid hex color ignored
        }
    }
}
