using FS_LevelEditor.Editor.UI;
using FS_LevelEditor.SaveSystem;
using Harmony;
using Il2Cpp;
using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static MelonLoader.bHaptics;

namespace FS_LevelEditor.Editor
{
    public enum EditorState
    {
        NORMAL,
        MOVING_OBJECT,
        SNAPPING_TO_GRID,
        SELECTING_TARGET_OBJ,
        PAUSED,
    }

    [RegisterTypeInIl2Cpp]
    public class EditorController : MonoBehaviour
    {
        public static EditorController Instance { get; private set; }

        public string levelName = "test_level";
        public string levelFileNameWithoutExtension = "test_level";

        public EditorState previousEditorState;
        public EditorState currentEditorState;

        // Avaiable objects from all of the categories.
        GameObject editorObjectsRootFromBundle;
        public List<Dictionary<LE_Object.ObjectType, GameObject>> allCategoriesObjectsSorted = new ();
        public Dictionary<LE_Object.ObjectType, GameObject> allCategoriesObjects = new ();
        GameObject[] otherObjectsFromBundle;

        // Available categories related variables.
        public List<string> categoriesNames = new List<string>();
        public string currentCategory = "";
        public int currentCategoryID = 0;

        public LE_Object.ObjectType? currentObjectToBuildType = null;
        GameObject currentObjectToBuild;
        GameObject previewObjectToBuildObj = null;

        // Related to object placement? Dunno how to call this.
        Vector3? lastHittenNormalByPreviewRay = null;
        GameObject currentHittenSnapTrigger = null;

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
        public List<LE_Object> currentInstantiatedObjects = new List<LE_Object>();

        // Selected mode.
        public enum Mode { Building, Selection, Deletion }
        public Mode currentMode = Mode.Building;

        // Gizmos arrows to move objects.
        GameObject gizmosArrows;
        enum GizmosArrow { None, X, Y, Z }
        GizmosArrow collidingArrow;
        Vector3 objPositionWhenArrowClick;
        Vector3 objLocalPositionWhenStartedMoving;
        Vector3 offsetObjPositionAndMosueWhenClick;
        Plane movementPlane;
        bool globalGizmosArrowsEnabled = false;

        // SNAP
        GameObject snapToGridCube;
        Vector3 objPositionWhenStartToSnap;
        Vector3 objLocalPositionWhenStartToSnap;
        Quaternion objLocalRotationWhenStartToSnap;

        public List<LEAction> actionsMade = new List<LEAction>();
        public LEAction currentExecutingAction;
        public bool levelHasBeenModified = false;

        // Misc?
        public DeathYPlaneCtrl deathYPlane;

        public bool enteringPlayMode = false;

        // ----------------------------
        public Dictionary<string, object> globalProperties = LevelData.GetDefaultGlobalProperties();
        List<Material> skyboxes = new List<Material>();

        void Awake()
        {
            Instance = this;
            MenuController.isInLevelEditor = true;
            LoadAssetBundle();

            levelObjectsParent = new GameObject("LevelObjects");
            levelObjectsParent.transform.position = Vector3.zero;

            multipleSelectedObjsParent = new GameObject("MultipleSelectedObjsParent");
            multipleSelectedObjsParent.transform.position = Vector3.zero;

            deathYPlane = Instantiate(LoadOtherObjectInBundle("DeathYPlane")).AddComponent<DeathYPlaneCtrl>();
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
                categoriesNames.Add(child.name);
            }

            currentCategory = categoriesNames[0];
            currentCategoryID = 0;

            foreach (var categoryObj in editorObjectsRootFromBundle.GetChilds())
            {
                Dictionary<LE_Object.ObjectType, GameObject> categoryObjects = new();

                foreach (var obj in categoryObj.GetChilds())
                {
                    if (obj.name == "None") continue;

                    var objectType = LE_Object.ConvertNameToObjectType(obj.name);
                    if (objectType == null) continue; // JUST IN CASE.

                    categoryObjects.Add(objectType.Value, obj);
                    allCategoriesObjects.Add(objectType.Value, obj);
                }

                allCategoriesObjectsSorted.Add(categoryObjects);
            }

            gizmosArrows = Instantiate(bundle.Load<GameObject>("MoveObjectArrows"));
            gizmosArrows.name = "MoveObjectArrows";
            gizmosArrows.transform.localPosition = Vector3.zero;
            gizmosArrows.SetActive(false);

            snapToGridCube = Instantiate(bundle.Load<GameObject>("SnapToGridCube"));
            snapToGridCube.name = "SnapToGridCube";
            snapToGridCube.transform.localPosition = Vector3.zero;
            snapToGridCube.SetActive(false);

            otherObjectsFromBundle = bundle.Load<GameObject>("OtherObjects").GetChilds();

            Utils.LoadMaterials(bundle);

            foreach (var material in bundle.LoadAll<Material>())
            {
                if (material.name.StartsWith("Skybox"))
                {
                    material.shader = Shader.Find("Skybox/6 Sided 3 Axis Rotation");
                    skyboxes.Add(material);
                }
            }

            bundle.Unload(false);
        }
        public GameObject LoadOtherObjectInBundle(string objectName)
        {
            return otherObjectsFromBundle.FirstOrDefault(obj => obj.name == objectName);
        }

        void Start()
        {
            // Disable occlusion culling.
            Camera.main.useOcclusionCulling = false;

            // The code to change the Mode to Selection by default is in EditorUIManager.Start() since here, the UI script hasn't been initialized yet.
        }
        public void AfterFinishedLoadingLevel()
        {
            SetupSkybox((int)globalProperties["Skybox"]);
        }

        void Update()
        {
            if (PlayFromMenuHelper.PlayImmediatelyOnEditorLoad &&
    PlayFromMenuHelper.LevelToPlay == levelFileNameWithoutExtension)
            {
                PlayFromMenuHelper.PlayImmediatelyOnEditorLoad = false;
                PlayFromMenuHelper.LevelToPlay = null;
                EditorController.Instance.EnterPlayMode();
            }
            if (enteringPlayMode) return;

            ManageEscAction();

            if (IsCurrentState(EditorState.PAUSED) || EditorUIManager.IsCurrentUIContext(EditorUIContext.EVENTS_PANEL) ||
                EditorUIManager.IsCurrentUIContext(EditorUIContext.TEXT_EDITOR)) return;

            #region Select Target Object For Events
            if (IsCurrentState(EditorState.SELECTING_TARGET_OBJ))
            {
                if (GetCollidingWithAnArrow() == GizmosArrow.None)
                {
                    if (CanSelectObjectWithRay(out GameObject obj))
                    {
                        LE_Object objComp = obj.GetComponent<LE_Object>();

                        EditorUIManager.Instance.UpdateHittenTargetObjPanel(objComp.objectFullNameWithID);
                        if (Input.GetMouseButtonDown(0))
                        {
                            SetCurrentEditorState(EditorState.PAUSED); // It's set to paused while in events panel, so the user can't move the camera or anything.
                            EditorUIManager.Instance.SetEditorUIContext(EditorUIContext.EVENTS_PANEL);
                            EventsUIPageManager.Instance.SetTargetObjectWithLE_Object(objComp);
                        }
                        return;
                    }
                }

                EditorUIManager.Instance.UpdateHittenTargetObjPanel("");

                return;
            }
            #endregion

            // When click, check if it's clicking a gizmos arrow.
            if (Input.GetMouseButtonDown(0))
            {
                collidingArrow = GetCollidingWithAnArrow();
            }

            #region Preview Object and Build
            // For previewing the current selected object...
            // !Input.GetMouseButton(1) is to detect when LE camera isn't rotating.
            if (!Input.GetMouseButton(1) && currentMode == Mode.Building && collidingArrow == GizmosArrow.None && previewObjectToBuildObj != null && !Utils.IsMouseOverUIElement())
            {
                PreviewObject();

                if (Input.GetMouseButtonDown(0) && previewObjectToBuildObj.activeInHierarchy)
                {
                    InstanceObjectInThePreviewObjectPos();
                }
            }
            // If isn't previewing, disable the preview object if not null.
            else if (previewObjectToBuildObj != null)
            {
                lastHittenNormalByPreviewRay = null;
                previewObjectToBuildObj.SetActive(false);
            }
            #endregion

            #region Align Instantiated Object to Grid
            // For snap already instantiated object to grid again.
            if (Input.GetKey(KeyCode.F) && currentSelectedObj != null && currentMode == Mode.Selection && !Utils.theresAnInputFieldSelected)
            {
                snapToGridCube.SetActive(true);
                gizmosArrows.SetActive(false);

                if (Input.GetMouseButtonDown(0))
                {
                    if (IsHittingObject("SnapToGridCube"))
                    {
                        objPositionWhenStartToSnap = currentSelectedObj.transform.position;
                        objLocalPositionWhenStartToSnap = currentSelectedObj.transform.localPosition;
                        objLocalRotationWhenStartToSnap = currentSelectedObj.transform.localRotation;

                        SetCurrentEditorState(EditorState.SNAPPING_TO_GRID);
                    }
                }
                if (Input.GetMouseButton(0) && IsCurrentState(EditorState.SNAPPING_TO_GRID))
                {
                    AlignSelectedObjectToGrid();
                }
                if (Input.GetMouseButtonUp(0) && IsCurrentState(EditorState.SNAPPING_TO_GRID))
                {
                    SetCurrentEditorState(EditorState.NORMAL);

                    if (currentSelectedObj.transform.position != objPositionWhenStartToSnap)
                    {
                        RegisterLEAction(LEAction.LEActionType.SnapObject, currentSelectedObj, multipleObjectsSelected, objLocalPositionWhenStartToSnap,
                            currentSelectedObj.transform.localPosition, objLocalRotationWhenStartToSnap, currentSelectedObj.transform.localRotation);
                    }
                }
            }
            else
            {
                snapToGridCube.SetActive(false);

                if (currentSelectedObj != null && currentMode == Mode.Selection)
                {
                    gizmosArrows.SetActive(true);
                }

                if (Input.GetMouseButtonUp(0) && IsCurrentState(EditorState.SNAPPING_TO_GRID))
                {
                    SetCurrentEditorState(EditorState.NORMAL);

                    if (currentSelectedObj.transform.position != objPositionWhenStartToSnap)
                    {
                        RegisterLEAction(LEAction.LEActionType.SnapObject, currentSelectedObj, multipleObjectsSelected, objLocalPositionWhenStartToSnap,
                            currentSelectedObj.transform.localPosition, objLocalRotationWhenStartToSnap, currentSelectedObj.transform.localRotation);
                    }
                }
            }
            #endregion

            #region Select Object
            // For object selection...
            if (Input.GetMouseButtonDown(0) && currentMode == Mode.Selection && collidingArrow == GizmosArrow.None && !Utils.IsMouseOverUIElement() && !IsCurrentState(EditorState.SNAPPING_TO_GRID))
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
            #endregion

            #region Delete With Click
            // If click and it's on deletion and it's NOT clicking a gizmos arrow AND the mouse isn't over a UI element..
            if (Input.GetMouseButtonDown(0) && currentMode == Mode.Deletion && collidingArrow == GizmosArrow.None && !Utils.IsMouseOverUIElement())
            {
                // If it's clicking an object.
                if (CanSelectObjectWithRay(out GameObject obj))
                {
                    // Yep, I'm lazy and Ik this isn't the best way of doing it... but it works I think :D
                    // UPDATE: NEW FUNCTION TO DELETE OBJECTS IN THE RIGHT WAY, LET'S GO
                    DeleteObject(obj);
                }
            }
            #endregion

            #region Move Object
            // If it's clicking a gizmos arrow.
            if (Input.GetMouseButton(0) && collidingArrow != GizmosArrow.None)
            {
                // Move the object.
                MoveObject(collidingArrow);
            }
            else if (IsCurrentState(EditorState.MOVING_OBJECT)) // This SHOULD be executed only when the user stopped moving an object.
            {
                collidingArrow = GizmosArrow.None;
                SetCurrentEditorState(EditorState.NORMAL);

                RegisterLEAction(LEAction.LEActionType.MoveObject, currentSelectedObj, multipleObjectsSelected, objLocalPositionWhenStartedMoving,
                    currentSelectedObj.transform.localPosition, null, null);

                levelHasBeenModified = true;
            }
            #endregion

            #region Delete Object With Delete
            // If press the Delete key and there's a selected object, delete it.
            // Also, only delete when the user is NOT typing in an input field.
            if ((Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.KeypadPeriod)) && currentSelectedObj != null && !Utils.theresAnInputFieldSelected)
            {
                DeleteSelectedObj();
            }
            #endregion

            // Update the global attributes of the object if it's moving it and it's only one (multiple objects aren't supported).
            if (IsCurrentState(EditorState.MOVING_OBJECT))
            {
                if (multipleObjectsSelected)
                {
                    SelectedObjPanel.Instance.UpdateGlobalObjectAttributes(multipleSelectedObjsParent.transform);
                }
                else
                {
                    SelectedObjPanel.Instance.UpdateGlobalObjectAttributes(currentSelectedObj.transform);
                }
            }

            // The code to force reset the gizmos arrows to 0 when global gizmos are enabled, is in LateUpdate().

            ManageSomeShortcuts();

            ManageUndo();

            // If the user's typing and then he uses an arrow key to navigate to another character of the field... well... the arrow also moves the object LOL.
            // We need to avoid that.
            if (!Utils.theresAnInputFieldSelected && currentMode == Mode.Selection) ManageMoveObjectShortcuts();
        }
        void LateUpdate()
        {
            if (gizmosArrows.activeSelf && currentSelectedObj)
            {
                gizmosArrows.transform.position = currentSelectedObj.transform.position;

                // If the global gizmos arrows are enabled, force them to be with 0 rotation.
                if (globalGizmosArrowsEnabled)
                {
                    gizmosArrows.transform.rotation = Quaternion.identity;
                }
                else
                {
                    gizmosArrows.transform.rotation = currentSelectedObj.transform.rotation;
                }
            }

            if (snapToGridCube.activeSelf && currentSelectedObj)
            {
                snapToGridCube.transform.position = currentSelectedObj.transform.position;
            }

            if (deathYPlane && deathYPlane.gameObject.activeSelf)
            {
                deathYPlane.gameObject.SetActive(true);
                deathYPlane.SetYPos((float)globalProperties["DeathYLimit"]);
            }
        }

        void ManageEscAction()
        {
            // Shortcut for pausing LE.
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (EditorUIManager.IsCurrentUIContext(EditorUIContext.EVENTS_PANEL))
                {
                    EventsUIPageManager.Instance.HideEventsPage();
                    return;
                }
                else if (EditorUIManager.IsCurrentUIContext(EditorUIContext.TEXT_EDITOR))
                {
                    TextEditorUI.Instance.HideTextEditor();
                    return;
                }
                else if (EditorUIManager.IsCurrentUIContext(EditorUIContext.SELECTING_TARGET_OBJ))
                {
                    SetCurrentEditorState(EditorState.PAUSED); // It's set to paused while in events panel, so the user can't move the camera or anything.
                    EditorUIManager.Instance.SetEditorUIContext(EditorUIContext.EVENTS_PANEL);
                    return;
                }

                if (!IsCurrentState(EditorState.PAUSED))
                {
                    EditorUIManager.Instance.ShowPause();
                }
                else
                {
                    EditorUIManager.Instance.Resume();
                }
            }
        }

        void ManageSomeShortcuts()
        {
            // Ignore shortcuts when the user is typing.
            if (Utils.theresAnInputFieldSelected)
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

            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.P) && !enteringPlayMode)
            {
                // Save data automatically.
                LevelData.SaveLevelData(levelName, levelFileNameWithoutExtension);

                EnterPlayMode();
            }

            // Shortcut for hide/show category button in UI.
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.H) && currentMode == Mode.Building)
            {
                EditorObjectsToBuildUI.Instance.HideOrShowCategoryButtons();
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

            if (Input.GetKeyDown(KeyCode.Space) && currentSelectedObj)
            {
                SelectedObjPanel.Instance.setActiveAtStartToggle.Set(!SelectedObjPanel.Instance.setActiveAtStartToggle.isChecked);
            }

            // Shortcut to show keybinds help panel.
            if (Input.GetKeyDown(KeyCode.F1))
            {
                EditorUIManager.Instance.ShowOrHideHelpPanel();
            }

            if (Input.GetKeyDown(KeyCode.O))
            {
                GlobalPropertiesPanel.Instance.ShowOrHideGlobalPropertiesPanel();
            }
        }
        void ManageMoveObjectShortcuts()
        {
            GameObject targetObj = currentMode == Mode.Building ? previewObjectToBuildObj : currentSelectedObj;
            if (targetObj == null) return;

            Vector3 toMove = Vector3.zero;

            if (Input.GetKeyDown(KeyCode.LeftArrow)) toMove += Vector3.left * 0.01f;
            if (Input.GetKeyDown(KeyCode.RightArrow)) toMove += Vector3.right * 0.01f;
            if (Input.GetKeyDown(KeyCode.UpArrow)) toMove += Vector3.up * 0.01f;
            if (Input.GetKeyDown(KeyCode.DownArrow)) toMove += Vector3.down * 0.01f;

            if (toMove != Vector3.zero)
            {
                Vector3 oldPos = targetObj.transform.localPosition;

                if (globalGizmosArrowsEnabled)
                {
                    targetObj.transform.Translate(toMove, Space.World);
                }
                else
                {
                    if (Vector3.Dot(targetObj.transform.forward, Camera.main.transform.forward) < 0) toMove *= -1;

                    targetObj.transform.Translate(toMove, Space.Self);
                }

                if (currentSelectedObj) // If the target obj is the current selected object, to check if it's NOT the preview object.
                {
                    RegisterLEAction(LEAction.LEActionType.MoveObject, currentSelectedObj, multipleObjectsSelected, oldPos, currentSelectedObj.transform.localPosition,
                        null, null);

                    SelectedObjPanel.Instance.UpdateGlobalObjectAttributes(currentSelectedObj.transform);
                }
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
                // Update global attributes.
                SelectedObjPanel.Instance.UpdateGlobalObjectAttributes(currentSelectedObj.transform);

                RegisterLEAction(LEAction.LEActionType.RotateObject, currentSelectedObj, multipleObjectsSelected, null, null, rotation,
                    currentSelectedObj.transform.localRotation);

                // Also set the level as modified:
                levelHasBeenModified = true;
            }
        }

        void ManageUndo()
        {
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Z))
            {
                if (actionsMade.Count > 0)
                {
                    LEAction toUndo = actionsMade.Last();

                    // Remove the whole LEActions that make reference to an unexisting object and get the last one.
                    while ((toUndo.targetObj == null && !toUndo.forMultipleObjects) || (toUndo.targetObjs == null && toUndo.forMultipleObjects))
                    {
                        actionsMade.Remove(toUndo);
                        if (actionsMade.Count <= 0) return;
                        toUndo = actionsMade.Last();
                    }

                    toUndo.Undo(this);

                    Logger.Log($"Undid {toUndo.actionType} action for " + (toUndo.forMultipleObjects ? $"{toUndo.targetObjs.Count} objects." : $"\"{toUndo.targetObj.name}\"."));
                    levelHasBeenModified = true;

                    actionsMade.Remove(toUndo);
                }
            }
        }

        // For now, this method only disables and enables the "building" UI, with the objects available to build.
        public void ChangeMode(Mode mode)
        {
            currentMode = mode;

            switch (currentMode)
            {
                case Mode.Building:
                    // Only enable the panel if the keybinds help panel is DISABLED.
                    if (EditorUIManager.IsCurrentUIContext(EditorUIContext.NORMAL))
                    {
                        EditorObjectsToBuildUI.Instance.root.SetActive(true);
                    }
                    break;

                case Mode.Selection:
                case Mode.Deletion:
                    EditorObjectsToBuildUI.Instance.root.SetActive(false);
                    break;
            }

            if (currentMode == Mode.Selection)
            {
                // Only enable gizmos if there's a selected object.
                if (currentSelectedObj) gizmosArrows.SetActive(true);
            }
            else
            {
                gizmosArrows.SetActive(false);
            }

            Logger.Log("Changed LE mode to: " + currentMode);
            EditorUIManager.Instance.SetCurrentModeLabelText(currentMode);
        }

        void PreviewObject()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            List<RaycastHit> hits = Physics.RaycastAll(ray, Mathf.Infinity, -1, QueryTriggerInteraction.Collide).ToList();
            hits.Sort((hit1, hit2) => hit1.distance.CompareTo(hit2.distance));

            bool snapWithTrigger = false;
            RaycastHit rayToUseWithSnap = new RaycastHit();

            bool theyAreAllSnapTriggers = hits.All(hit => hit.collider.gameObject.name.StartsWith("StaticPos"));

            if (hits.Count > 0)
            {
                // If there's only one hit and if it is a snap trigger, it has to be the only hitten object for sure.
                // Or, if all of the hits are snap to grid triggers, also execute this and use the first hit as well, it doesn't matter.
                if (hits.Count == 1 || theyAreAllSnapTriggers)
                {
                    if (hits[0].collider.gameObject.name.StartsWith("StaticPos"))
                    {
                        if (CanUseThatSnapToGridTrigger(currentObjectToBuildType.Value, hits[0].collider.gameObject))
                        {
                            snapWithTrigger = true;
                            rayToUseWithSnap = hits[0];
                        }
                        hits.RemoveAll(hit => hit.collider.gameObject.name.StartsWith("StaticPos"));
                    }
                    else
                    {
                        hits.RemoveAll(hit => hit.collider.gameObject.name.StartsWith("StaticPos"));
                    }
                }
                else
                {
                    // If there are 2 hits or more, then iterate over the hits and check if you can snap with them.
                    // Only if you're pressing Ctrl.
                    foreach (var hit in hits)
                    {
                        if (hit.collider.gameObject.name.StartsWith("StaticPos") && Input.GetKey(KeyCode.LeftControl))
                        {
                            if (CanUseThatSnapToGridTrigger(currentObjectToBuildType.Value, hit.collider.gameObject))
                            {
                                snapWithTrigger = true;
                                rayToUseWithSnap = hit;
                                break;
                            }
                        }
                        else
                        {
                            // Hits are shorted from the closest one to the farthest one.
                            // If at some point, we stop detecting snap triggers, remove all of the snap triggeres from the hits list.
                            // This is to avoid things like grounds colliding with the triggers and no snapping since there are more objects below.
                            hits.RemoveAll(hit => hit.collider.gameObject.name.StartsWith("StaticPos"));
                            break;
                        }
                    }
                }

                if (snapWithTrigger)
                {
                    previewObjectToBuildObj.SetActive(true);
                    previewObjectToBuildObj.transform.position = rayToUseWithSnap.collider.transform.position;
                    // Only update rotation when the trigger is different, so user can rotate preview object even when snap trigger is found.
                    if (currentHittenSnapTrigger != rayToUseWithSnap.collider.gameObject)
                    {
                        currentHittenSnapTrigger = rayToUseWithSnap.collider.gameObject;
                        previewObjectToBuildObj.transform.rotation = rayToUseWithSnap.collider.transform.rotation;
                    }
                }
                else if (hits.Count > 0) // When using the default preview behaviour, use the closest hit, why not?
                {
                    currentHittenSnapTrigger = null;

                    previewObjectToBuildObj.SetActive(true);
                    previewObjectToBuildObj.transform.position = hits[0].point;
                    // Only update the preview object rotation when the ray hit ANOTHER surface, so the user can rotate the preview object before placing it.
                    if (lastHittenNormalByPreviewRay != hits[0].normal)
                    {
                        lastHittenNormalByPreviewRay = hits[0].normal;
                        previewObjectToBuildObj.transform.up = hits[0].normal;
                    }
                }
                else
                {
                    previewObjectToBuildObj.SetActive(false);
                }
            }
            else
            {
                previewObjectToBuildObj.SetActive(false);
            }
        }
        void InstanceObjectInThePreviewObjectPos()
        {
            levelHasBeenModified = true;
            PlaceObject(currentObjectToBuildType, previewObjectToBuildObj.transform.localPosition, previewObjectToBuildObj.transform.localEulerAngles, Vector3.one, true);

            // About the scale being fixed to 1... you can't change the scale of the PREVIEW object, so...
        }
        public GameObject PlaceObject(LE_Object.ObjectType? objectType, Vector3 position, Vector3 eulerAngles, Vector3 scale, bool setAsSelected = true)
        {
            if (setAsSelected)
            {
                // if setAsSelect is false, that would probably mean it's placing objects from save.
                Logger.Log($"Placing object of name \"{objectType}\". This log only appears when setAsSelected is true.");
            }

            if (objectType == null)
            {
                Logger.Error("objectType is null. Skipping object placement...");
                return null;
            }
            if (!allCategoriesObjects.ContainsKey(objectType.Value))
            {
                Logger.Error($"Can't find object with name \"{objectType}\". Skipping it...");
                return null;
            }

            GameObject template = allCategoriesObjects[objectType.Value];
            GameObject obj = Instantiate(template, levelObjectsParent.transform);

            obj.transform.localPosition = position;
            obj.transform.localEulerAngles = eulerAngles;
            obj.transform.localScale = scale;

            LE_Object addedComp = LE_Object.AddComponentToObject(obj, objectType.Value);

            if (addedComp == null)
            {
                Destroy(obj);
                return null;
            }

            addedComp.SetObjectColor(LE_Object.LEObjectContext.NORMAL);

            obj.SetActive(true);

            if (setAsSelected)
            {
                SetSelectedObj(obj);
            }

            return obj;
        }

        public enum SelectionType { Normal, ForceSingle, ForceMultiple }
        public void SetSelectedObj(GameObject obj, SelectionType selectionType = SelectionType.Normal)
        {
            if (currentSelectedObj == obj) return;

            if (obj && obj.name == gizmosArrows.name)
            {
                Logger.Error("HOW THE FUCK DID YOU MANAGE TO SELECT THE FUCKING GIZMOS ARROWS!? Anyways, this shouldn't case any trouble now :)");
                return;
            }

            if (obj) Logger.DebugLog($"SetSelectedObj called for object with name: \"{obj.name}\".");
            else Logger.DebugLog($"SetSelectedObj called with NO NEW TARGET OBJECT (To deselect).");

            gizmosArrows.SetActive(false);

            // SnapToGrid cube is adjusted in Late Update.

            // Reset the last selected object color back to normal.
            if (currentSelectedObj != null)
            {
                if (multipleObjectsSelected)
                {
                    foreach (var @object in currentSelectedObjects)
                    {
                        @object.GetComponent<LE_Object>().SetObjectColor(LE_Object.LEObjectContext.NORMAL);
                    }
                }
                else
                {
                    currentSelectedObjComponent.SetObjectColor(LE_Object.LEObjectContext.NORMAL);
                }
            }

            // Get when the user is pressing Left Control, normally, that's for when the user wanna select multiple objects.
            // Also only execute this when the use is NOT duplicating objects, due to some interferences when then user is pressing Ctrl BUT to duplicate.
            if ((Input.GetKey(KeyCode.LeftControl) || selectionType == SelectionType.ForceMultiple) && obj != null && obj != multipleSelectedObjsParent && !isDuplicatingObj &&
                selectionType != SelectionType.ForceSingle)
            {
                // If it's the first time pressing ctrl to select multiple objects, also add the previous selected object to the new selected objs list.
                if (currentSelectedObj != null && currentSelectedObj != multipleSelectedObjsParent)
                {
                    // But only if it hasn't been selected yet.
                    if (!currentSelectedObjects.Contains(currentSelectedObj)) currentSelectedObjects.Add(currentSelectedObj);
                }
                // And add the most recent now, ofc lol (but only if it hasn't been selected yet).
                if (!currentSelectedObjects.Contains(obj))
                {
                    currentSelectedObjects.Add(obj);
                }
                else // If the object is already in the list, DEselect it:
                {
                    currentSelectedObjects.Remove(obj);
                    obj.transform.parent = obj.GetComponent<LE_Object>().objectParent; // Remove the object from the multipleSelectedObjsParent.
                    if (currentSelectedObjects.Count == 1)
                    {
                        SetSelectedObj(currentSelectedObjects[0]); // If there's only one object left, set it as the selected object.
                        return;
                    }
                }

                // LE will only detect multiple objects as selected when the selected count is more than 1.
                if (currentSelectedObjects.Count > 1)
                {
                    // Set the bool.
                    multipleObjectsSelected = true;

                    // Get the center position of the whole objects.
                    Vector3 centeredPosition = Vector3.zero;
                    foreach (var objInList in currentSelectedObjects) { centeredPosition += objInList.transform.position; }
                    centeredPosition /= currentSelectedObjects.Count;

                    // Remove the parent from the selected objects, set the new parent position and put the parent in the objects again.
                    currentSelectedObjects.ForEach(x => x.transform.parent = x.GetComponent<LE_Object>().objectParent);
                    multipleSelectedObjsParent.transform.localScale = Vector3.one;
                    multipleSelectedObjsParent.transform.position = centeredPosition;
                    multipleSelectedObjsParent.transform.rotation = Quaternion.identity;
                    currentSelectedObjects.ForEach(x => x.transform.parent = multipleSelectedObjsParent.transform);

                    // The "main" selected object now is the parent of the selected objects.
                    currentSelectedObj = multipleSelectedObjsParent;

                    Logger.DebugLog($"Adding \"{obj.name}\" to the multiple selected objects.");

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
                        if (currentSelectedObjComponent != null) currentSelectedObjComponent.OnDeselect(null);
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

                    Logger.Log($"\"{obj.name}\" selected while pressing CTRL, BUT NO OTHER OBJECTS ARE SELECTED.");
                }
            }
            else
            {
                // Since the obj parameter can also be the multipleSelectedObjectsParent, check if it is before setting the multipleObjectsSelected bool to false.
                if (obj != multipleSelectedObjsParent)
                {
                    if (currentSelectedObjects.Count > 0)
                    {
                        Logger.Log($"Deselecting the current selected objects, the count was: {currentSelectedObjects.Count}.");
                        currentSelectedObjects.ForEach(x => x.transform.parent = x.GetComponent<LE_Object>().objectParent);
                        currentSelectedObjects.ForEach(x => x.GetComponent<LE_Object>().OnDeselect(obj));
                        currentSelectedObjects.Clear();
                    }
                    multipleObjectsSelected = false; // Set the bool again.
                }
                else // Otherwise, if it IS... set this bool again to true.
                {
                    multipleObjectsSelected = true;
                }

                // Work as always (the normal selection system lol).
                currentSelectedObj = obj;
                // multipleSelectedObjectsParent doesn't have a LE_Object component, so skip this part if that's the case.
                if (currentSelectedObj != null && currentSelectedObj != multipleSelectedObjsParent)
                {
                    if (currentSelectedObjComponent != null) currentSelectedObjComponent.OnDeselect(currentSelectedObj);
                    currentSelectedObjComponent = currentSelectedObj.GetComponent<LE_Object>();
                    // The OnSelect method will be called more below AFTER the funciton changes the color of the mesh to green.
                }
                else if (currentSelectedObj == null)
                {
                    if (currentSelectedObjComponent != null) currentSelectedObjComponent.OnDeselect(null);
                    currentSelectedObjComponent = null;
                }
            }

            if (currentSelectedObj != null)
            {
                // Change the color of the new select object to the "selected" color.
                if (multipleObjectsSelected)
                {
                    foreach (var @object in currentSelectedObjects)
                    {
                        @object.GetComponent<LE_Object>().SetObjectColor(LE_Object.LEObjectContext.SELECT);
                    }
                }
                else
                {
                    currentSelectedObjComponent.SetObjectColor(LE_Object.LEObjectContext.SELECT);
                }

                if (currentMode == Mode.Selection) gizmosArrows.SetActive(true);
                gizmosArrows.transform.localRotation = currentSelectedObj.transform.rotation;

                if (multipleObjectsSelected)
                {
                    SelectedObjPanel.Instance.SetMultipleObjectsSelected();
                    currentSelectedObjects.ForEach(x => x.GetComponent<LE_Object>().OnSelect());
                }
                else
                {
                    SelectedObjPanel.Instance.SetSelectedObject(currentSelectedObjComponent);
                    currentSelectedObjComponent.OnSelect();
                }
            }
            else
            {
                SelectedObjPanel.Instance.SetSelectedObjPanelAsNone();
            }
        }
        public void SetMultipleObjectsAsSelected(List<GameObject> objects, bool isForUndo = false)
        {
            // Set the selected object as null so all of the "old" selected objects are deselected.
            SetSelectedObj(null);

            if (objects != null)
            {
                if (!isForUndo) multipleSelectedObjsParent.transform.localScale = Vector3.one;

                if (objects.Count > 0)
                {
                    if (!isForUndo) // Use this system, SetSelectObj will do whatever is needed correcty.
                    {
                        foreach (var obj in objects)
                        {
                            SetSelectedObj(obj, SelectionType.ForceMultiple);
                        }
                    }
                    else // Use this old "awful" system that for some reason hasn't break anything yet, and it's needed for ManageUndo().
                    {
                        currentSelectedObjects = new List<GameObject>(objects); // Replace the list with the new one with the copied objects.
                        currentSelectedObjects.ForEach(obj => obj.transform.parent = multipleSelectedObjsParent.transform);
                        SetSelectedObj(multipleSelectedObjsParent);
                    }
                }
            }
        }

        void DeleteObject(GameObject obj)
        {
            // Get the current existing objects in the level objects parent.
            int existingObjects = levelObjectsParent.GetChilds(false).ToArray().Length;

            if (existingObjects <= 1)
            {
                Logger.Warning("Attemped to delete one single object but IS THE LAST OBJECT IN THE SCENE!");

                Utils.ShowCustomNotificationRed("There must be at least 1 object in the level", 2f);
                return;
            }

            if (multipleObjectsSelected && currentSelectedObjects.Contains(obj))
            {
                // Since the object is already selected, this SetSelectedObj is going to DESELECT it.
                SetSelectedObj(obj, SelectionType.ForceMultiple);
                if (currentSelectedObjects.Count > 1)
                {
                    SetMultipleObjectsAsSelected(new List<GameObject>(currentSelectedObjects));
                }
                else
                {
                    // Since it's only one object left, use the currentSelectedObj variable.
                    // Afaik, calling SetSelectedObj now it's not needed, but I'm just doing it to be sure.
                    SetSelectedObj(currentSelectedObj, SelectionType.ForceSingle);
                }
            }
            else
            {
                if (currentSelectedObj == obj)
                {
                    SetSelectedObj(null); // Deselect the object if it was the current selected object.
                }
            }

            LE_Object objComp = obj.GetComponent<LE_Object>();
            objComp.OnDelete();
            if (objComp.canUndoDeletion)
            {
                Logger.Log("Single object deleted, but it can be undone.");
                obj.SetActive(false);
            }
            else
            {
                Logger.Log("Single object deleted permanently!");
                Destroy(obj);
            }
            levelHasBeenModified = true;

            if (objComp.canUndoDeletion)
            {
                // Register the LEAction before deselecting the object, so I can set the target obj with the reference to the current selected object.
                RegisterLEAction(LEAction.LEActionType.DeleteObject, obj, false, null, null, null, null);
            }
        }
        void DeleteSelectedObj()
        {
            // Get the current existing objects in the level objects parent.
            int existingObjects = levelObjectsParent.GetChilds(false).ToArray().Length;

            if (multipleObjectsSelected)
            {
                // Since the selected objects are in another parent, also count the objects in that parent.
                existingObjects += multipleSelectedObjsParent.GetChilds(false).ToArray().Length;

                if (existingObjects - currentSelectedObjects.Count <= 0)
                {
                    Utils.ShowCustomNotificationRed("There must be at least 1 object in the level", 2f);
                    return;
                }

                foreach (var obj in currentSelectedObj.GetChilds())
                {
                    obj.GetComponent<LE_Object>().OnDelete();

                    if (obj.GetComponent<LE_Object>().canUndoDeletion)
                    {
                        obj.SetActive(false);
                    }
                    else
                    {
                        Destroy(obj);
                    }
                    levelHasBeenModified = true;
                }

                Logger.Log("Deleted multiple selected objects.");
            }
            else
            {
                if (existingObjects <= 1)
                {
                    Logger.Warning("Attemped to delete one single object but IS THE LAST OBJECT IN THE SCENE!");

                    Utils.ShowCustomNotificationRed("There must be at least 1 object in the level", 2f);
                    return;
                }
                currentSelectedObjComponent.OnDelete();
                if (currentSelectedObjComponent.canUndoDeletion)
                {
                    Logger.Log("Single object deleted, but it can be undone.");
                    currentSelectedObj.SetActive(false);
                }
                else
                {
                    Logger.Log("Single object deleted permanently!");
                    Destroy(currentSelectedObj);
                }
                levelHasBeenModified = true;
            }

            if ((!multipleObjectsSelected && currentSelectedObjComponent.canUndoDeletion) || multipleObjectsSelected)
            {
                // Register the LEAction before deselecting the object, so I can set the target obj with the reference to the current selected object.
                RegisterLEAction(LEAction.LEActionType.DeleteObject, currentSelectedObj, multipleObjectsSelected, null, null, null, null);
            }

            SetSelectedObj(null);
        }

        bool CanUseThatSnapToGridTrigger(LE_Object.ObjectType objToBuildType, GameObject triggerObj)
        {
            var triggerRootObj = triggerObj.transform.parent.parent.gameObject;

            // Check for ALL of the object-specific triggers for this object, and see if there's a specific trigger for this object to build.
            bool existsSpecificTriggerForThisObjToBuild = false;
            foreach (var child in triggerRootObj.GetChilds())
            {
                foreach (var availableObjectNames in child.name.Split('|'))
                {
                    var trimmedName = availableObjectNames.Trim();
                    var objectTypesForTriggerSet = LE_Object.GetObjectTypesForSnapToGrid(trimmedName);
                    if (objectTypesForTriggerSet.Contains(objToBuildType))
                    {
                        existsSpecificTriggerForThisObjToBuild = true;
                        break;
                    }
                }
            }

            // Now get the objects that this trigger is compatible with.
            var availableObjectsForTrigger = triggerObj.transform.parent.name
                .Split('|')
                .SelectMany(x => LE_Object.GetObjectTypesForSnapToGrid(x.Trim())).ToList();

            if (availableObjectsForTrigger.Contains(objToBuildType))
                return true;

            if (triggerObj.transform.parent.name == "Global" && !existsSpecificTriggerForThisObjToBuild)
                return true;

            return false;
        }

        void StartMovingObject(string arrowColliderName, Ray cameraRay)
        {
            // Save the position of the object from the first time we clicked.
            objPositionWhenArrowClick = currentSelectedObj.transform.position;

            objLocalPositionWhenStartedMoving = currentSelectedObj.transform.localPosition;

            // Create the panel with the rigt normals.
            if (arrowColliderName == "X" || arrowColliderName == "Z")
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
            else if (arrowColliderName == "Y")
            {
                Vector3 cameraPosition = Camera.main.transform.position;
                Vector3 directionToCamera = cameraPosition - objPositionWhenArrowClick;
                Vector3 planeNormal = new Vector3(directionToCamera.normalized.x, 0f, directionToCamera.normalized.z);

                movementPlane = new Plane(planeNormal, objPositionWhenArrowClick);
            }

            // Then get the right offset of the arrows.
            offsetObjPositionAndMosueWhenClick = Vector3.zero;
            if (movementPlane.Raycast(cameraRay, out float enter))
            {
                Vector3 collisionOnPlane = cameraRay.GetPoint(enter);
                // Not do any of this complex math that I don't even understand anymore LMAO.
                if (!globalGizmosArrowsEnabled)
                {
                    collisionOnPlane = RotatePositionAroundPivot(collisionOnPlane, objPositionWhenArrowClick, Quaternion.Inverse(currentSelectedObj.transform.rotation));
                }

                if (arrowColliderName == "X") offsetObjPositionAndMosueWhenClick.x = objPositionWhenArrowClick.x - collisionOnPlane.x;
                if (arrowColliderName == "Y") offsetObjPositionAndMosueWhenClick.y = objPositionWhenArrowClick.y - collisionOnPlane.y;
                if (arrowColliderName == "Z") offsetObjPositionAndMosueWhenClick.z = objPositionWhenArrowClick.z - collisionOnPlane.z;
            }
        }
        void MoveObject(GizmosArrow direction)
        {
            // Get the ray from the camera.
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // If the ray can collide with the "invisible" plane.
            if (movementPlane.Raycast(ray, out float distance))
            {
                if (!IsCurrentState(EditorState.MOVING_OBJECT)) SetCurrentEditorState(EditorState.MOVING_OBJECT);

                // IT WORKS, DON'T EVEN DARE TO TOUCH THIS EVER AGAIN!

                Vector3 hitWorldPosition = ray.GetPoint(distance);
                Vector3 displacement = hitWorldPosition - objPositionWhenArrowClick;

                float movementDistance = Vector3.Dot(displacement, GetAxisDirection(collidingArrow, currentSelectedObj));

                Vector3 realOffset = RotatePositionAroundPivot(offsetObjPositionAndMosueWhenClick + objPositionWhenArrowClick, objPositionWhenArrowClick, currentSelectedObj.transform.rotation) - objPositionWhenArrowClick;

                // If it's using global arrows, just use the normal offset, otherwise, use the damn complex math path.
                if (globalGizmosArrowsEnabled)
                {
                    currentSelectedObj.transform.position = objPositionWhenArrowClick + (GetAxisDirection(collidingArrow, currentSelectedObj) * movementDistance) + offsetObjPositionAndMosueWhenClick;
                }
                else
                {
                    currentSelectedObj.transform.position = objPositionWhenArrowClick + (GetAxisDirection(collidingArrow, currentSelectedObj) * movementDistance) + realOffset;
                }
            }
        }

        void DuplicateSelectedObject()
        {
            if (currentSelectedObj == null) return;

            if (multipleObjectsSelected)
            {
                Logger.Log("Duplicating multiple selected objects...");

                // Create a copy of every object inside of the selected objects list.
                List<GameObject> newSelectedObjectsList = new List<GameObject>();
                foreach (var obj in currentSelectedObjects)
                {
                    LE_Object objComponent = obj.GetComponent<LE_Object>();

                    GameObject placedObj = PlaceObject(objComponent.objectType, objComponent.transform.position, objComponent.transform.eulerAngles,
                        objComponent.transform.localScale, false);
                    if (!placedObj)
                    {
                        Logger.Log($"PlaceObject when duplicating \"{objComponent.objectType}\" returned null. It probably reached its max object limit.");
                        continue;
                    }
                    LE_Object newPlacedObjComp = placedObj.GetComponent<LE_Object>();

                    foreach (var property in objComponent.properties)
                    {
                        newPlacedObjComp.SetProperty(property.Key, Utils.CreateCopyOf(property.Value));
                    }

                    newSelectedObjectsList.Add(placedObj);
                }

                SetMultipleObjectsAsSelected(newSelectedObjectsList);
                levelHasBeenModified = true;
            }
            else
            {
                Logger.Log("Duplicating one single object...");

                isDuplicatingObj = true;
                LE_Object objComponent = currentSelectedObj.GetComponent<LE_Object>();
                GameObject placedObj = PlaceObject(objComponent.objectType, objComponent.transform.localPosition, objComponent.transform.localEulerAngles,
                    objComponent.transform.localScale, false);
                if (!placedObj)
                {
                    Logger.Log($"PlaceObject when duplicating \"{objComponent.objectType}\" returned null. It probably reached its max object limit.");
                    return;
                }

                LE_Object newPlacedObjComp = placedObj.GetComponent<LE_Object>();
                foreach (var property in objComponent.properties)
                {
                    newPlacedObjComp.SetProperty(property.Key, Utils.CreateCopyOf(property.Value));
                }

                SetSelectedObj(placedObj);

                isDuplicatingObj = false;
                levelHasBeenModified = true;
            }

            Logger.Log("DuplicateSelectedObj function finished!");
        }

        void AlignSelectedObjectToGrid()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            List<RaycastHit> hits = Physics.RaycastAll(ray, Mathf.Infinity, -1, QueryTriggerInteraction.Collide).ToList();
            hits.Sort((hit1, hit2) => hit1.distance.CompareTo(hit2.distance));

            if (hits.Count > 0)
            {
                foreach (var hit in hits)
                {
                    if (hit.collider.gameObject.name == snapToGridCube.name) continue;

                    #region Skip if it's the current selected object
                    bool hitIsFromTheCurrentSelectedObj = false;
                    if (multipleObjectsSelected)
                    {
                        hitIsFromTheCurrentSelectedObj = currentSelectedObjects.Any(obj => obj == hit.collider.transform.parent.gameObject);
                    }
                    else
                    {
                        hitIsFromTheCurrentSelectedObj = currentSelectedObj == hit.collider.transform.parent.gameObject;
                    }
                    if (hitIsFromTheCurrentSelectedObj) continue;
                    #endregion

                    if (hit.collider.gameObject.name.StartsWith("StaticPos"))
                    {
                        #region Skip trigger if it's from the current selected object
                        bool triggerIsFromTheCurrentSelectedObj = false;
                        if (multipleObjectsSelected)
                        {
                            triggerIsFromTheCurrentSelectedObj =
                                currentSelectedObjects.Any(obj => obj == hit.collider.transform.parent.parent.parent.gameObject);
                        }
                        else
                        {
                            triggerIsFromTheCurrentSelectedObj = hit.collider.transform.parent.parent.parent.gameObject == currentSelectedObj;
                        }
                        if (triggerIsFromTheCurrentSelectedObj) continue;
                        #endregion

                        // currentSelectedObjComponent isn't null even when selecting multiple objects, but only when the selected objects are of the
                        // same type, so, use it to identify the available snap triggers (no matter if is selecting multiple objects or not).
                        if (currentSelectedObjComponent != null)
                        {
                            if (CanUseThatSnapToGridTrigger(currentSelectedObjComponent.objectType.Value, hit.collider.gameObject))
                            {
                                currentSelectedObj.transform.position = hit.collider.transform.position;
                                currentSelectedObj.transform.rotation = hit.collider.transform.rotation;
                                SelectedObjPanel.Instance.UpdateGlobalObjectAttributes(currentSelectedObj.transform);

                                levelHasBeenModified = true;

                                return;
                            }
                        }
                        else
                        {
                            currentSelectedObj.transform.position = hit.collider.transform.position;
                            currentSelectedObj.transform.rotation = hit.collider.transform.rotation;
                            // Don't update global object attributes, since if the current selected component is null, that means the user is 100% selecting
                            // multiple objects.

                            levelHasBeenModified = true;

                            return;
                        }
                    }
                    else
                    {
                        // Avoid detecting triggers by traspassing objects.
                        break;
                    }
                }
            }
        }

        // This method is called when the scale of the object is changed, this is to adjust the gizmos scale in case the current selected object's scale is smaller than 1.
        public void ApplyGizmosArrowsScale()
        {
            float highestAxis = Utils.HighestValueOfVector(currentSelectedObj.transform.localScale);
            
            if (highestAxis >= 1f)
            {
                gizmosArrows.transform.localScale = Vector3.one * 2f;
            }
            else
            {
                gizmosArrows.transform.localScale = Vector3.one * 2f * highestAxis;
            }
        }

        public void RegisterLEAction(LEAction.LEActionType type, GameObject targetObj, bool forMultipleObjs, Vector3? oldPos = null, Vector3? newPos = null,
            Quaternion? oldRot = null, Quaternion? newRot = null, Vector3? oldScale = null, Vector3? newScale = null)
        {
            if (!targetObj) return;

            currentExecutingAction = new LEAction();
            currentExecutingAction.forMultipleObjects = forMultipleObjs;

            currentExecutingAction.actionType = type;

            switch (type)
            {
                case LEAction.LEActionType.MoveObject:
                    currentExecutingAction.oldPos = oldPos.Value;
                    currentExecutingAction.newPos = newPos.Value;
                    break;

                case LEAction.LEActionType.RotateObject:
                    currentExecutingAction.oldRot = oldRot.Value;
                    currentExecutingAction.newRot = newRot.Value;
                    break;

                case LEAction.LEActionType.ScaleObject:
                    currentExecutingAction.oldScale = oldScale.Value;
                    currentExecutingAction.newScale = newScale.Value;
                    break;

                case LEAction.LEActionType.SnapObject:
                    currentExecutingAction.oldPos = oldPos.Value;
                    currentExecutingAction.newPos = newPos.Value;
                    currentExecutingAction.oldRot = oldRot.Value;
                    currentExecutingAction.newRot = newRot.Value;
                    break;
            }

            if (forMultipleObjs)
            {
                currentExecutingAction.targetObjs = new List<GameObject>();
                foreach (var obj in targetObj.GetChilds())
                {
                    // If the type is Deletion, only add those objects that CAN be actually un-deleted.
                    if (type == LEAction.LEActionType.DeleteObject && !obj.GetComponent<LE_Object>().canUndoDeletion) continue;

                    currentExecutingAction.targetObjs.Add(obj);
                }
            }
            else
            {
                currentExecutingAction.targetObj = targetObj;
            }

            actionsMade.Add(currentExecutingAction);
        }


        public void EnterPlayMode()
        {
            if (enteringPlayMode) return;

            if (!EditorController.Instance.currentInstantiatedObjects.Any(x => x is LE_Player_Spawn && x.gameObject.activeSelf))
            {
                Logger.Warning("Attemped to enter playmode but THERE'S NO PLAYER SPAWN OBJECT!");

                Utils.ShowCustomNotificationRed("There's no a Player Spawn object in the level.", 2f);
                return;
            }

            MelonCoroutines.Start(Coroutine());

            IEnumerator Coroutine()
            {
                enteringPlayMode = true;

                Melon<Core>.Instance.loadCustomLevelOnSceneLoad = true;
                Melon<Core>.Instance.levelFileNameWithoutExtensionToLoad = levelFileNameWithoutExtension;
                EditorUIManager.Instance.DeleteUI();

                MenuController.SoftInputAuthorized = true;
                MenuController.InputAuthorized = true;
                MenuController.GetInstance().ButtonPressed(ButtonController.Type.CHAPTER_4);

                // Wait a few so when the pause menu ui is not visible anymore, destroy the pause menu LE buttons, and it doesn't look weird when destroying them and the user can see it.
                yield return new WaitForSecondsRealtime(0.2f);
                EditorUIManager.Instance.pauseMenu.GetComponent<EditorPauseMenuPatcher>().BeforeDestroying();
                EditorUIManager.Instance.pauseMenu.RemoveComponent<EditorPauseMenuPatcher>();

                // Also, enable navigation.
                EditorUIManager.Instance.navigation.SetActive(true);

                Logger.Log("Entering playmode...");
            }
        }

        void OnDestroy()
        {
            MenuController.isInLevelEditor = false;
        }

        #region Current Editor State Methods
        public void SetCurrentEditorState(EditorState newState)
        {
            previousEditorState = currentEditorState;
            currentEditorState = newState;
        }
        public static bool IsCurrentState(EditorState state)
        {
            if (Instance == null) return false;

            return Instance.currentEditorState == state;
        }
        #endregion

        #region Methods called from UI buttons
        public void ChangeCategory(int categoryID)
        {
            if (currentCategoryID == categoryID) return;

            currentCategoryID = categoryID;
            currentCategory = categoriesNames[currentCategoryID];
        }

        public void SelectObjectToBuild(LE_Object.ObjectType? objectType)
        {
            // Do nothing if trying to select the same object as the last selected one.
            if (currentObjectToBuildType == objectType) return;

            if (objectType == null)
            {
                currentObjectToBuildType = null;
                currentObjectToBuild = null;
                Destroy(previewObjectToBuildObj);
                return;
            }

            currentObjectToBuildType = objectType;
            currentObjectToBuild = allCategoriesObjectsSorted[currentCategoryID][objectType.Value];

            // Destroy the preview object and create another one with the mew selected model.
            Destroy(previewObjectToBuildObj);
            previewObjectToBuildObj = Instantiate(currentObjectToBuild);

            // Disable collision of the preview object.
            foreach (var collider in previewObjectToBuildObj.TryGetComponents<Collider>())
            {
                collider.enabled = false;
            }
            // STUPID CUBE PHYSICS!!
            foreach (var rigidBody in previewObjectToBuildObj.TryGetComponents<Rigidbody>())
            {
                Destroy(rigidBody); // Destroy the RigidBody, fuck it.
            }
            // This is an static method used for cases like this, where there's no LE_Object at all, all we have is the preview object.
            LE_Object.SetObjectColor(previewObjectToBuildObj, objectType.Value, LE_Object.LEObjectContext.PREVIEW);
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
            // The parent of the gizmos arrows is "MoveObjectsArrows".
            // hittenArrow is NOT the parent, it's the actual hitten arrow :)
            if (IsHittingObjectWhoseParentIs(gizmosArrows.name, out GameObject hittenArrow, out Ray cameraRay))
            {
                StartMovingObject(hittenArrow.name, cameraRay);

                if (hittenArrow.name == "X") return GizmosArrow.X;
                if (hittenArrow.name == "Y") return GizmosArrow.Y;
                if (hittenArrow.name == "Z") return GizmosArrow.Z;
            }

            return GizmosArrow.None;
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

        public bool IsHittingObject(GameObject targetObj)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity);

            // Loop foreach all of the collisions of the ray.
            foreach (var hit in hits)
            {
                if (hit.collider.gameObject == targetObj) return true;
            }

            return false;
        }
        public bool IsHittingObject(string targetObjName)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity);

            // Loop foreach all of the collisions of the ray.
            foreach (var hit in hits)
            {
                if (hit.collider.gameObject.name == targetObjName) return true;
            }

            return false;
        }
        /// <summary>
        /// Detects if the user is currently hitting an object whose parent is of the specified name.
        /// </summary>
        /// <param name="objParentName">The parent name.</param>
        /// <param name="hittenObj">The actual hitten object (NOT THE PARENT).</param>
        /// <returns></returns>
        public bool IsHittingObjectWhoseParentIs(string objParentName, out GameObject hittenObj, out Ray cameraRay)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, -1, QueryTriggerInteraction.Collide);

            cameraRay = ray;

            // Loop foreach all of the collisions of the ray.
            foreach (var hit in hits)
            {
                if (hit.collider.transform.parent != null)
                {
                    if (hit.collider.transform.parent.name == objParentName)
                    {
                        hittenObj = hit.collider.gameObject;
                        return true;
                    }
                }
            }

            hittenObj = null;
            return false;
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

        public void SetupSkybox(int skyboxID)
        {
            RenderSettings.skybox = skyboxes[skyboxID];
        }
    }

    public struct LEAction
    {
        public enum LEActionType
        {
            MoveObject,
            RotateObject,
            ScaleObject,
            SnapObject,
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

        public Vector3 oldScale;
        public Vector3 newScale;

        public void Undo(EditorController editor)
        {
            switch (actionType)
            {
                case LEActionType.MoveObject:
                    UndoMoveObject(editor);
                    break;
                case LEActionType.RotateObject:
                    UndoRotateObject(editor);
                    break;
                case LEActionType.ScaleObject:
                    UndoScaleObject(editor);
                    break;
                case LEActionType.SnapObject:
                    UndoMoveObject(editor);
                    UndoRotateObject(editor);
                    break;
                case LEActionType.DeleteObject:
                    UndoDeleteObject(editor);
                    break;
            }
        }
        void UndoMoveObject(EditorController editor)
        {
            if (forMultipleObjects)
            {
                editor.SetMultipleObjectsAsSelected(null); // Not needed (I think) but looks good for when reading the code LOL.
                editor.multipleSelectedObjsParent.transform.localPosition = newPos; // Set to the newest position.
                editor.SetMultipleObjectsAsSelected(targetObjs, true);
                // Move the parent so the whole selection is moved too.
                editor.multipleSelectedObjsParent.transform.localPosition = oldPos;

                SelectedObjPanel.Instance.UpdateGlobalObjectAttributes(editor.multipleSelectedObjsParent.transform);
            }
            else
            {
                // Since we use local coordinates, set the selected obj to null to avoid breaking the object position lol.
                if (editor.multipleObjectsSelected && editor.currentSelectedObjects.Contains(targetObj)) editor.SetSelectedObj(null);

                targetObj.transform.localPosition = oldPos;
                // In case the selected object is already the object to undo, update its global attributes manually:
                if (editor.currentSelectedObj == targetObj)
                {
                    SelectedObjPanel.Instance.UpdateGlobalObjectAttributes(targetObj.transform);
                }
                editor.SetSelectedObj(targetObj);
            }
        }
        void UndoRotateObject(EditorController editor)
        {
            if (forMultipleObjects)
            {
                editor.SetMultipleObjectsAsSelected(null); // Not needed (I think) but looks good for when reading the code LOL.
                editor.multipleSelectedObjsParent.transform.localRotation = newRot; // Set to the newest rotation.
                editor.SetMultipleObjectsAsSelected(targetObjs, true);
                // Rotate the parent so the whole selection is rotated too.
                editor.multipleSelectedObjsParent.transform.localRotation = oldRot;

                SelectedObjPanel.Instance.UpdateGlobalObjectAttributes(editor.multipleSelectedObjsParent.transform);
            }
            else
            {
                targetObj.transform.localRotation = oldRot;
                // In case the selected object is already the object to undo, update its global attributes manually:
                if (editor.currentSelectedObj == targetObj)
                {
                    SelectedObjPanel.Instance.UpdateGlobalObjectAttributes(targetObj.transform);
                }
                editor.SetSelectedObj(targetObj);
            }
        }
        void UndoScaleObject(EditorController editor)
        {
            if (forMultipleObjects)
            {
                editor.SetMultipleObjectsAsSelected(null); // Not needed (I think) but looks good for when reading the code LOL.
                editor.multipleSelectedObjsParent.transform.localScale = newScale; // Set to the newest scale.
                editor.SetMultipleObjectsAsSelected(targetObjs, true);
                // Move the parent so the whole selection is scaled too.
                editor.multipleSelectedObjsParent.transform.localScale = oldScale;

                SelectedObjPanel.Instance.UpdateGlobalObjectAttributes(editor.multipleSelectedObjsParent.transform);
            }
            else
            {
                targetObj.transform.localScale = oldScale;
                // In case the selected object is already the object to undo, update its global attributes manually:
                if (editor.currentSelectedObj == targetObj)
                {
                    SelectedObjPanel.Instance.UpdateGlobalObjectAttributes(targetObj.transform);
                }
                editor.SetSelectedObj(targetObj);
            }
        }
        void UndoDeleteObject(EditorController editor)
        {
            if (forMultipleObjects)
            {
                editor.SetMultipleObjectsAsSelected(null); // Not needed (I think) but looks good for when reading the code LOL.
                targetObjs.ForEach(obj => obj.SetActive(true)); // Enable the objects again and then select them again.
                targetObjs.ForEach(obj => obj.GetComponent<LE_Object>().isDeleted = false);
                editor.SetMultipleObjectsAsSelected(targetObjs, true);
            }
            else
            {
                targetObj.GetComponent<LE_Object>().isDeleted = false;
                targetObj.SetActive(true);
                editor.SetSelectedObj(targetObj);
            }
        }
    }
}