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

        public GameObject currentCategoryBG;
        List<GameObject> currentCategoryButtons = new List<GameObject>();

        GameObject selectedObjPanel;
        GameObject savingLevelLabel;
        GameObject savingLevelLabelInPauseMenu;
        Coroutine savingLevelLabelRoutine;

        GameObject occluderForWhenPaused;
        public GameObject pauseMenu;
        GameObject navigation;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            SetupEditorUI();
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
        }

        void GetReferences()
        {
            GameObject uiParentObj = GameObject.Find("MainMenu/Camera/Holder/");

            occluderForWhenPaused = uiParentObj.GetChildWithName("Occluder");
            pauseMenu = uiParentObj.GetChildWithName("Main");
            navigation = uiParentObj.GetChildWithName("Navigation");
        }


        void SetupObjectsCategories()
        {
            editorUIParent = new GameObject("LevelEditor");
            editorUIParent.transform.parent = GameObject.Find("MainMenu/Camera/Holder").transform;
            editorUIParent.transform.localScale = Vector3.one;

            GameObject buttonTemplate = GameObject.Find("MainMenu/Camera/Holder/TaserCustomization/Holder/Tabs/1_Taser");

            for (int i = 0; i < EditorController.Instance.categories.Count; i++)
            {
                string category = EditorController.Instance.categories[i];

                GameObject categoryButton = Instantiate(buttonTemplate, editorUIParent.transform);
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

            categoryButtons[EditorController.Instance.currentCategoryID].GetComponent<UIToggle>().Set(true);
        }

        void CreateObjectsBackground()
        {
            GameObject template = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Background");

            currentCategoryBG = new GameObject("CategoryObjectsBackground");
            currentCategoryBG.transform.parent = editorUIParent.transform;
            currentCategoryBG.transform.localPosition = new Vector3(0f, 330f, 0f);
            currentCategoryBG.transform.localScale = Vector3.one;

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

            BoxCollider bodyCollider = selectedObjPanel.AddComponent<BoxCollider>();
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


        public void SetupPauseWhenInEditor()
        {
            // Setup the resume button, to actually resume the editor scene and not load another scene, which is the defualt behaviour of that button.
            GameObject originalResumeBtn = pauseMenu.GetChildAt("LargeButtons/1_Resume");
            GameObject resumeBtnWhenInsideLE = Instantiate(originalResumeBtn, originalResumeBtn.transform.parent);
            resumeBtnWhenInsideLE.name = "1_ResumeWhenInEditor";
            Destroy(resumeBtnWhenInsideLE.GetComponent<ButtonController>());
            resumeBtnWhenInsideLE.GetComponent<UIButton>().onClick.Add(new EventDelegate(this, nameof(EditorUIManager.Resume)));

            // Same with exit button.
            GameObject originalExitBtn = pauseMenu.GetChildAt("LargeButtons/7_Exit");
            GameObject exitBtnWhenInsideLE = Instantiate(originalExitBtn, originalExitBtn.transform.parent);
            exitBtnWhenInsideLE.name = "7_ExitWhenInEditor";
            Destroy(exitBtnWhenInsideLE.GetComponent<ButtonController>());
            exitBtnWhenInsideLE.GetComponent<UIButton>().onClick.Add(new EventDelegate(this, nameof(EditorUIManager.ExitToMenu)));

            // Create a save level button.
            GameObject saveLevelButtonTemplate = pauseMenu.GetChildAt("LargeButtons/2_Chapters");
            GameObject saveLevelButton = Instantiate(saveLevelButtonTemplate, saveLevelButtonTemplate.transform.parent);
            saveLevelButton.name = "2_SaveLevel";
            Destroy(saveLevelButton.GetComponent<ButtonController>());
            Destroy(saveLevelButton.GetChildWithName("Label").GetComponent<UILocalize>());
            saveLevelButton.GetChildWithName("Label").GetComponent<UILabel>().text = "Save Level";
            saveLevelButton.GetComponent<UIButton>().onClick.Add(new EventDelegate(this, nameof(EditorUIManager.SaveLevelWithPauseMenuButton)));

            // A custom script to make the damn large buttons be the correct ones, resume, options and exit, that's all.
            pauseMenu.AddComponent<EditorPauseLargeButtonsSetter>();
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
        public void ExitToMenu()
        {
            // Remove this component, since this component is only needed when inside of LE.
            pauseMenu.RemoveComponent<EditorPauseLargeButtonsSetter>();

            DeleteUI();

            MenuController.GetInstance().ReturnToMainMenu();
        }
        public void SaveLevelWithPauseMenuButton()
        {
            LevelData.SaveLevelData(EditorController.Instance.levelName, EditorController.Instance.levelFileNameWithoutExtension);
            PlaySavingLevelLabel();
        }

        public void DeleteUI()
        {
            MelonCoroutines.Stop(savingLevelLabelRoutine);
            Destroy(editorUIParent);
            Destroy(pauseMenu.GetChildWithName("SavingLevelInPauseMenu"));
        }
    }
}
