using FS_LevelEditor.UI_Related;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UnityEngine;

namespace FS_LevelEditor.Editor.UI
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class SelectedObjPanel : MonoBehaviour
    {
        public static SelectedObjPanel Instance;

        GameObject header;
        UILabel headerTitle;
        public UIToggle setActiveAtStartToggle;
        UIButtonAsToggle globalObjAttributesToggle;

        GameObject body;
        Transform globalObjectPanelsParent;
        UICustomInputField posXField, posYField, posZField;
        UICustomInputField rotXField, rotYField, rotZField;
        UICustomInputField scaleXField, scaleYField, scaleZField;
        // ------------------------------
        Transform objectSpecificPanelsParent;
        Dictionary<string, GameObject> attributesPanels = new Dictionary<string, GameObject>();

        Transform whereToCreateObjAttributesParent;


        bool executeSetActiveAtStartToggleActions = true;

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

            headerTitle = NGUI_Utils.CreateLabel(header.transform, Vector3.zero, new Vector3Int(520, 60, 0), "No Object Selected", NGUIText.Alignment.Center,
                UIWidget.Pivot.Center);
            headerTitle.name = "Label";
            headerTitle.fontSize = 27;
            headerTitle.depth = 1;

            CreateSetActiveAtStartToggle();
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

            toggleObj.SetActive(false);

            GameObject line = new GameObject("Line");
            line.transform.parent = toggleObj.GetChildWithName("Background").transform;
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
            body.transform.localPosition = new Vector3(0f, -160f, 0f);
            body.transform.localScale = Vector3.one;

            UISprite sprite = body.AddComponent<UISprite>();
            sprite.atlas = NGUI_Utils.UITexturesAtlas;
            sprite.spriteName = "Square_Border_Beveled_HighOpacity";
            sprite.type = UIBasicSprite.Type.Sliced;
            sprite.color = new Color(0.0039f, 0.3568f, 0.3647f, 1f);
            sprite.depth = -1;
            sprite.width = 500;
            sprite.height = 300;

            BoxCollider collider = body.AddComponent<BoxCollider>();
            collider.size = new Vector3(500f, 300f, 1f);

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
            globalObjectOptionsParent.transform.localPosition = Vector3.zero;
            globalObjectOptionsParent.transform.localScale = Vector3.one;
            globalObjectPanelsParent = globalObjectOptionsParent.transform;
        }
        void CreateGlobalObjectAttributesPanel()
        {
            CreateObjectPositionUIElements();
            CreateObjectRotationUIElements();
            CreateObjectScaleUIElements();
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
            GameObject xField = NGUI_Utils.CreateInputField(positionThingsParent, new Vector3(10f, 90f, 0f), new Vector3Int(65, 38, 0), 27, "0");
            xField.name = "XField";
            posXField = xField.AddComponent<UICustomInputField>();
            posXField.Setup(UICustomInputField.UIInputType.FLOAT, maxDecimals: 2);
            posXField.onChange += (() => SetPropertyWithInput("XPosition", posXField));

            UILabel yTitle = NGUI_Utils.CreateLabel(positionThingsParent, new Vector3(60f, 90f, 0f), new Vector3Int(28, 38, 0), "Y", NGUIText.Alignment.Center,
                UIWidget.Pivot.Center);
            yTitle.name = "YTitle";
            // ------------------------------
            GameObject yField = NGUI_Utils.CreateInputField(positionThingsParent, new Vector3(110f, 90f, 0f), new Vector3Int(65, 38, 0), 27, "0");
            yField.name = "YField";
            posYField = yField.AddComponent<UICustomInputField>();
            posYField.Setup(UICustomInputField.UIInputType.FLOAT, maxDecimals: 2);
            posYField.onChange += (() => SetPropertyWithInput("YPosition", posYField));

            UILabel zTitle = NGUI_Utils.CreateLabel(positionThingsParent, new Vector3(160f, 90f, 0f), new Vector3Int(28, 38, 0), "Z", NGUIText.Alignment.Center,
                UIWidget.Pivot.Center);
            zTitle.name = "ZTitle";
            // ------------------------------
            GameObject zField = NGUI_Utils.CreateInputField(positionThingsParent, new Vector3(210f, 90f, 0f), new Vector3Int(65, 38, 0), 27, "0");
            zField.name = "ZField";
            posZField = zField.AddComponent<UICustomInputField>();
            posZField.Setup(UICustomInputField.UIInputType.FLOAT, maxDecimals: 2);
            posZField.onChange += (() => SetPropertyWithInput("ZPosition", posZField));
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
            GameObject xField = NGUI_Utils.CreateInputField(rotationThingsParent, new Vector3(10f, 40f, 0f), new Vector3Int(65, 38, 0), 27, "0");
            xField.name = "XField";
            rotXField = xField.AddComponent<UICustomInputField>();
            rotXField.Setup(UICustomInputField.UIInputType.FLOAT, maxDecimals: 2);
            rotXField.onChange += (() => SetPropertyWithInput("XRotation", rotXField));

            UILabel yTitle = NGUI_Utils.CreateLabel(rotationThingsParent, new Vector3(60f, 40f, 0f), new Vector3Int(28, 38, 0), "Y", NGUIText.Alignment.Center,
                UIWidget.Pivot.Center);
            yTitle.name = "YTitle";
            // ------------------------------
            GameObject yField = NGUI_Utils.CreateInputField(rotationThingsParent, new Vector3(110f, 40f, 0f), new Vector3Int(65, 38, 0), 27, "0");
            yField.name = "YField";
            rotYField = yField.AddComponent<UICustomInputField>();
            rotYField.Setup(UICustomInputField.UIInputType.FLOAT, maxDecimals: 2);
            rotYField.onChange += (() => SetPropertyWithInput("YRotation", rotYField));

            UILabel zTitle = NGUI_Utils.CreateLabel(rotationThingsParent, new Vector3(160f, 40f, 0f), new Vector3Int(28, 38, 0), "Z", NGUIText.Alignment.Center,
                UIWidget.Pivot.Center);
            zTitle.name = "ZTitle";
            // ------------------------------
            GameObject zField = NGUI_Utils.CreateInputField(rotationThingsParent, new Vector3(210f, 40f, 0f), new Vector3Int(65, 38, 0), 27, "0");
            zField.name = "ZField";
            rotZField = zField.AddComponent<UICustomInputField>();
            rotZField.Setup(UICustomInputField.UIInputType.FLOAT, maxDecimals: 2);
            rotZField.onChange += (() => SetPropertyWithInput("ZRotation", rotZField));
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
            GameObject xField = NGUI_Utils.CreateInputField(scaleThingsParent, new Vector3(10f, -10f, 0f), new Vector3Int(65, 38, 0), 27, "0");
            xField.name = "XField";
            scaleXField = xField.AddComponent<UICustomInputField>();
            scaleXField.Setup(UICustomInputField.UIInputType.FLOAT, maxDecimals: 2);
            scaleXField.onChange += (() => SetPropertyWithInput("XScale", scaleXField));

            UILabel yTitle = NGUI_Utils.CreateLabel(scaleThingsParent, new Vector3(60f, -10f, 0f), new Vector3Int(28, 38, 0), "Y", NGUIText.Alignment.Center,
                UIWidget.Pivot.Center);
            yTitle.name = "YTitle";
            // ------------------------------
            GameObject yField = NGUI_Utils.CreateInputField(scaleThingsParent, new Vector3(110f, -10f, 0f), new Vector3Int(65, 38, 0), 27, "0");
            yField.name = "YField";
            scaleYField = yField.AddComponent<UICustomInputField>();
            scaleYField.Setup(UICustomInputField.UIInputType.FLOAT, maxDecimals: 2);
            scaleYField.onChange += (() => SetPropertyWithInput("YScale", scaleYField));

            UILabel zTitle = NGUI_Utils.CreateLabel(scaleThingsParent, new Vector3(160f, -10f, 0f), new Vector3Int(28, 38, 0), "Z", NGUIText.Alignment.Center,
                UIWidget.Pivot.Center);
            zTitle.name = "ZTitle";
            // ------------------------------
            GameObject zField = NGUI_Utils.CreateInputField(scaleThingsParent, new Vector3(210f, -10f, 0f), new Vector3Int(65, 38, 0), 27, "0");
            zField.name = "ZField";
            scaleZField = zField.AddComponent<UICustomInputField>();
            scaleZField.Setup(UICustomInputField.UIInputType.FLOAT, maxDecimals: 2);
            scaleZField.onChange += (() => SetPropertyWithInput("ZScale", scaleZField));
        }
        // ------------------------------
        void CreateObjectSpecificOptionsParent()
        {
            GameObject objectSpecificOptionsParent = new GameObject("ObjectSpecificOptions");
            objectSpecificOptionsParent.transform.parent = body.transform;
            objectSpecificOptionsParent.transform.localPosition = Vector3.zero;
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
        }
        #region Create Object Specific Panels
        void CreateDirectionalLightAttributesPanel()
        {
            GameObject directionalLightAttributes = new GameObject("Directional Light");
            directionalLightAttributes.transform.parent = objectSpecificPanelsParent;
            directionalLightAttributes.transform.localPosition = Vector3.zero;
            directionalLightAttributes.transform.localScale = Vector3.one;

            SetCurrentParentToCreateAttributes(directionalLightAttributes);

            CreateObjectAttribute("Color (Hex)", AttributeType.INPUT_FIELD, "FFFFFF", UICustomInputField.UIInputType.HEX_COLOR, "Color", true);
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

            CreateObjectAttribute("Color (Hex)", AttributeType.INPUT_FIELD, "FFFFFF", UICustomInputField.UIInputType.HEX_COLOR, "Color", true);
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

            CreateObjectAttribute("Activate On Start", AttributeType.TOGGLE, true, null, "ActivateOnStart");
            CreateObjectAttribute("Damage", AttributeType.INPUT_FIELD, "50", UICustomInputField.UIInputType.NON_NEGATIVE_INT, "Damage");
            CreateObjectAttribute("+ Add Waypoint", AttributeType.BUTTON, null, null, "AddWaypoint");

            sawAttributes.SetActive(false);
            attributesPanels.Add("Saw", sawAttributes);
        }
        void CreateSawWaypointAttributesPanel()
        {
            GameObject sawWaypointAttributes = new GameObject("SawWaypointAttributes");
            sawWaypointAttributes.transform.parent = objectSpecificPanelsParent;
            sawWaypointAttributes.transform.localPosition = Vector3.zero;
            sawWaypointAttributes.transform.localScale = Vector3.one;

            #region Wait Time Input Field
            UILabel waitTimeTitle = NGUI_Utils.CreateLabel(sawWaypointAttributes.transform, new Vector3(-230, 90), new Vector3Int(260, NGUI_Utils.defaultLabelSize.y, 0),
                "Wait Time");
            waitTimeTitle.name = "WaitTimeTitle";
            waitTimeTitle.color = Color.white;

            GameObject waitTimeInputField = NGUI_Utils.CreateInputField(sawWaypointAttributes.transform, new Vector3(140f, 90f, 0f), new Vector3Int(200, 38, 0), 27,
                "0.3", false, NGUIText.Alignment.Left);
            waitTimeInputField.name = "WaitTimeInputField";
            var waitTimeFieldCustomScript = waitTimeInputField.AddComponent<UICustomInputField>();
            waitTimeFieldCustomScript.Setup(UICustomInputField.UIInputType.NON_NEGATIVE_FLOAT);
            waitTimeFieldCustomScript.onChange += (() => SetPropertyWithInput("WaitTime", waitTimeFieldCustomScript));
            #endregion

            #region Add Waypoint
            UIButtonPatcher addWaypoint = NGUI_Utils.CreateButton(sawWaypointAttributes.transform, new Vector3(0f, 35f, 0f), new Vector3Int(480, 55, 0), "+ Add Waypoint");
            addWaypoint.name = "AddWaypointButton";
            addWaypoint.onClick += () => TriggerAction("AddWaypoint");
            addWaypoint.GetComponent<UIButtonScale>().hover = Vector3.one * 1.05f;
            addWaypoint.GetComponent<UIButtonScale>().pressed = Vector3.one * 1.02f;
            #endregion

            sawWaypointAttributes.SetActive(false);
            attributesPanels.Add("SawWaypoint", sawWaypointAttributes);
        }
        void CreateSwitchAttributesPanel()
        {
            GameObject switchAttributes = new GameObject("SwitchAttributes");
            switchAttributes.transform.parent = objectSpecificPanelsParent;
            switchAttributes.transform.localPosition = Vector3.zero;
            switchAttributes.transform.localScale = Vector3.one;

            #region Usable Once Toggle
            UILabel usableOnceTitle = NGUI_Utils.CreateLabel(switchAttributes.transform, new Vector3(-230, 90), new Vector3Int(395, NGUI_Utils.defaultLabelSize.y, 0),
                "Usable Once");
            usableOnceTitle.name = "UsableOnceTitle";
            usableOnceTitle.color = Color.white;

            GameObject usableOnceToggle = NGUI_Utils.CreateToggle(switchAttributes.transform, new Vector3(200f, 90f, 0f), new Vector3Int(48, 48, 0));
            usableOnceToggle.name = "UsableOnceToggle";
            usableOnceToggle.GetComponent<UIToggle>().onChange.Clear();
            var usableOnceDelegate = NGUI_Utils.CreateEvenDelegate(this, nameof(SetPropertyWithToggle),
                NGUI_Utils.CreateEventDelegateParamter(this, "propertyName", "UsableOnce"),
                NGUI_Utils.CreateEventDelegateParamter(this, "toggle", usableOnceToggle.GetComponent<UIToggle>()));
            usableOnceToggle.GetComponent<UIToggle>().onChange.Add(usableOnceDelegate);
            #endregion

            #region Can Use Taser Toggle
            UILabel canUseTaserTitle = NGUI_Utils.CreateLabel(switchAttributes.transform, new Vector3(-230, 35), new Vector3Int(395, NGUI_Utils.defaultLabelSize.y, 0),
                "Can be shot by Taser");
            canUseTaserTitle.name = "CanUseTaserTitle";
            canUseTaserTitle.color = Color.white;

            GameObject canUseTaserToggle = NGUI_Utils.CreateToggle(switchAttributes.transform, new Vector3(200f, 35f, 0f), new Vector3Int(48, 48, 0));
            canUseTaserToggle.name = "CanUseTaserToggle";
            canUseTaserToggle.GetComponent<UIToggle>().onChange.Clear();
            var canUseTaserDelegate = NGUI_Utils.CreateEvenDelegate(this, nameof(SetPropertyWithToggle),
                NGUI_Utils.CreateEventDelegateParamter(this, "propertyName", "CanUseTaser"),
                NGUI_Utils.CreateEventDelegateParamter(this, "toggle", canUseTaserToggle.GetComponent<UIToggle>()));
            canUseTaserToggle.GetComponent<UIToggle>().onChange.Add(canUseTaserDelegate);
            #endregion

            #region Manage Events
            UIButtonPatcher manageEvents = NGUI_Utils.CreateButton(switchAttributes.transform, new Vector3(0f, -20f, 0f), new Vector3Int(480, 55, 0), "Manage Events");
            manageEvents.name = "ManageEventsButton";
            manageEvents.onClick += () => TriggerAction("ManageEvents");
            manageEvents.GetComponent<UIButtonScale>().hover = Vector3.one * 1.05f;
            manageEvents.GetComponent<UIButtonScale>().pressed = Vector3.one * 1.02f;
            #endregion

            switchAttributes.SetActive(false);
            attributesPanels.Add("Switch", switchAttributes);
        }
        void CreateAmmoAndHealthPackAttributesPanel()
        {
            GameObject ammoHealthAttributes = new GameObject("AmmoAndHealthPackAttributes");
            ammoHealthAttributes.transform.parent = objectSpecificPanelsParent;
            ammoHealthAttributes.transform.localPosition = Vector3.zero;
            ammoHealthAttributes.transform.localScale = Vector3.one;

            #region Respawn Time Input Field
            UILabel respawnTitle = NGUI_Utils.CreateLabel(ammoHealthAttributes.transform, new Vector3(-230, 90), new Vector3Int(260, NGUI_Utils.defaultLabelSize.y, 0),
                "Respawn Title");
            respawnTitle.name = "RespawnTitle";
            respawnTitle.color = Color.white;

            GameObject respawnInputField = NGUI_Utils.CreateInputField(ammoHealthAttributes.transform, new Vector3(140f, 90f, 0f), new Vector3Int(200, 38, 0), 27,
                "50", false, NGUIText.Alignment.Left);
            respawnInputField.name = "RespawnInputField";
            var respawnFieldCustomScript = respawnInputField.AddComponent<UICustomInputField>();
            respawnFieldCustomScript.Setup(UICustomInputField.UIInputType.NON_NEGATIVE_FLOAT);
            respawnFieldCustomScript.onChange += (() => SetPropertyWithInput("RespawnTime", respawnFieldCustomScript));
            #endregion

            ammoHealthAttributes.SetActive(false);
            attributesPanels.Add("AmmoAndHealth", ammoHealthAttributes);
        }
        void CreateLaserAttributesPanel()
        {
            GameObject laserAttributes = new GameObject("LaserAttributes");
            laserAttributes.transform.parent = objectSpecificPanelsParent;
            laserAttributes.transform.localPosition = Vector3.zero;
            laserAttributes.transform.localScale = Vector3.one;

            #region Activate On Start Toggle
            UILabel activateOnStartTitle = NGUI_Utils.CreateLabel(laserAttributes.transform, new Vector3(-230, 90), new Vector3Int(395, NGUI_Utils.defaultLabelSize.y, 0),
                "Activate On Start");
            activateOnStartTitle.name = "ActivateOnStartTitle";
            activateOnStartTitle.color = Color.white;

            GameObject activateOnStartToggle = NGUI_Utils.CreateToggle(laserAttributes.transform, new Vector3(200f, 90f, 0f), new Vector3Int(48, 48, 0));
            activateOnStartToggle.name = "ActivateOnStartToggle";
            activateOnStartToggle.GetComponent<UIToggle>().onChange.Clear();
            var activateOnStartDelegate = NGUI_Utils.CreateEvenDelegate(this, nameof(SetPropertyWithToggle),
                NGUI_Utils.CreateEventDelegateParamter(this, "propertyName", "ActivateOnStart"),
                NGUI_Utils.CreateEventDelegateParamter(this, "toggle", activateOnStartToggle.GetComponent<UIToggle>()));
            activateOnStartToggle.GetComponent<UIToggle>().onChange.Add(activateOnStartDelegate);
            #endregion

            #region Insta Kill Toggle
            UILabel instaKillTitle = NGUI_Utils.CreateLabel(laserAttributes.transform, new Vector3(-230, 40), new Vector3Int(395, NGUI_Utils.defaultLabelSize.y, 0),
                "Instant Kill");
            instaKillTitle.name = "InstaKillTitle";
            instaKillTitle.color = Color.white;

            GameObject instaKillToggle = NGUI_Utils.CreateToggle(laserAttributes.transform, new Vector3(200f, 40f, 0f), new Vector3Int(48, 48, 0));
            instaKillToggle.name = "InstaKillToggle";
            instaKillToggle.GetComponent<UIToggle>().onChange.Clear();
            var instaKillDelegate = NGUI_Utils.CreateEvenDelegate(this, nameof(SetPropertyWithToggle),
                NGUI_Utils.CreateEventDelegateParamter(this, "propertyName", "InstaKill"),
                NGUI_Utils.CreateEventDelegateParamter(this, "toggle", instaKillToggle.GetComponent<UIToggle>()));
            instaKillToggle.GetComponent<UIToggle>().onChange.Add(instaKillDelegate);
            #endregion

            #region Damage Input Field
            UILabel damageTitle = NGUI_Utils.CreateLabel(laserAttributes.transform, new Vector3(-230, -10), new Vector3Int(260, NGUI_Utils.defaultLabelSize.y, 0),
                "Damage");
            damageTitle.name = "DamageTitle";
            damageTitle.color = Color.white;

            GameObject damageInputField = NGUI_Utils.CreateInputField(laserAttributes.transform, new Vector3(140f, -10f, 0f), new Vector3Int(200, 38, 0), 27,
                "34", false, NGUIText.Alignment.Left);
            damageInputField.name = "DamageInputField";
            var damageFieldCustomScript = damageInputField.AddComponent<UICustomInputField>();
            damageFieldCustomScript.Setup(UICustomInputField.UIInputType.NON_NEGATIVE_FLOAT);
            damageFieldCustomScript.onChange += (() => SetPropertyWithInput("Damage", damageFieldCustomScript));
            #endregion

            laserAttributes.SetActive(false);
            attributesPanels.Add("Laser", laserAttributes);
        }
        void CreateCeilingLightPanel()
        {
            GameObject ceilingLightAttributes = new GameObject("CeilingLightAttributes");
            ceilingLightAttributes.transform.parent = objectSpecificPanelsParent;
            ceilingLightAttributes.transform.localPosition = Vector3.zero;
            ceilingLightAttributes.transform.localScale = Vector3.one;

            #region Activate On Start Toggle
            UILabel activateOnStartTitle = NGUI_Utils.CreateLabel(ceilingLightAttributes.transform, new Vector3(-230, 90), new Vector3Int(395, NGUI_Utils.defaultLabelSize.y, 0),
                "Activate On Start");
            activateOnStartTitle.name = "ActivateOnStartTitle";
            activateOnStartTitle.color = Color.white;

            GameObject activateOnStartToggle = NGUI_Utils.CreateToggle(ceilingLightAttributes.transform, new Vector3(200f, 90f, 0f), new Vector3Int(48, 48, 0));
            activateOnStartToggle.name = "ActivateOnStartToggle";
            activateOnStartToggle.GetComponent<UIToggle>().onChange.Clear();
            var activateOnStartDelegate = NGUI_Utils.CreateEvenDelegate(this, nameof(SetPropertyWithToggle),
                NGUI_Utils.CreateEventDelegateParamter(this, "propertyName", "ActivateOnStart"),
                NGUI_Utils.CreateEventDelegateParamter(this, "toggle", activateOnStartToggle.GetComponent<UIToggle>()));
            activateOnStartToggle.GetComponent<UIToggle>().onChange.Add(activateOnStartDelegate);
            #endregion

            #region Color Input Field
            UILabel colorTitle = NGUI_Utils.CreateLabel(ceilingLightAttributes.transform, new Vector3(-230, 40), new Vector3Int(235, NGUI_Utils.defaultLabelSize.y, 0),
                "Color (Hex)");
            colorTitle.name = "ColorTitle";
            colorTitle.color = Color.white;

            UILabel hashtagLOL = NGUI_Utils.CreateLabel(ceilingLightAttributes.transform, new Vector3(15, 40), new Vector3Int(20, NGUI_Utils.defaultLabelSize.y, 0), "#");
            hashtagLOL.name = "HashtagLOL";
            hashtagLOL.color = Color.white;

            GameObject colorInputField = NGUI_Utils.CreateInputField(ceilingLightAttributes.transform, new Vector3(140f, 40f, 0f), new Vector3Int(200, 38, 0), 27,
                "FFFFFF", false, NGUIText.Alignment.Left);
            colorInputField.name = "ColorField";
            var colorFieldCustomScript = colorInputField.AddComponent<UICustomInputField>();
            colorFieldCustomScript.Setup(UICustomInputField.UIInputType.HEX_COLOR);
            colorFieldCustomScript.onChange += (() => SetPropertyWithInput("Color", colorFieldCustomScript));
            #endregion

            ceilingLightAttributes.SetActive(false);
            attributesPanels.Add("Ceiling Light", ceilingLightAttributes);
        }
        void CreateFlameTrapAttributesPanel()
        {
            GameObject flameTrapAttributes = new GameObject("FlameTrapAttributes");
            flameTrapAttributes.transform.parent = objectSpecificPanelsParent;
            flameTrapAttributes.transform.localPosition = Vector3.zero;
            flameTrapAttributes.transform.localScale = Vector3.one;

            #region Activate On Start Toggle
            UILabel activateOnStartTitle = NGUI_Utils.CreateLabel(flameTrapAttributes.transform, new Vector3(-230, 90), new Vector3Int(395, NGUI_Utils.defaultLabelSize.y, 0),
                "Activate On Start");
            activateOnStartTitle.name = "ActivateOnStartTitle";
            activateOnStartTitle.color = Color.white;

            GameObject activateOnStartToggle = NGUI_Utils.CreateToggle(flameTrapAttributes.transform, new Vector3(200f, 90f, 0f), new Vector3Int(48, 48, 0));
            activateOnStartToggle.name = "ActivateOnStartToggle";
            activateOnStartToggle.GetComponent<UIToggle>().onChange.Clear();
            var activateOnStartDelegate = NGUI_Utils.CreateEvenDelegate(this, nameof(SetPropertyWithToggle),
                NGUI_Utils.CreateEventDelegateParamter(this, "propertyName", "ActivateOnStart"),
                NGUI_Utils.CreateEventDelegateParamter(this, "toggle", activateOnStartToggle.GetComponent<UIToggle>()));
            activateOnStartToggle.GetComponent<UIToggle>().onChange.Add(activateOnStartDelegate);
            #endregion

            #region Constant Toggle
            UILabel constantTitle = NGUI_Utils.CreateLabel(flameTrapAttributes.transform, new Vector3(-230, 40), new Vector3Int(395, NGUI_Utils.defaultLabelSize.y, 0),
                "Constant");
            constantTitle.name = "ConstantTitle";
            constantTitle.color = Color.white;

            GameObject constantToggle = NGUI_Utils.CreateToggle(flameTrapAttributes.transform, new Vector3(200f, 40f, 0f), new Vector3Int(48, 48, 0));
            constantToggle.name = "ConstantToggle";
            constantToggle.GetComponent<UIToggle>().onChange.Clear();
            var constantDelegate = NGUI_Utils.CreateEvenDelegate(this, nameof(SetPropertyWithToggle),
                NGUI_Utils.CreateEventDelegateParamter(this, "propertyName", "Constant"),
                NGUI_Utils.CreateEventDelegateParamter(this, "toggle", constantToggle.GetComponent<UIToggle>()));
            constantToggle.GetComponent<UIToggle>().onChange.Add(constantDelegate);
            #endregion

            flameTrapAttributes.SetActive(false);
            attributesPanels.Add("Flame Trap", flameTrapAttributes);
        }

        enum AttributeType { TOGGLE, INPUT_FIELD, BUTTON }
        void SetCurrentParentToCreateAttributes(GameObject newParent)
        {
            whereToCreateObjAttributesParent = newParent.transform;
        }
        void CreateObjectAttribute(string text, AttributeType attrType, object defaultValue, UICustomInputField.UIInputType? fieldType, string targetPropName, bool createHastag = false)
        {
            GameObject attributeParent = new GameObject(targetPropName);
            attributeParent.transform.parent = whereToCreateObjAttributesParent;
            attributeParent.transform.localPosition = Vector3.zero;
            attributeParent.transform.localScale = Vector3.one;

            float yPos = 90 - (50 * (whereToCreateObjAttributesParent.childCount - 1));

            if (attrType != AttributeType.BUTTON)
            {
                int titleWidth = attrType == AttributeType.INPUT_FIELD ? 260 : 395;
                if (createHastag) titleWidth = 235;
                UILabel title = NGUI_Utils.CreateLabel(attributeParent.transform, new Vector3(-230, yPos), new Vector3Int(titleWidth, NGUI_Utils.defaultLabelSize.y, 0),
                    text);
                title.name = "Title";
            }

            if (createHastag && attrType == AttributeType.INPUT_FIELD)
            {
                UILabel hashtagLOL = NGUI_Utils.CreateLabel(attributeParent.transform, new Vector3(15, yPos - 5), new Vector3Int(20, NGUI_Utils.defaultLabelSize.y, 0), "#",
                    NGUIText.Alignment.Center, UIWidget.Pivot.Left);
                hashtagLOL.name = "HashtagLOL";
                hashtagLOL.color = Color.white;
            }

            if (attrType == AttributeType.INPUT_FIELD)
            {
                GameObject field = NGUI_Utils.CreateInputField(attributeParent.transform, new Vector3(140, yPos), new Vector3Int(200, 38, 0), 27, (string)defaultValue, false);
                field.name = "Field";
                var fieldScript = field.AddComponent<UICustomInputField>();
                fieldScript.Setup((UICustomInputField.UIInputType)fieldType);
                fieldScript.setFieldColorAutomatically = false;
                fieldScript.onChange += () => SetPropertyWithInput(targetPropName, fieldScript);
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
            }
            else if (attrType == AttributeType.BUTTON)
            {
                UIButtonPatcher button = NGUI_Utils.CreateButton(attributeParent.transform, new Vector3(0, yPos), new Vector3Int(480, 55, 0), text);
                button.name = "Button";
                button.onClick += () => TriggerAction(targetPropName);
                button.GetComponent<UIButtonScale>().hover = Vector3.one * 1.05f;
                button.GetComponent<UIButtonScale>().pressed = Vector3.one * 1.02f;
            }
        }
        #endregion
        #endregion

        public void SetSelectedObjPanelAsNone()
        {
            headerTitle.text = "No Object Selected";
            setActiveAtStartToggle.gameObject.SetActive(false);
            globalObjAttributesToggle.gameObject.SetActive(false);
            body.SetActive(false);
            gameObject.transform.localPosition = new Vector3(-700f, -505f, 0f);
        }
        public void SetMultipleObjectsSelected()
        {
            headerTitle.text = "Multiple Objects Selected";
            body.SetActive(true);
            gameObject.transform.localPosition = new Vector3(-700f, -220f, 0f);

            setActiveAtStartToggle.gameObject.SetActive(true);
            // If this is null, that means the "Set Active At Start" in the current selected objects is different in at least one of them.
            // If it's true or false, then ALL of them are true or false.
            bool? setActiveStateInObjects = null;
            foreach (var obj in EditorController.Instance.currentSelectedObjects)
            {
                LE_Object comp = obj.GetComponent<LE_Object>();
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

            globalObjAttributesToggle.gameObject.SetActive(false);
            globalObjAttributesToggle.SetToggleState(true, true);

            UpdateGlobalObjectAttributes(EditorController.Instance.currentSelectedObj.transform);
        }
        public void SetSelectedObject(LE_Object objComponent)
        {
            headerTitle.text = objComponent.objectFullNameWithID;
            body.SetActive(true);
            gameObject.transform.localPosition = new Vector3(-700f, -220, 0f);

            attributesPanels.ToList().ForEach(x => x.Value.SetActive(false));

            // Enable the toggle and show object-specific attributes, then it will be disabled or changed to GLOBAL attributes if the object doesn't have unique ones.
            globalObjAttributesToggle.gameObject.SetActive(true);
            globalObjAttributesToggle.SetToggleState(false, true);

            if (objComponent.objectOriginalName == "Directional Light")
            {
                attributesPanels["Directional Light"].SetActive(true);

                UpdateObjectSpecificAttribute("Directional Light", "Color", Utilities.ColorToHex(objComponent.GetProperty<Color>("Color")));
                UpdateObjectSpecificAttribute("Directional Light", "Intensity", objComponent.GetProperty<float>("Intensity") + "");
            }
            else if (objComponent.objectOriginalName == "Point Light")
            {
                attributesPanels["Point Light"].SetActive(true);

                UpdateObjectSpecificAttribute("Point Light", "Color", Utilities.ColorToHex(objComponent.GetProperty<Color>("Color")));
                UpdateObjectSpecificAttribute("Point Light", "Intensity", objComponent.GetProperty<float>("Intensity") + "");
                UpdateObjectSpecificAttribute("Point Light", "Range", objComponent.GetProperty<float>("Range") + "");
            }
            else if (objComponent.objectOriginalName == "Saw")
            {
                attributesPanels["Saw"].SetActive(true);

                UpdateObjectSpecificAttribute("Saw", "ActivateOnStart", objComponent.GetProperty<bool>("ActivateOnStart"));
                UpdateObjectSpecificAttribute("Saw", "Damage", objComponent.GetProperty<int>("Damage") + "");
            }
            else if (objComponent.objectOriginalName == "Saw Waypoint")
            {
                attributesPanels["SawWaypoint"].SetActive(true);

                // Set wait time input field...
                var waitTimeInputField = attributesPanels["SawWaypoint"].GetChildWithName("WaitTimeInputField").GetComponent<UIInput>();
                waitTimeInputField.text = (float)objComponent.GetProperty("WaitTime") + "";
            }
            else if (objComponent.objectOriginalName == "Switch")
            {
                attributesPanels["Switch"].SetActive(true);

                // Set usable once toggle...
                var usableOnceToggle = attributesPanels["Switch"].GetChildWithName("UsableOnceToggle").GetComponent<UIToggle>();
                usableOnceToggle.Set((bool)objComponent.GetProperty("UsableOnce"));

                // Set can use taser toggle...
                var canUseTaserToggle = attributesPanels["Switch"].GetChildWithName("CanUseTaserToggle").GetComponent<UIToggle>();
                canUseTaserToggle.Set((bool)objComponent.GetProperty("CanUseTaser"));
            }
            else if (objComponent.objectOriginalName == "Ammo Pack" || objComponent.objectOriginalName == "Health Pack")
            {
                attributesPanels["AmmoAndHealth"].SetActive(true);

                var respawnTimeInputField = attributesPanels["AmmoAndHealth"].GetChildWithName("RespawnInputField").GetComponent<UIInput>();
                respawnTimeInputField.text = (float)objComponent.GetProperty("RespawnTime") + "";
            }
            else if (objComponent.objectOriginalName == "Laser")
            {
                attributesPanels["Laser"].SetActive(true);

                // Set activate on start toggle...
                var activateOnStartToggle = attributesPanels["Laser"].GetChildWithName("ActivateOnStartToggle").GetComponent<UIToggle>();
                activateOnStartToggle.Set((bool)objComponent.GetProperty("ActivateOnStart"));

                // Set insta-kill toggle...
                var instaKillToggle = attributesPanels["Laser"].GetChildWithName("InstaKillToggle").GetComponent<UIToggle>();
                instaKillToggle.Set((bool)objComponent.GetProperty("InstaKill"));

                // Set the damage input field...
                var damageInput = attributesPanels["Laser"].GetChildWithName("DamageInputField").GetComponent<UIInput>();
                damageInput.text = (int)objComponent.GetProperty("Damage") + "";
            }
            else if (objComponent.objectOriginalName == "Ceiling Light")
            {
                attributesPanels["Ceiling Light"].SetActive(true);

                // Set activate on start toggle...
                var activateOnStartToggle = attributesPanels["Ceiling Light"].GetChildWithName("ActivateOnStartToggle").GetComponent<UIToggle>();
                activateOnStartToggle.Set((bool)objComponent.GetProperty("ActivateOnStart"));

                // Set color input...
                var colorInput = attributesPanels["Ceiling Light"].GetChildWithName("ColorField").GetComponent<UIInput>();
                colorInput.text = Utilities.ColorToHex((Color)objComponent.GetProperty("Color"));
            }
            else if (objComponent.objectOriginalName == "Flame Trap")
            {
                attributesPanels["Flame Trap"].SetActive(true);

                // Set activate on start toggle...
                var activateOnStartToggle = attributesPanels["Flame Trap"].GetChildWithName("ActivateOnStartToggle").GetComponent<UIToggle>();
                activateOnStartToggle.Set((bool)objComponent.GetProperty("ActivateOnStart"));

                // Set constant toggle...
                var constantToggle = attributesPanels["Flame Trap"].GetChildWithName("ConstantToggle").GetComponent<UIToggle>();
                constantToggle.Set((bool)objComponent.GetProperty("Constant"));
            }
            else
            {
                globalObjAttributesToggle.gameObject.SetActive(false);
                globalObjAttributesToggle.SetToggleState(true, true);
            }

            UpdateGlobalObjectAttributes(objComponent.transform);

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
        }
        void UpdateObjectSpecificAttribute(string objName, string attrName, object value)
        {
            GameObject attrParent = objectSpecificPanelsParent.gameObject.GetChildAt($"{objName}/{attrName}");
            if (attrParent.ExistsChildWithName("Field"))
            {
                attrParent.GetChildWithName("Field").GetComponent<UICustomInputField>().SetText((string)value);
            }
            else if (attrParent.ExistsChildWithName("Toggle"))
            {
                attrParent.GetChildWithName("Toggle").GetComponent<UIToggle>().Set((bool)value);
            }
        }

        public void SetSetActiveAtStart()
        {
            if (!executeSetActiveAtStartToggleActions) return;

            if (EditorController.Instance.multipleObjectsSelected)
            {
                gameObject.GetChildAt("SetActiveAtStartToggle/Background/Line").SetActive(false);
                foreach (var obj in EditorController.Instance.currentSelectedObjects)
                {
                    LE_Object comp = obj.GetComponent<LE_Object>();
                    comp.setActiveAtStart = setActiveAtStartToggle.isChecked;
                }
            }
            else
            {
                EditorController.Instance.currentSelectedObjComponent.setActiveAtStart = setActiveAtStartToggle.isChecked;
            }
            EditorController.Instance.levelHasBeenModified = true;
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
            posXField.SetText(obj.position.x, 2);
            posYField.SetText(obj.position.y, 2);
            posZField.SetText(obj.position.z, 2);

            rotXField.SetText(obj.localEulerAngles.x, 2);
            rotYField.SetText(obj.localEulerAngles.y, 2);
            rotZField.SetText(obj.localEulerAngles.z, 2);

            scaleXField.SetText(obj.localScale.x, 2);
            scaleYField.SetText(obj.localScale.y, 2);
            scaleZField.SetText(obj.localScale.z, 2);
        }

        public void SetPropertyWithInput(string propertyName, UICustomInputField inputField)
        {
            // Even if the input only accepts numbers and decimals, check if it CAN be converted to float anyways, what if the text is just a "-"!?
            if ((propertyName.Contains("Position") || propertyName.Contains("Rotation") || propertyName.Contains("Scale")) &&
                Utilities.TryParseFloat(inputField.GetText(), out float floatValue))
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
            }

            if (EditorController.Instance.currentSelectedObjComponent.SetProperty(propertyName, toggle.isChecked))
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
        void OnLaserInstaKillChecked(bool newState)
        {
            attributesPanels["Laser"].GetChildWithName("DamageTitle").SetActive(!newState);
            attributesPanels["Laser"].GetChildWithName("DamageInputField").SetActive(!newState);
        }
    }
}
