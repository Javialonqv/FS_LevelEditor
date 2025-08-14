using FS_LevelEditor.Editor.UI;
using FS_LevelEditor.SaveSystem;
using FS_LevelEditor.UI_Related;
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

		// Status label fields for camera speed and grid size
		private GameObject statusLabelsRoot;
		private UILabel cameraSpeedLabel;
		private UILabel gridSizeLabel;

		public string levelName = "test_level";
		public string levelFileNameWithoutExtension = "test_level";

		public EditorState previousEditorState;
		public EditorState currentEditorState;

		// Avaiable objects from all of the categories.
		GameObject editorObjectsRootFromBundle;
		public List<Dictionary<LE_Object.ObjectType, GameObject>> allCategoriesObjectsSorted = new();
		public Dictionary<LE_Object.ObjectType, GameObject> allCategoriesObjects = new();
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
		public enum Mode { Building, Selection }
		public Mode currentMode = Mode.Building;
		private BulkSelectionMode currentBulkSelectionMode = BulkSelectionMode.Everything;

		//Bulk selection.
		private bool isSelecting = false;
		private Vector2 selectionStartScreen;
		private Vector2 selectionEndScreen;
		private float selectionStartTime;
		private const float multiSelectDelay = 0.3f; // seconds
		private const float minDragDistance = 5f; // pixels
		private GameObject selectionBox;
		private UISprite selectionBoxSprite;


		// Gizmos arrows to move objects.
		//GameObject gizmosArrows;
		EditorGizmo gizmo;
		GameObject gizmoPrefab; // <-- Add this as a private field
		GameObject gizmosArrows => gizmo?.Root;
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
		Camera MainCam;
		public bool showAllWaypoints = false;

		public bool enteringPlayMode = false;

		// ----------------------------
		public Dictionary<string, object> globalProperties = LevelData.GetDefaultGlobalProperties();
		List<Material> skyboxes = new List<Material>();

		// --- GRID FIELDS ---
		private float gridSize = 1f;
		private const float MIN_GRID_SIZE = 0.0001f; // Infinite precision
		private const float MAX_GRID_SIZE = 8f;
		private const float GRID_SIZE_MULTIPLIER = 2f;
		private float gridHeight = 121.7324f; // Default to correct Y
		private int gridExtent = 256; // Larger for infinite look
		private bool gridVisible = true;
		private bool gridEnabled = true;
		private Material gridLineMaterial;
		private Vector3 gridCenter = Vector3.zero;

		private void UpdateGridCenter()
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

		private static Material _gizmoArrowMaterial = null;
		public static Material GizmoArrowMaterial => _gizmoArrowMaterial;

		void Awake()
		{
			Instance = this;
			MenuController.isInLevelEditor = true;
			LoadAssetBundle();

			// --- Extract old gizmo material and destroy old gizmo ---
			if (gizmoPrefab != null)
			{
				GameObject oldGizmo = Instantiate(gizmoPrefab);
				var xObj = oldGizmo.transform.Find("X");
				if (xObj != null)
				{
					var renderer = xObj.GetComponent<Renderer>();
					if (renderer != null)
					{
						_gizmoArrowMaterial = new Material(renderer.material); // Copy material
					}
				}
				Destroy(oldGizmo);
			}

			levelObjectsParent = new GameObject("LevelObjects");
			levelObjectsParent.transform.position = Vector3.zero;

			multipleSelectedObjsParent = new GameObject("MultipleSelectedObjsParent");
			multipleSelectedObjsParent.transform.position = Vector3.zero;

			deathYPlane = Instantiate(LoadOtherObjectInBundle("DeathYPlane")).AddComponent<DeathYPlaneCtrl>();
			MainCam = GameObject.Find("Main Camera").GetComponent<Camera>();
			MainCam.fieldOfView = 90f; // Set FOV to 90 by default.
			MainCam.nearClipPlane = 0.1f; //to prevent disappearing when near objects.

			//Toensure nothing is left in our scene
			InGameUIManager ui = InGameUIManager.Instance;
			ui.HideHealthBarRoutine();
			ui.HideDodgeCooldown(true);
		 ui.HideHoverGauge(true);
			ui.ShowSprintFeedback(false);
			ui.ShowFuelBar(false, 0, 0);
			ui.ForceHideFuelBar();
			ui.HideFuelBarRoutine(0);

			CreateGridLineMaterial();
			gridHeight = 121.7324f;
			gridCenter = new Vector3(0, gridHeight, 0); // Always start centered at origin
			UpdateGridCenter(); // Ensure gridCenter is correct at start
			currentEditorState = EditorState.NORMAL; // Ensure state is initialized

			// --- Initialize new gizmo ---
			gizmo = new EditorGizmo(); // Use the new line-renderer based gizmo, not the prefab

			// Create status labels root
			statusLabelsRoot = new GameObject("EditorStatusLabels");
			statusLabelsRoot.transform.SetParent(null); // Not parented to build UI
			statusLabelsRoot.transform.localScale = Vector3.one;
			// Place at bottom center of screen (NGUI coordinates)
			statusLabelsRoot.transform.position = Vector3.zero;
			// Create labels
			float yBase = -470f; // Lowered further
			cameraSpeedLabel = NGUI_Utils.CreateLabel(statusLabelsRoot.transform, new Vector3(0f, yBase, 0f), new Vector3Int(400, 30, 0), "Camera Speed: 0", NGUIText.Alignment.Center, UIWidget.Pivot.Center);
			cameraSpeedLabel.fontSize = 24; // Increased font size
			cameraSpeedLabel.color = Color.white;
			cameraSpeedLabel.name = "CameraSpeedLabel";
			gridSizeLabel = NGUI_Utils.CreateLabel(statusLabelsRoot.transform, new Vector3(0f, yBase - 34f, 0f), new Vector3Int(400, 30, 0), "Grid Size: 0", NGUIText.Alignment.Center, UIWidget.Pivot.Center);
			gridSizeLabel.fontSize = 24; // Increased font size
			gridSizeLabel.color = Color.white;
			gridSizeLabel.name = "GridSizeLabel";
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

			// REMOVE old gizmosArrows instantiation
			// gizmosArrows = Instantiate(bundle.Load<GameObject>("MoveObjectArrows"));
			// gizmosArrows.name = "MoveObjectArrows";
			// gizmosArrows.transform.localPosition = Vector3.zero;
			// gizmosArrows.SetActive(false);

			// --- Load old gizmo prefab for EditorGizmo ---
			if (bundle.Contains("MoveObjectArrows"))
			{
				gizmoPrefab = bundle.Load<GameObject>("MoveObjectArrows");
			}

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
		public GameObject LoadOtherObjectInBundle(String objectName)
		{
			GameObject toReturn = otherObjectsFromBundle.FirstOrDefault(obj => obj.name == objectName);

			if (objectName == "EditorLine")
			{
				toReturn.GetComponent<LineRenderer>().material.shader = Shader.Find("Sprites/Default");
			}

			return toReturn;
		}

		void Start()
		{
			// Disable occlusion culling.
			Camera.main.useOcclusionCulling = false;

			UpdateGridCenter(); // Ensure grid is placed correctly on editor start
		}
		public void AfterFinishedLoadingLevel()
		{
			SetupSkybox((int)globalProperties["Skybox"]);
		}

		void Update()
		{
			if (PlayFromMenuHelper.PlayImmediatelyOnEditorLoad && PlayFromMenuHelper.LevelToPlay == levelFileNameWithoutExtension)
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
			if (Input.GetMouseButtonDown(0) && currentMode == Mode.Selection &&
				!Utils.IsMouseOverUIElement() && !IsCurrentState(EditorState.SNAPPING_TO_GRID))
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
				// Only start selection if we're not using gizmo
				if (GetCollidingWithAnArrow() == GizmosArrow.None && currentMode == Mode.Selection)
				{
					isSelecting = true;
					selectionStartScreen = Input.mousePosition;
					selectionEndScreen = selectionStartScreen;
					selectionStartTime = Time.unscaledTime;
					// Do NOT show the selection box yet
				}
			}

			// Update selection rectangle
			if (isSelecting && Input.GetMouseButton(0) && !Input.GetKey(KeyCode.F))
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

			// End selection
			if (isSelecting && Input.GetMouseButtonUp(0))
			{
				isSelecting = false;
				if (selectionBox != null)
					selectionBox.SetActive(false);

				float dragDistance = (selectionEndScreen - selectionStartScreen).magnitude;
				float heldTime = Time.unscaledTime - selectionStartTime;

				// Only perform rectangle selection if it was a drag and not snapping
				if (!IsCurrentState(EditorState.MOVING_OBJECT) && !Input.GetKey(KeyCode.F))
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

			// Update camera speed and grid size labels
			if (cameraSpeedLabel != null && gridSizeLabel != null)
			{
				float camSpeed = FS_LevelEditor.Editor.EditorCameraMovement.Instance != null ? FS_LevelEditor.Editor.EditorCameraMovement.Instance.moveSpeed : 0f;
				float gridSizeVal = GetGridSize();
				cameraSpeedLabel.text = $"Camera Speed: {camSpeed:0.###}";
				gridSizeLabel.text = $"Grid Size: {gridSizeVal:0.###}";
			}
		}

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
			// Calculate selection rectangle bounds
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
			Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(cam);
			var selectedObjects = new List<GameObject>();

			foreach (var obj in currentInstantiatedObjects)
			{
				if (obj == null || obj.isDeleted || !obj.gameObject.activeSelf)
					continue;

				switch (currentBulkSelectionMode)
				{
					case BulkSelectionMode.ObjectsOnly:
						if (obj is LE_Waypoint) continue;
						break;
					case BulkSelectionMode.WaypointsAndObjectsWithWaypoints:
						// Skip if not a waypoint and doesn't have waypoints
						if (!(obj is LE_Waypoint) && (obj.waypoints == null || obj.waypoints.Count == 0))
							continue;
						break;
				}

				var renderers = obj.gameObject.GetComponentsInChildren<Renderer>(false);
				if (renderers.Length == 0) continue;

				Bounds bounds = renderers[0].bounds;
				for (int i = 1; i < renderers.Length; i++)
				{
					bounds.Encapsulate(renderers[i].bounds);
				}

				if (!GeometryUtility.TestPlanesAABB(frustumPlanes, bounds))
					continue;

				Vector3[] corners = new Vector3[8];
				corners[0] = new Vector3(bounds.min.x, bounds.min.y, bounds.min.z);
				corners[1] = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
				corners[2] = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
				corners[3] = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);
				corners[4] = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
				corners[5] = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
				corners[6] = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
				corners[7] = new Vector3(bounds.max.x, bounds.max.y, bounds.max.z);

				bool anyCornerInSelection = false;
				float objMinX = float.MaxValue, objMaxX = float.MinValue;
				float objMinY = float.MaxValue, objMaxY = float.MinValue;

				foreach (var corner in corners)
				{
					Vector3 screenPos = cam.WorldToScreenPoint(corner);
					if (screenPos.z < 0) continue;

					objMinX = Mathf.Min(objMinX, screenPos.x);
					objMaxX = Mathf.Max(objMaxX, screenPos.x);
					objMinY = Mathf.Min(objMinY, screenPos.y);
					objMaxY = Mathf.Max(objMaxY, screenPos.y);

					if (screenPos.x >= minX && screenPos.x <= maxX &&
						screenPos.y >= minY && screenPos.y <= maxY)
					{
						anyCornerInSelection = true;
						break;
					}
				}

				bool containsSelectionRect =
					objMinX <= minX && objMaxX >= maxX &&
					objMinY <= minY && objMaxY >= maxY;

				if (objMinX != float.MaxValue &&
					(anyCornerInSelection || containsSelectionRect))
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

		void LateUpdate()
		{
			if (gizmosArrows.activeSelf && currentSelectedObj)
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
				// Improved infinite scaling for arrows and cones
				float distance = Vector3.Distance(MainCam.transform.position, currentSelectedObj.transform.position);
				float baseArrowScale = 2f;
				float scaleFactor = Mathf.Max(0.1f, distance * 0.15f);
				float highestAxis = Utils.HighestValueOfVector(currentSelectedObj.transform.localScale);
				if (highestAxis < 1f)
					scaleFactor *= highestAxis;
				// Set arrows and cones scale
				gizmo.SetScale(Vector3.one * baseArrowScale * scaleFactor);
				// Make cones always a bit larger than arrows, and thickness grow with distance
				gizmo.UpdateScaleByCamera(MainCam, 10f, 0.01f, 1000f);
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

			// --- GRID SHORTCUTS ---
			float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
			if (!Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.RightAlt))
			{
				if (Input.GetKey(KeyCode.LeftControl) && Mathf.Abs(scrollDelta) > 0.0001f)
				{
					if (scrollDelta > 0)
						DecreaseGridSize(); // Finer
					else if (scrollDelta < 0)
					IncreaseGridSize(); // Coarser
				}
				// MouseWheel: Change grid height
				if (!Input.GetKey(KeyCode.LeftControl) && Mathf.Abs(scrollDelta) > 0.0001f)
				{
					AdjustGridHeight(scrollDelta, Input.GetKey(KeyCode.LeftShift));
				}
			}
			// G: Toggle grid visibility
			if (Input.GetKeyDown(KeyCode.G) && !Input.GetKey(KeyCode.LeftShift))
			{
				SetGridVisible(!gridVisible);
			}
			// Shift+G: Toggle grid enabled AND visibility
			if (Input.GetKeyDown(KeyCode.G) && Input.GetKey(KeyCode.LeftShift))
			{
				bool newState = !gridEnabled;
				SetGridEnabled(newState);
				SetGridVisible(newState);
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

			// Shortcut to show all waypoints.
			if (Input.GetKeyDown(KeyCode.Alpha3))
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

			// Shortcut for saving level data.
			if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.S) && levelHasBeenModified)
			{
				LevelData.SaveLevelData(levelName, levelFileNameWithoutExtension);
				EditorUIManager.Instance.PlaySavingLevelLabel();
				levelHasBeenModified = false;
			}
			if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.W))
			{
				EditorUIManager.Instance.SwitchToNextBulkSelectionMode();
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

			// Shortcut for toggling gizmo mode (4 key)
			if (Input.GetKeyDown(KeyCode.Alpha4) && currentMode == Mode.Selection && currentSelectedObj != null)
			{
				globalGizmosArrowsEnabled = !globalGizmosArrowsEnabled;
				gizmo.SetRotation(globalGizmosArrowsEnabled ? Quaternion.identity : currentSelectedObj.transform.rotation);
				
				// Show feedback to user
				string mode = globalGizmosArrowsEnabled ? "Global" : "Local";
				Utils.ShowCustomNotificationRed($"Switched to {mode} Gizmo Mode", 1.5f);
			}

			// Shortcuts to switch between local and global gizmos arrows.
			if (Input.GetKeyDown(KeyCode.G) && !Input.GetKey(KeyCode.LeftShift) && currentMode == Mode.Selection && currentSelectedObj != null)
            {
                globalGizmosArrowsEnabled = !globalGizmosArrowsEnabled;
                gizmo.SetRotation(globalGizmosArrowsEnabled ? Quaternion.identity : currentSelectedObj.transform.rotation);
                
                // Show feedback to user
                string mode = globalGizmosArrowsEnabled ? "Global" : "Local";
                Utils.ShowCustomNotificationRed($"Switched to {mode} Gizmo Mode", 1.5f);
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

			if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.A) && currentMode == Mode.Selection)
			{
				// Only select objects based on bulk selection mode, preserving active states
				var objectsToSelect = new List<GameObject>(currentInstantiatedObjects.Count);
				foreach (var obj in currentInstantiatedObjects)
				{
					if (obj == null || obj.isDeleted)
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
		}
		void ManageMoveObjectShortcuts()
		{
			GameObject targetObj = currentMode == Mode.Building ? previewObjectToBuildObj : currentSelectedObj;
			if (targetObj == null) return;

			float moveAmount = gridEnabled ? gridSize : 0.01f;
			Vector3 toMove = Vector3.zero;

			// Only process if an arrow key is pressed
			if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow))
			{
				// Use the object's up as the plane normal, or grid up if grid is enabled
				Vector3 planeNormal = gridEnabled ? Vector3.up : targetObj.transform.up;
				// Project camera's right and up onto the plane
				Vector3 moveRight = Vector3.ProjectOnPlane(Camera.main.transform.right, planeNormal).normalized;
				Vector3 moveUp = Vector3.ProjectOnPlane(Camera.main.transform.up, planeNormal).normalized;

				if (Input.GetKeyDown(KeyCode.LeftArrow))
					toMove -= moveRight * moveAmount;
				else if (Input.GetKeyDown(KeyCode.RightArrow))
					toMove += moveRight * moveAmount;
				else if (Input.GetKeyDown(KeyCode.UpArrow))
					toMove += moveUp * moveAmount;
				else if (Input.GetKeyDown(KeyCode.DownArrow))
					toMove -= moveUp * moveAmount;

				if (gridEnabled)
				{
					Vector3 newPos = targetObj.transform.localPosition + toMove;
					newPos.x = Mathf.Round(newPos.x / gridSize) * gridSize;
					newPos.y = Mathf.Round(newPos.y / gridSize) * gridSize;
					newPos.z = Mathf.Round(newPos.z / gridSize) * gridSize;
					Vector3 oldPos = targetObj.transform.localPosition;
					targetObj.transform.localPosition = newPos;
					if (currentSelectedObj)
					{
						RegisterLEAction(LEAction.LEActionType.MoveObject, currentSelectedObj, multipleObjectsSelected, oldPos, currentSelectedObj.transform.localPosition, null, null);
						SelectedObjPanel.Instance.UpdateGlobalObjectAttributes(currentSelectedObj.transform);
					}
				}
				else
				{
					Vector3 oldPos = targetObj.transform.localPosition;
					if (globalGizmosArrowsEnabled)
						targetObj.transform.Translate(toMove, Space.World);
					else
						targetObj.transform.Translate(toMove, Space.Self);
					if (currentSelectedObj)
					{
						RegisterLEAction(LEAction.LEActionType.MoveObject, currentSelectedObj, multipleObjectsSelected, oldPos, currentSelectedObj.transform.localPosition, null, null);
						SelectedObjPanel.Instance.UpdateGlobalObjectAttributes(currentSelectedObj.transform);
					}
				}
			}
		}

		private object currentRotationCoroutine;
private bool isRotatingWithR = false;

void ManageObjectRotationShortcuts()
{
    GameObject targetObj = currentMode == Mode.Building ? previewObjectToBuildObj : currentSelectedObj;
    if (targetObj == null) return;

    int multiplier = Input.GetKey(KeyCode.T) ? -1 : 1;
    float rotationAngle = 15f;
    Quaternion rotation = targetObj.transform.localRotation;

    // Start continuous rotation if R is held
    if (Input.GetKey(KeyCode.R))
    {
        if (!isRotatingWithR)
        {
            isRotatingWithR = true;
            if (currentRotationCoroutine != null)
                MelonCoroutines.Stop(currentRotationCoroutine);
            currentRotationCoroutine = MelonCoroutines.Start(ContinuousSmoothRotate(targetObj.transform, multiplier, rotationAngle));
        }
    }
    else if (isRotatingWithR)
    {
        isRotatingWithR = false;
        if (currentRotationCoroutine != null)
        {
            MelonCoroutines.Stop(currentRotationCoroutine);
            currentRotationCoroutine = null;
        }
    }

    // If the rotation changed and the object isn't the preview object...
    if (rotation != targetObj.transform.localRotation && currentMode != Mode.Building)
    {
        SelectedObjPanel.Instance.UpdateGlobalObjectAttributes(currentSelectedObj.transform);
        RegisterLEAction(LEAction.LEActionType.RotateObject, currentSelectedObj, multipleObjectsSelected, null, null, rotation, currentSelectedObj.transform.localRotation);
        levelHasBeenModified = true;
    }
}

IEnumerator ContinuousSmoothRotate(Transform transform, int multiplier, float rotationAngle)
{
    while (Input.GetKey(KeyCode.R))
    {
        Quaternion startRotation = transform.localRotation;
        Quaternion targetRotation = startRotation * Quaternion.Euler(0f, rotationAngle * multiplier, 0f);
        float elapsedTime = 0f;
        float duration = 0.15f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            t = t * t * (3f - 2f * t);
            transform.localRotation = Quaternion.Lerp(startRotation, targetRotation, t);
            yield return null;
        }
        transform.localRotation = targetRotation;
        // Normalize rotation to prevent drift
        Vector3 euler = transform.localRotation.eulerAngles;
        transform.localRotation = Quaternion.Euler(Mathf.Round(euler.x), Mathf.Round(euler.y), Mathf.Round(euler.z));
        yield return null;
    }
    isRotatingWithR = false;
    currentRotationCoroutine = null;
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
				if (currentSelectedObj) gizmosArrows.SetActive(true);
			}
			else
			{
				gizmosArrows.SetActive(false);
			}

			Logger.Log("Changed LE mode to: " + currentMode);
			EditorUIManager.Instance.SetCurrentModeLabelText(currentMode);
		}

		// --- GRID API ---
		public void SetGridSize(float newSize)
		{
			gridSize = Mathf.Clamp(newSize, MIN_GRID_SIZE, MAX_GRID_SIZE);
			UpdateGridCenter();
		}
		public void IncreaseGridSize()
		{
			SetGridSize(gridSize * GRID_SIZE_MULTIPLIER);
		}
		public void DecreaseGridSize()
		{
			SetGridSize(gridSize / GRID_SIZE_MULTIPLIER);
		}
		public void SetGridHeight(float newHeight)
		{
			gridHeight = newHeight;
			UpdateGridCenter();
		}
		public void AdjustGridHeight(float delta, bool precise)
		{
			gridHeight += precise ? delta * 0.1f : delta;
			UpdateGridCenter();
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
				// Handle snap triggers first
				if (hits.Count == 1 || theyAreAllSnapTriggers)
				{
					if (hits[0].collider.gameObject.name.StartsWith("StaticPos"))
					{
						if (currentObjectToBuildType.HasValue && CanUseCaughtSnapToGridTrigger(currentObjectToBuildType.Value, hits[0].collider.gameObject))
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
					foreach (var hit in hits.ToList())
					{
						if (hit.collider.gameObject.name.StartsWith("StaticPos") && Input.GetKey(KeyCode.LeftControl))
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
							hits.RemoveAll(h => h.collider.gameObject.name.StartsWith("StaticPos"));
							break;
						}
					}
				}
				if (snapWithTrigger)
				{
					previewObjectToBuildObj.SetActive(true);
					previewObjectToBuildObj.transform.position = rayToUseWithSnap.collider.transform.position;
					if (currentHittenSnapTrigger != rayToUseWithSnap.collider.gameObject)
					{
						currentHittenSnapTrigger = rayToUseWithSnap.collider.gameObject;
						previewObjectToBuildObj.transform.rotation = rayToUseWithSnap.collider.transform.rotation;
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
							previewObjectToBuildObj.transform.rotation = Quaternion.LookRotation(up, normal);
						}
						else
						{
							if (normal == Vector3.up)
								previewObjectToBuildObj.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
							else
								previewObjectToBuildObj.transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.down);
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
								previewObjectToBuildObj.transform.rotation = Quaternion.LookRotation(up, wallNormal);
							}
							else
							{
								if (wallNormal == Vector3.up)
								{
									previewObjectToBuildObj.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
								}
								else
								{
									previewObjectToBuildObj.transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.down);
								}
							}
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
							previewObjectToBuildObj.transform.rotation = Quaternion.identity;
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
						previewObjectToBuildObj.transform.rotation = Quaternion.identity;
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

			if (obj && obj != multipleSelectedObjsParent && obj.GetComponent<LE_Object>() == null)
			{
				Logger.Error($"No no, wait wait, how did you select an object called \"{obj.name}\"!? ARE YOU INSANE!? HOW!?!?!?");
				// Idk either mate.
				return;
			}

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
			if (objects == null)
			{
				SetSelectedObj(null);
				return;
			}

			var filtered = objects.Where(obj => obj != null).ToList();
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

			// Calculate center position using only active objects
			Vector3 centeredPosition = Vector3.zero;
			int validCount = 0;
			foreach (var obj in filtered.Where(o => o.activeSelf))
			{
				centeredPosition += obj.transform.position;
				validCount++;
			}

			if (validCount > 0)
			{
				centeredPosition /= validCount;

				multipleSelectedObjsParent.transform.position = centeredPosition;
				multipleSelectedObjsParent.transform.rotation = Quaternion.identity;

				currentSelectedObjects = new List<GameObject>();

				// --- FIX: Store and restore active state ---
				var activeStates = new Dictionary<GameObject, bool>();
				foreach (var obj in filtered)
					activeStates[obj] = obj.activeSelf;

				foreach (var obj in filtered)
				{
					var leObj = obj.GetComponent<LE_Object>();
					leObj.SetObjectColor(LE_Object.LEObjectContext.SELECT);
					obj.transform.parent = multipleSelectedObjsParent.transform;
					currentSelectedObjects.Add(obj);
					leObj.OnSelect();
				}

				// Restore active states
				foreach (var obj in filtered)
					obj.SetActive(activeStates[obj]);
				// --- END FIX ---
			}

			multipleObjectsSelected = true;
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
				if (!IsCurrentState(EditorState.MOVING_OBJECT)) 
					SetCurrentEditorState(EditorState.MOVING_OBJECT);

				Vector3 hitWorldPosition = ray.GetPoint(distance);
				Vector3 axisDirection;

				// Get proper axis direction based on mode
				if (globalGizmosArrowsEnabled)
				{
					// Global axes are always world-aligned
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
					// Local axes need to account for object's current orientation
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

				// Grid snapping with proper axis constraint
				if (gridEnabled)
				{
					Vector3 gridPos = newPosition;
					if (globalGizmosArrowsEnabled)
					{
						// Global mode: snap along world axes
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
						// Local mode: snap along local axes
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
				if (hit.collider.transform.parent == null)
				{
					Logger.Warning($"For some reason, the object you just tried to select ({hit.collider.name}) doesn't have a parent.");
					obj = null;
					return false;
				}

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
			// Check for collision with new EditorGizmo arrows/cones
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, -1, QueryTriggerInteraction.Collide);

			foreach (var hit in hits)
			{
				if (hit.collider != null && hit.collider.gameObject != null && hit.collider.transform.parent != null)
				{
					var parent = hit.collider.transform.parent.gameObject;
					if (parent == gizmo.XArrow || parent == gizmo.YArrow || parent == gizmo.ZArrow)
					{
						string arrowName = parent.name;
						StartMovingObject(arrowName, ray);
						if (arrowName == "X") return GizmosArrow.X;
						if (arrowName == "Y") return GizmosArrow.Y;
						if (arrowName == "Z") return GizmosArrow.Z;
					}
					// Also check for cones
					if (parent == gizmo.XCone || parent == gizmo.YCone || parent == gizmo.ZCone)
					{
						string coneName = parent.name;
						// Cones are named X_Cone, Y_Cone, Z_Cone
						if (coneName.StartsWith("X")) { StartMovingObject("X", ray); return GizmosArrow.X; }
						if (coneName.StartsWith("Y")) { StartMovingObject("Y", ray); return GizmosArrow.Y; }
						if (coneName.StartsWith("Z")) { StartMovingObject("Z", ray); return GizmosArrow.Z; }
					}
				}
			}
			return GizmosArrow.None;
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
				// Local axes need to account for object's current orientation
				Vector3 direction = Vector3.zero;
				if (arrow == GizmosArrow.X) direction = Vector3.right;
				if (arrow == GizmosArrow.Y) direction = Vector3.up;
				if (arrow == GizmosArrow.Z) direction = Vector3.forward;
                
				// Transform the direction by the object's rotation
				return obj.transform.TransformDirection(direction);
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

		void CreateGridLineMaterial()
		{
			if (!gridLineMaterial)
			{
				Shader shader = Shader.Find("Hidden/Internal-Colored");
				gridLineMaterial = new Material(shader)
				{
					hideFlags = HideFlags.HideAndDontSave
				};
				gridLineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
				gridLineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				gridLineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
				gridLineMaterial.SetInt("_ZWrite", 0);
				gridLineMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.LessEqual); // Enable depth test
			}
		}

		void OnRenderObject()
		{
			if (!gridVisible || !gridLineMaterial) return;
			if (Camera.current != Camera.main) return;
			gridLineMaterial.SetPass(0);
			GL.PushMatrix();
			GL.MultMatrix(Matrix4x4.identity);
			GL.Begin(GL.LINES);
			// Grid colors
			Color gridColor = new Color(1f, 1f, 1f, 0.10f);
			Color axisColorX = new Color(1f, 0.3f, 0.3f, 0.25f);
			Color axisColorZ = new Color(0.3f, 0.6f, 1f, 0.25f);

			float y = gridHeight;
			Vector3 center = gridCenter;
			Camera cam = Camera.main;
			// --- Minimum region size: 200x200 ---
			float renderGridSize = Mathf.Max(gridSize, 0.1f); // Clamp minimum grid size for rendering
			float minRegion = 100f * renderGridSize; // 200x200 region
			float camRange = Mathf.Max(Mathf.Clamp(Vector3.Distance(cam.transform.position, center) * 2f, 32f * renderGridSize, 256f * renderGridSize), minRegion);
			float maxWorldRange = camRange;
			int maxLines = 512;
			int halfLines = Mathf.Clamp(Mathf.CeilToInt(maxWorldRange / renderGridSize), 1, maxLines);
			float minAlpha = 0.03f; // Minimum alpha for distant lines

			// Calculate visible grid bounds in world space
			Vector3 camPos = cam.transform.position;
			float gridY = y;
			float minX = Mathf.Floor((camPos.x - maxWorldRange) / renderGridSize) * renderGridSize;
			float maxX = Mathf.Ceil((camPos.x + maxWorldRange) / renderGridSize) * renderGridSize;
			float minZ = Mathf.Floor((camPos.z - maxWorldRange) / renderGridSize) * renderGridSize;
			float maxZ = Mathf.Ceil((camPos.z + maxWorldRange) / renderGridSize) * renderGridSize;

			// --- Smooth fade: use a wider fade region for nice blending ---
			float fadeStart = maxWorldRange * 0.7f;
			float fadeEnd = maxWorldRange;

			// Draw regular grid lines (skip axes)
			for (float x = minX; x <= maxX; x += renderGridSize)
			{
				if (Mathf.Approximately(x, center.x)) continue;
				float camDist = Mathf.Abs(x - camPos.x);
				float fade = 1f;
				if (camDist > fadeStart)
					fade = Mathf.InverseLerp(fadeEnd, fadeStart, camDist); // 1 at fadeStart, 0 at fadeEnd
				float alpha = Mathf.Lerp(minAlpha, gridColor.a, fade);
				Color fadedColor = new Color(gridColor.r, gridColor.g, gridColor.b, alpha);
				GL.Color(fadedColor);
				GL.Vertex3(x, gridY, minZ);
				GL.Vertex3(x, gridY, maxZ);
			}
			for (float z = minZ; z <= maxZ; z += renderGridSize)
			{
				if (Mathf.Approximately(z, center.z)) continue;
				float camDist = Mathf.Abs(z - camPos.z);
				float fade = 1f;
				if (camDist > fadeStart)
					fade = Mathf.InverseLerp(fadeEnd, fadeStart, camDist);
				float alpha = Mathf.Lerp(minAlpha, gridColor.a, fade);
				Color fadedColor = new Color(gridColor.r, gridColor.g, gridColor.b, alpha);
				GL.Color(fadedColor);
				GL.Vertex3(minX, gridY, z);
				GL.Vertex3(maxX, gridY, z);
			}
			// Draw axes last (no blending/overlap) - always draw at center.x and center.z
			GL.Color(axisColorX);
			GL.Vertex3(center.x, y, minZ);
			GL.Vertex3(center.x, y, maxZ);
			GL.Color(axisColorZ);
			GL.Vertex3(minX, y, center.z);
			GL.Vertex3(maxX, y, center.z);
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