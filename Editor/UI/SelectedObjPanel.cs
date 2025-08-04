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
        public UIToggle setActiveAtStartToggle;
        UIButtonPatcher expandPanelButton;
        UISprite expandPanelButtonSprite;
        UIButtonAsToggle globalObjAttributesToggle;

        GameObject body;
        Transform globalObjectPanelsParent;
        UICustomInputField posXField, posYField, posZField;
        UICustomInputField rotXField, rotYField, rotZField;
        UICustomInputField scaleXField, scaleYField, scaleZField;
        UIToggle collisionToggle;
        UIButtonPatcher addWaypointButton;
        UIToggle startMovingAtStartToggle;
        UICustomInputField movingSpeedField;
        UICustomInputField startDelayField;
        UISmallButtonMultiple waypointModeButton;
        // ------------------------------
        bool showingPanel = false;
        bool panelIsExpanded = false;
        string currentHeaderLocKey = "";
        // ------------------------------
        Transform objectSpecificPanelsParent;
        Dictionary<string, GameObject> attributesPanels = new Dictionary<string, GameObject>();
        Transform whereToCreateObjAttributesParent;

        LE_Object currentSelectedObj;
        bool executeSetActiveAtStartToggleActions = true;
        bool executeCollisionToggleActions = true;

        Vector3 objPositionWhenSelectedField;
        Quaternion objRotationWhenSelectedField;
        Vector3 objScaleWhenSelectedField;

        public static void Create(Transform editorUIParent)
        {
            GameObject root = new GameObject("CurrentSelectedObjPanel");
            root.transform.parent = editorUIParent;
            root.transform.localPosition = new Vector3(-700f, -220f, 0f);
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
            GameObject toggleObj = NGUI_Utils.CreateToggle(header.transform, new Vector3(-220f, 0f, 0f),
                new Vector3Int(48, 48, 0));
            toggleObj.name = "SetActiveAtStartToggle";

            setActiveAtStartToggle = toggleObj.GetComponent<UIToggle>();
            setActiveAtStartToggle.onChange.Clear();
            setActiveAtStartToggle.onChange.Add(new EventDelegate(this, nameof(SetSetActiveAtStart)));
            setActiveAtStartToggle.instantTween = true;

            FractalTooltip tooltip = toggleObj.AddComponent<FractalTooltip>();
            tooltip.toolTipLocKey = "tooltip.SetActiveAtStartToggle";
            tooltip.staticTooltipPos = true;
            tooltip.staticTooltipOffset = new Vector2(0.42f, 0.1f);

            toggleObj.SetActive(false);

            GameObject line = new GameObject("Line");
            line.transform.parent = toggleObj.GetChild("Background").transform;
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
            CreateAddWaypointButton();
            CreateStartMovingAtStartToggle();
            CreateMovingSpeedField();
            CreateStartDelayField();
            CreateWaypointModeButton();
        }
        void CreateObjectPositionUIElements()
        {
            Transform positionThingsParent = new GameObject("Position").transform;
            positionThingsParent.parent = globalObjectPanelsParent;
            positionThingsParent.localPosition = Vector3.zero;
            positionThingsParent.localScale = Vector3.one;

            UILabel title = NGUI_Utils.CreateLabel(positionThingsParent, new Vector3(-230f, 90f, 0f), new Vector3Int(150, 38, 0), "Position");
            title.name = "Title";

            UILabel xTitle = NGUI_Utils.CreateLabel(positionThingsParent, new Vector3(-40f, 90f, 0f), new Vector3Int(28, 38, 0), "X", NGUIText.Alignment.Center,
                UIWidget.Pivot.Center);
            xTitle.name = "XTitle";
            // ------------------------------
            posXField = NGUI_Utils.CreateInputField(positionThingsParent, new Vector3(10f, 90f, 0f), new Vector3Int(65, 38, 0), 27, "0", inputType: UICustomInputField.UIInputType.FLOAT,
                maxDecimals: 2);
            posXField.name = "XField";
            posXField.onSelected += (() => OnGlobalAttributeFieldSelected(GlobalFieldType.Position));
            posXField.onChange += (() => SetPropertyWithInput("XPosition", posXField));
            posXField.onDeselected += (() => OnGlobalAttributeFieldDeselected(GlobalFieldType.Position));

            UILabel yTitle = NGUI_Utils.CreateLabel(positionThingsParent, new Vector3(60f, 90f, 0f), new Vector3Int(28, 38, 0), "Y", NGUIText.Alignment.Center,
                UIWidget.Pivot.Center);
            yTitle.name = "YTitle";
            // ------------------------------
            posYField = NGUI_Utils.CreateInputField(positionThingsParent, new Vector3(110f, 90f, 0f), new Vector3Int(65, 38, 0), 27, "0", inputType: UICustomInputField.UIInputType.FLOAT,
                maxDecimals: 2);
            posYField.name = "YField";
            posYField.onSelected += (() => OnGlobalAttributeFieldSelected(GlobalFieldType.Position));
            posYField.onChange += (() => SetPropertyWithInput("YPosition", posYField));
            posYField.onDeselected += (() => OnGlobalAttributeFieldDeselected(GlobalFieldType.Position));

            UILabel zTitle = NGUI_Utils.CreateLabel(positionThingsParent, new Vector3(160f, 90f, 0f), new Vector3Int(28, 38, 0), "Z", NGUIText.Alignment.Center,
                UIWidget.Pivot.Center);
            zTitle.name = "ZTitle";
            // ------------------------------
            posZField = NGUI_Utils.CreateInputField(positionThingsParent, new Vector3(210f, 90f, 0f), new Vector3Int(65, 38, 0), 27, "0", inputType: UICustomInputField.UIInputType.FLOAT,
                maxDecimals: 2);
            posZField.name = "ZField";
            posZField.onSelected += (() => OnGlobalAttributeFieldSelected(GlobalFieldType.Position));
            posZField.onChange += (() => SetPropertyWithInput("ZPosition", posZField));
            posZField.onDeselected += (() => OnGlobalAttributeFieldDeselected(GlobalFieldType.Position));
        }
        void CreateObjectRotationUIElements()
        {
            Transform rotationThingsParent = new GameObject("Rotation").transform;
            rotationThingsParent.parent = globalObjectPanelsParent;
            rotationThingsParent.localPosition = Vector3.zero;
            rotationThingsParent.localScale = Vector3.one;

            UILabel title = NGUI_Utils.CreateLabel(rotationThingsParent, new Vector3(-230f, 40f, 0f), new Vector3Int(150, 38, 0), "Rotation");
            title.name = "Title";

            UILabel xTitle = NGUI_Utils.CreateLabel(rotationThingsParent, new Vector3(-40f, 40f, 0f), new Vector3Int(28, 38, 0), "X", NGUIText.Alignment.Center,
                UIWidget.Pivot.Center);
            xTitle.name = "XTitle";
            // ------------------------------
            rotXField = NGUI_Utils.CreateInputField(rotationThingsParent, new Vector3(10f, 40f, 0f), new Vector3Int(65, 38, 0), 27, "0", inputType: UICustomInputField.UIInputType.FLOAT,
                maxDecimals: 2);
            rotXField.name = "XField";
            rotXField.onSelected += (() => OnGlobalAttributeFieldSelected(GlobalFieldType.Rotation));
            rotXField.onChange += (() => SetPropertyWithInput("XRotation", rotXField));
            rotXField.onDeselected += (() => OnGlobalAttributeFieldDeselected(GlobalFieldType.Rotation));

            UILabel yTitle = NGUI_Utils.CreateLabel(rotationThingsParent, new Vector3(60f, 40f, 0f), new Vector3Int(28, 38, 0), "Y", NGUIText.Alignment.Center,
                UIWidget.Pivot.Center);
            yTitle.name = "YTitle";
            // ------------------------------
            rotYField = NGUI_Utils.CreateInputField(rotationThingsParent, new Vector3(110f, 40f, 0f), new Vector3Int(65, 38, 0), 27, "0", inputType: UICustomInputField.UIInputType.FLOAT,
                maxDecimals: 2);
            rotYField.name = "YField";
            rotYField.onSelected += (() => OnGlobalAttributeFieldSelected(GlobalFieldType.Rotation));
            rotYField.onChange += (() => SetPropertyWithInput("YRotation", rotYField));
            rotYField.onDeselected += (() => OnGlobalAttributeFieldDeselected(GlobalFieldType.Rotation));

            UILabel zTitle = NGUI_Utils.CreateLabel(rotationThingsParent, new Vector3(160f, 40f, 0f), new Vector3Int(28, 38, 0), "Z", NGUIText.Alignment.Center,
                UIWidget.Pivot.Center);
            zTitle.name = "ZTitle";
            // ------------------------------
            rotZField = NGUI_Utils.CreateInputField(rotationThingsParent, new Vector3(210f, 40f, 0f), new Vector3Int(65, 38, 0), 27, "0", inputType: UICustomInputField.UIInputType.FLOAT,
                maxDecimals: 2);
            rotZField.name = "ZField";
            rotZField.onSelected += (() => OnGlobalAttributeFieldSelected(GlobalFieldType.Rotation));
            rotZField.onChange += (() => SetPropertyWithInput("ZRotation", rotZField));
            rotZField.onDeselected += (() => OnGlobalAttributeFieldDeselected(GlobalFieldType.Rotation));
        }
        void CreateObjectScaleUIElements()
        {
            Transform scaleThingsParent = new GameObject("Scale").transform;
            scaleThingsParent.parent = globalObjectPanelsParent;
            scaleThingsParent.localPosition = Vector3.zero;
            scaleThingsParent.localScale = Vector3.one;

            UILabel title = NGUI_Utils.CreateLabel(scaleThingsParent, new Vector3(-230f, -10f, 0f), new Vector3Int(150, 38, 0), "Scale");
            title.name = "Title";

            UILabel xTitle = NGUI_Utils.CreateLabel(scaleThingsParent, new Vector3(-40f, -10f, 0f), new Vector3Int(28, 38, 0), "X", NGUIText.Alignment.Center,
                UIWidget.Pivot.Center);
            xTitle.name = "XTitle";
            // ------------------------------
            scaleXField = NGUI_Utils.CreateInputField(scaleThingsParent, new Vector3(10f, -10f, 0f), new Vector3Int(65, 38, 0), 27, "1", inputType: UICustomInputField.UIInputType.FLOAT,
                maxDecimals: 2);
            scaleXField.name = "XField";
            scaleXField.onSelected += (() => OnGlobalAttributeFieldSelected(GlobalFieldType.Scale));
            scaleXField.onChange += (() => SetPropertyWithInput("XScale", scaleXField));
            scaleXField.onDeselected += (() => OnGlobalAttributeFieldDeselected(GlobalFieldType.Scale));

            UILabel yTitle = NGUI_Utils.CreateLabel(scaleThingsParent, new Vector3(60f, -10f, 0f), new Vector3Int(28, 38, 0), "Y", NGUIText.Alignment.Center,
                UIWidget.Pivot.Center);
            yTitle.name = "YTitle";
            // ------------------------------
            scaleYField = NGUI_Utils.CreateInputField(scaleThingsParent, new Vector3(110f, -10f, 0f), new Vector3Int(65, 38, 0), 27, "1", inputType: UICustomInputField.UIInputType.FLOAT,
                maxDecimals: 2);
            scaleYField.name = "YField";
            scaleYField.onSelected += (() => OnGlobalAttributeFieldSelected(GlobalFieldType.Scale));
            scaleYField.onChange += (() => SetPropertyWithInput("YScale", scaleYField));
            scaleYField.onDeselected += (() => OnGlobalAttributeFieldDeselected(GlobalFieldType.Scale));

            UILabel zTitle = NGUI_Utils.CreateLabel(scaleThingsParent, new Vector3(160f, -10f, 0f), new Vector3Int(28, 38, 0), "Z", NGUIText.Alignment.Center,
                UIWidget.Pivot.Center);
            zTitle.name = "ZTitle";
            // ------------------------------
            scaleZField = NGUI_Utils.CreateInputField(scaleThingsParent, new Vector3(210f, -10f, 0f), new Vector3Int(65, 38, 0), 27, "1", inputType: UICustomInputField.UIInputType.FLOAT,
                maxDecimals: 2);
            scaleZField.name = "ZField";
            scaleZField.onSelected += (() => OnGlobalAttributeFieldSelected(GlobalFieldType.Scale));
            scaleZField.onChange += (() => SetPropertyWithInput("ZScale", scaleZField));
            scaleZField.onDeselected += (() => OnGlobalAttributeFieldDeselected(GlobalFieldType.Scale));
        }
        void CreateCollisionToggle()
        {
            Transform collisionToggleParent = new GameObject("Collision").transform;
            collisionToggleParent.parent = globalObjectPanelsParent;
            collisionToggleParent.localPosition = Vector3.zero;
            collisionToggleParent.localScale = Vector3.one;

            UILabel title = NGUI_Utils.CreateLabel(collisionToggleParent, new Vector3(-230, -60), new Vector3Int(395, 38, 0), "Collision");
            title.name = "Title";

            GameObject toggle = NGUI_Utils.CreateToggle(collisionToggleParent, new Vector3(200, -60), Vector3Int.one * 48);
            toggle.name = "Toggle";
            toggle.GetComponent<UIToggle>().onChange.Clear();
            var toggleDelegate = NGUI_Utils.CreateEvenDelegate(this, nameof(SetCollisionToggle));
            toggle.GetComponent<UIToggle>().onChange.Add(toggleDelegate);
            collisionToggle = toggle.GetComponent<UIToggle>();
            collisionToggle.instantTween = true;

            GameObject line = new GameObject("Line");
            line.transform.parent = toggle.GetChild("Background").transform;
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
        void CreateAddWaypointButton()
        {
            addWaypointButton = NGUI_Utils.CreateButton(globalObjectPanelsParent, new Vector3(0, -115), new Vector3Int(480, 50, 0), "AddWaypoint");
            addWaypointButton.name = "AddWaypointButton";
            addWaypointButton.onClick += AddWaypointForObject;
        }
        void CreateStartMovingAtStartToggle()
        {
            Transform toggleParent = new GameObject("StartMovingAtStart").transform;
            toggleParent.parent = globalObjectPanelsParent;
            toggleParent.localPosition = Vector3.zero;
            toggleParent.localScale = Vector3.one;

            UILabel title = NGUI_Utils.CreateLabel(toggleParent, new Vector3(-230, -165), new Vector3Int(395, 38, 0), "Start Moving At Start");
            title.name = "Title";

            GameObject toggle = NGUI_Utils.CreateToggle(toggleParent, new Vector3(200, -165), Vector3Int.one * 48);
            toggle.name = "Toggle";
            toggle.GetComponent<UIToggle>().onChange.Clear();
            toggle.GetComponent<UIToggle>().onChange.Add(new EventDelegate(this, nameof(SetStartMovingAtStart)));
            startMovingAtStartToggle = toggle.GetComponent<UIToggle>();
            startMovingAtStartToggle.instantTween = true;
        }
        void CreateMovingSpeedField()
        {
            Transform fieldParent = new GameObject("MovingSpeed").transform;
            fieldParent.parent = globalObjectPanelsParent;
            fieldParent.localPosition = Vector3.zero;
            fieldParent.localScale = Vector3.one;

            UILabel title = NGUI_Utils.CreateLabel(fieldParent, new Vector3(-230f, -215f, 0f), new Vector3Int(260, 38, 0), "Moving Speed");
            title.name = "Title";

            movingSpeedField = NGUI_Utils.CreateInputField(fieldParent, new Vector3(140, -215), new Vector3Int(200, 38, 0), 27, "5", false,
                inputType: UICustomInputField.UIInputType.NON_NEGATIVE_FLOAT);
            movingSpeedField.name = "Field";
            movingSpeedField.onChange += () => SetPropertyWithInput("MovingSpeed", movingSpeedField);
        }
        void CreateStartDelayField()
        {
            Transform fieldParent = new GameObject("StartDelay").transform;
            fieldParent.parent = globalObjectPanelsParent;
            fieldParent.localPosition = Vector3.zero;
            fieldParent.localScale = Vector3.one;

            UILabel title = NGUI_Utils.CreateLabel(fieldParent, new Vector3(-230f, -260f, 0f), new Vector3Int(260, 38, 0), "Start Delay");
            title.name = "Title";

            startDelayField = NGUI_Utils.CreateInputField(fieldParent, new Vector3(140, -260), new Vector3Int(200, 38, 0), 27, "0", false,
                inputType: UICustomInputField.UIInputType.NON_NEGATIVE_FLOAT);
            startDelayField.name = "Field";
            startDelayField.onChange += () => SetPropertyWithInput("StartDelay", startDelayField);
        }
        void CreateWaypointModeButton()
        {
            var optionParent = new GameObject("WaypointMode").transform;
            optionParent.parent = globalObjectPanelsParent;
            optionParent.localPosition = Vector3.zero;
            optionParent.localScale = Vector3.one;

            UILabel title = NGUI_Utils.CreateLabel(optionParent, new Vector3(-230f, -315f, 0f), new Vector3Int(260, 38, 0), "Moving Speed");
            title.name = "Title";

            waypointModeButton = NGUI_Utils.CreateSmallButtonMultiple(optionParent, new Vector3(140, -315),
                new Vector3Int(200, 38, 0), "NONE", 25);
            waypointModeButton.name = "ButtonMultiple";
            waypointModeButton.onChange += (id) => SetPropertyWithButtonMultiple("WaypointMode", waypointModeButton);
            waypointModeButton.GetComponent<UIButtonScale>().hover = Vector3.one * 1.05f;
            waypointModeButton.GetComponent<UIButtonScale>().pressed = Vector3.one * 1.02f;
            waypointModeButton.AddOption("NONE", Color.black);
            waypointModeButton.AddOption("TRAVEL BACK", Color.red);
            waypointModeButton.AddOption("LOOP", Color.blue);
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
            CreateDirectionalLightAttributesPanel();
            CreatePointLightAttributesPanel();
            CreateSawAttributesPanel();
            CreateSawWaypointAttributesPanel();
            CreateSwitchAttributesPanel();
            CreateAmmoAndHealthPackAttributesPanel();
            CreateLaserAttributesPanel();
            CreateCeilingLightPanel();
            CreateFlameTrapAttributesPanel();
            CreatePressurePlateAttributesPanel();
            CreateScreenAttributesPanel();
            CreateSmallScreenAttributesPanel();
            CreateTriggerAttributesPanel();
            CreateDoorAttributesPanel();
            CreateDoorV2AttributesPanel();
            CreateDeathTriggerAttributesPanel();
            CreateWaypointAttributesPanel();
            CreateLaserFieldAttributesPanel();
        }
        #region Create Object Specific Panels
        void CreateDirectionalLightAttributesPanel()
        {
            GameObject directionalLightAttributes = new GameObject("Directional Light");
            directionalLightAttributes.transform.parent = objectSpecificPanelsParent;
            directionalLightAttributes.transform.localPosition = Vector3.zero;
            directionalLightAttributes.transform.localScale = Vector3.one;

            SetCurrentParentToCreateAttributes(directionalLightAttributes);

            CreateObjectAttribute("ColorHex", AttributeType.INPUT_FIELD, "FFFFFF", UICustomInputField.UIInputType.HEX_COLOR, "Color", true);
            CreateObjectAttribute("Intensity", AttributeType.INPUT_FIELD, "1", UICustomInputField.UIInputType.NON_NEGATIVE_FLOAT, "Intensity");

            directionalLightAttributes.SetActive(false);
            attributesPanels.Add("Directional Light", directionalLightAttributes);
        }
        void CreatePointLightAttributesPanel()
        {
            GameObject pointLightAttributes = new GameObject("Point Light");
            pointLightAttributes.transform.parent = objectSpecificPanelsParent;
            pointLightAttributes.transform.localPosition = Vector3.zero;
            pointLightAttributes.transform.localScale = Vector3.one;

            SetCurrentParentToCreateAttributes(pointLightAttributes);

            CreateObjectAttribute("ColorHex", AttributeType.INPUT_FIELD, "FFFFFF", UICustomInputField.UIInputType.HEX_COLOR, "Color", true);
            CreateObjectAttribute("Intensity", AttributeType.INPUT_FIELD, "1", UICustomInputField.UIInputType.NON_NEGATIVE_FLOAT, "Intensity");
            CreateObjectAttribute("Range", AttributeType.INPUT_FIELD, "10", UICustomInputField.UIInputType.NON_NEGATIVE_FLOAT, "Range");

            pointLightAttributes.SetActive(false);
            attributesPanels.Add("Point Light", pointLightAttributes);
        }
        void CreateSawAttributesPanel()
        {
            GameObject sawAttributes = new GameObject("Saw");
            sawAttributes.transform.parent = objectSpecificPanelsParent;
            sawAttributes.transform.localPosition = Vector3.zero;
            sawAttributes.transform.localScale = Vector3.one;

            SetCurrentParentToCreateAttributes(sawAttributes);

            CreateObjectAttribute("ActivateOnStart", AttributeType.TOGGLE, true, null, "ActivateOnStart");
            CreateObjectAttribute("Damage", AttributeType.INPUT_FIELD, "50", UICustomInputField.UIInputType.NON_NEGATIVE_INT, "Damage");
            CreateObjectAttribute("TravelBack", AttributeType.TOGGLE, true, null, "TravelBack", tooltip: "TravelBackTooltip");
            CreateObjectAttribute("Loop", AttributeType.TOGGLE, false, null, "Loop", tooltip: "LoopTooltip");
            CreateObjectAttribute("AddWaypoint", AttributeType.BUTTON, null, null, "AddWaypoint");
            CreateObjectAttribute("WaitTime", AttributeType.INPUT_FIELD, "0", UICustomInputField.UIInputType.NON_NEGATIVE_FLOAT, "WaitTime");
            CreateObjectAttribute("Rotate", AttributeType.TOGGLE, false, null, "Rotate");
            CreateObjectAttribute("RotateSpeed", AttributeType.INPUT_FIELD, "1", UICustomInputField.UIInputType.NON_NEGATIVE_INT, "RotateSpeed");

            sawAttributes.SetActive(false);
            attributesPanels.Add("Saw", sawAttributes);
        }
        void CreateSawWaypointAttributesPanel()
        {
            GameObject sawWaypointAttributes = new GameObject("Saw Waypoint");
            sawWaypointAttributes.transform.parent = objectSpecificPanelsParent;
            sawWaypointAttributes.transform.localPosition = Vector3.zero;
            sawWaypointAttributes.transform.localScale = Vector3.one;

            SetCurrentParentToCreateAttributes(sawWaypointAttributes);

            CreateObjectAttribute("WaitTime", AttributeType.INPUT_FIELD, "0.3", UICustomInputField.UIInputType.NON_NEGATIVE_FLOAT, "WaitTime");
            CreateObjectAttribute("AddWaypoint", AttributeType.BUTTON, null, null, "AddWaypoint");

            sawWaypointAttributes.SetActive(false);
            attributesPanels.Add("Saw Waypoint", sawWaypointAttributes);
        }
        void CreateSwitchAttributesPanel()
        {
            GameObject switchAttributes = new GameObject("Switch");
            switchAttributes.transform.parent = objectSpecificPanelsParent;
            switchAttributes.transform.localPosition = Vector3.zero;
            switchAttributes.transform.localScale = Vector3.one;

            SetCurrentParentToCreateAttributes(switchAttributes);

            CreateObjectAttribute("InitialState", AttributeType.BUTTON_MULTIPLE, 0, null, "InitialState");
            var initiaStateButton = switchAttributes.GetChildAt("InitialState/ButtonMultiple").GetComponent<UISmallButtonMultiple>();
            initiaStateButton.AddOption("DEACTIVATED", new Color(0.8f, 0f, 0f));
            initiaStateButton.AddOption("ACTIVATED", Color.green);
            initiaStateButton.AddOption("UNUSABLE", Color.black);

            CreateObjectAttribute("UsableOnce", AttributeType.TOGGLE, false, null, "UsableOnce");
            CreateObjectAttribute("CanBeShotByTaser", AttributeType.TOGGLE, true, null, "CanUseTaser");
            CreateObjectAttribute("ManageEvents", AttributeType.BUTTON, null, null, "ManageEvents");

            switchAttributes.SetActive(false);
            attributesPanels.Add("Switch", switchAttributes);
        }
        void CreateAmmoAndHealthPackAttributesPanel()
        {
            GameObject ammoHealthAttributes = new GameObject("Ammo Pack | Health Pack");
            ammoHealthAttributes.transform.parent = objectSpecificPanelsParent;
            ammoHealthAttributes.transform.localPosition = Vector3.zero;
            ammoHealthAttributes.transform.localScale = Vector3.one;

            SetCurrentParentToCreateAttributes(ammoHealthAttributes);

            CreateObjectAttribute("RespawnTime", AttributeType.INPUT_FIELD, "50", UICustomInputField.UIInputType.NON_NEGATIVE_FLOAT, "RespawnTime");

            ammoHealthAttributes.SetActive(false);
            attributesPanels.Add("Ammo Pack | Health Pack", ammoHealthAttributes);
        }
        void CreateLaserAttributesPanel()
        {
            GameObject laserAttributes = new GameObject("Laser");
            laserAttributes.transform.parent = objectSpecificPanelsParent;
            laserAttributes.transform.localPosition = Vector3.zero;
            laserAttributes.transform.localScale = Vector3.one;

            SetCurrentParentToCreateAttributes(laserAttributes);

            CreateObjectAttribute("ActivateOnStart", AttributeType.TOGGLE, true, null, "ActivateOnStart");
            CreateObjectAttribute("InstantKill", AttributeType.TOGGLE, false, null, "InstaKill");
            CreateObjectAttribute("Damage", AttributeType.INPUT_FIELD, "34", UICustomInputField.UIInputType.NON_NEGATIVE_FLOAT, "Damage");
            CreateObjectAttribute("Blinking", AttributeType.TOGGLE, false, null, "Blinking");
            CreateObjectAttribute("OFFDuration", AttributeType.INPUT_FIELD, "1", UICustomInputField.UIInputType.NON_NEGATIVE_FLOAT, "OffDuration");
            CreateObjectAttribute("ONDuration", AttributeType.INPUT_FIELD, "1", UICustomInputField.UIInputType.NON_NEGATIVE_FLOAT, "OnDuration");

            laserAttributes.SetActive(false);
            attributesPanels.Add("Laser", laserAttributes);
        }
        void CreateCeilingLightPanel()
        {
            GameObject ceilingLightAttributes = new GameObject("Ceiling Light");
            ceilingLightAttributes.transform.parent = objectSpecificPanelsParent;
            ceilingLightAttributes.transform.localPosition = Vector3.zero;
            ceilingLightAttributes.transform.localScale = Vector3.one;

            SetCurrentParentToCreateAttributes(ceilingLightAttributes);

            CreateObjectAttribute("ActivateOnStart", AttributeType.TOGGLE, true, null, "ActivateOnStart");
            CreateObjectAttribute("ColorHex", AttributeType.INPUT_FIELD, "FFFFFF", UICustomInputField.UIInputType.HEX_COLOR, "Color", true);
            CreateObjectAttribute("Range", AttributeType.INPUT_FIELD, "6", UICustomInputField.UIInputType.NON_NEGATIVE_FLOAT, "Range");

            ceilingLightAttributes.SetActive(false);
            attributesPanels.Add("Ceiling Light", ceilingLightAttributes);
        }
        void CreateFlameTrapAttributesPanel()
        {
            GameObject flameTrapAttributes = new GameObject("Flame Trap");
            flameTrapAttributes.transform.parent = objectSpecificPanelsParent;
            flameTrapAttributes.transform.localPosition = Vector3.zero;
            flameTrapAttributes.transform.localScale = Vector3.one;

            SetCurrentParentToCreateAttributes(flameTrapAttributes);

            CreateObjectAttribute("ActivateOnStart", AttributeType.TOGGLE, true, null, "ActivateOnStart");
            CreateObjectAttribute("Constant", AttributeType.TOGGLE, false, null, "Constant");

            flameTrapAttributes.SetActive(false);
            attributesPanels.Add("Flame Trap", flameTrapAttributes);
        }
        void CreatePressurePlateAttributesPanel()
        {
            GameObject pressurePlateAttributes = new GameObject("Pressure Plate");
            pressurePlateAttributes.transform.parent = objectSpecificPanelsParent;
            pressurePlateAttributes.transform.localPosition = Vector3.zero;
            pressurePlateAttributes.transform.localScale = Vector3.one;

            SetCurrentParentToCreateAttributes(pressurePlateAttributes);

            CreateObjectAttribute("OnlyOnce", AttributeType.TOGGLE, false, null, "OnlyOnce");
            CreateObjectAttribute("ManageEvents", AttributeType.BUTTON, null, null, "ManageEvents");

            pressurePlateAttributes.SetActive(false);
            attributesPanels.Add("Pressure Plate", pressurePlateAttributes);
        }
        void CreateScreenAttributesPanel()
        {
            GameObject screenAttributes = new GameObject("Screen");
            screenAttributes.transform.parent = objectSpecificPanelsParent;
            screenAttributes.transform.localPosition = Vector3.zero;
            screenAttributes.transform.localScale = Vector3.one;

            SetCurrentParentToCreateAttributes(screenAttributes);

            CreateObjectAttribute("ScreenColor", AttributeType.BUTTON_MULTIPLE, 0, null, "ColorType");
            var screenColorButton = screenAttributes.GetChildAt("ColorType/ButtonMultiple").GetComponent<UISmallButtonMultiple>();
            screenColorButton.AddOption("CYAN", null); // Use the default button color.
            screenColorButton.AddOption("GREEN", Color.green);
            screenColorButton.AddOption("RED", new Color(0.8f, 0f, 0f));

            CreateObjectAttribute("InvisibleMesh", AttributeType.TOGGLE, false, null, "InvisibleMesh");
            CreateObjectAttribute("InvertTextWithGravity", AttributeType.TOGGLE, true, null, "InvertWithGravity");
            CreateObjectAttribute("ScaledText", AttributeType.TOGGLE, true, null, "ScaledText");
            CreateObjectAttribute("EditText", AttributeType.BUTTON, null, null, "EditText");

            screenAttributes.SetActive(false);
            attributesPanels.Add("Screen", screenAttributes);
        }
        void CreateSmallScreenAttributesPanel()
        {
            GameObject smallScreenAttributes = new GameObject("Small Screen");
            smallScreenAttributes.transform.parent = objectSpecificPanelsParent;
            smallScreenAttributes.transform.localPosition = Vector3.zero;
            smallScreenAttributes.transform.localScale = Vector3.one;

            SetCurrentParentToCreateAttributes(smallScreenAttributes);

            CreateObjectAttribute("ScreenColor", AttributeType.BUTTON_MULTIPLE, 0, null, "ColorType");
            var screenColorButton = smallScreenAttributes.GetChildAt("ColorType/ButtonMultiple").GetComponent<UISmallButtonMultiple>();
            screenColorButton.AddOption("CYAN", null); // Use the default button color.
            screenColorButton.AddOption("GREEN", Color.green);
            screenColorButton.AddOption("RED", new Color(0.8f, 0f, 0f));

            CreateObjectAttribute("InvisibleMesh", AttributeType.TOGGLE, false, null, "InvisibleMesh");
            CreateObjectAttribute("InvertTextWithGravity", AttributeType.TOGGLE, true, null, "InvertWithGravity");
            CreateObjectAttribute("ScaledText", AttributeType.TOGGLE, true, null, "ScaledText");
            CreateObjectAttribute("EditText", AttributeType.BUTTON, null, null, "EditText");

            smallScreenAttributes.SetActive(false);
            attributesPanels.Add("Small Screen", smallScreenAttributes);
        }
        void CreateTriggerAttributesPanel()
        {
            GameObject triggerAttributes = new GameObject("Trigger");
            triggerAttributes.transform.parent = objectSpecificPanelsParent;
            triggerAttributes.transform.localPosition = Vector3.zero;
            triggerAttributes.transform.localScale = Vector3.one;

            SetCurrentParentToCreateAttributes(triggerAttributes);

            CreateObjectAttribute("ManageEvents", AttributeType.BUTTON, null, null, "ManageEvents");

            triggerAttributes.SetActive(false);
            attributesPanels.Add("Trigger", triggerAttributes);
        }
        void CreateDoorAttributesPanel()
        {
            GameObject doorAttributes = new GameObject("Door");
            doorAttributes.transform.parent = objectSpecificPanelsParent;
            doorAttributes.transform.localPosition = Vector3.zero;
            doorAttributes.transform.localScale = Vector3.one;

            SetCurrentParentToCreateAttributes(doorAttributes);

            CreateObjectAttribute("Is Automatic?", AttributeType.TOGGLE, false, null, "IsAuto");

            CreateObjectAttribute("Initial State", AttributeType.BUTTON_MULTIPLE, 0, null, "InitialState");
            var initialStateButton = doorAttributes.GetChildAt("InitialState/ButtonMultiple").GetComponent<UISmallButtonMultiple>();
            initialStateButton.AddOption("CLOSED", new Color(0.8f, 0f, 0f));
            initialStateButton.AddOption("OPEN", Color.green);

            CreateObjectAttribute("Initial State", AttributeType.BUTTON_MULTIPLE, 0, null, "InitialStateAuto", dontChangeYPos: true);
            var initialStateAutoButton = doorAttributes.GetChildAt("InitialStateAuto/ButtonMultiple").GetComponent<UISmallButtonMultiple>();
            initialStateAutoButton.AddOption("LOCKED", new Color(0.8f, 0f, 0f));
            initialStateAutoButton.AddOption("UNLOCKED", Color.green);

            doorAttributes.SetActive(false);
            attributesPanels.Add("Door", doorAttributes);
        }
        void CreateDoorV2AttributesPanel()
        {
            GameObject doorV2Attributes = new GameObject("Door V2");
            doorV2Attributes.transform.parent = objectSpecificPanelsParent;
            doorV2Attributes.transform.localPosition = Vector3.zero;
            doorV2Attributes.transform.localScale = Vector3.one;

            SetCurrentParentToCreateAttributes(doorV2Attributes);

            CreateObjectAttribute("Is Automatic?", AttributeType.TOGGLE, false, null, "IsAuto");

            CreateObjectAttribute("Initial State", AttributeType.BUTTON_MULTIPLE, 0, null, "InitialState");
            var initialStateButton = doorV2Attributes.GetChildAt("InitialState/ButtonMultiple").GetComponent<UISmallButtonMultiple>();
            initialStateButton.AddOption("CLOSED", new Color(0.8f, 0f, 0f));
            initialStateButton.AddOption("OPEN", Color.green);

            CreateObjectAttribute("Initial State", AttributeType.BUTTON_MULTIPLE, 0, null, "InitialStateAuto", dontChangeYPos: true);
            var initialStateAutoButton = doorV2Attributes.GetChildAt("InitialStateAuto/ButtonMultiple").GetComponent<UISmallButtonMultiple>();
            initialStateAutoButton.AddOption("LOCKED", new Color(0.8f, 0f, 0f));
            initialStateAutoButton.AddOption("UNLOCKED", Color.green);

            doorV2Attributes.SetActive(false);
            attributesPanels.Add("Door V2", doorV2Attributes);
        }
        void CreateDeathTriggerAttributesPanel()
        {
            GameObject deathTriggerAttributes = new GameObject("Death Trigger");
            deathTriggerAttributes.transform.parent = objectSpecificPanelsParent;
            deathTriggerAttributes.transform.localPosition = Vector3.zero;
            deathTriggerAttributes.transform.localScale = Vector3.one;

            SetCurrentParentToCreateAttributes(deathTriggerAttributes);

            CreateObjectAttribute("DeathTriggerType", AttributeType.BUTTON_MULTIPLE, null, null, "Type");
            var typeButton = deathTriggerAttributes.GetChildAt("Type/ButtonMultiple").GetComponent<UISmallButtonMultiple>();
            typeButton.AddOption("DeathRelocation", new Color(0.8f, 0f, 0f));
            typeButton.AddOption("DeathImminent", Color.black);

            CreateObjectAttribute("Delay", AttributeType.INPUT_FIELD, "2", UICustomInputField.UIInputType.NON_NEGATIVE_FLOAT, "Delay");

            deathTriggerAttributes.SetActive(false);
            attributesPanels.Add("Death Trigger", deathTriggerAttributes);
        }
        void CreateWaypointAttributesPanel()
        {
            GameObject waypointAttributes = new GameObject("Waypoint");
            waypointAttributes.transform.parent = objectSpecificPanelsParent;
            waypointAttributes.transform.localPosition = Vector3.zero;
            waypointAttributes.transform.localScale = Vector3.one;

            SetCurrentParentToCreateAttributes(waypointAttributes);

            CreateObjectAttribute("WaitTime", AttributeType.INPUT_FIELD, "0.3", UICustomInputField.UIInputType.NON_NEGATIVE_FLOAT, "WaitTime");

            attributesPanels.Add("Waypoint", waypointAttributes);
        }
        void CreateLaserFieldAttributesPanel()
        {
            GameObject laserFieldAttributes = new GameObject("Laser Field");
            laserFieldAttributes.transform.parent = objectSpecificPanelsParent;
            laserFieldAttributes.transform.localPosition = Vector3.zero;
            laserFieldAttributes.transform.localScale = Vector3.one;

            SetCurrentParentToCreateAttributes(laserFieldAttributes);

            CreateObjectAttribute("Invisible Edges", AttributeType.TOGGLE, false, null, "InvisibleEdges");

            attributesPanels.Add("Laser Field", laserFieldAttributes);
        }

        enum AttributeType { TOGGLE, INPUT_FIELD, BUTTON, BUTTON_MULTIPLE }
        void SetCurrentParentToCreateAttributes(GameObject newParent)
        {
            whereToCreateObjAttributesParent = newParent.transform;
        }
        void CreateObjectAttribute(string text, AttributeType attrType, object defaultValue, UICustomInputField.UIInputType? fieldType, string targetPropName,
            bool createHastag = false, string tooltip = null, bool dontChangeYPos = false)
        {
            GameObject attributeParent = new GameObject(targetPropName);
            attributeParent.transform.parent = whereToCreateObjAttributesParent;
            attributeParent.transform.localPosition = Vector3.zero;
            attributeParent.transform.localScale = Vector3.one;

            float yPos = 90 - (50 * (whereToCreateObjAttributesParent.gameObject.GetChilds().Where(x => !x.ExistsChild("IgnoreYPos")).ToArray().Length - 1));
            if (dontChangeYPos) yPos += 50;

            if (attrType != AttributeType.BUTTON)
            {
                int titleWidth = attrType == AttributeType.INPUT_FIELD || attrType == AttributeType.BUTTON_MULTIPLE ? 260 : 395;
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
            }
            else if (attrType == AttributeType.TOGGLE)
            {
                GameObject toggle = NGUI_Utils.CreateToggle(attributeParent.transform, new Vector3(200f, yPos), new Vector3Int(48, 48, 0));
                toggle.name = "Toggle";
                toggle.GetComponent<UIToggle>().onChange.Clear();
                var toggleDelegate = NGUI_Utils.CreateEvenDelegate(this, nameof(SetPropertyWithToggle),
                    NGUI_Utils.CreateEventDelegateParamter(this, "propertyName", targetPropName),
                    NGUI_Utils.CreateEventDelegateParamter(this, "toggle", toggle.GetComponent<UIToggle>()));
                toggle.GetComponent<UIToggle>().onChange.Add(toggleDelegate);
                if ((bool)defaultValue) toggle.GetComponent<UIToggle>().Set(true);
                if (tooltip != null)
                {
                    toggle.AddComponent<FractalTooltip>().toolTipLocKey = tooltip;
                }
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
            }

            if (dontChangeYPos)
            {
                GameObject ignoreYPosObj = new GameObject("IgnoreYPos");
                ignoreYPosObj.transform.parent = attributeParent.transform;
                ignoreYPosObj.transform.localPosition = Vector3.zero;
                ignoreYPosObj.transform.localScale = Vector3.one;
            }
        }
        #endregion
        #endregion

        public void ShowPanel(bool show, string headerLocKey) => ShowPanel(show, panelIsExpanded, headerLocKey);
        public void ShowPanel(bool show, bool expand, string headerLocKey)
        {
            headerTitle.SetLocKey(headerLocKey);
            currentHeaderLocKey = headerLocKey;

            if (show)
            {
                if (!expand) // Normal selection
                {
                    gameObject.transform.localPosition = new Vector3(-700f, -220, 0f);
                    headerTitle.width = 300; // So it doesn't overlap with the two toggles in the sides.
                    body.SetActive(true);
                    body.GetComponent<UISprite>().height = 300;
                    body.GetComponent<BoxCollider>().center = new Vector3(0, -150f);
                    body.GetComponent<BoxCollider>().size = new Vector3(500, 300);
                    // Set the UIPanel clipping height a bit smaller just because NGUICRAP doesn't hide the objects until they're FULLY outside.
                    body.GetComponent<UIPanel>().clipRange = new Vector4(0f, -150f, 500, 280);

                    panelIsExpanded = false;
                }
                else // EXPANDED PANEL
                {
                    gameObject.transform.localPosition = new Vector3(-700f, 500, 0f);
                    headerTitle.width = 300; // So it doesn't overlap with the two toggles in the sides.
                    body.SetActive(true);
                    body.GetComponent<UISprite>().height = 1020;
                    body.GetComponent<BoxCollider>().center = new Vector3(0, -510f);
                    body.GetComponent<BoxCollider>().size = new Vector3(500, 1020);
                    // Set the UIPanel clipping height a bit smaller just because NGUICRAP doesn't hide the objects until they're FULLY outside.
                    body.GetComponent<UIPanel>().clipRange = new Vector4(0f, -510f, 500, 1000);

                    panelIsExpanded = true;
                }
            }
            else
            {
                gameObject.transform.localPosition = new Vector3(-700f, -505f, 0f);
                headerTitle.width = 520;
                body.SetActive(false);
                setActiveAtStartToggle.gameObject.SetActive(false);
                expandPanelButton.gameObject.SetActive(false);
                globalObjAttributesToggle.gameObject.SetActive(false);
            }

            expandPanelButtonSprite.transform.localScale = new Vector3(1f, expand ? -1 : 1, 1);

            showingPanel = show;
        }
        public void ExpandButtonClick()
        {
            ShowPanel(showingPanel, !panelIsExpanded, currentHeaderLocKey);
        }

        public void SetSelectedObjPanelAsNone()
        {
            ShowPanel(false, "selection.NoObjectSelected");
        }
        public void SetMultipleObjectsSelected()
        {
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
                executeSetActiveAtStartToggleActions = false;
                setActiveAtStartToggle.Set(false);
                executeSetActiveAtStartToggleActions = true;
                setActiveAtStartToggle.gameObject.GetChildAt("Background/Line").SetActive(true);
            }
            #endregion

            globalObjAttributesToggle.gameObject.SetActive(false);
            globalObjAttributesToggle.SetToggleState(true, true);

            UpdateGlobalObjectAttributes(EditorController.Instance.currentSelectedObj.transform);
        }
        public void SetSelectedObject(LE_Object objComponent)
        {
            currentSelectedObj = objComponent;

            // The obj name is obviously NOT a valid loc key, but that doesn't matter, NGUI will just show it as is.
            ShowPanel(true, objComponent.objectFullNameWithID);
            expandPanelButton.gameObject.SetActive(true);

            #region Select Right Attributes Panel And Setup Global Attributes Toggle
            // Disable all of the attributes panels.
            attributesPanels.ToList().ForEach(x => x.Value.SetActive(false));

            // Enable the toggle and show object-specific attributes, then it will be disabled or changed to GLOBAL attributes if the object doesn't have unique ones.
            globalObjAttributesToggle.gameObject.SetActive(true);
            globalObjAttributesToggle.SetToggleState(false, true);

            bool specificAttributesFound = false;
            // The child's name is the name of the target obj, but if it contains a '|' then that panel may be compatible for multiple objects (Like ammo & health packs).
            foreach (var child in objectSpecificPanelsParent.gameObject.GetChilds())
            {
                List<LE_Object.ObjectType?> thisPanelIsForObjects = child.name.Split('|').Select(x => LE_Object.ConvertNameToObjectType(x.Trim()))
                    .ToList();

                if (thisPanelIsForObjects.Contains(objComponent.objectType))
                {
                    specificAttributesFound = true;

                    child.SetActive(true);
                    UpdateObjectSpecificAttribute(objComponent, child);
                    break; // We already found the right panel, stop iterating.
                }
            }
            if (!specificAttributesFound)
            {
                globalObjAttributesToggle.gameObject.SetActive(false);
                globalObjAttributesToggle.SetToggleState(true, true);
            }
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
        void UpdateObjectSpecificAttribute(LE_Object objComp, GameObject panelInUI)
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
                            case int:
                            case float:
                                value = value + ""; // Convert to string.
                                break;

                            case Color:
                                value = Utils.ColorToHex((Color)value);
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

            if (objComp is LE_Saw)
            {
                var waypoints = objComp.GetProperty<List<LE_SawWaypointSerializable>>("waypoints");
                ShowOrHideSawWaitTimeField(waypoints.Count);

                if (objComp.TryGetProperty("Rotate", out object rotateValue) && rotateValue is bool rotate)
                {
                    OnSawRotateChecked(rotate);
                }
            }
        }

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
            if (!executeSetActiveAtStartToggleActions) return;

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
        public void AddWaypointForObject()
        {
            if (!EditorController.Instance.multipleObjectsSelected)
            {
                EditorController.Instance.currentSelectedObjComponent.GetComponent<WaypointSupport>().AddWaypoint();
            }
        }
        public void SetStartMovingAtStart()
        {
            SetPropertyWithToggle("StartMovingAtStart", startMovingAtStartToggle);
        }
        public void ShowGlobalObjectAttributes(bool show)
        {
            objectSpecificPanelsParent.gameObject.SetActive(!show);
            globalObjectPanelsParent.gameObject.SetActive(show);
        }
        public void UpdateGlobalObjectAttributes(Transform obj)
        {
            // UICustomInput already verifies if the user is typing on the field, if so, SetText does nothing, we don't need to worry about that.

            // Set Global Attributes...
            #region Position/Rotation/Scale Fields
            posXField.SetText(obj.position.x, 2, false);
            posYField.SetText(obj.position.y, 2, false);
            posZField.SetText(obj.position.z, 2, false);

            rotXField.SetText(obj.localEulerAngles.x, 2, false);
            rotYField.SetText(obj.localEulerAngles.y, 2, false);
            rotZField.SetText(obj.localEulerAngles.z, 2, false);

            scaleXField.SetText(obj.localScale.x, 2, false);
            scaleYField.SetText(obj.localScale.y, 2, false);
            scaleZField.SetText(obj.localScale.z, 2, false);
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

            #region Add Waypoint Button
            if (!EditorController.Instance.multipleObjectsSelected && EditorController.Instance.currentSelectedObjComponent.canHaveWaypoints)
            {
                addWaypointButton.gameObject.SetActive(true);
            }
            else
            {
                addWaypointButton.gameObject.SetActive(false);
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

        public void SetPropertyWithInput(string propertyName, UICustomInputField inputField)
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

            if (EditorController.Instance.currentSelectedObjComponent.SetProperty(propertyName, inputField.GetText()))
            {
                EditorController.Instance.levelHasBeenModified = true;
                inputField.Set(true);
            }
            else
            {
                inputField.Set(false);
            }
        }
        public void SetPropertyWithToggle(string propertyName, UIToggle toggle)
        {
            switch (propertyName)
            {
                case "InstaKill":
                    OnLaserInstaKillChecked(toggle.isChecked);
                    break;
                case "Blinking":
                    OnLaserBlinkingChecked(toggle.isChecked);
                    break;

                case "TravelBack":
                    SetSawTravelBackORLoop(toggle.isChecked, false);
                    break;
                case "Loop":
                    SetSawTravelBackORLoop(false, toggle.isChecked);
                    break;

                case "IsAuto":
                    OnDoorAutoChecked(toggle.isChecked);
                    OnDoorV2AutoChecked(toggle.isChecked);
                    break;
                case "Rotate":
                    OnSawRotateChecked(toggle.isChecked);
                    break;
            }

            if (EditorController.Instance.currentSelectedObjComponent.SetProperty(propertyName, toggle.isChecked))
            {
                EditorController.Instance.levelHasBeenModified = true;
            }
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
        void OnSawRotateChecked(bool isEnabled)
        {
            if (attributesPanels.TryGetValue("Saw", out var sawPanel))
            {
                var rotateSpeedAttr = sawPanel.GetChild("RotateSpeed");
                if (rotateSpeedAttr != null)
                {
                    rotateSpeedAttr.SetActive(isEnabled);

                    // Update the rotate speed value when showing the field
                    if (isEnabled)
                    {
                        var field = rotateSpeedAttr.GetChild("Field").GetComponent<UIInput>();
                        if (EditorController.Instance.currentSelectedObjComponent.TryGetProperty("RotateSpeed", out object value))
                        {
                            field.text = value.ToString();
                        }
                    }
                }
            }
        }
        void OnLaserInstaKillChecked(bool newState)
        {
            attributesPanels["Laser"].GetChild("Damage").SetActive(!newState);
        }
        void OnLaserBlinkingChecked(bool newState)
        {
            attributesPanels["Laser"].GetChild("OffDuration").SetActive(newState);
            attributesPanels["Laser"].GetChild("OnDuration").SetActive(newState);
        }
        void ShowOrHideSawWaitTimeField(int waypointsCount)
        {
            attributesPanels["Saw"].GetChild("WaitTime").SetActive(waypointsCount > 0);
        }
        void SetSawTravelBackORLoop(bool travelBack, bool loop)
        {
            // This is to always enable one or the other, but NEVER both of the toggles, only one or the other.
            // To avoid bugs, only change the values when at least one of the bools is true.

            if (travelBack && !loop) attributesPanels["Saw"].GetChildAt("TravelBack/Toggle").GetComponent<UIToggle>().Set(travelBack);
            if (!travelBack && loop) attributesPanels["Saw"].GetChildAt("Loop/Toggle").GetComponent<UIToggle>().Set(loop);
        }
        void OnDoorAutoChecked(bool newState)
        {
            attributesPanels["Door"].GetChild("InitialState").SetActive(!newState);
            attributesPanels["Door"].GetChild("InitialStateAuto").SetActive(newState);
        }
        void OnDoorV2AutoChecked(bool newState)
        {
            attributesPanels["Door V2"].GetChild("InitialState").SetActive(!newState);
            attributesPanels["Door V2"].GetChild("InitialStateAuto").SetActive(newState);
        }
    }
}
