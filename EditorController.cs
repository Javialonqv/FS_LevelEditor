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
        public string currentObjectName = "";

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
            else
            {
                previewObject.SetActive(false);
            }
        }

        public void ChangeCategory(int categoryID)
        {
            if (currentCategoryID == categoryID) return;

            currentCategoryID = categoryID;
            currentCategory = categories[currentCategoryID];
            EditorUIManager.Instance.SetupCurrentCategoryButtons();
        }

        public void SelectObject(string objName)
        {
            if (currentObjectName == objName) return;

            currentObjectName = objName;
            Melon<Core>.Logger.Msg(objName);

            Destroy(previewObject);
            previewObject = Instantiate(allCategoriesObjects[currentCategoryID][currentObjectName]);

            foreach (var collider in previewObject.TryGetComponents<Collider>())
            {
                collider.enabled = false;
            }
            foreach (var renderer in previewObject.TryGetComponents<MeshRenderer>())
            {
                foreach (var material in renderer.materials)
                {
                    material.SetInt("_ZWrite", 1);
                    material.color = new Color(0f, 0.666f, 0.894f, 1f);
                }
            }
        }

        void PreviewObject()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity))
            {
                previewObject.SetActive(true);
                previewObject.transform.position = hit.point;

                if (Input.GetMouseButtonDown(0))
                {
                    Melon<Core>.Logger.Msg(hit.normal);
                }
            }
            else
            {
                previewObject.SetActive(false);
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
