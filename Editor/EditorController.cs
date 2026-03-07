using FS_LevelEditor.Editor.UI;
using FS_LevelEditor.SaveSystem;
using FS_LevelEditor.UI_Related;
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
using System.Text.RegularExpressions;

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
    public enum BulkSelectionMode
    {
        Everything,
        ObjectsOnly,
        WaypointsAndObjectsWithWaypoints
    }

    [RegisterTypeInIl2Cpp]
    public class EditorController : MonoBehaviour
    {
        public static EditorController Instance { get; private set; }

        public string levelName = "test_level";
        public string levelFileNameWithoutExtension = "test_level";

        EditorState previousEditorState;
        EditorState currentEditorState;

        #region Available Objects From Bundle
        GameObject editorObjectsRootFromBundle;

        public List<Dictionary<LE_Object.ObjectType, GameObject>> allCategoriesObjectsSorted = new();
        public Dictionary<LE_Object.ObjectType, GameObject> allCategoriesObjects = new();

        GameObject[] otherObjectsFromBundle;
        Dictionary<string, Material> allMaterialsFromBundle = new();

        // ------------------------------------

        public List<string> categoriesNames = new List<string>();
        public string currentCategory = "";
        public int currentCategoryID = 0;
        #endregion

        #region Editor Objects Parents
        public GameObject levelObjectsParent;
        public GameObject multipleSelectedObjsParent;
        #endregion

        #region Current Object To Build
        LE_Object.ObjectType? currentObjectToBuildType = null;
        GameObject currentObjectToBuild;
        GameObject previewObjectToBuildObj = null;
        #endregion

        #region Object Placement
        const string SNAP_TRIGGERS_NAME = "StaticPos";

        Vector3 previewRotationOffsetEuler = Vector3.zero;
        Vector3? lastHittenNormalByPreviewRay = null;
        GameObject currentHittenSnapTrigger = null;
        #endregion

        #region Current Selected Object
        public GameObject currentSelectedObj;
        public LE_Object currentSelectedObjComponent;
        // When there's just one object selected, that object in in the currentSelectedObj variable.
        // But when there are multiple objects selected, this list contains em and "currentSelectedObj" is "multipleSelectedObjsParent".
        public List<GameObject> currentSelectedObjects = new List<GameObject>();
        public List<LE_Object> currentSelectedObjsComponents = new List<LE_Object>();
        #endregion

        public List<LE_Object> currentInstantiatedObjects = new List<LE_Object>();

        #region Selected Mode
        public enum Mode { Building, Selection }
        public Mode currentMode = Mode.Building;
        #endregion

        #region Gizmos
        GameObject gizmosRoot;
        EditorGizmo gizmo;
        GizmosArrow collidingArrow;
        Vector3 objPositionWhenArrowClick;
        Vector3 objLocalPositionWhenStartedMoving;
        Vector3 offsetObjPositionAndMosueWhenClick;
        Plane movementPlane;
        bool globalGizmosArrowsEnabled = false;
        #endregion

        #region Snap To Grid
        GameObject snapToGridCube;
        Vector3 objPositionWhenStartToSnap;
        Vector3 objLocalPositionWhenStartToSnap;
        Quaternion objLocalRotationWhenStartToSnap;
        #endregion

        #region Editor Registered Actions For Undo
        public List<LEAction> actionsMade = new List<LEAction>();
        public LEAction currentExecutingAction;
        #endregion

        #region Bulk Selection
        private bool isSelecting = false;
        private Vector2 selectionStartScreen;
        private Vector2 selectionEndScreen;
        private float selectionStartTime;
        private const float multiSelectDelay = 0.3f; // seconds
        private const float minDragDistance = 5f; // pixels
        private GameObject selectionBox;
        private UISprite selectionBoxSprite;
        BulkSelectionMode currentBulkSelectionMode = BulkSelectionMode.Everything;
        #endregion

        #region Grid
        private float gridSize = 1f;
        private const float MIN_GRID_SIZE = 0.0001f;
        private const float MAX_GRID_SIZE = 8f;
        private const float GRID_SIZE_MULTIPLIER = 2f;
        private float gridHeight = 121f;
        private bool gridVisible = true;
        private bool gridEnabled = true;
        private Material gridLineMaterial;
        private Vector3 gridCenter = Vector3.zero;
        private Texture2D gridTexture;
        #endregion

        #region Editor Variables
        public bool multipleObjectsSelected = false;
        public bool multipleObjectsOfTheSameTypeSelected = false;
        bool isDuplicatingObj = false;
        public bool levelHasBeenModified = false;
        public bool showAllWaypoints = false;
        public bool enteringPlayMode = false;

        // ESC Fix
        private bool _isInitialized = false;

        private bool lightingEnabled = true;
        public bool waypointRotation = true;
        #endregion

        // Misc?
        public DeathYPlaneCtrl deathYPlane;

        // ----------------------------
        public Dictionary<string, object> globalProperties = LevelData.GetDefaultGlobalProperties();
        List<Material> skyboxes = new List<Material>();
        List<AudioClip> tracks = new();
        Il2CppAssetBundle editorAssetBundle; // Keep reference to prevent GC and allow FMOD to access audio data

        void Awake()
        {
            Instance = this;
            MenuController.isInLevelEditor = true;

            EnsureGameUIIsHidden();

            LoadAssetBundle();

            levelObjectsParent = new GameObject("LevelObjects");
            levelObjectsParent.transform.position = Vector3.zero;

            multipleSelectedObjsParent = new GameObject("MultipleSelectedObjsParent");
            multipleSelectedObjsParent.transform.position = Vector3.zero;

            deathYPlane = Instantiate(LoadOtherObjectInBundle("DeathYPlane")).AddComponent<DeathYPlaneCtrl>();

            Camera.main.fieldOfView = 90f; // Default FOV.
            Camera.main.nearClipPlane = 0.1f; // To prevent disappearing when near objects.

            currentEditorState = EditorState.NORMAL; // Ensure state is initialized
        }

        void LoadAssetBundle()
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FS_LevelEditor.level_editor");
            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes);

            Il2CppAssetBundle bundle = Il2CppAssetBundleManager.LoadFromMemory(bytes);

            #region Load LE Objects From Bundle
            editorObjectsRootFromBundle = bundle.Load<GameObject>("LevelObjectsRoot");
            editorObjectsRootFromBundle.hideFlags = HideFlags.DontUnloadUnusedAsset;

            // Get categories
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
            #endregion

            #region Setup Gizmos
            gizmosRoot = Instantiate(bundle.Load<GameObject>("MoveObjectArrowsNew"));
            gizmosRoot.name = "MoveObjectArrows";
            gizmosRoot.transform.localPosition = Vector3.zero;
            gizmo = gizmosRoot.AddComponent<EditorGizmo>();
            gizmosRoot.SetActive(false);
            #endregion

            #region Setup Snap To Grid Cube
            snapToGridCube = Instantiate(bundle.Load<GameObject>("SnapToGridCube"));
            snapToGridCube.name = "SnapToGridCube";
            snapToGridCube.transform.localPosition = Vector3.zero;
            snapToGridCube.SetActive(false);
            #endregion

            otherObjectsFromBundle = bundle.Load<GameObject>("OtherObjects").GetChilds();

            Utils.LoadMaterials(bundle); // Opaque/Transparent materials for disabled objects and such.

            #region Load Grid Material
            gridLineMaterial = bundle.Load<Material>("GridLine");
            // Use the Cast function since that's the correct way to cast IL2CPP types.
            gridTexture = gridLineMaterial.mainTexture.Cast<Texture2D>();
            #endregion

            #region Load Skyboxes
            foreach (var material in bundle.LoadAll<Material>())
            {
                if (material.name.StartsWith("Skybox"))
                {
                    if (Regex.Match(material.name, @"(?:9|10|11|12|13)$").Success)
                    {
                        material.shader = Shader.Find("Skybox/6 Sided");
                    }
                    else
                    {
                        material.shader = Shader.Find("Skybox/6 Sided 3 Axis Rotation");
                    }
                    skyboxes.Add(material);
                }
                // sorting the list to get all new skyboxes in the order
                if (skyboxes.Count > 1)
                {
                    int ExtractChapterNumber(string name)
                    {
                        // Matches Skybox_CH<number> (stops at first non-digit after the number)
                        var match = Regex.Match(name, @"^Skybox_CH(\d+)");
                        if (match.Success && int.TryParse(match.Groups[1].Value, out int num))
                            return num;
                        return int.MaxValue; // Non-numbered variants go last
                    }
                    // Stable sort: first by extracted numeric value, then by original name to keep deterministic order for variants
                    skyboxes = skyboxes
                        .OrderBy(m => ExtractChapterNumber(m.name))
                        .ThenBy(m => m.name, StringComparer.Ordinal)
                        .ToList();
                }
            }
            #endregion

            #region Load All Materials
            foreach (var mat in bundle.LoadAll<Material>())
            {
                allMaterialsFromBundle.Add(mat.name, mat);
            }
            #endregion

            #region Setup OST
            string[] trackNames = new[]
            {
                "Level1",
                "Level2_old",
                "Level2",
                "Level3",
                "Level4",
                "Level5_Calm_Loop",
                "Fractaloween_Soundtrack",
                "Fractalentine_Soundtrack",
                "White Trees",
                "SR3d"
            };
            foreach (var trackName in trackNames)
            {
                AudioClip track = bundle.Load<AudioClip>(trackName);
                if (track != null)
                {
                    track.hideFlags = HideFlags.DontUnloadUnusedAsset;
                    tracks.Add(track);
                }
            }
            #endregion

            // Store bundle reference - don't unload it because FMOD needs access to FSB data for audio clips
            editorAssetBundle = bundle;
        }
        public GameObject LoadOtherObjectInBundle(string objectName)
        {
            GameObject toReturn = otherObjectsFromBundle.FirstOrDefault(obj => obj.name == objectName);

            if (objectName == "EditorLine")
            {
                toReturn.GetComponent<LineRenderer>().material.shader = Shader.Find("Sprites/Default");
            }

            return toReturn;
        }
        public Material GetMaterial(string name, bool ignoreCase = false)
        {
            if (ignoreCase)
            {
                foreach (var mat in allMaterialsFromBundle)
                {
                    if (mat.Value.name.Equals(name, StringComparison.OrdinalIgnoreCase)) return mat.Value;
                }
            }
            else
            {
                if (allMaterialsFromBundle.TryGetValue(name, out var material))
                {
                    return material;
                }
            }

            return null;
        }

        void EnsureGameUIIsHidden()
        {
            InGameUIManager ui = InGameUIManager.Instance;
            ui.HideHealthBarRoutine();
            ui.HideDodgeCooldown(true);
            ui.HideHoverGauge(true);
            ui.ShowSprintFeedback(false);
            ui.ShowFuelBar(false, 0, 0);
            ui.ForceHideFuelBar();
            ui.HideFuelBarRoutine(0);
        }

        void Start()
        {
            // Disable occlusion culling.
            Camera.main.useOcclusionCulling = false;

            UpdateGridCenter(); // Ensure gridCenter is correct at start

            _isInitialized = true;
        }

        public void AfterFinishedLoadingLevel()
        {
            SetupSkybox((int)globalProperties["Skybox"]);
        }

        void Update()
        {
            if (enteringPlayMode) return;

            ManageEscAction();

            // Block all editor input when save popup is active
            if (SaveMetadataPopup.IsPopupActive()) return;

            if (IsCurrentState(EditorState.PAUSED)) return;

            #region Gizmos Arrows Hover Color Feedback
            if (currentMode == Mode.Selection && currentSelectedObj && gizmosRoot.activeSelf && !Input.GetMouseButton(0) && !Input.GetMouseButton(1))
            {
                GizmosArrow hoveredArrow = gizmo.GetHoveredArrow(out _);
                if (hoveredArrow != GizmosArrow.None)
                {
                    gizmo.HighlightArrow(hoveredArrow);
                }
                else
                {
                    gizmo.UnhighlightAllArrows();
                }
            }
            else if (gizmosRoot.activeSelf && Input.GetMouseButton(0) && !Input.GetMouseButton(1))
            {
                // Keep highlighting the current arrow being dragged
                if (collidingArrow != GizmosArrow.None)
                {
                    gizmo.HighlightArrow(collidingArrow);
                }
            }
            else
            {
                gizmo.UnhighlightAllArrows();
            }
            #endregion

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
            if (Input.GetMouseButtonDown(0) && !Input.GetMouseButton(1))
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
                gizmosRoot.SetActive(false);

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
                    gizmosRoot.SetActive(true);
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
            if (Input.GetMouseButtonDown(0) && !Input.GetMouseButton(1) && currentMode == Mode.Selection && !Utils.IsMouseOverUIElement() && !IsCurrentState(EditorState.SNAPPING_TO_GRID))
            {
                // Don't handle selection if we're starting to use gizmo
                if (GetCollidingWithAnArrow() == GizmosArrow.None)
                {
                    // If it's selecting an object, well, set it as the selected one.
                    if (CanSelectObjectWithRay(out GameObject obj))
                    {
                        if (Input.GetKey(KeyCode.LeftControl))
                        {
                            SetSelectedObj(obj, SelectionType.ForceMultiple);
                        }
                        else
                        {
                            SetSelectedObj(obj);
                        }
                    }
                    // Otherwise, deselect the last selected object if there's one ONLY if it's not holding Ctrl
                    else if (!Input.GetKey(KeyCode.LeftControl))
                    {
                        SetSelectedObj(null);
                    }
                }
            }
            #endregion

            #region Move Object
            // If it's clicking a gizmos arrow.
            if (Input.GetMouseButton(0) && collidingArrow != GizmosArrow.None)
            {
                if (selectionBox != null && selectionBox.activeSelf)
                    selectionBox.SetActive(false); //hide the box in case
                                                   // Move the object.
                MoveObject(collidingArrow);
            }
            else if (Input.GetMouseButtonUp(0) && IsCurrentState(EditorState.MOVING_OBJECT))
            {
                // Only reset state after fully handling the movement
                RegisterLEAction(LEAction.LEActionType.MoveObject, currentSelectedObj, multipleObjectsSelected,
                    objLocalPositionWhenStartedMoving, currentSelectedObj.transform.localPosition, null, null);

                levelHasBeenModified = true;
                SetCurrentEditorState(EditorState.NORMAL);
                collidingArrow = GizmosArrow.None;
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

            #region Bulk selection
            if (Input.GetMouseButtonDown(0) && !Utils.IsMouseOverUIElement() && !Input.GetKey(KeyCode.F))
            {
                // Only start selection if we're not using gizmo and Shift is held
                if (GetCollidingWithAnArrow() == GizmosArrow.None && currentMode == Mode.Selection && Input.GetKey(KeyCode.LeftShift))
                {
                    isSelecting = true;
                    selectionStartScreen = Input.mousePosition;
                    selectionEndScreen = selectionStartScreen;
                    selectionStartTime = Time.unscaledTime;
                    // Do NOT show the selection box yet
                }
            }

            // Update selection rectangle (only if Shift is held)
            if (isSelecting && Input.GetMouseButton(0) && !Input.GetKey(KeyCode.F) && Input.GetKey(KeyCode.LeftShift))
            {
                selectionEndScreen = Input.mousePosition;
                float dragDistance = (selectionEndScreen - selectionStartScreen).magnitude;

                if (dragDistance > minDragDistance)
                {
                    if (selectionBox == null)
                        CreateSelectionBox();
                    if (!selectionBox.activeSelf)
                        selectionBox.SetActive(true);
                    UpdateSelectionBox();
                }
                else
                {
                    if (selectionBox != null && selectionBox.activeSelf)
                        selectionBox.SetActive(false);
                }
            }
            else if (isSelecting && Input.GetKey(KeyCode.F))
            {
                // If F is pressed during selection, hide the box
                if (selectionBox != null && selectionBox.activeSelf)
                    selectionBox.SetActive(false);
            }

            // End selection (only if Shift was held)
            if (isSelecting && Input.GetMouseButtonUp(0))
            {
                isSelecting = false;
                if (selectionBox != null)
                    selectionBox.SetActive(false);

                float dragDistance = (selectionEndScreen - selectionStartScreen).magnitude;
                float heldTime = Time.unscaledTime - selectionStartTime;

                // Only perform rectangle selection if it was a drag, not snapping, and Shift was held
                if (!IsCurrentState(EditorState.MOVING_OBJECT) && !Input.GetKey(KeyCode.F) && heldTime > 0 && Input.GetKey(KeyCode.LeftShift))
                {
                    if (dragDistance >= minDragDistance && currentMode == Mode.Selection)
                    {
                        SelectObjectsInRectangle(selectionStartScreen, selectionEndScreen);
                    }
                    // else: short click already handled in Select Object region
                }
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
            if (gizmosRoot.activeSelf && currentSelectedObj)
            {
                gizmo.SetPosition(currentSelectedObj.transform.position);

                if (globalGizmosArrowsEnabled)
                {
                    gizmo.SetRotation(Quaternion.identity);
                }
                else
                {
                    gizmo.SetRotation(currentSelectedObj.transform.rotation);
                }

                gizmo.ScaleRelativeToCamera(currentSelectedObj.transform);
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
            if (!_isInitialized)
            {
                return;
            }

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
                else if (EditorUIManager.IsCurrentUIContext(EditorUIContext.UPGRADES_PANEL))
                {
                    UpgradesPanel.Instance.HideUpgradesPanel();
                    return;
                }
                else if (EditorUIManager.IsCurrentUIContext(EditorUIContext.SAVE_METADATA_PANEL))
                {
                    SaveMetadataPopup.Instance.OnCancelButtonClicked();
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

            #region Number Keys Shortcuts
            // 1: Enter building mode.
            if (EditorKeybinds.BuildingMode)
            {
                ChangeMode(Mode.Building);
            }
            // 2: Enter selection mode.
            else if (EditorKeybinds.SelectionMode)
            {
                ChangeMode(Mode.Selection);
            }
            // 3: Show/Hide all level waypoints.
            if (EditorKeybinds.ToggleWaypointsVisibility)
            {
                showAllWaypoints = !showAllWaypoints;
                if (multipleObjectsSelected)
                {
                    foreach (var obj in currentInstantiatedObjects)
                    {
                        if (!obj.canHaveWaypoints || !obj.gameObject.active) continue;
                        if (currentSelectedObjects.Contains(obj.gameObject)) continue;

                        foreach (var support in obj.GetComponents<WaypointSupport>())
                        {
                            // In case it's hiding, check if the user's selecting one of the waypoints of the object, skip it in that case.
                            if (!showAllWaypoints)
                            {
                                bool skipThisObject = false;
                                foreach (var waypoint in support.spawnedWaypoints)
                                {
                                    if (currentSelectedObjects.Contains(waypoint.gameObject)) skipThisObject = true; break;
                                }
                                if (skipThisObject) continue;
                            }

                            support.ShowWaypoints(showAllWaypoints);
                        }
                    }
                }
                else
                {
                    foreach (var obj in currentInstantiatedObjects)
                    {
                        if (!obj.canHaveWaypoints || !obj.gameObject.active) continue;
                        if (currentSelectedObj == obj) continue;

                        foreach (var support in obj.GetComponents<WaypointSupport>())
                        {
                            // In case it's hiding, check if the user's selecting one of the waypoints of the object, skip it in that case.
                            if (!showAllWaypoints)
                            {
                                bool skipThisObject = false;
                                foreach (var waypoint in support.spawnedWaypoints)
                                {
                                    if (currentSelectedObj == waypoint.gameObject) skipThisObject = true; break;
                                }
                                if (skipThisObject) continue;
                            }

                            support.ShowWaypoints(showAllWaypoints);
                        }
                    }
                }
            }
            // 4: Enable/disable global gizmos.
            if (EditorKeybinds.ToggleGlobalGizmos && currentMode == Mode.Selection && currentSelectedObj != null)
            {
                globalGizmosArrowsEnabled = !globalGizmosArrowsEnabled;
                gizmo.SetRotation(globalGizmosArrowsEnabled ? Quaternion.identity : currentSelectedObj.transform.rotation);

                // Show feedback to user
                string mode = globalGizmosArrowsEnabled ? "Global" : "Local";
                Utils.ShowCustomNotificationRed($"Switched to {mode} Gizmo Mode", 1.5f);
            }
            // 5: Exit existing level metadata.
            if (EditorKeybinds.ShowLevelMetadataPopup)
            {
                if (SaveMetadataPopup.Instance != null)
                {
                    SaveMetadataPopup.Instance.ShowPopup();
                }
                else
                {
                    Logger.Error("SaveMetadataPopup.Instance is null! Cannot show save popup.");
                }
            }
            // 6: Switch between Lit/Unlit lighting. EDITOR ONLY.
            if (EditorKeybinds.ToggleEditorLighting)
            {
                ToggleLighting();
            }
            // 7: Toggle waypoints rotation.
            if (EditorKeybinds.ToggleWaypointRotation)
            {
                waypointRotation = !waypointRotation;

                EditorUIManager.Instance.UpdateStatsLabel();
            }
            #endregion

            #region In-Editor Shortcuts (Object Manipulation)
            // Duplicate current selected object.
            if (EditorKeybinds.DuplicateCurrentObject)
            {
                DuplicateSelectedObject();
            }

            // Select all objects in the level.
            if (EditorKeybinds.SelectAllObjects && currentMode == Mode.Selection)
            {
                // Only select objects based on bulk selection mode, preserving active states
                var objectsToSelect = new List<GameObject>(currentInstantiatedObjects.Count);
                foreach (var obj in currentInstantiatedObjects)
                {
                    if (obj == null || obj.isDeleted)
                        continue;

                    if (obj is LE_Waypoint waypoint && waypoint.mainSupport.targetObject.isDeleted)
                        continue;

                    if (obj.objectType == LE_Object.ObjectType.PLAYER_SPAWN)
                        continue;

                    switch (currentBulkSelectionMode)
                    {
                        case BulkSelectionMode.ObjectsOnly:
                            if (obj is LE_Waypoint) continue;
                            break;
                        case BulkSelectionMode.WaypointsAndObjectsWithWaypoints:
                            if (!(obj is LE_Waypoint) && (obj.waypoints == null || obj.waypoints.Count == 0))
                                continue;
                            break;
                    }

                    objectsToSelect.Add(obj.gameObject);
                }

                if (objectsToSelect.Count > 0)
                    SetMultipleObjectsAsSelected(objectsToSelect);
                else
                    SetSelectedObj(null);
            }

            // Switch start spawn state for select object(s).
            if (EditorKeybinds.ToggleStartSpawnState && currentSelectedObj)
            {
                SelectedObjPanel.Instance.setActiveAtStartToggle.Set(!SelectedObjPanel.Instance.setActiveAtStartToggle.isChecked);
            }
            #endregion

            #region In-Editor Shortcuts
            // Save level data.
            if (EditorKeybinds.SaveLevel && levelHasBeenModified)
            {
                // Show "Saving..." notification immediately
                if (NotificationSystem.Instance != null)
                {
                    NotificationSystem.Instance.ShowNotification("Saving level...", "WhiteSquare");
                }

                // Check if level has metadata - if not, show metadata popup
                if (!LevelData.HasMetadata(levelFileNameWithoutExtension))
                {
                    if (SaveMetadataPopup.Instance != null)
                    {
                        SaveMetadataPopup.Instance.ShowPopup();
                    }
                    else
                    {
                        Logger.Error("SaveMetadataPopup.Instance is null! Cannot show save popup.");
                    }
                }
                else
                {
                    // Has metadata - just save directly, preserving existing metadata
                    LevelData.SaveLevelData(levelName, levelFileNameWithoutExtension);
                    levelHasBeenModified = false;

                    // Show "Saved!" notification after save completes
                    if (NotificationSystem.Instance != null)
                    {
                        NotificationSystem.Instance.ShowNotification("Level saved!", "WhiteSquare");
                    }
                }
            }

            // Enter playmode.
            if (EditorKeybinds.EnterPlaymode && !enteringPlayMode)
            {
                // Save data automatically.
                LevelData.SaveLevelData(levelName, levelFileNameWithoutExtension);

                EnterPlayMode();
            }
            #endregion

            #region UI Shortcuts
            // Show/Hide Help Panel.
            if (EditorKeybinds.ToggleHelpPanel)
            {
                EditorUIManager.Instance.ShowOrHideHelpPanel();
            }

            // Show/Hide category buttons in building UI.
            if (EditorKeybinds.HideOrShowCategoryButtons && currentMode == Mode.Building)
            {
                EditorObjectsToBuildUI.Instance.HideOrShowCategoryButtons();
            }

            // Show/Hide Global Properties Panel.
            if (EditorKeybinds.ToggleGlobalProperties)
            {
                GlobalPropertiesPanel.Instance.ShowOrHideGlobalPropertiesPanel();
            }
            #endregion

            #region Grid Shortcuts
            // Toggle grid visibility.
            if (EditorKeybinds.ToggleGridVisibility)
            {
                SetGridVisible(!gridVisible);
            }

            // Toggle grid enabled AND visibility
            if (EditorKeybinds.ToggleGridState)
            {
                bool newState = !gridEnabled;
                SetGridEnabled(newState);
                SetGridVisible(newState);
            }

            // Adjust grid size/height.
            if (EditorKeybinds.AllowedToAdjustGrid)
            {
                if (EditorKeybinds.ChangeGridSize(out float scrollDelta))
                {
                    if (scrollDelta < 0)
                        DecreaseGridSize(); // Finer
                    else if (scrollDelta > 0)
                        IncreaseGridSize(); // Coarser

                    EditorUIManager.Instance.UpdateStatsLabel();
                }
                if (EditorKeybinds.ChangeGridHeight(out scrollDelta))
                {
                    AdjustGridHeight(scrollDelta, EditorKeybinds.AdjustGridSizePrecisly);
                }
            }
            #endregion

            ManageObjectRotationShortcuts();
        }
        void ManageMoveObjectShortcuts()
        {
            GameObject targetObj = currentMode == Mode.Building ? previewObjectToBuildObj : currentSelectedObj;
            if (targetObj == null) return;

            float moveAmount = gridEnabled ? gridSize : 0.01f;
            Vector3 toMove = Vector3.zero;
            bool movingY = false;

            #region Get Camera Directions
            // Get camera-relative directions (projected onto XZ plane for horizontal movement)
            Vector3 cameraForward = Camera.main.transform.forward;
            Vector3 cameraRight = Camera.main.transform.right;

            // Project onto horizontal plane (XZ)
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();
            #endregion

            #region Get Moving Vector By Input
            // Arrow keys - move in camera-relative directions
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                toMove = -cameraRight * moveAmount;
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                toMove = cameraRight * moveAmount;
            }
            else if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                toMove = cameraForward * moveAmount;
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                toMove = -cameraForward * moveAmount;
            }
            // Mouse 4/5 for vertical movement (Y axis only)
            else if (Input.GetKeyDown(KeyCode.Mouse4))
            {
                toMove = Vector3.up * moveAmount;
                movingY = true;
            }
            else if (Input.GetKeyDown(KeyCode.Mouse3))
            {
                toMove = Vector3.down * moveAmount;
                movingY = true;
            }
            else
            {
                return;
            }
            #endregion

            #region Move The Object
            Vector3 oldPosition = targetObj.transform.localPosition;
            Vector3 newPosition = targetObj.transform.localPosition + toMove;

            if (gridEnabled)
            {
                if (movingY)
                {
                    newPosition.y = Mathf.Round(newPosition.y / gridSize) * gridSize;
                }
                else
                {
                    newPosition.x = Mathf.Round(newPosition.x / gridSize) * gridSize;
                    newPosition.z = Mathf.Round(newPosition.z / gridSize) * gridSize;
                }
            }

            targetObj.transform.localPosition = newPosition;
            #endregion

            #region Register Action If A Selected Object
            // The moving object is an already instantiated obj, which is selected.
            if (targetObj == currentSelectedObj)
            {
                // Register the action and cleanup
                RegisterLEAction(LEAction.LEActionType.MoveObject, targetObj, multipleObjectsSelected,
                    oldPosition, newPosition);

                SelectedObjPanel.Instance.UpdateGlobalObjectAttributes(targetObj.transform);

                levelHasBeenModified = true;
            }
            #endregion
        }

        // --- Snap Euler Angles Helper ---
        public static Vector3 SnapEulerAnglesToStep(Vector3 euler, float step)
        {
            // Always snap to the nearest multiple of step, in [0,360)
            float Snap(float angle)
            {
                float snapped = Mathf.Round(angle / step) * step;
                // Normalize to [0, 360)
                snapped = snapped % 360f;
                if (snapped < 0) snapped += 360f;
                return snapped;
            }
            return new Vector3(
                Snap(euler.x),
                Snap(euler.y),
                Snap(euler.z)
            );
        }

        #region Rotation Shit
        private float lastRotationTime = 0f;
        private float rotationRepeatDelay = 0.12f;
        private float rotationHoldDelay = 0.3f;

        private Coroutine rotationCoroutine;
        private Coroutine previewRotationCoroutine;

        void ManageObjectRotationShortcuts()
        {
            GameObject targetObj = currentMode == Mode.Building ? previewObjectToBuildObj : currentSelectedObj;
            if (targetObj == null) return;

            // Prevent rotation when snapping to a trigger in building mode
            if (currentMode == Mode.Building && currentHittenSnapTrigger != null)
            {
                return;
            }

            // Check if R is being held (not just pressed once)
            bool isRotating = Input.GetKey(KeyCode.R);

            // Reset timing when key is released
            if (!isRotating)
            {
                return;
            }

            // For initial press, always trigger immediately
            bool shouldRotate = Input.GetKeyDown(KeyCode.R);

            // For continuous rotation while holding, apply the delay
            if (!shouldRotate && isRotating)
            {
                float timeSinceFirstPress = Time.unscaledTime - lastRotationTime;

                // Only start auto-rotating after the hold delay has passed
                if (timeSinceFirstPress >= rotationHoldDelay)
                {
                    // Check if enough time has passed since last rotation for the repeat
                    float timeSinceLastRotation = (Time.unscaledTime - lastRotationTime) - rotationHoldDelay;
                    if (timeSinceLastRotation >= rotationRepeatDelay)
                    {
                        if (currentMode == Mode.Building)
                        {
                            shouldRotate = previewRotationCoroutine == null;
                        }
                        else
                        {
                            shouldRotate = rotationCoroutine == null;
                        }
                    }
                }
            }

            if (shouldRotate)
            {
                // Only reset time on the FIRST press, not during auto-rotation
                if (Input.GetKeyDown(KeyCode.R))
                {
                    lastRotationTime = Time.unscaledTime;
                }

                bool reset = Input.GetKey(KeyCode.LeftControl);
                float angleStep = 15f * (Input.GetKey(KeyCode.T) ? -1f : 1f);

                // Determine which axis to rotate around
                bool rotateX = Input.GetKey(KeyCode.LeftShift);
                bool rotateZ = !rotateX && Input.GetKey(KeyCode.LeftAlt);
                bool rotateY = !rotateX && !rotateZ; // default

                if (currentMode == Mode.Building)
                {
                    // PREVIEW MODE: smooth rotation
                    Vector3 oldOffset = previewRotationOffsetEuler;
                    Vector3 newOffset;

                    if (reset)
                    {
                        newOffset = Vector3.zero;
                    }
                    else
                    {
                        newOffset = oldOffset;
                        if (rotateX) newOffset.x = oldOffset.x + angleStep;
                        else if (rotateZ) newOffset.z = oldOffset.z + angleStep;
                        else /* Y */ newOffset.y = oldOffset.y + angleStep;
                    }

                    // Start smooth rotation for preview
                    StartPreviewRotationCoroutine(oldOffset, newOffset);
                    return;
                }

                // SELECTION MODE (actual placed objects) -> smooth rotate
                Quaternion oldRotation = targetObj.transform.rotation;
                Quaternion delta;

                if (reset)
                {
                    Quaternion upright = Quaternion.identity;
                    StartRotationCoroutine(targetObj, oldRotation, upright);
                    //RotateWaypointsWithObject(targetObj, oldRotation, upright);
                    return;
                }

                if (rotateY)
                {
                    delta = Quaternion.AngleAxis(angleStep, Vector3.up);
                    StartRotationCoroutine(targetObj, oldRotation, delta * oldRotation);
                    //RotateWaypointsWithObject(targetObj, oldRotation, delta * oldRotation);
                }
                else if (rotateX)
                {
                    delta = Quaternion.AngleAxis(angleStep, Vector3.right);
                    StartRotationCoroutine(targetObj, oldRotation, oldRotation * delta);
                    //RotateWaypointsWithObject(targetObj, oldRotation, oldRotation * delta);
                }
                else if (rotateZ)
                {
                    delta = Quaternion.AngleAxis(angleStep, Vector3.forward);
                    StartRotationCoroutine(targetObj, oldRotation, oldRotation * delta);
                    //RotateWaypointsWithObject(targetObj, oldRotation, oldRotation * delta);
                }
            }
        }
        void StartPreviewRotationCoroutine(Vector3 oldOffset, Vector3 newOffset)
        {
            if (previewRotationCoroutine != null)
            {
                MelonCoroutines.Stop(previewRotationCoroutine);
                previewRotationCoroutine = null;
            }
            previewRotationCoroutine = (Coroutine)MelonCoroutines.Start(SmoothRotatePreview(oldOffset, newOffset));
        }
        IEnumerator SmoothRotatePreview(Vector3 oldOffset, Vector3 newOffset)
        {
            // Calculate rotation angle for consistent speed
            float angle = Vector3.Angle(Quaternion.Euler(oldOffset) * Vector3.forward, Quaternion.Euler(newOffset) * Vector3.forward);
            float degreesPerSecond = 720f; // Same speed as placed objects
            float duration = angle / degreesPerSecond;
            duration = Mathf.Clamp(duration, 0.08f, 0.25f); // Keep within reasonable bounds

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float dt = Time.unscaledDeltaTime;
                // Prevent large jumps at very low FPS but still progress
                if (dt > 0.05f) dt = 0.05f;
                elapsed += dt;
                float t = Mathf.Clamp01(elapsed / duration);
                // Smoothstep for nice easing
                t = t * t * (3f - 2f * t);

                // Interpolate the Euler angles
                previewRotationOffsetEuler = Vector3.Lerp(oldOffset, newOffset, t);
                yield return null;
            }

            previewRotationOffsetEuler = newOffset;
            previewRotationCoroutine = null;
        }
        void StartRotationCoroutine(GameObject obj, Quaternion oldRot, Quaternion newRot)
        {
            if (rotationCoroutine != null)
            {
                MelonCoroutines.Stop(rotationCoroutine);
                rotationCoroutine = null;
            }
            rotationCoroutine = (Coroutine)MelonCoroutines.Start(SmoothRotate(obj, oldRot, newRot));
        }
        IEnumerator SmoothRotate(GameObject obj, Quaternion oldRotation, Quaternion newRotation)
        {
            if (!waypointRotation) AttachWaypointsFromObject(obj, false);

            // Adaptive duration based on angle
            float angle = Quaternion.Angle(oldRotation, newRotation);
            float degreesPerSecond = 720f;
            float duration = angle / degreesPerSecond;
            duration = Mathf.Clamp(duration, 0.08f, 0.25f);

            float elapsed = 0f;

            while (elapsed < duration)
            {
                float dt = Time.unscaledDeltaTime;

                // At very low FPS, still allow reasonable progress
                // but don't let a single frame complete the entire animation
                dt = Mathf.Min(dt, duration * 0.5f);

                elapsed += dt;
                float t = Mathf.Clamp01(elapsed / duration);

                // Smoothstep
                t = t * t * (3f - 2f * t);

                // CRITICAL: Slerp from CURRENT rotation to target
                // This prevents jumps if the coroutine restarts
                obj.transform.rotation = Quaternion.Slerp(obj.transform.rotation, newRotation, t);

                yield return null;
            }

            // Ensure we end exactly at target
            obj.transform.rotation = newRotation;

            if (currentMode != Mode.Building && currentSelectedObj != null)
            {
                SelectedObjPanel.Instance.UpdateGlobalObjectAttributes(obj.transform);
                RegisterLEAction(LEAction.LEActionType.RotateObject, obj, multipleObjectsSelected,
                                 null, null, oldRotation, newRotation, waypointRotation: waypointRotation);
            }

            if (!waypointRotation) AttachWaypointsFromObject(obj, true);

            rotationCoroutine = null;
        }
        #endregion

        // Helper to rotate waypoints with their parent object
        void RotateWaypointsWithObject(GameObject parentObj, Quaternion oldRot, Quaternion newRot)
        {
            var leObj = parentObj.GetComponent<LE_Object>();
            if (leObj == null || leObj.waypoints == null || leObj.waypoints.Count == 0)
                return;

            var supports = parentObj.GetComponents<WaypointSupport>();
            float rotationStep = 15f;

            foreach (var support in supports)
            {
                foreach (var waypoint in support.spawnedWaypoints)
                {
                    var t = waypoint.transform;
                    // Calculate the rotation delta in Euler angles
                    Vector3 oldEuler = t.localEulerAngles;
                    Quaternion deltaRot = newRot * Quaternion.Inverse(oldRot);
                    Vector3 deltaEuler = deltaRot.eulerAngles;

                    // Snap delta to nearest 15 deg
                    deltaEuler.x = Mathf.Round(deltaEuler.x / rotationStep) * rotationStep;
                    deltaEuler.y = Mathf.Round(deltaEuler.y / rotationStep) * rotationStep;
                    deltaEuler.z = Mathf.Round(deltaEuler.z / rotationStep) * rotationStep;

                    // Add delta to current waypoint rotation
                    Vector3 newEuler = oldEuler + deltaEuler;
                    t.localEulerAngles = newEuler;
                }
            }
        }

        public void AttachWaypointsFromObject(GameObject mainObj, bool attach)
        {
            if (mainObj == multipleSelectedObjsParent)
            {
                foreach (var obj in currentSelectedObjects)
                {
                    foreach (var support in obj.GetComponents<WaypointSupport>())
                    {
                        foreach (var waypoint in support.spawnedWaypoints)
                        {
                            waypoint.transform.parent = attach ? waypoint.objectParent : null;
                        }
                    }
                }
            }
            else
            {
                if (!mainObj.TryGetComponent<LE_Object>(out _)) return;

                foreach (var support in mainObj.GetComponents<WaypointSupport>())
                {
                    foreach (var waypoint in support.spawnedWaypoints)
                    {
                        waypoint.transform.parent = attach ? waypoint.objectParent : null;
                    }
                }
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

            // Clean up any ongoing preview rotation when changing modes
            if (previewRotationCoroutine != null)
            {
                MelonCoroutines.Stop(previewRotationCoroutine);
                previewRotationCoroutine = null;
            }

            switch (currentMode)
            {
                case Mode.Building:
                    // Only enable the panel if the keybinds help panel is DISABLED.
                    if (EditorUIManager.IsCurrentUIContext(EditorUIContext.NORMAL))
                    {
                        EditorObjectsToBuildUI.Instance.root.SetActive(true);
                        SelectedObjPanel.Instance.gameObject.SetActive(false);
                    }
                    break;

                case Mode.Selection:
                    EditorObjectsToBuildUI.Instance.root.SetActive(false);
                    SelectedObjPanel.Instance.gameObject.SetActive(EditorUIManager.IsCurrentUIContext(EditorUIContext.NORMAL)); // Only when normal.
                    break;
            }

            if (currentMode == Mode.Selection)
            {
                // Only enable gizmos if there's a selected object.
                if (currentSelectedObj) gizmosRoot.SetActive(true);
            }
            else
            {
                gizmosRoot.SetActive(false);
            }

            Logger.Log("Changed LE mode to: " + currentMode);
            EditorUIManager.Instance.SetCurrentModeLabelText(currentMode);
        }

        #region Bulk Selection
        void CreateSelectionBox()
        {
            if (selectionBox != null) return;

            selectionBox = new GameObject("SelectionBox");
            selectionBox.transform.parent = EditorUIManager.Instance.editorUIParent.transform;
            selectionBox.transform.localPosition = Vector3.zero;
            selectionBox.transform.localScale = Vector3.one;

            selectionBoxSprite = selectionBox.AddComponent<UISprite>();

            selectionBoxSprite.atlas = NGUI_Utils.UITexturesAtlas;
            selectionBoxSprite.spriteName = "Square_Border_HighOpacity";
            selectionBoxSprite.type = UIBasicSprite.Type.Sliced;
            selectionBoxSprite.color = new Color(0.218f, 0.6464f, 0.6509f, 0.5f);
            selectionBoxSprite.depth = 9999;
            selectionBoxSprite.pivot = UIWidget.Pivot.TopLeft;
            selectionBoxSprite.width = 100;
            selectionBoxSprite.height = 100;

            UICamera uiCam = UICamera.list[0];
            if (uiCam != null)
            {
                selectionBox.layer = uiCam.gameObject.layer;
            }

            selectionBox.SetActive(false);
        }
        private void UpdateSelectionBox()
        {
            if (selectionBox == null) return;

            // Get screen positions
            Vector2 start = selectionStartScreen;
            Vector2 end = selectionEndScreen;

            // Calculate min/max for width/height
            float minX = Mathf.Min(start.x, end.x);
            float maxX = Mathf.Max(start.x, end.x);
            float minY = Mathf.Min(start.y, end.y);
            float maxY = Mathf.Max(start.y, end.y);

            // Convert screen coordinates to NGUI world space
            Vector3 topLeftScreen = new Vector3(minX, maxY, 0f);
            Vector3 bottomRightScreen = new Vector3(maxX, minY, 0f);

            // Use the main menu camera for NGUI
            Camera uiCamera = NGUI_Utils.mainMenuCamera;
            Transform uiParent = EditorUIManager.Instance.editorUIParent.transform;

            Vector3 topLeftWorld = uiCamera.ScreenToWorldPoint(topLeftScreen);
            Vector3 bottomRightWorld = uiCamera.ScreenToWorldPoint(bottomRightScreen);

            Vector3 topLeftLocal = uiParent.InverseTransformPoint(topLeftWorld);
            Vector3 bottomRightLocal = uiParent.InverseTransformPoint(bottomRightWorld);

            // Set position (top-left corner)
            selectionBox.transform.localPosition = topLeftLocal;

            // Calculate and set size (do NOT apply any scale factor)
            selectionBoxSprite.width = Mathf.RoundToInt(Mathf.Abs(bottomRightLocal.x - topLeftLocal.x));
            selectionBoxSprite.height = Mathf.RoundToInt(Mathf.Abs(bottomRightLocal.y - topLeftLocal.y));
        }
        public void SetBulkSelectionMode(BulkSelectionMode mode)
        {
            currentBulkSelectionMode = mode;
        }
        public BulkSelectionMode GetBulkSelectionMode()
        {
            return currentBulkSelectionMode;
        }
        private void SelectObjectsInRectangle(Vector2 start, Vector2 end)
        {
            // Calculate selection rectangle boundaries
            float minX = Mathf.Min(start.x, end.x);
            float maxX = Mathf.Max(start.x, end.x);
            float minY = Mathf.Min(start.y, end.y);
            float maxY = Mathf.Max(start.y, end.y);

            // Check if selection rectangle is too small
            float width = maxX - minX;
            float height = maxY - minY;
            if (width < minDragDistance && height < minDragDistance)
            {
                SetSelectedObj(null);
                return;
            }

            Camera cam = Camera.main;
            var selectedObjects = new List<GameObject>();

            foreach (var obj in currentInstantiatedObjects)
            {
                if (obj == null || obj.isDeleted || !obj.gameObject.activeSelf)
                    continue;

                // Filter by bulk selection mode
                switch (currentBulkSelectionMode)
                {
                    case BulkSelectionMode.ObjectsOnly:
                        if (obj is LE_Waypoint) continue;
                        break;
                    case BulkSelectionMode.WaypointsAndObjectsWithWaypoints:
                        if (!(obj is LE_Waypoint) && (obj.waypoints == null || obj.waypoints.Count == 0))
                            continue;
                        break;
                }

                // Get all renderers for this object
                var renderers = obj.gameObject.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) continue;

                // Frustum culling first
                Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(cam);
                Bounds? combinedBounds = null;
                foreach (var renderer in renderers)
                {
                    if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                        continue;
                    if (combinedBounds == null)
                        combinedBounds = renderer.bounds;
                    else
                        combinedBounds.Value.Encapsulate(renderer.bounds);
                }
                if (combinedBounds == null || !GeometryUtility.TestPlanesAABB(frustumPlanes, combinedBounds.Value))
                    continue;

                // Sample mesh vertices and project to screen space
                bool isInSelection = false;
                foreach (var renderer in renderers)
                {
                    if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                        continue;

                    MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
                    if (meshFilter == null || meshFilter.sharedMesh == null)
                        continue;

                    Mesh mesh = meshFilter.sharedMesh;
                    Transform transform = renderer.transform;
                    Vector3[] vertices = mesh.vertices;

                    // Sample vertices (use step for performance on large meshes)
                    int step = Mathf.Max(1, vertices.Length / 100);
                    for (int i = 0; i < vertices.Length; i += step)
                    {
                        Vector3 worldPos = transform.TransformPoint(vertices[i]);
                        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

                        // Check if vertex is in front of camera and inside rectangle
                        if (screenPos.z > 0 &&
                            screenPos.x >= minX && screenPos.x <= maxX &&
                            screenPos.y >= minY && screenPos.y <= maxY)
                        {
                            isInSelection = true;
                            break;
                        }
                    }

                    if (isInSelection)
                        break;
                }

                if (isInSelection)
                {
                    selectedObjects.Add(obj.gameObject);
                }
            }

            if (selectedObjects.Count == 0)
            {
                SetSelectedObj(null);
            }
            else if (selectedObjects.Count == 1)
            {
                SetSelectedObj(selectedObjects[0]);
            }
            else
            {
                SetMultipleObjectsAsSelected(selectedObjects);
            }
        }
        #endregion

        #region Grid
        void UpdateGridCenter()
        {
            // Center grid on all objects, or at (0, gridHeight, 0) if none
            if (currentInstantiatedObjects.Count > 0)
            {
                Vector3 sum = Vector3.zero;
                int count = 0;
                foreach (var obj in currentInstantiatedObjects)
                {
                    if (obj != null && obj.gameObject.activeSelf)
                    {
                        sum += obj.transform.position;
                        count++;
                    }
                }
                gridCenter = (count > 0) ? (sum / count) : Vector3.zero;
            }
            else
            {
                gridCenter = Vector3.zero;
            }
            gridCenter.y = gridHeight;
        }
        public void SetGridSize(float newSize)
        {
            gridSize = Mathf.Clamp(newSize, MIN_GRID_SIZE, MAX_GRID_SIZE);
            UpdateGridCenter();

            // Show notification
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowNotification($"Grid size: {gridSize:0.###}", "WhiteSquare");
            }
        }
        public void IncreaseGridSize()
        {
            int level = Mathf.RoundToInt(Mathf.Log(gridSize, GRID_SIZE_MULTIPLIER)) + 1;
            SetGridSize(Mathf.Pow(GRID_SIZE_MULTIPLIER, level));
        }
        public void DecreaseGridSize()
        {
            int level = Mathf.RoundToInt(Mathf.Log(gridSize, GRID_SIZE_MULTIPLIER)) - 1;
            SetGridSize(Mathf.Pow(GRID_SIZE_MULTIPLIER, level));
        }
        public void SetGridHeight(float newHeight)
        {
            gridHeight = newHeight;
            UpdateGridCenter();

            // Show notification
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowNotification($"Grid height: {gridHeight:0.###}", "WhiteSquare");
            }
        }
        public void AdjustGridHeight(float delta, bool precise)
        {
            gridHeight += precise ? delta * 0.1f : delta;
            UpdateGridCenter();

            // Show notification
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowNotification($"Grid height: {gridHeight:0.###}", "WhiteSquare");
            }
        }
        public void SetGridEnabled(bool enabled)
        {
            gridEnabled = enabled;
            // Hide grid if disabled
            if (!gridEnabled)
                SetGridVisible(false);
        }
        public void SetGridVisible(bool visible)
        {
            // Allow toggling visibility even if grid is disabled
            gridVisible = visible;
            // Re-enable grid if becoming visible
            if (visible && !gridEnabled)
            {
                gridEnabled = true;
            }
        }
        public float GetGridSize()
        {
            return gridSize;
        }
        #endregion

        void PreviewObject()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            List<RaycastHit> hits = Physics.RaycastAll(ray, Mathf.Infinity, -1, QueryTriggerInteraction.Collide).ToList();
            hits.Sort((hit1, hit2) => hit1.distance.CompareTo(hit2.distance));
            bool snapWithTrigger = false;
            RaycastHit rayToUseWithSnap = new RaycastHit();
            bool theyAreAllSnapTriggers = hits.All(hit => hit.collider.gameObject.name.StartsWith(SNAP_TRIGGERS_NAME));
            Quaternion baseRotation = previewObjectToBuildObj ? previewObjectToBuildObj.transform.rotation : Quaternion.identity;

            // Check if grid plane should take priority
            bool shouldUseGrid = false;
            float gridPlaneDistance = float.MaxValue;
            if (gridEnabled && gridVisible)
            {
                Plane gridPlane = new Plane(Vector3.up, new Vector3(0, gridHeight, 0));
                float enter = 0f;
                if (gridPlane.Raycast(ray, out enter))
                {
                    gridPlaneDistance = enter;
                    // If grid is closer than any object hit, or if grid is above the closest hit point, use grid
                    if (hits.Count == 0 || gridPlaneDistance < hits[0].distance || gridHeight > hits[0].point.y)
                    {
                        shouldUseGrid = true;
                    }
                }
            }

            // If grid should take priority, place on grid immediately
            if (shouldUseGrid)
            {
                previewObjectToBuildObj.SetActive(true);
                Vector3 gridPoint = ray.GetPoint(gridPlaneDistance);
                gridPoint.x = Mathf.Round(gridPoint.x / gridSize) * gridSize;
                gridPoint.y = gridHeight;
                gridPoint.z = Mathf.Round(gridPoint.z / gridSize) * gridSize;
                previewObjectToBuildObj.transform.position = gridPoint;
                baseRotation = Quaternion.identity;
                currentHittenSnapTrigger = null;

                // Apply user rotation offset
                previewObjectToBuildObj.transform.rotation = baseRotation * Quaternion.Euler(previewRotationOffsetEuler);
                return;
            }

            if (hits.Count > 0)
            {
                // Handle snap triggers first
                if (hits.Count == 1 || theyAreAllSnapTriggers)
                {
                    if (hits[0].collider.gameObject.name.StartsWith(SNAP_TRIGGERS_NAME))
                    {
                        if (currentObjectToBuildType.HasValue && CanUseCaughtSnapToGridTrigger(currentObjectToBuildType.Value, hits[0].collider.gameObject))
                        {
                            snapWithTrigger = true;
                            rayToUseWithSnap = hits[0];
                        }
                        hits.RemoveAll(hit => hit.collider.gameObject.name.StartsWith(SNAP_TRIGGERS_NAME));
                    }
                    else
                    {
                        hits.RemoveAll(hit => hit.collider.gameObject.name.StartsWith(SNAP_TRIGGERS_NAME));
                    }
                }
                else
                {
                    foreach (var hit in hits.ToList())
                    {
                        if (hit.collider.gameObject.name.StartsWith(SNAP_TRIGGERS_NAME) && Input.GetKey(KeyCode.LeftControl))
                        {
                            if (currentObjectToBuildType.HasValue && CanUseCaughtSnapToGridTrigger(currentObjectToBuildType.Value, hit.collider.gameObject))
                            {
                                snapWithTrigger = true;
                                rayToUseWithSnap = hit;
                                break;
                            }
                        }
                        else
                        {
                            hits.RemoveAll(hit => hit.collider.gameObject.name.StartsWith(SNAP_TRIGGERS_NAME));
                            break;
                        }
                    }
                }
                if (snapWithTrigger)
                {
                    previewObjectToBuildObj.SetActive(true);
                    previewObjectToBuildObj.transform.position = rayToUseWithSnap.collider.transform.position;

                    // Only apply trigger rotation when hitting a NEW trigger, otherwise preserve current rotation
                    if (currentHittenSnapTrigger != rayToUseWithSnap.collider.gameObject)
                    {
                        currentHittenSnapTrigger = rayToUseWithSnap.collider.gameObject;
                        // Apply trigger rotation and reset user rotation offset when snapping to a new trigger
                        baseRotation = rayToUseWithSnap.collider.transform.rotation;
                        previewRotationOffsetEuler = Vector3.zero; // Reset user rotation when snapping
                    }
                    else
                    {
                        // When staying on the same trigger, use the trigger's rotation (no user modifications allowed)
                        baseRotation = rayToUseWithSnap.collider.transform.rotation;
                    }
                }
                else if (hits.Count > 0)
                {
                    // Only consider colliders that are children of objects in the levelObjectsParent
                    var surfaceHit = hits.FirstOrDefault(h =>
                        h.collider is Collider &&
                        !(h.collider is TerrainCollider) &&
                        h.collider.transform.IsChildOf(levelObjectsParent.transform) &&
                        h.collider.enabled &&
                        h.collider.gameObject.activeInHierarchy
                    );
                    if (surfaceHit.collider != null)
                    {
                        currentHittenSnapTrigger = null;
                        previewObjectToBuildObj.SetActive(true);
                        previewObjectToBuildObj.transform.position = surfaceHit.point;
                        Vector3 normal = surfaceHit.normal;
                        if (normal != Vector3.up && normal != Vector3.down)
                        {
                            Vector3 right = Vector3.Cross(Vector3.up, normal).normalized;
                            Vector3 up = Vector3.Cross(normal, right).normalized;
                            baseRotation = Quaternion.LookRotation(up, normal);
                        }
                        else
                        {
                            if (normal == Vector3.up)
                                baseRotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
                            else
                                baseRotation = Quaternion.LookRotation(Vector3.back, Vector3.down);
                        }
                    }
                    else
                    {
                        currentHittenSnapTrigger = null;
                        previewObjectToBuildObj.SetActive(true);
                        previewObjectToBuildObj.transform.position = hits[0].point;
                        if (lastHittenNormalByPreviewRay != hits[0].normal)
                        {
                            lastHittenNormalByPreviewRay = hits[0].normal;
                            Vector3 wallNormal = hits[0].normal;
                            if (wallNormal != Vector3.up && wallNormal != Vector3.down)
                            {
                                Vector3 right = Vector3.Cross(Vector3.up, wallNormal).normalized;
                                Vector3 up = Vector3.Cross(wallNormal, right).normalized;
                                baseRotation = Quaternion.LookRotation(up, wallNormal);
                            }
                            else
                            {
                                if (wallNormal == Vector3.up)
                                {
                                    baseRotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
                                }
                                else
                                {
                                    baseRotation = Quaternion.LookRotation(Vector3.back, Vector3.down);
                                }
                            }
                        }
                        else
                        {
                            baseRotation = previewObjectToBuildObj.transform.rotation; // keep last
                        }
                    }
                }
                else // If nothing is hit, place on grid
                {
                    if (gridEnabled && gridVisible)
                    {
                        Plane gridPlane = new Plane(Vector3.up, new Vector3(0, gridHeight, 0));
                        float enter = 0f;
                        if (gridPlane.Raycast(ray, out enter))
                        {
                            previewObjectToBuildObj.SetActive(true);
                            Vector3 gridPoint = ray.GetPoint(enter);
                            gridPoint.x = Mathf.Round(gridPoint.x / gridSize) * gridSize;
                            gridPoint.y = gridHeight;
                            gridPoint.z = Mathf.Round(gridPoint.z / gridSize) * gridSize;
                            previewObjectToBuildObj.transform.position = gridPoint;
                            baseRotation = Quaternion.identity;
                        }
                        else
                        {
                            previewObjectToBuildObj.SetActive(false);
                            return;
                        }
                    }
                    else
                    {
                        previewObjectToBuildObj.SetActive(false);
                        return;
                    }
                }
            }
            else // If nothing is hit, place on grid
            {
                if (gridEnabled && gridVisible)
                {
                    Plane gridPlane = new Plane(Vector3.up, new Vector3(0, gridHeight, 0));
                    float enter = 0f;
                    if (gridPlane.Raycast(ray, out enter))
                    {
                        previewObjectToBuildObj.SetActive(true);
                        Vector3 gridPoint = ray.GetPoint(enter);
                        gridPoint.x = Mathf.Round(gridPoint.x / gridSize) * gridSize;
                        gridPoint.y = gridHeight;
                        gridPoint.z = Mathf.Round(gridPoint.z / gridSize) * gridSize;
                        previewObjectToBuildObj.transform.position = gridPoint;
                        baseRotation = Quaternion.identity;
                    }
                    else
                    {
                        previewObjectToBuildObj.SetActive(false);
                        return;
                    }
                }
                else
                {
                    previewObjectToBuildObj.SetActive(false);
                    return;
                }
            }
            // Finally apply user rotation offset (so manual rotations persist across alignment updates)
            // But only when NOT snapping to a trigger
            if (previewObjectToBuildObj.activeInHierarchy)
            {
                if (currentHittenSnapTrigger != null)
                {
                    // When snapping, use only the trigger's rotation (no user offset)
                    previewObjectToBuildObj.transform.rotation = baseRotation;
                }
                else
                {
                    // When not snapping, apply user rotation offset
                    previewObjectToBuildObj.transform.rotation = baseRotation * Quaternion.Euler(previewRotationOffsetEuler);
                }
            }
        }

        void InstanceObjectInThePreviewObjectPos()
        {
            levelHasBeenModified = true;

            // ONly set the "default object scale" when placing it.
            Vector3 objScale = LE_Object.defaultScalesForObjects.ContainsKey(currentObjectToBuildType) ?
                LE_Object.defaultScalesForObjects[currentObjectToBuildType] : Vector3.one;
            PlaceObject(currentObjectToBuildType, previewObjectToBuildObj.transform.localPosition, previewObjectToBuildObj.transform.localEulerAngles, objScale, true);

            // About the scale being fixed to 1... you can't change the scale of the PREVIEW object, so...
        }
        public GameObject PlaceObject(LE_Object.ObjectType? objectType, Vector3 position, Vector3 eulerAngles, Vector3 scale, bool setAsSelected = true)
        {
            if (setAsSelected)
            {
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

            if (obj && obj.name == gizmosRoot.name)
            {
                Logger.Error("HOW THE FUCK DID YOU MANAGE TO SELECT THE FUCKING GIZMOS ARROWS!? Anyways, this shouldn't case any trouble now :)");
                return;
            }

            if (obj) Logger.DebugLog($"SetSelectedObj called for object with name: \"{obj.name}\".");
            else Logger.DebugLog($"SetSelectedObj called with NO NEW TARGET OBJECT (To deselect).");

            LE_Object objComp = null;

            if (obj && obj != multipleSelectedObjsParent && !obj.TryGetComponent<LE_Object>(out objComp))
            {
                Logger.Error($"Illegal object selected! Name: {obj.name}, path: {obj.GetGameObjectPath(">")}");
                // Idk either mate.
                return;
            }

            gizmosRoot.SetActive(false);

            // SnapToGrid cube is adjusted in Late Update.

            // Reset the last selected object color back to normal.
            if (currentSelectedObj != null)
            {
                if (multipleObjectsSelected)
                {
                    foreach (var @object in currentSelectedObjsComponents)
                    {
                        @object.SetObjectColor(LE_Object.LEObjectContext.NORMAL);
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
                    if (!currentSelectedObjects.Contains(currentSelectedObj))
                    {
                        currentSelectedObjects.Add(currentSelectedObj);
                        currentSelectedObjsComponents.Add(currentSelectedObjComponent);
                    }
                }
                // And add the most recent now, ofc lol (but only if it hasn't been selected yet).
                if (!currentSelectedObjects.Contains(obj))
                {
                    currentSelectedObjects.Add(obj);
                    currentSelectedObjsComponents.Add(objComp);
                }
                else // If the object is already in the list, DEselect it:
                {
                    currentSelectedObjects.Remove(obj);
                    currentSelectedObjsComponents.Remove(objComp);
                    obj.transform.parent = objComp.objectParent; // Remove the object from the multipleSelectedObjsParent.
                    objComp.OnDeselect(null);
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
                    currentSelectedObjsComponents.ForEach(x => x.transform.parent = x.objectParent);
                    multipleSelectedObjsParent.transform.localScale = Vector3.one;
                    multipleSelectedObjsParent.transform.position = centeredPosition;
                    multipleSelectedObjsParent.transform.rotation = Quaternion.identity;
                    currentSelectedObjects.ForEach(x => x.transform.parent = multipleSelectedObjsParent.transform);

                    // The "main" selected object now is the parent of the selected objects.
                    currentSelectedObj = multipleSelectedObjsParent;

                    Logger.DebugLog($"Adding \"{obj.name}\" to the multiple selected objects.");

                    #region Set Current Selected Obj Component
                    multipleObjectsOfTheSameTypeSelected = LE_Object.ObjectsAreOfTheSameType(currentSelectedObjsComponents);

                    // If the obj types diffier, set the component as null.
                    if (!multipleObjectsOfTheSameTypeSelected)
                    {
                        if (currentSelectedObjComponent != null) currentSelectedObjComponent.OnDeselect(null);
                        currentSelectedObjComponent = null;
                    }
                    else // Otherwise, get the component from the first element in the list.
                    {
                        currentSelectedObjComponent = currentSelectedObjsComponents[0];
                    }
                    #endregion
                }
                else
                {
                    multipleObjectsSelected = false;
                    multipleObjectsOfTheSameTypeSelected = false;

                    currentSelectedObj = obj;
                    currentSelectedObjComponent = objComp;
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
                        currentSelectedObjsComponents.ForEach(x => x.transform.parent = x.objectParent);
                        currentSelectedObjsComponents.ForEach(x => x.OnDeselect(obj));
                        currentSelectedObjects.Clear();
                        currentSelectedObjsComponents.Clear();
                    }
                    multipleObjectsSelected = false; // Set the bool again.
                    multipleObjectsOfTheSameTypeSelected = false;
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
                    foreach (var @object in currentSelectedObjsComponents)
                    {
                        @object.SetObjectColor(LE_Object.LEObjectContext.SELECT);
                    }
                }
                else
                {
                    currentSelectedObjComponent.SetObjectColor(LE_Object.LEObjectContext.SELECT);
                }

                if (currentMode == Mode.Selection) gizmosRoot.SetActive(true);
                gizmosRoot.transform.localRotation = currentSelectedObj.transform.rotation;

                if (multipleObjectsSelected)
                {
                    SelectedObjPanel.Instance.SetMultipleObjectsSelected();
                    currentSelectedObjsComponents.ForEach(x => x.OnSelect());
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
            if (objects == null)
            {
                SetSelectedObj(null);
                return;
            }

            // Only select active objects for parenting
            var filtered = objects.Where(obj => obj != null && obj.activeSelf).ToList();
            if (filtered.Count == 0)
            {
                SetSelectedObj(null);
                return;
            }

            multipleSelectedObjsParent.SetActive(true);

            // Deselect current selection
            if (currentSelectedObj != null)
            {
                if (multipleObjectsSelected)
                {
                    foreach (var obj in currentSelectedObjects)
                    {
                        if (obj != null)
                        {
                            obj.GetComponent<LE_Object>().SetObjectColor(LE_Object.LEObjectContext.NORMAL);
                            obj.transform.parent = obj.GetComponent<LE_Object>().objectParent;
                            obj.GetComponent<LE_Object>().OnDeselect(null);
                        }
                    }
                }
                else if (currentSelectedObjComponent != null)
                {
                    currentSelectedObjComponent.SetObjectColor(LE_Object.LEObjectContext.NORMAL);
                    currentSelectedObjComponent.OnDeselect(null);
                }
            }

            multipleSelectedObjsParent.transform.localScale = Vector3.one;

            // Calculate center position
            Vector3 centeredPosition = Vector3.zero;
            foreach (var obj in filtered)
                centeredPosition += obj.transform.position;
            centeredPosition /= filtered.Count;
            multipleSelectedObjsParent.transform.position = centeredPosition;
            if (!isForUndo) multipleSelectedObjsParent.transform.rotation = Quaternion.identity;

            currentSelectedObjects.Clear();
            currentSelectedObjsComponents.Clear();

            foreach (var obj in filtered)
            {
                var objComp = obj.GetComponent<LE_Object>();

                obj.transform.parent = multipleSelectedObjsParent.transform;
                objComp.SetObjectColor(LE_Object.LEObjectContext.SELECT);
                objComp.OnSelect();

                currentSelectedObjects.Add(obj);
                currentSelectedObjsComponents.Add(objComp);
            }

            multipleObjectsSelected = true;
            multipleObjectsOfTheSameTypeSelected = LE_Object.ObjectsAreOfTheSameType(currentSelectedObjsComponents);
            currentSelectedObj = multipleSelectedObjsParent;

            if (currentSelectedObjects.Count > 0)
            {
                SelectedObjPanel.Instance.SetMultipleObjectsSelected();
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
                // Create a copy of the list to avoid modifying the original list while iterating
                List<GameObject> objectsToDelete = new List<GameObject>(currentSelectedObjects);

                foreach (var obj in objectsToDelete)
                {
                    // Skip if object is null
                    if (obj == null)
                        continue;

                    // Since the selected objects are in another parent, also count the objects in that parent.
                    existingObjects += multipleSelectedObjsParent.GetChilds(false).ToArray().Length;

                    if (existingObjects - currentSelectedObjects.Count <= 0)
                    {
                        Utils.ShowCustomNotificationRed("There must be at least 1 object in the level", 2f);
                        return;
                    }

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

            if ((!multipleObjectsSelected && currentSelectedObjComponent != null && currentSelectedObjComponent.canUndoDeletion) || multipleObjectsSelected)
            {
                // Register the LEAction before deselecting the object, so I can set the target obj with the reference to the current selected object.
                RegisterLEAction(LEAction.LEActionType.DeleteObject, currentSelectedObj, multipleObjectsSelected, null, null, null, null);
            }

            SetSelectedObj(null);
        }

        bool CanUseCaughtSnapToGridTrigger(LE_Object.ObjectType objToBuildType, GameObject triggerObj)
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
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (movementPlane.Raycast(ray, out float distance))
            {
                // Only set MOVING_OBJECT state when the user actually moves the object, not just on arrow press
                Vector3 hitWorldPosition = ray.GetPoint(distance);
                Vector3 axisDirection;

                // Get proper axis direction based on mode
                if (globalGizmosArrowsEnabled)
                {
                    switch (collidingArrow)
                    {
                        case GizmosArrow.X: axisDirection = Vector3.right; break;
                        case GizmosArrow.Y: axisDirection = Vector3.up; break;
                        case GizmosArrow.Z: axisDirection = Vector3.forward; break;
                        default: axisDirection = Vector3.zero; break;
                    }
                }
                else
                {
                    switch (collidingArrow)
                    {
                        case GizmosArrow.X: axisDirection = currentSelectedObj.transform.right; break;
                        case GizmosArrow.Y: axisDirection = currentSelectedObj.transform.up; break;
                        case GizmosArrow.Z: axisDirection = currentSelectedObj.transform.forward; break;
                        default: axisDirection = Vector3.zero; break;
                    }
                }

                Vector3 displacement = hitWorldPosition - objPositionWhenArrowClick;
                float movementDistance = Vector3.Dot(displacement, axisDirection);
                Vector3 movement = axisDirection * movementDistance;

                // Only start moving if the mouse has moved a minimum distance
                float minMoveDistance = 0.001f; // Small threshold to avoid snapping on click
                if (movement.magnitude < minMoveDistance)
                {
                    // Don't move or snap to grid until user actually drags
                    return;
                }
                if (!IsCurrentState(EditorState.MOVING_OBJECT))
                    SetCurrentEditorState(EditorState.MOVING_OBJECT);

                // Calculate offset based on movement mode
                Vector3 offset;
                if (globalGizmosArrowsEnabled)
                {
                    offset = offsetObjPositionAndMosueWhenClick;
                }
                else
                {
                    offset = RotatePositionAroundPivot(offsetObjPositionAndMosueWhenClick + objPositionWhenArrowClick,
                        objPositionWhenArrowClick, currentSelectedObj.transform.rotation) - objPositionWhenArrowClick;
                }

                Vector3 newPosition = objPositionWhenArrowClick + movement + offset;

                // Grid snapping with proper axis constraint, only when actually moving
                if (gridEnabled)
                {
                    Vector3 gridPos = newPosition;
                    if (globalGizmosArrowsEnabled)
                    {
                        switch (collidingArrow)
                        {
                            case GizmosArrow.X:
                                gridPos.x = Mathf.Round(newPosition.x / gridSize) * gridSize;
                                gridPos.y = objPositionWhenArrowClick.y;
                                gridPos.z = objPositionWhenArrowClick.z;
                                break;
                            case GizmosArrow.Y:
                                float mouseDeltaY = Mathf.Abs(newPosition.y - objPositionWhenArrowClick.y);
                                if (mouseDeltaY > 0.01f)
                                {
                                    gridPos.x = objPositionWhenArrowClick.x;
                                    gridPos.y = Mathf.Round(newPosition.y / gridSize) * gridSize;
                                    gridPos.z = objPositionWhenArrowClick.z;
                                }
                                else
                                {
                                    gridPos = objPositionWhenArrowClick;
                                }
                                break;
                            case GizmosArrow.Z:
                                gridPos.x = objPositionWhenArrowClick.x;
                                gridPos.y = objPositionWhenArrowClick.y;
                                gridPos.z = Mathf.Round(newPosition.z / gridSize) * gridSize;
                                break;
                        }
                    }
                    else
                    {
                        Vector3 localPos = currentSelectedObj.transform.InverseTransformPoint(newPosition);
                        Vector3 snappedLocalPos = localPos;

                        switch (collidingArrow)
                        {
                            case GizmosArrow.X:
                                snappedLocalPos.x = Mathf.Round(localPos.x / gridSize) * gridSize;
                                snappedLocalPos.y = currentSelectedObj.transform.InverseTransformPoint(objPositionWhenArrowClick).y;
                                snappedLocalPos.z = currentSelectedObj.transform.InverseTransformPoint(objPositionWhenArrowClick).z;
                                break;
                            case GizmosArrow.Y:
                                snappedLocalPos.x = currentSelectedObj.transform.InverseTransformPoint(objPositionWhenArrowClick).x;
                                snappedLocalPos.y = Mathf.Round(localPos.y / gridSize) * gridSize;
                                snappedLocalPos.z = currentSelectedObj.transform.InverseTransformPoint(objPositionWhenArrowClick).z;
                                break;
                            case GizmosArrow.Z:
                                snappedLocalPos.x = currentSelectedObj.transform.InverseTransformPoint(objPositionWhenArrowClick).x;
                                snappedLocalPos.y = currentSelectedObj.transform.InverseTransformPoint(objPositionWhenArrowClick).y;
                                snappedLocalPos.z = Mathf.Round(localPos.z / gridSize) * gridSize;
                                break;
                        }
                        gridPos = currentSelectedObj.transform.TransformPoint(snappedLocalPos);
                    }
                    newPosition = gridPos;
                }

                if (multipleObjectsSelected)
                {
                    currentSelectedObj.transform.position = newPosition;
                }
                else
                {
                    currentSelectedObj.transform.position = newPosition;
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
                foreach (var objComp in currentSelectedObjsComponents)
                {
                    GameObject placedObj = null;
                    if (LE_Object.IsWaypoint(objComp.objectType.Value))
                    {
                        // Weird shit happends when duplicating waypoints + multiple objects, and I'm not mentally stable enough to see why. - Jav.
                        Utils.ShowCustomNotificationRed("Duplicating waypoints while selecting multiple objects is not supported.", 3f);
                        return;
                    }
                    else
                    {
                        placedObj = PlaceObject(objComp.objectType, objComp.transform.position, objComp.transform.eulerAngles,
                        objComp.transform.localScale, false);
                    }

                    if (!placedObj)
                    {
                        Logger.Log($"PlaceObject when duplicating \"{objComp.objectType}\" returned null. It probably reached its max object limit.");
                        continue;
                    }
                    LE_Object newPlacedObjComp = placedObj.GetComponent<LE_Object>();

                    // Copy every property from the origin to the copied obj.
                    newPlacedObjComp.setActiveAtStart = objComp.setActiveAtStart;
                    newPlacedObjComp.collision = objComp.collision;
                    newPlacedObjComp.invisibleMesh = objComp.invisibleMesh;
                    newPlacedObjComp.startMovingAtStart = objComp.startMovingAtStart;
                    newPlacedObjComp.movingSpeed = objComp.movingSpeed;
                    newPlacedObjComp.startDelay = objComp.startDelay;
                    newPlacedObjComp.waitTime = objComp.waitTime;
                    newPlacedObjComp.waypointMode = objComp.waypointMode;
                    foreach (var property in objComp.properties)
                    {
                        newPlacedObjComp.SetProperty(property.Key, Utils.CreateCopyOf(property.Value));
                    }
                    objComp.BeforeSave(); // If the origin obj was waypoints, force them to update their position, rotation and props before copying them.
                    foreach (var waypoint in objComp.waypoints)
                    {
                        newPlacedObjComp.waypoints.Add((WaypointData)Utils.CreateCopyOf(waypoint));
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
                GameObject placedObj = null;
                if (LE_Object.IsWaypoint(objComponent.objectType.Value))
                {
                    placedObj = ((LE_Waypoint)objComponent).AddWaypoint(false).gameObject;
                }
                else
                {
                    placedObj = PlaceObject(objComponent.objectType, objComponent.transform.localPosition, objComponent.transform.localEulerAngles,
                    objComponent.transform.localScale, false);
                }

                if (!placedObj)
                {
                    Logger.Log($"PlaceObject when duplicaing \"{objComponent.objectType}\" returned null. It probably reached its max object limit.");
                    return;
                }

                LE_Object newPlacedObjComp = placedObj.GetComponent<LE_Object>();

                // Copy every property from the origin to the copied obj.
                newPlacedObjComp.setActiveAtStart = objComponent.setActiveAtStart;
                newPlacedObjComp.collision = objComponent.collision;
                newPlacedObjComp.invisibleMesh = objComponent.invisibleMesh;
                newPlacedObjComp.startMovingAtStart = objComponent.startMovingAtStart;
                newPlacedObjComp.movingSpeed = objComponent.movingSpeed;
                newPlacedObjComp.startDelay = objComponent.startDelay;
                newPlacedObjComp.waitTime = objComponent.waitTime;
                newPlacedObjComp.waypointMode = objComponent.waypointMode;
                foreach (var property in objComponent.properties)
                {
                    newPlacedObjComp.SetProperty(property.Key, Utils.CreateCopyOf(property.Value));
                }
                objComponent.BeforeSave(); // If the origin obj was waypoints, force them to update their position, rotation and props before copying them.
                foreach (var waypoint in objComponent.waypoints)
                {
                    newPlacedObjComp.waypoints.Add((WaypointData)Utils.CreateCopyOf(waypoint));
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

                    if (hit.collider.gameObject.name.StartsWith(SNAP_TRIGGERS_NAME))
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
                            LE_Object.ObjectType objectTypeToUse = currentSelectedObjComponent.objectType.Value;
                            if (currentSelectedObjComponent is LE_Waypoint waypoint)
                            {
                                objectTypeToUse = waypoint.mainObjectType.Value;
                            }
                            if (CanUseCaughtSnapToGridTrigger(objectTypeToUse, hit.collider.gameObject))
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
            else // If nothing is hit, place on grid
            {
                if (gridEnabled && gridVisible)
                {
                    Plane gridPlane = new Plane(Vector3.up, new Vector3(0, gridHeight, 0));
                    float enter = 0f;
                    if (gridPlane.Raycast(ray, out enter))
                    {
                        currentSelectedObj.transform.position = ray.GetPoint(enter);
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
                gizmosRoot.transform.localScale = Vector3.one * 2f;
            }
            else
            {
                gizmosRoot.transform.localScale = Vector3.one * 2f * highestAxis;
            }
        }

        public void RegisterLEAction(LEAction.LEActionType type, GameObject targetObj, bool forMultipleObjs, Vector3? oldPos = null, Vector3? newPos = null,
            Quaternion? oldRot = null, Quaternion? newRot = null, Vector3? oldScale = null, Vector3? newScale = null, bool waypointRotation = true)
        {
            if (!targetObj) return;

            currentExecutingAction = new LEAction();
            currentExecutingAction.forMultipleObjects = forMultipleObjs;
            currentExecutingAction.waypointRotation = waypointRotation;

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
                    if (type == LEAction.LEActionType.DeleteObject)
                    {
                        if (obj.GetComponent<LE_Object>().canUndoDeletion)
                        {
                            currentExecutingAction.targetObjs.Add(obj);
                        }
                        continue;
                    }

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
            Destroy(editorObjectsRootFromBundle);

            // Unload the asset bundle when the editor is destroyed
            if (editorAssetBundle != null)
            {
                try
                {
                    editorAssetBundle.Unload(true);
                }
                catch (System.Runtime.InteropServices.SEHException)
                {
                    // Asset bundle may have already been garbage collected by IL2CPP runtime
                }
                editorAssetBundle = null;
            }
        }

        #region Current Editor State Methods
        public void SetCurrentEditorState(EditorState newState)
        {
            previousEditorState = currentEditorState;
            currentEditorState = newState;

            //same, just in case.
            if (newState == EditorState.MOVING_OBJECT && selectionBox != null)
                selectionBox.SetActive(false);
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

            // Clean up any ongoing preview rotation
            if (previewRotationCoroutine != null)
            {
                MelonCoroutines.Stop(previewRotationCoroutine);
                previewRotationCoroutine = null;
            }

            if (objectType == null)
            {
                currentObjectToBuildType = null;
                currentObjectToBuild = null;
                Destroy(previewObjectToBuildObj);
                previewRotationOffsetEuler = Vector3.zero; // reset offset
                return;
            }

            currentObjectToBuildType = objectType;
            currentObjectToBuild = allCategoriesObjectsSorted[currentCategoryID][objectType.Value];
            previewRotationOffsetEuler = Vector3.zero; // reset when changing object

            // Destroy the preview object and create another one with the new selected model.
            Destroy(previewObjectToBuildObj);
            previewObjectToBuildObj = Instantiate(currentObjectToBuild);

            //Preview scale enforcement
            if (LE_Object.defaultScalesForObjects.ContainsKey(objectType))
            {
                previewObjectToBuildObj.transform.localScale = LE_Object.defaultScalesForObjects[objectType.Value];
            }

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
                // Search up the hierarchy for a GameObject with LE_Object component
                Transform current = hit.collider.transform;
                while (current != null)
                {
                    if (current.GetComponent<LE_Object>() != null)
                    {
                        obj = current.gameObject;
                        return true;
                    }
                    current = current.parent;
                }

                Logger.Warning($"For some reason, the object you just tried to select ({hit.collider.name}) doesn't have a LE_Object component in its hierarchy.");
                obj = null;
                return false;
            }
            else
            {
                obj = null;
                return false;
            }
        }

        /// <summary>
     /// Returns if a ray from the mouse position to real world is colliding with a gizmos arrow of an object.
        /// Uses prioritization to select the most appropriate axis when multiple colliders overlap.
        /// </summary>
        /// <returns></returns>
        GizmosArrow GetCollidingWithAnArrow()
        {
            GizmosArrow arrow = gizmo.GetHoveredArrow(out Ray ray);
            
            if (arrow != GizmosArrow.None) StartMovingObject(arrow.ToString(), ray);
            return arrow;
        }

        Vector3 GetAxisDirection(GizmosArrow arrow, GameObject obj)
        {
            if (globalGizmosArrowsEnabled)
            {
                // Global axes are always world-aligned
                if (arrow == GizmosArrow.X) return Vector3.right;
                if (arrow == GizmosArrow.Y) return Vector3.up;
                if (arrow == GizmosArrow.Z) return Vector3.forward;
            }
            else
            {
                switch (arrow)
                {
                    case GizmosArrow.X: return obj.transform.right;
                    case GizmosArrow.Y: return obj.transform.up;
                    case GizmosArrow.Z: return obj.transform.forward;
                    default: return Vector3.zero;
                }
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
        public void SetupLevelMusic(int musicID)
        {
            MusicManager.Instance.SetCurrentLevelNormalMusic(tracks[musicID]);
            MusicManager.Instance.PauseMenuMusic();
            MusicManager.Instance.m_context = MusicManager.MusicContext.NORMAL;
        }

        void ToggleLighting()
        {
            lightingEnabled = !lightingEnabled;

            // Toggle ambient light
            if (lightingEnabled)
            {
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
                RenderSettings.ambientIntensity = 1f;
            }
            else
            {
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientIntensity = 0f;
                RenderSettings.ambientLight = Color.white;
            }

            // Find all Light components in the scene and toggle them
            //var allLights = FindObjectsOfType<Light>();
            //foreach (var light in allLights)
            //{
            //    light.enabled = lightingEnabled;
            //}

            // Show notification to user
            string state = lightingEnabled ? "Lit" : "Unlit";
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowNotification($"{state} mode enabled", "WhiteSquare");
            }
            else
            {
                Utils.ShowCustomNotificationRed($"{state} mode enabled", 1.5f);
            }
        }

        void OnRenderObject()
        {
            if (!gridVisible || !gridLineMaterial || gridTexture == null) return;
            if (Camera.current != Camera.main) return;

            Camera cam = Camera.main;
            Vector3 camPos = cam.transform.position;
            float y = gridHeight + 0.001f; // Slight offset to prevent Z-fighting

            // Calculate grid size based on current setting
            float worldGridSize = Mathf.Max(gridSize, 0.1f);

            // Calculate view distance (adaptive based on grid size)
            float viewDistance = Mathf.Max(256f, worldGridSize * 512f);

            // Create large quad centered on camera
            float quadSize = viewDistance;
            Vector3 center = new Vector3(camPos.x, y, camPos.z);

            // Calculate UV tiling based on grid size
            // Each texture tile = one grid cell
            float uvScale = quadSize / worldGridSize;

            // Offset UVs to align with world grid
            float uvOffsetX = (center.x % worldGridSize) / worldGridSize;
            float uvOffsetZ = (center.z % worldGridSize) / worldGridSize;

            gridLineMaterial.SetPass(0);
            GL.PushMatrix();
            GL.LoadIdentity();
            GL.MultMatrix(cam.worldToCameraMatrix);
            GL.LoadProjectionMatrix(cam.projectionMatrix);

            GL.Begin(GL.QUADS);
            GL.Color(Color.white);

            // Render single quad with tiled texture
            float halfSize = quadSize * 0.5f;

            // Bottom-RIGHT (was bottom-left)
            GL.TexCoord2(uvScale * 0.5f + uvOffsetX, -uvScale * 0.5f + uvOffsetZ);
            GL.Vertex3(center.x + halfSize, y, center.z - halfSize);

            // Bottom-LEFT (was bottom-right)
            GL.TexCoord2(-uvScale * 0.5f + uvOffsetX, -uvScale * 0.5f + uvOffsetZ);
            GL.Vertex3(center.x - halfSize, y, center.z - halfSize);

            // Top-left (unchanged)
            GL.TexCoord2(-uvScale * 0.5f + uvOffsetX, uvScale * 0.5f + uvOffsetZ);
            GL.Vertex3(center.x - halfSize, y, center.z + halfSize);

            // Top-right (unchanged)
            GL.TexCoord2(uvScale * 0.5f + uvOffsetX, uvScale * 0.5f + uvOffsetZ);
            GL.Vertex3(center.x + halfSize, y, center.z + halfSize);

            GL.End();
            GL.PopMatrix();
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
        public bool waypointRotation;

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
                case LEAction.LEActionType.MoveObject:
                    UndoMoveObject(editor);
                    break;
                case LEAction.LEActionType.RotateObject:
                    UndoRotateObject(editor);
                    break;
                case LEAction.LEActionType.ScaleObject:
                    UndoScaleObject(editor);
                    break;
                case LEAction.LEActionType.SnapObject:
                    UndoMoveObject(editor);
                    UndoRotateObject(editor);
                    break;
                case LEAction.LEActionType.DeleteObject:
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

                if (!waypointRotation) editor.AttachWaypointsFromObject(editor.multipleSelectedObjsParent, false);
                // Rotate the parent so the whole selection is rotated too.
                editor.multipleSelectedObjsParent.transform.localRotation = oldRot;
                if (!waypointRotation) editor.AttachWaypointsFromObject(editor.multipleSelectedObjsParent, true);

                SelectedObjPanel.Instance.UpdateGlobalObjectAttributes(editor.multipleSelectedObjsParent.transform);
            }
            else
            {
                if (!waypointRotation) editor.AttachWaypointsFromObject(targetObj, false);
                targetObj.transform.localRotation = oldRot;
                if (!waypointRotation) editor.AttachWaypointsFromObject(targetObj, true);


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
                targetObjs.ForEach(obj => obj.GetComponent<LE_Object>().OnUndoDeletion());
                editor.SetMultipleObjectsAsSelected(targetObjs, true);
            }
            else
            {
                targetObj.GetComponent<LE_Object>().OnUndoDeletion();
                targetObj.SetActive(true);
                editor.SetSelectedObj(targetObj);
            }
        }
    }
}