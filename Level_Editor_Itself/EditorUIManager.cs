using FS_LevelEditor.UI_Related;
using Il2Cpp;
using Il2CppInControl.NativeDeviceProfiles;
using Il2CppVLB;
using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using static Il2Cpp.UIAtlas;
using static Il2CppSystem.Linq.Expressions.Interpreter.CastInstruction.CastInstructionNoT;

namespace FS_LevelEditor
{
    public enum EditorUIContext
    {
        NORMAL,
        HELP_PANEL,
        EVENTS_PANEL,
        SELECTING_TARGET_OBJ,
        GLOBAL_PROPERTIES
    }

    [RegisterTypeInIl2Cpp]
    public class EditorUIManager : MonoBehaviour
    {
        public static EditorUIManager Instance;

        public GameObject editorUIParent;

        EditorUIContext previousUIContext;
        EditorUIContext currentUIContext;

        // This is for the top buttons, like "Structures", "Decorations", "System", etc.
        public List<GameObject> categoryButtons = new List<GameObject>();
        public GameObject categoryButtonsParent;
        bool categoryButtonsAreHidden = false;

        // For the object buttons.
        public GameObject currentCategoryBG;
        List<GameObject> currentCategoryButtons = new List<GameObject>();

        // Current Selected Object Panel related:
        public GameObject selectedObjPanel;
        Transform objectSpecificPanelsParent;
        Transform globalObjectPanelsParent;
        UIButtonAsToggle globalObjAttributesToggle;
        Dictionary<string, Transform> globalAttributesList = new Dictionary<string, Transform>();
        Dictionary<string, GameObject> attrbutesPanels = new Dictionary<string, GameObject>();
        public UIToggle setActiveAtStartToggle;
        bool executeSetActiveAtStartToggleActions = true;

        UILabel savingLevelLabel;
        UILabel savingLevelLabelInPauseMenu;
        Coroutine savingLevelLabelRoutine;

        public UILabel currentModeLabel;

        GameObject onExitPopupBackButton;
        GameObject onExitPopupSaveAndExitButton;
        GameObject onExitPopupExitButton;
        bool exitPopupEnabled = false;

        public GameObject helpPanel;
        GameObject globalPropertiesPanel;

        GameObject hittenTargetObjPanel;
        UILabel hittenTargetObjLabel;

        // Misc
        GameObject occluderForWhenPaused;
        public GameObject pauseMenu;
        public GameObject navigation;
        GameObject popup;
        PopupController popupController;
        GameObject popupTitle;
        GameObject popupContentLabel;
        GameObject popupSmallButtonsParent;

        public EditorUIManager(IntPtr ptr) : base(ptr) { }

        void Awake()
        {
            Instance = this;

            TranslationsManager.Init();
        }

        void Start()
        {
            SetupEditorUI();

            Invoke("ForceEnableFirstCategory", 0.1f);
        }

        void ForceEnableFirstCategory()
        {
            // For some fucking reason the code enables the content in the SECOND category, I need to force it... damn it.
            EditorController.Instance.ChangeCategory(0);
        }

        void Update()
        {
            // For some reason the occluder sometimes is disabled, so I need to force it to be enabled EVERYTIME.
            occluderForWhenPaused.SetActive(EditorController.IsCurrentState(EditorState.PAUSED));

            if (hittenTargetObjPanel)
            {
                hittenTargetObjPanel.SetActive(!EditorCameraMovement.isRotatingCamera && IsCurrentUIContext(EditorUIContext.SELECTING_TARGET_OBJ));
            }
        }

        void SetupEditorUI()
        {
            GetReferences();

            // Disable Menu UI elements.
            pauseMenu.SetActive(false);
            navigation.SetActive(false);

            SetupPauseWhenInEditor();

            SetupObjectsCategories();
            CreateObjectsBackground();
            SetupCurrentCategoryButtons();
            CreateSelectedObjPanel();
            CreateSavingLevelLabel();
            CreateCurrentModeLabel();
            CreateHelpPanel();
            CreateGlobalPropertiesPanel();

            EventsUIPageManager.Create();

            CreateHittenTargetObjPanel();

            // To fix the bug where sometimes the LE UI elements are "covered" by an object if it's too close to the editor camera, set the depth HIGHER.
            GameObject.Find("MainMenu/Camera").GetComponent<Camera>().depth = 12;
        }

        void GetReferences()
        {
            GameObject uiParentObj = GameObject.Find("MainMenu/Camera/Holder/");

            occluderForWhenPaused = uiParentObj.GetChildWithName("Occluder");
            pauseMenu = uiParentObj.GetChildWithName("Main");
            navigation = uiParentObj.GetChildWithName("Navigation");
            popup = uiParentObj.GetChildWithName("Popup");
            popupController = popup.GetComponent<PopupController>();
            popupTitle = popup.GetChildAt("PopupHolder/Title/Label");
            popupContentLabel = popup.GetChildAt("PopupHolder/Content/Label");
            popupSmallButtonsParent = popup.GetChildAt("PopupHolder/SmallButtons");
        }


        void SetupObjectsCategories()
        {
            editorUIParent = new GameObject("LevelEditor");
            editorUIParent.transform.parent = GameObject.Find("MainMenu/Camera/Holder").transform;
            editorUIParent.transform.localScale = Vector3.one;

            // Setup the category buttons parent and add a panel to it so I can modify the alpha of the whole buttons inside of it with just one panel.
            categoryButtonsParent = new GameObject("CategoryButtons");
            categoryButtonsParent.transform.parent = editorUIParent.transform;
            categoryButtonsParent.transform.localPosition = Vector3.zero;
            categoryButtonsParent.transform.localScale = Vector3.one;
            categoryButtonsParent.layer = LayerMask.NameToLayer("2D GUI");
            categoryButtonsParent.AddComponent<UIPanel>();

            GameObject buttonTemplate = GameObject.Find("MainMenu/Camera/Holder/TaserCustomization/Holder/Tabs/1_Taser");

            for (int i = 0; i < EditorController.Instance.categoriesNames.Count; i++)
            {
                string category = EditorController.Instance.categoriesNames[i];

                GameObject categoryButton = Instantiate(buttonTemplate, categoryButtonsParent.transform);
                categoryButton.name = $"{category}_Button";
                categoryButton.transform.localPosition = new Vector3(-800f + (250f * i), 450f, 0f);
                Destroy(categoryButton.GetChildWithName("Label").GetComponent<UILocalize>());
                categoryButton.GetChildWithName("Label").GetComponent<UILabel>().text = category;

                categoryButton.GetComponent<UIToggle>().onChange.Clear();
                categoryButton.GetComponent<UIToggle>().Set(false);

                EventDelegate onChange = new EventDelegate(EditorController.Instance, nameof(EditorController.ChangeCategory));
                EventDelegate.Parameter parameter = new EventDelegate.Parameter
                {
                    field = "categoryID",
                    value = i,
                    obj = EditorController.Instance
                };
                onChange.mParameters = new EventDelegate.Parameter[] { parameter };
                categoryButton.GetComponent<UIToggle>().onChange.Add(onChange);

                categoryButton.SetActive(true);

                categoryButtons.Add(categoryButton);
            }


            categoryButtons[0].GetComponent<UIToggle>().Set(true);
        }

        void CreateObjectsBackground()
        {
            GameObject template = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Background");

            currentCategoryBG = new GameObject("CategoryObjectsBackground");
            currentCategoryBG.transform.parent = editorUIParent.transform;
            currentCategoryBG.transform.localPosition = new Vector3(0f, 330f, 0f);
            currentCategoryBG.transform.localScale = Vector3.one;
            currentCategoryBG.layer = LayerMask.NameToLayer("2D GUI");
            currentCategoryBG.AddComponent<UIPanel>();

            UISprite bgSprite = currentCategoryBG.AddComponent<UISprite>();
            bgSprite.atlas = template.GetComponent<UISprite>().atlas;
            bgSprite.spriteName = "Square_Border_Beveled_HighOpacity";
            bgSprite.type = UIBasicSprite.Type.Sliced;
            bgSprite.color = new Color(0.218f, 0.6464f, 0.6509f, 1f);
            bgSprite.width = 1800;
            bgSprite.height = 150;

            BoxCollider collider = currentCategoryBG.AddComponent<BoxCollider>();
            collider.size = new Vector3(1800f, 150f, 1f);
        }

        public void SetupCurrentCategoryButtons()
        {
            GameObject template = GameObject.Find("MainMenu/Camera/Holder/TaserCustomization/Holder/ColorSelection/ColorSwatch");

            // Delete the old buttons.
            currentCategoryButtons.Clear();
            currentCategoryBG.DeleteAllChildren();

            for (int i = 0; i < EditorController.Instance.allCategoriesObjectsSorted[EditorController.Instance.currentCategoryID].Count; i++)
            {
                // Get the object.
                var currentCategoryObj = EditorController.Instance.allCategoriesObjectsSorted[EditorController.Instance.currentCategoryID].ToList()[i];

                // Setup the position, scale and disable the selected ui object just in case.
                GameObject currentCategoryButton = Instantiate(template, currentCategoryBG.transform);
                currentCategoryButton.name = currentCategoryObj.Key;
                currentCategoryButton.transform.localPosition = new Vector3(-800 + (150f * i), -25f, 0f);
                currentCategoryButton.transform.localScale = Vector3.one * 0.8f;
                currentCategoryButton.GetChildWithName("ActiveSwatch").SetActive(false);
                currentCategoryButton.GetChildWithName("ColorSample").SetActive(false);
                currentCategoryButton.SetActive(true);

                // Change the title and reset its on click actions.
                currentCategoryButton.GetChildWithName("ColorName").GetComponent<UILabel>().text = currentCategoryObj.Key;
                currentCategoryButton.GetComponent<UIButton>().onClick.Clear();

                // This action to select the object to build in the EditorController class.
                EventDelegate onChange = new EventDelegate(EditorController.Instance, nameof(EditorController.SelectObjectToBuild));
                EventDelegate.Parameter onChangeParameter = new EventDelegate.Parameter
                {
                    field = "objName",
                    value = currentCategoryObj.Key,
                    obj = EditorController.Instance
                };
                onChange.mParameters = new EventDelegate.Parameter[] { onChangeParameter };
                currentCategoryButton.GetComponent<UIButton>().onClick.Add(onChange);

                // This to make the changes visible inside of the LE UI (setting this button as the selected one).
                EventDelegate onChangeUI = new EventDelegate(this, nameof(SetCurrentObjButtonAsSelected));
                EventDelegate.Parameter onChangeUIParameter = new EventDelegate.Parameter
                {
                    field = "selectedButton",
                    value = currentCategoryButton,
                    obj = this
                };
                onChangeUI.mParameters = new EventDelegate.Parameter[] { onChangeUIParameter };
                currentCategoryButton.GetComponent<UIButton>().onClick.Add(onChangeUI);

                currentCategoryButton.GetComponent<UIButtonScale>().mScale = Vector3.one * 0.8f;

                currentCategoryButtons.Add(currentCategoryButton);
            }

            // Select the very first element on the objects list.
            currentCategoryButtons[0].GetComponent<UIButton>().OnClick();
        }

        public void SetCurrentObjButtonAsSelected(GameObject selectedButton)
        {
            foreach (var obj in currentCategoryButtons)
            {
                obj.GetChildWithName("ActiveSwatch").SetActive(false);
            }

            selectedButton.GetChildWithName("ActiveSwatch").SetActive(true);
        }

        #region Selected Object Panel Related
        public void CreateSelectedObjPanel()
        {
            selectedObjPanel = new GameObject("CurrentSelectedObjPanel");
            selectedObjPanel.transform.parent = editorUIParent.transform;
            selectedObjPanel.transform.localPosition = new Vector3(-700f, -220f, 0f);
            selectedObjPanel.transform.localScale = Vector3.one;

            UISprite headerSprite = selectedObjPanel.AddComponent<UISprite>();
            headerSprite.atlas = NGUI_Utils.UITexturesAtlas;
            headerSprite.spriteName = "Square_Border_Beveled_HighOpacity";
            headerSprite.type = UIBasicSprite.Type.Sliced;
            headerSprite.color = new Color(0.218f, 0.6464f, 0.6509f, 1f);
            headerSprite.width = 520;
            headerSprite.height = 60;

            BoxCollider headerCollider = selectedObjPanel.AddComponent<BoxCollider>();
            headerCollider.size = new Vector3(520f, 60f, 1f);

            UILabel headerLabel = NGUI_Utils.CreateLabel(selectedObjPanel.transform, Vector3.zero, new Vector3Int(520, 60, 0), "No Object Selected", NGUIText.Alignment.Center,
                UIWidget.Pivot.Center);
            headerLabel.name = "Label";
            headerLabel.fontSize = 27;
            headerLabel.depth = 1;

            GameObject setActiveAtStartToggleObj = NGUI_Utils.CreateToggle(selectedObjPanel.transform, new Vector3(-220f, 0f, 0f),
                new Vector3Int(48, 48, 0));
            setActiveAtStartToggleObj.name = "SetActiveAtStartToggle";
            setActiveAtStartToggle = setActiveAtStartToggleObj.GetComponent<UIToggle>();
            setActiveAtStartToggle.onChange.Clear();
            setActiveAtStartToggle.onChange.Add(new EventDelegate(this, nameof(SetSetActiveAtStart)));
            setActiveAtStartToggle.instantTween = true;
            setActiveAtStartToggleObj.SetActive(false);

            GameObject setActiveAtStartLine = new GameObject("Line");
            setActiveAtStartLine.transform.parent = setActiveAtStartToggleObj.GetChildWithName("Background").transform;
            setActiveAtStartLine.transform.localPosition = Vector3.zero;
            setActiveAtStartLine.transform.localScale = Vector3.one;
            UISprite setActiveAtStartLineSprite = setActiveAtStartLine.AddComponent<UISprite>();
            setActiveAtStartLineSprite.atlas = NGUI_Utils.fractalSpaceAtlas;
            setActiveAtStartLineSprite.spriteName = "Square";
            setActiveAtStartLineSprite.width = 35;
            setActiveAtStartLineSprite.height = 6;
            setActiveAtStartLineSprite.depth = 8;
            setActiveAtStartLine.SetActive(false);

            globalObjAttributesToggle = NGUI_Utils.CreateButtonAsToggleWithSprite(selectedObjPanel.transform, new Vector3(220f, 0f, 0f), new Vector3Int(45, 45, 0), 2, "Global",
                Vector2Int.one * 25);
            globalObjAttributesToggle.name = "GlobalObjectAttributesBtnToggle";
            globalObjAttributesToggle.onClick += ShowGlobalObjectAttributes;
            globalObjAttributesToggle.gameObject.SetActive(false);

            GameObject selectedObjPanelBody = new GameObject("Body");
            selectedObjPanelBody.transform.parent = selectedObjPanel.transform;
            selectedObjPanelBody.transform.localPosition = new Vector3(0f, -160f, 0f);
            selectedObjPanelBody.transform.localScale = Vector3.one;

            UISprite bodySprite = selectedObjPanelBody.AddComponent<UISprite>();
            bodySprite.atlas = NGUI_Utils.UITexturesAtlas;
            bodySprite.spriteName = "Square_Border_Beveled_HighOpacity";
            bodySprite.type = UIBasicSprite.Type.Sliced;
            bodySprite.color = new Color(0.0039f, 0.3568f, 0.3647f, 1f);
            bodySprite.depth = -1;
            bodySprite.width = 500;
            bodySprite.height = 300;

            BoxCollider bodyCollider = selectedObjPanelBody.AddComponent<BoxCollider>();
            bodyCollider.size = new Vector3(500f, 300f, 1f);

            GameObject objectSpecificOptionsParent = new GameObject("ObjectSpecificOptions");
            objectSpecificOptionsParent.transform.parent = selectedObjPanelBody.transform;
            objectSpecificOptionsParent.transform.localPosition = Vector3.zero;
            objectSpecificOptionsParent.transform.localScale = Vector3.one;
            objectSpecificPanelsParent = objectSpecificOptionsParent.transform;

            GameObject globalObjectOptionsParent = new GameObject("GlobalObjectOptions");
            globalObjectOptionsParent.transform.parent = selectedObjPanelBody.transform;
            globalObjectOptionsParent.transform.localPosition = Vector3.zero;
            globalObjectOptionsParent.transform.localScale = Vector3.one;
            globalObjectPanelsParent = globalObjectOptionsParent.transform;

            SetSelectedObjPanelAsNone();

            CreateGlobalObjectAttributesPanel();
            CreateLightAttributesPanel();
            CreateSawAttributesPanel();
            CreateSawWaypointAttributesPanel();
            CreateSwitchAttributesPanel();
            CreateAmmoAndHealthPackAttributesPanel();
            CreateLaserAttributesPanel();
            CreateCeilingLightPanel();
            CreateFlameTrapAttributesPanel();
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
            GameObject xField = NGUI_Utils.CreateInputField(positionThingsParent, new Vector3(10f, 90f, 0f), new Vector3Int(65, 38, 0), 27, "0");
            xField.name = "XField";
            var xScript = xField.AddComponent<UICustomInputField>();
            xScript.Setup(UICustomInputField.UIInputType.FLOAT, maxDecimals: 2);
            xScript.onChange += (() => SetPropertyWithInput("XPosition", xScript));

            UILabel yTitle = NGUI_Utils.CreateLabel(positionThingsParent, new Vector3(60f, 90f, 0f), new Vector3Int(28, 38, 0), "Y", NGUIText.Alignment.Center,
                UIWidget.Pivot.Center);
            yTitle.name = "YTitle";
            GameObject yField = NGUI_Utils.CreateInputField(positionThingsParent, new Vector3(110f, 90f, 0f), new Vector3Int(65, 38, 0), 27, "0");
            yField.name = "YField";
            var yScript = yField.AddComponent<UICustomInputField>();
            yScript.Setup(UICustomInputField.UIInputType.FLOAT, maxDecimals: 2);
            yScript.onChange += (() => SetPropertyWithInput("YPosition", yScript));

            UILabel zTitle = NGUI_Utils.CreateLabel(positionThingsParent, new Vector3(160f, 90f, 0f), new Vector3Int(28, 38, 0), "Z", NGUIText.Alignment.Center,
                UIWidget.Pivot.Center);
            zTitle.name = "ZTitle";
            GameObject zField = NGUI_Utils.CreateInputField(positionThingsParent, new Vector3(210f, 90f, 0f), new Vector3Int(65, 38, 0), 27, "0");
            zField.name = "ZField";
            var zScript = zField.AddComponent<UICustomInputField>();
            zScript.Setup(UICustomInputField.UIInputType.FLOAT, maxDecimals: 2);
            zScript.onChange += (() => SetPropertyWithInput("ZPosition", zScript));

            globalAttributesList.Add("Position", positionThingsParent);
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
            GameObject xField = NGUI_Utils.CreateInputField(rotationThingsParent, new Vector3(10f, 40f, 0f), new Vector3Int(65, 38, 0), 27, "0");
            xField.name = "XField";
            var xScript = xField.AddComponent<UICustomInputField>();
            xScript.Setup(UICustomInputField.UIInputType.FLOAT, maxDecimals: 2);
            xScript.onChange += (() => SetPropertyWithInput("XRotation", xScript));

            UILabel yTitle = NGUI_Utils.CreateLabel(rotationThingsParent, new Vector3(60f, 40f, 0f), new Vector3Int(28, 38, 0), "Y", NGUIText.Alignment.Center,
                UIWidget.Pivot.Center);
            yTitle.name = "YTitle";
            GameObject yField = NGUI_Utils.CreateInputField(rotationThingsParent, new Vector3(110f, 40f, 0f), new Vector3Int(65, 38, 0), 27, "0");
            yField.name = "YField";
            var yScript = yField.AddComponent<UICustomInputField>();
            yScript.Setup(UICustomInputField.UIInputType.FLOAT, maxDecimals: 2);
            yScript.onChange += (() => SetPropertyWithInput("YRotation", yScript));

            UILabel zTitle = NGUI_Utils.CreateLabel(rotationThingsParent, new Vector3(160f, 40f, 0f), new Vector3Int(28, 38, 0), "Z", NGUIText.Alignment.Center,
                UIWidget.Pivot.Center);
            zTitle.name = "ZTitle";
            GameObject zField = NGUI_Utils.CreateInputField(rotationThingsParent, new Vector3(210f, 40f, 0f), new Vector3Int(65, 38, 0), 27, "0");
            zField.name = "ZField";
            var zScript = zField.AddComponent<UICustomInputField>();
            zScript.Setup(UICustomInputField.UIInputType.FLOAT, maxDecimals: 2);
            zScript.onChange += (() => SetPropertyWithInput("ZRotation", zScript));

            globalAttributesList.Add("Rotation", rotationThingsParent);
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
            GameObject xField = NGUI_Utils.CreateInputField(scaleThingsParent, new Vector3(10f, -10f, 0f), new Vector3Int(65, 38, 0), 27, "0");
            xField.name = "XField";
            var xScript = xField.AddComponent<UICustomInputField>();
            xScript.Setup(UICustomInputField.UIInputType.FLOAT, maxDecimals: 2);
            xScript.onChange += (() => SetPropertyWithInput("XScale", xScript));

            UILabel yTitle = NGUI_Utils.CreateLabel(scaleThingsParent, new Vector3(60f, -10f, 0f), new Vector3Int(28, 38, 0), "Y", NGUIText.Alignment.Center,
                UIWidget.Pivot.Center);
            yTitle.name = "YTitle";
            GameObject yField = NGUI_Utils.CreateInputField(scaleThingsParent, new Vector3(110f, -10f, 0f), new Vector3Int(65, 38, 0), 27, "0");
            yField.name = "YField";
            var yScript = yField.AddComponent<UICustomInputField>();
            yScript.Setup(UICustomInputField.UIInputType.FLOAT, maxDecimals: 2);
            yScript.onChange += (() => SetPropertyWithInput("YScale", yScript));

            UILabel zTitle = NGUI_Utils.CreateLabel(scaleThingsParent, new Vector3(160f, -10f, 0f), new Vector3Int(28, 38, 0), "Z", NGUIText.Alignment.Center,
                UIWidget.Pivot.Center);
            zTitle.name = "ZTitle";
            GameObject zField = NGUI_Utils.CreateInputField(scaleThingsParent, new Vector3(210f, -10f, 0f), new Vector3Int(65, 38, 0), 27, "0");
            zField.name = "ZField";
            var zScript = zField.AddComponent<UICustomInputField>();
            zScript.Setup(UICustomInputField.UIInputType.FLOAT, maxDecimals: 2);
            zScript.onChange += (() => SetPropertyWithInput("ZScale", zScript));

            globalAttributesList.Add("Scale", scaleThingsParent);
        }

        void CreateLightAttributesPanel()
        {
            GameObject lightAttributes = new GameObject("LightAttributes");
            lightAttributes.transform.parent = objectSpecificPanelsParent;
            lightAttributes.transform.localPosition = Vector3.zero;
            lightAttributes.transform.localScale = Vector3.one;

            #region Color Input Field
            UILabel colorTitle = NGUI_Utils.CreateLabel(lightAttributes.transform, new Vector3(-230, 90), new Vector3Int(235, NGUI_Utils.defaultLabelSize.y, 0), "Color (Hex)");
            colorTitle.name = "ColorTitle";
            colorTitle.color = Color.white;

            UILabel hashtagLOL = NGUI_Utils.CreateLabel(lightAttributes.transform, new Vector3(15, 90), new Vector3Int(20, NGUI_Utils.defaultLabelSize.y, 0), "#",
                NGUIText.Alignment.Center, UIWidget.Pivot.Left);
            hashtagLOL.name = "HashtagLOL";
            hashtagLOL.color = Color.white;

            GameObject colorInputField = NGUI_Utils.CreateInputField(lightAttributes.transform, new Vector3(140f, 90f, 0f), new Vector3Int(200, 38, 0), 27,
                "FFFFFF", false);
            colorInputField.name = "ColorField";
            var colorFieldCustomScript = colorInputField.AddComponent<UICustomInputField>();
            colorFieldCustomScript.Setup(UICustomInputField.UIInputType.HEX_COLOR);
            colorFieldCustomScript.setFieldColorAutomatically = false;
            colorFieldCustomScript.onChange += (() => SetPropertyWithInput("Color", colorFieldCustomScript));
            #endregion

            #region Intensity Input Field
            UILabel intensityTitle = NGUI_Utils.CreateLabel(lightAttributes.transform, new Vector3(-230, 40), new Vector3Int(260, NGUI_Utils.defaultLabelSize.y, 0), "Intensity");
            intensityTitle.name = "IntensityTitle";
            intensityTitle.color = Color.white;

            GameObject intensityInputField = NGUI_Utils.CreateInputField(lightAttributes.transform, new Vector3(140f, 40f, 0f), new Vector3Int(200, 38, 0), 27,
                "1", false);
            intensityInputField.name = "IntensityField";
            var intensityFieldCustomScript = intensityInputField.AddComponent<UICustomInputField>();
            intensityFieldCustomScript.Setup(UICustomInputField.UIInputType.NON_NEGATIVE_FLOAT);
            intensityFieldCustomScript.setFieldColorAutomatically = false;
            intensityFieldCustomScript.onChange += (() => SetPropertyWithInput("Intensity", intensityFieldCustomScript));
            #endregion

            lightAttributes.SetActive(false);
            attrbutesPanels.Add("Light", lightAttributes);
        }
        void CreateSawAttributesPanel()
        {
            GameObject sawAttributes = new GameObject("SawAttributes");
            sawAttributes.transform.parent = objectSpecificPanelsParent;
            sawAttributes.transform.localPosition = Vector3.zero;
            sawAttributes.transform.localScale = Vector3.one;

            #region Activate On Start Toggle
            UILabel activateOnStartTitle = NGUI_Utils.CreateLabel(sawAttributes.transform, new Vector3(-230, 90), new Vector3Int(395, NGUI_Utils.defaultLabelSize.y, 0),
                "Activate On Start");
            activateOnStartTitle.name = "ActivateOnStartTitle";
            activateOnStartTitle.color = Color.white;

            GameObject activateOnStartToggle = NGUI_Utils.CreateToggle(sawAttributes.transform, new Vector3(200f, 90f, 0f), new Vector3Int(48, 48, 0));
            activateOnStartToggle.name = "ActivateOnStartToggle";
            activateOnStartToggle.GetComponent<UIToggle>().onChange.Clear();
            var activateOnStartDelegate = NGUI_Utils.CreateEvenDelegate(this, nameof(SetPropertyWithToggle),
                NGUI_Utils.CreateEventDelegateParamter(this, "propertyName", "ActivateOnStart"),
                NGUI_Utils.CreateEventDelegateParamter(this, "toggle", activateOnStartToggle.GetComponent<UIToggle>()));
            activateOnStartToggle.GetComponent<UIToggle>().onChange.Add(activateOnStartDelegate);
            #endregion

            #region Damage Input Field
            UILabel damageTitle = NGUI_Utils.CreateLabel(sawAttributes.transform, new Vector3(-230, 40), new Vector3Int(260, NGUI_Utils.defaultLabelSize.y, 0), "Damage");
            damageTitle.name = "DamageTitle";
            damageTitle.color = Color.white;

            GameObject damageInputField = NGUI_Utils.CreateInputField(sawAttributes.transform, new Vector3(140f, 40f, 0f), new Vector3Int(200, 38, 0), 27,
                "50", false, NGUIText.Alignment.Left);
            damageInputField.name = "DamageInputField";
            var damangeFieldCustomScript = damageInputField.AddComponent<UICustomInputField>();
            damangeFieldCustomScript.Setup(UICustomInputField.UIInputType.NON_NEGATIVE_INT);
            damangeFieldCustomScript.onChange += (() => SetPropertyWithInput("Damage", damangeFieldCustomScript));
            #endregion

            #region Add Waypoint
            UIButtonPatcher addWaypoint = NGUI_Utils.CreateButton(sawAttributes.transform, new Vector3(0f, -15f, 0f), new Vector3Int(480, 55, 0), "+ Add Waypoint");
            addWaypoint.name = "AddWaypointButton";
            addWaypoint.onClick += () => TriggerAction("AddWaypoint");
            addWaypoint.GetComponent<UIButtonScale>().hover = Vector3.one * 1.05f;
            addWaypoint.GetComponent<UIButtonScale>().pressed = Vector3.one * 1.02f;
            #endregion

            sawAttributes.SetActive(false);
            attrbutesPanels.Add("Saw", sawAttributes);
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
            attrbutesPanels.Add("SawWaypoint", sawWaypointAttributes);
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
            attrbutesPanels.Add("Switch", switchAttributes);
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
            attrbutesPanels.Add("AmmoAndHealth", ammoHealthAttributes);
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
            attrbutesPanels.Add("Laser", laserAttributes);
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
            attrbutesPanels.Add("Ceiling Light", ceilingLightAttributes);
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
            attrbutesPanels.Add("Flame Trap", flameTrapAttributes);
        }

        public void SetSelectedObjPanelAsNone()
        {
            selectedObjPanel.GetChildWithName("Label").GetComponent<UILabel>().text = "No Object Selected";
            selectedObjPanel.GetChildWithName("SetActiveAtStartToggle").SetActive(false);
            globalObjAttributesToggle.gameObject.SetActive(false);
            selectedObjPanel.GetChildWithName("Body").SetActive(false);
            selectedObjPanel.transform.localPosition = new Vector3(-700f, -505f, 0f);
        }
        public void SetMultipleObjectsSelected()
        {
            selectedObjPanel.GetChildWithName("Label").GetComponent<UILabel>().text = "Multiple Objects Selected";
            selectedObjPanel.GetChildWithName("Body").SetActive(true);
            selectedObjPanel.transform.localPosition = new Vector3(-700f, -220f, 0f);

            selectedObjPanel.GetChildWithName("SetActiveAtStartToggle").SetActive(true);
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
                selectedObjPanel.GetChildWithName("SetActiveAtStartToggle").GetComponent<UIToggle>().Set((bool)setActiveStateInObjects);
                selectedObjPanel.GetChildAt("SetActiveAtStartToggle/Background/Line").SetActive(false);
            }
            else
            {
                executeSetActiveAtStartToggleActions = false;
                selectedObjPanel.GetChildWithName("SetActiveAtStartToggle").GetComponent<UIToggle>().Set(false);
                executeSetActiveAtStartToggleActions = true;
                selectedObjPanel.GetChildAt("SetActiveAtStartToggle/Background/Line").SetActive(true);
            }

            globalObjAttributesToggle.gameObject.SetActive(false);
            globalObjAttributesToggle.SetToggleState(true, true);

            UpdateGlobalObjectAttributes(EditorController.Instance.currentSelectedObj.transform);
        }
        public void SetSelectedObject(LE_Object objComponent)
        {
            selectedObjPanel.GetChildWithName("Label").GetComponent<UILabel>().text = objComponent.objectFullNameWithID;
            selectedObjPanel.GetChildWithName("Body").SetActive(true);
            selectedObjPanel.transform.localPosition = new Vector3(-700f, -220, 0f);

            attrbutesPanels.ToList().ForEach(x => x.Value.SetActive(false));

            // Enable the toggle and show object-specific attributes, then it will be disabled or changed to GLOBAL attributes if the object doesn't have unique ones.
            globalObjAttributesToggle.gameObject.SetActive(true);
            globalObjAttributesToggle.SetToggleState(false, true);

            if (objComponent.objectOriginalName == "Directional Light" || objComponent.objectOriginalName == "Point Light")
            {
                attrbutesPanels["Light"].SetActive(true);

                // Set color input...
                var colorInput = attrbutesPanels["Light"].GetChildWithName("ColorField").GetComponent<UIInput>();
                colorInput.text = Utilities.ColorToHex((Color)objComponent.GetProperty("Color"));

                // Set intensity input...
                var intensityInput = attrbutesPanels["Light"].GetChildWithName("IntensityField").GetComponent<UIInput>();
                intensityInput.text = (float)objComponent.GetProperty("Intensity") + "";
            }
            else if (objComponent.objectOriginalName == "Saw")
            {
                attrbutesPanels["Saw"].SetActive(true);

                // Set activate on start toggle...
                var activateOnStartToggle = attrbutesPanels["Saw"].GetChildWithName("ActivateOnStartToggle").GetComponent<UIToggle>();
                activateOnStartToggle.Set((bool)objComponent.GetProperty("ActivateOnStart"));

                // Set the damage input field...
                var damageInput = attrbutesPanels["Saw"].GetChildWithName("DamageInputField").GetComponent<UIInput>();
                damageInput.text = (int)objComponent.GetProperty("Damage") + "";
            }
            else if (objComponent.objectOriginalName == "Saw Waypoint")
            {
                attrbutesPanels["SawWaypoint"].SetActive(true);

                // Set wait time input field...
                var waitTimeInputField = attrbutesPanels["SawWaypoint"].GetChildWithName("WaitTimeInputField").GetComponent<UIInput>();
                waitTimeInputField.text = (float)objComponent.GetProperty("WaitTime") + "";
            }
            else if (objComponent.objectOriginalName == "Switch")
            {
                attrbutesPanels["Switch"].SetActive(true);

                // Set usable once toggle...
                var usableOnceToggle = attrbutesPanels["Switch"].GetChildWithName("UsableOnceToggle").GetComponent<UIToggle>();
                usableOnceToggle.Set((bool)objComponent.GetProperty("UsableOnce"));

                // Set can use taser toggle...
                var canUseTaserToggle = attrbutesPanels["Switch"].GetChildWithName("CanUseTaserToggle").GetComponent<UIToggle>();
                canUseTaserToggle.Set((bool)objComponent.GetProperty("CanUseTaser"));
            }
            else if (objComponent.objectOriginalName == "Ammo Pack" || objComponent.objectOriginalName == "Health Pack")
            {
                attrbutesPanels["AmmoAndHealth"].SetActive(true);

                var respawnTimeInputField = attrbutesPanels["AmmoAndHealth"].GetChildWithName("RespawnInputField").GetComponent<UIInput>();
                respawnTimeInputField.text = (float)objComponent.GetProperty("RespawnTime") + "";
            }
            else if (objComponent.objectOriginalName == "Laser")
            {
                attrbutesPanels["Laser"].SetActive(true);

                // Set activate on start toggle...
                var activateOnStartToggle = attrbutesPanels["Laser"].GetChildWithName("ActivateOnStartToggle").GetComponent<UIToggle>();
                activateOnStartToggle.Set((bool)objComponent.GetProperty("ActivateOnStart"));

                // Set insta-kill toggle...
                var instaKillToggle = attrbutesPanels["Laser"].GetChildWithName("InstaKillToggle").GetComponent<UIToggle>();
                instaKillToggle.Set((bool)objComponent.GetProperty("InstaKill"));

                // Set the damage input field...
                var damageInput = attrbutesPanels["Laser"].GetChildWithName("DamageInputField").GetComponent<UIInput>();
                damageInput.text = (int)objComponent.GetProperty("Damage") + "";
            }
            else if (objComponent.objectOriginalName == "Ceiling Light")
            {
                attrbutesPanels["Ceiling Light"].SetActive(true);

                // Set activate on start toggle...
                var activateOnStartToggle = attrbutesPanels["Ceiling Light"].GetChildWithName("ActivateOnStartToggle").GetComponent<UIToggle>();
                activateOnStartToggle.Set((bool)objComponent.GetProperty("ActivateOnStart"));

                // Set color input...
                var colorInput = attrbutesPanels["Ceiling Light"].GetChildWithName("ColorField").GetComponent<UIInput>();
                colorInput.text = Utilities.ColorToHex((Color)objComponent.GetProperty("Color"));
            }
            else if (objComponent.objectOriginalName == "Flame Trap")
            {
                attrbutesPanels["Flame Trap"].SetActive(true);

                // Set activate on start toggle...
                var activateOnStartToggle = attrbutesPanels["Flame Trap"].GetChildWithName("ActivateOnStartToggle").GetComponent<UIToggle>();
                activateOnStartToggle.Set((bool)objComponent.GetProperty("ActivateOnStart"));

                // Set constant toggle...
                var constantToggle = attrbutesPanels["Flame Trap"].GetChildWithName("ConstantToggle").GetComponent<UIToggle>();
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
                selectedObjPanel.GetChildWithName("SetActiveAtStartToggle").SetActive(true);
                selectedObjPanel.GetChildWithName("SetActiveAtStartToggle").GetComponent<UIToggle>().Set(objComponent.setActiveAtStart);
                selectedObjPanel.GetChildAt("SetActiveAtStartToggle/Background/Line").SetActive(false);
            }
            else
            {
                selectedObjPanel.GetChildWithName("SetActiveAtStartToggle").SetActive(false);
                objComponent.setActiveAtStart = true; // Just in case ;)
            }
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
            globalAttributesList["Position"].GetChildWithName("XField").GetComponent<UICustomInputField>().SetText(obj.position.x, 2);
            globalAttributesList["Position"].GetChildWithName("YField").GetComponent<UICustomInputField>().SetText(obj.position.y, 2);
            globalAttributesList["Position"].GetChildWithName("ZField").GetComponent<UICustomInputField>().SetText(obj.position.z, 2);

            globalAttributesList["Rotation"].GetChildWithName("XField").GetComponent<UICustomInputField>().SetText(obj.localEulerAngles.x, 2);
            globalAttributesList["Rotation"].GetChildWithName("YField").GetComponent<UICustomInputField>().SetText(obj.localEulerAngles.y, 2);
            globalAttributesList["Rotation"].GetChildWithName("ZField").GetComponent<UICustomInputField>().SetText(obj.localEulerAngles.z, 2);

            globalAttributesList["Scale"].GetChildWithName("XField").GetComponent<UICustomInputField>().SetText(obj.localScale.x, 2);
            globalAttributesList["Scale"].GetChildWithName("YField").GetComponent<UICustomInputField>().SetText(obj.localScale.y, 2);
            globalAttributesList["Scale"].GetChildWithName("ZField").GetComponent<UICustomInputField>().SetText(obj.localScale.z, 2);
        }
        // I need this EXTRA AND USELESS function just because NGUIzzzzzz can't call the LE_Object function directly...
        // AAALSO now its seems crapGUI can't recognize between two different overloads of a method, so I need to put different names foreach method, DAMN IT.
        public void SetSetActiveAtStart()
        {
            if (!executeSetActiveAtStartToggleActions) return;

            if (EditorController.Instance.multipleObjectsSelected)
            {
                selectedObjPanel.GetChildAt("SetActiveAtStartToggle/Background/Line").SetActive(false);
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
            attrbutesPanels["Laser"].GetChildWithName("DamageTitle").SetActive(!newState);
            attrbutesPanels["Laser"].GetChildWithName("DamageInputField").SetActive(!newState);
        }
        #endregion

        void CreateSavingLevelLabel()
        {
            savingLevelLabel = NGUI_Utils.CreateLabel(editorUIParent.transform, new Vector3(0, 510), new Vector3Int(150, 50, 0), "Saving...", NGUIText.Alignment.Center,
                UIWidget.Pivot.Center);
            savingLevelLabel.name = "SavingLevelLabel";
            savingLevelLabel.fontSize = 32;

            TweenAlpha tween = savingLevelLabel.gameObject.AddComponent<TweenAlpha>();
            tween.from = 1f;
            tween.to = 0f;
            tween.duration = 2f;

            savingLevelLabel.gameObject.SetActive(false);

            savingLevelLabelInPauseMenu = Instantiate(savingLevelLabel.gameObject, pauseMenu.transform).GetComponent<UILabel>();
            savingLevelLabelInPauseMenu.name = "SavingLevelInPauseMenuLabel";
            savingLevelLabelInPauseMenu.transform.localPosition = new Vector3(0f, -425f, 0f);
            savingLevelLabelInPauseMenu.gameObject.SetActive(false);
        }
        public void PlaySavingLevelLabel()
        {
            // If the coroutine was already played, stop it if it's currently playing to "restart" it.
            if (savingLevelLabelRoutine != null) MelonCoroutines.Stop(savingLevelLabelRoutine);

            // Execute the coroutine.
            savingLevelLabelRoutine = (Coroutine)MelonCoroutines.Start(Coroutine());
            IEnumerator Coroutine()
            {
                savingLevelLabel.gameObject.SetActive(true);
                savingLevelLabelInPauseMenu.gameObject.SetActive(true);

                TweenAlpha tween = savingLevelLabel.GetComponent<TweenAlpha>();
                TweenAlpha tweenInPauseMenu = savingLevelLabelInPauseMenu.GetComponent<TweenAlpha>();
                tween.ResetToBeginning();
                tween.PlayForward();
                tweenInPauseMenu.ResetToBeginning();
                tweenInPauseMenu.PlayForward();

                yield return new WaitForSecondsRealtime(2f);

                savingLevelLabel.gameObject.SetActive(false);
                savingLevelLabelInPauseMenu.gameObject.SetActive(false);
            }
        }

        void CreateCurrentModeLabel()
        {
            currentModeLabel = NGUI_Utils.CreateLabel(editorUIParent.transform, new Vector3(0, -515), new Vector3Int(500, 50, 0), "Current Mode:", NGUIText.Alignment.Center,
                UIWidget.Pivot.Center);
            currentModeLabel.fontSize = 35;
            SetCurrentModeLabelText(EditorController.Mode.Building);

            currentModeLabel.gameObject.SetActive(true);
        }
        public void SetCurrentModeLabelText(EditorController.Mode mode)
        {
            currentModeLabel.text = $"[c][ffff00]Current mode:[-][/c] {mode.ToString()}";
        }

        public void HideOrShowCategoryButtons()
        {
            categoryButtonsAreHidden = !categoryButtonsAreHidden;

            if (categoryButtonsAreHidden)
            {
                TweenAlpha.Begin(categoryButtonsParent, 0.2f, 0f);
                TweenPosition.Begin(currentCategoryBG, 0.2f, new Vector3(0f, 410f, 0f));
                InGameUIManager.Instance.m_uiAudioSource.PlayOneShot(InGameUIManager.Instance.hideHUDSound);
            }
            else
            {
                TweenAlpha.Begin(categoryButtonsParent, 0.2f, 1f);
                TweenPosition.Begin(currentCategoryBG, 0.2f, new Vector3(0f, 330f, 0f));
                InGameUIManager.Instance.m_uiAudioSource.PlayOneShot(InGameUIManager.Instance.showHUDSound);
            }
        }

        void CreateHittenTargetObjPanel()
        {
            hittenTargetObjPanel = new GameObject("HittenTargetObjPanel");
            hittenTargetObjPanel.transform.parent = editorUIParent.transform;
            hittenTargetObjPanel.transform.localPosition = Vector3.zero;
            hittenTargetObjPanel.transform.localScale = Vector3.one;

            UISprite sprite = hittenTargetObjPanel.AddComponent<UISprite>();
            sprite.atlas = NGUI_Utils.UITexturesAtlas;
            sprite.spriteName = "Square_Border_Beveled_HighOpacity";
            sprite.type = UIBasicSprite.Type.Sliced;
            sprite.width = 300;
            sprite.height = 50;
            sprite.color = new Color(0.218f, 0.6464f, 0.6509f, 1f);
            sprite.pivot = UIWidget.Pivot.TopLeft;
            sprite.depth = 0;

            GameObject label = new GameObject("HittenObjName");
            label.transform.parent = hittenTargetObjPanel.transform;
            label.transform.localScale = Vector3.one;
            hittenTargetObjLabel = label.AddComponent<UILabel>();
            hittenTargetObjLabel.font = NGUI_Utils.labelFont;
            hittenTargetObjLabel.fontSize = 27;
            hittenTargetObjLabel.width = 290;
            hittenTargetObjLabel.height = 40;
            hittenTargetObjLabel.pivot = UIWidget.Pivot.Left;
            hittenTargetObjLabel.depth = 1;
            label.transform.localPosition = new Vector3(5f, -25f);

            hittenTargetObjPanel.SetActive(false);
        }
        public void UpdateHittenTargetObjPanel(string hittenObjName)
        {
            Vector3 mousePos = Input.mousePosition;
            Vector3 worldPos = NGUI_Utils.mainMenuCamera.ScreenToWorldPoint(mousePos);
            Vector3 localPos = hittenTargetObjPanel.transform.parent.InverseTransformPoint(worldPos);
            hittenTargetObjPanel.transform.localPosition = localPos - new Vector3(-20f, 20f);
            hittenTargetObjLabel.text = hittenObjName;
        }

        public void CreateHelpPanel()
        {
            #region Create Help Panel With The BG
            helpPanel = new GameObject("HelpPanel");
            helpPanel.transform.parent = editorUIParent.transform;
            helpPanel.transform.localScale = Vector3.one;

            UISprite helpPanelBG = helpPanel.AddComponent<UISprite>();
            helpPanelBG.atlas = NGUI_Utils.UITexturesAtlas;
            helpPanelBG.spriteName = "Square_Border_Beveled_HighOpacity";
            helpPanelBG.type = UIBasicSprite.Type.Sliced;
            helpPanelBG.color = new Color(0.218f, 0.6464f, 0.6509f, 1f);
            helpPanelBG.width = 1850;
            helpPanelBG.height = 1010;
            #endregion

            #region Create Title
            UILabel titleLabel = NGUI_Utils.CreateLabel(helpPanel.transform, new Vector3(0, 460), new Vector3Int(200, 50, 0), "KEYBINDS", NGUIText.Alignment.Center,
                UIWidget.Pivot.Center);
            titleLabel.name = "Title";
            titleLabel.fontSize = 50;
            #endregion

            #region Create Keybinds Text
            GameObject keybindsObj = new GameObject("Keybinds");
            keybindsObj.transform.parent = helpPanel.transform;
            keybindsObj.transform.localScale = Vector3.one;
            keybindsObj.transform.localPosition = new Vector3(-900f, 425f, 0f);

            Stream keybindsTextStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FS_LevelEditor.KeybindsList.txt");
            byte[] keybindsTextBytes = new byte[keybindsTextStream.Length];
            keybindsTextStream.Read(keybindsTextBytes);

            UILabel keybindsLabel = keybindsObj.AddComponent<UILabel>();
            keybindsLabel.depth = 1;
            keybindsLabel.material = NGUI_Utils.controllerAtlasMaterial;
            keybindsLabel.font = NGUI_Utils.robotoFont;
            keybindsLabel.text = Encoding.UTF8.GetString(keybindsTextBytes);
            keybindsLabel.alignment = NGUIText.Alignment.Left;
            keybindsLabel.pivot = UIWidget.Pivot.TopLeft;
            keybindsLabel.fontSize = 35;
            keybindsLabel.width = 900;
            keybindsLabel.height = 900;

            // Set the position again since when I change the pivot, it also changes the position.
            keybindsObj.transform.localPosition = new Vector3(-900f, 425f, 0f);

            GameObject keybindsObj2 = new GameObject("Keybinds2");
            keybindsObj2.transform.parent = helpPanel.transform;
            keybindsObj2.transform.localScale = Vector3.one;
            keybindsObj2.transform.localPosition = new Vector3(0f, 425f, 0f);

            Stream keybindsTextStream2 = Assembly.GetExecutingAssembly().GetManifestResourceStream("FS_LevelEditor.KeybindsList2.txt");
            byte[] keybindsTextBytes2 = new byte[keybindsTextStream2.Length];
            keybindsTextStream2.Read(keybindsTextBytes2);

            UILabel keybindsLabel2 = keybindsObj2.AddComponent<UILabel>();
            keybindsLabel2.depth = 1;
            keybindsLabel2.material = NGUI_Utils.controllerAtlasMaterial;
            keybindsLabel2.font = NGUI_Utils.robotoFont;
            keybindsLabel2.text = Encoding.UTF8.GetString(keybindsTextBytes2);
            keybindsLabel2.alignment = NGUIText.Alignment.Left;
            keybindsLabel2.pivot = UIWidget.Pivot.TopLeft;
            keybindsLabel2.fontSize = 35;
            keybindsLabel2.width = 900;
            keybindsLabel2.height = 900;

            keybindsObj2.transform.localPosition = new Vector3(0f, 425f, 0f);
            #endregion

            helpPanel.SetActive(false);
        }
        public void ShowOrHideHelpPanel()
        {
            bool isEnablingIt = !helpPanel.activeSelf;

            if (isEnablingIt) { SetEditorUIContext(EditorUIContext.HELP_PANEL); }
            else { SetEditorUIContext(EditorUIContext.NORMAL); }
        }

        #region Global Properties Related
        public void CreateGlobalPropertiesPanel()
        {
            #region Create Object With Background
            globalPropertiesPanel = new GameObject("GlobalPropertiesPanel");
            globalPropertiesPanel.transform.parent = editorUIParent.transform;
            globalPropertiesPanel.transform.localScale = Vector3.one;
            globalPropertiesPanel.transform.localPosition = new Vector3(1320f, 0f, 0f);

            UISprite background = globalPropertiesPanel.AddComponent<UISprite>();
            background.atlas = NGUI_Utils.UITexturesAtlas;
            background.spriteName = "Square_Border_Beveled_HighOpacity";
            background.type = UIBasicSprite.Type.Sliced;
            background.color = new Color(0.218f, 0.6464f, 0.6509f, 1f);
            background.width = 650;
            background.height = 1010;

            BoxCollider collider = globalPropertiesPanel.AddComponent<BoxCollider>();
            collider.size = new Vector2(650f, 1010f);
            #endregion

            #region Create Title
            UILabel titleLabel = NGUI_Utils.CreateLabel(globalPropertiesPanel.transform, new Vector3(0, 460), new Vector3Int(600, 50, 0), "Global Properties",
                NGUIText.Alignment.Center, UIWidget.Pivot.Center);
            titleLabel.name = "Title";
            titleLabel.depth = 1;
            titleLabel.fontSize = 30;
            #endregion

            #region Create Has Taser Toggle
            GameObject hasTaserToggle = NGUI_Utils.CreateToggle(globalPropertiesPanel.transform,
                new Vector3(-300f, 350f), new Vector3Int(200, 42, 1), "Has Taser");
            hasTaserToggle.name = "HasTaserToggle";
            EventDelegate hasTaserDelegate = NGUI_Utils.CreateEvenDelegate(this, nameof(SetGlobalPropertyWithToggle),
                NGUI_Utils.CreateEventDelegateParamter(this, "name", "HasTaser"),
                NGUI_Utils.CreateEventDelegateParamter(this, "toggle", hasTaserToggle.GetComponent<UIToggle>()));
            hasTaserToggle.GetComponent<UIToggle>().onChange.Clear();
            hasTaserToggle.GetComponent<UIToggle>().onChange.Add(hasTaserDelegate);
            #endregion

            #region Create Has Jetpack Toggle
            GameObject hasJetpackToggle = NGUI_Utils.CreateToggle(globalPropertiesPanel.transform,
                new Vector3(40f, 350f), new Vector3Int(200, 42, 1), "Has Jetpack");
            hasJetpackToggle.name = "HasJetpackToggle";
            EventDelegate hasJetpackDelegate = NGUI_Utils.CreateEvenDelegate(this, nameof(SetGlobalPropertyWithToggle),
                NGUI_Utils.CreateEventDelegateParamter(this, "name", "HasJetpack"),
                NGUI_Utils.CreateEventDelegateParamter(this, "toggle", hasJetpackToggle.GetComponent<UIToggle>()));
            hasJetpackToggle.GetComponent<UIToggle>().onChange.Clear();
            hasJetpackToggle.GetComponent<UIToggle>().onChange.Add(hasJetpackDelegate);
            #endregion

            #region Create Death Y Limit Field
            UILabel deathYLimitLabel = NGUI_Utils.CreateLabel(globalPropertiesPanel.transform, new Vector3(-300, 270), new Vector3Int(250, 50, 0), "Death Y Limit");
            deathYLimitLabel.name = "DeathYLimitLabel";
            deathYLimitLabel.depth = 1;
            deathYLimitLabel.fontSize = 30;

            GameObject deathYLimitField = NGUI_Utils.CreateInputField(globalPropertiesPanel.transform, new Vector3(100f, 270f, 0f),
                new Vector3Int(300, 50, 0), 30, "100");
            deathYLimitField.name = "DeathYLimit";
            deathYLimitField.GetComponent<UIInput>().onValidate = (UIInput.OnValidate)NGUI_Utils.ValidateNonNegativeFloat;
            var deathFieldCustomScript = deathYLimitField.AddComponent<UICustomInputField>();
            deathFieldCustomScript.Setup(UICustomInputField.UIInputType.NON_NEGATIVE_FLOAT);
            deathFieldCustomScript.onChange += (() => SetGlobalPropertyWithInput("DeathYLimit", deathFieldCustomScript));

            UIButtonAsToggle visualizeDeathYLimitButton = NGUI_Utils.CreateButtonAsToggleWithSprite(globalPropertiesPanel.transform,
                new Vector3(285f, 270f, 0f), new Vector3Int(48, 48, 1), 1, "WhiteSquare", Vector2Int.one * 20);
            visualizeDeathYLimitButton.name = "VisualizeDeathYLimitBtnToggle";
            visualizeDeathYLimitButton.GetComponent<UIButtonScale>().hover = Vector3.one * 1.05f;
            visualizeDeathYLimitButton.GetComponent<UIButtonScale>().pressed = Vector3.one * 1.02f;
            visualizeDeathYLimitButton.onClick += OnVisualizeDeathYLimitToggleClick;
            #endregion

            #region Create Level Skybox Dropdown
            UIDropdownPatcher skyboxDropdown = NGUI_Utils.CreateDropdown(globalPropertiesPanel.transform, new Vector3(0f, 160f), Vector3.one * 0.8f);
            skyboxDropdown.gameObject.name = "SkyboxDropdown";
            skyboxDropdown.SetTitle("Skybox");
            skyboxDropdown.AddOption("Chapter 1", true);
            skyboxDropdown.AddOption("Chapter 2", false);
            skyboxDropdown.AddOption("Chapter 3 & 4", false);

            skyboxDropdown.AddOnChangeOption((id) => SetGlobalPropertyWithDropdown("Skybox", id));
            #endregion
        }
        public void ShowOrHideGlobalPropertiesPanel()
        {
            if (!IsCurrentUIContext(EditorUIContext.GLOBAL_PROPERTIES)) { SetEditorUIContext(EditorUIContext.GLOBAL_PROPERTIES); }
            else { SetEditorUIContext(EditorUIContext.NORMAL); }
        }
        void RefreshGlobalPropertiesPanelValues()
        {
            GameObject panel = globalPropertiesPanel;

            panel.GetChildWithName("HasTaserToggle").GetComponent<UIToggle>().Set((bool)GetGlobalProperty("HasTaser"));
            panel.GetChildWithName("HasJetpackToggle").GetComponent<UIToggle>().Set((bool)GetGlobalProperty("HasJetpack"));
            panel.GetChildWithName("DeathYLimit").GetComponent<UIInput>().text = (float)GetGlobalProperty("DeathYLimit") + "";
            panel.GetChildWithName("SkyboxDropdown").GetComponent<UIDropdownPatcher>().SelectOption((int)GetGlobalProperty("Skybox"));
        }

        public void SetGlobalPropertyWithToggle(string name, UIToggle toggle)
        {
            SetGlobalProperty(name, toggle.isChecked);
        }
        public void SetGlobalPropertyWithInput(string propertyName, UICustomInputField inputField)
        {
            // ParseInputFieldData returns true if the introduced data CAN be parsed.
            if (ParseInputFieldData(inputField.name, inputField.GetText(), out object parsedData))
            {
                EditorController.Instance.levelHasBeenModified = true;
                SetGlobalProperty(propertyName, parsedData);
                inputField.Set(true);
            }
            else
            {
                inputField.Set(false);
            }
        }
        bool ParseInputFieldData(string inputFieldName, string fieldText, out object parsedData)
        {
            switch (inputFieldName)
            {
                case "DeathYLimit":
                    bool toReturn = Utilities.TryParseFloat(fieldText, out float result);
                    parsedData = result;
                    return toReturn;
            }

            parsedData = null;
            return false;
        }
        public void SetGlobalPropertyWithDropdown(string propertyName, int selectedID)
        {
            SetGlobalProperty(propertyName, selectedID);
        }
        public void SetGlobalProperty(string name, object value)
        {
            if (EditorController.Instance.globalProperties.ContainsKey(name))
            {
                if (EditorController.Instance.globalProperties[name].GetType().Name == value.GetType().Name)
                {
                    EditorController.Instance.globalProperties[name] = value;
                    EditorController.Instance.levelHasBeenModified = true;

                    if (name == "Skybox")
                    {
                        EditorController.Instance.SetupSkybox((int)value);
                    }
                }
            }
        }
        public object GetGlobalProperty(string name)
        {
            if (EditorController.Instance.globalProperties.ContainsKey(name))
            {
                return EditorController.Instance.globalProperties[name];
            }

            return null;
        }

        // Methods for "special" UI elements, such as buttons.
        void OnVisualizeDeathYLimitToggleClick(bool newState)
        {
            EditorController.Instance.deathYPlane.gameObject.SetActive(newState);
        }
        #endregion


        public void SetupPauseWhenInEditor()
        {
            // Setup the resume button, to actually resume the editor scene and not load another scene, which is the defualt behaviour of that button.
            GameObject originalResumeBtn = pauseMenu.GetChildAt("LargeButtons/1_Resume");
            GameObject resumeBtnWhenInsideLE = Instantiate(originalResumeBtn, originalResumeBtn.transform.parent);
            resumeBtnWhenInsideLE.name = "1_ResumeWhenInEditor";
            Destroy(resumeBtnWhenInsideLE.GetComponent<ButtonController>());
            resumeBtnWhenInsideLE.GetComponent<UIButton>().onClick.Add(new EventDelegate(this, nameof(EditorUIManager.Resume)));
            // This two more lines are used just in case the original resume button is disabled, that may happen when you didn't start a new game yet.
            if (!resumeBtnWhenInsideLE.GetComponent<UIButton>().isEnabled)
            {
                resumeBtnWhenInsideLE.GetComponent<UIButton>().isEnabled = true;
                resumeBtnWhenInsideLE.GetComponent<UIButton>().ResetDefaultColor();
            }
            resumeBtnWhenInsideLE.SetActive(true);

            // Same with exit button.
            GameObject originalExitBtn = pauseMenu.GetChildAt("LargeButtons/8_ExitGame");
            GameObject exitBtnWhenInsideLE = Instantiate(originalExitBtn, originalExitBtn.transform.parent);
            exitBtnWhenInsideLE.name = "7_ExitWhenInEditor";
            Destroy(exitBtnWhenInsideLE.GetComponent<ButtonController>());
            exitBtnWhenInsideLE.GetComponent<UIButton>().onClick.Add(new EventDelegate(this, nameof(ShowExitPopup)));
            exitBtnWhenInsideLE?.SetActive(true);

            // Create a save level button.
            GameObject saveLevelButton = Instantiate(originalResumeBtn, originalResumeBtn.transform.parent);
            saveLevelButton.name = "3_SaveLevel";
            Destroy(saveLevelButton.GetComponent<ButtonController>());
            Destroy(saveLevelButton.GetChildWithName("Label").GetComponent<UILocalize>());
            saveLevelButton.GetChildWithName("Label").GetComponent<UILabel>().text = "Save Level";
            saveLevelButton.GetComponent<UIButton>().onClick.Add(new EventDelegate(this, nameof(SaveLevelWithPauseMenuButton)));
            saveLevelButton.SetActive(true);

            // Create a PLAY level button.
            //GameObject playLevelButtonTemplate = pauseMenu.GetChildAt("LargeButtons/2_Chapters");
            GameObject playLevelButton = Instantiate(originalResumeBtn, originalResumeBtn.transform.parent);
            playLevelButton.name = "2_PlayLevel";
            Destroy(playLevelButton.GetComponent<ButtonController>());
            Destroy(playLevelButton.GetChildWithName("Label").GetComponent<UILocalize>());
            playLevelButton.GetChildWithName("Label").GetComponent<UILabel>().text = "Play Level";
            playLevelButton.GetComponent<UIButton>().onClick.Add(new EventDelegate(this, nameof(EditorUIManager.PlayLevel)));
            playLevelButton.SetActive(true);

            // A custom script to make the damn large buttons be the correct ones, resume, options and exit, that's all.
            // EDIT: Also to patch and do some stuff in the pause menu while in LE.
            pauseMenu.AddComponent<EditorPauseMenuPatcher>();
        }
        public void ShowPause()
        {
            // Disable the editor UI and enable the navigation bar.
            editorUIParent.SetActive(false);
            navigation.SetActive(true);

            Utilities.PlayFSUISound(Utilities.FS_UISound.SHOW_NEW_PAGE_SOUND);

            // Set the occluder color, it's opaque by defualt for some reason (Anyways, Charles and his weird systems...).
            occluderForWhenPaused.GetComponent<UISprite>().color = new Color(0f, 0f, 0f, 0.9f);

            // Enable the pause panel and play its animations.
            pauseMenu.SetActive(true);
            TweenAlpha.Begin(pauseMenu, 0.2f, 1f);

            // Set the paused variable in the LE controller.
            EditorController.Instance.SetCurrentEditorState(EditorState.PAUSED);

            Logger.Log("LE paused!");
        }
        public void Resume()
        {
            // If you're resuming BUT if the pause menu is disabled itself, then is likely cause the user is in another submenu (like options), in that cases.. don't do anything.
            if (!pauseMenu.activeSelf) return;

            // If the user is in the exit confirmation popup, just hide it and do nothing.
            if (exitPopupEnabled)
            {
                OnExitPopupBackButton();
                return;
            }

            MelonCoroutines.Start(Coroutine());

            IEnumerator Coroutine()
            {
                // Disable the navigation bar.
                navigation.SetActive(false);

                // Play the pause menu animations backwards.
                TweenAlpha.Begin(pauseMenu, 0.2f, 0f);

                // Threshold to wait for the pause animation to end.
                yield return new WaitForSecondsRealtime(0.3f);

                // Enable the LE UI and disable the pause menu.
                editorUIParent.SetActive(true);
                pauseMenu.SetActive(false);

                // And set the paused variable in the controller as false.
                EditorController.Instance.SetCurrentEditorState(EditorState.NORMAL);
            }

            Logger.Log("LE resumed!");
        }

        public void ShowExitPopup()
        {
            if (!EditorController.Instance.levelHasBeenModified)
            {
                ExitToMenu(false);
                return;
            }

            popupTitle.GetComponent<UILabel>().text = "Warning";
            popupContentLabel.GetComponent<UILabel>().text = "Warning, exiting will erase your last saved changes if you made any before saving, are you sure you want to continue?";
            popupSmallButtonsParent.DisableAllChildren();
            popupSmallButtonsParent.transform.localPosition = new Vector3(-10f, -315f, 0f);
            popupSmallButtonsParent.GetComponent<UITable>().padding = new Vector2(10f, 0f);

            // Make a copy of the yess button since for some reason the yes button is red as the no button should, that's doesn't make any sense lol.
            onExitPopupBackButton = Instantiate(popupSmallButtonsParent.GetChildAt("3_Yes"), popupSmallButtonsParent.transform);
            onExitPopupBackButton.name = "1_Back";
            onExitPopupBackButton.transform.localPosition = new Vector3(-400f, 0f, 0f);
            Destroy(onExitPopupBackButton.GetComponent<ButtonController>());
            Destroy(onExitPopupBackButton.GetChildWithName("Label").GetComponent<UILocalize>());
            onExitPopupBackButton.GetChildWithName("Label").GetComponent<UILabel>().text = "No";
            onExitPopupBackButton.GetComponent<UIButtonScale>().hover = Vector3.one * 1.1f;
            onExitPopupBackButton.GetComponent<UIButton>().onClick.Clear();
            onExitPopupBackButton.GetComponent<UIButton>().onClick.Add(new EventDelegate(this, nameof(OnExitPopupBackButton)));
            onExitPopupBackButton.SetActive(true);

            onExitPopupSaveAndExitButton = Instantiate(popupSmallButtonsParent.GetChildAt("3_Yes"), popupSmallButtonsParent.transform);
            onExitPopupSaveAndExitButton.name = "2_SaveAndExit";
            onExitPopupSaveAndExitButton.transform.localPosition = new Vector3(-400f, 0f, 0f);
            Destroy(onExitPopupSaveAndExitButton.GetComponent<ButtonController>());
            Destroy(onExitPopupSaveAndExitButton.GetChildWithName("Label").GetComponent<UILocalize>());
            onExitPopupSaveAndExitButton.GetChildWithName("Label").GetComponent<UILabel>().text = "Save and Exit";
            onExitPopupSaveAndExitButton.GetComponent<UIButtonScale>().hover = Vector3.one * 1.1f;
            onExitPopupSaveAndExitButton.GetComponent<UIButton>().onClick.Clear();
            onExitPopupSaveAndExitButton.GetComponent<UIButton>().onClick.Add(new EventDelegate(this, nameof(OnExitPopupSaveAndExitButton)));
            onExitPopupSaveAndExitButton.SetActive(true);

            // Same with exit button.
            onExitPopupExitButton = Instantiate(popupSmallButtonsParent.GetChildAt("1_No"), popupSmallButtonsParent.transform);
            onExitPopupExitButton.name = "3_ExitWithoutSaving";
            onExitPopupExitButton.transform.localPosition = new Vector3(200f, 0f, 0f);
            Destroy(onExitPopupExitButton.GetComponent<ButtonController>());
            Destroy(onExitPopupExitButton.GetChildWithName("Label").GetComponent<UILocalize>());
            onExitPopupExitButton.GetChildWithName("Label").GetComponent<UILabel>().text = "Exit without Saving";
            onExitPopupExitButton.GetChildWithName("Label").GetComponent<UILabel>().fontSize = 35; // Since this label is a bit too much large (lol), reduce its font size so it fits.
            onExitPopupExitButton.GetComponent<UIButtonScale>().hover = Vector3.one * 1.1f;
            onExitPopupExitButton.GetComponent<UIButton>().onClick.Clear();
            onExitPopupExitButton.GetComponent<UIButton>().onClick.Add(new EventDelegate(this, nameof(OnExitPopupExitWithoutSavingButton)));
            onExitPopupExitButton.SetActive(true);

            popupController.Show();
            exitPopupEnabled = true;
            Logger.Log("Showed LE exit popup!");
        }
        public void OnExitPopupBackButton()
        {
            popupController.Hide();
            exitPopupEnabled = false;

            Destroy(onExitPopupBackButton);
            Destroy(onExitPopupSaveAndExitButton);
            Destroy(onExitPopupExitButton);

            popupSmallButtonsParent.transform.localPosition = new Vector3(-130f, -315f, 0f);
            popupSmallButtonsParent.GetComponent<UITable>().padding = new Vector2(130f, 0f);
        }
        public void OnExitPopupSaveAndExitButton()
        {
            OnExitPopupBackButton();
            ExitToMenu(true);
        }
        public void OnExitPopupExitWithoutSavingButton()
        {
            OnExitPopupBackButton();
            ExitToMenu(false);
        }

        public void SaveLevelWithPauseMenuButton()
        {
            Logger.Log("Saving Level Data from pause menu...");
            LevelData.SaveLevelData(EditorController.Instance.levelName, EditorController.Instance.levelFileNameWithoutExtension);
            PlaySavingLevelLabel();
            EditorController.Instance.levelHasBeenModified = false;

            // Refresh the pause menu patch after saving...
            pauseMenu.GetComponent<EditorPauseMenuPatcher>().OnEnable();
        }

        public void ExitToMenu(bool saveDataBeforeExit = false)
        {
            MelonCoroutines.Start(Coroutine());

            IEnumerator Coroutine()
            {
                Logger.Log("About to exit from LE to main menu...");

                if (saveDataBeforeExit)
                {
                    // Save data.
                    LevelData.SaveLevelData(EditorController.Instance.levelName, EditorController.Instance.levelFileNameWithoutExtension);
                }

                DeleteUI();

                MenuController.GetInstance().ReturnToMainMenuConfirmed();

                // Wait a few so when the pause menu ui is not visible anymore, destroy the pause menu LE buttons, and it doesn't look weird when destroying them and the user can see it.
                yield return new WaitForSecondsRealtime(0.2f);
                // Remove this component, since this component is only needed when inside of LE.
                pauseMenu.GetComponent<EditorPauseMenuPatcher>().BeforeDestroying();
                pauseMenu.RemoveComponent<EditorPauseMenuPatcher>();
            }
        }

        public void PlayLevel()
        {
            Logger.Log("About to enter playmode from LE pause menu...");

            // Save data automatically.
            LevelData.SaveLevelData(EditorController.Instance.levelName, EditorController.Instance.levelFileNameWithoutExtension);

            EditorController.Instance.EnterPlayMode();
        }
        public void DeleteUI()
        {
            // If the coroutine was already played, stop it if it's currently playing to "restart" it.
            if (savingLevelLabelRoutine != null) MelonCoroutines.Stop(savingLevelLabelRoutine);

            // To avoid bugs, reset the MainMenu UI Camera depth to its default value.
            GameObject.Find("MainMenu/Camera").GetComponent<Camera>().depth = 10;

            Destroy(editorUIParent);
            Destroy(pauseMenu.GetChildWithName("SavingLevelInPauseMenu"));

            Logger.Log("LE UI deleted!");
        }

        public void SetEditorUIContext(EditorUIContext context)
        {
            if (context == EditorUIContext.HELP_PANEL)
            {
                helpPanel.SetActive(true);

                if (currentUIContext == EditorUIContext.GLOBAL_PROPERTIES)
                {
                    TweenPosition.Begin(globalPropertiesPanel, 0.2f, new Vector2(1320, 0));
                }
            }

            // If the user is trying to switch from Events Panel to Normal but the previous context was help panel.
            // Techinically, that SHOULD be impossible since help panel disables all of the buttons to open events panel, but who knows...
            if (context == EditorUIContext.NORMAL && currentUIContext == EditorUIContext.EVENTS_PANEL && previousUIContext == EditorUIContext.HELP_PANEL)
            {
                SetEditorUIContext(EditorUIContext.HELP_PANEL);
                return;
            }

            if (context == EditorUIContext.SELECTING_TARGET_OBJ && currentUIContext == EditorUIContext.EVENTS_PANEL)
            {
                hittenTargetObjPanel.SetActive(true);
                EventsUIPageManager.Instance.StartedSelectingTargetObject(true);
            }
            if (context == EditorUIContext.EVENTS_PANEL && currentUIContext == EditorUIContext.SELECTING_TARGET_OBJ)
            {
                hittenTargetObjPanel.SetActive(false);
                EventsUIPageManager.Instance.StartedSelectingTargetObject(false);
            }

            if (context == EditorUIContext.GLOBAL_PROPERTIES)
            {
                RefreshGlobalPropertiesPanelValues();
                TweenPosition.Begin(globalPropertiesPanel, 0.2f, new Vector2(600, 0));

                if (currentUIContext == EditorUIContext.HELP_PANEL)
                {
                    helpPanel.SetActive(false);
                }
            }

            if (context == EditorUIContext.NORMAL)
            {
                switch (currentUIContext)
                {
                    case EditorUIContext.HELP_PANEL:
                        helpPanel.SetActive(false);
                        break;

                    case EditorUIContext.GLOBAL_PROPERTIES:
                        TweenPosition.Begin(globalPropertiesPanel, 0.2f, new Vector2(1320, 0));
                        break;
                }

                switch (previousUIContext)
                {
                    case EditorUIContext.HELP_PANEL:
                        if (currentUIContext != EditorUIContext.GLOBAL_PROPERTIES) // Avoid an infinite loop with help panel and global properties.
                        {
                            helpPanel.SetActive(true);
                            context = EditorUIContext.HELP_PANEL;
                        }
                        break;

                    case EditorUIContext.GLOBAL_PROPERTIES:
                        TweenPosition.Begin(globalPropertiesPanel, 0.2f, new Vector2(600, 0));
                        context = EditorUIContext.GLOBAL_PROPERTIES;
                        break;
                }
            }

            // Only enable these panels if the current editor mode is building, and the UI is normal.
            if (context != EditorUIContext.NORMAL)
            {
                categoryButtonsParent.SetActive(false);
                currentCategoryBG.SetActive(false);
            }
            else if (EditorController.Instance.currentMode == EditorController.Mode.Building)
            {
                categoryButtonsParent.SetActive(true);
                currentCategoryBG.SetActive(true);
            }
            // Only when normal.
            selectedObjPanel.SetActive(context == EditorUIContext.NORMAL);
            currentModeLabel.gameObject.SetActive(context == EditorUIContext.NORMAL);

            previousUIContext = currentUIContext;
            currentUIContext = context;

            Logger.Log($"Switched Editor UI Context from {previousUIContext} to {currentUIContext}.");
        }
        public static bool IsCurrentUIContext(EditorUIContext context)
        {
            if (Instance == null) return false;

            return Instance.currentUIContext == context;
        }
    }
}
