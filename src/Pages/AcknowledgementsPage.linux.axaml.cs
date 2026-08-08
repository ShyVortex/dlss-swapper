using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using DLSS_Swapper.Core.Services;
using DLSS_Swapper.Helpers;

namespace DLSS_Swapper.Pages;

public class AcknowledgementItem
{
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? License { get; set; }
}

public partial class AcknowledgementsPage : Window
{
    private readonly List<AcknowledgementItem> _items = new();

    public AcknowledgementsPage()
    {
        InitializeComponent();
        LoadItems();
        UpdateTranslations();
        LinuxLanguageService.Instance.OnLanguageChanged += UpdateTranslations;
    }

    private void UpdateTranslations()
    {
        Title = ResourceHelper.GetString("AcknowledgementsPage_Title", "Licences & Acknowledgements");
        TitleTextBlock.Text = ResourceHelper.GetString("AcknowledgementsPage_Title", "Licences & Acknowledgements");
    }

    private void LoadItems()
    {
        _items.Clear();
        var assemblies = new[] { typeof(LinuxSettingsService).Assembly, Assembly.GetExecutingAssembly() };
        var dict = new Dictionary<string, (string? Notes, string? License)>(StringComparer.OrdinalIgnoreCase);

        var regex = new Regex(@"^(?:DLSS_Swapper\.|DLSS_Swapper\.Core\.)?Acknowledgements\.(?<name>[^.]+)\.(?<file>license\.txt|notes\.md)$", RegexOptions.IgnoreCase);

        foreach (var asm in assemblies)
        {
            var resourceNames = asm.GetManifestResourceNames();
            foreach (var resName in resourceNames)
            {
                var match = regex.Match(resName);
                if (!match.Success) continue;

                var name = match.Groups["name"].Value.Replace('_', '-');
                var file = match.Groups["file"].Value.ToLowerInvariant();

                if (!dict.TryGetValue(name, out var entry))
                {
                    entry = (null, null);
                }

                try
                {
                    using var stream = asm.GetManifestResourceStream(resName);
                    if (stream != null)
                    {
                        using var reader = new StreamReader(stream);
                        var text = reader.ReadToEnd();
                        if (file == "notes.md") entry.Notes = text;
                        else if (file == "license.txt") entry.License = text;
                    }
                }
                catch { }

                dict[name] = entry;
            }
        }

        // Standard priority order matching Windows WinUI 3
        var nameOrder = new[] { "You", "DLSS", "Streamline", "FidelityFX-SDK", "XeSS", "Proton Autogen" };

        var sortedKeys = dict.Keys.OrderBy(k => k).ToList();

        foreach (var priority in nameOrder)
        {
            var key = sortedKeys.FirstOrDefault(k => string.Equals(k, priority, StringComparison.OrdinalIgnoreCase) || string.Equals(k, priority.Replace(" ", ""), StringComparison.OrdinalIgnoreCase));
            if (key != null)
            {
                var data = dict[key];
                _items.Add(new AcknowledgementItem { Name = priority, Notes = data.Notes, License = data.License });
                sortedKeys.Remove(key);
            }
        }

        // Add Linux-specific Proton Autogen if not already present from embedded resources
        if (!_items.Any(x => x.Name.Equals("Proton Autogen", StringComparison.OrdinalIgnoreCase)))
        {
            _items.Add(new AcknowledgementItem
            {
                Name = "Proton Autogen",
                Notes = "Proton Autogen is an open-source tool for automatic Proton / Wine DXVK & NVAPI environment configuration on Linux.\nhttps://github.com/N3oRay/proton-autogen",
                License = "MIT License\n\nCopyright (c) 2024 N3oRay\n\nPermission is hereby granted, free of charge, to any person obtaining a copy of this software..."
            });
        }

        foreach (var key in sortedKeys)
        {
            var data = dict[key];
            _items.Add(new AcknowledgementItem { Name = key, Notes = data.Notes, License = data.License });
        }

        ItemsListBox.ItemsSource = _items;
        if (_items.Count > 0)
        {
            ItemsListBox.SelectedIndex = 0;
        }
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ItemsListBox.SelectedItem is AcknowledgementItem item)
        {
            NotesTextBlock.IsVisible = !string.IsNullOrWhiteSpace(item.Notes);
            SetMarkdownText(NotesTextBlock, item.Notes ?? string.Empty);

            NotesSeparator.IsVisible = !string.IsNullOrWhiteSpace(item.Notes) && !string.IsNullOrWhiteSpace(item.License);

            LicenseTextBox.IsVisible = !string.IsNullOrWhiteSpace(item.License);
            LicenseTextBox.Text = item.License ?? string.Empty;
        }
        else
        {
            NotesTextBlock.IsVisible = false;
            NotesSeparator.IsVisible = false;
            LicenseTextBox.IsVisible = false;
        }
    }

    private static void SetMarkdownText(TextBlock textBlock, string text)
    {
        textBlock.Inlines?.Clear();
        if (string.IsNullOrWhiteSpace(text)) return;

        var regex = new Regex(@"\[(?<text>[^\]]+)\]\((?<url>https?://[^\)]+)\)|(?<rawUrl>https?://[^\s\)]+)", RegexOptions.IgnoreCase);
        int lastIndex = 0;

        foreach (Match match in regex.Matches(text))
        {
            if (match.Index > lastIndex)
            {
                textBlock.Inlines?.Add(new Run(text.Substring(lastIndex, match.Index - lastIndex)));
            }

            string linkText;
            string url;

            if (match.Groups["rawUrl"].Success)
            {
                url = match.Groups["rawUrl"].Value;
                linkText = url;
            }
            else
            {
                linkText = match.Groups["text"].Value;
                url = match.Groups["url"].Value;
            }

            var linkButton = new Button
            {
                Content = linkText,
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                MinHeight = 0,
                MinWidth = 0,
                Background = Brushes.Transparent,
                Foreground = Brush.Parse("#58A6FF"),
                BorderThickness = new Thickness(0),
                Cursor = new Cursor(StandardCursorType.Hand),
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center
            };

            linkButton.Click += (s, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                }
                catch { }
            };

            textBlock.Inlines?.Add(new InlineUIContainer(linkButton)
            {
                BaselineAlignment = BaselineAlignment.Baseline
            });

            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < text.Length)
        {
            textBlock.Inlines?.Add(new Run(text.Substring(lastIndex)));
        }
    }
}
