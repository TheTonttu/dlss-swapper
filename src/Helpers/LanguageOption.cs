using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DLSS_Swapper.Helpers;

public partial class LanguageOption : ObservableObject
{
    /// <summary>
    /// IETF language tag (e.g. "en-US", "fr-FR") that identifies the language.
    /// </summary>
    public string Id { get; }

    public string NativeName { get; }

    public string EnglishName { get; }

    /// <summary>
    /// Name of the language in the app's current language.
    /// </summary>
    /// <remarks>
    /// May be null when the name would be redundant (identical to <see cref="NativeName"/> or <see cref="EnglishName"/>)
    /// or when this option is for the currently selected language.
    /// </remarks>
    [ObservableProperty]
    public partial string? NameInCurrentLanguage { get; set; }

    public LanguageOption(string languageId, string currentLanguageId)
    {
        Id = languageId;
        NativeName = LanguageManager.Instance.GetLanguageName(languageId);
        EnglishName = LanguageManager.Instance.GetEnglishLanguageName(languageId);
        UpdateForCurrentLanguage(currentLanguageId);
    }

    public void UpdateForCurrentLanguage(string currentLanguageId)
    {
        // Relies on the calling (UI) thread's UI culture being the app language.
        // LanguageManager.ChangeLanguage keeps it in sync and is run on the UI thread.
        string nameInCurrentLanguage = LanguageManager.Instance.GetLanguageNameInCurrentLanguage(Id);

        // When the name in the current language would repeat the native or the English name there is no point in showing it,
        // e.g. when the language is the currently selected one.
        bool isRedundant =
            string.Equals(nameInCurrentLanguage, NativeName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(nameInCurrentLanguage, EnglishName, StringComparison.OrdinalIgnoreCase);
        bool isCurrentLanguage = LanguageManager.IsSameLanguage(Id, currentLanguageId);

        NameInCurrentLanguage = isRedundant || isCurrentLanguage ? null : nameInCurrentLanguage;
    }
}
