using FS_LevelEditor.Editor;
using FS_LevelEditor.Playmode;
using FS_LevelEditor.SaveSystem.Converters;
using FS_LevelEditor.SaveSystem.SerializableTypes;
using Harmony;
using Il2Cpp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FS_LevelEditor.SaveSystem
{
    public enum UpgradeType
    {
        DODGE,
        SPRINT,
        HYPER_SPEED
    }
    public class UpgradeSaveData
    {
        public UpgradeType type { get; set; }
        public bool active { get; set; }
        public int level { get; set; }

        public UpgradeSaveData() { }
        public UpgradeSaveData(UpgradeType _type, bool _active, int _level)
        {
            type = _type;
            active = _active;
            level = _level;
        }

        public static UpgradePageController.UpgradeType? ConvertTypeToFSType(UpgradeType type)
        {
            switch (type)
            {
                case UpgradeType.DODGE:
                    return UpgradePageController.UpgradeType.DODGE;
                case UpgradeType.SPRINT:
                    return UpgradePageController.UpgradeType.SPRINT;
                case UpgradeType.HYPER_SPEED:
                    return UpgradePageController.UpgradeType.CONCENTRATION;

                default:
                    return null;
            }
        }
    }

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
                    component.BeforeSave();
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

        public static LevelData GetLevelData(string levelFileNameWithoutExtension, bool printLogs = false)
        {
            string filePath = Path.Combine(levelsDirectory, levelFileNameWithoutExtension + ".lvl");
            LevelData data = null;
            LevelObjectDataConverter.RefreshCounters();
            try
            {
                data = JsonSerializer.Deserialize<LevelData>(File.ReadAllText(filePath), SavePatches.OnReadSaveFileOptions);
                if (printLogs) LevelObjectDataConverter.PrintLogs();
            }
            catch { }

            return data;
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
                    levelData = JsonSerializer.Deserialize<LevelData>(File.ReadAllText(levelPath), SavePatches.OnReadSaveFileOptions);
                }
                catch { }
                levels.Add(Path.GetFileNameWithoutExtension(levelPath), levelData);
            }

            return levels;
        }

        static LevelData LoadLevelData(string levelFileNameWithoutExtension)
        {
            LevelData data = GetLevelData(levelFileNameWithoutExtension, true);

            SavePatches.ReevaluateOldProperties(ref data);

            List<LE_ObjectData> toCheck = data.objects;
            if (Utils.ListHasMultipleObjectsWithSameID(toCheck, false))
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

			// Set camera properties in batch
			var cam = Camera.main;
			cam.transform.position = data.cameraPosition;
			cam.GetComponent<EditorCameraMovement>().SetRotation(data.cameraRotation);

			// Pre-allocate capacity for better performance
			var objectsToInstantiate = new List<(LE_Object.ObjectType type, Vector3 pos, Vector3 rot, Vector3 scale)>(data.objects.Count);

			// Batch collect object data
			foreach (LE_ObjectData obj in data.objects)
			{
				objectsToInstantiate.Add(((LE_Object.ObjectType type, Vector3 pos, Vector3 rot, Vector3 scale))(
					obj.objectType,
					obj.objPosition,
					obj.objRotation,
					obj.objScale
				));
			}

			// Clear existing objects
			GameObject objectsParent = EditorController.Instance.levelObjectsParent;
			objectsParent.DeleteAllChildren();

			// Batch instantiate objects
			var instantiatedObjects = new List<(GameObject obj, LE_ObjectData data)>(data.objects.Count);
			foreach (var objData in objectsToInstantiate)
			{
				var objInstance = EditorController.Instance.PlaceObject(
					objData.type,
					objData.pos,
					objData.rot,
					objData.scale,
					false
				);
				if (objInstance != null)
				{
					instantiatedObjects.Add((objInstance, data.objects[instantiatedObjects.Count]));
				}
			}

			// Batch configure objects
			foreach (var (obj, objData) in instantiatedObjects)
			{
				var objClassInstance = obj.GetComponent<LE_Object>();
				SetInstantiatedObjectProperties(objClassInstance, objData);

				if (!objClassInstance.setActiveAtStart)
				{
					obj.SetTransparentMaterials();
				}
			}

			// Batch apply global properties
			foreach (var keyPair in data.globalProperties)
			{
				if (EditorController.Instance.globalProperties.ContainsKey(keyPair.Key))
				{
					if (keyPair.Value is List<UpgradeSaveData>)
					{
						BatchApplyUpgradeData(keyPair, EditorController.Instance.globalProperties);
					}
					else
					{
						EditorController.Instance.globalProperties[keyPair.Key] = keyPair.Value;
					}
				}
			}

			EditorController.Instance.AfterFinishedLoadingLevel();
		}

		private static void BatchApplyUpgradeData(KeyValuePair<string, object> keyPair, Dictionary<string, object> targetProperties)
		{
			var savedList = keyPair.Value as List<UpgradeSaveData>;
			var defaultList = targetProperties[keyPair.Key] as List<UpgradeSaveData>;

			// Pre-create lookup for faster matching
			var upgradeMap = savedList.ToDictionary(x => x.type);

			for (int i = 0; i < defaultList.Count; i++)
			{
				if (upgradeMap.TryGetValue(defaultList[i].type, out var savedData))
				{
					defaultList[i] = savedData;
				}
			}
		}

		public static void LoadLevelDataInPlaymode(string levelFileNameWithoutExtension)
		{
			// Initialize essential components first
			LE_Object.GetTemplatesReferences();
			PlayModeController playModeCtrl = new GameObject("PlayModeController").AddComponent<PlayModeController>();

			// Pre-load level data before any instantiation
			LevelData data = LoadLevelData(levelFileNameWithoutExtension);

			// Clear existing objects in one operation
			GameObject objectsParent = playModeCtrl.levelObjectsParent;
			objectsParent.DeleteAllChildren();

			// Pre-allocate collections and batch object creation
			int objectCount = data.objects.Count;
			var objectsToInstantiate = new List<(LE_ObjectData data, GameObject obj)>(objectCount);

			// First pass: Create all GameObjects without configuring them
			foreach (LE_ObjectData obj in data.objects)
			{
				var objInstance = playModeCtrl.PlaceObject(
					obj.objectType,
					obj.objPosition,
					obj.objRotation,
					obj.objScale,
					false
				);

				if (objInstance != null)
				{
					objectsToInstantiate.Add((obj, objInstance));
				}
			}

			// Second pass: Configure all objects in batch
			foreach (var (objData, objInstance) in objectsToInstantiate)
			{
				var objClassInstance = objInstance.GetComponent<LE_Object>();
				SetInstantiatedObjectProperties(objClassInstance, objData);

				// Only handle inactive objects - active ones will initialize naturally
				if (!objData.setActiveAtStart)
				{
					objInstance.SetActive(false);
					objClassInstance.Start();
				}
			}

			// Set controller properties once
			playModeCtrl.levelFileNameWithoutExtension = levelFileNameWithoutExtension;
			playModeCtrl.levelName = data.levelName;

			// Batch apply global properties
			foreach (var keyPair in data.globalProperties)
			{
				if (playModeCtrl.globalProperties.ContainsKey(keyPair.Key))
				{
					// Handle JsonElement conversion in batch
					if (keyPair.Value is JsonElement jsonElement)
					{
						var targetType = playModeCtrl.globalProperties[keyPair.Key].GetType();
						playModeCtrl.globalProperties[keyPair.Key] = LEPropertiesConverterNew.NewDeserealize(targetType, jsonElement);
					}
					else
					{
						playModeCtrl.globalProperties[keyPair.Key] = keyPair.Value;
					}
				}
			}
		}
		static void SetInstantiatedObjectProperties(LE_Object spawnedObject, LE_ObjectData objectData)
        {
            spawnedObject.objectID = objectData.objectID;
            spawnedObject.gameObject.name = spawnedObject.objectFullNameWithID;
            spawnedObject.setActiveAtStart = objectData.setActiveAtStart;
            spawnedObject.collision = objectData.collision;
            spawnedObject.waypoints = objectData.waypoints;
            spawnedObject.startMovingAtStart = objectData.moveStart;
            spawnedObject.movingSpeed = objectData.movingSpeed;
            spawnedObject.startDelay = objectData.startDelay;
            spawnedObject.waitTime = objectData.waitTime;
            spawnedObject.waypointMode = objectData.wayMode;

            if (objectData.properties != null)
            {
                foreach (var property in objectData.properties)
                {
                    spawnedObject.SetProperty(property.Key, SavePatches.ConvertFromSerializableValue(property.Value));
                }
            }
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
            LevelData toRename = GetLevelData(levelFileNameWithoutExtension);

            toRename.levelName = newLevelName.Trim();

            // Save the file with the new name.
            SaveLevelData(newLevelName, levelFileNameWithoutExtension, toRename);

            string oldPath = Path.Combine(levelsDirectory, levelFileNameWithoutExtension + ".lvl");
            string newPath = Path.Combine(levelsDirectory, Utils.SanitizeFileName(newLevelName) + ".lvl");

            if (File.Exists(newPath))
            {
                newPath = Path.Combine(levelsDirectory, GetAvailableLevelName(newLevelName) + ".lvl");
            }

            Logger.Log("New level file path is: " + newPath);
            File.Move(oldPath, newPath);
        }

        // This method was generated by Grok AI LOL, I kinda understand it, but not at all LOL.
        static List<LE_ObjectData> FixMultipleObjectsWithSameID(List<LE_ObjectData> levelObjects)
        {
            // To know the used ids.
            var idUsage = new Dictionary<LE_Object.ObjectType, HashSet<int>>();
            var result = new List<LE_ObjectData>();

            // Find the max actual ID to generate new unique IDs.
            int maxId = levelObjects.Any() ? levelObjects.Max(item => item.objectID) : 0;

            foreach (var item in levelObjects)
            {
                if (item.objectType == null) continue; // Skip JUST IN CASE if the object type is null.

                LE_Object.ObjectType type = item.objectType.Value;
                int id = item.objectID;

                // If the name isn't in the dictionary, init a HashSet
                if (!idUsage.ContainsKey(type))
                {
                    // I didn't even knew it was possible to create new dictionary elements without using the "Add" function LOOOOL.
                    idUsage[type] = new HashSet<int>();
                }

                // If the ID is already used for this name, assign a new unique ID.
                if (idUsage[type].Contains(id))
                {
                    maxId++;
                    item.objectID = maxId;
                }
                else
                {
                    idUsage[type].Add(id);
                }

                result.Add(item);
            }

            return result;
        }

        public static Dictionary<string, object> GetDefaultGlobalProperties()
        {
            return new Dictionary<string, object>()
            {
                { "HasTaser", true },
                { "HasJetpack", true },
                { "DeathYLimit", 100f },
                { "Skybox", 0 },
                { "Upgrades", GetDefaultUpgradeSaveData() }
            };
        }
        public static List<UpgradeSaveData> GetDefaultUpgradeSaveData()
        {
            return new List<UpgradeSaveData>()
            {
                new (UpgradeType.DODGE, true, Controls.m_dodgeUpgradeMaxLevel),
                new (UpgradeType.SPRINT, true, 0),
                new (UpgradeType.HYPER_SPEED, true, 0)
            };
        }
    }
}