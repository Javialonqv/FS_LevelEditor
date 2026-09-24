using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using HarmonyLib;

namespace FS_LevelEditor.LE_Objects
{
    public static class ModObjectRegistry
    {
        private static readonly Dictionary<string, RegisteredObjectData> Registry = new(StringComparer.OrdinalIgnoreCase);
        private static readonly string ModsDirectory = Path.Combine(Application.persistentDataPath, "Mods", "Objects");

        public static void Initialize()
        {
            Registry.Clear();

            if (!Directory.Exists(ModsDirectory))
            {
                Directory.CreateDirectory(ModsDirectory);
                return;
            }

            foreach (string objectFolder in Directory.GetDirectories(ModsDirectory))
            {
                LoadObjectModule(objectFolder);
            }
        }
        private static void LoadObjectModule(string folderPath)
        {
            string folderName = Path.GetFileName(folderPath);
            string jsonPath = Path.Combine(folderPath, $"{folderName}.json");

            if (!File.Exists(jsonPath))
            {
                // Fallback check for config.json
                jsonPath = Path.Combine(folderPath, "config.json");
                if (!File.Exists(jsonPath)) return;
            }

            try
            {
                JObject config = JObject.Parse(File.ReadAllText(jsonPath));
                string classname = config.Value<string>("classname");
                string bundleName = config.Value<string>("assetBundle");
                string prefabName = config.Value<string>("prefabName");
                string dllName = config.Value<string>("logicAssembly");
                string logicClassName = config.Value<string>("logicClassName");

                // 1. Load Assembly & Run Harmony Patches
                Type logicType = null;
                if (!string.IsNullOrEmpty(dllName))
                {
                    string dllPath = Path.Combine(folderPath, dllName);
                    if (File.Exists(dllPath))
                    {
                        Assembly assembly = Assembly.LoadFrom(dllPath);
                        logicType = assembly.GetType(logicClassName);

                        // Auto-apply Harmony patches contained in the mod DLL
                        var harmony = new Harmony($"com.fle.object.{classname.ToLower()}");
                        harmony.PatchAll(assembly);
                    }
                }

                // 2. Load Asset Bundle
                GameObject prefabTemplate = null;
                if (!string.IsNullOrEmpty(bundleName))
                {
                    string bundlePath = Path.Combine(folderPath, bundleName);
                    if (File.Exists(bundlePath))
                    {
                        AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);
                        if (bundle != null)
                        {
                            prefabTemplate = bundle.LoadAsset<GameObject>(prefabName);
                            // Keep loaded asset in memory while freeing the bundle header
                            bundle.Unload(false);
                        }
                    }
                }

                // 3. Parse Metadata
                Vector3 scale = Vector3.one;
                if (config["defaultScale"] != null)
                {
                    scale = new Vector3(
                        config["defaultScale"].Value<float>("x"),
                        config["defaultScale"].Value<float>("y"),
                        config["defaultScale"].Value<float>("z")
                    );
                }

                Dictionary<string, object> defaultProps = new();
                if (config["defaultProperties"] != null)
                {
                    defaultProps = config["defaultProperties"].ToObject<Dictionary<string, object>>();
                }

                RegisteredObjectData registeredObject = new RegisteredObjectData
                {
                    Classname = classname,
                    DisplayName = config.Value<string>("displayName") ?? classname,
                    SnapCategory = config.Value<string>("snapCategory") ?? "NONE",
                    PrefabTemplate = prefabTemplate,
                    LogicComponentType = logicType ?? typeof(LE_Object),
                    DefaultScale = scale,
                    EventsIDs = config["eventsIDs"]?.ToObject<string[]>() ?? Array.Empty<string>(),
                    DefaultProperties = defaultProps
                };

                Registry[classname] = registeredObject;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ModObjectRegistry] Failed to load module at {folderPath}: {ex.Message}");
            }
        }

        public static bool TryGet(string classname, out RegisteredObjectData data)
        {
            return Registry.TryGetValue(classname, out data);
        }

        public static bool IsRegistered(string classname) => Registry.ContainsKey(classname);

        public static IEnumerable<RegisteredObjectData> GetAllRegisteredObjects() => Registry.Values;
    }
}
