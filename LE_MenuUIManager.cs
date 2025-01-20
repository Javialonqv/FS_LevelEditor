using Il2Cpp;
using Il2CppInControl;
using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_MenuUIManager : MonoBehaviour
    {
        public static LE_MenuUIManager Instance;

        public bool inLEMenu;

        GameObject mainMenu;
        AudioSource uiSoundSource;
        AudioClip okSound;
        AudioClip hidePageSound;
        GameObject navigationBarButtonsParent;

        GameObject levelEditorUIButton;
        public GameObject leMenuPanel;
        GameObject leMenuButtonsParent;
        GameObject backButton;
        GameObject addButton;
        GameObject lvlButtonsParent;
        List<GameObject> lvlButtonsGrids = new List<GameObject>();
        int currentLevelsGridID;

        void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            GetSomeReferences();
            CreateLEButton();
            CreateLEMenuPanel();
            CreateBackButton();
            CreateAddButton();
        }

        void Update()
        {
#if DEBUG
            if (Input.GetKeyDown(KeyCode.F3))
            {
                SwitchBetweenMenuAndLEMenu();
            }
#endif
            if (EditorUIManager.Instance == null)
            {
                levelEditorUIButton.SetActive(Core.currentSceneName.Contains("Menu"));
            }

            // To exit from the LE menu with the ESC key.
            if (Input.GetKeyDown(KeyCode.Escape) && inLEMenu)
            {
                SwitchBetweenMenuAndLEMenu();
            }

            // To exit from the LE menu with the navigation bar buttons.
            if (inLEMenu)
            {
                NavigationAction action = navigationBarButtonsParent.transform.GetChild(0).GetComponent<NavigationAction>();
                if (action.assignedActionType != NavigationBarController.ActionType.BACK)
                {
                    navigationBarButtonsParent.DisableAllChildren();
                    action.gameObject.SetActive(true);
                    action.Setup(Localization.Get("Back").ToUpper(), "Keyboard_Black_Esc", NavigationBarController.ActionType.BACK);
                    action.onButtonClick = new Action<NavigationBarController.ActionType>(ExitLEMenuInNavigationBarButton);
                }
            }
        }

        void GetSomeReferences()
        {
            mainMenu = GameObject.Find("MainMenu/Camera/Holder/Main");
            uiSoundSource = GameObject.Find("MainMenu/UISound").GetComponent<AudioSource>();
            // Ik this isn't the best way to get these clips, but it works, so... I'mn not touching it again lol.
            okSound = GameObject.Find("MainMenu/Camera/Holder/Main/LargeButtons/2_Chapters").GetComponent<ButtonController>().m_pressSound;
            okSound.hideFlags = HideFlags.DontUnloadUnusedAsset;
            hidePageSound = MenuController.GetInstance().hidePageSound;
            hidePageSound.hideFlags = HideFlags.DontUnloadUnusedAsset;

            navigationBarButtonsParent = GameObject.Find("MainMenu/Camera/Holder/Navigation/Holder/Bar/ActionsHolder/");
        }

        // LE stands for "Level Editor" lmao.
        void CreateLEButton()
        {
            // The game disables the existing LE button since it detects we aren't in the unity editor or debugging, so I need to create a copy of the button.
            GameObject defaultLEButton = GameObject.Find("MainMenu/Camera/Holder/Main/LargeButtons/6_LevelEditor");
            levelEditorUIButton = GameObject.Instantiate(defaultLEButton, defaultLEButton.transform.parent);
            levelEditorUIButton.name = "6_Javi's LevelEditor";

            // And why not? Destroy the old button, since we don't need it anymore ;)
            GameObject.Destroy(defaultLEButton);

            // Change the button's label text.
            GameObject.Destroy(levelEditorUIButton.GetChildWithName("Label").GetComponent<UILocalize>());
            levelEditorUIButton.GetChildWithName("Label").GetComponent<UILabel>().text = "Level Editor";

            // Change the button's on click action.
            GameObject.Destroy(levelEditorUIButton.GetComponent<ButtonController>());

            // So... I just realized if you add a class to a gameobject with also a UIButton, the button will automatically call a "OnClick" function inside of the class if it exists,
            // without adding it manually to the UIButton via code... good to know :)
            LE_UIButtonActionCtrl onClickClass = levelEditorUIButton.AddComponent<LE_UIButtonActionCtrl>();

            // Finally, enable the button.
            levelEditorUIButton.SetActive(true);
        }

        // And yes, this whole function is directly copied from the OST mod (almost), DON'T JUDGE ME.
        public void CreateLEMenuPanel()
        {
            // Get the Options menu and create a copy.
            GameObject originalOptionsMenu = GameObject.Find("MainMenu/Camera/Holder/Options");
            leMenuPanel = GameObject.Instantiate(originalOptionsMenu, originalOptionsMenu.transform.parent);

            // Change the name of the copy.
            leMenuPanel.name = "LE_Menu";

            // Remove the OptionsController and UILocalize components so I can change the title of the panel.
            GameObject.Destroy(leMenuPanel.GetComponent<OptionsController>());
            GameObject.Destroy(leMenuPanel.transform.GetChild(2).GetComponent<UILocalize>());

            // Change the title of the panel.
            leMenuPanel.transform.GetChild(2).GetComponent<UILabel>().text = "Level Editor";

            // Destroy the tabs and disable everything inside of the Game_Options object.
            GameObject.Destroy(leMenuPanel.GetChildWithName("Tabs"));
            leMenuPanel.GetChildWithName("Game_Options").SetActive(true);
            leMenuButtonsParent = leMenuPanel.GetChildAt("Game_Options/Buttons");
            leMenuButtonsParent.DisableAllChildren();

            // Disable the damn lines.
            leMenuPanel.GetChildAt("Game_Options/HorizontalLine").SetActive(false);
            leMenuPanel.GetChildAt("Game_Options/VerticalLine").SetActive(false);

            // Reset the scale of the new custom menu to one.
            leMenuPanel.transform.localScale = Vector3.one;

            // Add a UIPanel so the TweenScale can work.
            UIPanel panel = leMenuPanel.AddComponent<UIPanel>();
            leMenuPanel.GetComponent<TweenAlpha>().mRect = panel;

            leMenuPanel.GetChildWithName("Window").AddComponent<TweenAlpha>().duration = 0.2f;
        }

        public void CreateBackButton()
        {
            // Get the template, spawn the copy and set some parameters.
            GameObject template = leMenuPanel.GetChildAt("Controls_Options/Buttons/RemapControls");
            backButton = Instantiate(template, leMenuButtonsParent.transform);
            backButton.name = "BackButton";
            backButton.transform.localPosition = new Vector3(-690f, 290f, 0f);

            // Remove unnecesary components.
            GameObject.Destroy(backButton.GetComponent<ButtonController>());
            GameObject.Destroy(backButton.GetComponent<OptionsButton>());

            // Set the sprite width and height, and in the box collider as well.
            backButton.GetComponent<UISprite>().width = 250;
            backButton.GetComponent<UISprite>().height = 80;
            backButton.GetComponent<BoxCollider>().size = new Vector3(250, 80);

            // Destroy the FUCKING UILocalize component, I hate it.
            GameObject.Destroy(backButton.GetChildAt("Background/Label").GetComponent<UILocalize>());

            // Set the label data.
            UILabel label = backButton.GetChildAt("Background/Label").GetComponent<UILabel>();
            label.SetAnchor((Transform)null);
            label.CheckAnchors();
            label.transform.localPosition = new Vector3(25f, 0f, 0f);
            label.width = 360;
            label.height = 67;
            label.text = "Back";
            label.fontSize = 35;

            // Set the in-button sprite data.
            UISprite sprite = new GameObject("Image").AddComponent<UISprite>();
            sprite.transform.parent = backButton.GetChildWithName("Background").transform;
            sprite.transform.localScale = Vector3.one;
            sprite.SetExternalSprite("BackArrow");
            sprite.color = new Color(0.6235f, 1f, 0.9843f, 1f);
            sprite.width = 20;
            sprite.height = 30;
            sprite.depth = 1;
            sprite.transform.localPosition = new Vector3(-45f, 3f, 0f);

            // Set OnClick action, which is go back lol.
            UIButton button = backButton.GetComponent<UIButton>();
            button.onClick.Add(new EventDelegate(this, nameof(LE_MenuUIManager.SwitchBetweenMenuAndLEMenu)));
        }

        // The same shit as the CreateBackButton function.
        public void CreateAddButton()
        {
            // Get the template, spawn the copy and set some parameters.
            GameObject template = leMenuPanel.GetChildAt("Controls_Options/Buttons/RemapControls");
            addButton = Instantiate(template, leMenuButtonsParent.transform);
            addButton.name = "AddButton";
            addButton.transform.localPosition = new Vector3(690f, 290f, 0f);

            // Remove unnecesary components.
            GameObject.Destroy(addButton.GetComponent<ButtonController>());
            GameObject.Destroy(addButton.GetComponent<OptionsButton>());

            // Set the sprite width and height, and in the box collider as well.
            addButton.GetComponent<UISprite>().width = 250;
            addButton.GetComponent<UISprite>().height = 80;
            addButton.GetComponent<BoxCollider>().size = new Vector3(250, 80);

            // Destroy the FUCKING UILocalize component, I hate it.
            GameObject.Destroy(addButton.GetChildAt("Background/Label").GetComponent<UILocalize>());

            // Set the label data.
            UILabel label = addButton.GetChildAt("Background/Label").GetComponent<UILabel>();
            label.SetAnchor((Transform)null);
            label.CheckAnchors();
            label.transform.localPosition = new Vector3(25f, 0f, 0f);
            label.width = 360;
            label.height = 67;
            label.text = "New";
            label.fontSize = 35;

            // Set the in-button sprite data.
            UISprite sprite = new GameObject("Image").AddComponent<UISprite>();
            sprite.transform.parent = addButton.GetChildWithName("Background").transform;
            sprite.transform.localScale = Vector3.one;
            sprite.SetExternalSprite("Plus");
            sprite.color = new Color(0.6235f, 1f, 0.9843f, 1f);
            sprite.width = 30;
            sprite.height = 30;
            sprite.depth = 1;
            sprite.transform.localPosition = new Vector3(-45f, 5f, 0f);

            // Set OnClick action, which is creating a new level with a new name.
            UIButton button = addButton.GetComponent<UIButton>();
            button.onClick.Add(new EventDelegate(this, nameof(LE_MenuUIManager.CreateNewLevel)));
        }

        public void CreateLevelsList()
        {
            Dictionary<string, LevelData> levels = LevelData.GetLevelsList();
            GameObject btnTemplate = leMenuPanel.GetChildAt("Controls_Options/Buttons/RemapControls");
            currentLevelsGridID = 0;

            // Manage correctly when the parent already exists or no, since the whole UI is on DontDestroyOnLoad :').
            if (lvlButtonsParent == null)
            {
                lvlButtonsParent = new GameObject("LevelButtons");
                lvlButtonsParent.transform.parent = leMenuButtonsParent.transform;
                lvlButtonsParent.transform.localScale = Vector3.one;
            }
            else
            {
                lvlButtonsParent.DeleteAllChildren();
                lvlButtonsGrids.Clear();
            }

            List<string> keys = new List<string>(levels.Keys);

            int counter = 0;
            GameObject currentGrid = null;
            for (int i = 0; i < levels.Count; i++)
            {
                string levelFileNameWithoutExtension = keys[i];
                LevelData data = levels[levelFileNameWithoutExtension];

                if (i % 5 == 0 || i == 0) // Idk bro, this is literally copied from the OST mod LOL.
                {
                    // Create a grid.
                    currentGrid = new GameObject($"Grid {(int)(i / 5)}");
                    currentGrid.transform.parent = lvlButtonsParent.transform;
                    currentGrid.transform.localPosition = new Vector3(0f, 150f, 0f);
                    currentGrid.transform.localScale = Vector3.one;

                    // Add the UIGrid component, ofc.
                    UIGrid grid = currentGrid.AddComponent<UIGrid>();
                    grid.arrangement = UIGrid.Arrangement.Vertical;
                    grid.cellWidth = 1640f;
                    grid.cellHeight = 110f;

                    if (i != 0) currentGrid.SetActive(false);

                    lvlButtonsGrids.Add(currentGrid);
                }

                // Create the level button and set some things on it.
                GameObject lvlButton = Instantiate(btnTemplate, currentGrid.transform);
                lvlButton.name = $"Level {counter}";
                // Set the button position dinamicly.
                lvlButton.transform.localPosition = new Vector3(0f, 100f, 0f) + (new Vector3(0f, -110f, 0f) * counter);

                // Remove innecesary components.
                Destroy(lvlButton.GetComponent<ButtonController>());
                Destroy(lvlButton.GetComponent<OptionsButton>());

                // Set the sprite's size, as well in the BoxCollider.
                UISprite sprite = lvlButton.GetComponent<UISprite>();
                sprite.width = 1640;
                sprite.height = 100;
                BoxCollider collider = lvlButton.GetComponent<BoxCollider>();
                collider.size = new Vector3(1640f, 100f);

                // Change the label text.
                Destroy(lvlButton.GetChildAt("Background/Label").GetComponent<UILocalize>());
                UILabel label = lvlButton.GetChildAt("Background/Label").GetComponent<UILabel>();
                label.SetAnchor((Transform)null);
                label.CheckAnchors();
                label.width = 1200;
                label.height = 67;
                label.alignment = NGUIText.Alignment.Left;
                label.text = data.levelName;
                label.fontSize = 40;
                label.transform.localPosition = new Vector3(-180f, 0f, 0f);

                // Set button's new scale properties.
                UIButtonScale buttonScale = lvlButton.GetComponent<UIButtonScale>();
                buttonScale.mScale = Vector3.one;
                buttonScale.hover = new Vector3(1.02f, 1.02f, 1.02f);
                buttonScale.pressed = new Vector3(1.01f, 1.01f, 1.01f);

                // Set button's action.
                UIButton button = lvlButton.GetComponent<UIButton>();
                EventDelegate onClick = new EventDelegate(this, nameof(LE_MenuUIManager.LoadLevel));
                EventDelegate.Parameter parameter = new EventDelegate.Parameter
                {
                    field = "levelFileNameWithoutExtension",
                    value = levelFileNameWithoutExtension,
                    obj = this
                };
                EventDelegate.Parameter parameter2 = new EventDelegate.Parameter
                {
                    field = "levelName",
                    value = data.levelName,
                    obj = this
                };
                onClick.mParameters = new EventDelegate.Parameter[] { parameter, parameter2 };
                button.onClick.Add(onClick);

                #region Create Delete Button
                GameObject deleteBtn = Instantiate(btnTemplate, lvlButton.transform);
                deleteBtn.name = "DeleteBtn";
                deleteBtn.transform.localPosition = new Vector3(750f, 0f, 0f);

                Destroy(deleteBtn.GetComponent<ButtonController>());
                Destroy(deleteBtn.GetComponent<OptionsButton>());
                Destroy(deleteBtn.GetChildAt("Background/Label"));

                UISprite deleteSprite = deleteBtn.GetComponent<UISprite>();
                deleteSprite.width = 70;
                deleteSprite.height = 70;
                deleteSprite.depth = 1;
                BoxCollider deleteCollider = deleteBtn.GetComponent<BoxCollider>();
                deleteCollider.size = new Vector3(70f, 70f, 0f);

                UIButtonColor deleteButtonColor = deleteBtn.GetComponent<UIButtonColor>();
                deleteButtonColor.defaultColor = new Color(0.8f, 0f, 0f, 1f);
                deleteButtonColor.hover = new Color(1f, 0f, 0f, 1f);
                deleteButtonColor.pressed = new Color(0.5f, 0f, 0f, 1f);

                UISprite trashSprite = deleteBtn.GetChildWithName("Background").GetComponent<UISprite>();
                trashSprite.name = "Trash";
                trashSprite.SetExternalSprite("Trash");
                trashSprite.width = 40;
                trashSprite.height = 50;
                trashSprite.color = Color.white;
                trashSprite.transform.localPosition = Vector3.zero;
                trashSprite.enabled = true;

                UIButton deleteButton = deleteBtn.GetComponent<UIButton>();
                EventDelegate deleteOnClick = new EventDelegate(this, nameof(LE_MenuUIManager.DeleteLevel));
                EventDelegate.Parameter deleteOnClickParameter = new EventDelegate.Parameter
                {
                    field = "levelFileNameWithoutExtension",
                    value = levelFileNameWithoutExtension,
                    obj = this
                };
                deleteOnClick.mParameters = new EventDelegate.Parameter[] { deleteOnClickParameter };
                deleteButton.onClick.Add(deleteOnClick);
                #endregion

                #region Create Edit Button
                GameObject renameBtnObj = Instantiate(btnTemplate, lvlButton.transform);
                renameBtnObj.name = "EditBtn";
                renameBtnObj.transform.localPosition = new Vector3(650f, 0f, 0f);

                Destroy(renameBtnObj.GetComponent<ButtonController>());
                Destroy(renameBtnObj.GetComponent<OptionsButton>());
                Destroy(renameBtnObj.GetChildAt("Background/Label"));

                UISprite renameSprite = renameBtnObj.GetComponent<UISprite>();
                renameSprite.width = 70;
                renameSprite.height = 70;
                renameSprite.depth = 1;
                BoxCollider renameCollider = renameBtnObj.GetComponent<BoxCollider>();
                renameCollider.size = new Vector3(70f, 70f, 0f);

                UIButtonColor renameButtonColor = renameBtnObj.GetComponent<UIButtonColor>();
                renameButtonColor.defaultColor = new Color(0f, 0f, 0.8f, 1f);
                renameButtonColor.hover = new Color(0f, 0f, 1f, 1f);
                renameButtonColor.pressed = new Color(0f, 0f, 0.5f, 1f);

                UISprite pencilSprite = renameBtnObj.GetChildWithName("Background").GetComponent<UISprite>();
                pencilSprite.name = "Pencil";
                pencilSprite.SetExternalSprite("Pencil");
                pencilSprite.width = 40;
                pencilSprite.height = 50;
                pencilSprite.color = Color.white;
                pencilSprite.transform.localPosition = Vector3.zero;
                pencilSprite.enabled = true;

                UIButton renameButton = renameBtnObj.GetComponent<UIButton>();
                EventDelegate renameOnClick = new EventDelegate(this, nameof(LE_MenuUIManager.OnRenameLevelButtonClick));
                EventDelegate.Parameter renameOnClickParameter = new EventDelegate.Parameter
                {
                    field = "levelFileNameWithoutExtension",
                    value = levelFileNameWithoutExtension,
                    obj = this
                };
                EventDelegate.Parameter renameOnClickParameter2 = new EventDelegate.Parameter
                {
                    field = "lvlButtonLabelObj",
                    value = label.gameObject,
                    obj = this
                };
                renameOnClick.mParameters = new EventDelegate.Parameter[] { renameOnClickParameter, renameOnClickParameter2 };
                renameButton.onClick.Add(renameOnClick);
                #endregion

                counter++;
            }

            // If there are more than 5 levels, create the buttons to travel between lists.
            if (levels.Count> 5)
            {
                CreatePreviousListButton();
                CreateNextListButton();
            }
        }

        public void CreatePreviousListButton()
        {
            // Create the button.
            GameObject btnTemplate = leMenuPanel.GetChildAt("Controls_Options/Buttons/RemapControls");
            GameObject btnPrevious = Instantiate(btnTemplate, lvlButtonsParent.transform);
            btnPrevious.name = "BtnPrevious";
            btnPrevious.transform.localPosition = new Vector3(-840f, -70f, 0f);

            // Remove unnecesary components.
            GameObject.Destroy(btnPrevious.GetComponent<ButtonController>());
            GameObject.Destroy(btnPrevious.GetComponent<OptionsButton>());

            // Adjust the sprite and the collider as well.
            UISprite sprite = btnPrevious.GetComponent<UISprite>();
            sprite.width = 30;
            sprite.height = 100;
            BoxCollider collider = btnPrevious.GetComponent<BoxCollider>();
            collider.size = new Vector3(30f, 100f);

            // Adjust the label, removing the FUCKING UILocalize.
            GameObject.Destroy(btnPrevious.GetChildAt("Background/Label").GetComponent<UILocalize>());
            UILabel label = btnPrevious.GetChildAt("Background/Label").GetComponent<UILabel>();
            label.text = "<";

            // Set the button on click action.
            UIButton button = btnPrevious.GetComponent<UIButton>();
            button.onClick.Add(new EventDelegate(this, nameof(LE_MenuUIManager.PreviousLevelsList)));
        }
        public void CreateNextListButton()
        {
            // Create the button.
            GameObject btnTemplate = leMenuPanel.GetChildAt("Controls_Options/Buttons/RemapControls");
            GameObject btnNext = Instantiate(btnTemplate, lvlButtonsParent.transform);
            btnNext.name = "BtnNext";
            btnNext.transform.localPosition = new Vector3(840f, -70f, 0f);

            // Remove unnecesary components.
            GameObject.Destroy(btnNext.GetComponent<ButtonController>());
            GameObject.Destroy(btnNext.GetComponent<OptionsButton>());

            // Adjust the sprite and the collider as well.
            UISprite sprite = btnNext.GetComponent<UISprite>();
            sprite.width = 30;
            sprite.height = 100;
            BoxCollider collider = btnNext.GetComponent<BoxCollider>();
            collider.size = new Vector3(30f, 100f);

            // Adjust the label, removing the FUCKING UILocalize.
            GameObject.Destroy(btnNext.GetChildAt("Background/Label").GetComponent<UILocalize>());
            UILabel label = btnNext.GetChildAt("Background/Label").GetComponent<UILabel>();
            label.text = ">";

            // Set the button on click action.
            UIButton button = btnNext.GetComponent<UIButton>();
            button.onClick.Add(new EventDelegate(this, nameof(LE_MenuUIManager.NextLevelsList)));
        }


        void CreateNewLevel()
        {
            MelonCoroutines.Start(Init());

            IEnumerator Init()
            {
                // It seems even if you specify te fade to be 3 seconds long, the fade lasts less time, so I need to "split" the wait instruction.
                InGameUIManager.Instance.StartTotalFadeOut(3, true);
                yield return new WaitForSecondsRealtime(1.5f);

                SwitchBetweenMenuAndLEMenu();
                Melon<Core>.Instance.SetupTheWholeEditor();
                EditorController.Instance.levelName = LevelData.GetAvailableLevelName();
                EditorController.Instance.levelFileNameWithoutExtension = EditorController.Instance.levelName;
                LevelData.SaveLevelData(EditorController.Instance.levelName, EditorController.Instance.levelFileNameWithoutExtension);

                yield return new WaitForSecondsRealtime(1.5f);
                InGameUIManager.Instance.StartTotalFadeIn(3, true);
            }
        }
        void LoadLevel(string levelFileNameWithoutExtension, string levelName)
        {
            MelonCoroutines.Start(Init());

            IEnumerator Init()
            {
                // It seems even if you specify te fade to be 3 seconds long, the fade lasts less time, so I need to "split" the wait instruction.
                InGameUIManager.Instance.StartTotalFadeOut(3, true);
                yield return new WaitForSecondsRealtime(1.5f);

                SwitchBetweenMenuAndLEMenu();
                Melon<Core>.Instance.SetupTheWholeEditor();

                yield return new WaitForSecondsRealtime(1.5f);
                InGameUIManager.Instance.StartTotalFadeIn(3, true);
                EditorController.Instance.levelName = levelName;
                EditorController.Instance.levelFileNameWithoutExtension = levelFileNameWithoutExtension;
                LevelData.LoadLevelData(levelFileNameWithoutExtension);
            }
        }
        void DeleteLevel(string levelFileNameWithoutExtension)
        {
            LevelData.DeleteLevel(levelFileNameWithoutExtension);
            CreateLevelsList();
        }
        void OnRenameLevelButtonClick(string levelFileNameWithoutExtension, GameObject lvlButtonLabelObj)
        {
            // If the label already has an UIInput component, that means it already is initialized, just select it.
            if (lvlButtonLabelObj.TryGetComponent<UIInput>(out UIInput component))
            {
                component.isSelected = true;
            }

            // Get the UILabel component.
            UILabel label = lvlButtonLabelObj.GetComponent<UILabel>();

            // Create a UIInput component.
            UIInput input = lvlButtonLabelObj.AddComponent<UIInput>();

            // Set the UILabel on it, set the default text as the last one the UILabel had and select it automatically.
            input.label = label;
            input.text = input.label.text;
            input.isSelected = true;

            // Highlight the whole text on it.
            input.selectionStart = 0;
            input.selectionEnd = label.text.Length;

            // Set the method for when the user finishes typing the new name (OnSubmit).
            EventDelegate onSubmit = new EventDelegate(this, nameof(LE_MenuUIManager.RenameLevel));
            EventDelegate.Parameter parameter1 = new EventDelegate.Parameter
            {
                field = "levelFileNameWithoutExtension",
                value = levelFileNameWithoutExtension,
                obj = this
            };
            EventDelegate.Parameter parameter2 = new EventDelegate.Parameter
            {
                field = "input",
                value = input,
                obj = this
            };
            onSubmit.mParameters = new EventDelegate.Parameter[] { parameter1, parameter2 };
            input.onSubmit.Add(onSubmit);

            // So.... for some reason the damn NGUI doesn't call the OnSubmit function when it should, so I had to create my own fix... FUCK!
            lvlButtonLabelObj.AddComponent<UIInputSubmitFix>();
        }
        void RenameLevel(string levelFileNameWithoutExtension, UIInput input)
        {
            // Trim the text.
            input.text = input.text.Trim();

            // Rename the level.
            LevelData.RenameLevel(levelFileNameWithoutExtension, input.text);
        }


        public void ExitLEMenuInNavigationBarButton(NavigationBarController.ActionType type)
        {
            if (type == NavigationBarController.ActionType.BACK && inLEMenu)
            {
                SwitchBetweenMenuAndLEMenu();
            }
        }
        public void SwitchBetweenMenuAndLEMenu()
        {
            // Switch!
            inLEMenu = !inLEMenu;

            if (inLEMenu)
            {
                CreateLevelsList();
            }
            else
            {
                // Put the navigation bar exit button to its original state one we exit from the LE menu.
                NavigationAction action = navigationBarButtonsParent.transform.GetChild(0).GetComponent<NavigationAction>();
                action.transform.parent.gameObject.EnableAllChildren();
                action.Setup(Localization.Get("Exit").ToUpper(), "Keyboard_Black_Esc", NavigationBarController.ActionType.QUIT);
                action.onButtonClick = new Action<NavigationBarController.ActionType>(NavigationBarController.Instance.ManualButtonPressed);
            }

            MelonCoroutines.Start(Animation());

            IEnumerator Animation()
            {
                if (inLEMenu)
                {
                    uiSoundSource.clip = okSound;
                    uiSoundSource.Play();

                    mainMenu.GetComponent<TweenAlpha>().PlayIgnoringTimeScale(true);
                    leMenuPanel.SetActive(true);
                    leMenuPanel.GetComponent<TweenAlpha>().PlayIgnoringTimeScale(false);
                    leMenuPanel.GetComponent<TweenScale>().PlayIgnoringTimeScale(false);
                    yield return new WaitForSecondsRealtime(0.2f);
                    mainMenu.SetActive(false);
                }
                else
                {
                    uiSoundSource.clip = hidePageSound;
                    uiSoundSource.Play();

                    mainMenu.SetActive(true);
                    mainMenu.GetComponent<TweenAlpha>().PlayIgnoringTimeScale(false);
                    leMenuPanel.GetComponent<TweenAlpha>().PlayIgnoringTimeScale(true);
                    leMenuPanel.GetComponent<TweenScale>().PlayIgnoringTimeScale(true);
                    yield return new WaitForSecondsRealtime(0.2f);
                    leMenuPanel.SetActive(false);
                }
            }
        }

        public void PreviousLevelsList()
        {
            if (currentLevelsGridID <= 0) return;
            currentLevelsGridID--;

            lvlButtonsGrids.ForEach(grid => grid.SetActive(false));

            lvlButtonsGrids[currentLevelsGridID].SetActive(true);
        }
        public void NextLevelsList()
        {
            if (currentLevelsGridID >= lvlButtonsGrids.Count - 1) return;
            currentLevelsGridID++;

            lvlButtonsGrids.ForEach(grid => grid.SetActive(false));

            lvlButtonsGrids[currentLevelsGridID].SetActive(true);
        }
    }
}
