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
                string[] columns = lines[i].Trim().Split(',');

                if (i == 0)
                {
                    for (int j = 1; j < columns.Length; j++)
                    {
                        languages.Add(columns[j]);
                    }
                    continue;
                }

                if (string.IsNullOrEmpty(columns[0])) continue;

                string currentKey = columns[0];
                List<string> currentKeyTranslations = new List<string>();
                for (int j = 1; j < columns.Length; j++)
                {
                    currentKeyTranslations.Add(columns[j].Trim());
                }

                translations.Add(currentKey, currentKeyTranslations);
            }
        }

        public static string GetTranslation(string key)
        {
            if (!initialized)
            {
                Logger.Error("Translations Manager Script is NOT initialized yet!");
                return null;
            }
            if (!translations.ContainsKey(key))
            {
                Logger.Error($"\"{key}\" doesn't exists in the LE Translations!");
                return key;
            }

            int langIndex = languages.Contains(Localization.language) ? languages.IndexOf(Localization.language) : 0;
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
        public static string Get(string key)
        {
            return TranslationsManager.GetTranslation(key);
        }
    }
}
