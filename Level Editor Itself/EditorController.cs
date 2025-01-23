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

        public string levelName = "test_level";
        public string levelFileNameWithoutExtension = "test_level";

        GameObject gizmosArrows;

        GameObject editorObjectsRootFromBundle;

        // Available categories related variables.
        public List<string> categories = new List<string>();
        public string currentCategory = "";
        public int currentCategoryID = 0;

        // Avaiable objects from all of the categories.
        public List<Dictionary<string, GameObject>> allCategoriesObjectsSorted = new List<Dictionary<string, GameObject>>();
        public Dictionary<string, GameObject> allCategoriesObjects = new Dictionary<string, GameObject>();
        public string currentObjectToBuildName = "";
        GameObject currentObjectToBuild;
        GameObject previewObjectToBuildObj = null;

        // Related to current selected object for level building.
        public GameObject levelObjectsParent;
        public GameObject currentSelectedObj;
        LE_Object currentSelectedObjComponent;
        // When there's just one object selected, that object in in the currentSelectedObj variable.
        // But when there are multiple objects selected, this list contains em and "currentSelectedObj" is "multipleSelectedObjsParent".
        public List<GameObject> currentSelectedObjects = new List<GameObject>();
        public bool multipleObjectsSelected = false;
        public GameObject multipleSelectedObjsParent;
        bool isDuplicatingObj = false;

        // Selected mode.
        public enum Mode { Building, Selection, Deletion }
        public Mode currentMode = Mode.Building;

        // Gizmos arrows to move objects.
        enum GizmosArrow { None, X, Y, Z }
        GizmosArrow collidingArrow;
        Vector3 objPositionWhenArrowClick;
        Vector3 offsetObjPositionAndMosueWhenClick;
        Plane movementPlane;

        GameObject snapToGridCube;
        bool startSnapToGridWithCurrentSelectedObj = false;

        public bool isEditorPaused = false;

        void Awake()
        {
            Instance = this;
            LoadAssetBundle();

            levelObjectsParent = new GameObject("LevelObjects");
            levelObjectsParent.transform.position = Vector3.zero;

            multipleSelectedObjsParent = new GameObject("MultipleSelectedObjsParent");
            multipleSelectedObjsParent.transform.position = Vector3.zero;
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

            if (Input.GetKey(KeyCode.V) && currentSelectedObj != null)
            {
                snapToGridCube.SetActive(true);
                gizmosArrows.SetActive(false);

                if (Input.GetMouseButtonDown(0))
                {
                    if (IsClickingSnapToGridCube())
                    {
                        startSnapToGridWithCurrentSelectedObj = true;
                    }
                }
                if (Input.GetMouseButton(0) && startSnapToGridWithCurrentSelectedObj)
                {
                    AlignSelectedObjectToGrid();
                }
                if (Input.GetMouseButtonUp(0))
                {
                    startSnapToGridWithCurrentSelectedObj = false;
                }
            }
            else
            {
                snapToGridCube.SetActive(false);

                if (currentSelectedObj != null)
                {
                    gizmosArrows.SetActive(true);
                }
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

            // If click and it's on deletion and it's NOT clicking a gizmos arrow AND the mouse isn't over a UI element..
            if (Input.GetMouseButtonDown(0) && currentMode == Mode.Deletion && collidingArrow == GizmosArrow.None && !Utilities.IsMouseOverUIElement())
            {
                // If it's clicking an object.
                if (CanSelectObjectWithRay(out GameObject obj))
                {
                    // Yep, I'm lazy and Ik this isn't the best way of doing it... but it works I think :D
                    SetSelectedObj(obj);
                    DeleteSelectedObj();
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
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                ChangeMode(Mode.Deletion);
            }

            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.S))
            {
                LevelData.SaveLevelData(levelName, levelFileNameWithoutExtension);
                EditorUIManager.Instance.PlaySavingLevelLabel();
            }

            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.D))
            {
                DuplicateSelectedObject();
            }

            ManageObjectRotationShortcuts();
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
                case Mode.Deletion:
                    EditorUIManager.Instance.categoryButtons.ForEach(button => button.SetActive(false));
                    EditorUIManager.Instance.currentCategoryBG.SetActive(false);
                    break;
            }

            EditorUIManager.Instance.SetCurrentModeLabelText(currentMode);
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
            LE_Object.ObjectType? objToBuildType = LE_Object.ConvertNameToObjectType(objToBuildName);

            // First, check if the object already has a specific trigger set for it, just to make sure it doesn't pick up the global one ;)
            foreach (GameObject rootChild in triggerRootObj.GetChilds())
            {
                List<LE_Object.ObjectType?> availableObjectsForTriggerInCurrentChild = rootChild.name.Split('|').Select(x => LE_Object.ConvertNameToObjectType(x.Trim())).ToList();

                if (availableObjectsForTriggerInCurrentChild.Contains(objToBuildType))
                {
                    existsSpecificTriggerForThisObjToBuild = true;
                }
            }

            // Then, check if the object CAN use that trigger the player is clicking.
            List<LE_Object.ObjectType?> availableObjectsForTrigger = triggerObj.transform.parent.name.Split('|').Select(x => LE_Object.ConvertNameToObjectType(x.Trim())).ToList();
            if (availableObjectsForTrigger.Contains(objToBuildType))
            {
                return true;
            }

            if (triggerObj.transform.parent.name == "Global" && !existsSpecificTriggerForThisObjToBuild) return true;

            return false;
        }

        void MoveObject(GizmosArrow direction)
        {
            // Get the ray from the camera.
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // If the ray can collide with the "invisible" plane.
            if (movementPlane.Raycast(ray, out float distance))
            {
                // IT WORKS, DON'T EVEN DARE TO TOUCH THIS EVER AGAIN!

                Vector3 hitWorldPosition = ray.GetPoint(distance);
                Vector3 displacement = hitWorldPosition - objPositionWhenArrowClick;

                float movementDistance = Vector3.Dot(displacement, GetAxisDirection(collidingArrow, currentSelectedObj));

                Vector3 realOffset = RotatePositionAroundPivot(offsetObjPositionAndMosueWhenClick + objPositionWhenArrowClick, objPositionWhenArrowClick, currentSelectedObj.transform.rotation) - objPositionWhenArrowClick;

                currentSelectedObj.transform.localPosition = objPositionWhenArrowClick + (GetAxisDirection(collidingArrow, currentSelectedObj) * movementDistance) + realOffset;
            }
        }

        void DeleteSelectedObj()
        {
            if (multipleObjectsSelected)
            {
                foreach (var obj in currentSelectedObj.GetChilds())
                {
                    if (obj.name == "MoveObjectArrows" || obj.name == "SnapToGridCube") continue;

                    Destroy(obj);
                }
            }
            else
            {
                Destroy(currentSelectedObj);
            }
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
                    allCategoriesObjects.Add(obj.name, obj);
                }

                allCategoriesObjectsSorted.Add(categoryObjects);
            }

            gizmosArrows = Instantiate(bundle.Load<GameObject>("MoveObjectArrows"));
            gizmosArrows.name = "MoveObjectArrows";
            gizmosArrows.transform.localPosition = Vector3.zero;
            gizmosArrows.SetActive(false);

            foreach (var collider in gizmosArrows.TryGetComponents<Collider>())
            {
                collider.gameObject.layer = LayerMask.NameToLayer("ARROWS");
            }

            snapToGridCube = Instantiate(bundle.Load<GameObject>("SnapToGridCube"));
            snapToGridCube.name = "SnapToGridCube";
            snapToGridCube.transform.localPosition = Vector3.zero;
            snapToGridCube.SetActive(false);

            bundle.Unload(false);
        }

        public void SetSelectedObj(GameObject obj)
        {
            if (currentSelectedObj == obj) return;

            gizmosArrows.SetActive(false);
            gizmosArrows.transform.parent = null;
            gizmosArrows.transform.localPosition = Vector3.zero;
            gizmosArrows.transform.localRotation = Quaternion.identity;

            snapToGridCube.transform.parent = null;
            snapToGridCube.transform.localPosition = Vector3.zero;
            snapToGridCube.transform.localRotation = Quaternion.identity;

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

            // If is pressing control, the object isn't null and it's not the selecteed objects parent and it's NOT duplicating objects at this moment:
            if ((Input.GetKey(KeyCode.LeftControl) || startSnapToGridWithCurrentSelectedObj) && obj != null && obj != multipleSelectedObjsParent && !isDuplicatingObj)
            {
                // If there was another object selected before, add it to the selected objects list too.
                if (currentSelectedObj != null && currentSelectedObj != multipleSelectedObjsParent)
                {
                    // But only if it hasn't been selected yet.
                    if (!currentSelectedObjects.Contains(currentSelectedObj)) currentSelectedObjects.Add(currentSelectedObj);
                }
                // And add the most recent now, ofc lol (but only if it hasn't been selected yet).
                if(!currentSelectedObjects.Contains(obj)) currentSelectedObjects.Add(obj);

                // Set the bool.
                if (currentSelectedObjects.Count > 1) multipleObjectsSelected = true;
                else multipleObjectsSelected = false;

                // Get the center position of the whole objects.
                Vector3 centeredPosition = Vector3.zero;
                foreach (var objInList in currentSelectedObjects) { centeredPosition += objInList.transform.position; }
                centeredPosition /= currentSelectedObjects.Count;

                // Remove the parent from the selected objects, set the new parent position and put the parent in the objects again.
                currentSelectedObjects.ForEach(x => x.transform.parent = levelObjectsParent.transform);
                multipleSelectedObjsParent.transform.position = centeredPosition;
                multipleSelectedObjsParent.transform.rotation = Quaternion.identity;
                currentSelectedObjects.ForEach(x => x.transform.parent = multipleSelectedObjsParent.transform);

                // The "main" selected object now is the parent of the selected objects.
                currentSelectedObj = multipleSelectedObjsParent;
            }
            else
            {
                // If don't press Ctrl AND the obj trying to select isn't the selected objects parent, just remvoe the parent for all of the objects and clear the list.
                if (obj != multipleSelectedObjsParent)
                {
                    currentSelectedObjects.ForEach(x => x.transform.parent = levelObjectsParent.transform);
                    currentSelectedObjects.Clear();
                    multipleObjectsSelected = false; // Set the bool again.
                }
                else // Otherwise, if it IS... set this bool again to true.
                {
                    multipleObjectsSelected = true;
                }

                // Work as always (the normal selection system lol).
                currentSelectedObj = obj;
                if (currentSelectedObj != null && currentSelectedObj != multipleSelectedObjsParent)
                {
                    currentSelectedObjComponent = currentSelectedObj.GetComponent<LE_Object>();
                }
            }

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

                snapToGridCube.transform.parent = currentSelectedObj.transform;
                snapToGridCube.transform.localPosition = Vector3.zero;
                snapToGridCube.transform.localRotation = Quaternion.identity;

                if (multipleObjectsSelected)
                {
                    EditorUIManager.Instance.SetMultipleObjectsSelected();
                }
                else
                {
                    EditorUIManager.Instance.SetSelectedObject(obj.name);
                }
            }
            else
            {
                EditorUIManager.Instance.SetSelectedObjPanelAsNone();
            }
        }

        public GameObject PlaceObject(string objName, Vector3 position, Vector3 eulerAngles, bool setAsSelected = true)
        {
            if (objName == "ProvisionalLight")
            {
                return Melon<Core>.Instance.CreateProvicionalLight(position, eulerAngles);
            }

            GameObject template = allCategoriesObjects[objName];
            GameObject obj = Instantiate(template, levelObjectsParent.transform);

            obj.transform.localPosition = position;
            obj.transform.localEulerAngles = eulerAngles;

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

            return obj;
        }

        void DuplicateSelectedObject()
        {
            if (currentSelectedObj == null) return;

            if (multipleObjectsSelected)
            {
                // Create a copy of every object inside of the selected objects list.
                List<GameObject> newSelectedObjectsList = new List<GameObject>();
                foreach (var obj in currentSelectedObjects)
                {
                    LE_Object objComponent = obj.GetComponent<LE_Object>();
                    newSelectedObjectsList.Add(PlaceObject(objComponent.objectOriginalName, objComponent.transform.position, objComponent.transform.eulerAngles, false));
                }

                // Set the selected object as null so all of the "old" selected objects are deselected. Also remove them from the selected objects parent.
                SetSelectedObj(null);
                currentSelectedObjects.ForEach(x => x.transform.parent = levelObjectsParent.transform);
                currentSelectedObjects.Clear(); // Clear the list, just in case.
                currentSelectedObjects = new List<GameObject>(newSelectedObjectsList); // Replace the list with the new one with the copied objects.
                currentSelectedObjects.ForEach(x => x.transform.parent = multipleSelectedObjsParent.transform); // Set the parents on it.
                SetSelectedObj(multipleSelectedObjsParent); // Select the selected objects parent again.
            }
            else
            {
                isDuplicatingObj = true;
                LE_Object objComponent = currentSelectedObj.GetComponent<LE_Object>();
                PlaceObject(objComponent.objectOriginalName, objComponent.transform.localPosition, objComponent.transform.localEulerAngles);
                isDuplicatingObj = false;
            }
        }

        void ManageObjectRotationShortcuts()
        {
            if (currentSelectedObj == null) return;

            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.R))
            {
                currentSelectedObj.transform.localRotation = Quaternion.identity;
            }
            else if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.R))
            {
                currentSelectedObj.transform.Rotate(15f, 0f, 0f);
            }
            else if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.R))
            {
                currentSelectedObj.transform.Rotate(0f, 0f, 15f);
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                currentSelectedObj.transform.Rotate(0f, 15f, 0f);
            }
        }

        void AlignSelectedObjectToGrid()
        {
            // Get all of the possible rays and short them, since Unity doesn't short them by defualt.
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, -1, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (hit1, hit2) => hit1.distance.CompareTo(hit2.distance));

            // If it hits at least one object.
            if (hits.Length > 0)
            {
                foreach (var hit in hits)
                {
                    // If the hit is an static pos trigger (snap trigger)...
                    if (hit.collider.gameObject.name.StartsWith("StaticPos"))
                    {
                        // Only if the snap trigger is NOT from the current selected object.
                        bool isFromTheSameObject = false;
                        if (multipleObjectsSelected)
                        {
                            foreach (var obj in currentSelectedObjects)
                            {
                                if (hit.collider.transform.parent.parent.gameObject == obj) isFromTheSameObject = true;
                            }
                        }
                        else
                        {
                            if (hit.collider.transform.parent.parent.gameObject == currentSelectedObj) isFromTheSameObject = true;
                        }

                        if (!isFromTheSameObject)
                        {
                            // AND ONLY (lol) if that trigger actually CAN be used with the selected object.
                            if (CanUseThatSnapToGridTrigger(currentSelectedObjComponent.objectOriginalName, hit.collider.gameObject))
                            {
                                currentSelectedObj.transform.position = hit.collider.transform.position;
                                currentSelectedObj.transform.rotation = hit.collider.transform.rotation;

                                return;
                            }
                        }
                    }
                    // If the hitten object is the current selected one, keep iterating...
                    else if (hit.collider.transform.parent.gameObject == currentSelectedObj)
                    {
                        continue;
                    }
                    // Otherwise, return and do nothing.
                    else
                    {
                        return;
                    }
                }
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
            currentObjectToBuild = allCategoriesObjectsSorted[currentCategoryID][currentObjectToBuildName];

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

            // Loop foreach all of the collisions of the ray.
            foreach (var hit in hits)
            {
                if (hit.collider.transform.parent != null)
                {
                    // If one of them are the move object gizmos...
                    if (hit.collider.transform.parent.name == "MoveObjectArrows")
                    {
                        // Save the position of the object from the first time we clicked.
                        objPositionWhenArrowClick = currentSelectedObj.transform.localPosition;

                        // Create the panel with the rigt normals.
                        if (hit.collider.name == "X" || hit.collider.name == "Z")
                        {
                            movementPlane = new Plane(currentSelectedObj.transform.up, objPositionWhenArrowClick);
                        }
                        else if (hit.collider.name == "Y")
                        {
                            Vector3 cameraPosition = Camera.main.transform.position;
                            Vector3 directionToCamera = cameraPosition - objPositionWhenArrowClick;
                            Vector3 planeNormal = new Vector3(directionToCamera.normalized.x, 0f, directionToCamera.normalized.z);

                            movementPlane = new Plane(planeNormal, objPositionWhenArrowClick);
                        }

                        // Then get the right offset of the arrows.
                        offsetObjPositionAndMosueWhenClick = Vector3.zero;
                        if (movementPlane.Raycast(ray, out float enter))
                        {
                            Vector3 collisionOnPlane = ray.GetPoint(enter);
                            collisionOnPlane = RotatePositionAroundPivot(collisionOnPlane, objPositionWhenArrowClick, Quaternion.Inverse(currentSelectedObj.transform.rotation));

                            if (hit.collider.name == "X") offsetObjPositionAndMosueWhenClick.x = objPositionWhenArrowClick.x - collisionOnPlane.x;
                            if (hit.collider.name == "Y") offsetObjPositionAndMosueWhenClick.y = objPositionWhenArrowClick.y - collisionOnPlane.y;
                            if (hit.collider.name == "Z") offsetObjPositionAndMosueWhenClick.z = objPositionWhenArrowClick.z - collisionOnPlane.z;
                        }

                        // Finally, return the final result of the gizmos arrow we touched.
                        if (hit.collider.name == "X") return GizmosArrow.X;
                        if (hit.collider.name == "Y") return GizmosArrow.Y;
                        if (hit.collider.name == "Z") return GizmosArrow.Z;
                    }
                }
            }

            return GizmosArrow.None;
        }

        bool IsClickingSnapToGridCube()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity);

            // Loop foreach all of the collisions of the ray.
            foreach (var hit in hits)
            {
                if (hit.collider.gameObject.name == "SnapToGridCube") return true;
            }

            return false;
        }

        Vector3 GetAxisDirection(GizmosArrow arrow, GameObject obj)
        {
            if (arrow == GizmosArrow.X) return obj.transform.right;
            if (arrow == GizmosArrow.Y) return obj.transform.up;
            if (arrow == GizmosArrow.Z) return obj.transform.forward;

            return Vector3.zero;
        }

        Vector3 RotatePositionAroundPivot(Vector3 position, Vector3 pivot, Quaternion rotation)
        {
            // I DON'T WANNA TOUCH THIS FUCKING CODE IN MY LIFE!!!

            Vector3 positionInCenterOfWorld = position - pivot;
            Vector3 rotatedPosition = rotation * positionInCenterOfWorld;
            Vector3 rotatedPositionWithOriginalPivot = rotatedPosition + pivot;

            return rotatedPositionWithOriginalPivot;
        }
        #endregion
    }
}
