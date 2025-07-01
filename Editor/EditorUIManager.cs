using FS_LevelEditor.Editor.UI;
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

namespace FS_LevelEditor.Editor
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

        // For the objects to build buttons.
        public GameObject objectsToBuildBG;
        List<GameObject> objectsToBuildButtonsParents = new();

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

            editorUIParent = new GameObject("LevelEditor");
            editorUIParent.transform.parent = GameObject.Find("MainMenu/Camera/Holder").transform;
            editorUIParent.transform.localScale = Vector3.one;

            SetupPauseWhenInEditor();

            CreateObjectsCategories();
            CreateObjectsBackground();
            CreateALLOfTheObjectsButtons();
            SelectedObjPanel.Create(editorUIParent.transform);
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

        void CreateObjectsCategories()
        {
            // Setup the category buttons parent and add a panel to it so I can modify the alpha of the whole buttons inside of it with just one panel.
            categoryButtonsParent = new GameObject("CategoryButtons");
            categoryButtonsParent.transform.parent = editorUIParent.transform;
            categoryButtonsParent.transform.localPosition = Vector3.zero;
            categoryButtonsParent.transform.localScale = Vector3.one;
            categoryButtonsParent.layer = LayerMask.NameToLayer("2D GUI");
            categoryButtonsParent.AddComponent<UIPanel>();

            for (int i = 0; i < EditorController.Instance.categoriesNames.Count; i++)
            {
                string category = EditorController.Instance.categoriesNames[i];
                Vector3 buttonPosition = new Vector3(-800f + (250f * i), 450f, 0f);

                UITogglePatcher categoryButton = NGUI_Utils.CreateTabToggle(categoryButtonsParent.transform, buttonPosition, category);
                categoryButton.name = $"{category}_Button";
                // The toggle is set to false by default.

                // It seems it's a bug, I need to create a copy of 'i'. Otherwise ALL of the toggles will end using the same value.
                int index = i;
                categoryButton.onClick += () => EditorController.Instance.ChangeCategory(index);

                categoryButtons.Add(categoryButton.gameObject);
            }


            categoryButtons[0].GetComponent<UIToggle>().Set(true);
        }
        void CreateObjectsBackground()
        {
            objectsToBuildBG = new GameObject("CategoryObjectsBackground");
            objectsToBuildBG.transform.parent = editorUIParent.transform;
            objectsToBuildBG.transform.localPosition = new Vector3(0f, 330f, 0f);
            objectsToBuildBG.transform.localScale = Vector3.one;
            objectsToBuildBG.layer = LayerMask.NameToLayer("2D GUI");
            objectsToBuildBG.AddComponent<UIPanel>();

            UISprite bgSprite = objectsToBuildBG.AddComponent<UISprite>();
            bgSprite.atlas = NGUI_Utils.UITexturesAtlas;
            bgSprite.spriteName = "Square_Border_Beveled_HighOpacity";
            bgSprite.type = UIBasicSprite.Type.Sliced;
            bgSprite.color = new Color(0.218f, 0.6464f, 0.6509f, 1f);
            bgSprite.width = 1800;
            bgSprite.height = 150;

            BoxCollider collider = objectsToBuildBG.AddComponent<BoxCollider>();
            collider.size = new Vector3(1800f, 150f, 1f);
        }
        void CreateALLOfTheObjectsButtons()
        {
            for (int i = 0; i < EditorController.Instance.categoriesNames.Count; i++)
            {
                GameObject createdButtonsParent = CreateObjectsForCategory(i);

                // Only enable the very first category.
                createdButtonsParent.SetActive(i == 0);
            }
        }
        public GameObject CreateObjectsForCategory(int categoryID)
        {
            GameObject parent = new GameObject(EditorController.Instance.categoriesNames[categoryID]);
            parent.transform.parent = objectsToBuildBG.transform;
            parent.transform.localPosition = Vector3.zero;
            parent.transform.localScale = Vector3.one;

            for (int i = 0; i < EditorController.Instance.allCategoriesObjectsSorted[categoryID].Count; i++)
            {
                // Get the object.
                var objectInfo = EditorController.Instance.allCategoriesObjectsSorted[categoryID].ToList()[i];
                Vector3 buttonPos = new Vector3(-800 + (150f * i), -25f, 0f);

                var button = NGUI_Utils.CreateColorButton(parent.transform, buttonPos, objectInfo.Key);

                button.onClick += () => EditorController.Instance.SelectObjectToBuild(objectInfo.Key);
                button.onClick += () => SetCurrentObjButtonAsSelected(button.gameObject);

                button.transform.localScale = Vector3.one * 0.8f;
                button.GetComponent<UIButtonScale>().mScale = Vector3.one * 0.8f;
            }

            objectsToBuildButtonsParents.Add(parent);

            return parent;
        }

        public void ShowObjectButtonsForCategory(int categoryID)
        {
            foreach (var parent in objectsToBuildButtonsParents)
            {
                parent.SetActive(false);
            }

            objectsToBuildButtonsParents[categoryID].SetActive(true);

            // Select the very first element on the objects list.
            objectsToBuildButtonsParents[categoryID].transform.GetChild(0).GetComponent<UIButton>().OnClick();
        }
        public void SetCurrentObjButtonAsSelected(GameObject selectedButton)
        {
            // Disable the "selected" obj in the other buttons.
            foreach (var button in objectsToBuildButtonsParents[EditorController.Instance.currentCategoryID].GetChilds())
            {
                button.GetChildWithName("ActiveSwatch").SetActive(false);
            }

            selectedButton.GetChildWithName("ActiveSwatch").SetActive(true);
        }

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
                TweenPosition.Begin(objectsToBuildBG, 0.2f, new Vector3(0f, 410f, 0f));
                InGameUIManager.Instance.m_uiAudioSource.PlayOneShot(InGameUIManager.Instance.hideHUDSound);
            }
            else
            {
                TweenAlpha.Begin(categoryButtonsParent, 0.2f, 1f);
                TweenPosition.Begin(objectsToBuildBG, 0.2f, new Vector3(0f, 330f, 0f));
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

            UICustomInputField deathYLimitField = NGUI_Utils.CreateInputField(globalPropertiesPanel.transform, new Vector3(100f, 270f, 0f),
                new Vector3Int(300, 50, 0), 30, "100", inputType: UICustomInputField.UIInputType.NON_NEGATIVE_FLOAT);
            deathYLimitField.name = "DeathYLimit";
            deathYLimitField.onChange += (() => SetGlobalPropertyWithInput("DeathYLimit", deathYLimitField));

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
                objectsToBuildBG.SetActive(false);
            }
            else if (EditorController.Instance.currentMode == EditorController.Mode.Building)
            {
                categoryButtonsParent.SetActive(true);
                objectsToBuildBG.SetActive(true);
            }
            // Only when normal.
            SelectedObjPanel.Instance.gameObject.SetActive(context == EditorUIContext.NORMAL);
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
