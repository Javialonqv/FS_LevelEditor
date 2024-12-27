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
        public GameObject currentSelectedObj;
        public string currentObjectName = "";

        GameObject levelObjectsParent;
        GameObject previewObject = null;

        public enum Mode { Building, Selection, Deletion }
        public Mode currentMode = Mode.Building;

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
            previewObject.name = "Default";

            levelObjectsParent = new GameObject("LevelObjects");
            levelObjectsParent.transform.position = Vector3.zero;
        }

        void Update()
        {
            if (!Input.GetMouseButton(1) && currentMode == Mode.Building)
            {
                PreviewObject();
            }
            else
            {
                previewObject.SetActive(false);
            }

            if (Input.GetMouseButtonDown(0) && currentMode == Mode.Selection)
            {
                if (SelectObjectWithRay(out GameObject obj))
                {
                    SetSelectedObj(obj);
                }
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                ChangeMode(Mode.Building);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                ChangeMode(Mode.Selection);
            }
        }

        public void ChangeMode(Mode mode)
        {
            currentMode = mode;

            switch (currentMode)
            {
                case Mode.Building:
                    EditorUIManager.Instance.categoryButtons.ForEach(button => button.SetActive(true));
                    EditorUIManager.Instance.currentCategoryBG.SetActive(true);
                    break;

                case Mode.Selection:
                    EditorUIManager.Instance.categoryButtons.ForEach(button => button.SetActive(false));
                    EditorUIManager.Instance.currentCategoryBG.SetActive(false);
                    break;
            }
        }

        public void ChangeCategory(int categoryID)
        {
            if (currentCategoryID == categoryID) return;

            currentCategoryID = categoryID;
            currentCategory = categories[currentCategoryID];
            EditorUIManager.Instance.SetupCurrentCategoryButtons();
        }

        public void SelectObjectToBuild(string objName)
        {
            if (currentObjectName == objName) return;

            currentObjectName = objName;
            currentSelectedObj = allCategoriesObjects[currentCategoryID][currentObjectName];
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
                previewObject.transform.up = hit.normal;

                if (hit.collider.gameObject.name.StartsWith("StaticPos"))
                {
                    if (hit.collider.transform.parent.name == currentObjectName)
                    {
                        previewObject.transform.position = hit.collider.transform.position;
                        previewObject.transform.rotation = hit.collider.transform.rotation;
                    }
                }

                if (Input.GetMouseButtonDown(0) && previewObject.name != "Default")
                {
                    Melon<Core>.Logger.Msg(hit.normal);
                    PlaceObject();
                }
            }
            else
            {
                previewObject.SetActive(false);
            }
        }

        void PlaceObject()
        {
            GameObject obj = Instantiate(previewObject, levelObjectsParent.transform);
            obj.name = allCategoriesObjects[currentCategoryID][currentObjectName].name;

            foreach (var collider in obj.TryGetComponents<Collider>())
            {
                collider.enabled = true;
            }
            foreach (var renderer in obj.TryGetComponents<MeshRenderer>())
            {
                foreach (var material in renderer.materials)
                {
                    material.SetInt("_ZWrite", 1);
                    material.color = new Color(1f, 1f, 1f, 1f);
                }
            }

            SetSelectedObj(obj);
        }

        bool SelectObjectWithRay(out GameObject obj)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, -1, QueryTriggerInteraction.Ignore))
            {
                obj = hit.collider.transform.parent.gameObject;
                return true;
            }
            else
            {
                obj = null;
                return false;
            }
        }

        void SetSelectedObj(GameObject obj)
        {
            if (currentSelectedObj == obj) return;

            if (currentSelectedObj != null)
            {
                foreach (var renderer in currentSelectedObj.TryGetComponents<MeshRenderer>())
                {
                    foreach (var material in renderer.materials)
                    {
                        material.SetInt("_ZWrite", 1);
                        material.color = new Color(1f, 1f, 1f, 1f);
                    }
                }
            }

            currentSelectedObj = obj;

            foreach (var renderer in currentSelectedObj.TryGetComponents<MeshRenderer>())
            {
                foreach (var material in renderer.materials)
                {
                    material.SetInt("_ZWrite", 1);
                    material.color = new Color(0f, 1f, 0f, 1f);
                }
            }

            EditorUIManager.Instance.SetSelectedObject(obj.name);
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
