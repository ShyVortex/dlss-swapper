using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using DLSS_Swapper.Core.Services;
using DLSS_Swapper.Helpers;

namespace DLSS_Swapper;

public partial class TranslationRow : ObservableObject
{
    public string Key { get; set; } = string.Empty;

    private string _comment = string.Empty;
    public string Comment
    {
        get => _comment;
        set => SetProperty(ref _comment, value);
    }

    private string _sourceTranslation = string.Empty;
    public string SourceTranslation
    {
        get => _sourceTranslation;
        set => SetProperty(ref _sourceTranslation, value);
    }

    private string _newTranslation = string.Empty;
    public string NewTranslation
    {
        get => _newTranslation;
        set
        {
            if (SetProperty(ref _newTranslation, value))
            {
                OnTranslationChanged?.Invoke();
            }
        }
    }

    public Action? OnTranslationChanged { get; set; }
}

public partial class TranslationToolboxWindow : Window
{
    private readonly ObservableCollection<TranslationRow> _rows = new();
    private readonly Dictionary<string, string> _enUsComments = new(StringComparer.OrdinalIgnoreCase);
    private List<KeyValuePair<string, string>> _sourceLanguages = new();

    public TranslationToolboxWindow()
    {
        InitializeComponent();
        TranslationDataGrid.ItemsSource = _rows;
        LoadDefaultSourceTranslations();
        LoadSourceLanguages();
        UpdateTranslations();
        LinuxLanguageService.Instance.OnLanguageChanged += UpdateTranslations;
    }

    private static string? FindReswFile(string langKey)
    {
        var basePath = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(basePath, "Translations", langKey, "Resources.resw"),
            Path.Combine(basePath, "..", "..", "..", "Translations", langKey, "Resources.resw"),
            Path.Combine(basePath, "..", "..", "..", "..", "Translations", langKey, "Resources.resw"),
            Path.Combine(basePath, "..", "..", "..", "..", "..", "Translations", langKey, "Resources.resw"),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "Translations", langKey, "Resources.resw"),
            Path.Combine(Directory.GetCurrentDirectory(), "Translations", langKey, "Resources.resw")
        };

        foreach (var candidate in candidates)
        {
            var fullPath = Path.GetFullPath(candidate);
            if (File.Exists(fullPath)) return fullPath;
        }
        return null;
    }

    private void UpdateTranslations()
    {
        var appTitle = ResourceHelper.GetString("ApplicationTitle", "DLSS Swapper");
        var winTitle = ResourceHelper.GetString("TranslationToolboxPage_WindowTitle", "Translation Toolbox");
        Title = $"{appTitle} - {winTitle}";

        SourceLanguageLabel.Text = ResourceHelper.GetString("TranslationToolboxPage_SourceLanguage", "Source language:");
        HelpButton.Content = ResourceHelper.GetString("TranslationToolboxPage_TranslationGuideButton", "Help");
        LoadExistingButton.Content = ResourceHelper.GetString("TranslationToolboxPage_LoadExistingTranslation", "Load Existing");
        LoadButton.Content = ResourceHelper.GetString("General_Load", "Load");
        SaveButton.Content = ResourceHelper.GetString("General_Save", "Save");
        PublishButton.Content = ResourceHelper.GetString("TranslationToolboxPage_Publish", "Publish");
        ReloadAppButton.Content = ResourceHelper.GetString("TranslationToolboxPage_ReloadApp", "Reload app");

        if (TranslationDataGrid.Columns.Count >= 4)
        {
            TranslationDataGrid.Columns[0].Header = ResourceHelper.GetString("TranslationToolboxPage_Key", "Key");
            TranslationDataGrid.Columns[1].Header = ResourceHelper.GetString("TranslationToolboxPage_Comment", "Comment");
            TranslationDataGrid.Columns[2].Header = ResourceHelper.GetString("TranslationToolboxPage_SourceTranslation", "Source translation");
            TranslationDataGrid.Columns[3].Header = ResourceHelper.GetString("TranslationToolboxPage_NewTranslation", "New translation");
        }
    }

    private void LoadSourceLanguages()
    {
        _sourceLanguages = LinuxLanguageService.Instance.GetAvailableLanguages();
        SourceLanguageComboBox.ItemsSource = _sourceLanguages.Select(x => x.Value).ToList();
        SourceLanguageComboBox.SelectedIndex = _sourceLanguages.FindIndex(x => x.Key == "en-US");
        if (SourceLanguageComboBox.SelectedIndex < 0 && _sourceLanguages.Count > 0)
        {
            SourceLanguageComboBox.SelectedIndex = 0;
        }
    }

    private void LoadDefaultSourceTranslations()
    {
        _rows.Clear();
        _enUsComments.Clear();
        var enUsPath = FindReswFile("en-US");

        if (enUsPath != null && File.Exists(enUsPath))
        {
            try
            {
                var doc = XDocument.Load(enUsPath);
                foreach (var data in doc.Descendants("data"))
                {
                    var key = data.Attribute("name")?.Value ?? string.Empty;
                    var val = data.Element("value")?.Value ?? string.Empty;
                    var comment = data.Element("comment")?.Value ?? string.Empty;

                    if (!string.IsNullOrEmpty(key))
                    {
                        _enUsComments[key] = comment;
                        var row = new TranslationRow
                        {
                            Key = key,
                            Comment = comment,
                            SourceTranslation = val,
                            NewTranslation = string.Empty,
                            OnTranslationChanged = RecalculateProgress
                        };
                        _rows.Add(row);
                    }
                }
            }
            catch { }
        }

        RecalculateProgress();
    }

    private void RecalculateProgress()
    {
        int total = _rows.Count;
        int translated = _rows.Count(r => !string.IsNullOrWhiteSpace(r.NewTranslation));
        int pct = total > 0 ? (translated * 100 / total) : 0;

        TranslationProgressValue.Text = $"{translated} / {total} ({pct}%)";
    }

    private void OnSourceLanguageChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_sourceLanguages == null || SourceLanguageComboBox == null) return;
        int idx = SourceLanguageComboBox.SelectedIndex;
        if (idx < 0 || idx >= _sourceLanguages.Count) return;

        var selectedLang = _sourceLanguages[idx].Key;
        var reswPath = FindReswFile(selectedLang);
        var langDict = new Dictionary<string, (string Value, string Comment)>(StringComparer.OrdinalIgnoreCase);

        if (reswPath != null && File.Exists(reswPath))
        {
            try
            {
                var doc = XDocument.Load(reswPath);
                foreach (var dataElem in doc.Descendants("data"))
                {
                    var name = dataElem.Attribute("name")?.Value;
                    if (string.IsNullOrEmpty(name)) continue;

                    var val = dataElem.Element("value")?.Value ?? string.Empty;
                    var comment = dataElem.Element("comment")?.Value ?? string.Empty;

                    langDict[name] = (val, comment);
                }
            }
            catch { }
        }

        foreach (var row in _rows)
        {
            if (langDict.TryGetValue(row.Key, out var entry))
            {
                row.SourceTranslation = entry.Value;
                row.Comment = !string.IsNullOrWhiteSpace(entry.Comment)
                    ? entry.Comment
                    : (_enUsComments.TryGetValue(row.Key, out var enComment) ? enComment : string.Empty);
            }
            else
            {
                row.SourceTranslation = string.Empty;
                row.Comment = _enUsComments.TryGetValue(row.Key, out var enComment) ? enComment : string.Empty;
            }
        }
    }

    private void OnHelpClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = "https://github.com/beeradmoore/dlss-swapper/wiki/Translation-Guide", UseShellExecute = true });
        }
        catch { }
    }

    private async void OnLoadExistingClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel != null)
        {
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Load Existing Translation XML (.resw)",
                AllowMultiple = false
            });
            if (files.Count > 0)
            {
                var filePath = files[0].Path.LocalPath;
                LoadReswIntoNewTranslation(filePath);
            }
        }
    }

    private async void OnLoadClick(object? sender, RoutedEventArgs e)
    {
        OnLoadExistingClick(sender, e);
    }

    private void LoadReswIntoNewTranslation(string filePath)
    {
        if (File.Exists(filePath))
        {
            try
            {
                var doc = XDocument.Load(filePath);
                var dict = doc.Descendants("data")
                    .ToDictionary(
                        d => d.Attribute("name")?.Value ?? string.Empty,
                        d => d.Element("value")?.Value ?? string.Empty,
                        StringComparer.OrdinalIgnoreCase
                    );

                foreach (var row in _rows)
                {
                    if (dict.TryGetValue(row.Key, out var newVal))
                    {
                        row.NewTranslation = newVal;
                    }
                }
                RecalculateProgress();
            }
            catch { }
        }
    }

    private async void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel != null)
        {
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Translation (.resw)",
                DefaultExtension = "resw"
            });
            if (file != null)
            {
                SaveReswToFile(file.Path.LocalPath);
            }
        }
    }

    private void SaveReswToFile(string filePath)
    {
        try
        {
            var root = new XElement("root",
                new XElement("resheader", new XAttribute("name", "resmimetype"), new XElement("value", "text/microsoft-resx")),
                new XElement("resheader", new XAttribute("name", "version"), new XElement("value", "2.0")),
                new XElement("resheader", new XAttribute("name", "reader"), new XElement("value", "System.Resources.ResXResourceReader, System.Windows.Forms")),
                new XElement("resheader", new XAttribute("name", "writer"), new XElement("value", "System.Resources.ResXResourceWriter, System.Windows.Forms"))
            );

            foreach (var row in _rows)
            {
                var val = !string.IsNullOrWhiteSpace(row.NewTranslation) ? row.NewTranslation : row.SourceTranslation;
                var dataElem = new XElement("data",
                    new XAttribute("name", row.Key),
                    new XAttribute(XNamespace.Xml + "space", "preserve"),
                    new XElement("value", val)
                );
                if (!string.IsNullOrWhiteSpace(row.Comment))
                {
                    dataElem.Add(new XElement("comment", row.Comment));
                }
                root.Add(dataElem);
            }

            var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root);
            doc.Save(filePath);
        }
        catch { }
    }

    private void OnPublishClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = "https://github.com/beeradmoore/dlss-swapper/pulls", UseShellExecute = true });
        }
        catch { }
    }

    private void OnReloadAppClick(object? sender, RoutedEventArgs e)
    {
        LinuxLanguageService.Instance.ChangeLanguage(LinuxLanguageService.Instance.CurrentLanguageKey);
    }
}
