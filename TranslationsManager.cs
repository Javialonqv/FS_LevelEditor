using FS_LevelEditor.Editor.UI;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FS_LevelEditor
{
    public static class TranslationsManager
    {
        public static bool initialized { get; private set; } = false;

        static Dictionary<string, List<string>> translations;
        static List<string> languages;
        public static string currentLanguage
        {
            get
            {
                return Localization.language;
            }
        }

        public static void Init()
        {
            ReadTranslationsFile();

            initialized = true;
        }

        static void ReadTranslationsFile()
        {
            string[] test = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FS_LevelEditor.Translations.Translations.csv");
            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes);

            StreamReader sr = new StreamReader(stream);
            string fileContent = Encoding.UTF8.GetString(bytes);
            ReadTranslations(fileContent);
        }
        static void ReadTranslations(string fileContent)
        {
            translations = new Dictionary<string, List<string>>();
            languages = new List<string>();

            string[] lines = fileContent.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                string[] columns = SplitWithCommas(lines[i].Trim());

                if (i == 0)
                {
                    for (int j = 1; j < columns.Length; j++)
                    {
                        if (languages.Contains(columns[j].ToUpper()))
                        {
                            Logger.Error($"Duplicate language found in translations file: \"{columns[j]}\" at line {j}. Skipping it...");
                            continue;
                        }
                        languages.Add(columns[j].ToUpper());
                    }
                    continue;
                }

                if (columns.Length == 0) continue;
                if (string.IsNullOrEmpty(columns[0])) continue;

                string currentKey = columns[0];
                if (translations.ContainsKey(currentKey))
                {
                    Logger.Error($"Duplicate key found in translations file: \"{currentKey}\" at line {i + 1}. Skipping it...");
                    continue;
                }
                List<string> currentKeyTranslations = new List<string>();
                for (int j = 1; j < columns.Length; j++)
                {
                    currentKeyTranslations.Add(columns[j].Trim());
                }

                translations.Add(currentKey, currentKeyTranslations);
            }
        }

        static string[] SplitWithCommas(string line)
        {
            var fields = new List<string>();
            var currentField = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"') // Handle escaped quotes
                    {
                        currentField.Append('"');
                        i++; // Skip the next quote
                    }
                    else
                    {
                        inQuotes = !inQuotes; // Toggle inQuotes state
                    }
                }
                else if (line[i] == ',' && !inQuotes)
                {
                    fields.Add(currentField.ToString().Trim());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(line[i]);
                }
            }
            // Add the last field if it exists
            if (currentField.Length > 0) fields.Add(currentField.ToString().Trim());

            return fields.ToArray();
        }

        public static string GetTranslation(string key, bool throwErrorIfNotFound)
        {
            if (!initialized)
            {
                Init();
                return null;
            }
            if (!translations.ContainsKey(key))
            {
                if (throwErrorIfNotFound) Logger.Error($"\"{key}\" doesn't exists in the LE Translations!");
                return key;
            }

#if DEBUG
            int langIndex = languages.Contains(Localization.language.ToUpper()) ? languages.IndexOf(Localization.language.ToUpper()) : 0;
#else
            int langIndex = 0; // Default to English if translations are disabled.  
#endif
            if (translations[key].Count >= langIndex)
            {
                return translations[key][langIndex];
            }
            else
            {
                return key;
            }
        }
    }

    // This class is only to avoid writing TranslationsManager.GetTranslation bla bla bla every time I wanna use it.
    public static class Loc
    {
        public static string Get(string key, bool throwErrorIfNotFound = true)
        {
            return TranslationsManager.GetTranslation(key, throwErrorIfNotFound);
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(Localization), nameof(Localization.Get))]
    public static class UILocalizePatch
    {
        public static bool Prefix(ref string __result, string key)
        {
            string LETranslation = Loc.Get(key, false);
            if (LETranslation != key) // If the translation was succesfully.
            {
                __result = LETranslation;
                return false;
            }

            return true;
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(OptionsController), nameof(OptionsController.UpdateAllLocalizedLabels))]
    public static class OnLanguageChangedPatch
    {
        public static void Postfix()
        {
            if (EditorUIManager.Instance)
            {
                EditorUIManager.Instance.OnLanguageChanged();
            }
        }
    }
}
