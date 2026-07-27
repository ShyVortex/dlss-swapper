using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace DLSS_Swapper.Core.Services;

public class LinuxLanguageService
{
    private static readonly Lazy<LinuxLanguageService> _instance = new(() => new LinuxLanguageService());
    public static LinuxLanguageService Instance => _instance.Value;

    public event Action? OnLanguageChanged;

    public string CurrentLanguageKey { get; private set; } = "en-US";

    private readonly Dictionary<string, string> _currentStrings = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _fallbackStrings = new(StringComparer.OrdinalIgnoreCase);

    private readonly List<KeyValuePair<string, string>> _languages = new()
    {
        new("ar-SA", "اللغة العربية (المملكة العربية السعودية)"),
        new("ar-SY", "العربية (سوريا)"),
        new("ca-ES", "Català"),
        new("cs-CZ", "Čeština"),
        new("de-DE", "Deutsch"),
        new("en-AU", "English (Australia)"),
        new("en-GB", "English (United Kingdom)"),
        new("en-US", "English (United States)"),
        new("es-ES", "Español"),
        new("fa-IR", "فارسی"),
        new("fi-FI", "Suomi"),
        new("fr-FR", "Français"),
        new("it-IT", "Italiano"),
        new("ja-JP", "日本語"),
        new("ko-KR", "한국어"),
        new("pl-PL", "Polski"),
        new("pt-BR", "Português (Brasil)"),
        new("ru-RU", "Русский"),
        new("th-TH", "ไทย"),
        new("tr-TR", "Türkçe"),
        new("uk-UA", "Українська"),
        new("vi-VN", "Tiếng Việt"),
        new("zh-CN", "简体中文"),
        new("zh-TW", "繁體中文")
    };

    public LinuxLanguageService()
    {
        LoadLanguage("en-US", _fallbackStrings);
        var savedLang = LinuxSettingsService.Instance.Settings.Language;
        if (!string.IsNullOrWhiteSpace(savedLang))
        {
            ChangeLanguage(savedLang);
        }
        else
        {
            ChangeLanguage("en-US");
        }
    }

    public List<KeyValuePair<string, string>> GetAvailableLanguages() => _languages;

    public void ChangeLanguage(string langKey)
    {
        if (string.IsNullOrWhiteSpace(langKey))
            langKey = "en-US";

        CurrentLanguageKey = langKey;
        _currentStrings.Clear();
        LoadLanguage(langKey, _currentStrings);

        LinuxSettingsService.Instance.Settings.Language = langKey;
        LinuxSettingsService.Instance.SaveSettings();

        OnLanguageChanged?.Invoke();
    }

    private void LoadLanguage(string langKey, Dictionary<string, string> targetDict)
    {
        try
        {
            var basePath = AppContext.BaseDirectory;
            var reswPath = Path.Combine(basePath, "Translations", langKey, "Resources.resw");

            if (!File.Exists(reswPath))
            {
                // Fallback search relative to source directory
                reswPath = Path.Combine(basePath, "..", "..", "..", "..", "Translations", langKey, "Resources.resw");
            }

            if (File.Exists(reswPath))
            {
                var doc = XDocument.Load(reswPath);
                foreach (var dataElem in doc.Descendants("data"))
                {
                    var nameAttr = dataElem.Attribute("name")?.Value;
                    var valElem = dataElem.Element("value")?.Value;

                    if (!string.IsNullOrEmpty(nameAttr) && valElem != null)
                    {
                        targetDict[nameAttr] = valElem;
                    }
                }
            }
        }
        catch
        {
            // Error loading resw XML
        }
    }

    public string GetString(string resourceKey, string? fallback = null)
    {
        if (string.IsNullOrEmpty(resourceKey))
            return fallback ?? string.Empty;

        if (_currentStrings.TryGetValue(resourceKey, out var value) && !string.IsNullOrWhiteSpace(value))
            return value;

        if (_fallbackStrings.TryGetValue(resourceKey, out var fallbackValue) && !string.IsNullOrWhiteSpace(fallbackValue))
            return fallbackValue;

        return fallback ?? resourceKey;
    }
}
