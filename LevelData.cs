using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using MelonLoader;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace FS_LevelEditor
{
    [Serializable]
    public class LevelData
    {
        public List<LE_ObjectData> objects { get; set; } = new List<LE_ObjectData>();
        static readonly string levelsDirectory = Path.Combine(Application.persistentDataPath, "Custom Levels");

        // Create a LeveData instance with all of the current objects in the level.
        public static LevelData CreateLevelData()
        {
            LevelData data = new LevelData();

            GameObject objectsParent = EditorController.Instance.levelObjectsParent;

            foreach (GameObject obj in objectsParent.GetChilds())
            {
                LE_ObjectData objData = new LE_ObjectData(obj.GetComponent<LE_Object>());
                data.objects.Add(objData);
            }

            return data;
        }

        public static void SaveLevelData(string levelName, LevelData data = null)
        {
            // If the LevelData to save is null, create a new one with the objects in the current level.
            if (data == null)
            {
                data = CreateLevelData();
                Melon<Core>.Logger.Msg("Creating save data!");
            }

            try
            {
                // Serialize and save the level in JSON format.
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                };

                if (!Directory.Exists(levelsDirectory))
                {
                    Directory.CreateDirectory(levelsDirectory);
                }

                string filePath = Path.Combine(levelsDirectory, levelName + ".lvl");
                File.WriteAllText(filePath, JsonSerializer.Serialize(data, options));

                Melon<Core>.Logger.Msg("Level saved! Path: " + filePath);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        // This method is for loading the saved level in the LE.
        public static void LoadLevelData(string levelName)
        {
            GameObject objectsParent = EditorController.Instance.levelObjectsParent;
            objectsParent.DeleteAllChildren();

            string filePath = Path.Combine(levelsDirectory, levelName + ".lvl");
            LevelData data = JsonSerializer.Deserialize<LevelData>(File.ReadAllText(filePath));

            foreach (LE_ObjectData obj in data.objects)
            {
                GameObject objInstance = EditorController.Instance.PlaceObject(obj.objectOriginalName, obj.objPosition, obj.objRotation, false);
                LE_Object objClassInstance = objInstance.GetComponent<LE_Object>();

                objClassInstance.objectID = obj.objectID;
            }

            Melon<Core>.Logger.Msg($"\"{levelName}\" level loaded!");
        }

        public static string GetAvailableLevelName()
        {
            string levelName = "New Level";
            int counter = 1;

            if (!Directory.Exists(levelsDirectory)) return levelName;

            string[] existingLevels = Directory.GetFiles(levelsDirectory);
            while (existingLevels.Any(lvl => Path.GetFileNameWithoutExtension(lvl) == levelName))
            {
                levelName = $"{levelName} {counter}";
            }

            Melon<Core>.Logger.Msg("To return: " + levelName);
            return levelName;
        }

        public static LevelData[] GetLevelsList()
        {
            string[] levelsPaths = Directory.GetFiles(levelsDirectory);
            List<LevelData> levels = new List<LevelData>();

            foreach (string levelPath in levelsPaths)
            {
                LevelData levelData = JsonSerializer.Deserialize<LevelData>(File.ReadAllText(levelPath));
                levels.Add(levelData);
            }

            return levels.ToArray();
        }
    }

    [Serializable]
    public struct Vector3Serializable
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }

        public Vector3Serializable(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static implicit operator Vector3Serializable(Vector3 v)
        {
            return new Vector3Serializable(v.x, v.y, v.z);
        }
        public static implicit operator Vector3(Vector3Serializable v)
        {
            return new Vector3(v.x, v.y, v.z);
        }
    }
}
