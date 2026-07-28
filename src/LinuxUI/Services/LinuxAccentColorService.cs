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

            var hoverLighterColor = Color.FromArgb(
                255,
                (byte)Math.Min(255, color.R + 30),
                (byte)Math.Min(255, color.G + 30),
                (byte)Math.Min(255, color.B + 30)
            );
            var hoverLighterBrush = new SolidColorBrush(hoverLighterColor);

            var hoverTransparentColor = Color.FromArgb((byte)(color.A * 0.25), color.R, color.G, color.B);
            var hoverTransparentBrush = new SolidColorBrush(hoverTransparentColor);

            if (Application.Current != null)
            {
                Application.Current.Resources["SystemAccentColor"] = color;
                Application.Current.Resources["SystemAccentColorLight1"] = hoverLighterColor;
                Application.Current.Resources["SystemAccentColorLight2"] = hoverLighterColor;
                Application.Current.Resources["SystemAccentColorDark1"] = color;

                Application.Current.Resources["SystemControlHighlightListAccentLowBrush"] = hoverTransparentBrush;
                Application.Current.Resources["SystemControlHighlightListAccentMediumBrush"] = hoverTransparentBrush;
                Application.Current.Resources["SystemControlHighlightAccentBrush"] = brush;
                Application.Current.Resources["SystemControlHighlightAccent3Brush"] = hoverLighterBrush;

                Application.Current.Resources["AccentButtonBackground"] = brush;
                Application.Current.Resources["AccentButtonBackgroundPointerOver"] = hoverLighterBrush;
                Application.Current.Resources["ProgressBarForeground"] = brush;

                Application.Current.Resources["ToggleSwitchFillOn"] = brush;
                Application.Current.Resources["ToggleSwitchFillOnPointerOver"] = hoverLighterBrush;
                Application.Current.Resources["ToggleSwitchKnobFillOn"] = Colors.White;
                Application.Current.Resources["ToggleSwitchKnobFillOnPointerOver"] = Colors.White;

                Application.Current.Resources["CheckBoxCheckBackgroundStrokeChecked"] = brush;
                Application.Current.Resources["CheckBoxCheckBackgroundFillChecked"] = brush;
                Application.Current.Resources["CheckBoxCheckBackgroundFillCheckedPointerOver"] = hoverLighterBrush;
                Application.Current.Resources["CheckBoxCheckBackgroundStrokeCheckedPointerOver"] = hoverLighterBrush;

                Application.Current.Resources["ComboBoxItemBackgroundSelected"] = hoverTransparentBrush;
                Application.Current.Resources["ComboBoxItemBackgroundSelectedPointerOver"] = hoverTransparentBrush;

                Application.Current.Resources["AppAccentBrush"] = brush;
                Application.Current.Resources["AppAccentHoverBrush"] = hoverLighterBrush;
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
