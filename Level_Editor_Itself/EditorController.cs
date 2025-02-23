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
using System.Collections;
using Il2CppLunarCatsStudio.SuperCombiner;

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
        GameObject[] otherObjectsFromBundle;
        public string currentObjectToBuildName = "";
        GameObject currentObjectToBuild;
        GameObject previewObjectToBuildObj = null;
        Vector3? lastHittenNormalByPreviewRay = null;

        // Related to current selected object for level building.
        public GameObject levelObjectsParent;
        public GameObject currentSelectedObj;
        public LE_Object currentSelectedObjComponent;
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
        bool isCurrentlyMovingAnObject = false;
        bool globalGizmosArrowsEnabled = false;

        GameObject snapToGridCube;
        bool startSnapToGridWithCurrentSelectedObj = false;

        List<LEAction> actionsMade = new List<LEAction>();
        LEAction currentExecutingAction;

        public bool isEditorPaused = false;
        public bool levelHasBeenModified = false;

        public List<LE_Object> currentInstantiatedObjects = new List<LE_Object>();

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
            //Logger.DebugLog($"Is over UI element: {Utilities.IsMouseOverUIElement()}. Is input field selected: {Utilities.theresAnInputFieldSelected}");

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

            // If pressing F key and it's not typing in an input field.
            if (Input.GetKey(KeyCode.F) && currentSelectedObj != null && !Utilities.theresAnInputFieldSelected)
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

            // If click and it's on selection and it's NOT clicking a gizmos arrow AND the mouse isn't over a UI element AAAND it's not using snap to grid while edition right now...
            if (Input.GetMouseButtonDown(0) && currentMode == Mode.Selection && collidingArrow == GizmosArrow.None && !Utilities.IsMouseOverUIElement() && !startSnapToGridWithCurrentSelectedObj)
            {
                // If it's selecting an object, well, set it as the selected one.
                if (CanSelectObjectWithRay(out GameObject obj))
                {
                    SetSelectedObj(obj);
                }
                // Otherwise, deselect the last selected object if there's one ONLY if it's not holding Ctrl, since it may be selecting multiple objects.
                // We don't want the user to lost his whole selection by just one simple mistake, do we?
                else if (!Input.GetKey(KeyCode.LeftControl))
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
            else if (isCurrentlyMovingAnObject) // This SHOULD be executed only when the user stopped moving an object.
            {
                collidingArrow = GizmosArrow.None;
                isCurrentlyMovingAnObject = false;

                currentExecutingAction.newPos = currentSelectedObj.transform.localPosition;
                actionsMade.Add(currentExecutingAction);

                levelHasBeenModified = true;
            }

            // If press the Delete key and there's a selected object, delete it.
            // Also, only delete when the user is NOT typing in an input field.
            if (Input.GetKeyDown(KeyCode.Delete) && currentSelectedObj != null && !Utilities.theresAnInputFieldSelected)
            {
                DeleteSelectedObj();
            }

            // If the global gizmos arrows are enabled, force them to be with 0 rotation.
            if (globalGizmosArrowsEnabled && gizmosArrows.activeSelf)
            {
                gizmosArrows.transform.rotation = Quaternion.identity;
            }

            ManageSomeShortcuts();

            ManageUndo();
        }

        void ManageSomeShortcuts()
        {
            // Ignore shortcuts when the user is typing.
            if (Utilities.theresAnInputFieldSelected)
            {
                return;
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

            // Shortcut for saving level data.
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.S) && levelHasBeenModified)
            {
                LevelData.SaveLevelData(levelName, levelFileNameWithoutExtension);
                EditorUIManager.Instance.PlaySavingLevelLabel();
                levelHasBeenModified = false;
            }

            // Shortcut for duplicating current selected object.
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.D))
            {
                DuplicateSelectedObject();
            }

            ManageObjectRotationShortcuts();

            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.P))
            {
                EnterPlayMode();
            }

            // Shortcut for hide/show category button in UI.
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.H) && currentMode == Mode.Building)
            {
                EditorUIManager.Instance.HideOrShowCategoryButtons();
            }

            // Shortcuts to switch between local and global gizmos arrows.
            if (Input.GetKeyDown(KeyCode.G) && collidingArrow == GizmosArrow.None)
            {
                globalGizmosArrowsEnabled = !globalGizmosArrowsEnabled;

                // If it's using gizmos arrows right now, change its rotation rn.
                if (!globalGizmosArrowsEnabled && gizmosArrows.activeSelf)
                {
                    gizmosArrows.transform.localRotation = Quaternion.identity;
                }
            }

            // Shortcut to show keybinds help panel.
            if (Input.GetKeyDown(KeyCode.F1))
            {
                EditorUIManager.Instance.ShowOrHideHelpPanel();
            }
        }

        void ManageUndo()
        {
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Z))
            {
                if (actionsMade.Count > 0)
                {
                    bool undoActionExecuted = true;
                    LEAction toUndo = actionsMade.Last();

                    // Remove the whole LEActions that make reference to an unexisting object and get the last one.
                    while ((toUndo.targetObj == null && !toUndo.forMultipleObjects) || (toUndo.targetObjs == null && toUndo.forMultipleObjects))
                    {
                        actionsMade.Remove(toUndo);
                        toUndo = actionsMade.Last();
                    }

                    switch (toUndo.actionType)
                    {
                        case LEAction.LEActionType.MoveObject:
                            if (toUndo.forMultipleObjects)
                            {
                                SetMultipleObjectsAsSelected(null);
                                multipleSelectedObjsParent.transform.localPosition = toUndo.newPos; // Set to the newest position.
                                SetMultipleObjectsAsSelected(toUndo.targetObjs);
                                // Move the parent so the whole selection is moved too.
                                multipleSelectedObjsParent.transform.localPosition = toUndo.oldPos;
                            }
                            else
                            {
                                toUndo.targetObj.transform.localPosition = toUndo.oldPos;
                                SetSelectedObj(toUndo.targetObj);
                            }
                            break;

                        case LEAction.LEActionType.RotateObject:
                            if (toUndo.forMultipleObjects)
                            {
                                SetMultipleObjectsAsSelected(null);
                                multipleSelectedObjsParent.transform.localRotation = toUndo.newRot; // Set to the newest rotation.
                                SetMultipleObjectsAsSelected(toUndo.targetObjs);
                                // Rotate the parent so the whole selection is rotated too.
                                multipleSelectedObjsParent.transform.localRotation = toUndo.oldRot;
                            }
                            else
                            {
                                toUndo.targetObj.transform.localRotation = toUndo.oldRot;
                                SetSelectedObj(toUndo.targetObj);
                            }
                            break;

                        case LEAction.LEActionType.DeleteObject:
                            if (toUndo.forMultipleObjects)
                            {
                                SetMultipleObjectsAsSelected(null);
                                toUndo.targetObjs.ForEach(obj => obj.SetActive(true)); // Enable the objects again and then select them again.
                                SetMultipleObjectsAsSelected(toUndo.targetObjs);
                            }
                            else
                            {
                                toUndo.targetObj.SetActive(true);
                                SetSelectedObj(toUndo.targetObj);
                            }
                            break;

                        default:
                            undoActionExecuted = false;
                            break;
                    }

                    // Only set the level as modified if a undo action was executed.
                    if (undoActionExecuted)
                    {
                        levelHasBeenModified = true;
                    }

                    actionsMade.Remove(toUndo);
                }
            }
        }

        // For now, this method only disables and enables the "building" UI, with the objects available to build.
        void ChangeMode(Mode mode)
        {
            currentMode = mode;

            switch (currentMode)
            {
                case Mode.Building:
                    // Only enable the panel if the keybinds help panel is DISABLED.
                    if (!EditorUIManager.Instance.helpPanel.activeSelf)
                    {
                        EditorUIManager.Instance.categoryButtonsParent.SetActive(true);
                        EditorUIManager.Instance.currentCategoryBG.SetActive(true);
                    }
                    break;

                case Mode.Selection:
                case Mode.Deletion:
                    EditorUIManager.Instance.categoryButtonsParent.SetActive(false);
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
                        // Only update the preview object rotation when the ray hit ANOTHER surface, so the user can rotate the preview object before placing it.
                        if (lastHittenNormalByPreviewRay != hit.normal)
                        {
                            lastHittenNormalByPreviewRay = hit.normal;
                            previewObjectToBuildObj.transform.up = hit.normal;
                        }
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
            levelHasBeenModified = true;
            PlaceObject(currentObjectToBuildName, previewObjectToBuildObj.transform.localPosition, previewObjectToBuildObj.transform.localEulerAngles, true);
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
                isCurrentlyMovingAnObject = true;

                // IT WORKS, DON'T EVEN DARE TO TOUCH THIS EVER AGAIN!

                Vector3 hitWorldPosition = ray.GetPoint(distance);
                Vector3 displacement = hitWorldPosition - objPositionWhenArrowClick;

                float movementDistance = Vector3.Dot(displacement, GetAxisDirection(collidingArrow, currentSelectedObj));

                Vector3 realOffset = RotatePositionAroundPivot(offsetObjPositionAndMosueWhenClick + objPositionWhenArrowClick, objPositionWhenArrowClick, currentSelectedObj.transform.rotation) - objPositionWhenArrowClick;

                // If it's using global arrows, just use the normal offset, otherwise, use the damn complex math path.
                if (globalGizmosArrowsEnabled)
                {
                    currentSelectedObj.transform.localPosition = objPositionWhenArrowClick + (GetAxisDirection(collidingArrow, currentSelectedObj) * movementDistance) + offsetObjPositionAndMosueWhenClick;
                }
                else
                {
                    currentSelectedObj.transform.localPosition = objPositionWhenArrowClick + (GetAxisDirection(collidingArrow, currentSelectedObj) * movementDistance) + realOffset;
                }
            }
        }

        void DeleteSelectedObj()
        {
            // Get the current existing objects in the level objects parent.
            int existingObjects = levelObjectsParent.GetChilds(false).Where(x => x.name != "MoveObjectArrows" && x.name != "SnapToGridCube").ToArray().Length;

            if (multipleObjectsSelected)
            {
                // Since the selected objects are in another parent, also count the objects in that parent.
                existingObjects += multipleSelectedObjsParent.GetChilds(false).Where(x => x.name != "MoveObjectArrows" && x.name != "SnapToGridCube").ToArray().Length;

                if (existingObjects - currentSelectedObjects.Count <= 0)
                {
                    Utilities.ShowCustomNotificationRed("There must be at least 1 object in the level", 2f);
                    return;
                }

                foreach (var obj in currentSelectedObj.GetChilds())
                {
                    if (obj.name == "MoveObjectArrows" || obj.name == "SnapToGridCube") continue;

                    obj.SetActive(false);
                    levelHasBeenModified = true;
                }
            }
            else
            {
                if (existingObjects <= 1)
                {
                    Utilities.ShowCustomNotificationRed("There must be at least 1 object in the level", 2f);
                    return;
                }
                currentSelectedObj.SetActive(false);
                currentSelectedObjComponent.OnDelete();
                levelHasBeenModified = true;
            }
            // Register the LEAction before deselecting the object, so I can set the target obj with the reference to the current selected object.
            #region Register LEAction
            currentExecutingAction = new LEAction();
            currentExecutingAction.actionType = LEAction.LEActionType.DeleteObject;

            currentExecutingAction.forMultipleObjects = multipleObjectsSelected;
            if (multipleObjectsSelected)
            {
                currentExecutingAction.targetObjs = new List<GameObject>();
                foreach (var obj in currentSelectedObj.GetChilds())
                {
                    if (obj.name == "MoveObjectArrows") continue;
                    if (obj.name == "SnapToGridCube") continue;

                    currentExecutingAction.targetObjs.Add(obj);
                }
            }
            else
            {
                currentExecutingAction.targetObj = currentSelectedObj;
            }

            actionsMade.Add(currentExecutingAction);
            #endregion

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

            otherObjectsFromBundle = bundle.Load<GameObject>("OtherObjects").GetChilds();

            bundle.Unload(false);
        }

        public GameObject LoadOtherObjectInBundle(string objectName)
        {
            return otherObjectsFromBundle.FirstOrDefault(obj => obj.name == objectName);
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
            if (Input.GetKey(KeyCode.LeftControl) && obj != null && obj != multipleSelectedObjsParent && !isDuplicatingObj)
            {
                // If there was another object selected before, add it to the selected objects list too.
                if (currentSelectedObj != null && currentSelectedObj != multipleSelectedObjsParent)
                {
                    // But only if it hasn't been selected yet.
                    if (!currentSelectedObjects.Contains(currentSelectedObj)) currentSelectedObjects.Add(currentSelectedObj);
                }
                // And add the most recent now, ofc lol (but only if it hasn't been selected yet).
                if (!currentSelectedObjects.Contains(obj)) currentSelectedObjects.Add(obj);

                if (currentSelectedObjects.Count > 1)
                {
                    // Set the bool.
                    multipleObjectsSelected = true;

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

                    #region Set Current Selected Obj Component
                    // Get obj component:
                    bool selectionHasDifferentObjTypes = false;
                    LE_Object.ObjectType? firstTypeFound = null;
                    foreach (var objInList in currentSelectedObjects)
                    {
                        // If there's no type found yet, use the first one found.
                        if (firstTypeFound == null)
                        {
                            firstTypeFound = objInList.GetComponent<LE_Object>().objectType;
                            continue;
                        }

                        // If the obj type of this obj is different from the first found one, the obj types diffier.
                        if (objInList.GetComponent<LE_Object>().objectType != firstTypeFound)
                        {
                            selectionHasDifferentObjTypes = true;
                        }
                    }

                    // If the obj types diffier, set the component as null.
                    if (selectionHasDifferentObjTypes)
                    {
                        if (currentSelectedObjComponent != null) currentSelectedObjComponent.OnDeselect();
                        currentSelectedObjComponent = null;
                    }
                    else // Otherwise, get the component from the first element in the list.
                    {
                        currentSelectedObjComponent = currentSelectedObjects[0].GetComponent<LE_Object>();
                    }
                    #endregion
                }
                else
                {
                    multipleObjectsSelected = false;

                    currentSelectedObj = obj;
                    currentSelectedObjComponent = currentSelectedObj.GetComponent<LE_Object>();
                    currentSelectedObjComponent.OnSelect();
                }
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
                    currentSelectedObjComponent.OnSelect();
                }
                else if (currentSelectedObj == null)
                {
                    if (currentSelectedObjComponent != null) currentSelectedObjComponent.OnDeselect();
                    currentSelectedObjComponent = null;
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
                    EditorUIManager.Instance.SetSelectedObject(currentSelectedObjComponent);
                }
            }
            else
            {
                EditorUIManager.Instance.SetSelectedObjPanelAsNone();
            }
        }

        public void SetMultipleObjectsAsSelected(List<GameObject> objects)
        {
            // Set the selected object as null so all of the "old" selected objects are deselected. Also remove them from the selected objects parent.
            SetSelectedObj(null);
            currentSelectedObjects.ForEach(obj => obj.transform.parent = levelObjectsParent.transform);
            currentSelectedObjects.Clear();

            if (objects != null)
            {
                if (objects.Count > 0)
                {
                    currentSelectedObjects = new List<GameObject>(objects); // Replace the list with the new one with the copied objects.
                    currentSelectedObjects.ForEach(obj => obj.transform.parent = multipleSelectedObjsParent.transform);
                    SetSelectedObj(multipleSelectedObjsParent); // Select the selected objects parent again.
                }
            }
        }

        public GameObject PlaceObject(string objName, Vector3 position, Vector3 eulerAngles, bool setAsSelected = true)
        {
            if (!allCategoriesObjects.ContainsKey(objName))
            {
                Logger.Error($"Can't find object with name \"{objName}\". Skipping it...");
                return null;
            }

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

                SetMultipleObjectsAsSelected(newSelectedObjectsList);
                levelHasBeenModified = true;
            }
            else
            {
                isDuplicatingObj = true;
                LE_Object objComponent = currentSelectedObj.GetComponent<LE_Object>();
                PlaceObject(objComponent.objectOriginalName, objComponent.transform.localPosition, objComponent.transform.localEulerAngles);
                isDuplicatingObj = false;
                levelHasBeenModified = true;
            }
        }

        void ManageObjectRotationShortcuts()
        {
            GameObject targetObj = currentMode == Mode.Building ? previewObjectToBuildObj : currentSelectedObj;

            if (targetObj == null) return;

            // Rotate to the other side when pressing Left Shift.
            int multiplier = Input.GetKey(KeyCode.T) ? -1 : 1;

            Quaternion rotation = targetObj.transform.localRotation;

            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.R))
            {
                targetObj.transform.localRotation = Quaternion.identity;
            }
            else if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.R))
            {
                targetObj.transform.Rotate(15f * multiplier, 0f, 0f);
            }
            else if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.R))
            {
                targetObj.transform.Rotate(0f, 0f, 15f * multiplier);
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                targetObj.transform.Rotate(0f, 15f * multiplier, 0f);
            }

            // If the rotation changed and the object isn't the preview object...
            if (rotation != targetObj.transform.localRotation && currentMode != Mode.Building)
            {
                // Save it to editor history.
                #region Register LEAction
                currentExecutingAction = new LEAction();
                currentExecutingAction.actionType = LEAction.LEActionType.RotateObject;

                currentExecutingAction.forMultipleObjects = multipleObjectsSelected;
                if (multipleObjectsSelected)
                {
                    currentExecutingAction.targetObjs = new List<GameObject>();
                    foreach (var obj in currentSelectedObj.GetChilds())
                    {
                        if (obj.name == "MoveObjectArrows") continue;
                        if (obj.name == "SnapToGridCube") continue;

                        currentExecutingAction.targetObjs.Add(obj);
                    }
                }
                else
                {
                    currentExecutingAction.targetObj = currentSelectedObj;
                }

                currentExecutingAction.oldRot = rotation;
                currentExecutingAction.newRot = currentSelectedObj.transform.localRotation;

                actionsMade.Add(currentExecutingAction);
                #endregion

                // Also set the level as modified:
                levelHasBeenModified = true;
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
                    bool theHittenObjectIsTheSelectedOne = false;
                    if (multipleObjectsSelected)
                    {
                        foreach (var obj in currentSelectedObjects)
                        {
                            if (hit.collider.transform.parent.gameObject == obj) theHittenObjectIsTheSelectedOne = true;
                        }
                    }
                    else
                    {
                        if (hit.collider.transform.parent.gameObject == currentSelectedObj) theHittenObjectIsTheSelectedOne = true;
                    }

                    // If the hit is an static pos trigger (snap trigger)...
                    if (hit.collider.gameObject.name.StartsWith("StaticPos"))
                    {
                        // Only if the snap trigger is NOT from the current selected object.
                        bool isFromTheSameObject = false;
                        if (multipleObjectsSelected)
                        {
                            foreach (var obj in currentSelectedObjects)
                            {
                                if (hit.collider.transform.parent.parent.parent.gameObject == obj) isFromTheSameObject = true;
                            }
                        }
                        else
                        {
                            if (hit.collider.transform.parent.parent.parent.gameObject == currentSelectedObj) isFromTheSameObject = true;
                        }

                        if (!isFromTheSameObject)
                        {
                            // If the component isn't null, use it to idenfity which snap triggers use and which ones no.
                            if (currentSelectedObjComponent != null)
                            {
                                // Detect if that trigger actually CAN be used with the selected object.
                                if (CanUseThatSnapToGridTrigger(currentSelectedObjComponent.objectOriginalName, hit.collider.gameObject))
                                {
                                    currentSelectedObj.transform.position = hit.collider.transform.position;
                                    currentSelectedObj.transform.rotation = hit.collider.transform.rotation;

                                    levelHasBeenModified = true;

                                    return;
                                }
                            }
                            else // If it's null, use any snap trigger we find.
                            {
                                currentSelectedObj.transform.position = hit.collider.transform.position;
                                currentSelectedObj.transform.rotation = hit.collider.transform.rotation;

                                levelHasBeenModified = true;

                                return;
                            }
                        }
                    }
                    // If the hitten object is the current selected one, keep iterating...
                    else if (theHittenObjectIsTheSelectedOne)
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

        public void EnterPlayMode()
        {
            MelonCoroutines.Start(Coroutine());

            IEnumerator Coroutine()
            {
                Melon<Core>.Instance.loadCustomLevelOnSceneLoad = true;
                Melon<Core>.Instance.levelFileNameWithoutExtensionToLoad = levelFileNameWithoutExtension;
                EditorUIManager.Instance.DeleteUI();
                MenuController.GetInstance().ButtonPressed(ButtonController.Type.CHAPTER_4);

                // Wait a few so when the pause menu ui is not visible anymore, destroy the pause menu LE buttons, and it doesn't look weird when destroying them and the user can see it.
                yield return new WaitForSecondsRealtime(0.2f);
                EditorUIManager.Instance.pauseMenu.GetComponent<EditorPauseMenuPatcher>().BeforeDestroying();
                EditorUIManager.Instance.pauseMenu.RemoveComponent<EditorPauseMenuPatcher>();

                // Also, enable navigation.
                EditorUIManager.Instance.navigation.SetActive(true);
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

                        #region Register LEAction
                        currentExecutingAction = new LEAction();
                        currentExecutingAction.forMultipleObjects = multipleObjectsSelected;

                        currentExecutingAction.actionType = LEAction.LEActionType.MoveObject;

                        if (multipleObjectsSelected)
                        {
                            currentExecutingAction.targetObjs = new List<GameObject>();
                            foreach (var obj in currentSelectedObj.GetChilds())
                            {
                                if (obj.name == "MoveObjectArrows") continue;
                                if (obj.name == "SnapToGridCube") continue;

                                currentExecutingAction.targetObjs.Add(obj);
                            }
                        }
                        else
                        {
                            currentExecutingAction.targetObj = currentSelectedObj;
                        }
                        currentExecutingAction.oldPos = objPositionWhenArrowClick;
                        #endregion

                        // Create the panel with the rigt normals.
                        if (hit.collider.name == "X" || hit.collider.name == "Z")
                        {
                            if (globalGizmosArrowsEnabled)
                            {
                                movementPlane = new Plane(Vector3.up, objPositionWhenArrowClick);
                            }
                            else
                            {
                                movementPlane = new Plane(currentSelectedObj.transform.up, objPositionWhenArrowClick);
                            }
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
                            // Not do any of this complex math that I don't even understand anymore LMAO.
                            if (!globalGizmosArrowsEnabled)
                            {
                                collisionOnPlane = RotatePositionAroundPivot(collisionOnPlane, objPositionWhenArrowClick, Quaternion.Inverse(currentSelectedObj.transform.rotation));
                            }

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
            if (globalGizmosArrowsEnabled)
            {
                if (arrow == GizmosArrow.X) return Vector3.right;
                if (arrow == GizmosArrow.Y) return Vector3.up;
                if (arrow == GizmosArrow.Z) return Vector3.forward;
            }
            else
            {
                if (arrow == GizmosArrow.X) return obj.transform.right;
                if (arrow == GizmosArrow.Y) return obj.transform.up;
                if (arrow == GizmosArrow.Z) return obj.transform.forward;
            }

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

public struct LEAction
{
    public enum LEActionType
    {
        MoveObject,
        RotateObject,
        DeleteObject
    }

    public bool forMultipleObjects;

    public GameObject targetObj;
    public List<GameObject> targetObjs;

    public LEActionType actionType;

    public Vector3 oldPos;
    public Vector3 newPos;

    public Quaternion oldRot;
    public Quaternion newRot;
}