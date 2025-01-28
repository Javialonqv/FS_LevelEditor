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
using Il2Cpp;

namespace FS_LevelEditor
{
    [Serializable]
    public class LevelData
    {
        public string levelName { get; set; }
        public Vector3Serializable cameraPosition { get; set; }
        public Vector3Serializable cameraRotation { get; set; }
        public long createdTime { get; set; }
        public long lastModificationTime { get; set; }
        public List<LE_ObjectData> objects { get; set; } = new List<LE_ObjectData>();

        static readonly string levelsDirectory = Path.Combine(Application.persistentDataPath, "Custom Levels");

        // Create a LeveData instance with all of the current objects in the level.
        public static LevelData CreateLevelData(string levelName)
        {
            LevelData data = new LevelData();
            data.levelName = levelName;
            data.cameraPosition = Camera.main.transform.position;
            data.cameraRotation = Camera.main.transform.eulerAngles;
            data.createdTime = DateTimeOffset.Now.ToUnixTimeSeconds();

            if (EditorController.Instance.multipleObjectsSelected)
            {
                EditorController.Instance.currentSelectedObjects.ForEach(x => x.transform.parent = EditorController.Instance.levelObjectsParent.transform);
            }

            GameObject objectsParent = EditorController.Instance.levelObjectsParent;

            foreach (GameObject obj in objectsParent.GetChilds())
            {
                LE_ObjectData objData = new LE_ObjectData(obj.GetComponent<LE_Object>());
                data.objects.Add(objData);
            }

            if (EditorController.Instance.multipleObjectsSelected)
            {
                EditorController.Instance.currentSelectedObjects.ForEach(x => x.transform.parent = EditorController.Instance.multipleSelectedObjsParent.transform);
            }

            return data;
        }

        public static void SaveLevelData(string levelName, string levelFileNameWithoutExtension, LevelData data = null)
        {
            // If the LevelData to save is null, create a new one with the objects in the current level.
            if (data == null)
            {
                data = CreateLevelData(levelName);
            }

            LevelData oldLevelData = GetLevelData(levelFileNameWithoutExtension);
            if (oldLevelData != null)
            {
                data.createdTime = oldLevelData.createdTime;
            }

            data.lastModificationTime = DateTimeOffset.Now.ToUnixTimeSeconds();

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

                string filePath = Path.Combine(levelsDirectory, levelFileNameWithoutExtension + ".lvl");
                File.WriteAllText(filePath, JsonSerializer.Serialize(data, options));

                Logger.Log("Level saved! Path: " + filePath);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public static LevelData GetLevelData(string levelFileNameWithoutExtension)
        {
            string filePath = Path.Combine(levelsDirectory, levelFileNameWithoutExtension + ".lvl");
            LevelData data = null;
            try
            {
                data = JsonSerializer.Deserialize<LevelData>(File.ReadAllText(filePath));
            }
            catch { }

            return data;
        }

        // This method is for loading the saved level in the LE.
        public static void LoadLevelData(string levelFileNameWithoutExtension)
        {
            GameObject objectsParent = EditorController.Instance.levelObjectsParent;
            objectsParent.DeleteAllChildren();

            string filePath = Path.Combine(levelsDirectory, levelFileNameWithoutExtension + ".lvl");
            LevelData data = JsonSerializer.Deserialize<LevelData>(File.ReadAllText(filePath));

            Camera.main.transform.position = data.cameraPosition;
            Camera.main.transform.eulerAngles = data.cameraRotation;

            foreach (LE_ObjectData obj in data.objects)
            {
                GameObject objInstance = EditorController.Instance.PlaceObject(obj.objectOriginalName, obj.objPosition, obj.objRotation, false);
                LE_Object objClassInstance = objInstance.GetComponent<LE_Object>();

                objClassInstance.objectID = obj.objectID;
                objInstance.name = objClassInstance.objectFullNameWithID;
            }

            Logger.Log($"\"{data.levelName}\" level loaded!");
        }

        // And this for loading the saved level in playmode lol.
        public static void LoadLevelDataInPlaymode(string levelFileNameWithoutExtension)
        {
            PlayModeController playModeCtrl = new GameObject("PlayModeController").AddComponent<PlayModeController>();

            GameObject objectsParent = playModeCtrl.levelObjectsParent;
            objectsParent.DeleteAllChildren();

            string filePath = Path.Combine(levelsDirectory, levelFileNameWithoutExtension + ".lvl");
            LevelData data = JsonSerializer.Deserialize<LevelData>(File.ReadAllText(filePath));

            playModeCtrl.levelFileNameWithoutExtension = levelFileNameWithoutExtension;
            playModeCtrl.levelName = data.levelName;

            foreach (LE_ObjectData obj in data.objects)
            {
                GameObject objInstance = playModeCtrl.PlaceObject(obj.objectOriginalName, obj.objPosition, obj.objRotation, false);
                LE_Object objClassInstance = objInstance.GetComponent<LE_Object>();

                objClassInstance.objectID = obj.objectID;
                objInstance.name = objClassInstance.objectFullNameWithID;
            }

            Logger.Log($"\"{data.levelName}\" level loaded in playmode!");
        }

        public static string GetAvailableLevelName(string levelNameOriginal = "New Level")
        {
            string levelName = levelNameOriginal;
            string toReturn = levelName;
            int counter = 1;

            if (!Directory.Exists(levelsDirectory)) return levelName;

            string[] existingLevels = Directory.GetFiles(levelsDirectory);
            while (existingLevels.Any(lvl => Path.GetFileNameWithoutExtension(lvl) == toReturn))
            {
                toReturn = $"{levelName} {counter}";
                counter++;
            }

            return toReturn;
        }

        public static Dictionary<string, LevelData> GetLevelsList()
        {
            if (!Directory.Exists(levelsDirectory)) Directory.CreateDirectory(levelsDirectory);

            string[] levelsPaths = Directory.GetFiles(levelsDirectory, "*.lvl");
            Dictionary<string, LevelData> levels = new Dictionary<string, LevelData>();

            foreach (string levelPath in levelsPaths)
            {
                LevelData levelData = null;
                try
                {
                    levelData = JsonSerializer.Deserialize<LevelData>(File.ReadAllText(levelPath));
                }
                catch { }
                levels.Add(Path.GetFileNameWithoutExtension(levelPath), levelData);
            }

            return levels;
        }

        public static void DeleteLevel(string levelFileNameWithoutExtension)
        {
            string path = Path.Combine(levelsDirectory, levelFileNameWithoutExtension + ".lvl");
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public static void RenameLevel(string levelFileNameWithoutExtension, string newLevelName)
        {
            // Get the level to rename.
            var levels = GetLevelsList();
            LevelData toRename = levels[levelFileNameWithoutExtension];
            string oldLevelName = toRename.levelName;

            // Set the new level in the level data.
            toRename.levelName = newLevelName;

            // Save the file with the new name.
            SaveLevelData(newLevelName, levelFileNameWithoutExtension, toRename);

            // If the level file name is the same than the level name itself, also rename the file name.
            if (levelFileNameWithoutExtension.Equals(oldLevelName))
            {
                Logger.Log("Level file name is the same than the old level name, renaming the file name as well.");
                string oldPath = Path.Combine(levelsDirectory, levelFileNameWithoutExtension + ".lvl");
                string newPath = Path.Combine(levelsDirectory, newLevelName + ".lvl");

                if (File.Exists(newPath))
                {
                    Logger.Warning("The level file name already exists. Selecting a new leve file name to avoid conflicts, level file name won't be renamed in the next edits.");
                    newPath = Path.Combine(levelsDirectory, GetAvailableLevelName(newLevelName) + ".lvl");
                }

                Logger.Log("New level file path is: " + newPath);
                File.Move(oldPath, newPath);
            }
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
