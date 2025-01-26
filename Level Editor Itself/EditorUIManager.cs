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

namespace FS_LevelEditor
{
    [RegisterTypeInIl2Cpp]
    public class EditorUIManager : MonoBehaviour
    {
        public static EditorUIManager Instance;

        GameObject editorUIParent;

        public List<GameObject> categoryButtons = new List<GameObject>();
        GameObject categoryButtonsParent;
        bool categoryButtonsAreHidden = false;

        public GameObject currentCategoryBG;
        List<GameObject> currentCategoryButtons = new List<GameObject>();

        GameObject selectedObjPanel;
        GameObject savingLevelLabel;
        GameObject savingLevelLabelInPauseMenu;
        Coroutine savingLevelLabelRoutine;
        UILabel currentModeLabel;
        GameObject onExitPopupBackButton;
        GameObject onExitPopupSaveAndExitButton;
        GameObject onExitPopupExitButton;
        bool exitPopupEnabled = false;

        GameObject occluderForWhenPaused;
        public GameObject pauseMenu;
        public GameObject navigation;
        GameObject popup;
        PopupController popupController;
        GameObject popupTitle;
        GameObject popupContentLabel;
        GameObject popupSmallButtonsParent;

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

            currentCategoryButtons.Clear();
            currentCategoryBG.DeleteAllChildren();

            for (int i = 0; i < EditorController.Instance.allCategoriesObjectsSorted[EditorController.Instance.currentCategoryID].Count; i++)
            {
                var currentCategoryObj = EditorController.Instance.allCategoriesObjectsSorted[EditorController.Instance.currentCategoryID].ToList()[i];

                GameObject currentCategoryButton = Instantiate(template, currentCategoryBG.transform);
                currentCategoryButton.name = currentCategoryObj.Key;
                currentCategoryButton.transform.localPosition = new Vector3(-800 + (150f * i), -25f, 0f);
                currentCategoryButton.transform.localScale = Vector3.one * 0.8f;
                currentCategoryButton.GetChildWithName("ActiveSwatch").SetActive(false);
                currentCategoryButton.GetChildWithName("ColorSample").SetActive(false);
                currentCategoryButton.SetActive(true);

                currentCategoryButton.GetChildWithName("ColorName").GetComponent<UILabel>().text = currentCategoryObj.Key;
                currentCategoryButton.GetComponent<UIButton>().onClick.Clear();

                EventDelegate onChange = new EventDelegate(EditorController.Instance, nameof(EditorController.SelectObjectToBuild));
                EventDelegate.Parameter onChangeParameter = new EventDelegate.Parameter
                {
                    field = "objName",
                    value = currentCategoryObj.Key,
                    obj = EditorController.Instance
                };
                onChange.mParameters = new EventDelegate.Parameter[] { onChangeParameter };
                currentCategoryButton.GetComponent<UIButton>().onClick.Add(onChange);

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
        }

        public void SetSelectedObjPanelAsNone()
        {
            selectedObjPanel.GetChildWithName("Label").GetComponent<UILabel>().text = "No Object Selected";
            selectedObjPanel.GetChildWithName("Body").SetActive(false);
            selectedObjPanel.transform.localPosition = new Vector3(-700f, -505f, 0f);
        }
        public void SetMultipleObjectsSelected()
        {
            selectedObjPanel.GetChildWithName("Label").GetComponent<UILabel>().text = "Multiple Objects Selected";
            selectedObjPanel.GetChildWithName("Body").SetActive(false);
            selectedObjPanel.transform.localPosition = new Vector3(-700f, -505f, 0f);
        }
        public void SetSelectedObject(string objName)
        {
            selectedObjPanel.GetChildWithName("Label").GetComponent<UILabel>().text = objName;
            selectedObjPanel.GetChildWithName("Body").SetActive(true);
            selectedObjPanel.transform.localPosition = new Vector3(-700f, -220, 0f);
        }

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

            // Same with exit button.
            GameObject originalExitBtn = pauseMenu.GetChildAt("LargeButtons/7_Exit");
            GameObject exitBtnWhenInsideLE = Instantiate(originalExitBtn, originalExitBtn.transform.parent);
            exitBtnWhenInsideLE.name = "7_ExitWhenInEditor";
            Destroy(exitBtnWhenInsideLE.GetComponent<ButtonController>());
            exitBtnWhenInsideLE.GetComponent<UIButton>().onClick.Add(new EventDelegate(this, nameof(ShowExitPopup)));

            // Create a save level button.
            GameObject saveLevelButtonTemplate = pauseMenu.GetChildAt("LargeButtons/2_Chapters");
            GameObject saveLevelButton = Instantiate(saveLevelButtonTemplate, saveLevelButtonTemplate.transform.parent);
            saveLevelButton.name = "3_SaveLevel";
            Destroy(saveLevelButton.GetComponent<ButtonController>());
            Destroy(saveLevelButton.GetChildWithName("Label").GetComponent<UILocalize>());
            saveLevelButton.GetChildWithName("Label").GetComponent<UILabel>().text = "Save Level";
            saveLevelButton.GetComponent<UIButton>().onClick.Add(new EventDelegate(this, nameof(SaveLevelWithPauseMenuButton)));

            // Create a PLAY level button.
            GameObject playLevelButton = Instantiate(saveLevelButtonTemplate, saveLevelButtonTemplate.transform.parent);
            playLevelButton.name = "2_PlayLevel";
            Destroy(playLevelButton.GetComponent<ButtonController>());
            Destroy(playLevelButton.GetChildWithName("Label").GetComponent<UILocalize>());
            playLevelButton.GetChildWithName("Label").GetComponent<UILabel>().text = "Play Level";
            playLevelButton.GetComponent<UIButton>().onClick.Add(new EventDelegate(this, nameof(EditorUIManager.PlayLevel)));

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

                // Enable the LE UI.
                editorUIParent.SetActive(true);

                // And set the paused variable in the controller as false.
                EditorController.Instance.isEditorPaused = false;
            }
        }

        public void ShowExitPopup()
        {
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
        }

        public void ExitToMenu(bool saveDataBeforeExit = false)
        {
            if (saveDataBeforeExit)
            {
                // Save data.
                LevelData.SaveLevelData(EditorController.Instance.levelName, EditorController.Instance.levelFileNameWithoutExtension);
            }

            // Remove this component, since this component is only needed when inside of LE.
            pauseMenu.GetComponent<EditorPauseMenuPatcher>().BeforeDestroying();
            pauseMenu.RemoveComponent<EditorPauseMenuPatcher>();

            DeleteUI();

            MenuController.GetInstance().ReturnToMainMenu();
        }

        public void PlayLevel()
        {
            EditorController.Instance.EnterPlayMode();
        }
        public void DeleteUI()
        {
            // If the coroutine was already played, stop it if it's currently playing to "restart" it.
            if (savingLevelLabelRoutine != null) MelonCoroutines.Stop(savingLevelLabelRoutine);

            NavigationAction exitButtonFromNavigation = navigation.GetChildAt("Holder/Bar/ActionsHolder").transform.GetChild(0).GetComponent<NavigationAction>();
            exitButtonFromNavigation.onButtonClick = new Action<NavigationBarController.ActionType>(NavigationBarController.Instance.ManualButtonPressed);

            Destroy(editorUIParent);
            Destroy(pauseMenu.GetChildWithName("SavingLevelInPauseMenu"));
        }
    }
}
