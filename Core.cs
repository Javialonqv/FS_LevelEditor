using Il2Cpp;
using MelonLoader;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

[assembly: MelonInfo(typeof(FS_LevelEditor.Core), "FS_LevelEditor", "0.1.0", "Javialon_qv", null)]
[assembly: MelonGame("Haze Games", "Fractal Space")]

namespace FS_LevelEditor
{
    public class Core : MelonMod
    {
        public static string currentSceneName;

        static readonly Vector3 groundBaseTopLeftPivot = new Vector3(-17f, 121f, -72f);

        public override void OnInitializeMelon()
        {
            
        }

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
            }
#endif
            if (sceneName.Contains("Menu"))
            {
                if (ExternalSpriteLoader.Instance == null) new GameObject("LE_ExternalSpriteLoader").AddComponent<ExternalSpriteLoader>();
                if (LE_MenuUIManager.Instance == null) new GameObject("LE_MEnuUIManager").AddComponent<LE_MenuUIManager>();
                LE_MenuUIManager.Instance.OnSceneLoaded();
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

                    EditorController.Instance.PlaceObject("Ground", position, Vector3.zero, false);
                }
            }
        }

        public GameObject CreateDirectionalLight(Vector3 position, Vector3 rotation)
        {
            GameObject lightObj = EditorController.Instance.PlaceObject("Directional Light", position, rotation, false);
            return lightObj;
        }
    }
}