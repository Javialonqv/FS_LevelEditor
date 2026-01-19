using FS_LevelEditor.UI_Related;
using Il2Cpp;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static System.Net.Mime.MediaTypeNames;

namespace FS_LevelEditor.Editor.UI
{
	[MelonLoader.RegisterTypeInIl2Cpp]
	public class SelectedObjPanel : MonoBehaviour
	{
		public static SelectedObjPanel Instance;

		GameObject header;
		UILabel headerTitle;
		public UITogglePatcher setActiveAtStartToggle;
		UIButtonPatcher expandPanelButton;
		UISprite expandPanelButtonSprite;
		UIButtonAsToggle globalObjAttributesToggle;

		GameObject body;
		Transform globalObjectPanelsParent;
		UICustomInputField posXField, posYField, posZField;
		UICustomInputField rotXField, rotYField, rotZField;
		UICustomInputField scaleXField, scaleYField, scaleZField;
		UITogglePatcher collisionToggle;
        UITogglePatcher invisibleMeshToggle;
        UIButtonPatcher addWaypointButton;
		UITogglePatcher startMovingAtStartToggle;
		UICustomInputField movingSpeedField;
		UICustomInputField startDelayField;
		UICustomInputField waitTimeField;
		UISmallButtonMultiple waypointModeButton;
		// ------------------------------
		bool showingPanel = false;
		bool panelIsExpanded = false;
		string currentHeaderLocKey = "";
		// ------------------------------
		Transform objectSpecificPanelsParent;
		Dictionary<LE_Object.ObjectType?, GameObject> attributesPanels = new Dictionary<LE_Object.ObjectType?, GameObject>();
		Transform whereToCreateObjAttributesParent;
		LE_Object.ObjectType currentlyCreatingPropsUIFor;

		static readonly Dictionary<(LE_Object.ObjectType objType, string propName), string> objectPropsTooltips = new Dictionary<(LE_Object.ObjectType objType, string propName), string>
		{
			{ (LE_Object.ObjectType.SAW, "TravelBack"), "TravelBackTooltip" },
			{ (LE_Object.ObjectType.SAW, "Loop"),		"LoopTooltip" },
		};
		// Object properties whose position will be the same as the latest added one.
		static readonly List<(LE_Object.ObjectType objType, string propName)> objectPropsWithNoYChange = new()
		{
			(LE_Object.ObjectType.DOOR, "InitialStateAuto"), // InitialStateAuto will be in the same position as InitialState.
			(LE_Object.ObjectType.DOOR_V2, "InitialStateAuto") // Same for Door V2.
		};
		static readonly Dictionary<string, Color> colorsForButtons = new Dictionary<string, Color>()
		{
			{ "DEACTIVATED", new Color(0.8f, 0f, 0f) },
			{ "ACTIVATED", Color.green },
			{ "UNUSABLE", Color.black },
			{ "ONCE", new Color(0.8f, 0.8f, 0.8f) },
			{ "MULTIPLE", Color.green },
			{ "CUBE_ONLY", Color.green },
			{ "RETRACTED", new Color(0.8f, 0f, 0f) },
			{ "DEPLOYED", Color.green },
			{ "CYAN", NGUI_Utils.fsButtonsDefaultColor },
			{ "GREEN", Color.green },
			{ "RED", new Color(0.8f, 0f, 0f) },
			{ "RELOCATION", new Color(0.8f, 0f, 0f) },
			{ "IMMINENT", Color.black },
			{ "CLOSED", new Color(0.8f, 0f, 0f) },
			{ "OPEN", Color.green },
			{ "LOCKED", new Color(0.8f, 0f, 0f) },
			{ "UNLOCKED", Color.green },
			{ "NONE", Color.black },
			{ "TRAVEL_BACK", Color.red },
			{ "LOOP", Color.blue },
		};
		static readonly string[] bannedPropertiesFromUI = new string[]
		{
			"AutoFontSize",
			"FontSize",
			"MinFontSize",
			"MaxFontSize",
			"TextAlign",
			"Text"
		};
		// For objects where the prop name is not the same as the loc key.
		static readonly Dictionary<string, string> correctLocKeysForProps = new Dictionary<string, string>()
		{
			{ "InstaKill", "InstantKill" },
			{ "IsAuto", "IsAutomatic" },
			{ "InitialStateAuto", "InitialState" }, // InitialStateAuto also uses the InitialState loc key.
			{ "InvertWithGravity", "InvertTextWithGravity" }, 
			{ "ColorType", "ScreenColor" }, 
			{ "DPS", "Damage" }, 
			{ "MoveSpeed", "MovingSpeed" }, 
			{ "CanUseTaser", "CanBeShotByTaser" }, 

			// Yes, button options also here.
			{ "NONE", "None_Mayus" },
			{ "TRAVEL_BACK", "TravelBack_Mayus" },
			{ "LOOP", "Loop_Mayus" },
		};
		// For object properties that are only visible/active when another property is set to a specific value (like toggles).
		static readonly Dictionary<(LE_Object.ObjectType? type, string propName), (string requiredPropName, object requiredPropValue)> optionalProps = new()
		{
			{ (LE_Object.ObjectType.DOOR, "InitialState"), ("IsAuto", false) },
			{ (LE_Object.ObjectType.DOOR, "InitialStateAuto"), ("IsAuto", true) },
            { (LE_Object.ObjectType.DOOR_V2, "InitialState"), ("IsAuto", false) },
            { (LE_Object.ObjectType.DOOR_V2, "InitialStateAuto"), ("IsAuto", true) },

			{ (LE_Object.ObjectType.LASER, "Damage"), ("InstaKill", false) },
			{ (LE_Object.ObjectType.LASER, "OffDuration"), ("Blinking", true) },
			{ (LE_Object.ObjectType.LASER, "OnDuration"), ("Blinking", true) },

			{ (LE_Object.ObjectType.SWITCH, "OnlyByTaser"), ("CanUseTaser", true) },

			{ (LE_Object.ObjectType.DEATH_TRIGGER, "TeleportCoordinates"), ("CustomCoordinates", true) },

			{ (LE_Object.ObjectType.SAW, "WaitTime"), ("waypoints", null) }, // If it's checking for waypoints, the code already checks if the list count is greater than 0.
        };

		bool isSelectingAnObjectRightNow = false;
		bool isSelectingMultipleObjects = false;
		LE_Object currentSelectedObj;
		bool executeCollisionToggleActions = true;
        bool executeInvisibleMeshToggleActions = true;

        Vector3 objPositionWhenSelectedField;
		Quaternion objRotationWhenSelectedField;
		Vector3 objScaleWhenSelectedField;

		public SelectedObjPanel(IntPtr ptr) : base (ptr) { }

		public static void Create(Transform editorUIParent)
		{
			GameObject root = new GameObject("CurrentSelectedObjPanel");
			root.transform.parent = editorUIParent;
			root.transform.localPosition = new Vector3(-690f, -220f, 0f); // Changed from -700f to -690f
			root.transform.localScale = Vector3.one;

			root.AddComponent<SelectedObjPanel>();
		}

		void Awake()
		{
			Instance = this;

			CreateHeader();
			CreateBody();
		}

		#region Create UI
		void CreateHeader()
		{
			header = new GameObject("Header");
			header.transform.parent = transform;
			header.transform.localPosition = Vector3.zero;
			header.transform.localScale = Vector3.one;

			UISprite sprite = header.AddComponent<UISprite>();
			sprite.atlas = NGUI_Utils.UITexturesAtlas;
			sprite.spriteName = "Square_Border_Beveled_HighOpacity";
			sprite.type = UIBasicSprite.Type.Sliced;
			sprite.color = new Color(0.218f, 0.6464f, 0.6509f, 1f);
			sprite.width = 520;
			sprite.height = 60;

			BoxCollider collider = header.AddComponent<BoxCollider>();
			collider.size = new Vector3(520f, 60f, 1f);

			headerTitle = NGUI_Utils.CreateLabel(header.transform, Vector3.zero, new Vector3Int(520, 60, 0), "selection.NoObjectSelected", NGUIText.Alignment.Center,
				UIWidget.Pivot.Center);
			headerTitle.name = "Label";
			headerTitle.fontSize = 27;
			headerTitle.depth = 1;

			CreateSetActiveAtStartToggle();
			CreateExpandPanelToggle();
			CreateGlobalObjectAttributesToggle();
		}
		void CreateSetActiveAtStartToggle()
		{

			setActiveAtStartToggle = NGUI_Utils.CreateToggle(header.transform, new Vector3(-220f, 0f, 0f),
                new Vector3Int(48, 48, 0));
            setActiveAtStartToggle.name = "SetActiveAtStartToggle";
			setActiveAtStartToggle.onClick += (state) => SetSetActiveAtStart();
            setActiveAtStartToggle.toggle.instantTween = true;

			FractalTooltip tooltip = setActiveAtStartToggle.gameObject.AddComponent<FractalTooltip>();
			tooltip.toolTipLocKey = "tooltip.SetActiveAtStartToggle";
			tooltip.staticTooltipPos = true;
			tooltip.staticTooltipOffset = new Vector2(0.42f, 0.1f);

            setActiveAtStartToggle.gameObject.SetActive(false);

			GameObject line = new GameObject("Line");
			line.transform.parent = setActiveAtStartToggle.gameObject.GetChild("Background").transform;
			line.transform.localPosition = Vector3.zero;
			line.transform.localScale = Vector3.one;

			UISprite lineSprite = line.AddComponent<UISprite>();
			lineSprite.atlas = NGUI_Utils.fractalSpaceAtlas;
			lineSprite.spriteName = "Square";
			lineSprite.width = 35;
			lineSprite.height = 6;
			lineSprite.depth = 8;
			line.SetActive(false);
		}
		void CreateExpandPanelToggle()
		{
			expandPanelButton = NGUI_Utils.CreateButtonWithSprite(header.transform, new Vector3(-160f, 0f, 0f), new Vector3Int(45, 45, 0), 2, "Triangle",
				new Vector2Int(25, 15));
			expandPanelButton.name = "ExpandPanelButton";
			expandPanelButton.onClick += ExpandButtonClick;
			expandPanelButton.GetComponent<UISprite>().depth = 1;

			expandPanelButtonSprite = expandPanelButton.gameObject.GetChildAt("Background/Label").GetComponent<UISprite>();

			expandPanelButton.gameObject.SetActive(false);
		}
		void CreateGlobalObjectAttributesToggle()
		{
			globalObjAttributesToggle = NGUI_Utils.CreateButtonAsToggleWithSprite(header.transform, new Vector3(220f, 0f, 0f), new Vector3Int(45, 45, 0), 2, "Global",
				Vector2Int.one * 25);
			globalObjAttributesToggle.name = "GlobalObjectAttributesBtnToggle";
			globalObjAttributesToggle.onClick += ShowGlobalObjectAttributes;
			globalObjAttributesToggle.gameObject.SetActive(false);
		}

		void CreateBody()
		{
			body = new GameObject("Body");
			body.transform.parent = gameObject.transform;
			body.transform.localScale = Vector3.one;
			body.layer = LayerMask.NameToLayer("2D GUI"); // To avoid the object not showing once the UIPanel attached.

			UISprite sprite = body.AddComponent<UISprite>();
			sprite.atlas = NGUI_Utils.UITexturesAtlas;
			sprite.spriteName = "Square_Border_Beveled_HighOpacity";
			sprite.type = UIBasicSprite.Type.Sliced;
			sprite.color = new Color(0.0039f, 0.3568f, 0.3647f, 1f);
			sprite.depth = -1;
			sprite.width = 500;
			sprite.height = 300;
			sprite.pivot = UIWidget.Pivot.Top;

			BoxCollider collider = body.AddComponent<BoxCollider>();
			collider.size = new Vector3(500f, 300f, 1f);
			collider.center = new Vector3(0f, -150f);

			// Add a UIPanel just to hide the objects outside of the panel.
			UIPanel panel = body.AddComponent<UIPanel>();
			panel.clipRange = new Vector4(0f, -150f, 500f, 280f);
			panel.clipping = UIDrawCall.Clipping.SoftClip;

			body.transform.localPosition = new Vector3(0f, -10f, 0f);

			CreateGlobalObjectsOptionsParent();
			CreateGlobalObjectAttributesPanel();

			CreateObjectSpecificOptionsParent();
			CreateObjectSpecificOptionsPanels();

			SetSelectedObjPanelAsNone();
		}
		// ------------------------------
		int yPosForGlobalProps = 90;
		void CreateGlobalObjectsOptionsParent()
		{
			GameObject globalObjectOptionsParent = new GameObject("GlobalObjectOptions");
			globalObjectOptionsParent.transform.parent = body.transform;
			globalObjectOptionsParent.transform.localPosition = new Vector3(0f, -150f);
			globalObjectOptionsParent.transform.localScale = Vector3.one;
			globalObjectPanelsParent = globalObjectOptionsParent.transform;
		}
		void CreateGlobalObjectAttributesPanel()
		{
			CreateObjectPositionUIElements();
			CreateObjectRotationUIElements();
			CreateObjectScaleUIElements();
			CreateCollisionToggle();
			CreateInvisibleMeshToggle();
			CreateAddWaypointButton();
			CreateStartMovingAtStartToggle();
			CreateMovingSpeedField();
			CreateStartDelayField();
			CreateWaitTimeField();
			CreateWaypointModeButton();
		}
		void CreateObjectPositionUIElements()
		{
			Transform positionThingsParent = new GameObject("Position").transform;
			positionThingsParent.parent = globalObjectPanelsParent;
			positionThingsParent.localPosition = Vector3.zero;
			positionThingsParent.localScale = Vector3.one;

			UILabel title = NGUI_Utils.CreateLabel(positionThingsParent, new Vector3(-230f, yPosForGlobalProps, 0f), new Vector3Int(150, 38, 0), "Position");
			title.name = "Title";

			UILabel xTitle = NGUI_Utils.CreateLabel(positionThingsParent, new Vector3(-40f, yPosForGlobalProps, 0f), new Vector3Int(28, 38, 0), "X", NGUIText.Alignment.Center,
				UIWidget.Pivot.Center);
			xTitle.name = "XTitle";
			// ------------------------------
			posXField = NGUI_Utils.CreateInputField(positionThingsParent, new Vector3(10f, yPosForGlobalProps, 0f), new Vector3Int(65, 38, 0), 27, "0", inputType: UICustomInputField.UIInputType.FLOAT,
				maxDecimals: 3);
			posXField.name = "XField";
			posXField.onSelected += (() => OnGlobalAttributeFieldSelected(GlobalFieldType.Position));
			posXField.onChange += (() => SetPropertyWithInput("XPosition", posXField, true));
			posXField.onDeselected += (() => OnGlobalAttributeFieldDeselected(GlobalFieldType.Position));

			UILabel yTitle = NGUI_Utils.CreateLabel(positionThingsParent, new Vector3(60f, yPosForGlobalProps, 0f), new Vector3Int(28, 38, 0), "Y", NGUIText.Alignment.Center,
				UIWidget.Pivot.Center);
			yTitle.name = "YTitle";
			// ------------------------------
			posYField = NGUI_Utils.CreateInputField(positionThingsParent, new Vector3(110f, yPosForGlobalProps, 0f), new Vector3Int(65, 38, 0), 27, "0", inputType: UICustomInputField.UIInputType.FLOAT,
				maxDecimals: 3);
			posYField.name = "YField";
			posYField.onSelected += (() => OnGlobalAttributeFieldSelected(GlobalFieldType.Position));
			posYField.onChange += (() => SetPropertyWithInput("YPosition", posYField, true));
			posYField.onDeselected += (() => OnGlobalAttributeFieldDeselected(GlobalFieldType.Position));

			UILabel zTitle = NGUI_Utils.CreateLabel(positionThingsParent, new Vector3(160f, yPosForGlobalProps, 0f), new Vector3Int(28, 38, 0), "Z", NGUIText.Alignment.Center,
				UIWidget.Pivot.Center);
			zTitle.name = "ZTitle";
			// ------------------------------
			posZField = NGUI_Utils.CreateInputField(positionThingsParent, new Vector3(210f, yPosForGlobalProps, 0f), new Vector3Int(65, 38, 0), 27, "0", inputType: UICustomInputField.UIInputType.FLOAT,
				maxDecimals: 3);
			posZField.name = "ZField";
			posZField.onSelected += (() => OnGlobalAttributeFieldSelected(GlobalFieldType.Position));
			posZField.onChange += (() => SetPropertyWithInput("ZPosition", posZField, true));
			posZField.onDeselected += (() => OnGlobalAttributeFieldDeselected(GlobalFieldType.Position));

			yPosForGlobalProps -= 50;
		}
		void CreateObjectRotationUIElements()
		{
			Transform rotationThingsParent = new GameObject("Rotation").transform;
			rotationThingsParent.parent = globalObjectPanelsParent;
			rotationThingsParent.localPosition = Vector3.zero;
			rotationThingsParent.localScale = Vector3.one;

			UILabel title = NGUI_Utils.CreateLabel(rotationThingsParent, new Vector3(-230f, yPosForGlobalProps, 0f), new Vector3Int(150, 38, 0), "Rotation");
			title.name = "Title";

			UILabel xTitle = NGUI_Utils.CreateLabel(rotationThingsParent, new Vector3(-40f, yPosForGlobalProps, 0f), new Vector3Int(28, 38, 0), "X", NGUIText.Alignment.Center,
				UIWidget.Pivot.Center);
			xTitle.name = "XTitle";
			// ------------------------------
			rotXField = NGUI_Utils.CreateInputField(rotationThingsParent, new Vector3(10f, yPosForGlobalProps, 0f), new Vector3Int(65, 38, 0), 27, "0", inputType: UICustomInputField.UIInputType.FLOAT,
				maxDecimals: 3);
			rotXField.name = "XField";
			rotXField.onSelected += (() => OnGlobalAttributeFieldSelected(GlobalFieldType.Rotation));
			rotXField.onChange += (() => SetPropertyWithInput("XRotation", rotXField, true));
			rotXField.onDeselected += (() => OnGlobalAttributeFieldDeselected(GlobalFieldType.Rotation));

			UILabel yTitle = NGUI_Utils.CreateLabel(rotationThingsParent, new Vector3(60f, yPosForGlobalProps, 0f), new Vector3Int(28, 38, 0), "Y", NGUIText.Alignment.Center,
				UIWidget.Pivot.Center);
			yTitle.name = "YTitle";
			// ------------------------------
			rotYField = NGUI_Utils.CreateInputField(rotationThingsParent, new Vector3(110f, yPosForGlobalProps, 0f), new Vector3Int(65, 38, 0), 27, "0", inputType: UICustomInputField.UIInputType.FLOAT,
				maxDecimals: 3);
			rotYField.name = "YField";
			rotYField.onSelected += (() => OnGlobalAttributeFieldSelected(GlobalFieldType.Rotation));
			rotYField.onChange += (() => SetPropertyWithInput("YRotation", rotYField, true));
			rotYField.onDeselected += (() => OnGlobalAttributeFieldDeselected(GlobalFieldType.Rotation));

			UILabel zTitle = NGUI_Utils.CreateLabel(rotationThingsParent, new Vector3(160f, yPosForGlobalProps, 0f), new Vector3Int(28, 38, 0), "Z", NGUIText.Alignment.Center,
				UIWidget.Pivot.Center);
			zTitle.name = "ZTitle";
			// ------------------------------
			rotZField = NGUI_Utils.CreateInputField(rotationThingsParent, new Vector3(210f, yPosForGlobalProps, 0f), new Vector3Int(65, 38, 0), 27, "0", inputType: UICustomInputField.UIInputType.FLOAT,
				maxDecimals: 3);
			rotZField.name = "ZField";
			rotZField.onSelected += (() => OnGlobalAttributeFieldSelected(GlobalFieldType.Rotation));
			rotZField.onChange += (() => SetPropertyWithInput("ZRotation", rotZField, true));
			rotZField.onDeselected += (() => OnGlobalAttributeFieldDeselected(GlobalFieldType.Rotation));

			yPosForGlobalProps -= 50;
		}
		void CreateObjectScaleUIElements()
		{
			Transform scaleThingsParent = new GameObject("Scale").transform;
			scaleThingsParent.parent = globalObjectPanelsParent;
			scaleThingsParent.localPosition = Vector3.zero;
			scaleThingsParent.localScale = Vector3.one;

			UILabel title = NGUI_Utils.CreateLabel(scaleThingsParent, new Vector3(-230f, yPosForGlobalProps, 0f), new Vector3Int(150, 38, 0), "Scale");
			title.name = "Title";

			UILabel xTitle = NGUI_Utils.CreateLabel(scaleThingsParent, new Vector3(-40f, yPosForGlobalProps, 0f), new Vector3Int(28, 38, 0), "X", NGUIText.Alignment.Center,
				UIWidget.Pivot.Center);
			xTitle.name = "XTitle";
			// ------------------------------
			scaleXField = NGUI_Utils.CreateInputField(scaleThingsParent, new Vector3(10f, yPosForGlobalProps, 0f), new Vector3Int(65, 38, 0), 27, "1", inputType: UICustomInputField.UIInputType.FLOAT,
				maxDecimals: 3);
			scaleXField.name = "XField";
			scaleXField.onSelected += (() => OnGlobalAttributeFieldSelected(GlobalFieldType.Scale));
			scaleXField.onChange += (() => SetPropertyWithInput("XScale", scaleXField, true));
			scaleXField.onDeselected += (() => OnGlobalAttributeFieldDeselected(GlobalFieldType.Scale));

			UILabel yTitle = NGUI_Utils.CreateLabel(scaleThingsParent, new Vector3(60f, yPosForGlobalProps, 0f), new Vector3Int(28, 38, 0), "Y", NGUIText.Alignment.Center,
				UIWidget.Pivot.Center);
			yTitle.name = "YTitle";
			// ------------------------------
			scaleYField = NGUI_Utils.CreateInputField(scaleThingsParent, new Vector3(110f, yPosForGlobalProps, 0f), new Vector3Int(65, 38, 0), 27, "1", inputType: UICustomInputField.UIInputType.FLOAT,
				maxDecimals: 3);
			scaleYField.name = "YField";
			scaleYField.onSelected += (() => OnGlobalAttributeFieldSelected(GlobalFieldType.Scale));
			scaleYField.onChange += (() => SetPropertyWithInput("YScale", scaleYField, true));
			scaleYField.onDeselected += (() => OnGlobalAttributeFieldDeselected(GlobalFieldType.Scale));

			UILabel zTitle = NGUI_Utils.CreateLabel(scaleThingsParent, new Vector3(160f, yPosForGlobalProps, 0f), new Vector3Int(28, 38, 0), "Z", NGUIText.Alignment.Center,
				UIWidget.Pivot.Center);
			zTitle.name = "ZTitle";
			// ------------------------------
			scaleZField = NGUI_Utils.CreateInputField(scaleThingsParent, new Vector3(210f, yPosForGlobalProps, 0f), new Vector3Int(65, 38, 0), 27, "1", inputType: UICustomInputField.UIInputType.FLOAT,
				maxDecimals: 3);
			scaleZField.name = "ZField";
			scaleZField.onSelected += (() => OnGlobalAttributeFieldSelected(GlobalFieldType.Scale));
			scaleZField.onChange += (() => SetPropertyWithInput("ZScale", scaleZField, true));
			scaleZField.onDeselected += (() => OnGlobalAttributeFieldDeselected(GlobalFieldType.Scale));

			yPosForGlobalProps -= 50;
		}
		void CreateCollisionToggle()
		{
			Transform collisionToggleParent = new GameObject("Collision").transform;
			collisionToggleParent.parent = globalObjectPanelsParent;
			collisionToggleParent.localPosition = Vector3.zero;
			collisionToggleParent.localScale = Vector3.one;

			UILabel title = NGUI_Utils.CreateLabel(collisionToggleParent, new Vector3(-230, yPosForGlobalProps), new Vector3Int(395, 38, 0), "Collision");
			title.name = "Title";

            collisionToggle = NGUI_Utils.CreateToggle(collisionToggleParent, new Vector3(200, yPosForGlobalProps), Vector3Int.one * 48);
            collisionToggle.gameObject.name = "Toggle";
            collisionToggle.onClick += (state) => SetCollisionToggle();
			collisionToggle.toggle.instantTween = true;

			GameObject line = new GameObject("Line");
			line.transform.parent = collisionToggle.gameObject.GetChild("Background").transform;
			line.transform.localPosition = Vector3.zero;
			line.transform.localScale = Vector3.one;

			UISprite lineSprite = line.AddComponent<UISprite>();
			lineSprite.atlas = NGUI_Utils.fractalSpaceAtlas;
			lineSprite.spriteName = "Square";
			lineSprite.width = 35;
			lineSprite.height = 6;
			lineSprite.depth = 8;
			line.SetActive(false);

			yPosForGlobalProps -= 55;
		}

        void CreateInvisibleMeshToggle()
        {
            Transform invisibleMeshToggleParent = new GameObject("InvisibleMesh").transform;
            invisibleMeshToggleParent.parent = globalObjectPanelsParent;
            invisibleMeshToggleParent.localPosition = Vector3.zero;
            invisibleMeshToggleParent.localScale = Vector3.one;

            UILabel title = NGUI_Utils.CreateLabel(invisibleMeshToggleParent, new Vector3(-230, yPosForGlobalProps), new Vector3Int(395, 38, 0), "InvisibleMesh");
            title.name = "Title";

            invisibleMeshToggle = NGUI_Utils.CreateToggle(invisibleMeshToggleParent, new Vector3(200, yPosForGlobalProps), Vector3Int.one * 48);
            invisibleMeshToggle.gameObject.name = "Toggle";
			invisibleMeshToggle.onClick += (state) => SetInvisibleMeshToggle();
            invisibleMeshToggle.toggle.instantTween = true;

            GameObject line = new GameObject("Line");
            line.transform.parent = invisibleMeshToggle.gameObject.GetChild("Background").transform;
            line.transform.localPosition = Vector3.zero;
            line.transform.localScale = Vector3.one;

            UISprite lineSprite = line.AddComponent<UISprite>();
            lineSprite.atlas = NGUI_Utils.fractalSpaceAtlas;
            lineSprite.spriteName = "Square";
            lineSprite.width = 35;
            lineSprite.height = 6;
            lineSprite.depth = 8;
            line.SetActive(false);

            yPosForGlobalProps -= 55;
        }
        void CreateAddWaypointButton()
		{
			addWaypointButton = NGUI_Utils.CreateButton(globalObjectPanelsParent, new Vector3(0, yPosForGlobalProps), new Vector3Int(480, 50, 0), "AddGlobalWaypoint");
			addWaypointButton.name = "AddWaypointButton";
			addWaypointButton.onClick += AddWaypointForObject;
			addWaypointButton.GetComponent<UIButtonScale>().hover = Vector3.one * 1.05f;
			addWaypointButton.GetComponent<UIButtonScale>().pressed = Vector3.one * 1.02f;

			yPosForGlobalProps -= 55;
		}
		void CreateStartMovingAtStartToggle()
		{
			Transform toggleParent = new GameObject("StartMovingAtStart").transform;
			toggleParent.parent = globalObjectPanelsParent;
			toggleParent.localPosition = Vector3.zero;
			toggleParent.localScale = Vector3.one;

			UILabel title = NGUI_Utils.CreateLabel(toggleParent, new Vector3(-230, yPosForGlobalProps), new Vector3Int(395, 38, 0), "StartMovingAtStart");
			title.name = "Title";

            startMovingAtStartToggle = NGUI_Utils.CreateToggle(toggleParent, new Vector3(200, yPosForGlobalProps), Vector3Int.one * 48);
            startMovingAtStartToggle.gameObject.name = "Toggle";
			startMovingAtStartToggle.onClick += (state) => SetStartMovingAtStart();
			startMovingAtStartToggle.toggle.instantTween = true;

			yPosForGlobalProps -= 50;
		}
		void CreateMovingSpeedField()
		{
			Transform fieldParent = new GameObject("MovingSpeed").transform;
			fieldParent.parent = globalObjectPanelsParent;
			fieldParent.localPosition = Vector3.zero;
			fieldParent.localScale = Vector3.one;

			UILabel title = NGUI_Utils.CreateLabel(fieldParent, new Vector3(-230f, yPosForGlobalProps, 0f), new Vector3Int(260, 38, 0), "MovingSpeed");
			title.name = "Title";

			movingSpeedField = NGUI_Utils.CreateInputField(fieldParent, new Vector3(140, yPosForGlobalProps), new Vector3Int(200, 38, 0), 27, "5", false,
				inputType: UICustomInputField.UIInputType.NON_NEGATIVE_FLOAT);
			movingSpeedField.name = "Field";
			movingSpeedField.onChange += () => SetPropertyWithInput("MovingSpeed", movingSpeedField, true);

			yPosForGlobalProps -= 50;
		}
		void CreateStartDelayField()
		{
			Transform fieldParent = new GameObject("StartDelay").transform;
			fieldParent.parent = globalObjectPanelsParent;
			fieldParent.localPosition = Vector3.zero;
			fieldParent.localScale = Vector3.one;

			UILabel title = NGUI_Utils.CreateLabel(fieldParent, new Vector3(-230f, yPosForGlobalProps, 0f), new Vector3Int(260, 38, 0), "StartDelay");
			title.name = "Title";

			startDelayField = NGUI_Utils.CreateInputField(fieldParent, new Vector3(140, yPosForGlobalProps), new Vector3Int(200, 38, 0), 27, "0", false,
				inputType: UICustomInputField.UIInputType.NON_NEGATIVE_FLOAT);
			startDelayField.name = "Field";
			startDelayField.onChange += () => SetPropertyWithInput("StartDelay", startDelayField, true);

			yPosForGlobalProps -= 50;
		}
		void CreateWaitTimeField()
		{
			Transform fieldParent = new GameObject("WaitTime").transform;
			fieldParent.parent = globalObjectPanelsParent;
			fieldParent.localPosition = Vector3.zero;
			fieldParent.localScale = Vector3.one;

			UILabel title = NGUI_Utils.CreateLabel(fieldParent, new Vector3(-230f, yPosForGlobalProps, 0f), new Vector3Int(260, 38, 0), "WaitTime");
			title.name = "Title";

			waitTimeField = NGUI_Utils.CreateInputField(fieldParent, new Vector3(140, yPosForGlobalProps), new Vector3Int(200, 38, 0), 27, "0", false,
				inputType: UICustomInputField.UIInputType.NON_NEGATIVE_FLOAT);
			waitTimeField.name = "Field";
			waitTimeField.onChange += () => SetPropertyWithInput("WaitTime", waitTimeField, true);

			yPosForGlobalProps -= 50;
		}
		void CreateWaypointModeButton()
		{
			var optionParent = new GameObject("WaypointMode").transform;
			optionParent.parent = globalObjectPanelsParent;
			optionParent.localPosition = Vector3.zero;
			optionParent.localScale = Vector3.one;

			UILabel title = NGUI_Utils.CreateLabel(optionParent, new Vector3(-230f, yPosForGlobalProps, 0f), new Vector3Int(260, 38, 0), "MovementMode");
			title.name = "Title";

			waypointModeButton = NGUI_Utils.CreateSmallButtonMultiple(optionParent, new Vector3(140, yPosForGlobalProps),
				new Vector3Int(200, 38, 0), "NONE", 25);
			waypointModeButton.name = "ButtonMultiple";
			waypointModeButton.onChange += (id) => SetPropertyWithButtonMultiple("WaypointMode", waypointModeButton);
			waypointModeButton.GetComponent<UIButtonScale>().hover = Vector3.one * 1.05f;
			waypointModeButton.GetComponent<UIButtonScale>().pressed = Vector3.one * 1.02f;
			waypointModeButton.AddOption("None_Mayus", Color.black);
			waypointModeButton.AddOption("TravelBack_Mayus", Color.red);
			waypointModeButton.AddOption("Loop_Mayus", Color.blue);

			yPosForGlobalProps -= 50;
		}
		// ------------------------------
		void CreateObjectSpecificOptionsParent()
		{
			GameObject objectSpecificOptionsParent = new GameObject("ObjectSpecificOptions");
			objectSpecificOptionsParent.transform.parent = body.transform;
			objectSpecificOptionsParent.transform.localPosition = new Vector3(0f, -150f);
			objectSpecificOptionsParent.transform.localScale = Vector3.one;
			objectSpecificPanelsParent = objectSpecificOptionsParent.transform;
		}
		void CreateObjectSpecificOptionsPanels()
		{
			foreach (LE_Object.ObjectType type in Enum.GetValues(typeof(LE_Object.ObjectType)))
			{
                string className = "LE_" + Utils.ObjectTypeToFormatedName(type).Replace(' ', '_');
                Type classType = Type.GetType("FS_LevelEditor." + className);
				if (classType == null) continue;

                Utils.CallStaticMethodIfExists(classType, "GetDefaultProperties", out object defaultProps);
				if (defaultProps == null || ((Dictionary<string, object>)defaultProps).Count == 0) continue;

				CreateObjectSpecificOptionsFor(type, (Dictionary<string, object>)defaultProps);
            }
        }
		void CreateObjectSpecificOptionsFor(LE_Object.ObjectType type, Dictionary<string, object> defaultProps)
		{
			GameObject parent = new GameObject(type.ToString());
			parent.transform.parent = objectSpecificPanelsParent;
			parent.transform.localPosition = Vector3.zero;
			parent.transform.localScale = Vector3.one;

			SetCurrentParentToCreateAttributes(parent);
			currentlyCreatingPropsUIFor = type;

			bool alreadyCreatedManageEventsButton = false;
			foreach (var prop in defaultProps)
			{
				object value = prop.Value;

				if (bannedPropertiesFromUI.Contains(prop.Key)) continue;

				if (value is List<WaypointData>) continue;

                string locName = prop.Key;
				AttributeType propType = AttributeType.INPUT_FIELD;
				UICustomInputField.UIInputType? inputType = UICustomInputField.UIInputType.HEX_COLOR;
				object defaultValue = value;
				string targetPropName = prop.Key;
				string tooltipKey = null;
				bool dontChangeYPos = false;

				if (value is Color colorValue)
				{
					locName = "ColorHex";
					propType = AttributeType.INPUT_FIELD;
					inputType = UICustomInputField.UIInputType.HEX_COLOR;
					defaultValue = Utils.ColorToHex(colorValue);
				}
				else if (value is float floatValue)
				{
					locName = prop.Key;
					propType = AttributeType.INPUT_FIELD;
					inputType = UICustomInputField.UIInputType.NON_NEGATIVE_FLOAT;
					defaultValue = floatValue.ToString();
				}
                else if (value is int intValue)
                {
                    locName = prop.Key;
                    propType = AttributeType.INPUT_FIELD;
                    inputType = UICustomInputField.UIInputType.NON_NEGATIVE_INT;
					defaultValue = intValue.ToString();
                }
                else if (value is bool boolValue)
				{
					locName = prop.Key;
					propType = AttributeType.TOGGLE;
					inputType = null;
					defaultValue = boolValue;
				}
				else if (value is Enum enumValue)
				{
					locName = prop.Key;
					propType = AttributeType.BUTTON_MULTIPLE;
					inputType = null;
					defaultValue = enumValue;
				}
				else if (value is List<LE_Event>)
				{
					if (alreadyCreatedManageEventsButton) continue;

					locName = "ManageEvents";
					propType = AttributeType.BUTTON;
					inputType = null;
					targetPropName = "ManageEvents";

					alreadyCreatedManageEventsButton = true;
				}
				else if (value is Vector3 vector3Value)
				{
					locName = prop.Key;
					propType = AttributeType.VECTOR;
					inputType = null;
					defaultValue = vector3Value;
				}

                // Get tooltip if exists.
                objectPropsTooltips.TryGetValue((type, prop.Key), out tooltipKey);

				// Determine if this prop should be in the same position as the last one.
				dontChangeYPos = objectPropsWithNoYChange.Contains((type, prop.Key));

				// In case the loc key is not the same as the prop name, set it.
				if (correctLocKeysForProps.TryGetValue(prop.Key, out string correctLocKey)) locName = correctLocKey;

				var created = CreateObjectAttribute(locName, propType, defaultValue, inputType, targetPropName, inputType == UICustomInputField.UIInputType.HEX_COLOR, tooltipKey, dontChangeYPos);

                #region Add Options To Small Button If It Is
                if (created is UISmallButtonMultiple smallBtn)
				{
                    foreach (var enumEntry in Enum.GetNames(value.GetType()))
                    {
						Color entryColor = colorsForButtons.GetValueOrDefault(enumEntry, NGUI_Utils.fsButtonsDefaultColor);

                        smallBtn.AddOption(correctLocKeysForProps.GetValueOrDefault(enumEntry, enumEntry), entryColor);
                    }
                }
                #endregion
            }

			if (ShouldHaveEditTextButton(defaultProps))
			{
				CreateObjectAttribute("EditText", AttributeType.BUTTON, null, null, "EditText");
			}

            // Add "Add Waypoint" button if it has local waypoints.
            if (LE_Object.customWaypointSupports.ContainsKey(type))
			{
				string addWaypointBtnLocKey = "Add" + type.ToString().Replace("_", string.Empty) + "Waypoint";
				CreateObjectAttribute(addWaypointBtnLocKey, AttributeType.BUTTON, null, null, "AddWaypoint");
			}

			attributesPanels.Add(type, parent);
			parent.SetActive(false);
		}
		bool ShouldHaveEditTextButton(Dictionary<string, object> props)
		{
			string[] textProps = { "AutoFontSize", "FontSize", "MinFontSize", "MaxFontSize", "TextAlign", "Text" };

			return textProps.All(p => props.ContainsKey(p));
		}

		enum AttributeType { TOGGLE, INPUT_FIELD, BUTTON, BUTTON_MULTIPLE, VECTOR }
		void SetCurrentParentToCreateAttributes(GameObject newParent)
		{
			whereToCreateObjAttributesParent = newParent.transform;
		}

		object CreateObjectAttribute(string text, AttributeType attrType, object defaultValue, UICustomInputField.UIInputType? fieldType, string targetPropName,
			bool createHastag = false, string tooltip = null, bool dontChangeYPos = false, int? maxLength = null)
		{
			object toReturn = null;

			GameObject attributeParent = new GameObject(targetPropName);
			attributeParent.transform.parent = whereToCreateObjAttributesParent;
			attributeParent.transform.localPosition = Vector3.zero;
			attributeParent.transform.localScale = Vector3.one;

			float yPos = 90 - (50 * (whereToCreateObjAttributesParent.gameObject.GetChilds().Where(x => !x.ExistsChild("IgnoreYPos")).ToArray().Length - 1));
			if (dontChangeYPos) yPos += 50;

			if (attrType != AttributeType.BUTTON)
			{
				int titleWidth = (attrType == AttributeType.INPUT_FIELD || attrType == AttributeType.BUTTON_MULTIPLE || attrType == AttributeType.VECTOR) ? 260 : 395;
				if (createHastag) titleWidth = 235;
				UILabel title = NGUI_Utils.CreateLabel(attributeParent.transform, new Vector3(-230, yPos), new Vector3Int(titleWidth, NGUI_Utils.defaultLabelSize.y, 0),
					text);
				title.name = "Title";
			}

			if (createHastag && attrType == AttributeType.INPUT_FIELD)
			{
				UILabel hashtagLOL = NGUI_Utils.CreateLabel(attributeParent.transform, new Vector3(15, yPos), new Vector3Int(20, NGUI_Utils.defaultLabelSize.y, 0), "#",
					NGUIText.Alignment.Center, UIWidget.Pivot.Left);
				hashtagLOL.name = "HashtagLOL";
				hashtagLOL.color = Color.white;
			}

			if (attrType == AttributeType.INPUT_FIELD)
			{
				var field = NGUI_Utils.CreateInputField(attributeParent.transform, new Vector3(140, yPos), new Vector3Int(200, 38, 0), 27, (string)defaultValue, false,
					inputType: (UICustomInputField.UIInputType)fieldType);
				field.name = "Field";
				field.setFieldColorAutomatically = false;
				field.onChange += () => SetPropertyWithInput(targetPropName, field);

				if (maxLength.HasValue)
				{
					field.input.characterLimit = maxLength.Value;
				}

				toReturn = field;
			}
			else if (attrType == AttributeType.TOGGLE)
			{
				UITogglePatcher toggle = NGUI_Utils.CreateToggle(attributeParent.transform, new Vector3(200f, yPos), new Vector3Int(48, 48, 0));
				toggle.gameObject.name = "Toggle";
				var targetObjType = currentlyCreatingPropsUIFor;
				toggle.onClick += (state) => SetPropertyWithToggle(targetObjType, targetPropName, toggle.isChecked);
				if ((bool)defaultValue) toggle.Set(true);
				if (tooltip != null)
				{
					toggle.gameObject.AddComponent<FractalTooltip>().toolTipLocKey = tooltip;
				}

				toReturn = toggle.GetComponent<UIToggle>();
			}
			else if (attrType == AttributeType.BUTTON)
			{
				UIButtonPatcher button = NGUI_Utils.CreateButton(attributeParent.transform, new Vector3(0, yPos), new Vector3Int(480, 50, 0), text);
				button.name = "Button";
				button.onClick += () => TriggerAction(targetPropName);
				button.GetComponent<UIButtonScale>().hover = Vector3.one * 1.05f;
				button.GetComponent<UIButtonScale>().pressed = Vector3.one * 1.02f;
				if (tooltip != null)
				{
					button.gameObject.AddComponent<FractalTooltip>().toolTipLocKey = tooltip;
				}

				toReturn = button;
			}
			else if (attrType == AttributeType.BUTTON_MULTIPLE)
			{
				UISmallButtonMultiple button = NGUI_Utils.CreateSmallButtonMultiple(attributeParent.transform, new Vector3(140, yPos),
					new Vector3Int(200, 38, 0), text, 25);
				button.name = "ButtonMultiple";
				button.onChange += (id) => SetPropertyWithButtonMultiple(targetPropName, button);
				button.GetComponent<UIButtonScale>().hover = Vector3.one * 1.05f;
				button.GetComponent<UIButtonScale>().pressed = Vector3.one * 1.02f;
				if (tooltip != null)
				{
					button.gameObject.AddComponent<FractalTooltip>().toolTipLocKey = tooltip;
				}

				toReturn = button;
			}
			else if (attrType == AttributeType.VECTOR)
			{
				string[] defaultValues = { "0", "0", "0" };
				if (defaultValue is string defaultString && !string.IsNullOrEmpty(defaultString))
				{
					string[] parsedValues = defaultString.Split(',');
					for (int i = 0; i < parsedValues.Length && i < 3; i++)
					{
						string trimmedValue = parsedValues[i].Trim();
						if (!string.IsNullOrEmpty(trimmedValue))
						{
							defaultValues[i] = trimmedValue;
						}
					}
				}
				var inputTypeForVector = fieldType ?? UICustomInputField.UIInputType.FLOAT;

				float startX = 70f;
				float fieldWidth = 60f;
				float spacing = 2f;

				UICustomInputField xField = NGUI_Utils.CreateInputField(attributeParent.transform, new Vector3(startX, yPos), new Vector3Int((int)fieldWidth, 38, 0), 27, defaultValues[0], inputType: inputTypeForVector);
				xField.name = "XField";
				// **THE FIX**: Call the new method that rebuilds the vector from the UI
				xField.onChange += () => SetVector3PropertyWithInput(targetPropName, attributeParent);

				// Y Coordinate
				UICustomInputField yField = NGUI_Utils.CreateInputField(attributeParent.transform, new Vector3(startX + fieldWidth + spacing, yPos), new Vector3Int((int)fieldWidth, 38, 0), 27, defaultValues[1], inputType: inputTypeForVector);
				yField.name = "YField";
				yField.onChange += () => SetVector3PropertyWithInput(targetPropName, attributeParent);

				// Z Coordinate
				UICustomInputField zField = NGUI_Utils.CreateInputField(attributeParent.transform, new Vector3(startX + (fieldWidth + spacing) * 2, yPos), new Vector3Int((int)fieldWidth, 38, 0), 27, defaultValues[2], inputType: inputTypeForVector);
				zField.name = "ZField";
				zField.onChange += () => SetVector3PropertyWithInput(targetPropName, attributeParent);

				if (tooltip != null)
				{
					attributeParent.AddComponent<FractalTooltip>().toolTipLocKey = tooltip;
				}
			}

			if (dontChangeYPos)
			{
				GameObject ignoreYPosObj = new GameObject("IgnoreYPos");
				ignoreYPosObj.transform.parent = attributeParent.transform;
				ignoreYPosObj.transform.localPosition = Vector3.zero;
				ignoreYPosObj.transform.localScale = Vector3.one;
			}

			return toReturn;
		}
		#endregion

		public void ShowPanel(bool show, string headerLocKey) => ShowPanel(show, panelIsExpanded, headerLocKey);
		public void ShowPanel(bool show, bool expand, string headerLocKey)
		{
			headerTitle.SetLocKey(headerLocKey);
			currentHeaderLocKey = headerLocKey;

			if (show)
			{
				// Show both header and body when panel is active
				header.SetActive(true);
				
				// Ensure button is visible when panel is shown
				expandPanelButton.gameObject.SetActive(true);

				if (!expand) // Normal selection
				{
					gameObject.transform.localPosition = new Vector3(-690f, -220, 0f); // Changed from -700f to -690f
					headerTitle.width = 300;
					body.SetActive(true);
					body.GetComponent<UISprite>().height = 300;
					body.GetComponent<BoxCollider>().center = new Vector3(0, -150f);
					body.GetComponent<BoxCollider>().size = new Vector3(500, 300);
					body.GetComponent<UIPanel>().clipRange = new Vector4(0f, -150f, 500, 260);
				}
				else // EXPANDED PANEL
				{
					gameObject.transform.localPosition = new Vector3(-690f, 500, 0f); // Changed from -700f to -690f
					headerTitle.width = 300;
					body.SetActive(true);
					body.GetComponent<UISprite>().height = 1020;
					body.GetComponent<BoxCollider>().center = new Vector3(0, -510f);
					body.GetComponent<BoxCollider>().size = new Vector3(500, 1020);
					body.GetComponent<UIPanel>().clipRange = new Vector4(0f, -510f, 500, 1000);
				}

				panelIsExpanded = expand;
			}
			else
			{
				// Hide both header and body when nothing is selected
				header.SetActive(false);
				body.SetActive(false);
				setActiveAtStartToggle.gameObject.SetActive(false);
				expandPanelButton.gameObject.SetActive(false);
				globalObjAttributesToggle.gameObject.SetActive(false);
				panelIsExpanded = false;
			}

			showingPanel = show;
		}
		public void ExpandButtonClick()
		{
			if (!showingPanel) return; // Don't process clicks if panel isn't shown

			// Toggle expanded state and update panel immediately
			panelIsExpanded = !panelIsExpanded;
			ShowPanel(true, panelIsExpanded, currentHeaderLocKey);

			// Update button sprite orientation
			if (expandPanelButtonSprite != null)
			{
				expandPanelButtonSprite.transform.localScale = new Vector3(1f, panelIsExpanded ? -1 : 1, 1);
			}
		}
		public void UpdateHeaderTitle()
		{
			if (isSelectingAnObjectRightNow)
			{
				if (isSelectingMultipleObjects)
				{
					headerTitle.SetLocKey("selection.MultipleObjectsSelected");
				}
				else
				{
					headerTitle.SetLocKey(currentSelectedObj.objectFullNameWithID);
				}
			}
			else
			{
				headerTitle.SetLocKey("selection.NoObjectSelected");
			}
		}

		public void SetSelectedObjPanelAsNone()
		{
			isSelectingAnObjectRightNow = false;
			isSelectingMultipleObjects = true;

			ShowPanel(false, "selection.NoObjectSelected");
		}
		public void SetMultipleObjectsSelected()
		{
			isSelectingAnObjectRightNow = true;
			isSelectingMultipleObjects = true;

			ShowPanel(true, "selection.MultipleObjectsSelected");

			setActiveAtStartToggle.gameObject.SetActive(true);
			expandPanelButton.gameObject.SetActive(true);

			#region Set Active At Start Toggle
			// If this is null, that means the "Set Active At Start" in the current selected objects is different in at least one of them.
			// If it's true or false, then ALL of them are true or false.
			bool? setActiveStateInObjects = null;
			foreach (var obj in EditorController.Instance.currentSelectedObjects)
			{
				LE_Object comp = obj.GetComponent<LE_Object>();
				// Skip objects that can't be disabled at start.
				if (!comp.canBeDisabledAtStart) continue;

				if (setActiveStateInObjects == null)
				{
					setActiveStateInObjects = comp.setActiveAtStart;
					continue;
				}

				if (setActiveStateInObjects == comp.setActiveAtStart)
				{
					continue;
				}
				else
				{
					setActiveStateInObjects = null;
					break;
				}
			}

			if (setActiveStateInObjects != null)
			{
				setActiveAtStartToggle.Set((bool)setActiveStateInObjects);
				setActiveAtStartToggle.gameObject.GetChildAt("Background/Line").SetActive(false);
			}
			else
			{
				setActiveAtStartToggle.Set(false, false);
				setActiveAtStartToggle.gameObject.GetChildAt("Background/Line").SetActive(true);
			}
			#endregion

			globalObjAttributesToggle.gameObject.SetActive(false);
			globalObjAttributesToggle.SetToggleState(true, true);

			UpdateGlobalObjectAttributes(EditorController.Instance.currentSelectedObj.transform);
		}
		public void SetSelectedObject(LE_Object objComponent)
		{
			isSelectingAnObjectRightNow = true;
			isSelectingMultipleObjects = false;

			currentSelectedObj = objComponent;

			// The obj name is obviously NOT a valid loc key, but that doesn't matter, NGUI will just show it as is.
			ShowPanel(true, objComponent.objectFullNameWithID);
			expandPanelButton.gameObject.SetActive(true);

            bool specificAttributesFound = false;

            #region Select Right Attributes Panel
            attributesPanels.ToList().ForEach(x => x.Value.SetActive(false));

			specificAttributesFound = attributesPanels.TryGetValue(objComponent.objectType, out GameObject panel);
            if (specificAttributesFound)
            {
				panel.SetActive(true);
                UpdateObjectSpecificAttributes(objComponent, panel);
            }
            #endregion

            #region Setup Global Attributes Toggle
            globalObjAttributesToggle.gameObject.SetActive(specificAttributesFound);
            globalObjAttributesToggle.SetToggleState(!specificAttributesFound, true);
            #endregion

			UpdateGlobalObjectAttributes(objComponent.transform);

			#region Set At Start Toggle
			if (objComponent.canBeDisabledAtStart)
			{
				setActiveAtStartToggle.gameObject.SetActive(true);
				setActiveAtStartToggle.Set(objComponent.setActiveAtStart);
				setActiveAtStartToggle.gameObject.GetChildAt("Background/Line").SetActive(false);
			}
			else
			{
				setActiveAtStartToggle.gameObject.SetActive(false);
				objComponent.setActiveAtStart = true; // Just in case ;)
			}
			#endregion
		}

        public void ShowGlobalObjectAttributes(bool show)
        {
            objectSpecificPanelsParent.gameObject.SetActive(!show);
            globalObjectPanelsParent.gameObject.SetActive(show);
        }

        #region Global Attributes Logic
        enum GlobalFieldType { Position, Rotation, Scale }
		void OnGlobalAttributeFieldSelected(GlobalFieldType fieldType)
		{
			switch (fieldType)
			{
				case GlobalFieldType.Position:
					objPositionWhenSelectedField = EditorController.Instance.currentSelectedObj.transform.localPosition;
					break;

				case GlobalFieldType.Rotation:
					objRotationWhenSelectedField = EditorController.Instance.currentSelectedObj.transform.localRotation;
					break;

				case GlobalFieldType.Scale:
					objScaleWhenSelectedField = EditorController.Instance.currentSelectedObj.transform.localScale;
					break;
			}
		}
		void OnGlobalAttributeFieldDeselected(GlobalFieldType fieldType)
		{
			EditorController editor = EditorController.Instance;

			switch (fieldType)
			{
				case GlobalFieldType.Position:
					editor.RegisterLEAction(LEAction.LEActionType.MoveObject, editor.currentSelectedObj, editor.multipleObjectsSelected,
						objPositionWhenSelectedField, editor.currentSelectedObj.transform.localPosition, null, null);
					break;

				case GlobalFieldType.Rotation:
					editor.RegisterLEAction(LEAction.LEActionType.RotateObject, editor.currentSelectedObj, editor.multipleObjectsSelected, null, null,
						objRotationWhenSelectedField, editor.currentSelectedObj.transform.localRotation);
					break;

				case GlobalFieldType.Scale:
					editor.RegisterLEAction(LEAction.LEActionType.ScaleObject, editor.currentSelectedObj, editor.multipleObjectsSelected, null, null, null, null,
						objScaleWhenSelectedField, editor.currentSelectedObj.transform.localScale);
					break;
			}
		}

		public void SetSetActiveAtStart()
		{
			if (EditorController.Instance.multipleObjectsSelected)
			{
				setActiveAtStartToggle.gameObject.GetChildAt("Background/Line").SetActive(false);
				foreach (var obj in EditorController.Instance.currentSelectedObjects)
				{
					LE_Object comp = obj.GetComponent<LE_Object>();
					if (comp.canBeDisabledAtStart)
					{
						comp.setActiveAtStart = setActiveAtStartToggle.isChecked;
					}
				}
			}
			else
			{
				EditorController.Instance.currentSelectedObjComponent.setActiveAtStart = setActiveAtStartToggle.isChecked;
			}
			EditorController.Instance.levelHasBeenModified = true;
		}
		public void SetCollisionToggle()
		{
			if (!executeCollisionToggleActions) return;

			if (EditorController.Instance.multipleObjectsSelected)
			{
				collisionToggle.gameObject.GetChildAt("Background/Line").SetActive(false);
				foreach (var obj in EditorController.Instance.currentSelectedObjects)
				{
					LE_Object comp = obj.GetComponent<LE_Object>();
					comp.collision = collisionToggle.isChecked;
				}
			}
			else
			{
				EditorController.Instance.currentSelectedObjComponent.collision = collisionToggle.isChecked;
			}
			EditorController.Instance.levelHasBeenModified = true;
		}
        public void SetInvisibleMeshToggle()
        {
            if (!executeInvisibleMeshToggleActions) return;

            if (EditorController.Instance.multipleObjectsSelected)
            {
                invisibleMeshToggle.gameObject.GetChildAt("Background/Line").SetActive(false);
                foreach (var obj in EditorController.Instance.currentSelectedObjects)
                {
                    LE_Object comp = obj.GetComponent<LE_Object>();
                    comp.invisibleMesh = invisibleMeshToggle.isChecked;
                }
            }
            else
            {
                EditorController.Instance.currentSelectedObjComponent.invisibleMesh = invisibleMeshToggle.isChecked;
            }
            EditorController.Instance.levelHasBeenModified = true;
        }
        public void AddWaypointForObject()
		{
			if (!EditorController.Instance.multipleObjectsSelected)
			{
				var objComp = EditorController.Instance.currentSelectedObjComponent;
				objComp.GetComponent<WaypointSupport>().AddWaypoint();

				// If this is the first waypoint, set startMovingAtStart to true
				if (objComp.waypoints.Count == 1)
				{
					objComp.startMovingAtStart = true;
				}
			}
			else
			{
				List<GameObject> cachedSelectedObjects = new List<GameObject>(EditorController.Instance.currentSelectedObjects);
				EditorController.Instance.SetMultipleObjectsAsSelected(null);

				List<LE_Waypoint> createdWaypoints = new List<LE_Waypoint>();
				cachedSelectedObjects.ForEach(obj =>
				{
					var comp = obj.GetComponent<LE_Object>();
					var waypoint = comp.GetComponent<WaypointSupport>().AddWaypoint();
					createdWaypoints.Add(waypoint);

					// If this is the first waypoint, set startMovingAtStart to true
					if (comp.waypoints.Count == 1)
					{
						comp.startMovingAtStart = true;
					}
				});

				EditorController.Instance.SetMultipleObjectsAsSelected(createdWaypoints.Select(waypoint => waypoint.gameObject).ToList());
			}
		}
		public void SetStartMovingAtStart()
		{
			SetPropertyWithToggle(null, "StartMovingAtStart", startMovingAtStartToggle.isChecked);
		}

		public void UpdateGlobalObjectAttributes(Transform obj)
		{
			// UICustomInput already verifies if the user is typing on the field, if so, SetText does nothing, we don't need to worry about that.

			// Set Global Attributes...
			#region Position/Rotation/Scale Fields
			posXField.SetText(obj.position.x, 3, false); // Changed from 2 to 3
			posYField.SetText(obj.position.y, 3, false);
			posZField.SetText(obj.position.z, 3, false);

			rotXField.SetText(obj.localEulerAngles.x, 3, false);
			rotYField.SetText(obj.localEulerAngles.y, 3, false);
			rotZField.SetText(obj.localEulerAngles.z, 3, false);

			scaleXField.SetText(obj.localScale.x, 3, false);
			scaleYField.SetText(obj.localScale.y, 3, false);
			scaleZField.SetText(obj.localScale.z, 3, false);
			#endregion

			#region Collision Toggle
			if (EditorController.Instance.multipleObjectsSelected)
			{
				// If this is null, that means the "Collision" in the current selected objects is different in at least one of them.
				// If it's true or false, then ALL of them are true or false.
				bool? collisionStateInObjects = null;
				foreach (var @object in EditorController.Instance.currentSelectedObjects)
				{
					LE_Object comp = @object.GetComponent<LE_Object>();
					if (collisionStateInObjects == null)
					{
						collisionStateInObjects = comp.collision;
						continue;
					}

					if (collisionStateInObjects == comp.collision)
					{
						continue;
					}
					else
					{
						collisionStateInObjects = null;
						break;
					}
				}

				if (collisionStateInObjects != null)
				{
					collisionToggle.Set((bool)collisionStateInObjects);
					collisionToggle.gameObject.GetChildAt("Background/Line").SetActive(false);
				}
				else
				{
					executeCollisionToggleActions = false;
					collisionToggle.Set(false);
					executeCollisionToggleActions = true;
					collisionToggle.gameObject.GetChildAt("Background/Line").SetActive(true);
				}
			}
			else
			{
				collisionToggle.Set(obj.GetComponent<LE_Object>().collision);
				collisionToggle.gameObject.GetChildAt("Background/Line").SetActive(false);
			}
            #endregion

            // Add this section in UpdateGlobalObjectAttributes() after the Collision Toggle region (around line 910)
            #region Invisible Mesh Toggle
            if (EditorController.Instance.multipleObjectsSelected)
            {
                // If this is null, that means the "InvisibleMesh" in the current selected objects is different in at least one of them.
                // If it's true or false, then ALL of them are true or false.
                bool? invisibleMeshStateInObjects = null;
                foreach (var @object in EditorController.Instance.currentSelectedObjects)
                {
                    LE_Object comp = @object.GetComponent<LE_Object>();

                    if (invisibleMeshStateInObjects == null)
                    {
                        invisibleMeshStateInObjects = comp.invisibleMesh; // Direct field access
                        continue;
                    }

                    if (invisibleMeshStateInObjects == comp.invisibleMesh)
                    {
                        continue;
                    }
                    else
                    {
                        invisibleMeshStateInObjects = null;
                        break;
                    }
                }

                if (invisibleMeshStateInObjects != null)
                {
                    invisibleMeshToggle.Set((bool)invisibleMeshStateInObjects);
                    invisibleMeshToggle.gameObject.GetChildAt("Background/Line").SetActive(false);
                }
                else
                {
                    executeInvisibleMeshToggleActions = false;
                    invisibleMeshToggle.Set(false);
                    executeInvisibleMeshToggleActions = true;
                    invisibleMeshToggle.gameObject.GetChildAt("Background/Line").SetActive(true);
                }
            }
            else
            {
                invisibleMeshToggle.Set(obj.GetComponent<LE_Object>().invisibleMesh); // Direct field access
                invisibleMeshToggle.gameObject.GetChildAt("Background/Line").SetActive(false);
            }
            #endregion

            #region Add Waypoint Button
            if (EditorController.Instance.multipleObjectsSelected)
			{
				// Only enable the button when ALL of the selected objects allow waypoints.
				addWaypointButton.gameObject.SetActive(EditorController.Instance.currentSelectedObjects.All(x => x.GetComponent<LE_Object>().canHaveWaypoints));
			}
			else
			{
				addWaypointButton.gameObject.SetActive(EditorController.Instance.currentSelectedObjComponent.canHaveWaypoints);
			}
			#endregion

			#region Start Moving At Start Toggle
			if (!EditorController.Instance.multipleObjectsSelected && EditorController.Instance.currentSelectedObjComponent.waypoints.Count > 0)
			{
				startMovingAtStartToggle.transform.parent.gameObject.SetActive(true);
				startMovingAtStartToggle.Set(EditorController.Instance.currentSelectedObjComponent.startMovingAtStart);
			}
			else
			{
				startMovingAtStartToggle.transform.parent.gameObject.SetActive(false);
			}
			#endregion

			#region Moving Speed Field
			if (!EditorController.Instance.multipleObjectsSelected && EditorController.Instance.currentSelectedObjComponent.waypoints.Count > 0)
			{
				movingSpeedField.transform.parent.gameObject.SetActive(true);
				movingSpeedField.SetText(EditorController.Instance.currentSelectedObjComponent.movingSpeed);
			}
			else
			{
				movingSpeedField.transform.parent.gameObject.SetActive(false);
			}
			#endregion

			#region Start Delay Field
			if (!EditorController.Instance.multipleObjectsSelected && EditorController.Instance.currentSelectedObjComponent.waypoints.Count > 0)
			{
				startDelayField.transform.parent.gameObject.SetActive(true);
				startDelayField.SetText(EditorController.Instance.currentSelectedObjComponent.startDelay);
			}
			else
			{
				startDelayField.transform.parent.gameObject.SetActive(false);
			}
			#endregion

			#region Wait Time Field
			if (!EditorController.Instance.multipleObjectsSelected && EditorController.Instance.currentSelectedObjComponent.waypoints.Count > 0)
			{
				waitTimeField.transform.parent.gameObject.SetActive(true);
				waitTimeField.SetText(EditorController.Instance.currentSelectedObjComponent.waitTime);
			}
			else
			{
				waitTimeField.transform.parent.gameObject.SetActive(false);
			}
			#endregion

			#region Waypoint Mode Button
			if (!EditorController.Instance.multipleObjectsSelected && EditorController.Instance.currentSelectedObjComponent.waypoints.Count > 0)
			{
				waypointModeButton.transform.parent.gameObject.SetActive(true);
				waypointModeButton.SetOption((int)EditorController.Instance.currentSelectedObjComponent.waypointMode);
			}
			else
			{
				waypointModeButton.transform.parent.gameObject.SetActive(false);
			}
			#endregion
		}
        #endregion

        #region Object Specific Attributes Logic
        void UpdateObjectSpecificAttributes(LE_Object objComp, GameObject panelInUI)
        {
            // OFFICIALLY, THIS IS THE ULTIMATE MOST BETTER AUTOMATED PROPERTY UPDATER OF THE WORLD!
            foreach (var attribute in panelInUI.GetChilds())
            {
                string attributeName = attribute.name; // Assuming the name of the childs in the UI is the same as the REAL attribute name.
                if (objComp.TryGetProperty(attributeName, out object value))
                {
                    if (attribute.ExistsChild("Field"))
                    {
                        switch (value)
                        {
                            case int intValue:
                                value = value + ""; // Convert to string directly, no ToString() shit needed here.
                                break;
                            case float floatValue:
                                value = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
                                break;
                            case Color colorValue:
                                value = Utils.ColorToHex(colorValue);
                                break;

                            case string:
                                // With string there's no problem, but put this so it's not catched by "default:".
                                break;

                            default:
                                Logger.Error($"Tried to update \"{attributeName}\" with value of type \"{value.GetType().Name}\" in an INPUT FIELD?");
                                continue;
                        }

                        attribute.GetChild("Field").GetComponent<UIInput>().text = (string)value;
                    }
                    else if (attribute.ExistsChild("Toggle"))
                    {
                        // Values for toggles can ONLY be bools, nothing else LOL.
                        if (value is not bool)
                        {
                            Logger.Error($"Tried to update \"{attributeName}\" with value of type \"{value.GetType().Name}\" in a TOGGLE?");
                            continue;
                        }

                        attribute.GetChild("Toggle").GetComponent<UIToggle>().Set((bool)value);
                    }
                    else if (attribute.ExistsChild("ButtonMultiple"))
                    {
                        // Values for multiple option buttons can be, int or maybe an enum
                        if (value is not int && value is not Enum)
                        {
                            Logger.Error($"Tried to update \"{attributeName}\" with value of type \"{value.GetType().Name}\" in a BUTTON MULTIPLE?");
                            continue;
                        }

                        attribute.GetChild("ButtonMultiple").GetComponent<UISmallButtonMultiple>().SetOption((int)value);
                    }
                }
            }

            UpdateOptionalPropertiesVisibility(objComp.objectType);
        }

        void UpdateOptionalPropertiesVisibility(LE_Object.ObjectType? type)
        {
            foreach (var prop in optionalProps.Where(p => p.Key.type == type))
            {
                var value = prop.Value;

                bool setActive = false;

                if (value.requiredPropName == "waypoints")
                {
                    setActive = currentSelectedObj.GetProperty<List<WaypointData>>(value.requiredPropName).Count > 0;
                }
                else
                {
                    setActive = Equals(currentSelectedObj.GetProperty(value.requiredPropName), value.requiredPropValue);
                }
                attributesPanels[type].GetChild(prop.Key.propName).SetActive(setActive);
            }
        }
        #endregion


        void SetVector3PropertyWithInput(string propertyName, GameObject attributeParent)
		{
			var objComponent = EditorController.Instance.currentSelectedObjComponent;
			if (objComponent == null) return;

			// Find the three input fields within the attribute's parent object
			var xField = attributeParent.transform.Find("XField").GetComponent<UICustomInputField>();
			var yField = attributeParent.transform.Find("YField").GetComponent<UICustomInputField>();
			var zField = attributeParent.transform.Find("ZField").GetComponent<UICustomInputField>();

			if (xField == null || yField == null || zField == null)
			{
				UnityEngine.Debug.LogError("Could not find all three Vector3 input fields.");
				return;
			}

			// Parse the current values from all three UI fields
			float.TryParse(xField.input.text, out float xVal);
			float.TryParse(yField.input.text, out float yVal);
			float.TryParse(zField.input.text, out float zVal);

			// Create the new Vector3 from the UI state
			Vector3 newVector = new Vector3(xVal, yVal, zVal);

			// Set the property using the editor's reliable SetProperty method
			objComponent.SetProperty(propertyName, newVector);
		}
		public void SetPropertyWithInput(string propertyName, UICustomInputField inputField, bool isGlobalProp = false)
		{
			// Even if the input only accepts numbers and decimals, check if it CAN be converted to float anyways, what if the text is just a "-"!?
			if ((propertyName.Contains("Position") || propertyName.Contains("Rotation") || propertyName.Contains("Scale")) &&
				Utils.TryParseFloat(inputField.GetText(), out float floatValue))
			{
				switch (propertyName)
				{
					case "XPosition":
						EditorController.Instance.currentSelectedObj.transform.SetXPosition(floatValue);
						break;
					case "YPosition":
						EditorController.Instance.currentSelectedObj.transform.SetYPosition(floatValue);
						break;
					case "ZPosition":
						EditorController.Instance.currentSelectedObj.transform.SetZPosition(floatValue);
						break;

					case "XRotation":
						EditorController.Instance.currentSelectedObj.transform.SetXRotation(floatValue);
						break;
					case "YRotation":
						EditorController.Instance.currentSelectedObj.transform.SetYRotation(floatValue);
						break;
					case "ZRotation":
						EditorController.Instance.currentSelectedObj.transform.SetZRotation(floatValue);
						break;

					case "XScale":
						EditorController.Instance.currentSelectedObj.transform.SetXScale(floatValue);
						EditorController.Instance.ApplyGizmosArrowsScale();
						break;
					case "YScale":
						EditorController.Instance.currentSelectedObj.transform.SetYScale(floatValue);
						EditorController.Instance.ApplyGizmosArrowsScale();
						break;
					case "ZScale":
						EditorController.Instance.currentSelectedObj.transform.SetZScale(floatValue);
						EditorController.Instance.ApplyGizmosArrowsScale();
						break;
				}

				return;
			}
			if (propertyName == "Keycode")
			{
				string text = inputField.GetText();
				// Accept only if it's 4 digits (0-9)
				if (text.Length == 4 && text.All(char.IsDigit))
				{
					if (EditorController.Instance.currentSelectedObjComponent.SetProperty(propertyName, text))
					{
						EditorController.Instance.levelHasBeenModified = true;
						inputField.Set(true);
					}
					else
					{
						inputField.Set(false);
					}
				}
				else
				{
					inputField.Set(false); // Mark field as invalid
				}
				return;
			}
			if (propertyName == "Intensity" && Utils.TryParseFloat(inputField.GetText(), out float intensityValue))
			{
				if (EditorController.Instance.currentSelectedObjComponent.SetProperty(propertyName, intensityValue))
				{
					EditorController.Instance.levelHasBeenModified = true;
					inputField.Set(true);
				}
				else
				{
					inputField.Set(false);
				}
				return;
			}

			bool setPropResult = isGlobalProp ? EditorController.Instance.currentSelectedObjComponent.SetPropertyBase(propertyName, inputField.GetText())
				: EditorController.Instance.currentSelectedObjComponent.SetProperty(propertyName, inputField.GetText());
            if (setPropResult)
			{
				EditorController.Instance.levelHasBeenModified = true;
				inputField.Set(true);
			}
			else
			{
				inputField.Set(false);
			}
		}
		public void SetPropertyWithToggle(LE_Object.ObjectType? type, string propertyName, bool newValue)
		{
			switch (propertyName)
			{
				case "TravelBack":
					SetSawTravelBackORLoop(newValue, false);
					break;
				case "Loop":
					SetSawTravelBackORLoop(false, newValue);
					break;
			}

			if (EditorController.Instance.currentSelectedObjComponent.SetProperty(propertyName, newValue))
			{
				EditorController.Instance.levelHasBeenModified = true;
			}

			UpdateOptionalPropertiesVisibility(type);
        }
		public void SetPropertyWithButtonMultiple(string propertyName, UISmallButtonMultiple button)
		{
			if (EditorController.Instance.currentSelectedObjComponent.SetProperty(propertyName, button.currentOption))
			{
				EditorController.Instance.levelHasBeenModified = true;
			}
		}
		public void TriggerAction(string actionName)
		{
			if (EditorController.Instance.currentSelectedObjComponent.TriggerAction(actionName))
			{
				EditorController.Instance.levelHasBeenModified = true;
			}
		}

		// Extra functions for specific things for specific attributes for specific objects LOL.
		void SetSawTravelBackORLoop(bool travelBack, bool loop)
		{
			// This is to always enable one or the other, but NEVER both of the toggles, only one or the other.
			// To avoid bugs, only change the values when at least one of the bools is true.

			var travelBackToggle = attributesPanels[LE_Object.ObjectType.SAW].GetChildAt("TravelBack/Toggle").GetComponent<UIToggle>();
			var loopToggle = attributesPanels[LE_Object.ObjectType.SAW].GetChildAt("Loop/Toggle").GetComponent<UIToggle>();

			if (travelBack && !loop)
			{
				travelBackToggle.Set(true);
				if (loopToggle.isChecked) loopToggle.Set(false);

				EditorController.Instance.currentSelectedObjComponent.SetProperty("TravelBack", true);
				EditorController.Instance.currentSelectedObjComponent.SetProperty("Loop", false);
			}
			if (!travelBack && loop)
			{
				if (travelBackToggle.isChecked) travelBackToggle.Set(false);
				loopToggle.Set(true);

				EditorController.Instance.currentSelectedObjComponent.SetProperty("TravelBack", false);
				EditorController.Instance.currentSelectedObjComponent.SetProperty("Loop", true);
			}
		}
	}
}