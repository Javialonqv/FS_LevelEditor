using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Reflection;
using UnityEngine.SceneManagement;
using Il2Cpp;
using System.Collections;
using FS_LevelEditor;
using HarmonyLib;

namespace FS_LevelEditor
{
    [RegisterTypeInIl2Cpp]
    public class PlayModeController : MonoBehaviour
    {
        public static PlayModeController Instance;
        Il2CppAssetBundle LEBundle;

        public string levelFileNameWithoutExtension;
        public string levelName;

        GameObject editorObjectsRootFromBundle;
        List<string> categories = new List<string>();
        Dictionary<string, GameObject> allCategoriesObjects = new Dictionary<string, GameObject>();
        List<Dictionary<string, GameObject>> allCategoriesObjectsSorted = new List<Dictionary<string, GameObject>>();
        public GameObject levelObjectsParent;

        GameObject backToLEButton;

        void Awake()
        {
            Instance = this;

            LoadAssetBundle();

            Invoke("DisableTheCurrentScene", 0.2f);

            levelObjectsParent = new GameObject("LevelObjects");
            levelObjectsParent.transform.position = Vector3.zero;

            CreateBackToLEButton();
        }

        void Start()
        {
            // Teleport the player.
            Controls.Instance.transform.position = new Vector3(-13f, 123f, -68f);
            Controls.Instance.gameCamera.transform.localPosition = new Vector3(0f, 0.907f, 0f);
        }

        // When the script obj is destroyed, that means the scene has changed, unload the asset bundle and destroy the back to LE button, since it'll be created again when entering...
        // again...
        void OnDestroy()
        {
            UnloadBundle();

            Destroy(backToLEButton);
        }

        void DisableTheCurrentScene()
        {
            GameObject[] sceneObjects = SceneManager.GetActiveScene().GetRootGameObjects();

            foreach (GameObject obj in sceneObjects)
            {
                if (obj.name == gameObject.name) continue;
                if (obj.name == "Character") continue;
                if (obj.name == "FootStepController") continue;
                if (obj.name == "Checkpoints") continue;
                if (obj.name == "LevelObjects") continue;

                obj.SetActive(false);
            }
        }

        public GameObject PlaceObject(string objName, Vector3 position, Vector3 eulerAngles, bool setAsSelected = true)
        {
            GameObject template = allCategoriesObjects[objName];
            GameObject obj = Instantiate(template, levelObjectsParent.transform);

            obj.transform.localPosition = position;
            obj.transform.localEulerAngles = eulerAngles;

            LE_Object addedComp = LE_Object.AddComponentToObject(obj, objName);

            if (addedComp == null)
            {
                Destroy(obj);
                return null;
            }

            obj.SetActive(true);

            return obj;
        }

        void CreateBackToLEButton()
        {
            GameObject template = GameObject.Find("MainMenu/Camera/Holder/Main/LargeButtons/2_Chapters");
            backToLEButton = Instantiate(template, template.transform.parent);
            backToLEButton.name = "4_BackToLE";
            Destroy(backToLEButton.GetComponent<ButtonController>());
            Destroy(backToLEButton.GetChildWithName("Label").GetComponent<UILocalize>());
            backToLEButton.GetChildWithName("Label").GetComponent<UILabel>().text = "Back to Level Editor";

            backToLEButton.GetComponent<UIButton>().onClick.Add(new EventDelegate(this, nameof(GoBackToLEWhileInPlayMode)));

            backToLEButton.SetActive(true);
        }

        void GoBackToLEWhileInPlayMode()
        {
            Destroy(backToLEButton);
            LE_MenuUIManager.Instance.GoBackToLEWhileInPlayMode(levelFileNameWithoutExtension, levelName);
        }

        void LoadAssetBundle()
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FS_LevelEditor.level_editor");
            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes);

            LEBundle = Il2CppAssetBundleManager.LoadFromMemory(bytes);

            editorObjectsRootFromBundle = LEBundle.Load<GameObject>("LevelObjectsRoot");
            editorObjectsRootFromBundle.hideFlags = HideFlags.DontUnloadUnusedAsset;

            foreach (var child in editorObjectsRootFromBundle.GetChilds())
            {
                categories.Add(child.name);
            }


            foreach (var categoryObj in editorObjectsRootFromBundle.GetChilds())
            {
                Dictionary<string, GameObject> categoryObjects = new();

                foreach (var obj in categoryObj.GetChilds())
                {
                    categoryObjects.Add(obj.name, obj);
                    allCategoriesObjects.Add(obj.name, obj);
                }

                allCategoriesObjectsSorted.Add(categoryObjects);
            }
        }

        public T LoadFromLEBundle<T>(string name) where T : UnityEngine.Object
        {
            return LEBundle.Load<T>(name);
        }

        public void UnloadBundle()
        {
            LEBundle.Unload(false);
        }
    }
}

[HarmonyPatch(typeof(InGameUIManager), nameof(InGameUIManager.GetChapterTitle))]
public static class ChapterTextTitlePatch
{
    public static bool Prefix(ref string __result)
    {
        if (PlayModeController.Instance != null)
        {
            __result = "CUSTOM LEVEL";
            return false;
        }
        else
        {
            return true;
        }
    }
}
[HarmonyPatch(typeof(InGameUIManager), nameof(InGameUIManager.GetChapterName))]
public static class ChapterTextNamePatch
{
    public static bool Prefix(ref string __result)
    {
        if (PlayModeController.Instance != null)
        {
            __result = PlayModeController.Instance.levelName.ToUpper();
            return false;
        }
        else
        {
            return true;
        }
    }
}

[HarmonyPatch(typeof(Controls), nameof(Controls.KillCharacter), [typeof(bool), typeof(bool)])]
public static class OnPlayerDiePatch
{
    public static void Prefix()
    {
        if (PlayModeController.Instance != null)
        {
            // The asset bundle will be unloaded automatically in the PlayModeController class, since OnDestroy will be triggered.

            // Set this variable true again so when the scene is reloaded, the custom level is as well.
            // The level file name inside of the Core class still there for cases like this one, so we don't need to get it again.
            Melon<Core>.Instance.loadCustomLevelOnSceneLoad = true;
        }
    }
}