using System.Collections.Generic;

namespace BabyStepsMultiplayerClient.Localization
{
    public static class LanguageManager
    {
        private static Dictionary<string, ILanguage> _registeredLanguages;
        private static ILanguage _currentLanguage;

        public static string CurrentLanguage { get; private set; } = "English";

        static LanguageManager()
        {
            InitializeLanguages();
        }

        private static void InitializeLanguages()
        {
            _registeredLanguages = new Dictionary<string, ILanguage>
            {
                { "English", new EnglishLanguage() },
                { "Spanish", new SpanishLanguage() },
                { "French", new FrenchLanguage() },
                { "German", new GermanLanguage() }
            };

            LoadLanguageFromConfig();
        }

        private static void LoadLanguageFromConfig()
        {
            try
            {
                string savedLanguage = ModSettings.player.Language.Value;
                if (!string.IsNullOrEmpty(savedLanguage) && _registeredLanguages.ContainsKey(savedLanguage))
                {
                    SetLanguage(savedLanguage);
                }
                else
                {
                    SetLanguage("English");
                }
            }
            catch
            {
                SetLanguage("English");
            }
        }

        public static void SetLanguage(string languageName)
        {
            if (_registeredLanguages.ContainsKey(languageName))
            {
                _currentLanguage = _registeredLanguages[languageName];
                CurrentLanguage = languageName;
                SaveLanguageToConfig(languageName);
            }
            else
            {
                Core.logger?.Warning($"Language '{languageName}' not found. Using English.");
                _currentLanguage = _registeredLanguages["English"];
                CurrentLanguage = "English";
                SaveLanguageToConfig("English");
            }
        }

        private static void SaveLanguageToConfig(string languageName)
        {
            try
            {
                ModSettings.player.Language.Value = languageName;
                ModSettings.Save();
            }
            catch
            {
            }
        }

        public static ILanguage GetCurrentLanguage()
        {
            return _currentLanguage ?? new EnglishLanguage();
        }

        public static string[] GetAvailableLanguages()
        {
            return new List<string>(_registeredLanguages.Keys).ToArray();
        }

        public static void RegisterLanguage(string name, ILanguage language)
        {
            if (!_registeredLanguages.ContainsKey(name))
            {
                _registeredLanguages[name] = language;
            }
        }
    }
}
