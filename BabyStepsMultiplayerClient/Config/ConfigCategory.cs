using MelonLoader;
using MelonLoader.Utils;

namespace BabyStepsMultiplayerClient.Config
{
    public class ConfigCategory
    {
        public static SortedDictionary<string, ConfigCategory> _allCategories = new();

        public MelonPreferences_Category Category;
        public string FileExt = ".cfg";

        public virtual string ID { get; set; }
        public virtual string DisplayName { get; set; }

        public ConfigCategory()
        {
            Setup(MelonEnvironment.UserDataDirectory,
                "BabyStepsClientConfig",
                ID,
                DisplayName);

            if (!_allCategories.ContainsKey(ID))
                _allCategories[ID] = this;
        }

        public ConfigCategory(string folderPath,
            string fileName,
            string categoryID,
            string categoryDisplayName)
            => Setup(folderPath, fileName, categoryID, categoryDisplayName);

        public void Setup(string folderPath,
            string fileName,
            string categoryID,
            string categoryDisplayName)
        {
            if (string.IsNullOrEmpty(categoryDisplayName)
                || string.IsNullOrWhiteSpace(categoryDisplayName))
                categoryDisplayName = categoryID;

            string filePath = Path.Combine(folderPath, $"{fileName}{FileExt}");



            ID = categoryID;
            DisplayName = categoryDisplayName;

            Category = MelonPreferences.GetCategory(ID);
            if (Category == null)
            {
                Category = MelonPreferences.CreateCategory(ID, DisplayName, true, false);
                Category.DestroyFileWatcher();
                Category.SetFilePath(filePath, true, false);
            }

            CreatePreferences();
            Category.SaveToFile(false);
        }

        public virtual void CreatePreferences() { }

        public void Load() => Category.LoadFromFile(false);
        public void Save() => Category.SaveToFile(false);

        public MelonPreferences_Entry<T> CreatePref<T>(
            string id,
            string displayName,
            string description,
            T defaultValue = default,
            bool isHidden = false)
        {
            var existingEntry = Category.GetEntry<T>(id);
            if (existingEntry != null)
                return existingEntry;

            return Category.CreateEntry(id,
                defaultValue,
                displayName,
                description,
                isHidden,
                false,
                null);
        }
    }
}
