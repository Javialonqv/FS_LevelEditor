using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MelonLoader;
using System.Text.RegularExpressions;

namespace FS_LevelEditor
{
    [RegisterTypeInIl2Cpp]
    public class EditorController : MonoBehaviour
    {
        public static EditorController Instance { get; private set; }

        GameObject moveObjectsArrows;

        GameObject rootBundleObject;
        public List<string> categories = new List<string>();

        public string currentCategory = "";
        public int currentCategoryID = 0;
        public List<Dictionary<string, GameObject>> allCategoriesObjects = new List<Dictionary<string, GameObject>>();
        public GameObject currentSelectedObj;
        public string currentObjectName = "";

        public GameObject levelObjectsParent;
        GameObject previewObject = null;

        public enum Mode { Building, Selection, Deletion }
        public Mode currentMode = Mode.Building;

        enum Arrow { None, X, Y, Z }
        Arrow collidingArrow;
        Vector3 mousePositionWhenArrowClick;
        Vector3 objPositionWhenArrowClick;
        Plane movementPlane;

        void Awake()
        {
            Instance = this;
            LoadAssetBundle();

            levelObjectsParent = new GameObject("LevelObjects");
            levelObjectsParent.transform.position = Vector3.zero;
        }

        void Start()
        {
            
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                collidingArrow = GetCollidingWithAnArrow();
            }

            if (!Input.GetMouseButton(1) && currentMode == Mode.Building && collidingArrow == Arrow.None && previewObject != null)
            {
                PreviewObject();
            }
            else if (previewObject != null)
            {
                previewObject.SetActive(false);
            }

            if (Input.GetMouseButtonDown(0) && currentMode == Mode.Selection && collidingArrow == Arrow.None)
            {
                if (SelectObjectWithRay(out GameObject obj))
                {
                    SetSelectedObj(obj);
                }
                else
                {
                    SetSelectedObj(null);
                }
            }

            if (Input.GetMouseButton(0) && collidingArrow != Arrow.None)
            {
                MoveObject(collidingArrow);
            }

            if (Input.GetKeyDown(KeyCode.Delete) && currentSelectedObj != null)
            {
                DeleteSelectedObj();
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
                    if (GetInstantiateObjectOriginalName(hit.collider.transform.parent.name) == currentObjectName)
                    {
                        previewObject.transform.position = hit.collider.transform.position;
                        previewObject.transform.rotation = hit.collider.transform.rotation;
                    }
                }

                if (Input.GetMouseButtonDown(0))
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
            obj.name = GetObjectNameToInstantiate(allCategoriesObjects[currentCategoryID][currentObjectName].name);

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

        Arrow GetCollidingWithAnArrow()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity);

            foreach (var hit in hits)
            {
                if (hit.collider.transform.parent != null)
                {
                    if (hit.collider.transform.parent.name == "MoveObjectArrows")
                    {
                        Melon<Core>.Logger.Msg("YES!!");
                        mousePositionWhenArrowClick = Utilities.GetMousePositionInWorld();
                        objPositionWhenArrowClick = currentSelectedObj.transform.position;

                        movementPlane = new Plane(Camera.main.transform.forward, objPositionWhenArrowClick);

                        if (hit.collider.name == "X") return Arrow.X;
                        if (hit.collider.name == "Y") return Arrow.Y;
                        if (hit.collider.name == "Z") return Arrow.Z;
                    }
                }
            }

            return Arrow.None;
        }

        void MoveObject(Arrow direction)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (movementPlane.Raycast(ray, out float distance))
            {
                Vector3 mouseWorldPosition = ray.GetPoint(distance);
                Vector3 displacement = mouseWorldPosition - objPositionWhenArrowClick;

                float movementDistance = Vector3.Dot(displacement, GetAxisDirection(collidingArrow, currentSelectedObj));

                currentSelectedObj.transform.position = objPositionWhenArrowClick + GetAxisDirection(collidingArrow, currentSelectedObj) * (movementDistance * 1f);
            }
        }

        Vector3 GetAxisDirection(Arrow arrow, GameObject obj)
        {
            if (arrow == Arrow.X) return obj.transform.right;
            if (arrow == Arrow.Y) return obj.transform.up;
            if (arrow == Arrow.Z) return obj.transform.forward;

            return Vector3.zero;
        }

        Vector3 GetMouseWorldPosition()
        {
            Vector3 mouseScreenPosition = Input.mousePosition;
            mouseScreenPosition.z = Vector3.Distance(Camera.main.transform.position, currentSelectedObj.transform.localPosition); // Distancia desde la cámara.
            return Camera.main.ScreenToWorldPoint(mouseScreenPosition);
        }

        void SetSelectedObj(GameObject obj)
        {
            if (currentSelectedObj == obj) return;

            moveObjectsArrows.SetActive(false);
            moveObjectsArrows.transform.parent = null;
            moveObjectsArrows.transform.localPosition = Vector3.zero;
            moveObjectsArrows.transform.localRotation = Quaternion.identity;

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

            if (currentSelectedObj != null)
            {
                foreach (var renderer in currentSelectedObj.TryGetComponents<MeshRenderer>())
                {
                    foreach (var material in renderer.materials)
                    {
                        material.SetInt("_ZWrite", 1);
                        material.color = new Color(0f, 1f, 0f, 1f);
                    }
                }

                moveObjectsArrows.SetActive(true);
                moveObjectsArrows.transform.parent = currentSelectedObj.transform;
                moveObjectsArrows.transform.localPosition = Vector3.zero;
                moveObjectsArrows.transform.localRotation = Quaternion.identity;

                EditorUIManager.Instance.SetSelectedObject(obj.name);
            }
        }

        void DeleteSelectedObj()
        {
            Destroy(currentSelectedObj);
            SetSelectedObj(null);
        }

        public string GetObjectNameToInstantiate(string originalName)
        {
            int identifier = 0;
            string name = originalName + " " + identifier;

            while (levelObjectsParent.ExistsChildWithName(name))
            {
                identifier++;
                name = originalName + " " + identifier;
            }

            return name;
        }

        public string GetInstantiateObjectOriginalName(string instantiatedName)
        {
            if (Regex.IsMatch(instantiatedName, @"\d+$"))
            {
                return Regex.Replace(instantiatedName, @"\d+$", "").Trim();
            }

            return instantiatedName;
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

            moveObjectsArrows = Instantiate(bundle.Load<GameObject>("MoveObjectArrows"));
            moveObjectsArrows.name = "MoveObjectArrows";
            moveObjectsArrows.transform.localPosition = Vector3.zero;
            moveObjectsArrows.SetActive(false);

            foreach (var collider in moveObjectsArrows.TryGetComponents<Collider>())
            {
                collider.gameObject.layer = LayerMask.NameToLayer("ARROWS");
            }

            bundle.Unload(false);
        }
    }
}
