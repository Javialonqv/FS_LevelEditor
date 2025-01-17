using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MelonLoader;
using System.Text.RegularExpressions;
using UnityEngine.SceneManagement;
using Il2Cpp;

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
        GameObject currentObjectToBuild;
        GameObject previewObjectToBuildObj = null;

        // Related to current selected object for level building.
        public GameObject levelObjectsParent;
        public GameObject currentSelectedObj;

        // Selected mode.
        public enum Mode { Building, Selection, Deletion }
        public Mode currentMode = Mode.Building;

        // Gizmos arrows to move objects.
        enum GizmosArrow { None, X, Y, Z }
        GizmosArrow collidingArrow;
        Vector3 objPositionWhenArrowClick;
        Vector3 offsetObjPositionAndMosueWhenClick;
        Plane movementPlane;

        public bool isEditorPaused = false;

        void Awake()
        {
            Instance = this;
            LoadAssetBundle();

            levelObjectsParent = new GameObject("LevelObjects");
            levelObjectsParent.transform.position = Vector3.zero;
        }

        void Start()
        {
            // Disable occlusion culling.
            Camera.main.useOcclusionCulling = false;
        }

        void Update()
        {
            // Shortcut for pausing LE.
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (!isEditorPaused)
                {
                    EditorUIManager.Instance.ShowPause();
                }
                else
                {
                    EditorUIManager.Instance.Resume();
                }
            }

            if (isEditorPaused) return;

            // When click, check if it's clicking a gizmos arrow.
            if (Input.GetMouseButtonDown(0))
            {
                collidingArrow = GetCollidingWithAnArrow();
            }

            // If it's not rotating camera, and it's building, and it's NOT clicking a gizmos arrow and there's actually a selected object to build AND the mouse isn't over
            // a UI element... preview that object.
            if (!Input.GetMouseButton(1) && currentMode == Mode.Building && collidingArrow == GizmosArrow.None && previewObjectToBuildObj != null && !Utilities.IsMouseOverUIElement())
            {
                PreviewObject();
            }
            // If not, at least if the preview object isn't null, disable it.
            else if (previewObjectToBuildObj != null)
            {
                previewObjectToBuildObj.SetActive(false);
            }

            // If click and it's on selection and it's NOT clicking a gizmos arrow AND the mouse isn't over a UI element...
            if (Input.GetMouseButtonDown(0) && currentMode == Mode.Selection && collidingArrow == GizmosArrow.None && !Utilities.IsMouseOverUIElement())
            {
                // If it's selecting an object, well, set it as the selected one, otherwise, deselect the last selected object if there's one.
                if (CanSelectObjectWithRay(out GameObject obj))
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

        // For now, this method only disables and enables the "building" UI, with the objects available to build.
        void ChangeMode(Mode mode)
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

        void PreviewObject()
        {
            // Get all of the possible rays and short them, since Unity doesn't short them by defualt.
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, -1, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (hit1, hit2) => hit1.distance.CompareTo(hit2.distance));

            // If it hits at least one object.
            if (hits.Length > 0)
            {
                // Detect if hte first ray hit is colliding with a snap trigger.
                bool firstRayIsATrigger = hits[0].collider.gameObject.name.StartsWith("StaticPos");

                foreach (var hit in hits)
                {
                    bool snapNow = false;
                    bool breakAtTheEnd = false;

                    // If the hit is an static pos trigger (snap trigger)...
                    if (hit.collider.gameObject.name.StartsWith("StaticPos"))
                    {
                        // Set the preview position to a snapped to grid position only if is pressing the ctrl key or if it's the only object it's colliding to.
                        if (Input.GetKey(KeyCode.LeftControl) || Utilities.ItsTheOnlyHittedObjectByRaycast(ray, Mathf.Infinity, hit.collider.gameObject))
                        {
                            // Also, only snap if the hitten object trigger CAN be used with the current selected object.
                            if (CanUseThatSnapToGridTrigger(currentObjectToBuildName, hit.collider.transform.gameObject))
                            {
                                previewObjectToBuildObj.SetActive(true);
                                previewObjectToBuildObj.transform.position = hit.collider.transform.position;
                                previewObjectToBuildObj.transform.rotation = hit.collider.transform.rotation;

                                // We don't need anything else, if the user wants to use snap to grid and this is the correct trigger to use, use this and break the loop.
                                snapNow = true;
                                breakAtTheEnd = true;

                            }
                            // If the user actually wants to use snap, but this isn't the right trigger BUT the first one was a trigger, start looping again till find the correct one.
                            else if (firstRayIsATrigger)
                            {
                                continue;
                            }
                        }
                    }
                    else // If we couldn't find any compatible triggers, just use the default behaviour and break the loop at the end, since we only care about the trigger that are
                         // BEFORE the object itself.
                    {
                        breakAtTheEnd = true;
                    }

                    // If no correct trigger was found, use the default behaviour.
                    if (!snapNow)
                    {
                        // Set the preview object posiiton to the hit point.
                        previewObjectToBuildObj.SetActive(true);
                        previewObjectToBuildObj.transform.position = hit.point;
                        previewObjectToBuildObj.transform.up = hit.normal;
                        Melon<Core>.Logger.Msg("NO TRIGGER FOUND");
                    }

                    // If press left click while previewing, place the object :)
                    if (Input.GetMouseButtonDown(0))
                    {
                        InstanceObjectInThePreviewObjectPos();
                        break;
                    }

                    if (breakAtTheEnd) break;
                }
            }
            else // Disable the preview object if it's not hitting nothing in the world space.
            {
                previewObjectToBuildObj.SetActive(false);
            }
        }

        void InstanceObjectInThePreviewObjectPos()
        {
            GameObject obj = Instantiate(previewObjectToBuildObj, levelObjectsParent.transform);
            LE_Object.AddComponentToObject(obj, currentObjectToBuildName);

            foreach (var collider in obj.TryGetComponents<Collider>())
            {
                collider.enabled = true;
            }
            foreach (var renderer in obj.TryGetComponents<MeshRenderer>())
            {
                foreach (var material in renderer.materials)
                {
                    material.color = new Color(1f, 1f, 1f, 1f);
                }
            }

            SetSelectedObj(obj);
        }

        bool CanUseThatSnapToGridTrigger(string objToBuildName, GameObject triggerObj)
        {
            GameObject triggerRootObj = triggerObj.transform.parent.parent.gameObject;
            bool existsSpecificTriggerForThisObjToBuild = false;

            // First, check if the object already has a specific trigger set for it, just to make sure it doesn't pick up the global one ;)
            foreach (GameObject rootChild in triggerRootObj.GetChilds())
            {
                List<string> availableObjectsForTriggerInCurrentChild = rootChild.name.Split('|').ToList();
                for (int i = 0; i < availableObjectsForTriggerInCurrentChild.Count; i++)
                {
                    availableObjectsForTriggerInCurrentChild[i] = availableObjectsForTriggerInCurrentChild[i].Trim();
                }

                if (availableObjectsForTriggerInCurrentChild.Contains(objToBuildName))
                {
                    existsSpecificTriggerForThisObjToBuild = true;
                }
            }

            // Then, check if the object CAN use that trigger the player is clicking.
            List<string> availableObjectsForTrigger = triggerObj.transform.parent.name.Split('|').ToList();
            for (int i = 0; i < availableObjectsForTrigger.Count; i++)
            {
                availableObjectsForTrigger[i] = availableObjectsForTrigger[i].Trim();
            }
            if (availableObjectsForTrigger.Contains(objToBuildName))
            {
                return true;
            }

            if (triggerObj.transform.parent.name == "Global" && !existsSpecificTriggerForThisObjToBuild) return true;

            return false;
        }

        void MoveObject(GizmosArrow direction)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (movementPlane.Raycast(ray, out float distance))
            {
                Vector3 mouseWorldPosition = ray.GetPoint(distance);
                Vector3 displacement = mouseWorldPosition - objPositionWhenArrowClick;

                float movementDistance = Vector3.Dot(displacement, GetAxisDirection(collidingArrow, currentSelectedObj));

                currentSelectedObj.transform.position = objPositionWhenArrowClick + GetAxisDirection(collidingArrow, currentSelectedObj) * (movementDistance * 1f) + offsetObjPositionAndMosueWhenClick;
            }
        }

        void DeleteSelectedObj()
        {
            Destroy(currentSelectedObj);
            SetSelectedObj(null);
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

        public void SetSelectedObj(GameObject obj)
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
                        material.color = new Color(0f, 1f, 0f, 1f);
                    }
                }

                gizmosArrows.SetActive(true);
                gizmosArrows.transform.parent = currentSelectedObj.transform;
                gizmosArrows.transform.localPosition = Vector3.zero;
                gizmosArrows.transform.localRotation = Quaternion.identity;

                EditorUIManager.Instance.SetSelectedObject(obj.name);
            }
            else
            {
                EditorUIManager.Instance.SetSelectedObjPanelAsNone();
            }
        }

        public void PlaceObject(string objName, Vector3 position, Quaternion rotation, bool setAsSelected = true)
        {
            GameObject template = allCategoriesObjects[currentCategoryID][objName];
            GameObject obj = Instantiate(template, levelObjectsParent.transform);

            obj.transform.localPosition = position;
            obj.transform.localRotation = rotation;

            LE_Object.AddComponentToObject(obj, objName);

            foreach (var renderer in obj.TryGetComponents<MeshRenderer>())
            {
                foreach (var material in renderer.materials)
                {
                    material.color = new Color(1f, 1f, 1f, 1f);
                }
            }

            obj.SetActive(true);

            if (setAsSelected)
            {
                SetSelectedObj(obj);
            }
        }

        #region Methods called from UI buttons
        public void ChangeCategory(int categoryID)
        {
            if (currentCategoryID == categoryID) return;

            currentCategoryID = categoryID;
            currentCategory = categories[currentCategoryID];
            EditorUIManager.Instance.SetupCurrentCategoryButtons();
        }

        public void SelectObjectToBuild(string objName)
        {
            // Do nothing if trying to select the same object as the last selected one.
            if (currentObjectToBuildName == objName) return;

            if (objName == "None")
            {
                currentObjectToBuildName = "";
                currentObjectToBuild = null;
                Destroy(previewObjectToBuildObj);
                return;
            }

            currentObjectToBuildName = objName;
            currentObjectToBuild = allCategoriesObjects[currentCategoryID][currentObjectToBuildName];

            // Destroy the preview object and create another one with the mew selected model.
            Destroy(previewObjectToBuildObj);
            previewObjectToBuildObj = Instantiate(currentObjectToBuild);

            // Disable collision of the preview object.
            foreach (var collider in previewObjectToBuildObj.TryGetComponents<Collider>())
            {
                collider.enabled = false;
            }
            // Also change it's color to blue.
            foreach (var renderer in previewObjectToBuildObj.TryGetComponents<MeshRenderer>())
            {
                foreach (var material in renderer.materials)
                {
                    material.color = new Color(0f, 0.666f, 0.894f, 1f);
                }
            }
        }
        #endregion

        #region Some Utilities
        /// <summary>
        /// Selects an object with a ray from the camera and current mouse position, to see if an object can be detected.
        /// </summary>
        /// <param name="obj">If there's an object, the instance of that object, otherwise, null.</param>
        /// <returns>A bool that represents if there was an object there.</returns>
        bool CanSelectObjectWithRay(out GameObject obj)
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

        /// <summary>
        /// Returns if a ray from the mouse position to real world is colliding with a gizmos arrow of an object.
        /// </summary>
        /// <returns></returns>
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

                        offsetObjPositionAndMosueWhenClick = Vector3.zero;
                        if (hit.collider.name == "X") offsetObjPositionAndMosueWhenClick.x = objPositionWhenArrowClick.x - hit.point.x;
                        if (hit.collider.name == "Y") offsetObjPositionAndMosueWhenClick.y = objPositionWhenArrowClick.y - hit.point.y;
                        if (hit.collider.name == "Z") offsetObjPositionAndMosueWhenClick.z = objPositionWhenArrowClick.z - hit.point.z;

                        movementPlane = new Plane(Camera.main.transform.forward, objPositionWhenArrowClick);

                        if (hit.collider.name == "X") return GizmosArrow.X;
                        if (hit.collider.name == "Y") return GizmosArrow.Y;
                        if (hit.collider.name == "Z") return GizmosArrow.Z;
                    }
                }
            }

            return GizmosArrow.None;
        }

        Vector3 GetAxisDirection(GizmosArrow arrow, GameObject obj)
        {
            if (arrow == GizmosArrow.X) return obj.transform.right;
            if (arrow == GizmosArrow.Y) return obj.transform.up;
            if (arrow == GizmosArrow.Z) return obj.transform.forward;

            return Vector3.zero;
        }
        #endregion
    }
}
