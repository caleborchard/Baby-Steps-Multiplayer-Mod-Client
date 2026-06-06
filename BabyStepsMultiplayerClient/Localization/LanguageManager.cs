using System;
using System.Collections.Generic;
using Il2Cpp;

namespace BabyStepsMultiplayerClient.Localization
{
    public enum GameLanguage
    {
        English,
        Spanish,
        French,
        German,
        Japanese,
        Korean,
        ChineseSimplified,
        ChineseTraditional,
    }

    public static class LanguageManager
    {
        // Maps the display/I2Loc name to the Il2Cpp.Language enum value used by the game
        private static readonly Dictionary<string, Language> _nameToEnum = new()
        {
            { "English",             Language.English },
            { "Spanish",             Language.Spanish },
            { "French",              Language.French },
            { "German",              Language.German },
            { "Japanese",            Language.Japanese },
            { "Korean",              Language.Korean },
            { "Chinese Simplified",  Language.ChineseSimple },
            { "Chinese Traditional", Language.ChineseTraditional },
            { "Russian",             Language.Russian },
            { "Portuguese",          Language.Portuguese },
        };

        private static readonly Dictionary<Language, string> _enumToName = new()
        {
            { Language.English,            "English" },
            { Language.Spanish,            "Spanish" },
            { Language.French,             "French" },
            { Language.German,             "German" },
            { Language.Japanese,           "Japanese" },
            { Language.Korean,             "Korean" },
            { Language.ChineseSimple,      "Chinese Simplified" },
            { Language.ChineseTraditional, "Chinese Traditional" },
            { Language.Russian,            "Russian" },
            { Language.Portuguese,         "Portuguese" },
        };

        private static readonly Dictionary<string, ILanguage> _languages = new()
        {
            { "English",             new EnglishLanguage() },
            { "Spanish",             new SpanishLanguage() },
            { "French",              new FrenchLanguage() },
            { "German",              new GermanLanguage() },
            { "Japanese",            new JapaneseLanguage() },
            { "Korean",              new KoreanLanguage() },
            { "Chinese Simplified",  new ChineseSimplifiedLanguage() },
            { "Chinese Traditional", new ChineseTraditionalLanguage() },
            { "Russian",             new RussianLanguage() },
            { "Portuguese",          new PortugueseLanguage() },
        };

        // Reads the game's current language from Menu.cfg.language.
        // Falls back to "English" before the game menu has loaded.
        public static string CurrentLanguage
        {
            get
            {
                try
                {
                    var lang = Menu.cfg?.language;
                    if (lang.HasValue && _enumToName.TryGetValue(lang.Value, out var name))
                        return name;
                }
                catch { }
                return "English";
            }
        }

        public static ILanguage GetCurrentLanguage()
        {
            string name = CurrentLanguage;
            return _languages.TryGetValue(name, out var lang) ? lang : _languages["English"];
        }

        // Delegates to Menu.me.SetLang() which handles I2Loc + game config internally.
        public static void SetLanguage(string languageName)
        {
            try
            {
                if (!_nameToEnum.TryGetValue(languageName, out var enumVal)) return;
                Menu.me?.SetLang(enumVal);
            }
            catch (Exception e)
            {
                Core.logger?.Warning($"[LanguageManager] SetLanguage failed: {e}");
            }
        }

        public static string[] GetAvailableLanguages()
            => new[]
            {
                "English", "Spanish", "French", "German",
                "Japanese", "Korean", "Chinese Simplified", "Chinese Traditional",
                "Russian", "Portuguese"
            };
    }
}
