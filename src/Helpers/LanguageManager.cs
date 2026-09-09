using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using DLSS_Swapper.Attributes;

namespace DLSS_Swapper.Helpers;

public class LanguageManager
{
    public event Action? OnLanguageChanged;

    static LanguageManager? _instance;
    public static LanguageManager Instance => _instance ??= new LanguageManager();

    private LanguageManager()
    {
    }

    internal void ReloadLanguage()
    {
        OnLanguageChanged?.Invoke();
    }

    public void ChangeLanguage(string key)
    {
        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo(key);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(key);
        }
        catch (Exception err)
        {
            Logger.Error($"Failed to set Thread.CurrentThread to {key}: {err.Message}");
        }
        ResourceHelper.LoadResource(key);
        ReloadLanguage();
    }

    public string[] GetKnownLanguages()
    {
        // For now this is a hardcoded list. It would be nice to dynamically discover from resource class or something.
        return [
            "ar-SA",
            "ar-SY",
            "ca-ES",
            "cs-CZ",
            "de-DE",
            "en-AU",
            "en-GB",
            "en-US",
            "es-ES",
            "fa-IR",
            "fi-FI",
            "fr-FR",
            "it-IT",
            "ja-JP",
            "ko-KR",
            "pl-PL",
            "pt-BR",
            "ru-RU",
            "th-TH",
            "tr-TR",
            "uk-UA",
            "vi-VN",
            "zh-CN",
            "zh-TW",
#if DEBUG
            "LANG_HUNT",
#endif
        ];
    }

    public string GetLanguageName(string languageKey)
    {
        // For now this is a hardcoded list. It would be nice to dynamically discover from resource class or something.
        return languageKey switch
        {
            "ar-SA" => "اللغة العربية (المملكة العربية السعودية)", // Arabic (Saudi Arabia)
            "ar-SY" => " (سوريا) العربية", // Arabic (Syria)
            "ca-ES" => "Català", // Catalan
            "cs-CZ" => "Čeština", // Czech
            "de-DE" => "Deutsch", // German (Germany)
            "en-AU" => "English (Australia)",
            "en-GB" => "English (United Kingdom)",
            "en-US" => "English (United States)",
            "es-ES" => "Español", // Spanish
            "fa-IR" => "فارسی", // Farsi
            "fi-FI" => "Suomi", // Finnish (Finland)
            "fr-FR" => "Français", // French (France)
            "it-IT" => "Italiano", // Italian (Italy)
            "ja-JP" => "日本語", // Japanese
            "ko-KR" => "한국어", // Republic of Korea
            "pl-PL" => "Polski", // Polish
            "pt-BR" => "Português BR", // Portuguese
            "ru-RU" => "Русский", // Russian
            "th-TH" => "ไทย", // Thai 
            "tr-TR" => "Türkçe", // Turkish
            "uk-UA" => "Українська", // Ukrainian
            "vi-VN" => "Tiếng Việt", // Vietnamese
            "zh-CN" => "简体中文", // Simplified Chinese
            "zh-TW" => "繁體中文 (臺灣)", // Traditional Chinese (Taiwan)
            _ => languageKey,
        };
    }

    /// <summary>
    /// Gets the name of the language in English.
    /// </summary>
    /// <param name="languageKey">The language key, e.g. "en-US".</param>
    /// <returns>The name without a region qualifier, e.g. "English" instead of "English (United States)".</returns>
    public string GetEnglishLanguageName(string languageKey)
    {
        try
        {
            return StripRegionQualifier(CultureInfo.GetCultureInfo(languageKey).EnglishName);
        }
        catch (CultureNotFoundException)
        {
            // Not a valid culture, e.g. LANG_HUNT.
            return languageKey;
        }
    }

    /// <summary>
    /// Gets the name of the language as it would be displayed in the calling thread's UI culture.
    /// </summary>
    /// <param name="languageKey">The language key, e.g. "en-US".</param>
    /// <returns>
    /// The name in the calling thread's UI culture without a region qualifier,
    /// e.g. "Englisch" when the UI culture is "de-DE".
    /// </returns>
    public string GetLanguageNameInCurrentLanguage(string languageKey)
    {
        try
        {
            return StripRegionQualifier(CultureInfo.GetCultureInfo(languageKey).DisplayName);
        }
        catch (CultureNotFoundException)
        {
            // Not a valid culture, e.g. LANG_HUNT.
            return languageKey;
        }
    }

    /// <summary>
    /// Determines whether two language keys refer to the same language, ignoring the region.
    /// </summary>
    /// <param name="languageKeyA">The first language key, e.g. "en-GB".</param>
    /// <param name="languageKeyB">The second language key, e.g. "en-US".</param>
    /// <returns>True if both keys refer to the same language, e.g. "en-GB" and "en-US" are both English.</returns>
    public static bool IsSameLanguage(string languageKeyA, string languageKeyB)
    {
        return GetLanguagePart(languageKeyA).Equals(GetLanguagePart(languageKeyB), StringComparison.OrdinalIgnoreCase);
    }

    static ReadOnlySpan<char> GetLanguagePart(ReadOnlySpan<char> languageKey)
    {
        Span<Range> ranges = stackalloc Range[2];
        int partCount = languageKey.Split(ranges, "-");
        if (partCount < 1)
        {
            return languageKey;
        }

        return languageKey[ranges[0]];
    }

    static string StripRegionQualifier(string name)
    {
        // "English (United States)" -> "English"
        int index = name.IndexOf(" (", StringComparison.Ordinal);
        if (index > 0)
        {
            return name[..index];
        }

        return name;
    }

    public static IEnumerable<string> GetClassLanguagePropertyNames(Type classType)
    {
        return classType.GetProperties().Where(p => p.GetCustomAttribute<TranslationPropertyAttribute>() != null).Select(p => p.Name).ToList();
    }

    /// <summary>
    /// Determines if the specified language requires Right-to-Left (RTL) text direction.
    /// </summary>
    /// <param name="languageKey">The language key (e.g., "ar-SY", "he-IL")</param>
    /// <returns>True if the language requires RTL layout, false otherwise</returns>
    public static bool IsRightToLeftLanguage(string languageKey)
    {
        if (string.IsNullOrWhiteSpace(languageKey))
        {
            return false;
        }

        // List of RTL language codes (Semitic and other RTL languages)
        var rtlLanguages = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ar",      // Arabic
            "ar-SY",   // Arabic (Syria)
            "ar-SA",   // Arabic (Saudi Arabia)
            "ar-EG",   // Arabic (Egypt)
            "ar-AE",   // Arabic (UAE)
            "ar-JO",   // Arabic (Jordan)
            "ar-LB",   // Arabic (Lebanon)
            "ar-IQ",   // Arabic (Iraq)
            "ar-KW",   // Arabic (Kuwait)
            "ar-MA",   // Arabic (Morocco)
            "ar-TN",   // Arabic (Tunisia)
            "ar-DZ",   // Arabic (Algeria)
            "he",      // Hebrew
            "he-IL",   // Hebrew (Israel)
            "fa",      // Persian/Farsi
            "fa-IR",   // Persian (Iran)
            "ur",      // Urdu
            "ur-PK",   // Urdu (Pakistan)
            "ps",      // Pashto
            "ps-AF",   // Pashto (Afghanistan)
            "sd",      // Sindhi
            "ckb",     // Central Kurdish
            "dv",      // Divehi
            "yi"       // Yiddish
        };

        // Check exact match first
        if (rtlLanguages.Contains(languageKey))
        {
            return true;
        }

        // Check language part only (e.g., "ar" from "ar-SY")
        var languagePart = languageKey.Split('-')[0];
        return rtlLanguages.Contains(languagePart);
    }
}
