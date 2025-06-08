using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Il2Cpp;
using Il2CppVLB;
using MelonLoader;
using UnityEngine;
using System.Reflection;
using System.Collections;
using Il2CppInControl.NativeDeviceProfiles;
using static Il2Cpp.UIAtlas;
using FS_LevelEditor.UI_Related;
using System.Xml.Serialization;
using static Il2CppSystem.Linq.Expressions.Interpreter.CastInstruction.CastInstructionNoT;
using UnityEngine.Rendering.PostProcessing;

namespace FS_LevelEditor
{
    [RegisterTypeInIl2Cpp]
    public class EditorUIManager : MonoBehaviour
    {
        public static EditorUIManager Instance;

        public GameObject editorUIParent;

        // This is for the top buttons, like "Structures", "Decorations", "System", etc.
        public List<GameObject> categoryButtons = new List<GameObject>();
        public GameObject categoryButtonsParent;
        bool categoryButtonsAreHidden = false;

        public GameObject currentCategoryBG;
        List<GameObject> currentCategoryButtons = new List<GameObject>();

        public GameObject selectedObjPanel;
        Transform objectSpecificPanelsParent;
        Transform globalObjectPanelsParent;
        GameObject globalObjAttributesToggle;
        Dictionary<string, GameObject> attrbutesPanels = new Dictionary<string, GameObject>();

        GameObject savingLevelLabel;
        GameObject savingLevelLabelInPauseMenu;
        Coroutine savingLevelLabelRoutine;

        public UILabel currentModeLabel;

        GameObject onExitPopupBackButton;
        GameObject onExitPopupSaveAndExitButton;
        GameObject onExitPopupExitButton;
        bool exitPopupEnabled = false;

        public GameObject helpPanel;

        GameObject globalPropertiesPanel;
        public bool isShowingGlobalProperties;

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
            occluderForWhenPaused.SetActive(EditorController.Instance.isEditorPaused);
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
            GameObject template = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Background");
            GameObject labelTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Label");

            selectedObjPanel = new GameObject("CurrentSelectedObjPanel");
            selectedObjPanel.transform.parent = editorUIParent.transform;
            selectedObjPanel.transform.localPosition = new Vector3(-700f, -220f, 0f);
            selectedObjPanel.transform.localScale = Vector3.one;

            UISprite headerSprite = selectedObjPanel.AddComponent<UISprite>();
            headerSprite.atlas = template.GetComponent<UISprite>().atlas;
            headerSprite.spriteName = "Square_Border_Beveled_HighOpacity";
            headerSprite.type = UIBasicSprite.Type.Sliced;
            headerSprite.color = new Color(0.218f, 0.6464f, 0.6509f, 1f);
            headerSprite.width = 520;
            headerSprite.height = 60;

            BoxCollider headerCollider = selectedObjPanel.AddComponent<BoxCollider>();
            headerCollider.size = new Vector3(520f, 60f, 1f);

            GameObject headerText = new GameObject("Label");
            headerText.transform.parent = selectedObjPanel.transform;
            headerText.transform.localPosition = Vector3.zero;
            headerText.transform.localScale = Vector3.one;

            UILabel headerLabel = headerText.AddComponent<UILabel>();
            headerLabel.font = labelTemplate.GetComponent<UILabel>().font;
            headerLabel.fontSize = 27;
            headerLabel.text = "No Object Selected";
            headerLabel.depth = 1;
            headerLabel.width = 520;
            headerLabel.height = 60;

            GameObject setActiveAtStartToggle = NGUI_Utils.CreateToggle(selectedObjPanel.transform, new Vector3(-220f, 0f, 0f), new Vector3Int(48, 48, 0));
            setActiveAtStartToggle.name = "SetActiveAtStartToggle";
            setActiveAtStartToggle.GetComponent<UIToggle>().onChange.Clear();
            var activateOnStartDelegate = NGUI_Utils.CreateEvenDelegate(this, nameof(SetSetActiveAtStart),
                NGUI_Utils.CreateEventDelegateParamter(this, "toggle", setActiveAtStartToggle.GetComponent<UIToggle>()));
            setActiveAtStartToggle.GetComponent<UIToggle>().onChange.Add(activateOnStartDelegate);
            setActiveAtStartToggle.SetActive(false);

            globalObjAttributesToggle = NGUI_Utils.CreateButtonAsToggleWithSprite(selectedObjPanel.transform, new Vector3(220f, 0f, 0f), new Vector3Int(45, 45, 0), 2, "Global",
                Vector2Int.one * 25);
            globalObjAttributesToggle.name = "GlobalObjectAttributesBtnToggle";
            globalObjAttributesToggle.GetComponent<UIButtonAsToggle>().onClick += ShowGlobalObjectAttributes;
            globalObjAttributesToggle.SetActive(false);

            GameObject selectedObjPanelBody = new GameObject("Body");
            selectedObjPanelBody.transform.parent = selectedObjPanel.transform;
            selectedObjPanelBody.transform.localPosition = new Vector3(0f, -160f, 0f);
            selectedObjPanelBody.transform.localScale = Vector3.one;

            UISprite bodySprite = selectedObjPanelBody.AddComponent<UISprite>();
            bodySprite.atlas = template.GetComponent<UISprite>().atlas;
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
            CreateNoAttributesPanel();
            CreateLightAttributesPanel();
            CreateSawAttributesPanel();
            CreateSawWaypointAttributesPanel();
            CreateSwitchAttributesPanel();
            CreateAmmoAndHealthPackAttributesPanel();
            CreateLaserAttributesPanel();
            CreateCeilingLightPanel();
        }

        void CreateGlobalObjectAttributesPanel()
        {
            GameObject labelTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Label");

            #region Position Input Field
            UILabel positionTitle = NGUI_Utils.CreateLabel(globalObjectPanelsParent, new Vector3(-230f, 90f, 0f), new Vector3Int(150, 38, 0), "Position");
            positionTitle.name = "PositionTitle";

            UILabel xPositionTitle = NGUI_Utils.CreateLabel(globalObjectPanelsParent, new Vector3(-40f, 90f, 0f), new Vector3Int(28, 38, 0), "X", NGUIText.Alignment.Center,
                UIWidget.Pivot.Center);
            xPositionTitle.name = "XPositionTitle";
            GameObject xPositionField = NGUI_Utils.CreateInputField(globalObjectPanelsParent, new Vector3(10f, 90f, 0f), new Vector3Int(65, 38, 0), 27, "0");
            xPositionField.name = "XPositionField";
            var xPositionFieldScript = xPositionField.AddComponent<UICustomInputField>();
            xPositionFieldScript.Setup(UICustomInputField.UIInputType.FLOAT, maxDecimals:2);
            xPositionFieldScript.onChange += (() => SetPropertyWithInput("XPosition", xPositionFieldScript));

            UILabel yPositionTitle = NGUI_Utils.CreateLabel(globalObjectPanelsParent, new Vector3(60f, 90f, 0f), new Vector3Int(28, 38, 0), "Y", NGUIText.Alignment.Center,
                UIWidget.Pivot.Center);
            yPositionTitle.name = "YPositionTitle";
            GameObject yPositionField = NGUI_Utils.CreateInputField(globalObjectPanelsParent, new Vector3(110f, 90f, 0f), new Vector3Int(65, 38, 0), 27, "0");
            yPositionField.name = "YPositionField";
            var yPositionFieldScript = yPositionField.AddComponent<UICustomInputField>();
            yPositionFieldScript.Setup(UICustomInputField.UIInputType.FLOAT, maxDecimals:2);
            yPositionFieldScript.onChange += (() => SetPropertyWithInput("YPosition", yPositionFieldScript));

            UILabel zPositionTitle = NGUI_Utils.CreateLabel(globalObjectPanelsParent, new Vector3(160f, 90f, 0f), new Vector3Int(28, 38, 0), "Z", NGUIText.Alignment.Center,
                UIWidget.Pivot.Center);
            zPositionTitle.name = "ZPositionTitle";
            GameObject zPositionField = NGUI_Utils.CreateInputField(globalObjectPanelsParent, new Vector3(210f, 90f, 0f), new Vector3Int(65, 38, 0), 27, "0");
            zPositionField.name = "ZPositionField";
            var zPositionFieldScript = zPositionField.AddComponent<UICustomInputField>();
            zPositionFieldScript.Setup(UICustomInputField.UIInputType.FLOAT, maxDecimals:2);
            zPositionFieldScript.onChange += (() => SetPropertyWithInput("ZPosition", zPositionFieldScript));
            #endregion
        }

        void CreateNoAttributesPanel()
        {
            GameObject labelTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Label");

            GameObject noAttributes = new GameObject("NoAttributes");
            noAttributes.transform.parent = objectSpecificPanelsParent;
            noAttributes.transform.localPosition = Vector3.zero;
            noAttributes.transform.localScale = Vector3.one;

            UILabel label = noAttributes.AddComponent<UILabel>();
            label.font = labelTemplate.GetComponent<UILabel>().font;
            label.fontSize = 27;
            label.width = 500;
            label.height = 300;
            label.text = "No Attributes for this object.";

            noAttributes.SetActive(false);
            attrbutesPanels.Add("None", noAttributes);
        }
        void CreateLightAttributesPanel()
        {
            GameObject labelTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Label");

            GameObject lightAttributes = new GameObject("LightAttributes");
            lightAttributes.transform.parent = objectSpecificPanelsParent;
            lightAttributes.transform.localPosition = Vector3.zero;
            lightAttributes.transform.localScale = Vector3.one;

            #region Color Input Field
            GameObject colorTitle = Instantiate(labelTemplate, lightAttributes.transform);
            colorTitle.name = "ColorTitle";
            colorTitle.transform.localPosition = new Vector3(-230f, 90f, 0f);
            colorTitle.RemoveComponent<UILocalize>();
            colorTitle.GetComponent<UILabel>().width = 235;
            colorTitle.GetComponent<UILabel>().text = "Color (Hex)";
            colorTitle.GetComponent<UILabel>().color = Color.white;

            GameObject hashtagLOL = Instantiate(labelTemplate, lightAttributes.transform);
            hashtagLOL.name = "ColorHashtag";
            hashtagLOL.transform.localPosition = new Vector3(15f, 90f, 0f);
            hashtagLOL.RemoveComponent<UILocalize>();
            hashtagLOL.GetComponent<UILabel>().text = "#";
            hashtagLOL.GetComponent<UILabel>().color = Color.white;
            hashtagLOL.GetComponent<UILabel>().alignment = NGUIText.Alignment.Center;
            hashtagLOL.GetComponent<UILabel>().width = 20;

            GameObject colorInputField = NGUI_Utils.CreateInputField(lightAttributes.transform, new Vector3(140f, 90f, 0f), new Vector3Int(200, 38, 0), 27,
                "FFFFFF", false, NGUIText.Alignment.Left);
            colorInputField.name = "ColorField";
            var colorFieldCustomScript = colorInputField.AddComponent<UICustomInputField>();
            colorFieldCustomScript.Setup(UICustomInputField.UIInputType.HEX_COLOR);
            colorFieldCustomScript.setFieldColorAutomatically = false;
            colorFieldCustomScript.onChange += (() => SetPropertyWithInput("Color", colorFieldCustomScript));
            #endregion

            #region Intensity Input Field
            GameObject intensityTitle = Instantiate(labelTemplate, lightAttributes.transform);
            intensityTitle.name = "IntensityTitle";
            intensityTitle.transform.localPosition = new Vector3(-230f, 40f, 0f);
            intensityTitle.RemoveComponent<UILocalize>();
            intensityTitle.GetComponent<UILabel>().width = 260;
            intensityTitle.GetComponent<UILabel>().text = "Intensity";
            intensityTitle.GetComponent<UILabel>().color = Color.white;

            GameObject intensityInputField = NGUI_Utils.CreateInputField(lightAttributes.transform, new Vector3(140f, 40f, 0f), new Vector3Int(200, 38, 0), 27,
                "1", false, NGUIText.Alignment.Left);
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
            GameObject toggleTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles");
            GameObject labelTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Label");

            GameObject sawAttributes = new GameObject("SawAttributes");
            sawAttributes.transform.parent = objectSpecificPanelsParent;
            sawAttributes.transform.localPosition = Vector3.zero;
            sawAttributes.transform.localScale = Vector3.one;

            #region Activate On Start Toggle
            GameObject activateOnStartTitle = Instantiate(labelTemplate, sawAttributes.transform);
            activateOnStartTitle.name = "ActivateOnStartTitle";
            activateOnStartTitle.transform.localPosition = new Vector3(-230f, 90f, 0f);
            activateOnStartTitle.RemoveComponent<UILocalize>();
            activateOnStartTitle.GetComponent<UILabel>().width = 395;
            activateOnStartTitle.GetComponent<UILabel>().text = "Activate On Start";
            activateOnStartTitle.GetComponent<UILabel>().color = Color.white;

            GameObject activateOnStartToggle = NGUI_Utils.CreateToggle(sawAttributes.transform, new Vector3(200f, 90f, 0f), new Vector3Int(48, 48, 0));
            activateOnStartToggle.name = "ActivateOnStartToggle";
            activateOnStartToggle.GetComponent<UIToggle>().onChange.Clear();
            var activateOnStartDelegate = NGUI_Utils.CreateEvenDelegate(this, nameof(SetPropertyWithToggle),
                NGUI_Utils.CreateEventDelegateParamter(this, "propertyName", "ActivateOnStart"),
                NGUI_Utils.CreateEventDelegateParamter(this, "toggle", activateOnStartToggle.GetComponent<UIToggle>()));
            activateOnStartToggle.GetComponent<UIToggle>().onChange.Add(activateOnStartDelegate);
            #endregion

            #region Damage Input Field
            GameObject damageTitle = Instantiate(labelTemplate, sawAttributes.transform);
            damageTitle.name = "DamageTitle";
            damageTitle.transform.localPosition = new Vector3(-230f, 40f, 0f);
            damageTitle.RemoveComponent<UILocalize>();
            damageTitle.GetComponent<UILabel>().width = 260;
            damageTitle.GetComponent<UILabel>().text = "Damage";
            damageTitle.GetComponent<UILabel>().color = Color.white;

            GameObject damageInputField = NGUI_Utils.CreateInputField(sawAttributes.transform, new Vector3(140f, 40f, 0f), new Vector3Int(200, 38, 0), 27,
                "50", false, NGUIText.Alignment.Left);
            damageInputField.name = "DamageInputField";
            var damangeFieldCustomScript = damageInputField.AddComponent<UICustomInputField>();
            damangeFieldCustomScript.Setup(UICustomInputField.UIInputType.NON_NEGATIVE_INT);
            damangeFieldCustomScript.onChange += (() => SetPropertyWithInput("Damage", damangeFieldCustomScript));
            #endregion

            #region Add Waypoint
            GameObject addWaypoint = NGUI_Utils.CreateButton(sawAttributes.transform, new Vector3(0f, -15f, 0f), new Vector3Int(480, 55, 0), "+ Add Waypoint");
            addWaypoint.name = "AddWaypointButton";
            addWaypoint.GetComponent<UIButton>().onClick.Clear();
            var addWaypointDelegate = NGUI_Utils.CreateEvenDelegate(this, nameof(TriggerAction),
                NGUI_Utils.CreateEventDelegateParamter(this, "actionName", "AddWaypoint"));
            addWaypoint.GetComponent<UIButton>().onClick.Add(addWaypointDelegate);
            addWaypoint.GetComponent<UIButtonScale>().hover = Vector3.one * 1.05f;
            addWaypoint.GetComponent<UIButtonScale>().pressed = Vector3.one * 1.02f;
            #endregion

            sawAttributes.SetActive(false);
            attrbutesPanels.Add("Saw", sawAttributes);
        }
        void CreateSawWaypointAttributesPanel()
        {
            GameObject toggleTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles");
            GameObject labelTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Label");

            GameObject sawWaypointAttributes = new GameObject("SawWaypointAttributes");
            sawWaypointAttributes.transform.parent = objectSpecificPanelsParent;
            sawWaypointAttributes.transform.localPosition = Vector3.zero;
            sawWaypointAttributes.transform.localScale = Vector3.one;

            #region Wait Time Input Field
            GameObject waitTimeTitle = Instantiate(labelTemplate, sawWaypointAttributes.transform);
            waitTimeTitle.name = "WaitTimeTitle";
            waitTimeTitle.transform.localPosition = new Vector3(-230f, 90f, 0f);
            waitTimeTitle.RemoveComponent<UILocalize>();
            waitTimeTitle.GetComponent<UILabel>().width = 260;
            waitTimeTitle.GetComponent<UILabel>().text = "Wait Time";
            waitTimeTitle.GetComponent<UILabel>().color = Color.white;

            GameObject waitTimeInputField = NGUI_Utils.CreateInputField(sawWaypointAttributes.transform, new Vector3(140f, 90f, 0f), new Vector3Int(200, 38, 0), 27,
                "0.3", false, NGUIText.Alignment.Left);
            waitTimeInputField.name = "WaitTimeInputField";
            var waitTimeFieldCustomScript = waitTimeInputField.AddComponent<UICustomInputField>();
            waitTimeFieldCustomScript.Setup(UICustomInputField.UIInputType.NON_NEGATIVE_FLOAT);
            waitTimeFieldCustomScript.onChange += (() => SetPropertyWithInput("WaitTime", waitTimeFieldCustomScript));
            #endregion

            #region Add Waypoint
            GameObject addWaypoint = NGUI_Utils.CreateButton(sawWaypointAttributes.transform, new Vector3(0f, 35f, 0f), new Vector3Int(480, 55, 0), "+ Add Waypoint");
            addWaypoint.name = "AddWaypointButton";
            addWaypoint.GetComponent<UIButton>().onClick.Clear();
            var addWaypointDelegate = NGUI_Utils.CreateEvenDelegate(this, nameof(TriggerAction),
                NGUI_Utils.CreateEventDelegateParamter(this, "actionName", "AddWaypoint"));
            addWaypoint.GetComponent<UIButton>().onClick.Add(addWaypointDelegate);
            addWaypoint.GetComponent<UIButtonScale>().hover = Vector3.one * 1.05f;
            addWaypoint.GetComponent<UIButtonScale>().pressed = Vector3.one * 1.02f;
            #endregion

            sawWaypointAttributes.SetActive(false);
            attrbutesPanels.Add("SawWaypoint", sawWaypointAttributes);
        }
        void CreateSwitchAttributesPanel()
        {
            GameObject toggleTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles");
            GameObject labelTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Label");

            GameObject switchAttributes = new GameObject("SwitchAttributes");
            switchAttributes.transform.parent = objectSpecificPanelsParent;
            switchAttributes.transform.localPosition = Vector3.zero;
            switchAttributes.transform.localScale = Vector3.one;

            #region Usable Once Toggle
            GameObject usableOnceTitle = Instantiate(labelTemplate, switchAttributes.transform);
            usableOnceTitle.name = "UsableOnceTitle";
            usableOnceTitle.transform.localPosition = new Vector3(-230f, 90f, 0f);
            usableOnceTitle.RemoveComponent<UILocalize>();
            usableOnceTitle.GetComponent<UILabel>().width = 395;
            usableOnceTitle.GetComponent<UILabel>().text = "Usable Once";
            usableOnceTitle.GetComponent<UILabel>().color = Color.white;

            GameObject usableOnceToggle = NGUI_Utils.CreateToggle(switchAttributes.transform, new Vector3(200f, 90f, 0f), new Vector3Int(48, 48, 0));
            usableOnceToggle.name = "UsableOnceToggle";
            usableOnceToggle.GetComponent<UIToggle>().onChange.Clear();
            var usableOnceDelegate = NGUI_Utils.CreateEvenDelegate(this, nameof(SetPropertyWithToggle),
                NGUI_Utils.CreateEventDelegateParamter(this, "propertyName", "UsableOnce"),
                NGUI_Utils.CreateEventDelegateParamter(this, "toggle", usableOnceToggle.GetComponent<UIToggle>()));
            usableOnceToggle.GetComponent<UIToggle>().onChange.Add(usableOnceDelegate);
            #endregion

            #region Can Use Taser Toggle
            GameObject canUseTaserTitle = Instantiate(labelTemplate, switchAttributes.transform);
            canUseTaserTitle.transform.localPosition = new Vector3(-230f, 35f, 0f);
            canUseTaserTitle.name = "CanUseTaserTitle";
            canUseTaserTitle.RemoveComponent<UILocalize>();
            canUseTaserTitle.GetComponent<UILabel>().width = 395;
            canUseTaserTitle.GetComponent<UILabel>().text = "Can be shot by Taser";
            canUseTaserTitle.GetComponent<UILabel>().color = Color.white;

            GameObject canUseTaserToggle = NGUI_Utils.CreateToggle(switchAttributes.transform, new Vector3(200f, 35f, 0f), new Vector3Int(48, 48, 0));
            canUseTaserToggle.name = "CanUseTaserToggle";
            canUseTaserToggle.GetComponent<UIToggle>().onChange.Clear();
            var canUseTaserDelegate = NGUI_Utils.CreateEvenDelegate(this, nameof(SetPropertyWithToggle),
                NGUI_Utils.CreateEventDelegateParamter(this, "propertyName", "CanUseTaser"),
                NGUI_Utils.CreateEventDelegateParamter(this, "toggle", canUseTaserToggle.GetComponent<UIToggle>()));
            canUseTaserToggle.GetComponent<UIToggle>().onChange.Add(canUseTaserDelegate);
            #endregion

            #region Manage Events
            GameObject manageEvents = NGUI_Utils.CreateButton(switchAttributes.transform, new Vector3(0f, -20f, 0f), new Vector3Int(480, 55, 0), "Manage Events");
            manageEvents.name = "ManageEventsButton";
            manageEvents.GetComponent<UIButton>().onClick.Clear();
            var addWaypointDelegate = NGUI_Utils.CreateEvenDelegate(this, nameof(TriggerAction),
                NGUI_Utils.CreateEventDelegateParamter(this, "actionName", "ManageEvents"));
            manageEvents.GetComponent<UIButton>().onClick.Add(addWaypointDelegate);
            manageEvents.GetComponent<UIButtonScale>().hover = Vector3.one * 1.05f;
            manageEvents.GetComponent<UIButtonScale>().pressed = Vector3.one * 1.02f;
            #endregion

            switchAttributes.SetActive(false);
            attrbutesPanels.Add("Switch", switchAttributes);
        }
        void CreateAmmoAndHealthPackAttributesPanel()
        {
            GameObject toggleTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles");
            GameObject labelTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Label");

            GameObject ammoHealthAttributes = new GameObject("AmmoAndHealthPackAttributes");
            ammoHealthAttributes.transform.parent = objectSpecificPanelsParent;
            ammoHealthAttributes.transform.localPosition = Vector3.zero;
            ammoHealthAttributes.transform.localScale = Vector3.one;

            #region Respawn Time Input Field
            GameObject respawnTitle = Instantiate(labelTemplate, ammoHealthAttributes.transform);
            respawnTitle.name = "RespawnTitle";
            respawnTitle.transform.localPosition = new Vector3(-230f, 90f, 0f);
            respawnTitle.RemoveComponent<UILocalize>();
            respawnTitle.GetComponent<UILabel>().width = 260;
            respawnTitle.GetComponent<UILabel>().text = "Respawn Time";
            respawnTitle.GetComponent<UILabel>().color = Color.white;

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
            GameObject toggleTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles");
            GameObject labelTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Label");

            GameObject laserAttributes = new GameObject("LaserAttributes");
            laserAttributes.transform.parent = objectSpecificPanelsParent;
            laserAttributes.transform.localPosition = Vector3.zero;
            laserAttributes.transform.localScale = Vector3.one;

            #region Activate On Start Toggle
            GameObject activateOnStartTitle = Instantiate(labelTemplate, laserAttributes.transform);
            activateOnStartTitle.name = "ActivateOnStartTitle";
            activateOnStartTitle.transform.localPosition = new Vector3(-230f, 90f, 0f);
            activateOnStartTitle.RemoveComponent<UILocalize>();
            activateOnStartTitle.GetComponent<UILabel>().width = 395;
            activateOnStartTitle.GetComponent<UILabel>().text = "Activate On Start";
            activateOnStartTitle.GetComponent<UILabel>().color = Color.white;

            GameObject activateOnStartToggle = NGUI_Utils.CreateToggle(laserAttributes.transform, new Vector3(200f, 90f, 0f), new Vector3Int(48, 48, 0));
            activateOnStartToggle.name = "ActivateOnStartToggle";
            activateOnStartToggle.GetComponent<UIToggle>().onChange.Clear();
            var activateOnStartDelegate = NGUI_Utils.CreateEvenDelegate(this, nameof(SetPropertyWithToggle),
                NGUI_Utils.CreateEventDelegateParamter(this, "propertyName", "ActivateOnStart"),
                NGUI_Utils.CreateEventDelegateParamter(this, "toggle", activateOnStartToggle.GetComponent<UIToggle>()));
            activateOnStartToggle.GetComponent<UIToggle>().onChange.Add(activateOnStartDelegate);
            #endregion

            #region Damage Input Field
            GameObject damageTitle = Instantiate(labelTemplate, laserAttributes.transform);
            damageTitle.name = "DamageTitle";
            damageTitle.transform.localPosition = new Vector3(-230f, 40f, 0f);
            damageTitle.RemoveComponent<UILocalize>();
            damageTitle.GetComponent<UILabel>().width = 260;
            damageTitle.GetComponent<UILabel>().text = "Damage";
            damageTitle.GetComponent<UILabel>().color = Color.white;

            GameObject damageInputField = NGUI_Utils.CreateInputField(laserAttributes.transform, new Vector3(140f, 40f, 0f), new Vector3Int(200, 38, 0), 27,
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
            GameObject toggleTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles");
            GameObject labelTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Label");

            GameObject ceilingLightAttributes = new GameObject("CeilingLightAttributes");
            ceilingLightAttributes.transform.parent = objectSpecificPanelsParent;
            ceilingLightAttributes.transform.localPosition = Vector3.zero;
            ceilingLightAttributes.transform.localScale = Vector3.one;

            #region Activate On Start Toggle
            GameObject activateOnStartTitle = Instantiate(labelTemplate, ceilingLightAttributes.transform);
            activateOnStartTitle.name = "ActivateOnStartTitle";
            activateOnStartTitle.transform.localPosition = new Vector3(-230f, 90f, 0f);
            activateOnStartTitle.RemoveComponent<UILocalize>();
            activateOnStartTitle.GetComponent<UILabel>().width = 395;
            activateOnStartTitle.GetComponent<UILabel>().text = "Activate On Start";
            activateOnStartTitle.GetComponent<UILabel>().color = Color.white;

            GameObject activateOnStartToggle = NGUI_Utils.CreateToggle(ceilingLightAttributes.transform, new Vector3(200f, 90f, 0f), new Vector3Int(48, 48, 0));
            activateOnStartToggle.name = "ActivateOnStartToggle";
            activateOnStartToggle.GetComponent<UIToggle>().onChange.Clear();
            var activateOnStartDelegate = NGUI_Utils.CreateEvenDelegate(this, nameof(SetPropertyWithToggle),
                NGUI_Utils.CreateEventDelegateParamter(this, "propertyName", "ActivateOnStart"),
                NGUI_Utils.CreateEventDelegateParamter(this, "toggle", activateOnStartToggle.GetComponent<UIToggle>()));
            activateOnStartToggle.GetComponent<UIToggle>().onChange.Add(activateOnStartDelegate);
            #endregion

            #region Color Input Field
            GameObject colorTitle = Instantiate(labelTemplate, ceilingLightAttributes.transform);
            colorTitle.name = "ColorTitle";
            colorTitle.transform.localPosition = new Vector3(-230f, 40f, 0f);
            colorTitle.RemoveComponent<UILocalize>();
            colorTitle.GetComponent<UILabel>().width = 235;
            colorTitle.GetComponent<UILabel>().text = "Color (Hex)";
            colorTitle.GetComponent<UILabel>().color = Color.white;

            GameObject hashtagLOL = Instantiate(labelTemplate, ceilingLightAttributes.transform);
            hashtagLOL.name = "ColorHashtag";
            hashtagLOL.transform.localPosition = new Vector3(15f, 40f, 0f);
            hashtagLOL.RemoveComponent<UILocalize>();
            hashtagLOL.GetComponent<UILabel>().text = "#";
            hashtagLOL.GetComponent<UILabel>().color = Color.white;
            hashtagLOL.GetComponent<UILabel>().alignment = NGUIText.Alignment.Center;
            hashtagLOL.GetComponent<UILabel>().width = 20;

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

        public void SetSelectedObjPanelAsNone()
        {
            selectedObjPanel.GetChildWithName("Label").GetComponent<UILabel>().text = "No Object Selected";
            selectedObjPanel.GetChildWithName("SetActiveAtStartToggle").SetActive(false);
            globalObjAttributesToggle.SetActive(false);
            selectedObjPanel.GetChildWithName("Body").SetActive(false);
            selectedObjPanel.transform.localPosition = new Vector3(-700f, -505f, 0f);
        }
        public void SetMultipleObjectsSelected()
        {
            selectedObjPanel.GetChildWithName("Label").GetComponent<UILabel>().text = "Multiple Objects Selected";
            selectedObjPanel.GetChildWithName("SetActiveAtStartToggle").SetActive(false);
            globalObjAttributesToggle.SetActive(false);
            selectedObjPanel.GetChildWithName("Body").SetActive(false);
            selectedObjPanel.transform.localPosition = new Vector3(-700f, -505f, 0f);
        }
        public void SetSelectedObject(LE_Object objComponent)
        {
            selectedObjPanel.GetChildWithName("Label").GetComponent<UILabel>().text = objComponent.objectFullNameWithID;
            selectedObjPanel.GetChildWithName("Body").SetActive(true);
            selectedObjPanel.transform.localPosition = new Vector3(-700f, -220, 0f);

            attrbutesPanels.ToList().ForEach(x => x.Value.SetActive(false));

            // Enable the toggle and show object-specific attributes, then it will be disabled or changed to GLOBAL attributes if the object doesn't have unique ones.
            globalObjAttributesToggle.SetActive(true);
            globalObjAttributesToggle.GetComponent<UIButtonAsToggle>().SetToggleState(false, true);

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
            else
            {
                globalObjAttributesToggle.SetActive(false);
                globalObjAttributesToggle.GetComponent<UIButtonAsToggle>().SetToggleState(true, true);
            }

            if (objComponent.canBeDisabledAtStart)
            {
                selectedObjPanel.GetChildWithName("SetActiveAtStartToggle").SetActive(true);
                selectedObjPanel.GetChildWithName("SetActiveAtStartToggle").GetComponent<UIToggle>().Set(objComponent.setActiveAtStart);
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
        public void UpdateGlobalObjectAttributes(LE_Object objComponent)
        {
            // UICustomInput already verifies if the user is typing on the field, if so, SetText does nothing, we don't need to worry about that.

            // Set Global Attributes...
            globalObjectPanelsParent.GetChildWithName("XPositionField").GetComponent<UICustomInputField>().SetText(objComponent.transform.position.x, 2);
            globalObjectPanelsParent.GetChildWithName("YPositionField").GetComponent<UICustomInputField>().SetText(objComponent.transform.position.y, 2);
            globalObjectPanelsParent.GetChildWithName("ZPositionField").GetComponent<UICustomInputField>().SetText(objComponent.transform.position.z, 2);
        }
        // I need this EXTRA AND USELESS function just because NGUIzzzzzz can't call the LE_Object function directly...
        // AAALSO now its seems crapGUI can't recognize between two different overloads of a method, so I need to put different names foreach method, DAMN IT.
        public void SetSetActiveAtStart(UIToggle toggle)
        {
            EditorController.Instance.currentSelectedObjComponent.setActiveAtStart = toggle.isChecked;
            EditorController.Instance.levelHasBeenModified = true;
        }
        public void SetPropertyWithInput(string propertyName, UICustomInputField inputField)
        {
            // Even if the input only accepts numbers and decimals, check if it CAN be converted to float anyways, what if the text is just a "-"!?
            if (propertyName.Contains("Position") && float.TryParse(inputField.GetText(), out float floatValue))
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
        #endregion

        void CreateSavingLevelLabel()
        {
            GameObject labelTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Label");

            savingLevelLabel = new GameObject("SavingLevel");
            savingLevelLabel.transform.parent = editorUIParent.transform;
            savingLevelLabel.transform.localScale = Vector3.one;
            savingLevelLabel.transform.localPosition = new Vector3(0f, 510f, 0f);

            UILabel label = savingLevelLabel.AddComponent<UILabel>();
            label.font = labelTemplate.GetComponent<UILabel>().font;
            label.text = "Saving...";
            label.width = 150;
            label.height = 50;
            label.fontSize = 32;

            TweenAlpha tween = savingLevelLabel.AddComponent<TweenAlpha>();
            tween.from = 1f;
            tween.to = 0f;
            tween.duration = 2f;

            savingLevelLabel.SetActive(false);


            savingLevelLabelInPauseMenu = Instantiate(savingLevelLabel, pauseMenu.transform);
            savingLevelLabelInPauseMenu.name = "SavingLevelInPauseMenu";
            savingLevelLabelInPauseMenu.transform.localPosition = new Vector3(0f, -425f, 0f);
            savingLevelLabelInPauseMenu.SetActive(false);
        }
        public void PlaySavingLevelLabel()
        {
            // If the coroutine was already played, stop it if it's currently playing to "restart" it.
            if (savingLevelLabelRoutine != null) MelonCoroutines.Stop(savingLevelLabelRoutine);

            // Execute the coroutine.
            savingLevelLabelRoutine = (Coroutine)MelonCoroutines.Start(Coroutine());
            IEnumerator Coroutine()
            {
                savingLevelLabel.SetActive(true);
                savingLevelLabelInPauseMenu.SetActive(true);

                TweenAlpha tween = savingLevelLabel.GetComponent<TweenAlpha>();
                TweenAlpha tweenInPauseMenu = savingLevelLabelInPauseMenu.GetComponent<TweenAlpha>();
                tween.ResetToBeginning();
                tween.PlayForward();
                tweenInPauseMenu.ResetToBeginning();
                tweenInPauseMenu.PlayForward();

                yield return new WaitForSecondsRealtime(2f);

                savingLevelLabel.SetActive(false);
                savingLevelLabelInPauseMenu.SetActive(false);
            }
        }

        void CreateCurrentModeLabel()
        {
            GameObject template = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Label");

            GameObject currentModeLabelObj = new GameObject("CurrentModeLabel");
            currentModeLabelObj.transform.parent = editorUIParent.transform;
            currentModeLabelObj.transform.localScale = Vector3.one;
            currentModeLabelObj.transform.localPosition = new Vector3(0f, -515f, 0f);

            currentModeLabel = currentModeLabelObj.AddComponent<UILabel>();
            currentModeLabel.font = template.GetComponent<UILabel>().font;
            currentModeLabel.width = 500;
            currentModeLabel.height = 50;
            currentModeLabel.fontSize = 35;
            SetCurrentModeLabelText(EditorController.Mode.Building);

            currentModeLabelObj.SetActive(true);
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

        public void CreateHelpPanel()
        {
            GameObject template = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Background");
            GameObject labelTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Label");

            #region Create Help Panel With The BG
            helpPanel = new GameObject("HelpPanel");
            helpPanel.transform.parent = editorUIParent.transform;
            helpPanel.transform.localScale = Vector3.one;

            UISprite helpPanelBG = helpPanel.AddComponent<UISprite>();
            helpPanelBG.atlas = template.GetComponent<UISprite>().atlas;
            helpPanelBG.spriteName = "Square_Border_Beveled_HighOpacity";
            helpPanelBG.type = UIBasicSprite.Type.Sliced;
            helpPanelBG.color = new Color(0.218f, 0.6464f, 0.6509f, 1f);
            helpPanelBG.width = 1850;
            helpPanelBG.height = 1010;
            #endregion

            #region Create Title
            GameObject title = new GameObject("Title");
            title.transform.parent = helpPanel.transform;
            title.transform.localScale = Vector3.one;
            title.transform.localPosition = new Vector3(0f, 460f, 0f);

            UILabel titleLabel = title.AddComponent<UILabel>();
            titleLabel.depth = 1;
            titleLabel.font = labelTemplate.GetComponent<UILabel>().font;
            titleLabel.text = "KEYBINDS";
            titleLabel.fontSize = 50;
            titleLabel.width = 200;
            titleLabel.height = 50;
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
            keybindsLabel.material = GameObject.Find("MainMenu/Camera/Holder/Main/Window").GetComponent<UISprite>().material;
            keybindsLabel.font = GameObject.Find("MainMenu/Camera/Holder/Tooltip/Label").GetComponent<UILabel>().font;
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
            keybindsLabel2.material = GameObject.Find("MainMenu/Camera/Holder/Main/Window").GetComponent<UISprite>().material;
            keybindsLabel2.font = GameObject.Find("MainMenu/Camera/Holder/Tooltip/Label").GetComponent<UILabel>().font;
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

            helpPanel.SetActive(isEnablingIt);
            // Only enable these panels if the current editor mode is building.
            if (EditorController.Instance.currentMode == EditorController.Mode.Building && !isEnablingIt)
            {
                categoryButtonsParent.SetActive(true);
                currentCategoryBG.SetActive(true);
            }
            else
            {
                categoryButtonsParent.SetActive(false);
                currentCategoryBG.SetActive(false);
            }

            selectedObjPanel.SetActive(!isEnablingIt);
            currentModeLabel.gameObject.SetActive(!isEnablingIt);
        }

        #region Global Properties Related
        public void CreateGlobalPropertiesPanel()
        {
            GameObject template = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Background");
            GameObject labelTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Label");

            #region Create Object With Background
            globalPropertiesPanel = new GameObject("GlobalPropertiesPanel");
            globalPropertiesPanel.transform.parent = editorUIParent.transform;
            globalPropertiesPanel.transform.localScale = Vector3.one;
            globalPropertiesPanel.transform.localPosition = new Vector3(1320f, 0f, 0f);

            UISprite background = globalPropertiesPanel.AddComponent<UISprite>();
            background.atlas = template.GetComponent<UISprite>().atlas;
            background.spriteName = "Square_Border_Beveled_HighOpacity";
            background.type = UIBasicSprite.Type.Sliced;
            background.color = new Color(0.218f, 0.6464f, 0.6509f, 1f);
            background.width = 650;
            background.height = 1010;

            BoxCollider collider = globalPropertiesPanel.AddComponent<BoxCollider>();
            collider.size = new Vector2(650f, 1010f);

            TweenPosition tween = globalPropertiesPanel.AddComponent<TweenPosition>();
            tween.from = new Vector3(600f, 0f);
            tween.to = new Vector3(1320f, 0f);
            tween.duration = 0.2f;
            tween.Invoke("ResetToBeginning", 0.1f);
            #endregion

            #region Create Title
            GameObject title = new GameObject("Title");
            title.transform.parent = globalPropertiesPanel.transform;
            title.transform.localScale = Vector3.one;
            title.transform.localPosition = new Vector3(0f, 460f, 0f);

            UILabel titleLabel = title.AddComponent<UILabel>();
            titleLabel.depth = 1;
            titleLabel.font = labelTemplate.GetComponent<UILabel>().font;
            titleLabel.text = "Global Properties";
            titleLabel.fontSize = 30;
            titleLabel.width = 600;
            titleLabel.height = 50;
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
            GameObject deathYLimitTitle = new GameObject("DeathYLimitLabel");
            deathYLimitTitle.transform.parent = globalPropertiesPanel.transform;
            deathYLimitTitle.transform.localScale = Vector3.one;

            UILabel deathYLimitLabel = deathYLimitTitle.AddComponent<UILabel>();
            deathYLimitLabel.pivot = UIWidget.Pivot.Left;
            deathYLimitLabel.alignment = NGUIText.Alignment.Left;
            deathYLimitLabel.depth = 1;
            deathYLimitLabel.font = labelTemplate.GetComponent<UILabel>().font;
            deathYLimitLabel.text = "Death Y Limit";
            deathYLimitLabel.fontSize = 30;
            deathYLimitLabel.width = 250;
            deathYLimitLabel.height = 50;
            deathYLimitTitle.transform.localPosition = new Vector3(-300f, 270f, 0f);

            GameObject deathYLimitField = NGUI_Utils.CreateInputField(globalPropertiesPanel.transform, new Vector3(100f, 270f, 0f),
                new Vector3Int(300, 50, 0), 30, "100");
            deathYLimitField.name = "DeathYLimit";
            deathYLimitField.GetComponent<UIInput>().onValidate = (UIInput.OnValidate)NGUI_Utils.ValidateNonNegativeFloat;
            var deathFieldCustomScript = deathYLimitField.AddComponent<UICustomInputField>();
            deathFieldCustomScript.Setup(UICustomInputField.UIInputType.NON_NEGATIVE_FLOAT);
            deathFieldCustomScript.onChange += (() => SetGlobalPropertyWithInput("DeathYLimit", deathFieldCustomScript));

            GameObject visualizeDeathYLimitButton = NGUI_Utils.CreateButtonAsToggleWithSprite(globalPropertiesPanel.transform,
                new Vector3(285f, 270f, 0f), new Vector3Int(48, 48, 1), 1, "WhiteSquare", Vector2Int.one * 20);
            visualizeDeathYLimitButton.name = "VisualizeDeathYLimitBtnToggle";
            visualizeDeathYLimitButton.GetComponent<UIButtonScale>().hover = Vector3.one * 1.05f;
            visualizeDeathYLimitButton.GetComponent<UIButtonScale>().pressed = Vector3.one * 1.02f;
            visualizeDeathYLimitButton.GetComponent<UIButtonAsToggle>().onClick += OnVisualizeDeathYLimitToggleClick;
            #endregion
        }
        public void ShowOrHideGlobalPropertiesPanel()
        {
            isShowingGlobalProperties = !isShowingGlobalProperties;

            if (isShowingGlobalProperties) RefreshGlobalPropertiesPanelValues();

            if (isShowingGlobalProperties) globalPropertiesPanel.GetComponent<TweenPosition>().PlayReverse();
            else globalPropertiesPanel.GetComponent<TweenPosition>().PlayForward();

            // Only enable these panels if the current editor mode is building.
            if (EditorController.Instance.currentMode == EditorController.Mode.Building && !isShowingGlobalProperties)
            {
                categoryButtonsParent.SetActive(true);
                currentCategoryBG.SetActive(true);
            }
            else
            {
                categoryButtonsParent.SetActive(false);
                currentCategoryBG.SetActive(false);
            }

            selectedObjPanel.SetActive(!isShowingGlobalProperties);
            currentModeLabel.gameObject.SetActive(!isShowingGlobalProperties);
        }
        void RefreshGlobalPropertiesPanelValues()
        {
            GameObject panel = globalPropertiesPanel;

            panel.GetChildWithName("HasTaserToggle").GetComponent<UIToggle>().Set((bool)GetGlobalProperty("HasTaser"));
            panel.GetChildWithName("HasJetpackToggle").GetComponent<UIToggle>().Set((bool)GetGlobalProperty("HasJetpack"));
            panel.GetChildWithName("DeathYLimit").GetComponent<UIInput>().text = (float)GetGlobalProperty("DeathYLimit") + "";
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
                    bool toReturn = float.TryParse(fieldText, out float result);
                    parsedData = result;
                    return toReturn;
            }

            parsedData = null;
            return false;
        }
        public void SetGlobalProperty(string name, object value)
        {
            if (EditorController.Instance.globalProperties.ContainsKey(name))
            {
                if (EditorController.Instance.globalProperties[name].GetType().Name == value.GetType().Name)
                {
                    EditorController.Instance.globalProperties[name] = value;
                    EditorController.Instance.levelHasBeenModified = true;
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

            // Set the occluder color, it's opaque by defualt for some reason (Anyways, Charles and his weird systems...).
            occluderForWhenPaused.GetComponent<UISprite>().color = new Color(0f, 0f, 0f, 0.9f);

            // Enable the pause panel and play its animations.
            pauseMenu.SetActive(true);
            pauseMenu.GetComponent<TweenAlpha>().PlayForward();

            // Set the paused variable in the LE controller.
            EditorController.Instance.isEditorPaused = true;

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
                pauseMenu.GetComponent<TweenAlpha>().PlayReverse();

                // Threshold to wait for the pause animation to end.
                yield return new WaitForSecondsRealtime(0.3f);

                // Enable the LE UI and disable the pause menu.
                editorUIParent.SetActive(true);
                pauseMenu.SetActive(false);

                // And set the paused variable in the controller as false.
                EditorController.Instance.isEditorPaused = false;
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

        public void ExitToMenuFromNavigationBarButton(NavigationBarController.ActionType type)
        {
            ShowExitPopup();
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

            NavigationAction exitButtonFromNavigation = navigation.GetChildAt("Holder/Bar/ActionsHolder").transform.GetChild(0).GetComponent<NavigationAction>();
            exitButtonFromNavigation.onButtonClick = new Action<NavigationBarController.ActionType>(NavigationBarController.Instance.ManualButtonPressed);

            Destroy(editorUIParent);
            Destroy(pauseMenu.GetChildWithName("SavingLevelInPauseMenu"));

            Logger.Log("LE UI deleted!");
        }
    }
}
