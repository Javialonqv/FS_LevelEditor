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
using static Il2CppSystem.Linq.Expressions.Interpreter.CastInstruction.CastInstructionNoT;
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
		UIVector3Fields posFields;
		UIVector3Fields rotFields;
		UIVector3Fields scaleFields;
		UITogglePatcher collisionToggle;
        UITogglePatcher invisibleMeshToggle;
        UIButtonPatcher addWaypointButton;
		UITogglePatcher startMovingAtStartToggle;
		UICustomInputField movingSpeedField;
		UICustomInputField startDelayField;
		UICustomInputField waitTimeField;
		UISmallButtonMultiple waypointModeButton;
		UIButtonPatcher addToGroupButton;
		UIButtonPatcher removeFromGroupButton;
		// ------------------------------
		bool showingPanel = false;
		bool panelIsExpanded = false;
		string currentHeaderLocKey = "";
		// ------------------------------
		Transform objectSpecificPanelsParent;
		Dictionary<LE_Object.ObjectType?, GameObject> attributesPanels = new Dictionary<LE_Object.ObjectType?, GameObject>();
		Transform whereToCreateObjAttributesParent;
		LE_Object.ObjectType currentlyCreatingPropsUIFor;

        #region Rules/Patterns For Object Specific Props Creation
        static readonly Dictionary<(LE_Object.ObjectType objType, string propName), string> objectPropsTooltips = new Dictionary<(LE_Object.ObjectType objType, string propName), string>
		{
			{ (LE_Object.ObjectType.SAW, "TravelBack"), "TravelBackTooltip" },
			{ (LE_Object.ObjectType.SAW, "Loop"),		"LoopTooltip" },
            { (LE_Object.ObjectType.DEATH_TRIGGER_WAYPOINT, "RotatePlayer"), "BROKEN AS HELL!!" },
            { (LE_Object.ObjectType.TRIGGER, "ExecIfInside"), "ExecIfInsideTooltip" },
            { (LE_Object.ObjectType.TRIGGER, "ExecIfDespawned"), "ExecIfDespawnedTooltip" },
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
			{ "BLUE", Color.blue },
			{ "ORANGE", new Color(1f, 0.67f, 0.1f) },
			{ "YELLOW", new Color(0.8f, 0.8f, 0f) },
			{ "WHITE", new Color(0.94f, 0.95f, 0.96f) },
			{ "MAGENTA", new Color(1f, 0f, 1f) },
		};
		static readonly string[] bannedPropertiesFromUI = new string[]
		{
			"AutoFontSize",
			"FontSize",
			"MinFontSize",
			"MaxFontSize",
			"TextAlign",
			"Text",

			"upgrades"
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

			{ (LE_Object.ObjectType.DEATH_TRIGGER, "AddWaypoint"), ("not_waypoints", true) }, // Yes, I just added the AND (||) operator just for this one.
			{ (LE_Object.ObjectType.DEATH_TRIGGER_WAYPOINT, "AddWaypoint"), ("", null) }, // If requiredPropName is null, it'll be disabled :)

			{ (LE_Object.ObjectType.SWITCH, "OnlyByTaser"), ("CanUseTaser", true) },

			{ (LE_Object.ObjectType.SAW, "WaitTime"), ("waypoints", null) }, // If it's checking for waypoints, the code already checks if the list count is greater than 0.

#if EXP_ONLY
			{ (LE_Object.ObjectType.KEYPAD, "AlternativeComb"), ("Alternative", true) }
#endif
        };
		static readonly Dictionary<LE_Object.ObjectType, string> addWaypointBtnLocKeys = new Dictionary<LE_Object.ObjectType, string>()
		{
			{ LE_Object.ObjectType.SAW, "AddSawWaypoint" },
			{ LE_Object.ObjectType.SAW_WAYPOINT, "AddSawWaypoint" },

			{ LE_Object.ObjectType.MOVING_PLATFORM, "AddMovingPlatformWaypoint" },
			{ LE_Object.ObjectType.MOVING_PLATFORM_WAYPOINT, "AddMovingPlatformWaypoint" },

			{ LE_Object.ObjectType.DEATH_TRIGGER, "AddDeathTriggerWaypoint" },

			{ LE_Object.ObjectType.SEQUENCE, "AddSequencerWaypoint" },
			{ LE_Object.ObjectType.SEQUENCE_WAYPOINT, "AddSequencerWaypoint" }
		};
#endregion

        bool isSelectingAnObjectRightNow = false;
		bool isSelectingMultipleObjects = false;
		bool isSelectingMultipleObjectsOfTheSameType = false;
		LE_Object currentSelectedObj => EditorController.Instance.currentSelectedObjComponent;
		List<LE_Object> currentSelectedObjects => EditorController.Instance.currentSelectedObjsComponents;

        Vector3 objPositionWhenSelectedField;
		Quaternion objRotationWhenSelectedField;
		Vector3 objScaleWhenSelectedField;
			
		public SelectedObjPanel(IntPtr ptr) : base (ptr) { }

		public static void Create(Transform editorUIParent)
		{
			GameObject root = new GameObject("CurrentSelectedObjPanel");
			root.transform.parent = editorUIParent;
			root.transform.localPosition = new Vector3(-690f, -120f, 0f); // Changed from -700f to -690f
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
			sprite.height = 400;
			sprite.pivot = UIWidget.Pivot.Top;

			BoxCollider collider = body.AddComponent<BoxCollider>();
			collider.size = new Vector3(500f, 400f, 1f);
			collider.center = new Vector3(0f, -150f);

			// Add a UIPanel just to hide the objects outside of the panel.
			UIPanel panel = body.AddComponent<UIPanel>();
			panel.clipRange = new Vector4(0f, -200f, 500f, 360f);
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
			CreateAddToGroupButton();
			CreateRemoveFromGroupButton();
            CreateAddWaypointButton();
			CreateStartMovingAtStartToggle();
			CreateMovingSpeedField();
			CreateStartDelayField();
			CreateWaitTimeField();
			CreateWaypointModeButton();
        }
		void CreateObjectPositionUIElements()
		{
			SetCurrentParentToCreateAttributes(globalObjectPanelsParent.gameObject);

			posFields = (UIVector3Fields)CreateObjectAttribute("Position", AttributeType.VECTOR, null, UICustomInputField.UIInputType.FLOAT, null);

			posFields.onSelected += (axis) => OnGlobalAttributeFieldSelected(GlobalFieldType.Position);
			posFields.onChange += (axis) => SetVector3PropertyWithInput("Position", posFields, true);
			posFields.onDeselected += (axis) => OnGlobalAttributeFieldDeselected(GlobalFieldType.Position);

            yPosForGlobalProps -= 50;
		}
		void CreateObjectRotationUIElements()
		{
			SetCurrentParentToCreateAttributes(globalObjectPanelsParent.gameObject);

			rotFields = (UIVector3Fields)CreateObjectAttribute("Rotation", AttributeType.VECTOR, null, UICustomInputField.UIInputType.FLOAT, null);

			rotFields.onSelected += (axis) => OnGlobalAttributeFieldSelected(GlobalFieldType.Rotation);
			rotFields.onChange += (axis) => SetVector3PropertyWithInput("Rotation", rotFields, true);
			rotFields.onDeselected += (axis) => OnGlobalAttributeFieldDeselected(GlobalFieldType.Rotation);

			yPosForGlobalProps -= 50;
		}
		void CreateObjectScaleUIElements()
		{
			SetCurrentParentToCreateAttributes(globalObjectPanelsParent.gameObject);

            scaleFields = (UIVector3Fields)CreateObjectAttribute("Scale", AttributeType.VECTOR, null, UICustomInputField.UIInputType.FLOAT, null);

            scaleFields.onSelected += (axis) => OnGlobalAttributeFieldSelected(GlobalFieldType.Scale);
            scaleFields.onChange += (axis) => SetVector3PropertyWithInput("Scale", scaleFields, true);
            scaleFields.onDeselected += (axis) => OnGlobalAttributeFieldDeselected(GlobalFieldType.Scale);

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

            yPosForGlobalProps -= 55;
        }
        void CreateAddToGroupButton()
        {
            addToGroupButton = NGUI_Utils.CreateButton(globalObjectPanelsParent, new Vector3(0, yPosForGlobalProps), new Vector3Int(480, 50, 0), "AddToGroupButton");
            addToGroupButton.name = "AddToGroupButton";
            addToGroupButton.onClick += AddToGroupPressed;
            addToGroupButton.GetComponent<UIButtonScale>().hover = Vector3.one * 1.05f;
            addToGroupButton.GetComponent<UIButtonScale>().pressed = Vector3.one * 1.02f;

            // Do not substract yPos, add and remove buttons need to be on the same pos.
        }
        void CreateRemoveFromGroupButton()
        {
            // Leave the default text blank, so no UILocalize is created.
            removeFromGroupButton = NGUI_Utils.CreateButton(globalObjectPanelsParent, new Vector3(0, yPosForGlobalProps), new Vector3Int(480, 50, 0), "");
            removeFromGroupButton.name = "RemoveFromGroupButton";
            removeFromGroupButton.onClick += RemoveFromGroupPressed;
            removeFromGroupButton.GetComponent<UIButtonScale>().hover = Vector3.one * 1.05f;
            removeFromGroupButton.GetComponent<UIButtonScale>().pressed = Vector3.one * 1.02f;

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

			if (ShouldHaveManagedUpgradesButton(defaultProps))
			{
				CreateObjectAttribute("ManageUpgrades", AttributeType.BUTTON, null, null, "ManageUpgrades");
			}

            // Add "Add Waypoint" button if it has local waypoints.
            if (LE_Object.customWaypointSupports.ContainsKey(type) || LE_Object.IsWaypoint(type))
			{
				string addWaypointBtnLocKey = null;

				if (addWaypointBtnLocKeys.ContainsKey(type))
				{
					addWaypointBtnLocKey = addWaypointBtnLocKeys[type];
				}
				else
				{
					addWaypointBtnLocKey = "AddGlobalWaypoint";
				}

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
		bool ShouldHaveManagedUpgradesButton(Dictionary<string, object> props)
		{
			return props.ContainsKey("upgrades");
		}

		enum AttributeType { TOGGLE, INPUT_FIELD, BUTTON, BUTTON_MULTIPLE, VECTOR }
		void SetCurrentParentToCreateAttributes(GameObject newParent)
		{
			whereToCreateObjAttributesParent = newParent.transform;
		}

        /// <summary>
        /// Creates an <b>attribute</b> as a child of the object previously specified. Specify it with <i>SetCurrentParentToCreateAttributes()</i> method.
        /// </summary>
		/// 
        /// <param name="text">The title of the attribute to create. (Or the text that will be in case it's a button).</param>
		/// 
        /// <param name="attrType">The kind of attribute it's about to create.</param>
		/// 
        /// <param name="defaultValue">
		/// The default value for the attribute, the type depends of the attribute type to create:
		/// <para/>
		/// <see cref="AttributeType.TOGGLE"/>: <see langword="bool"/>
		/// <br/> <see cref="AttributeType.INPUT_FIELD"/>: <see langword="string"/>
		/// <br/> <see cref="AttributeType.BUTTON"/>: <see langword="null"/>
		/// <br/> <see cref="AttributeType.BUTTON_MULTIPLE"/>: <c>NOT SUPPORTED</c>
		/// <br/> <see cref="AttributeType.VECTOR"/>: <c>NOT SUPPORTED</c>
		/// </param>
		/// 
        /// <param name="fieldType">The type of field to create.
		/// <para/>
		/// Valid for <see cref="AttributeType.INPUT_FIELD"/> and <see cref="AttributeType.VECTOR"/> <b>only.</b>
		/// <br><see langword="null"/> otherwise.</br>
		/// </param>
		/// 
        /// <param name="targetPropName">The name of the <b>target property</b> inside of the <see cref="LE_Object"/>.</param>
		/// 
        /// <param name="createHastag">Defines if it should create a hashtag on the left of the field (for hex color inputs).
		/// <para/>
		/// Valid for <see cref="AttributeType.INPUT_FIELD"/> <b>only.</b>
		/// </param>
		/// 
        /// <param name="tooltip">Tooltip for the attribute. Leave <see langword="null"/> if you don't want any.</param>
		/// 
        /// <param name="dontChangeYPos">Defines if the attribute should be created in the <b>same position</b> as the previously created one, and not under it.</param>
		/// 
        /// <param name="maxLength">Character limit for the field.
		/// <para/>
		/// Valid for <see cref="AttributeType.INPUT_FIELD"/> <b>only.</b>
		/// </param>
		/// 
        /// <returns>The script instance for the created attribute, the script type depends of the attribute type:
		/// <para/>
		/// <see cref="UITogglePatcher"/> for <see cref="AttributeType.TOGGLE"/>.
		/// <br/> <see cref="UICustomInputField"/> for <see cref="AttributeType.INPUT_FIELD"/>.
		/// <br/> <see cref="UIButtonPatcher"/> for <see cref="AttributeType.BUTTON"/>.
		/// <br/> <see cref="UISmallButtonMultiple"/> for <see cref="AttributeType.BUTTON_MULTIPLE"/>.
		/// <br/> <see cref="UIVector3Fields"/> for <see cref="AttributeType.VECTOR"/>.
		/// </returns>
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

			#region Create Title Label
            if (attrType != AttributeType.BUTTON)
			{
				int titleWidth = 0;
				switch (attrType)
				{
					case AttributeType.INPUT_FIELD:
					case AttributeType.BUTTON_MULTIPLE:
						titleWidth = 260;
                        if (createHastag) titleWidth = 235;
                        break;

					case AttributeType.TOGGLE:
						titleWidth = 395;
						break;

					case AttributeType.VECTOR:
						titleWidth = 150;
						break;
				}

				UILabel title = NGUI_Utils.CreateLabel(attributeParent.transform, new Vector3(-230, yPos), new Vector3Int(titleWidth, NGUI_Utils.defaultLabelSize.y, 0),
					text);
				title.name = "Title";
			}
			#endregion

			#region Create Hastag If It's An Input Field
            if (createHastag && attrType == AttributeType.INPUT_FIELD)
			{
				UILabel hashtagLOL = NGUI_Utils.CreateLabel(attributeParent.transform, new Vector3(15, yPos), new Vector3Int(20, NGUI_Utils.defaultLabelSize.y, 0), "#",
					NGUIText.Alignment.Center, UIWidget.Pivot.Left);
				hashtagLOL.name = "HashtagLOL";
				hashtagLOL.color = Color.white;
			}
			#endregion

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
				if ((bool)defaultValue) toggle.Set(true, false);
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
				#region Parse Default Values
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
				#endregion

                var inputTypeForVector = fieldType ?? UICustomInputField.UIInputType.FLOAT;

				string[] axises = { "X", "Y", "Z" };
				float[] fieldsTitlesXPositions = { -40f, 60f, 160f };
				float[] fieldsXPositions = { 10f, 110f, 210f };
				int fieldsTitlesWidth = 28;
				int fieldsWidth = 65;

				UICustomInputField xField = null;
				UICustomInputField yField = null;
                UICustomInputField zField = null;

				UIVector3Fields fields = new GameObject("Fields").AddComponent<UIVector3Fields>();
				fields.transform.parent = attributeParent.transform;
				fields.transform.localPosition = Vector3.zero;
				fields.transform.localScale = Vector3.one;

				for (int i = 0; i < 3; i++)
				{
					string axis = axises[i];
					float titleXPos = fieldsTitlesXPositions[i];
					float fieldXPos = fieldsXPositions[i];

					UILabel title = NGUI_Utils.CreateLabel(fields.transform, new Vector3(titleXPos, yPos),
						new Vector3Int(fieldsTitlesWidth, 38, 0), axis, NGUIText.Alignment.Center, UIWidget.Pivot.Center);
					title.name = $"{axis}Title";

					UICustomInputField field = NGUI_Utils.CreateInputField(fields.transform, new Vector3(fieldXPos, yPos), new Vector3Int(fieldsWidth, 38, 0), 27, defaultValues[0], inputType: inputTypeForVector,
						maxDecimals: 3);
					field.name = $"{axis}Field";

					if (i == 0) xField = field;
					else if (i == 1) yField = field;
					else if (i == 2) zField = field;
				}

				fields.Assign(xField, yField, zField);

				toReturn = fields;

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
					gameObject.transform.localPosition = new Vector3(-690f, -120, 0f); // Changed from -700f to -690f
					headerTitle.width = 300;
					body.SetActive(true);
					body.GetComponent<UISprite>().height = 400;
					body.GetComponent<BoxCollider>().center = new Vector3(0, -200f);
					body.GetComponent<BoxCollider>().size = new Vector3(500, 400);
					body.GetComponent<UIPanel>().clipRange = new Vector4(0f, -200f, 500, 360);
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
				//panelIsExpanded = false;
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
            isSelectingMultipleObjectsOfTheSameType = EditorController.Instance.multipleObjectsOfTheSameTypeSelected;

            if (EditorController.Instance.currentSelectedGroup.HasValue)
                ShowPanel(true, $"{Loc.Get("Group")} {EditorController.Instance.currentSelectedGroup.Value}");
			else
                ShowPanel(true, "selection.MultipleObjectsSelected");

            setActiveAtStartToggle.gameObject.SetActive(true);
			expandPanelButton.gameObject.SetActive(true);

			SetPropInToggleDependingOfPropInObjects(setActiveAtStartToggle, (obj) => obj.setActiveAtStart, (obj) => obj.canBeDisabledAtStart);

            UpdateGlobalObjectAttributes(EditorController.Instance.currentSelectedObj.transform);

			if (isSelectingMultipleObjectsOfTheSameType)
			{
				#region Select Right Attributes Panel
                bool specificAttributesFound = false;

                attributesPanels.ToList().ForEach(x => x.Value.SetActive(false));

				// We know that all of the objects are of the same type, so doesn't matter which one we use, whatever!
                specificAttributesFound = attributesPanels.TryGetValue(currentSelectedObjects[0].objectType, out GameObject panel);
                if (specificAttributesFound)
                {
                    panel.SetActive(true);
					UpdateObjectSpecificAttributes(panel, currentSelectedObjects);
                }
				else
				{
					// Doesn't matter if they're of the same tiye, make them behave like they're not, so it only displays global props.
					isSelectingMultipleObjectsOfTheSameType = false;
				}
				#endregion
            }

            if (!isSelectingMultipleObjectsOfTheSameType)
            {
                globalObjAttributesToggle.gameObject.SetActive(false);
                globalObjAttributesToggle.SetToggleState(true, true);
            }
            else
            {
                globalObjAttributesToggle.gameObject.SetActive(true);
                globalObjAttributesToggle.SetToggleState(false, true);
            }
        }
		public void SetSelectedObject(LE_Object objComponent)
		{
			isSelectingAnObjectRightNow = true;
			isSelectingMultipleObjects = false;

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
                UpdateObjectSpecificAttributes(panel, objComponent);
            }
			#endregion

			#region Setup Global Attributes Toggle
            globalObjAttributesToggle.gameObject.SetActive(specificAttributesFound);
            globalObjAttributesToggle.SetToggleState(!specificAttributesFound, true);
			#endregion

			UpdateGlobalObjectAttributes(objComponent.transform);

			#region Set Active At Start Toggle
			if (objComponent.canBeDisabledAtStart)
			{
				setActiveAtStartToggle.gameObject.SetActive(true);
				setActiveAtStartToggle.Set(objComponent.setActiveAtStart, instant: true);
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
				foreach (var obj in EditorController.Instance.currentSelectedObjsComponents)
				{
					if (obj.canBeDisabledAtStart)
					{
                        obj.setActiveAtStart = setActiveAtStartToggle.isChecked;
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
			if (EditorController.Instance.multipleObjectsSelected)
			{
				foreach (var obj in EditorController.Instance.currentSelectedObjsComponents)
				{
                    obj.collision = collisionToggle.isChecked;
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
            if (EditorController.Instance.multipleObjectsSelected)
            {
                foreach (var obj in EditorController.Instance.currentSelectedObjsComponents)
                {
                    obj.invisibleMesh = invisibleMeshToggle.isChecked;
					if (obj.disableMeshInEditorIfIMEnabled)
						obj.SetMeshRenderersState(!invisibleMeshToggle.isChecked);
                }
            }
            else
            {
                EditorController.Instance.currentSelectedObjComponent.invisibleMesh = invisibleMeshToggle.isChecked;
				if (EditorController.Instance.currentSelectedObjComponent.disableMeshInEditorIfIMEnabled)
					EditorController.Instance.currentSelectedObjComponent.SetMeshRenderersState(!invisibleMeshToggle.isChecked);
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
		public void AddToGroupPressed()
		{
            if (EditorController.Instance.multipleObjectsSelected)
            {
				AddToGroupUI.Instance.Show(EditorController.Instance.currentSelectedObjsComponents.ToArray());
            }
            else
            {
				AddToGroupUI.Instance.Show(EditorController.Instance.currentSelectedObjComponent);
            }

            EditorController.Instance.levelHasBeenModified = true;
        }
        public void RemoveFromGroupPressed()
        {
			HashSet<int> modifiedGroups = new();
            if (EditorController.Instance.multipleObjectsSelected)
            {
				foreach (var obj in EditorController.Instance.currentSelectedObjsComponents)
				{
					if (obj.groupID.HasValue) modifiedGroups.Add(obj.groupID.Value);
                    obj.SetGroup(null);
                }
            }
            else
            {
                if (EditorController.Instance.currentSelectedObjComponent.groupID.HasValue) modifiedGroups.Add(EditorController.Instance.currentSelectedObjComponent.groupID.Value);
                EditorController.Instance.currentSelectedObjComponent.SetGroup(null);
            }

			// Remove groups with 0 objects.
			foreach (var groupID in modifiedGroups)
			{
				if (LE_Object.objectsPerGroup[groupID].Count == 0)
					LE_Object.objectsPerGroup.Remove(groupID);
			}

            addToGroupButton.gameObject.SetActive(true);
            removeFromGroupButton.gameObject.SetActive(false);

            EditorController.Instance.levelHasBeenModified = true;
        }

        public void UpdateGlobalObjectAttributes(Transform obj)
		{
			// UICustomInput already verifies if the user is typing on the field, if so, SetText does nothing, we don't need to worry about that.

			// Set Global Attributes...
			#region Position/Rotation/Scale Fields
			posFields.SetVector(obj.position, 3, false);

			rotFields.SetVector(obj.localEulerAngles, 3, false);

			scaleFields.SetVector(obj.localScale, 3, false);
			#endregion

			SetPropInToggleDependingOfPropInObjects(collisionToggle, (obj) => obj.collision);
			SetPropInToggleDependingOfPropInObjects(invisibleMeshToggle, (obj) => obj.invisibleMesh);

            #region Add To Group / Remove From Group Buttons
            if (EditorController.Instance.multipleObjectsSelected)
            {
                // Only enable the button when NONE of the selected objects have a group.
				if (EditorController.Instance.currentSelectedObjsComponents.All(x => x.groupID == null))
				{
                    addToGroupButton.gameObject.SetActive(true);
					removeFromGroupButton.gameObject.SetActive(false);
                }
				else
				{
                    addToGroupButton.gameObject.SetActive(false);
					removeFromGroupButton.gameObject.SetActive(true);
					if (LE_Object.ObjectsHaveTheSameGroupID(out int? groupID, EditorController.Instance.currentSelectedObjsComponents))
						removeFromGroupButton.buttonLabel.text = Loc.Get("RemoveFromGroup") + $" ({groupID})";
					else
						removeFromGroupButton.buttonLabel.text = Loc.Get("RemoveFromGroup");
                }
            }
            else
            {
				if (EditorController.Instance.currentSelectedObjComponent.groupID == null)
				{
                    addToGroupButton.gameObject.SetActive(true);
                    removeFromGroupButton.gameObject.SetActive(false);
                }
				else
				{
                    addToGroupButton.gameObject.SetActive(false);
                    removeFromGroupButton.gameObject.SetActive(true);
					removeFromGroupButton.buttonLabel.text = Loc.Get("RemoveFromGroup") + $" ({EditorController.Instance.currentSelectedObjComponent.groupID.Value})";
				}
            }
            #endregion

            #region Add Waypoint Button
            if (EditorController.Instance.multipleObjectsSelected)
			{
				// Only enable the button when ALL of the selected objects allow waypoints.
				addWaypointButton.gameObject.SetActive(EditorController.Instance.currentSelectedObjsComponents.All(x => x.canHaveWaypoints));
			}
			else
			{
				addWaypointButton.gameObject.SetActive(EditorController.Instance.currentSelectedObjComponent.canHaveWaypoints);
			}
			#endregion

			if (EvaluateInAllSelectedObjects((obj) => obj.canHaveWaypoints && obj.HasWaypoints()))
			{
				startMovingAtStartToggle.transform.parent.gameObject.SetActive(true);
				movingSpeedField.transform.parent.gameObject.SetActive(true);
				startDelayField.transform.parent.gameObject.SetActive(true);
				waitTimeField.transform.parent.gameObject.SetActive(true);
				waypointModeButton.transform.parent.gameObject.SetActive(true);

                SetPropInToggleDependingOfPropInObjects(startMovingAtStartToggle, (obj) => obj.startMovingAtStart, (obj) => obj.canHaveWaypoints && obj.HasWaypoints());
                SetPropInFieldDependingOfPropInObjects(movingSpeedField, (obj) => obj.movingSpeed.ToString(), (obj) => obj.canHaveWaypoints && obj.HasWaypoints());
                SetPropInFieldDependingOfPropInObjects(startDelayField, (obj) => obj.startDelay.ToString(), (obj) => obj.canHaveWaypoints && obj.HasWaypoints());
                SetPropInFieldDependingOfPropInObjects(waitTimeField, (obj) => obj.waitTime.ToString(), (obj) => obj.canHaveWaypoints && obj.HasWaypoints());
                SetPropInMultipleButtonDependingOfPropInObjects(waypointModeButton, (obj) => (int)obj.waypointMode, (obj) => obj.canHaveWaypoints && obj.HasWaypoints());
            }
			else
			{
                startMovingAtStartToggle.transform.parent.gameObject.SetActive(false);
                movingSpeedField.transform.parent.gameObject.SetActive(false);
                startDelayField.transform.parent.gameObject.SetActive(false);
                waitTimeField.transform.parent.gameObject.SetActive(false);
                waypointModeButton.transform.parent.gameObject.SetActive(false);
            }
		}
		#endregion

		#region Object Specific Attributes Logic
        void UpdateObjectSpecificAttributes(GameObject panelInUI, params List<LE_Object> objComps)
        {
            // OFFICIALLY, THIS IS THE ULTIMATE MOST BETTER AUTOMATED PROPERTY UPDATER OF THE WORLD!
            foreach (var attribute in panelInUI.GetChilds())
            {
                string attributeName = attribute.name; // Assuming the name of the childs in the UI is the same as the REAL attribute name.

				// Only enable buttons when it's selecting one object, it's not compatible with multiple objs.
				if (attribute.GetChild("Button"))
				{
					attribute.SetActive(objComps.Count == 1);
					continue;
				}

				if (!objComps[0].TryGetProperty(attributeName, out _)) continue;
				
				bool valuesAreTheSame = true;
				object value = null;
				#region Detect If Values Foreach Object Are Different
                if (objComps.Count == 1)
				{
					valuesAreTheSame = true;
					value = objComps[0].GetProperty(attributeName);
				}
				else
				{
					value = objComps[0].GetProperty(attributeName);
                    for (int i = 0; i < objComps.Count; i++)
					{
						if (!Equals(objComps[i].GetProperty(attributeName), value))
						{
							valuesAreTheSame = false;
							break;
						}
					}
                }
				#endregion

                if (attribute.ExistsChild("Field"))
                {
                    if (valuesAreTheSame)
					{
                        switch (value)
                        {
                            case int intValue:
                                // For keypad codes, format with leading zeros to preserve 4-digit format (e.g., 0451)
                                if (attributeName == "Keycode" || attributeName == "AlternativeComb")
                                    value = intValue.ToString("D4");
                                else
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

						attribute.GetChild("Field").GetComponent<UICustomInputField>().SetText((string)value, false);
						attribute.GetChild("Field").GetComponent<UICustomInputField>().Set(SetPropertyForObjects(attributeName, value, false, objComps.ToArray()));
                    }
					else
					{
						attribute.GetChild("Field").GetComponent<UICustomInputField>().SetAsUndefined();
					}
                }
                else if (attribute.ExistsChild("Toggle"))
                {
                    if (valuesAreTheSame)
					{
                        // Values for toggles can ONLY be bools, nothing else LOL.
                        if (value is not bool)
                        {
                            Logger.Error($"Tried to update \"{attributeName}\" with value of type \"{value.GetType().Name}\" in a TOGGLE?");
                            continue;
                        }

                        attribute.GetChild("Toggle").GetComponent<UITogglePatcher>().Set((bool)value, false, true);
                    }
					else
					{
						attribute.GetChild("Toggle").GetComponent<UITogglePatcher>().SetAsUndefined();
					}
                }
                else if (attribute.ExistsChild("ButtonMultiple"))
                {
					if (valuesAreTheSame)
					{
                        // Values for multiple option buttons can be, int or maybe an enum
                        if (value is not int && value is not Enum)
                        {
                            Logger.Error($"Tried to update \"{attributeName}\" with value of type \"{value.GetType().Name}\" in a BUTTON MULTIPLE?");
                            continue;
                        }

                        attribute.GetChild("ButtonMultiple").GetComponent<UISmallButtonMultiple>().SetOption((int)value);
                    }
					else
					{
						attribute.GetChild("ButtonMultiple").GetComponent<UISmallButtonMultiple>().SetAsUndefined();
					}
                }
            }

			UpdateOptionalPropertiesVisibility(objComps[0].objectType);
        }

        void UpdateOptionalPropertiesVisibility(LE_Object.ObjectType? type)
        {
            foreach (var prop in optionalProps.Where(p => p.Key.type == type))
            {
				// When selecting multiple objects, just show 'em all!
				if (isSelectingMultipleObjects)
				{
					var attributePanel = attributesPanels[type].GetChild(prop.Key.propName);

                    // Only enable buttons when it's selecting one object, it's not compatible with multiple objs.
                    if (attributePanel.GetChild("Button"))
                    {
						attributePanel.SetActive(false);
                        continue;
                    }

                    // Except those props whose Y position doesn't change, hide those.
                    if (objectPropsWithNoYChange.Contains((type.Value, prop.Key.propName)))
					{
                        attributePanel.SetActive(false);
						continue;
					}

                    attributePanel.SetActive(true);
				}
				else if (currentSelectedObj)
				{
					var value = prop.Value;
                    bool setActive = false;

                    foreach (var required in prop.Value.requiredPropName.Split("||"))
					{
                        string requiredPropName = required.Trim();
						if (string.IsNullOrEmpty(requiredPropName)) break;

                        if (requiredPropName == "waypoints")
                        {
                            setActive = currentSelectedObj.GetProperty<List<WaypointData>>("waypoints").Count > 0;
                        }
                        else if (requiredPropName == "not_waypoints")
                        {
                            setActive = currentSelectedObj.GetProperty<List<WaypointData>>("waypoints").Count == 0;
                        }
                        else
                        {
                            setActive = Equals(currentSelectedObj.GetProperty(requiredPropName), value.requiredPropValue);
                        }

						if (!setActive) break; // If there's just one required prop that's not true, break the loop and DON'T SHOW IT.
                    }

                    attributesPanels[type].GetChild(prop.Key.propName).SetActive(setActive);
                }
            }
        }
		#endregion


        void SetVector3PropertyWithInput(string propertyName, UIVector3Fields fields, bool isGlobalProp = false)
		{
			switch (propertyName)
			{
				case "Position":
					EditorController.Instance.currentSelectedObj.transform.position = fields.GetVector();
					return;
                case "Rotation":
                    EditorController.Instance.currentSelectedObj.transform.localEulerAngles = fields.GetVector();
                    return;
                case "Scale":
                    EditorController.Instance.currentSelectedObj.transform.localScale = fields.GetVector();
					EditorController.Instance.ApplyGizmosArrowsScale();
                    return;
            }

			if (SetPropertyForCurrentSelectedObjects(propertyName, fields.GetVector(), isGlobalProp))
			{
				EditorController.Instance.levelHasBeenModified = true;
			}
		}
		public void SetPropertyWithInput(string propertyName, UICustomInputField inputField, bool isGlobalProp = false)
		{
			if (propertyName == "Keycode" || propertyName == "AlternativeComb")
			{
				string text = inputField.GetText();
				// Accept only if it's 4 digits (0-9)
				if (text.Length == 4 && text.All(char.IsDigit))
				{
					if (SetPropertyForCurrentSelectedObjects(propertyName, text))
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
				if (SetPropertyForCurrentSelectedObjects(propertyName, intensityValue))
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

            if (SetPropertyForCurrentSelectedObjects(propertyName, inputField.GetText(), isGlobalProp))
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

			if (SetPropertyForCurrentSelectedObjects(propertyName, newValue))
			{
				EditorController.Instance.levelHasBeenModified = true;
			}

			UpdateOptionalPropertiesVisibility(type);
        }
		public void SetPropertyWithButtonMultiple(string propertyName, UISmallButtonMultiple button)
		{
			if (SetPropertyForCurrentSelectedObjects(propertyName, button.currentOption))
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

		bool SetPropertyForCurrentSelectedObjects(string propertyName, object value, bool useBaseMethod = false)
		{
			if (EditorController.Instance.multipleObjectsSelected)
			{
				return SetPropertyForObjects(propertyName, value, useBaseMethod, EditorController.Instance.currentSelectedObjsComponents.ToArray());
			}
			else if (EditorController.Instance.currentSelectedObjComponent)
			{
                return SetPropertyForObjects(propertyName, value, useBaseMethod, EditorController.Instance.currentSelectedObjComponent);
            }

			return false;
		}
		bool SetPropertyForObjects(string propertyName, object value, bool useBaseMethod = false, params LE_Object[] objects)
		{
            if (objects.Length > 1)
            {
                bool toReturn = false;

                foreach (var obj in objects)
                {
                    if (useBaseMethod)
                    {
                        toReturn = obj.SetPropertyBase(propertyName, value);
                    }
                    else
                    {
                        toReturn = obj.SetProperty(propertyName, value);
                    }
                }

                return toReturn;
            }
            else if (objects.Length == 1)
            {
                if (useBaseMethod)
                {
                    return objects[0].SetPropertyBase(propertyName, value);
                }
                else
                {
                    return objects[0].SetProperty(propertyName, value);
                }
            }

			return false;
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

		T? GetPropForAllSelectedObjects<T>(Func<LE_Object, T> func, Func<LE_Object, bool> filter = null) where T : struct
		{
			if (EditorController.Instance.multipleObjectsSelected)
			{
				var objects = EditorController.Instance.currentSelectedObjsComponents;

				bool hasValue = false;
				T first = default;

				foreach (var obj in objects)
				{
					if (filter != null && !filter(obj)) continue;

					var value = func(obj);

					if (!hasValue)
					{
						first = value;
						hasValue = true;
						continue;
					}

					if (!EqualityComparer<T>.Default.Equals(first, value)) return null;
				}

				return hasValue ? first : null;
			}
			else
			{
				return func(EditorController.Instance.currentSelectedObjComponent);
			}
		}
        T GetPropForAllSelectedObjectsByRef<T>(Func<LE_Object, T> func, Func<LE_Object, bool> filter = null) where T : class
        {
            if (EditorController.Instance.multipleObjectsSelected)
            {
                var objects = EditorController.Instance.currentSelectedObjsComponents;

                bool hasValue = false;
                T first = default;

                foreach (var obj in objects)
                {
                    if (filter != null && !filter(obj)) continue;

                    var value = func(obj);

                    if (!hasValue)
                    {
                        first = value;
                        hasValue = true;
                        continue;
                    }

                    if (!EqualityComparer<T>.Default.Equals(first, value)) return null;
                }

                return hasValue ? first : null;
            }
            else
            {
                return func(EditorController.Instance.currentSelectedObjComponent);
            }
        }
        void SetPropInToggleDependingOfPropInObjects(UITogglePatcher toggle, Func<LE_Object, bool> selector, Func<LE_Object, bool> filter = null)
		{
            bool? state = GetPropForAllSelectedObjects(selector, filter);

            if (state is bool value)
            {
                toggle.Set(value, true, true);
            }
            else
            {
                toggle.SetAsUndefined();
            }
        }
        void SetPropInFieldDependingOfPropInObjects(UICustomInputField field, Func<LE_Object, string> selector, Func<LE_Object, bool> filter = null)
        {
            string text = GetPropForAllSelectedObjectsByRef(selector, filter);

            if (text is string value)
            {
				field.SetText(value, true);
            }
            else
            {
				field.SetAsUndefined();
            }
        }
        void SetPropInMultipleButtonDependingOfPropInObjects(UISmallButtonMultiple button, Func<LE_Object, int> selector, Func<LE_Object, bool> filter = null)
        {
            int? option = GetPropForAllSelectedObjects(selector, filter);

            if (option is int value)
            {
				button.SetOption(value, true);
            }
            else
            {
                button.SetAsUndefined();
            }
        }

		bool EvaluateInAllSelectedObjects(Func<LE_Object, bool> selector)
		{
			if (EditorController.Instance.multipleObjectsSelected)
			{
				foreach (var obj in EditorController.Instance.currentSelectedObjsComponents)
				{
					if (!selector(obj))
						return false;
				}
				return true;
			}
			else
			{
				return selector(EditorController.Instance.currentSelectedObjComponent);
			}
		}
    }
}