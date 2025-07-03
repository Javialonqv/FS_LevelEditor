using FS_LevelEditor.UI_Related;
using Il2Cpp;
using Il2CppInControl;
using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using FS_LevelEditor.Editor;
using FS_LevelEditor.Editor.UI;
using FS_LevelEditor.SaveSystem;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_MenuUIManager : MonoBehaviour
    {
        public static LE_MenuUIManager Instance;

        public bool inLEMenu;
        public bool isInMidTransition { get; private set; }
        bool deletePopupEnabled = false;

        // Variables outside of LE menu.
        GameObject mainMenu;
        AudioSource uiSoundSource;
        AudioClip okSound;
        AudioClip hidePageSound;
        GameObject popup;
        PopupController popupController;
        GameObject popupTitle;
        GameObject popupContentLabel;
        GameObject popupSmallButtonsParent;

        // Variables for objects/things related to LE menu.
        GameObject levelEditorUIButton;
        public GameObject leMenuPanel;
        GameObject leMenuButtonsParent;
        GameObject backButton;
        GameObject addButton;
        GameObject lvlButtonsParent;
        List<GameObject> lvlButtonsGrids = new List<GameObject>();
        int currentLevelsGridID;
        GameObject onDeletePopupBackButton;
        GameObject onDeletePopupDeleteButton;
        public bool levelButtonsWasClicked = false;
        bool isGoingBackToLE = false;
        string levelFileNameWithoutExtensionWhileGoingBackToLE = "";
        string levelNameWhileGoingBackToLE = "";
        public GameObject levelNameLabel;
        public GameObject levelObjectsLabel;

        void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Init();
        }

        void Init()
        {
            GetSomeReferences();
            CreateLEButton();
            CreateLEMenuPanel();
            CreateBackButton();
            CreateAddButton();
            CreateTopLevelInfo();
            CreateCurrentModVersionLabel();
            CreateCreditsLabel();
        }

        void Update()
        {
            if (EditorUIManager.Instance == null && levelEditorUIButton != null)
            {
                levelEditorUIButton.SetActive(Core.currentSceneName.Contains("Menu"));
            }

            // To exit from the LE menu with the ESC key.
            if (Input.GetKeyDown(KeyCode.Escape) && inLEMenu && !isInMidTransition && !EditorController.Instance)
            {
                // BUT, if the delete level popup is enabled, then hide it and do ABSOLUTELY NOTHNG.
                if (deletePopupEnabled)
                {
                    OnDeletePopupBackButton();
                }
                else // If not, then... exit from the menu :)
                {
                    SwitchBetweenMenuAndLEMenu();
                }
            }
        }

        public void OnSceneLoaded(string sceneName)
        {
            if (leMenuPanel == null)
            {
                Init();
            }

            if (sceneName.Contains("Menu"))
            {
                // Disable this so fades can work correctly.
                InGameUIManager.Instance.isInPauseMode = false;

                // Reset this variable, so the user can click level buttons again.
                levelButtonsWasClicked = false;

                if (isGoingBackToLE)
                {
                    LoadLevel(levelFileNameWithoutExtensionWhileGoingBackToLE, levelNameWhileGoingBackToLE);
                }

                // For 0.606, it seems the menu music isn't played when returning to menu after being in LE, play it just in case.
                if (!GameObject.Find("MusicManager/MenuSource").GetComponent<AudioSource>().isPlaying)
                {
                    GameObject.Find("MusicManager/MenuSource").GetComponent<AudioSource>().Play();
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

            popup = GameObject.Find("MainMenu/Camera/Holder/Popup");
            popupController = popup.GetComponent<PopupController>();
            popupTitle = popup.GetChildAt("PopupHolder/Title/Label");
            popupContentLabel = popup.GetChildAt("PopupHolder/Content/Label");
            popupSmallButtonsParent = popup.GetChildAt("PopupHolder/SmallButtons");
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
            GameObject originalOptionsMenu = NGUI_Utils.optionsPanel;
            leMenuPanel = GameObject.Instantiate(originalOptionsMenu, originalOptionsMenu.transform.parent);

            // Change the name of the copy.
            leMenuPanel.name = "LE_Menu";

            // Remove the OptionsController and UILocalize components so I can change the title of the panel.
            GameObject.Destroy(leMenuPanel.GetComponent<OptionsController>());
            GameObject.Destroy(leMenuPanel.transform.GetChild(2).GetComponent<UILocalize>());

            // Change the title properties of the panel.
            leMenuPanel.transform.GetChild(2).transform.localPosition = new Vector3(0, 417, 0);
            leMenuPanel.transform.GetChild(2).GetComponent<UILabel>().width = 800;
            leMenuPanel.transform.GetChild(2).GetComponent<UILabel>().height = 50;
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
            backButton = Instantiate(NGUI_Utils.buttonTemplate, leMenuButtonsParent.transform);
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
            label.pivot = UIWidget.Pivot.Left;
            label.alignment = NGUIText.Alignment.Left;
            label.transform.localPosition = new Vector3(-25f, 0f, 0f);
            label.width = 150;
            label.height = 50;
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
            EventDelegate.Parameter eventParm = NGUI_Utils.CreateEventDelegateParamter(this, "showMainMenu", true);
            EventDelegate buttonEvent = NGUI_Utils.CreateEvenDelegate(this, nameof(SwitchBetweenMenuAndLEMenu), eventParm);
            button.onClick.Add(buttonEvent);
        }

        // The same shit as the CreateBackButton function.
        public void CreateAddButton()
        {
            // Get the template, spawn the copy and set some parameters.
            addButton = Instantiate(NGUI_Utils.buttonTemplate, leMenuButtonsParent.transform);
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
            label.pivot = UIWidget.Pivot.Left;
            label.alignment = NGUIText.Alignment.Left;
            label.transform.localPosition = new Vector3(-25f, 0f, 0f);
            label.width = 150;
            label.height = 50;
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
            sprite.transform.localPosition = new Vector3(-45f, 3f, 0f);

            // Set OnClick action, which is creating a new level with a new name.
            UIButton button = addButton.GetComponent<UIButton>();
            button.onClick.Add(new EventDelegate(this, nameof(LE_MenuUIManager.CreateNewLevel)));
        }

        // Functions literally copied and pasted from the old taser mod LOL.
        void CreateCurrentModVersionLabel()
        {
            // Create a copy of the menu title and change its partent to the options' parent.
            GameObject version = GameObject.Instantiate(leMenuPanel.GetChildWithName("Title"));
            version.transform.parent = leMenuPanel.transform;
            version.name = "CurrentModVersion";

            // Ik this this inaccessible code, it's just I'll change that bool when I release the public build.
            string currentModVersion = "v" + Assembly.GetExecutingAssembly().GetCustomAttribute<MelonInfoAttribute>().Version;
#if DEBUG
            currentModVersion += " DEV BUILD";
#endif

            // Destroy the FUCKING UI LOCALIZE COMPONENT.
            GameObject.Destroy(version.GetComponent<UILocalize>());

            // Change its label text and font size too.
            UILabel versionLabel = version.GetComponent<UILabel>();
            versionLabel.text = currentModVersion;
            versionLabel.fontSize = 30;
            versionLabel.alignment = NGUIText.Alignment.Right;
            versionLabel.pivot = UIWidget.Pivot.Right;
            versionLabel.width = 250;

            // Reset scale to one.
            version.transform.localScale = Vector3.one;

            // Change its position to the top-right.
            version.transform.localPosition = new Vector3(830f, 417f, 0f);
        }
        void CreateCreditsLabel()
        {
            GameObject credits = GameObject.Instantiate(leMenuPanel.GetChildWithName("Title"));
            credits.transform.parent = leMenuPanel.transform;
            credits.name = "Credits";

            GameObject.Destroy(credits.GetComponent<UILocalize>());

            UILabel creditsLabel = credits.GetComponent<UILabel>();
            creditsLabel.text = "Created by Javialon_qv";
            creditsLabel.fontSize = 25;
            creditsLabel.alignment = NGUIText.Alignment.Left;
            creditsLabel.pivot = UIWidget.Pivot.Left;
            creditsLabel.width = 1650;
            creditsLabel.height = 35;

            creditsLabel.transform.localScale = Vector3.one;

            creditsLabel.transform.localPosition = new Vector3(-830f, -368f, 0f);
        }

        public void CreateLevelsList()
        {
            Dictionary<string, LevelData> levels = LevelData.GetLevelsList();
            GameObject btnTemplate = NGUI_Utils.buttonTemplate;
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

                // Create the level button parent.
                GameObject lvlButtonParent = new GameObject($"Level {counter}");
                lvlButtonParent.transform.parent = currentGrid.transform;
                lvlButtonParent.transform.localScale = Vector3.one;

                // Create the level button and set some things on it.
                GameObject lvlButton = Instantiate(btnTemplate, lvlButtonParent.transform);
                lvlButton.name = "Button";
                // Set the button position to zero.
                lvlButton.transform.localPosition = new Vector3(-110f, 0f, 0f);

                // Remove innecesary components.
                Destroy(lvlButton.GetComponent<ButtonController>());
                Destroy(lvlButton.GetComponent<OptionsButton>());

                // Set the sprite's size, as well in the BoxCollider.
                UISprite sprite = lvlButton.GetComponent<UISprite>();
                sprite.width = 1420;
                sprite.height = 100;
                // If the data is null that means this .lvl file isn't a valid level file, put the sprite color red.
                if (data == null)
                {
                    sprite.color = new Color(0.3897f, 0.212f, 0.212f, 1f);
                }
                BoxCollider collider = lvlButton.GetComponent<BoxCollider>();
                collider.size = new Vector3(1420f, 100f);

                // Change the label text.
                Destroy(lvlButton.GetChildAt("Background/Label").GetComponent<UILocalize>());
                UILabel label = lvlButton.GetChildAt("Background/Label").GetComponent<UILabel>();
                label.SetAnchor((Transform)null);
                label.CheckAnchors();
                label.width = 1370;
                label.height = 67;
                label.alignment = NGUIText.Alignment.Left;
                label.pivot = UIWidget.Pivot.Left;
                // If the data is null put a warning in the beginning of the text, followed by the name of the file without extension, otherwise, put the real level name as usually.
                label.text = data != null ? data.levelName : $"[c][ffff00][INVALID LEVEL FILE][-][/c] {levelFileNameWithoutExtension}";
                label.fontSize = 40;
                label.transform.localPosition = new Vector3(-680f, 0f, 0f);

                // Only setup UIButtonScale and UIButton when is a valid level file, otherwise destroy the UIButton, UIButtonScale and UIButtonColor.
                if (data != null)
                {
                    // Set button's new scale properties.
                    UIButtonScale buttonScale = lvlButton.GetComponent<UIButtonScale>();
                    buttonScale.mScale = Vector3.one;
                    buttonScale.hover = new Vector3(1.02f, 1.02f, 1.02f);
                    buttonScale.pressed = new Vector3(1.01f, 1.01f, 1.01f);

                    // Set button's action.
                    UIButton button = lvlButton.GetComponent<UIButton>();
                    LevelButtonController btnController = lvlButton.AddComponent<LevelButtonController>();
                    btnController.levelFileNameWithoutExtension = levelFileNameWithoutExtension;
                    btnController.levelName = data.levelName;
                    btnController.objectsCount = data.objects.Count;

                    // Create tooltip for the button.
                    FractalTooltip tooltip = lvlButton.AddComponent<FractalTooltip>();
                    string levelCreationDate = DateTimeOffset.FromUnixTimeSeconds(data.createdTime).ToLocalTime().DateTime + "";
                    string levelLastModificationDate = DateTimeOffset.FromUnixTimeSeconds(data.lastModificationTime).ToLocalTime().DateTime + "";
                    // Protection in case the level is outdated and shows a different date...
                    if (data.createdTime == 0) levelCreationDate = "[c][ff0000]OUTDATED LEVEL, SAVE TO UPDATE THE DATE.[-][/c]";
                    if (data.lastModificationTime == 0) levelLastModificationDate = "[c][ff0000]OUTDATED LEVEL, SAVE TO UPDATE THE DATE.[-][/c]";

                    tooltip.toolTipLocKey = $"[c][ffff00]Creation date:[-][/c] {levelCreationDate}" +
                                          $"\n[c][ffff00]Last modification date:[-][/c] {levelLastModificationDate}";
                }
                else
                {
                    Destroy(lvlButton.GetComponent<UIButton>());
                    Destroy(lvlButton.GetComponent<UIButtonScale>());
                    Destroy(lvlButton.GetComponent<UIButtonColor>());
                }

                #region Create Delete Button
                // Create the button and set its name and positon.
                GameObject deleteBtn = Instantiate(btnTemplate, lvlButtonParent.transform);
                deleteBtn.name = "DeleteBtn";
                deleteBtn.transform.localPosition = new Vector3(750f, 0f, 0f);

                // Destroy some unnecesary components and the label, since we're going to add a SPRITE.
                Destroy(deleteBtn.GetComponent<ButtonController>());
                Destroy(deleteBtn.GetComponent<OptionsButton>());
                Destroy(deleteBtn.GetChildAt("Background/Label"));

                // Adjust the button sprite and create the BoxCollider as well.
                UISprite deleteSprite = deleteBtn.GetComponent<UISprite>();
                deleteSprite.width = 70;
                deleteSprite.height = 70;
                deleteSprite.depth = 1;
                BoxCollider deleteCollider = deleteBtn.GetComponent<BoxCollider>();
                deleteCollider.size = new Vector3(70f, 70f, 0f);

                // Adjust the button color with red color variants.
                UIButtonColor deleteButtonColor = deleteBtn.GetComponent<UIButtonColor>();
                deleteButtonColor.defaultColor = new Color(0.8f, 0f, 0f, 1f);
                deleteButtonColor.hover = new Color(1f, 0f, 0f, 1f);
                deleteButtonColor.pressed = new Color(0.5f, 0f, 0f, 1f);

                // Create another sprite "inside" of the button one.
                UISprite trashSprite = deleteBtn.GetChildWithName("Background").GetComponent<UISprite>();
                trashSprite.name = "Trash";
                trashSprite.SetExternalSprite("Trash");
                trashSprite.width = 40;
                trashSprite.height = 50;
                trashSprite.color = Color.white;
                trashSprite.transform.localPosition = Vector3.zero;
                trashSprite.enabled = true;

                // Adjust what should the button execute when clicked.
                UIButton deleteButton = deleteBtn.GetComponent<UIButton>();
                EventDelegate deleteOnClick = new EventDelegate(this, nameof(LE_MenuUIManager.ShowDeleteLevelPopup));
                EventDelegate.Parameter deleteOnClickParameter = new EventDelegate.Parameter
                {
                    field = "levelFileNameWithoutExtension",
                    value = levelFileNameWithoutExtension,
                    obj = this
                };
                deleteOnClick.mParameters = new EventDelegate.Parameter[] { deleteOnClickParameter };
                deleteButton.onClick.Add(deleteOnClick);
                #endregion

                // The edit button woun't work in invalid level files.
                if (data != null)
                {
                    #region Create Edit Button
                    // Create the button and set its name and positon.
                    GameObject renameBtnObj = Instantiate(btnTemplate, lvlButtonParent.transform);
                    renameBtnObj.name = "EditBtn";
                    renameBtnObj.transform.localPosition = new Vector3(650f, 0f, 0f);

                    // Destroy some unnecesary components and the label, since we're going to add a SPRITE.
                    Destroy(renameBtnObj.GetComponent<ButtonController>());
                    Destroy(renameBtnObj.GetComponent<OptionsButton>());
                    Destroy(renameBtnObj.GetChildAt("Background/Label"));

                    // Adjust the button sprite and create the BoxCollider as well.
                    UISprite renameSprite = renameBtnObj.GetComponent<UISprite>();
                    renameSprite.width = 70;
                    renameSprite.height = 70;
                    renameSprite.depth = 1;
                    BoxCollider renameCollider = renameBtnObj.GetComponent<BoxCollider>();
                    renameCollider.size = new Vector3(70f, 70f, 0f);

                    // Adjust the button color with blue color variants.
                    UIButtonColor renameButtonColor = renameBtnObj.GetComponent<UIButtonColor>();
                    renameButtonColor.defaultColor = new Color(0f, 0f, 0.8f, 1f);
                    renameButtonColor.hover = new Color(0f, 0f, 1f, 1f);
                    renameButtonColor.pressed = new Color(0f, 0f, 0.5f, 1f);

                    // Create another sprite "inside" of the button one.
                    UISprite pencilSprite = renameBtnObj.GetChildWithName("Background").GetComponent<UISprite>();
                    pencilSprite.name = "Pencil";
                    pencilSprite.SetExternalSprite("Pencil");
                    pencilSprite.width = 40;
                    pencilSprite.height = 50;
                    pencilSprite.color = Color.white;
                    pencilSprite.transform.localPosition = Vector3.zero;
                    pencilSprite.enabled = true;

                    // Adjust what should the button execute when clicked.
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
                }

                counter++;
            }

            // If there are more than 5 levels, create the buttons to travel between lists.
            if (levels.Count> 5)
            {
                CreatePreviousListButton();
                CreateNextListButton();
            }
        }
        public void CreateTopLevelInfo()
        {
            GameObject labelTemplate = leMenuPanel.GetChildWithName("Title");
            levelNameLabel = Instantiate(labelTemplate, labelTemplate.transform.parent);
            levelNameLabel.name = "LevelName";
            levelNameLabel.SetActive(false);

            levelNameLabel.transform.localPosition = new Vector3(0f, 330f, 0f);
            levelNameLabel.GetComponent<UILabel>().text = "Name Test";

            levelObjectsLabel = Instantiate(labelTemplate, labelTemplate.transform.parent);
            levelObjectsLabel.name = "LevelObjectsCount";
            levelObjectsLabel.SetActive(false);

            levelObjectsLabel.transform.localPosition = new Vector3(0f, 280f, 0f);
            levelObjectsLabel.GetComponent<UILabel>().text = "Objects: 0";
            levelObjectsLabel.GetComponent<UILabel>().fontSize = 30;
        }

        public void CreatePreviousListButton()
        {
            // Create the button.
            GameObject btnPrevious = Instantiate(NGUI_Utils.buttonTemplate, lvlButtonsParent.transform);
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
            GameObject btnNext = Instantiate(NGUI_Utils.buttonTemplate, lvlButtonsParent.transform);
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
                SwitchBetweenMenuAndLEMenu(false);

                // It seems even if you specify te fade to be 3 seconds long, the fade lasts less time, so I need to "split" the wait instruction.
                InGameUIManager.Instance.StartTotalFadeOut(3, true);
                yield return new WaitForSecondsRealtime(1.5f);

                mainMenu.SetActive(true);
                leMenuPanel.SetActive(false);
                Melon<Core>.Instance.SetupTheWholeEditor();
                EditorController.Instance.levelName = LevelData.GetAvailableLevelName();
                EditorController.Instance.levelFileNameWithoutExtension = EditorController.Instance.levelName;
                LevelData.SaveLevelData(EditorController.Instance.levelName, EditorController.Instance.levelFileNameWithoutExtension);

                yield return new WaitForSecondsRealtime(1.5f);
                InGameUIManager.Instance.StartTotalFadeIn(3, true);
            }
        }
        public void LoadLevel(string levelFileNameWithoutExtension, string levelName)
        {
            if (levelButtonsWasClicked) return;

            MelonCoroutines.Start(Init());

            levelButtonsWasClicked = true;

            IEnumerator Init()
            {
                if (!isGoingBackToLE)
                {
                    // It seems even if you specify te fade to be 3 seconds long, the fade lasts less time, so I need to "split" the wait instruction.
                    InGameUIManager.Instance.StartTotalFadeOut(3, true);
                    yield return new WaitForSecondsRealtime(1.5f);
                }
                else // If it's going back to LE, start total fade out again so it looks like a smooth transition.
                {
                    yield return new WaitForSecondsRealtime(0.1f);
                    InGameUIManager.Instance.StartTotalFadeOut(0.1f, true);
                    yield return new WaitForSecondsRealtime(0.2f);

                    // Reset this variables.
                    isGoingBackToLE = false;
                    levelFileNameWithoutExtensionWhileGoingBackToLE = "";
                    levelNameWhileGoingBackToLE = "";
                }

                // Remove menu music while in LE.
                GameObject.Find("MusicManager/MenuSource").GetComponent<AudioSource>().Stop();

                mainMenu.SetActive(true);
                leMenuPanel.SetActive(false);
                Melon<Core>.Instance.SetupTheWholeEditor(true);

                yield return new WaitForSecondsRealtime(1.5f);
                InGameUIManager.Instance.StartTotalFadeIn(3, true);
                EditorController.Instance.levelName = levelName;
                EditorController.Instance.levelFileNameWithoutExtension = levelFileNameWithoutExtension;
                LevelData.LoadLevelDataInEditor(levelFileNameWithoutExtension);
            }
        }
        public void GoBackToLEWhileInPlayMode(string levelFileNameWithoutExtension, string levelName)
        {
            MenuController.GetInstance().ReturnToMainMenu();
            isGoingBackToLE = true;
            levelFileNameWithoutExtensionWhileGoingBackToLE = levelFileNameWithoutExtension;
            levelNameWhileGoingBackToLE = levelName;
        }
        void ShowDeleteLevelPopup(string levelFileNameWithoutExtension)
        {
            popupTitle.GetComponent<UILabel>().text = "Warning";
            popupContentLabel.GetComponent<UILabel>().text = "Are you sure you want to delete this level?";
            popupSmallButtonsParent.DisableAllChildren();
            popupSmallButtonsParent.transform.localPosition = new Vector3(-130f, -315f, 0f);
            popupSmallButtonsParent.GetComponent<UITable>().padding = new Vector2(130f, 0f);

            // Make a copy of the yes button since for some reason the yes button is red as the no button should, that's doesn't make any sense lol.
            onDeletePopupBackButton = Instantiate(popupSmallButtonsParent.GetChildAt("3_Yes"), popupSmallButtonsParent.transform);
            onDeletePopupBackButton.name = "1_Back";
            onDeletePopupBackButton.transform.localPosition = new Vector3(-400f, 0f, 0f);
            Destroy(onDeletePopupBackButton.GetComponent<ButtonController>());
            Destroy(onDeletePopupBackButton.GetChildWithName("Label").GetComponent<UILocalize>());
            onDeletePopupBackButton.GetChildWithName("Label").GetComponent<UILabel>().text = "No";
            onDeletePopupBackButton.GetComponent<UIButton>().onClick.Clear();
            onDeletePopupBackButton.GetComponent<UIButton>().onClick.Add(new EventDelegate(this, nameof(OnDeletePopupBackButton)));
            onDeletePopupBackButton.SetActive(true);

            // Same with delete button.
            onDeletePopupDeleteButton = Instantiate(popupSmallButtonsParent.GetChildAt("1_No"), popupSmallButtonsParent.transform);
            onDeletePopupDeleteButton.name = "2_Delete";
            onDeletePopupDeleteButton.transform.localPosition = new Vector3(200f, 0f, 0f);
            Destroy(onDeletePopupDeleteButton.GetComponent<ButtonController>());
            Destroy(onDeletePopupDeleteButton.GetChildWithName("Label").GetComponent<UILocalize>());
            onDeletePopupDeleteButton.GetChildWithName("Label").GetComponent<UILabel>().text = "Delete";
            onDeletePopupDeleteButton.GetComponent<UIButton>().onClick.Clear();

            UIButton deleteButton = onDeletePopupDeleteButton.GetComponent<UIButton>();
            EventDelegate deleteOnClick = new EventDelegate(this, nameof(LE_MenuUIManager.DeleteLevel));
            EventDelegate.Parameter deleteOnClickParameter = new EventDelegate.Parameter
            {
                field = "levelFileNameWithoutExtension",
                value = levelFileNameWithoutExtension,
                obj = this
            };
            deleteOnClick.mParameters = new EventDelegate.Parameter[] { deleteOnClickParameter };
            onDeletePopupDeleteButton.GetComponent<UIButton>().onClick.Add(deleteOnClick);
            onDeletePopupDeleteButton.SetActive(true);

            popupController.Show();
            deletePopupEnabled = true;
        }
        void OnDeletePopupBackButton()
        {
            popupController.Hide();
            deletePopupEnabled = false;

            Destroy(onDeletePopupBackButton);
            Destroy(onDeletePopupDeleteButton);
        }
        void DeleteLevel(string levelFileNameWithoutExtension)
        {
            OnDeletePopupBackButton();

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
            CreateLevelsList();
        }


        public void SwitchBetweenMenuAndLEMenu(bool showMainMenu = true)
        {
            // Switch!
            inLEMenu = !inLEMenu;


            if (inLEMenu)
            {
                Logger.Log("Switching from main menu to LE Menu!");
                CreateLevelsList();
            }
            else
            {
                Logger.Log("Switching from LE Menu to main menu!");
            }

            NavigationBarController.Instance.RefreshNavigationBarActions();

            MelonCoroutines.Start(Animation());

            IEnumerator Animation()
            {
                if (inLEMenu)
                {
                    isInMidTransition = true;

                    uiSoundSource.clip = okSound;
                    uiSoundSource.Play();

                    mainMenu.GetComponent<TweenAlpha>().PlayIgnoringTimeScale(true);
                    leMenuPanel.SetActive(true);
                    leMenuPanel.GetComponent<TweenAlpha>().PlayIgnoringTimeScale(false);
                    leMenuPanel.GetComponent<TweenScale>().PlayIgnoringTimeScale(false);
                    yield return new WaitForSecondsRealtime(0.2f);
                    
                    mainMenu.SetActive(false);
                    isInMidTransition = false;
                }
                else
                {
                    isInMidTransition = true;

                    uiSoundSource.clip = hidePageSound;
                    uiSoundSource.Play();

                    if (showMainMenu)
                    {
                        mainMenu.SetActive(true);
                        mainMenu.GetComponent<TweenAlpha>().PlayIgnoringTimeScale(false);
                    }
                    leMenuPanel.GetComponent<TweenAlpha>().PlayIgnoringTimeScale(true);
                    leMenuPanel.GetComponent<TweenScale>().PlayIgnoringTimeScale(true);
                    yield return new WaitForSecondsRealtime(0.2f);
                    leMenuPanel.SetActive(false);

                    isInMidTransition = false;
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
