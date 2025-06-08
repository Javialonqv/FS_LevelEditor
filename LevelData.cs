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
                if (obj.TryGetComponent<LE_Object>(out LE_Object component))
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
                // Serialize and save the level in JSON format.
                var options = new JsonSerializerOptions
                {
#if DEBUG
                    WriteIndented = true,
#endif
                    Converters = { new LEPropertiesConverter() }
                };

                if (!Directory.Exists(levelsDirectory))
                {
                    Directory.CreateDirectory(levelsDirectory);
                }

                string filePath = Path.Combine(levelsDirectory, levelFileNameWithoutExtension + ".lvl");
                File.WriteAllText(filePath, JsonSerializer.Serialize(data, options));

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

            var jsonOptions = new JsonSerializerOptions
            {
                Converters = { new LEPropertiesConverter() }
            };

            string filePath = Path.Combine(levelsDirectory, levelFileNameWithoutExtension + ".lvl");
            LevelData data = JsonSerializer.Deserialize<LevelData>(File.ReadAllText(filePath), jsonOptions);

            Camera.main.transform.position = data.cameraPosition;
            Camera.main.GetComponent<EditorCameraMovement>().SetRotation(data.cameraRotation);

            List<LE_ObjectData> toCheck = data.objects;
            if (Utilities.ListHasMultipleObjectsWithSameID(toCheck, false))
            {
                Logger.Warning("Multiple objects with same ID detected, trying to fix...");
                toCheck = FixMultipleObjectsWithSameID(toCheck);
            }

            currentLevelObjsCount = toCheck.Count;

#if DEBUG
            if (currentLevelObjsCount > 100)
            {
                Logger.DebugWarning("More than 100 objects in the level, not printing logs while instantiating objects!");
            }
#endif

            foreach (LE_ObjectData obj in data.objects)
            {
                GameObject objInstance = EditorController.Instance.PlaceObject(obj.objectOriginalName, obj.objPosition, obj.objRotation, false);
                LE_Object objClassInstance = objInstance.GetComponent<LE_Object>();

                objClassInstance.objectID = obj.objectID;
                objInstance.name = objClassInstance.objectFullNameWithID;
                objClassInstance.setActiveAtStart = obj.setActiveAtStart;

                if (obj.properties != null)
                {
                    foreach (var property in obj.properties)
                    {
                        objClassInstance.SetProperty(property.Key, Utilities.ConvertFromSerializableValue(property.Value));
                    }
                }

                // In case the object is defined to be disabled at start, change its materials to transparent.
                if (!objClassInstance.setActiveAtStart)
                {
                    objInstance.SetTransparentMaterials();
                }
            }

            // Load Global Properties.
            // Load it in this way since we don't want to REMOVE elements from the editor list, only modify or
            // add.
            foreach (var keyPair in data.globalProperties)
            {
                if (EditorController.Instance.globalProperties.ContainsKey(keyPair.Key))
                {
                    EditorController.Instance.globalProperties[keyPair.Key] = keyPair.Value;
                }
                else
                {
                    EditorController.Instance.globalProperties.Add(keyPair.Key, keyPair.Value);
                }
            }

            Logger.Log($"\"{data.levelName}\" level loaded in the editor!");
        }

        // And this for loading the saved level in playmode lol.
        public static void LoadLevelDataInPlaymode(string levelFileNameWithoutExtension)
        {
            LE_Object.GetTemplatesReferences();

            PlayModeController playModeCtrl = new GameObject("PlayModeController").AddComponent<PlayModeController>();

            GameObject objectsParent = playModeCtrl.levelObjectsParent;
            objectsParent.DeleteAllChildren();

            var jsonOptions = new JsonSerializerOptions
            {
                Converters = { new LEPropertiesConverter() }
            };

            string filePath = Path.Combine(levelsDirectory, levelFileNameWithoutExtension + ".lvl");
            LevelData data = JsonSerializer.Deserialize<LevelData>(File.ReadAllText(filePath), jsonOptions);

            playModeCtrl.levelFileNameWithoutExtension = levelFileNameWithoutExtension;
            playModeCtrl.levelName = data.levelName;

            List<LE_ObjectData> toCheck = data.objects;
            if (Utilities.ListHasMultipleObjectsWithSameID(toCheck, false))
            {
                Logger.Warning("Multiple objects with same ID detected, trying to fix...");
                toCheck = FixMultipleObjectsWithSameID(toCheck);
            }

            currentLevelObjsCount = toCheck.Count;

            foreach (LE_ObjectData obj in toCheck)
            {
                GameObject objInstance = playModeCtrl.PlaceObject(obj.objectOriginalName, obj.objPosition, obj.objRotation, false);
                LE_Object objClassInstance = objInstance.GetComponent<LE_Object>();

                objClassInstance.objectID = obj.objectID;
                objInstance.name = objClassInstance.objectFullNameWithID;
                objClassInstance.setActiveAtStart = obj.setActiveAtStart;

                if (obj.properties != null)
                {
                    foreach (var property in obj.properties)
                    {
                        objClassInstance.SetProperty(property.Key, Utilities.ConvertFromSerializableValue(property.Value));
                    }
                }

                objInstance.SetActive(obj.setActiveAtStart);
            }

            // Load Global Properties.
            // In PlayModeController the global properties list is empty, since it's meant to be replaced with
            // ALL of the values inside of the saved global properties list.
            playModeCtrl.globalProperties = new Dictionary<string, object>(data.globalProperties);

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
        public Vector3Serializable(Vector3 v)
        {
            this.x = v.x;
            this.y = v.y;
            this.z = v.z;
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
    [Serializable]
    public struct ColorSerializable
    {
        public float r { get; set; }
        public float g { get; set; }
        public float b { get; set; }

        public ColorSerializable(float r, float g, float b)
        {
            this.r = r;
            this.g = g;
            this.b = b;
        }
        public ColorSerializable(Color color)
        {
            this.r = color.r;
            this.g = color.g;
            this.b = color.b;
        }

        public static explicit operator ColorSerializable(Color color)
        {
            return new ColorSerializable(color.r, color.g, color.b);
        }
        public static explicit operator Color(ColorSerializable color)
        {
            return new Color(color.r, color.g, color.b);
        }

        public Color ToColor()
        {
            return new Color(r, g, b);
        }
    }


    public class LEPropertiesConverter : JsonConverter<Dictionary<string, object>>
    {
        public override void Write(Utf8JsonWriter writer, Dictionary<string, object> dictionary, JsonSerializerOptions options)
        {
            writer.WriteStartObject(); // Beginning of the dictionary

            foreach (var kvp in dictionary)
            {
                writer.WritePropertyName(kvp.Key); // Write the key
                writer.WriteStartObject(); // Beginning of the object for Type and Value.

                // Write the value type (AssemblyQualifiedName makes sure it includes the assembly).
                writer.WriteString("Type", kvp.Value.GetType().AssemblyQualifiedName);

                // Write the value, serializing according to its real type.
                writer.WritePropertyName("Value");
                JsonSerializer.Serialize(writer, kvp.Value, kvp.Value.GetType(), options);

                writer.WriteEndObject(); // End of Type/Value object.
            }

            writer.WriteEndObject(); // End of dictionary.
        }

        public override Dictionary<string, object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("JSON object was expected.");
            }

            var dictionary = new Dictionary<string, object>();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return dictionary; // End of dictionary.
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("A property name was expected.");
                }

                string key = reader.GetString(); // Read the key.
                reader.Read(); // Next key content.

                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new JsonException("An object with Type and Value was expected.");
                }

                string typeName = null;
                JsonElement valueJson = default;

                // Read the Type and Value properties.
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        break; // End of the Type/Value object.
                    }

                    if (reader.TokenType != JsonTokenType.PropertyName)
                    {
                        throw new JsonException("A property name (Type or Value) was expected.");
                    }

                    string propertyName = reader.GetString();
                    reader.Read();

                    if (propertyName == "Type")
                    {
                        typeName = reader.GetString();
                    }
                    else if (propertyName == "Value")
                    {
                        valueJson = JsonSerializer.Deserialize<JsonElement>(ref reader, options);
                    }
                    else
                    {
                        throw new JsonException($"Unexpected property: {propertyName}");
                    }
                }

                if (typeName == null)
                {
                    throw new JsonException("Property Type is missing.");
                }

                // Get the type from the type name.
                var type = Type.GetType(typeName);
                if (type == null)
                {
                    throw new JsonException($"Can't find '{typeName}' type.");
                }

                // Deserialize the value using the obtained type.
                var value = JsonSerializer.Deserialize(valueJson, type, options);
                dictionary[key] = value;
            }

            throw new JsonException("JSON unexpected end.");
        }
    }
}
