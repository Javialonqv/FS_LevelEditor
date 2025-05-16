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

namespace FS_LevelEditor
{
    [RegisterTypeInIl2Cpp]
    public class EditorUIManager : MonoBehaviour
    {
        public static EditorUIManager Instance;

        public GameObject editorUIParent;

        public List<GameObject> categoryButtons = new List<GameObject>();
        public GameObject categoryButtonsParent;
        bool categoryButtonsAreHidden = false;

        public GameObject currentCategoryBG;
        List<GameObject> currentCategoryButtons = new List<GameObject>();

        public GameObject selectedObjPanel;
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

            for (int i = 0; i < EditorController.Instance.categories.Count; i++)
            {
                string category = EditorController.Instance.categories[i];

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

            SetSelectedObjPanelAsNone();

            CreateNoAttributesPanel();
            CreateLightAttributesPanel();
            CreateSawAttributesPanel();
            CreateSawWaypointAttributesPanel();
            CreateSwitchAttributesPanel();
            CreateAmmoAndHealthPackAttributesPanel();
        }

        void CreateNoAttributesPanel()
        {
            GameObject labelTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Label");

            GameObject noAttributes = new GameObject("NoAttributes");
            noAttributes.transform.parent = selectedObjPanel.GetChildWithName("Body").transform;
            noAttributes.transform.localPosition = Vector3.zero;
            noAttributes.transform.localScale = Vector3.one;

            UILabel label = noAttributes.AddComponent<UILabel>();
            label.font = labelTemplate.GetComponent<UILabel>().font;
            label.fontSize = 27;
            label.width = 500;
            label.height = 200;
            label.text = "No Attributes for this object.";

            noAttributes.SetActive(false);
            attrbutesPanels.Add("None", noAttributes);
        }
        void CreateLightAttributesPanel()
        {
            GameObject labelTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Label");

            GameObject lightAttributes = new GameObject("LightAttributes");
            lightAttributes.transform.parent = selectedObjPanel.GetChildWithName("Body").transform;
            lightAttributes.transform.localPosition = Vector3.zero;
            lightAttributes.transform.localScale = Vector3.one;

            #region Color Input Field
            GameObject colorTitle = Instantiate(labelTemplate, lightAttributes.transform);
            colorTitle.name = "ColorTitle";
            colorTitle.transform.localPosition = new Vector3(-230f, 90f, 0f);
            colorTitle.RemoveComponent<UILocalize>();
            colorTitle.GetComponent<UILabel>().text = "Color (Hex)";
            colorTitle.GetComponent<UILabel>().color = Color.white;

            GameObject hashtagLOL = Instantiate(labelTemplate, lightAttributes.transform);
            hashtagLOL.name = "ColorHashtag";
            hashtagLOL.transform.localPosition = new Vector3(5f, 90f, 0f);
            hashtagLOL.RemoveComponent<UILocalize>();
            hashtagLOL.GetComponent<UILabel>().text = "#";
            hashtagLOL.GetComponent<UILabel>().color = Color.white;
            hashtagLOL.GetComponent<UILabel>().alignment = NGUIText.Alignment.Center;
            hashtagLOL.GetComponent<UILabel>().width = 38;

            GameObject colorInputField = NGUI_Utils.CreateInputField(lightAttributes.transform, new Vector3(140f, 90f, 0f), new Vector3Int(200, 38, 0), 27);
            colorInputField.name = "ColorField";
            colorInputField.GetComponent<UILabel>().alignment = NGUIText.Alignment.Left;
            colorInputField.GetComponent<UILabel>().color = Color.gray;
            colorInputField.GetComponent<UIInput>().characterLimit = 6;
            colorInputField.GetComponent<UIInput>().defaultText = "ffffff";
            colorInputField.GetComponent<UIInput>().activeTextColor = Color.white;
            colorInputField.GetComponent<UIInput>().onChange.Clear();
            var colorDelegate = NGUI_Utils.CreateEvenDelegate(this, nameof(SetPropertyWithInput),
                NGUI_Utils.CreateEventDelegateParamter(this, "propertyName", "Color"),
                NGUI_Utils.CreateEventDelegateParamter(this, "inputField", colorInputField.GetComponent<UIInput>()));
            colorInputField.GetComponent<UIInput>().onChange.Add(colorDelegate);
            #endregion

            #region Intensity Input Field
            GameObject intensityTitle = Instantiate(labelTemplate, lightAttributes.transform);
            intensityTitle.name = "IntensityTitle";
            intensityTitle.transform.localPosition = new Vector3(-230f, 40f, 0f);
            intensityTitle.RemoveComponent<UILocalize>();
            intensityTitle.GetComponent<UILabel>().text = "Intensity";
            intensityTitle.GetComponent<UILabel>().color = Color.white;

            GameObject intensityInputField = NGUI_Utils.CreateInputField(lightAttributes.transform, new Vector3(140f, 40f, 0f), new Vector3Int(200, 38, 0), 27);
            intensityInputField.name = "IntensityField";
            intensityInputField.GetComponent<UILabel>().alignment = NGUIText.Alignment.Left;
            intensityInputField.GetComponent<UILabel>().color = Color.gray;
            intensityInputField.GetComponent<UIInput>().defaultText = "1";
            intensityInputField.GetComponent<UIInput>().activeTextColor = Color.white;
            intensityInputField.GetComponent<UIInput>().onValidate = (UIInput.OnValidate)NGUI_Utils.ValidateNonNegativeFloat;
            intensityInputField.GetComponent<UIInput>().onChange.Clear();
            var intensityDelegate = NGUI_Utils.CreateEvenDelegate(this, nameof(SetPropertyWithInput),
                NGUI_Utils.CreateEventDelegateParamter(this, "propertyName", "Intensity"),
                NGUI_Utils.CreateEventDelegateParamter(this, "inputField", intensityInputField.GetComponent<UIInput>()));
            intensityInputField.GetComponent<UIInput>().onChange.Add(intensityDelegate);
            #endregion

            lightAttributes.SetActive(false);
            attrbutesPanels.Add("Light", lightAttributes);
        }
        void CreateSawAttributesPanel()
        {
            GameObject toggleTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles");
            GameObject labelTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Label");

            GameObject sawAttributes = new GameObject("SawAttributes");
            sawAttributes.transform.parent = selectedObjPanel.GetChildWithName("Body").transform;
            sawAttributes.transform.localPosition = Vector3.zero;
            sawAttributes.transform.localScale = Vector3.one;

            #region Activate On Start Toggle
            GameObject activateOnStartTitle = Instantiate(labelTemplate, sawAttributes.transform);
            activateOnStartTitle.name = "ActivateOnStartTitle";
            activateOnStartTitle.transform.localPosition = new Vector3(-230f, 90f, 0f);
            activateOnStartTitle.RemoveComponent<UILocalize>();
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
            damageTitle.GetComponent<UILabel>().text = "Damage";
            damageTitle.GetComponent<UILabel>().color = Color.white;

            GameObject damageInputField = NGUI_Utils.CreateInputField(sawAttributes.transform, new Vector3(140f, 40f, 0f), new Vector3Int(200, 38, 0), 27);
            damageInputField.name = "DamageInputField";
            damageInputField.GetComponent<UILabel>().alignment = NGUIText.Alignment.Left;
            damageInputField.GetComponent<UILabel>().color = Color.gray;
            damageInputField.GetComponent<UIInput>().defaultText = "50";
            damageInputField.GetComponent<UIInput>().activeTextColor = Color.white;
            damageInputField.GetComponent<UIInput>().onValidate = (UIInput.OnValidate)NGUI_Utils.ValidateNonNegativeInt;
            damageInputField.GetComponent<UIInput>().onChange.Clear();
            var damageDelegate = NGUI_Utils.CreateEvenDelegate(this, nameof(SetPropertyWithInput),
                NGUI_Utils.CreateEventDelegateParamter(this, "propertyName", "Damage"),
                NGUI_Utils.CreateEventDelegateParamter(this, "inputField", damageInputField.GetComponent<UIInput>()));
            damageInputField.GetComponent<UIInput>().onChange.Add(damageDelegate);
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
            sawWaypointAttributes.transform.parent = selectedObjPanel.GetChildWithName("Body").transform;
            sawWaypointAttributes.transform.localPosition = Vector3.zero;
            sawWaypointAttributes.transform.localScale = Vector3.one;

            #region Wait Time Input Field
            GameObject waitTimeTitle = Instantiate(labelTemplate, sawWaypointAttributes.transform);
            waitTimeTitle.name = "WaitTimeTitle";
            waitTimeTitle.transform.localPosition = new Vector3(-230f, 90f, 0f);
            waitTimeTitle.RemoveComponent<UILocalize>();
            waitTimeTitle.GetComponent<UILabel>().text = "Wait Time";
            waitTimeTitle.GetComponent<UILabel>().color = Color.white;

            GameObject waitTimeInputField = NGUI_Utils.CreateInputField(sawWaypointAttributes.transform, new Vector3(140f, 90f, 0f), new Vector3Int(200, 38, 0), 27);
            waitTimeInputField.name = "WaitTimeInputField";
            waitTimeInputField.GetComponent<UILabel>().alignment = NGUIText.Alignment.Left;
            waitTimeInputField.GetComponent<UILabel>().color = Color.gray;
            waitTimeInputField.GetComponent<UIInput>().defaultText = "0.3";
            waitTimeInputField.GetComponent<UIInput>().activeTextColor = Color.white;
            waitTimeInputField.GetComponent<UIInput>().onValidate = (UIInput.OnValidate)NGUI_Utils.ValidateNonNegativeFloat;
            waitTimeInputField.GetComponent<UIInput>().onChange.Clear();
            var damageDelegate = NGUI_Utils.CreateEvenDelegate(this, nameof(SetPropertyWithInput),
                NGUI_Utils.CreateEventDelegateParamter(this, "propertyName", "WaitTime"),
                NGUI_Utils.CreateEventDelegateParamter(this, "inputField", waitTimeInputField.GetComponent<UIInput>()));
            waitTimeInputField.GetComponent<UIInput>().onChange.Add(damageDelegate);
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
            switchAttributes.transform.parent = selectedObjPanel.GetChildWithName("Body").transform;
            switchAttributes.transform.localPosition = Vector3.zero;
            switchAttributes.transform.localScale = Vector3.one;

            #region Usable Once Toggle
            GameObject usableOnceTitle = Instantiate(labelTemplate, switchAttributes.transform);
            usableOnceTitle.name = "UsableOnceTitle";
            usableOnceTitle.transform.localPosition = new Vector3(-230f, 90f, 0f);
            usableOnceTitle.RemoveComponent<UILocalize>();
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
            ammoHealthAttributes.transform.parent = selectedObjPanel.GetChildWithName("Body").transform;
            ammoHealthAttributes.transform.localPosition = Vector3.zero;
            ammoHealthAttributes.transform.localScale = Vector3.one;

            #region Respawn Time Input Field
            GameObject respawnTitle = Instantiate(labelTemplate, ammoHealthAttributes.transform);
            respawnTitle.name = "RespawnTitle";
            respawnTitle.transform.localPosition = new Vector3(-230f, 90f, 0f);
            respawnTitle.RemoveComponent<UILocalize>();
            respawnTitle.GetComponent<UILabel>().text = "Respawn Time";
            respawnTitle.GetComponent<UILabel>().color = Color.white;

            GameObject respawnInputField = NGUI_Utils.CreateInputField(ammoHealthAttributes.transform, new Vector3(140f, 90f, 0f), new Vector3Int(200, 38, 0), 27);
            respawnInputField.name = "RespawnInputField";
            respawnInputField.GetComponent<UILabel>().alignment = NGUIText.Alignment.Left;
            respawnInputField.GetComponent<UILabel>().color = Color.gray;
            respawnInputField.GetComponent<UIInput>().defaultText = "50";
            respawnInputField.GetComponent<UIInput>().activeTextColor = Color.white;
            respawnInputField.GetComponent<UIInput>().onChange.Clear();
            respawnInputField.GetComponent<UIInput>().onValidate = (UIInput.OnValidate)NGUI_Utils.ValidateNonNegativeFloat;
            var respawnDelegate = NGUI_Utils.CreateEvenDelegate(this, nameof(SetPropertyWithInput),
                NGUI_Utils.CreateEventDelegateParamter(this, "propertyName", "RespawnTime"),
                NGUI_Utils.CreateEventDelegateParamter(this, "inputField", respawnInputField.GetComponent<UIInput>()));
            respawnInputField.GetComponent<UIInput>().onChange.Add(respawnDelegate);
            #endregion

            ammoHealthAttributes.SetActive(false);
            attrbutesPanels.Add("AmmoAndHealth", ammoHealthAttributes);
        }

        public void SetSelectedObjPanelAsNone()
        {
            selectedObjPanel.GetChildWithName("Label").GetComponent<UILabel>().text = "No Object Selected";
            selectedObjPanel.GetChildWithName("SetActiveAtStartToggle").SetActive(false);
            selectedObjPanel.GetChildWithName("Body").SetActive(false);
            selectedObjPanel.transform.localPosition = new Vector3(-700f, -505f, 0f);
        }
        public void SetMultipleObjectsSelected()
        {
            selectedObjPanel.GetChildWithName("Label").GetComponent<UILabel>().text = "Multiple Objects Selected";
            selectedObjPanel.GetChildWithName("SetActiveAtStartToggle").SetActive(false);
            selectedObjPanel.GetChildWithName("Body").SetActive(false);
            selectedObjPanel.transform.localPosition = new Vector3(-700f, -505f, 0f);
        }
        public void SetSelectedObject(LE_Object objComponent)
        {
            selectedObjPanel.GetChildWithName("Label").GetComponent<UILabel>().text = objComponent.objectFullNameWithID;
            selectedObjPanel.GetChildWithName("Body").SetActive(true);
            selectedObjPanel.transform.localPosition = new Vector3(-700f, -220, 0f);

            attrbutesPanels.ToList().ForEach(x => x.Value.SetActive(false));

            if (objComponent.objectOriginalName.Contains("Light"))
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
            else
            {
                attrbutesPanels["None"].SetActive(true);
            }

            selectedObjPanel.GetChildWithName("SetActiveAtStartToggle").SetActive(true);
            selectedObjPanel.GetChildWithName("SetActiveAtStartToggle").GetComponent<UIToggle>().Set(objComponent.setActiveAtStart);
        }

        // I need this EXTRA AND USELESS function just because NGUIzzzzzz can't call the LE_Object function directly...
        // AAALSO now its seems crapGUI can't recognize between two different overloads of a method, so I need to put different names foreach method, DAMN IT.
        public void SetSetActiveAtStart(UIToggle toggle)
        {
            EditorController.Instance.currentSelectedObjComponent.setActiveAtStart = toggle.isChecked;
            EditorController.Instance.levelHasBeenModified = true;
        }
        public void SetPropertyWithInput(string propertyName, UIInput inputField)
        {
            Color validValueColor = new Color(0.0588f, 0.3176f, 0.3215f, 0.9412f);
            Color invalidValueColor = new Color(0.3215f, 0.2156f, 0.0588f, 0.9415f);

            if (EditorController.Instance.currentSelectedObjComponent.SetProperty(propertyName, inputField.text))
            {
                EditorController.Instance.levelHasBeenModified = true;
                inputField.GetComponent<UISprite>().color = validValueColor;
            }
            else
            {
                inputField.GetComponent<UISprite>().color = invalidValueColor;
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
        }
    }
}
