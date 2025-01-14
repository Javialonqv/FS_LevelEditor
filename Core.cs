using Il2Cpp;
using MelonLoader;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

[assembly: MelonInfo(typeof(FS_LevelEditor.Core), "FS_LevelEditor", "PROTOTYPE 0.0.1", "Javialon_qv", null)]
[assembly: MelonGame("Haze Games", "Fractal Space")]

namespace FS_LevelEditor
{
    public class Core : MelonMod
    {
        GameObject levelEditorUIButton;

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

            if (sceneName.Contains("Menu") && levelEditorUIButton == null)
            {
                CreateLEButton();
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

        // LE stands for "Level Editor" lmao.
        void CreateLEButton()
        {
            // The game disables the existing LE button since it detects we aren't in the unity editor or debugging, so I need to create a copy of the button.
            GameObject defaultLEButton = GameObject.Find("MainMenu/Camera/Holder/Main/LargeButtons/6_LevelEditor");
            levelEditorUIButton = GameObject.Instantiate(defaultLEButton, defaultLEButton.transform.parent);

            // And why not? Destroy the old button, since we don't need it anymore ;)
            GameObject.Destroy(defaultLEButton);

            // Change the button's label text.
            GameObject.Destroy(levelEditorUIButton.GetChildWithName("Label").GetComponent<UILocalize>());
            levelEditorUIButton.GetChildWithName("Label").GetComponent<UILabel>().text = "Level Editor";

            // Change the button's on click action.
            GameObject.Destroy(levelEditorUIButton.GetComponent<ButtonController>());

            // So... I just realized if you add a class to a gameobject with also a UIButton, the button will automatically call a "OnClick" function inside of the class if it exists,
            // without adding it manually to the UIButton via code... good to know :)
            LE_UIButtonActionCtrl onClickClass = levelEditorUIButton.AddComponent<LE_UIButtonActionCtrl>();

            // Finally, enable the button.
            levelEditorUIButton.SetActive(true);
        }

        public void SetupTheWholeEditor()
        {
            SetupEditorBasics();

            new GameObject("EditorController").AddComponent<EditorController>();
            new GameObject("EditorUIManager").AddComponent<EditorUIManager>();

            SpawnBase();
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

                    EditorController.Instance.PlaceObject("Ground", position, Quaternion.identity, false);
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