using DemoMod;
using FS_LevelEditor.Editor;
using FS_LevelEditor.Editor.UI;
using FS_LevelEditor.Playmode;
using FS_LevelEditor.Playmode.Patches;
using FS_LevelEditor.SaveSystem;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FS_LevelEditor
{
    public class Core
    {
        // MOD METADATA
        public const string ModName = "Level Editor";
        public const string Author = "Javialon_qv & Cafe";
        public const string Version = "0.1.0";
        public const string Description = "An unofficial Level Editor made for Fractal Space!";
        public const bool SupportsHotReload = false;


        // TODO: Add functionality for these ones.
        public static Harmony HarmonyInstance;

        public static string currentSceneName;
        public static bool loadCustomLevelOnSceneLoad;
        public static string levelFileNameWithoutExtensionToLoad;
        public static int totalDeathsInCurrentPlaymodeSession = 0;
        public static string LevelNameJustQuitFrom = "";
        public static bool JustQuitPlaymode = false;

        static readonly Vector3 groundBaseTopLeftPivot = new Vector3(-17f, 121f, -72f);

        public static bool isQuitting;

        public static void OnModLoaded()
        {
            Logger.Log("Level Editor mod loaded!");

            LE_CustomErrorPopups.Init();
            FixedUpdateProvider.Init();

            AssetBundleLoader.PreloadEmbeddedBundle("level_editor");
            AssetBundleLoader.PreloadEmbeddedBundle("leveleditoricons");

            HarmonyInstance = new Harmony("com.javialon_qv.leveleditor");
            HarmonyInstance.PatchAll();

            SceneManager.sceneLoaded += OnSceneWasLoaded;

            // Since the mod gets loaded AFTER the Menu scene is loaded, call OnSceneWasLoaded manually for the Menu scene.
            if (SceneManager.GetActiveScene().name.Contains("Menu"))
            {
                Logger.Log("LE loaded manually from the Fractal Loader menu!");
                OnSceneWasLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Additive);
            }
            else
            {
                Logger.Log("LE loaded automatically at the game's startup!");
            }
            //TODO: Remove when it's the time
            if(ModConsole.SecondaryToggleKey == KeyCode.Escape)
                ModConsole.SecondaryToggleKey = KeyCode.None;
        }

        public static void OnSceneWasLoaded(Scene scene, LoadSceneMode loadMode)
        {
            int buildIndex = scene.buildIndex;
            string sceneName = scene.name;

            currentSceneName = sceneName;

            MaterialUtils.ResetMaterialWithColorsReferences();

            // Debug option to know the camera position when using Free Cam from Unity Explorer.
#if DEBUG
            if (sceneName.Contains("Menu"))
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.parent = Camera.main.transform;
                cube.transform.localPosition = Vector3.zero;
                cube.transform.rotation = Quaternion.identity;
                cube.GetComponent<MeshRenderer>().castShadows = false;
            }
#endif
            if (sceneName.Contains("Menu"))
            {
                if (!ExternalSpriteLoader.Instance) new GameObject("LE_ExternalSpriteLoader").AddComponent<ExternalSpriteLoader>();
                if (!LE_MenuUIManager.Instance) new GameObject("LE_MEnuUIManager").AddComponent<LE_MenuUIManager>();
                LE_MenuUIManager.Instance.OnSceneLoaded(sceneName);
            }

            if (sceneName.Contains("Level4_PC") && loadCustomLevelOnSceneLoad)
            {
                LevelData.LoadLevelDataInPlaymode(levelFileNameWithoutExtensionToLoad);
                loadCustomLevelOnSceneLoad = false;
            }
            else
            {
                // Reset this variable.
                totalDeathsInCurrentPlaymodeSession = 0;
            }

            if (!sceneName.Contains("Level4_PC") && JustQuitPlaymode)
            {
                DeleteAutoSaveFilesPatch.DeleteCurrentLevelAutoSaveFileIfExists(LevelNameJustQuitFrom);

                LevelNameJustQuitFrom = "";
                JustQuitPlaymode = false;
            }
        }

        public static void SetupTheWholeEditor(bool willLoadALevel = false)
        {
            SetupEditorBasics();

            new GameObject("EditorController").AddComponent<EditorController>();
            new GameObject("EditorUIManager").AddComponent<EditorUIManager>();

            if (!willLoadALevel)
            {
                SpawnBase();
                CreateDirectionalLight(new Vector3(-13f, 130f, -56f), new Vector3(45f, 180f, 0f));
                CreatePlayerSpawn(new Vector3(-13f, 121.5f, -68f), Vector3.zero);
            }
        }

        static void SetupEditorBasics()
        {
            // Disable the Menu Level objects.
            GameObject.Find("Level").SetActive(false);

            // Set camera's new position and rotation.
            GameObject camera = GameObject.Find("Main Camera");
            GameObject.Destroy(camera.GetComponent<Animation>());
            camera.transform.position = new Vector3(-15f, 125f, -75f);
            camera.transform.localEulerAngles = new Vector3(45f, 0f, 0f);

            // Add the camera movement component to... well... the camera.
            camera.AddComponent<EditorCameraMovement>();
        }

        static void SpawnBase()
        {
            for (int width = 0; width < 3; width++)
            {
                for (int height = 0; height < 3; height++)
                {
                    Vector3 position = groundBaseTopLeftPivot;
                    position.x += width * 4f;
                    position.z += height * 4f;

                    EditorController.Instance.PlaceObject(LE_Object.ObjectType.GROUND, position, Vector3.zero, Vector3.one, false);
                }
            }
        }

        static public GameObject CreateDirectionalLight(Vector3 position, Vector3 rotation)
        {
            GameObject lightObj = EditorController.Instance.PlaceObject(LE_Object.ObjectType.DIRECTIONAL_LIGHT, position, rotation, Vector3.one, false);
            return lightObj;
        }

        static public GameObject CreatePlayerSpawn(Vector3 position, Vector3 rotation)
        {
            GameObject playerSpanw = EditorController.Instance.PlaceObject(LE_Object.ObjectType.PLAYER_SPAWN, position, rotation, Vector3.one, false);
            return playerSpanw;
        }

        public static GameObject LoadOtherObjectInBundle(string objectName)
        {
            if (EditorController.Instance != null && PlayModeController.Instance == null)
            {
                return EditorController.Instance.LoadOtherObjectInBundle(objectName);
            }
            else if (EditorController.Instance == null && PlayModeController.Instance != null)
            {
                return PlayModeController.Instance.LoadOtherObjectInBundle(objectName);
            }

            return null;
        }

        public void OnApplicationQuit()
        {
            isQuitting = true;
        }

    }
}