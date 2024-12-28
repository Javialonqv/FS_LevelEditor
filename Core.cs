using MelonLoader;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

[assembly: MelonInfo(typeof(FS_LevelEditor.Core), "FS_LevelEditor", "1.0.0", "Javialon_qv", null)]
[assembly: MelonGame("Haze Games", "Fractal Space")]

namespace FS_LevelEditor
{
    public class Core : MelonMod
    {
        public GameObject groundObj;

        static readonly Vector3 groundBaseTopLeftPivot = new Vector3(-17f, 121f, -72f);

        public override void OnInitializeMelon()
        {
            LoadAssetBundle();
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
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
        }

        public override void OnUpdate()
        {
            // Keybind to open the level editor.
            if (Input.GetKeyDown(KeyCode.Keypad5))
            {
                SetupEditorBasics();

                new GameObject("EditorController").AddComponent<EditorController>();
                new GameObject("EditorUIManager").AddComponent<EditorUIManager>();

                SpawnBase();
            }
        }

        void SetupEditorBasics()
        {
            // Disable Menu UI elements.
            GameObject.Find("MainMenu/Camera/Holder/Main").SetActive(false);
            GameObject.Find("MainMenu/Camera/Holder/Navigation").SetActive(false);

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
                    position.x += width * 2f;
                    position.z += height * 2f;

                    GameObject obj = GameObject.Instantiate(groundObj, EditorController.Instance.levelObjectsParent.transform);
                    obj.name = EditorController.Instance.GetObjectNameToInstantiate("Ground");
                    obj.transform.position = position;
                    foreach (var renderer in obj.TryGetComponents<MeshRenderer>())
                    {
                        foreach (var material in renderer.materials)
                        {
                            material.SetInt("_ZWrite", 1);
                        }
                    }
                }
            }
        }

        void LoadAssetBundle()
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FS_LevelEditor.level_editor");
            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes);

            Il2CppAssetBundle bundle = Il2CppAssetBundleManager.LoadFromMemory(bytes);

            groundObj = bundle.Load<GameObject>("Ground");
            groundObj.hideFlags = HideFlags.DontUnloadUnusedAsset;

            bundle.Unload(false);
        }
    }
}