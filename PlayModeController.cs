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

        public string levelName;

        GameObject editorObjectsRootFromBundle;
        List<string> categories = new List<string>();
        Dictionary<string, GameObject> allCategoriesObjects = new Dictionary<string, GameObject>();
        List<Dictionary<string, GameObject>> allCategoriesObjectsSorted = new List<Dictionary<string, GameObject>>();

        public GameObject levelObjectsParent;

        void Awake()
        {
            Instance = this;

            LoadAssetBundle();

            Invoke("DisableTheCurrentScene", 0.2f);

            levelObjectsParent = new GameObject("LevelObjects");
            levelObjectsParent.transform.position = Vector3.zero;
        }

        void Start()
        {
            // Teleport the player.
            Controls.Instance.transform.position = new Vector3(-13f, 123f, -68f);
            Controls.Instance.gameCamera.transform.localPosition = new Vector3(0f, 0.907f, 0f);
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

        void LoadAssetBundle()
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FS_LevelEditor.level_editor");
            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes);

            Il2CppAssetBundle bundle = Il2CppAssetBundleManager.LoadFromMemory(bytes);

            editorObjectsRootFromBundle = bundle.Load<GameObject>("LevelObjectsRoot");
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

            bundle.Unload(false);
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