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
using System.Reflection.Metadata;

namespace FS_LevelEditor
{
    [RegisterTypeInIl2Cpp]
    public class PlayModeController : MonoBehaviour
    {
        public static PlayModeController Instance;
        Il2CppAssetBundle LEBundle;

        public string levelFileNameWithoutExtension;
        public string levelName;
        public Dictionary<string, object> globalProperties = new Dictionary<string, object>();

        GameObject editorObjectsRootFromBundle;
        List<string> categories = new List<string>();
        Dictionary<string, GameObject> allCategoriesObjects = new Dictionary<string, GameObject>();
        List<Dictionary<string, GameObject>> allCategoriesObjectsSorted = new List<Dictionary<string, GameObject>>();
        GameObject[] otherObjectsFromBundle;
        public GameObject levelObjectsParent;
        public List<LE_Object> currentInstantiatedObjects = new List<LE_Object>();

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
            TeleportPlayer();
            ConfigureGlobalProperties();
        }

        // When the script obj is destroyed, that means the scene has changed, unload the asset bundle and destroy the back to LE button, since it'll be created again when entering...
        // again...
        void OnDestroy()
        {
            UnloadBundle();

            Destroy(backToLEButton);
        }

        void TeleportPlayer()
        {
            LE_Player_Spawn spawn = FindObjectOfType<LE_Player_Spawn>();

            Controls.Instance.transform.position = spawn.transform.position + Vector3.up;
            Controls.Instance.gameCamera.transform.localPosition = new Vector3(0f, 0.907f, 0f);
            Controls.Instance.gameCamera.transform.eulerAngles = spawn.transform.eulerAngles;
            Controls.Instance.Angle = new Vector2(spawn.transform.eulerAngles.y, spawn.transform.eulerAngles.x);
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
                if (obj.name == "Player") continue;
                if (obj.name == "GUI") continue;

                obj.SetActive(false);
            }
        }

        void ConfigureGlobalProperties()
        {
            if (!(bool)GetGlobalProperty("HasTaser"))
            {
                Controls.Instance.DeactivateWeapon();
            }
        }
        object GetGlobalProperty(string name)
        {
            if (globalProperties.ContainsKey(name))
            {
                return globalProperties[name];
            }

            return null;
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
            Invoke("DestroyBackToLEButton", 0.2f);
            LE_MenuUIManager.Instance.GoBackToLEWhileInPlayMode(levelFileNameWithoutExtension, levelName);
        }
        void DestroyBackToLEButton()
        {
            Destroy(backToLEButton);
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

            otherObjectsFromBundle = LEBundle.Load<GameObject>("OtherObjects").GetChilds();
        }

        public GameObject LoadOtherObjectInBundle(string objectName)
        {
            return otherObjectsFromBundle.FirstOrDefault(obj => obj.name == objectName);
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

    public static void Postfix()
    {
        if (PlayModeController.Instance != null)
        {
            // For some STUPID reason, the chapter display doesn't show "CUSTOM LEVEL" as title, it seems the GetChapterTitle function isn't patched at all after FS 0.604.
            // Anyways, if it doesn't work, then modify the text directly when this function of get chapter name is called ;)
            GameObject.Find("(singleton) InGameUIManager/Camera/Panel/ChapterDisplay/Holder/ChapterTitleLabel").GetComponent<UILabel>().text = "CUSTOM LEVEL";
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

[HarmonyPatch(typeof(MenuController), nameof(MenuController.RestartCurrentLevelConfirmed))]
public static class OnChapterReset
{
    public static void Prefix()
    {
        if (PlayModeController.Instance != null)
        {
            // The asset bundle will be unloaded automatically in the PlayModeController class, since OnDestroy will be triggered.

            // Set this variable true again so when the scene is restarted, the custom level is as well.
            // The level file name inside of the Core class still there for cases like this one, so we don't need to get it again.
            Melon<Core>.Instance.loadCustomLevelOnSceneLoad = true;
        }
    }
}