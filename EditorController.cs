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

        GameObject gizmosArrows;

        GameObject editorObjectsRootFromBundle;

        // Available categories related variables.
        public List<string> categories = new List<string>();
        public string currentCategory = "";
        public int currentCategoryID = 0;

        // Avaiable objects from all of the categories.
        public List<Dictionary<string, GameObject>> allCategoriesObjects = new List<Dictionary<string, GameObject>>();
        public string currentObjectToBuildName = "";

        // Related to current selected object for level building.
        public GameObject levelObjectsParent;
        GameObject previewObjectToBuild = null;
        public GameObject currentSelectedObj;

        // Selected mode.
        public enum Mode { Building, Selection, Deletion }
        public Mode currentMode = Mode.Building;

        // Gizmos arrows to move objects.
        enum GizmosArrow { None, X, Y, Z }
        GizmosArrow collidingArrow;
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
            // When click, check if it's clicking a gizmos arrow.
            if (Input.GetMouseButtonDown(0))
            {
                collidingArrow = GetCollidingWithAnArrow();
            }

            // If it's not rotating camera, and it's building, and it's NOT clicking a gizmos arrow and there's actually a selected object to build, preview that object.
            if (!Input.GetMouseButton(1) && currentMode == Mode.Building && collidingArrow == GizmosArrow.None && previewObjectToBuild != null)
            {
                PreviewObject();
            }
            // If not, at least if the preview object isn't null, disable it.
            else if (previewObjectToBuild != null)
            {
                previewObjectToBuild.SetActive(false);
            }

            // If click and it's on selection and it's NOT clicking a gizmos arrow.
            if (Input.GetMouseButtonDown(0) && currentMode == Mode.Selection && collidingArrow == GizmosArrow.None)
            {
                // If it's selecting an object, well, set it as the selected one, otherwise, deselect the last selected object if there's one.
                if (SelectObjectWithRay(out GameObject obj))
                {
                    SetSelectedObj(obj);
                }
                else
                {
                    SetSelectedObj(null);
                }
            }

            // If it's clicking a gizmos arrow.
            if (Input.GetMouseButton(0) && collidingArrow != GizmosArrow.None)
            {
                // Move the object.
                MoveObject(collidingArrow);
            }

            // If press the Delete key and there's a selected object, delete it.
            if (Input.GetKeyDown(KeyCode.Delete) && currentSelectedObj != null)
            {
                DeleteSelectedObj();
            }
            
            // Shortcuts for changing between editor modes.
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
            if (currentObjectToBuildName == objName) return;

            currentObjectToBuildName = objName;
            Melon<Core>.Logger.Msg(objName);

            Destroy(previewObjectToBuild);
            previewObjectToBuild = Instantiate(allCategoriesObjects[currentCategoryID][currentObjectToBuildName]);

            foreach (var collider in previewObjectToBuild.TryGetComponents<Collider>())
            {
                collider.enabled = false;
            }
            foreach (var renderer in previewObjectToBuild.TryGetComponents<MeshRenderer>())
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
                previewObjectToBuild.SetActive(true);
                previewObjectToBuild.transform.position = hit.point;
                previewObjectToBuild.transform.up = hit.normal;

                if (hit.collider.gameObject.name.StartsWith("StaticPos"))
                {
                    if (GetInstantiateObjectOriginalName(hit.collider.transform.parent.name) == currentObjectToBuildName)
                    {
                        previewObjectToBuild.transform.position = hit.collider.transform.position;
                        previewObjectToBuild.transform.rotation = hit.collider.transform.rotation;
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
                previewObjectToBuild.SetActive(false);
            }
        }

        void PlaceObject()
        {
            GameObject obj = Instantiate(previewObjectToBuild, levelObjectsParent.transform);
            obj.name = GetObjectNameToInstantiate(allCategoriesObjects[currentCategoryID][currentObjectToBuildName].name);

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

        GizmosArrow GetCollidingWithAnArrow()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity);

            foreach (var hit in hits)
            {
                if (hit.collider.transform.parent != null)
                {
                    if (hit.collider.transform.parent.name == "MoveObjectArrows")
                    {
                        objPositionWhenArrowClick = currentSelectedObj.transform.position;

                        movementPlane = new Plane(Camera.main.transform.forward, objPositionWhenArrowClick);

                        if (hit.collider.name == "X") return GizmosArrow.X;
                        if (hit.collider.name == "Y") return GizmosArrow.Y;
                        if (hit.collider.name == "Z") return GizmosArrow.Z;
                    }
                }
            }

            return GizmosArrow.None;
        }

        void MoveObject(GizmosArrow direction)
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

        Vector3 GetAxisDirection(GizmosArrow arrow, GameObject obj)
        {
            if (arrow == GizmosArrow.X) return obj.transform.right;
            if (arrow == GizmosArrow.Y) return obj.transform.up;
            if (arrow == GizmosArrow.Z) return obj.transform.forward;

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

            gizmosArrows.SetActive(false);
            gizmosArrows.transform.parent = null;
            gizmosArrows.transform.localPosition = Vector3.zero;
            gizmosArrows.transform.localRotation = Quaternion.identity;

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

                gizmosArrows.SetActive(true);
                gizmosArrows.transform.parent = currentSelectedObj.transform;
                gizmosArrows.transform.localPosition = Vector3.zero;
                gizmosArrows.transform.localRotation = Quaternion.identity;

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

            editorObjectsRootFromBundle = bundle.Load<GameObject>("LevelObjectsRoot");
            editorObjectsRootFromBundle.hideFlags = HideFlags.DontUnloadUnusedAsset;

            foreach (var child in editorObjectsRootFromBundle.GetChilds())
            {
                categories.Add(child.name);
            }

            currentCategory = categories[0];
            currentCategoryID = 0;

            foreach (var categoryObj in editorObjectsRootFromBundle.GetChilds())
            {
                Dictionary<string, GameObject> categoryObjects = new();

                foreach (var obj in categoryObj.GetChilds())
                {
                    categoryObjects.Add(obj.name, obj);
                }

                allCategoriesObjects.Add(categoryObjects);
            }

            gizmosArrows = Instantiate(bundle.Load<GameObject>("MoveObjectArrows"));
            gizmosArrows.name = "MoveObjectArrows";
            gizmosArrows.transform.localPosition = Vector3.zero;
            gizmosArrows.SetActive(false);

            foreach (var collider in gizmosArrows.TryGetComponents<Collider>())
            {
                collider.gameObject.layer = LayerMask.NameToLayer("ARROWS");
            }

            bundle.Unload(false);
        }
    }
}
