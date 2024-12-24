using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MelonLoader;

namespace FS_LevelEditor
{
    [RegisterTypeInIl2Cpp]
    public class EditorController : MonoBehaviour
    {
        public static EditorController Instance { get; private set; }

        GameObject rootBundleObject;
        public List<string> categories = new List<string>();

        public string currentCategory = "";
        public int currentCategoryID = 0;
        public List<Dictionary<string, GameObject>> allCategoriesObjects = new List<Dictionary<string, GameObject>>();

        GameObject previewObject = null;

        void Awake()
        {
            Instance = this;
            LoadAssetBundle();
        }

        void Start()
        {
            previewObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            previewObject.GetComponent<BoxCollider>().enabled = false;
            previewObject.transform.localScale = Vector3.one * 0.5f;
        }

        void Update()
        {
            if (!Input.GetMouseButton(1))
            {
                PreviewObject();
            }
        }

        void PreviewObject()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity))
            {
                Melon<Core>.Logger.Msg("hit");

                previewObject.transform.position = hit.point;
            }
        }

        void LoadAssetBundle()
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FS_LevelEditor.level_editor");
            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes);

            Il2CppAssetBundle bundle = Il2CppAssetBundleManager.LoadFromMemory(bytes);

            rootBundleObject = bundle.Load<GameObject>("LevelObjectsRoot");
            rootBundleObject.hideFlags = HideFlags.DontUnloadUnusedAsset;

            foreach (var child in rootBundleObject.GetChilds())
            {
                categories.Add(child.name);
            }

            currentCategory = categories[0];
            currentCategoryID = 0;

            foreach (var categoryObj in rootBundleObject.GetChilds())
            {
                Dictionary<string, GameObject> categoryObjects = new();

                foreach (var obj in categoryObj.GetChilds())
                {
                    categoryObjects.Add(obj.name, obj);
                }

                allCategoriesObjects.Add(categoryObjects);
            }

            bundle.Unload(false);
        }
    }
}
