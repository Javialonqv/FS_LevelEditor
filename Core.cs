using Il2Cpp;
using Il2CppInControl;
using MelonLoader;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using FS_LevelEditor.Editor;
using FS_LevelEditor.Editor.UI;
using FS_LevelEditor.SaveSystem;
using FS_LevelEditor.Playmode;

[assembly: MelonInfo(typeof(FS_LevelEditor.Core), "FS_LevelEditor", "0.2.1", "Javialon_qv", null)]
[assembly: MelonGame("Haze Games", "Fractal Space")]

namespace FS_LevelEditor
{
    public class Core : MelonMod
    {
        public static string currentSceneName;
        public bool loadCustomLevelOnSceneLoad;
        public string levelFileNameWithoutExtensionToLoad;
        public int totalDeathsInCurrentPlaymodeSession = 0;

        static readonly Vector3 groundBaseTopLeftPivot = new Vector3(-17f, 121f, -72f);

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            currentSceneName = sceneName;

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
        }

        public override void OnUpdate()
        {
#if DEBUG
            // Keybind to open the level editor.
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.L))
            {
                SetupEditorBasics();

                new GameObject("EditorController").AddComponent<EditorController>();
                new GameObject("EditorUIManager").AddComponent<EditorUIManager>();

                SpawnBase();
            }
#endif
        }

        public void SetupTheWholeEditor(bool willLoadALevel = false)
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

        void SetupEditorBasics()
        {
            // Disable the Menu Level objects.
            GameObject.Find("Level").SetActive(false);

            // Set camera's new position and rotation.
            GameObject.Destroy(GameObject.Find("Main Camera").GetComponent<Animation>());
            GameObject.Find("Main Camera").transform.position = new Vector3(-15f, 125f, -75f);
            GameObject.Find("Main Camera").transform.localEulerAngles = new Vector3(45f, 0f, 0f);

            // Add the camera movement component to... well... the camera.
            GameObject.Find("Main Camera").AddComponent<EditorCameraMovement>();
        }

        void SpawnBase()
        {
            for (int width = 0; width < 3; width++)
            {
                for (int height = 0; height < 3; height++)
                {
                    Vector3 position = groundBaseTopLeftPivot;
                    position.x += width * 4f;
                    position.z += height * 4f;

                    EditorController.Instance.PlaceObject("Ground", position, Vector3.zero, Vector3.one, false);
                }
            }
        }

        public GameObject CreateDirectionalLight(Vector3 position, Vector3 rotation)
        {
            GameObject lightObj = EditorController.Instance.PlaceObject("Directional Light", position, rotation, Vector3.one, false);
            return lightObj;
        }

        public GameObject CreatePlayerSpawn(Vector3 position, Vector3 rotation)
        {
            GameObject playerSpanw = EditorController.Instance.PlaceObject("Player Spawn", position, rotation, Vector3.one, false);
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
    }
}