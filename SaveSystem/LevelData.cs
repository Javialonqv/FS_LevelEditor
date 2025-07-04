using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Text.Json.Serialization;
using System.Text.Json;
using FS_LevelEditor.Editor;
using FS_LevelEditor.SaveSystem.Converters;
using FS_LevelEditor.SaveSystem.SerializableTypes;

namespace FS_LevelEditor.SaveSystem
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
        public Dictionary<string, object> globalProperties { get; set; } = new Dictionary<string, object>();

        public static int currentLevelObjsCount = 0;

        static readonly string levelsDirectory = Path.Combine(Application.persistentDataPath, "Custom Levels");

        // Create a LeveData instance with all of the current objects in the level.
        public static LevelData CreateLevelData(string levelName)
        {
            LevelData data = new LevelData();
            data.levelName = levelName;
            data.cameraPosition = Camera.main.transform.position;
            EditorCameraMovement editorCamera = Camera.main.GetComponent<EditorCameraMovement>();
            data.cameraRotation = new Vector3Serializable(editorCamera.xRotation, editorCamera.yRotation, 0f);
            data.createdTime = DateTimeOffset.Now.ToUnixTimeSeconds();

            if (EditorController.Instance.multipleObjectsSelected)
            {
                EditorController.Instance.currentSelectedObjects.ForEach(x => x.transform.parent = x.GetComponent<LE_Object>().objectParent);
            }

            GameObject objectsParent = EditorController.Instance.levelObjectsParent;

            // Don't get the disabled objects, since there are supposed to be DELETED objects.
            foreach (GameObject obj in objectsParent.GetChilds(false))
            {
                // Only if the object has the LE_Object component.
                if (obj.TryGetComponent(out LE_Object component))
                {
                    LE_ObjectData objData = new LE_ObjectData(component);
                    data.objects.Add(objData);
                }
                else
                {
                    Logger.Error($"The object with name \"{obj.name}\" doesn't have a LE_Object component, can't save it, please report it as a bug.");
                    continue;
                }
            }

            if (EditorController.Instance.multipleObjectsSelected)
            {
                EditorController.Instance.currentSelectedObjects.ForEach(x => x.transform.parent = EditorController.Instance.multipleSelectedObjsParent.transform);
            }

            data.globalProperties = new Dictionary<string, object>(EditorController.Instance.globalProperties);

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
                if (oldLevelData.createdTime != 0)
                {
                    data.createdTime = oldLevelData.createdTime;
                }
            }

            data.lastModificationTime = DateTimeOffset.Now.ToUnixTimeSeconds();

            try
            {
                if (!Directory.Exists(levelsDirectory))
                {
                    Directory.CreateDirectory(levelsDirectory);
                }

                string filePath = Path.Combine(levelsDirectory, levelFileNameWithoutExtension + ".lvl");
                File.WriteAllText(filePath, JsonSerializer.Serialize(data, SavePatches.OnWriteSaveFileOptions));

                Logger.Log("Level saved! Path: " + filePath);
            }
            catch (ArgumentException)
            {
                Logger.Error($"Error saving the file! The save path invalid. The level file name is: {levelFileNameWithoutExtension + ".lvl"}");
            }
            catch (DirectoryNotFoundException)
            {
                Logger.Error($"Error saving the file! Can't find the directory.");
            }
            catch (UnauthorizedAccessException)
            {
                Logger.Error($"Error saving the file! You don't have access to this file.");
            }
            catch (IOException e)
            {
                Logger.Error($"Error saving the file! Please, report the folowwing error as a bug: {e.Message}");
            }
        }

        public static LevelData GetLevelData(string levelFileNameWithoutExtension)
        {
            string filePath = Path.Combine(levelsDirectory, levelFileNameWithoutExtension + ".lvl");
            LevelData data = null;
            try
            {
                data = JsonSerializer.Deserialize<LevelData>(File.ReadAllText(filePath), SavePatches.OnReadSaveFileOptions);
            }
            catch { }

            return data;
        }

        static LevelData LoadLevelData(string levelFileNameWithoutExtension)
        {
            string filePath = Path.Combine(levelsDirectory, levelFileNameWithoutExtension + ".lvl");
            LevelData data = JsonSerializer.Deserialize<LevelData>(File.ReadAllText(filePath), SavePatches.OnReadSaveFileOptions);

            SavePatches.ReevaluateOldProperties(ref data);

            List<LE_ObjectData> toCheck = data.objects;
            if (Utilities.ListHasMultipleObjectsWithSameID(toCheck, false))
            {
                Logger.Warning("Multiple objects with same ID detected, trying to fix...");
                toCheck = FixMultipleObjectsWithSameID(toCheck);
            }
            data.objects = toCheck;

            currentLevelObjsCount = data.objects.Count;
#if DEBUG
            if (currentLevelObjsCount > 100)
            {
                Logger.DebugWarning("More than 100 objects in the level, not printing logs while instantiating objects!");
            }
#endif

            return data;
        }
        public static void LoadLevelDataInEditor(string levelFileNameWithoutExtension)
        {
            LevelData data = LoadLevelData(levelFileNameWithoutExtension);

            Camera.main.transform.position = data.cameraPosition;
            Camera.main.GetComponent<EditorCameraMovement>().SetRotation(data.cameraRotation);

            GameObject objectsParent = EditorController.Instance.levelObjectsParent;
            objectsParent.DeleteAllChildren();

            foreach (LE_ObjectData obj in data.objects)
            {
                GameObject objInstance = EditorController.Instance.PlaceObject(obj.objectOriginalName, obj.objPosition, obj.objRotation, obj.objScale, false);
                LE_Object objClassInstance = objInstance.GetComponent<LE_Object>();

                objClassInstance.objectID = obj.objectID;
                objInstance.name = objClassInstance.objectFullNameWithID;
                objClassInstance.setActiveAtStart = obj.setActiveAtStart;
                objClassInstance.collision = obj.collision;

                if (obj.properties != null)
                {
                    foreach (var property in obj.properties)
                    {
                        objClassInstance.SetProperty(property.Key, SavePatches.ConvertFromSerializableValue(property.Value));
                    }
                }

                // In case the object is defined to be disabled at start, change its materials to transparent.
                if (!objClassInstance.setActiveAtStart)
                {
                    objInstance.SetTransparentMaterials();
                }
            }

            // Load Global Properties.
            // Only load the SUPPORTED ones.
            foreach (var keyPair in data.globalProperties)
            {
                if (EditorController.Instance.globalProperties.ContainsKey(keyPair.Key))
                {
                    if (keyPair.Value is JsonElement) // The expected behaviour.
                    {
                        Type toConvert = EditorController.Instance.globalProperties[keyPair.Key].GetType();
                        EditorController.Instance.globalProperties[keyPair.Key] = LEPropertiesConverterNew.NewDeserealize(toConvert, (JsonElement)keyPair.Value);
                    }
                    else
                    {
                        EditorController.Instance.globalProperties[keyPair.Key] = keyPair.Value;
                    }
                }
            }

            EditorController.Instance.AfterFinishedLoadingLevel();
            Logger.Log($"\"{data.levelName}\" level loaded in the editor!");
        }
        public static void LoadLevelDataInPlaymode(string levelFileNameWithoutExtension)
        {
            LE_Object.GetTemplatesReferences();

            PlayModeController playModeCtrl = new GameObject("PlayModeController").AddComponent<PlayModeController>();

            GameObject objectsParent = playModeCtrl.levelObjectsParent;
            objectsParent.DeleteAllChildren();

            LevelData data = LoadLevelData(levelFileNameWithoutExtension);

            foreach (LE_ObjectData obj in data.objects)
            {
                GameObject objInstance = playModeCtrl.PlaceObject(obj.objectOriginalName, obj.objPosition, obj.objRotation, obj.objScale, false);
                LE_Object objClassInstance = objInstance.GetComponent<LE_Object>();

                objClassInstance.objectID = obj.objectID;
                objInstance.name = objClassInstance.objectFullNameWithID;
                objClassInstance.setActiveAtStart = obj.setActiveAtStart;
                objClassInstance.collision = obj.collision;

                if (obj.properties != null)
                {
                    foreach (var property in obj.properties)
                    {
                        objClassInstance.SetProperty(property.Key, SavePatches.ConvertFromSerializableValue(property.Value));
                    }
                }

                if (!obj.setActiveAtStart)
                {
                    // Only god knows when the user will enable the obj, so call Start() so the object calls InitComponent() to init the
                    // component NOW and not later.
                    objInstance.SetActive(false);
                    objClassInstance.Start();
                }
            }

            playModeCtrl.levelFileNameWithoutExtension = levelFileNameWithoutExtension;
            playModeCtrl.levelName = data.levelName;

            // Load Global Properties.
            // Only load the SUPPORTED ones.
            foreach (var keyPair in data.globalProperties)
            {
                if (playModeCtrl.globalProperties.ContainsKey(keyPair.Key))
                {
                    if (keyPair.Value is JsonElement) // The expected behaviour.
                    {
                        Type toConvert = playModeCtrl.globalProperties[keyPair.Key].GetType();
                        playModeCtrl.globalProperties[keyPair.Key] = LEPropertiesConverterNew.NewDeserealize(toConvert, (JsonElement)keyPair.Value);
                    }
                    else
                    {
                        playModeCtrl.globalProperties[keyPair.Key] = keyPair.Value;
                    }
                }
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
                    var jsonOptions = new JsonSerializerOptions
                    {
                        Converters =
                        {
                            //new LEPropertiesConverter(),
                            new LEPropertiesConverterNew(),
                            new OldPropertiesRename<LE_Event>(new Dictionary<string, string>
                            {
                                { "setActive", "spawn" }
                            })
                            // The conversion for old properties is in a different function since the FUCKING Json converter can't use 2 converters with the
                            // same type.
                        }
                    };

                    levelData = JsonSerializer.Deserialize<LevelData>(File.ReadAllText(levelPath), jsonOptions);
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

        // This method was generated by Grok AI LOL, I kinda understand it, but not at all LOL.
        static List<LE_ObjectData> FixMultipleObjectsWithSameID(List<LE_ObjectData> levelObjects)
        {
            // To know the used ids.
            var idUsage = new Dictionary<string, HashSet<int>>();
            var result = new List<LE_ObjectData>();

            // Find the max actual ID to generate new unique IDs.
            int maxId = levelObjects.Any() ? levelObjects.Max(item => item.objectID) : 0;

            foreach (var item in levelObjects)
            {
                string name = item.objectOriginalName;
                int id = item.objectID;

                // If the name isn't in the dictionary, init a HashSet
                if (!idUsage.ContainsKey(name))
                {
                    // I didn't even knew it was possible to create new dictionary elements without using the "Add" function LOOOOL.
                    idUsage[name] = new HashSet<int>();
                }

                // If the ID is already used for this name, assign a new unique ID.
                if (idUsage[name].Contains(id))
                {
                    maxId++;
                    item.objectID = maxId;
                }
                else
                {
                    idUsage[name].Add(id);
                }

                result.Add(item);
            }

            return result;
        }
    }
}