using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
#if WINDOWS
using DLSS_Swapper.Language;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.Resources.Core;
#endif

namespace DLSS_Swapper.Helpers;

public class ResourceHelper
{
    private const string error = "LangResourceError";

#if WINDOWS
    static readonly ResourceLoader _resourceLoader = new ResourceLoader();
    static readonly ResourceContext _resourceContext = ResourceContext.GetForViewIndependentUse();
    static readonly ResourceMap _resourceMap = ResourceManager.Current.MainResourceMap.GetSubtree("Resources");
#endif

    static readonly Dictionary<string, string> _resources = new Dictionary<string, string>();

    static bool _translatorModeEnabled;
#if DEBUG
    static bool _langHunterEnabled;
#endif

#if WINDOWS
    internal static bool TranslatorModeEnabled
    {
        get { return _translatorModeEnabled; }
        set
        {
            if (value != _translatorModeEnabled)
            {
                _translatorModeEnabled = value;
                LanguageManager.Instance.ReloadLanguage();
            }
        }
    }
#endif


#if WINDOWS
    internal static void SetLanguageKey(string key)
    {
#if DEBUG
        if (_langHunterEnabled)
        {
            return;
        }
#endif
        _resources.Clear();
        try
        {
            _resourceContext.Languages = new List<string> { key };
        }
        catch (Exception err)
        {
            Logger.Error($"Could not load resource map for key {key}: {err.Message}");
        }
    }

    internal static void UpdateFromLiveTranslations(List<TranslationRow> translations)
    {
        _resources.Clear();
        foreach (var translation in translations)
        {
            if (string.IsNullOrWhiteSpace(translation.NewTranslation))
            {
                continue;
            }
            // Add the translation to our dictionary.
            _resources[translation.Key] = translation.NewTranslation;
        }
        LanguageManager.Instance.ReloadLanguage();
    }
#endif

    public static string GetString(string resourceName, string? fallback = null)
    {
#if !WINDOWS
        return DLSS_Swapper.Core.Services.LinuxLanguageService.Instance.GetString(resourceName, fallback);
#else
#if DEBUG
        if (_langHunterEnabled)
        {
            return "...";
        }
#endif

        // Load from our dictionary if we are in translator mode, but then fallback if we don't have the value.
        if (TranslatorModeEnabled && _resources.TryGetValue(resourceName, out var value))
        {
            return value;
        }

        // But if we have a resource map fall back to it.
        var resourceCandidate = _resourceMap.GetValue(resourceName, _resourceContext);
        if (string.IsNullOrWhiteSpace(resourceCandidate?.ValueAsString) == false)
        {
            return resourceCandidate.ValueAsString;
        }

        Debug.WriteLine($"Translation not found: {resourceName}");

        // If not we fallback to the original language.
        var fallbackString = _resourceLoader.GetString(resourceName);
        return fallbackString;
#endif
    }

    public static string GetFormattedResourceTemplate(string templateResourceName, params object[] args)
    {
        try
        {
            return string.Format(CultureInfo.CurrentCulture, GetString(templateResourceName), args);
        }
        catch
        {
            return error;
        }
    }
}
