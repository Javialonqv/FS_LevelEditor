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
        UIButtonPatcher previousPageButton, nextPageButton;

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
            CreateOpenFolderButton();
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
                if (deletePopupEnabled)
                {
                    OnDeletePopupBackButton();
                }
                else
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
                    //LoadLevel(levelFileNameWithoutExtensionWhileGoingBackToLE, levelNameWhileGoingBackToLE);
                    EnterEditor(true, levelFileNameWithoutExtensionWhileGoingBackToLE, levelNameWhileGoingBackToLE);
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
            GameObject.Destroy(levelEditorUIButton.GetChild("Label").GetComponent<UILocalize>());
            levelEditorUIButton.GetChild("Label").GetComponent<UILabel>().text = "Level Editor";

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
            leMenuPanel = GameObject.Instantiate(NGUI_Utils.optionsPanel, NGUI_Utils.optionsPanel.transform.parent);
            leMenuPanel.name = "LE_Menu";

            // Destroy the unnecesary childs/objects.
            foreach (var child in leMenuPanel.GetChilds())
            {
                string[] notDelete = { "Window", "Title" };
                if (notDelete.Contains(child.name)) continue;

                Destroy(child);
            }

            // Change the title properties of the panel.
            UILabel title = leMenuPanel.GetChild("Title").GetComponent<UILabel>();
            title.gameObject.RemoveComponent<UILocalize>(); // I fucking hate UILocalize.
            title.transform.localPosition = new Vector3(0, 417, 0);
            title.width = 800;
            title.height = 50;
            title.text = "Level Editor";

            // Probably removing this does nothing, but just in case.
            leMenuPanel.RemoveComponent<OptionsController>();

            // Reset the scale of the new custom menu to one.
            leMenuPanel.transform.localScale = Vector3.one;

            // Adjust the UIPanel of the TweenAlpha component.
            UIPanel panel = leMenuPanel.GetComponent<UIPanel>();
            leMenuPanel.GetComponent<TweenAlpha>().mRect = panel;

            // Do I even need to explain WHAT this does?
            leMenuPanel.GetChild("Window").GetComponent<UISprite>().depth = -1;
            leMenuPanel.GetChild("Window").AddComponent<TweenAlpha>().duration = 0.2f;
            leMenuPanel.GetChildAt("Window/Window2").GetComponent<UISprite>().depth = -1;
        }

        public void CreateBackButton()
        {
            // Get the template, spawn the copy and set some parameters.
            backButton = Instantiate(NGUI_Utils.buttonTemplate, leMenuPanel.transform);
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
            sprite.transform.parent = backButton.GetChild("Background").transform;
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
            addButton = Instantiate(NGUI_Utils.buttonTemplate, leMenuPanel.transform);
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
            sprite.transform.parent = addButton.GetChild("Background").transform;
            sprite.transform.localScale = Vector3.one;
            sprite.SetExternalSprite("Plus");
            sprite.color = new Color(0.6235f, 1f, 0.9843f, 1f);
            sprite.width = 30;
            sprite.height = 30;
            sprite.depth = 1;
            sprite.transform.localPosition = new Vector3(-45f, 3f, 0f);

			// Set OnClick action, which is creating a new level with a new name.
			UIButtonPatcher patcher = addButton.AddComponent<UIButtonPatcher>();
			patcher.onClick += () => EnterEditor(false);
		}
        public void CreateOpenFolderButton()
        {
            // Get the template, spawn the copy and set some parameters.
            GameObject folderButton = Instantiate(NGUI_Utils.buttonTemplate, leMenuPanel.transform);
            folderButton.name = "OpenFolderButton";
            folderButton.transform.localPosition = new Vector3(420f, 290f, 0f); // Position it 200 units left of the Add button (690f)

            // Remove unnecessary components
            GameObject.Destroy(folderButton.GetComponent<ButtonController>());
            GameObject.Destroy(folderButton.GetComponent<OptionsButton>());

            // Set the sprite width and height, and in the box collider as well
            folderButton.GetComponent<UISprite>().width = 250;
            folderButton.GetComponent<UISprite>().height = 80;
            folderButton.GetComponent<BoxCollider>().size = new Vector3(250, 80);

            // Remove UILocalize component
            GameObject.Destroy(folderButton.GetChildAt("Background/Label").GetComponent<UILocalize>());

            // Set the label data.
            UILabel label = folderButton.GetChildAt("Background/Label").GetComponent<UILabel>();
            label.SetAnchor((Transform)null);
            label.CheckAnchors();
            label.pivot = UIWidget.Pivot.Left;
            label.alignment = NGUIText.Alignment.Left;
            label.transform.localPosition = new Vector3(-25f, 0f, 0f);
            label.width = 150;
            label.height = 50;
            label.text = "Open levels folder";
            label.fontSize = 35;

            // Set the in-button sprite data.
            UISprite sprite = new GameObject("Image").AddComponent<UISprite>();
            sprite.transform.parent = folderButton.GetChild("Background").transform;
            sprite.transform.localScale = Vector3.one;
            sprite.SetExternalSprite("Global");
            sprite.color = new Color(0.6235f, 1f, 0.9843f, 1f);
            sprite.width = 40;
            sprite.height = 40;
            sprite.depth = 1;
            sprite.transform.localPosition = new Vector3(-65f, 3f, 0f);

            // Set OnClick action to open levels folder
            UIButtonPatcher patcher = folderButton.AddComponent<UIButtonPatcher>();
            patcher.onClick += OpenLevelsFolder;
        }
        // Functions literally copied and pasted from the old taser mod LOL.
        void CreateCurrentModVersionLabel()
        {
            // Create a copy of the menu title and change its partent to the options' parent.
            GameObject version = GameObject.Instantiate(leMenuPanel.GetChild("Title"));
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
            GameObject credits = GameObject.Instantiate(leMenuPanel.GetChild("Title"));
            credits.transform.parent = leMenuPanel.transform;
            credits.name = "Credits";

            GameObject.Destroy(credits.GetComponent<UILocalize>());

            UILabel creditsLabel = credits.GetComponent<UILabel>();
            creditsLabel.text = "Created by Javialon_qv and Gray";
            creditsLabel.fontSize = 25;
            creditsLabel.alignment = NGUIText.Alignment.Left;
            creditsLabel.pivot = UIWidget.Pivot.Left;
            creditsLabel.width = 1650;
            creditsLabel.height = 35;

            creditsLabel.transform.localScale = Vector3.one;

            creditsLabel.transform.localPosition = new Vector3(-830f, -368f, 0f);
        }

        public void CreateTopLevelInfo()
        {
            GameObject labelTemplate = leMenuPanel.GetChild("Title");
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
            UIButtonPatcher btnPrevious = NGUI_Utils.CreateButton(leMenuPanel.transform, new Vector3(-840, -70), new Vector3Int(30, 100, 0), "<");
            btnPrevious.name = "BtnPrevious";

            btnPrevious.onClick += PreviousLevelsList;

            previousPageButton = btnPrevious;
        }
        public void CreateNextListButton()
        {
            UIButtonPatcher btnNext = NGUI_Utils.CreateButton(leMenuPanel.transform, new Vector3(840, -70), new Vector3Int(30, 100, 0), ">");
            btnNext.name = "BtnNext";

            btnNext.onClick += NextLevelsList;

            nextPageButton = btnNext;
        }
        //Opens the folder with all of the fun stuff
        private void OpenLevelsFolder()
        {
            string levelsPath = Path.Combine(Application.persistentDataPath, "Custom Levels").Replace('/', '\\');
            if (Directory.Exists(levelsPath))
            {
                System.Diagnostics.Process.Start("explorer.exe", $"/root,\"{levelsPath}\"");
            }
        }
        public void CreateLevelsList(int? desiredGridID = null)
        {
            Dictionary<string, LevelData> levels = LevelData.GetLevelsList();
            GameObject btnTemplate = NGUI_Utils.buttonTemplate;

            // Manage correctly when the parent already exists or not
            if (lvlButtonsParent == null)
            {
                lvlButtonsParent = new GameObject("LevelButtons");
                lvlButtonsParent.transform.parent = leMenuPanel.transform;
                lvlButtonsParent.transform.localScale = Vector3.one;
                currentLevelsGridID = 0; // Initialize only on first creation
            }
            else
            {
                lvlButtonsParent.DeleteAllChildren();
                lvlButtonsGrids.Clear();
            }

            List<string> keys = new List<string>(levels.Keys);

            // Adjust current grid ID based on desiredGridID or clamp existing value
            if (desiredGridID.HasValue)
            {
                currentLevelsGridID = desiredGridID.Value;
            }
            currentLevelsGridID = Mathf.Clamp(currentLevelsGridID, 0, Mathf.Max(0, (keys.Count - 1) / 7)); // 7 levels per grid

            GameObject currentGrid = null;
            for (int i = 0; i < keys.Count; i++)
            {
                string levelFileNameWithoutExtension = keys[i];
                LevelData data = levels[levelFileNameWithoutExtension];

                if (i % 7 == 0 || i == 0)
                {
                    currentGrid = new GameObject($"Grid {(int)(i / 7)}");
                    currentGrid.transform.parent = lvlButtonsParent.transform;
                    currentGrid.transform.localPosition = new Vector3(0f, 170f, 0f);
                    currentGrid.transform.localScale = Vector3.one;

                    UIGrid grid = currentGrid.AddComponent<UIGrid>();
                    grid.arrangement = UIGrid.Arrangement.Vertical;
                    grid.cellWidth = 1640f;
                    grid.cellHeight = 80f;

                    // Initially set all grids inactive
                    currentGrid.SetActive(false);
                    lvlButtonsGrids.Add(currentGrid);
                }


                // Create the level button parent.
                GameObject lvlButtonParent = new GameObject($"Level {i}");
                lvlButtonParent.transform.parent = currentGrid.transform;
                lvlButtonParent.transform.localScale = Vector3.one;

                #region Create Level Button
                UIButtonPatcher lvlButton = NGUI_Utils.CreateButton(lvlButtonParent.transform, new Vector3(-170, 0), new Vector3Int(1300, 70, 0), "");
                lvlButton.name = "Button";

                // If the data is null that means this .lvl file isn't a valid level file, put the sprite color red.
                if (data == null)
                {
                    lvlButton.GetComponent<UISprite>().color = new Color(0.3897f, 0.212f, 0.212f, 1f);
                }

                // Change the label text.
                lvlButton.buttonLabel.SetAnchor((Transform)null);
                lvlButton.buttonLabel.CheckAnchors();
                lvlButton.buttonLabel.width = 1370;
                lvlButton.buttonLabel.height = 67;
                lvlButton.buttonLabel.alignment = NGUIText.Alignment.Left;
                lvlButton.buttonLabel.pivot = UIWidget.Pivot.Left;
                // If the data is null put a warning in the beginning of the text, followed by the name of the file without extension, otherwise, put the real level name as usually.
                lvlButton.buttonLabel.text = data != null ? data.levelName : $"[c][ffff00][INVALID LEVEL FILE][-][/c] {levelFileNameWithoutExtension}";
                lvlButton.buttonLabel.fontSize = 40;
                lvlButton.buttonLabel.transform.localPosition = new Vector3(-620f, 0f, 0f);

                // Only setup UIButtonScale and UIButton when is a valid level file, otherwise destroy the UIButton, UIButtonScale and UIButtonColor.
                if (data != null)
                {
                    // Set button's new scale properties.
                    UIButtonScale buttonScale = lvlButton.GetComponent<UIButtonScale>();
                    buttonScale.mScale = Vector3.one;
                    buttonScale.hover = new Vector3(1.02f, 1.02f, 1.02f);
                    buttonScale.pressed = new Vector3(1.01f, 1.01f, 1.01f);

                    // Set button's action.
                    LevelButtonController btnController = lvlButton.gameObject.AddComponent<LevelButtonController>();
                    btnController.levelFileNameWithoutExtension = levelFileNameWithoutExtension;
                    btnController.levelName = data.levelName;
                    btnController.objectsCount = data.objects.Count;

                    // Create tooltip for the button.
                    FractalTooltip tooltip = lvlButton.gameObject.AddComponent<FractalTooltip>();
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
                #endregion

                #region Create Delete Button
                // Create the button and set its name and positon.
                UIButtonPatcher deleteBtn = NGUI_Utils.CreateButtonWithSprite(lvlButtonParent.transform, new Vector3(750, 0), new Vector3Int(70, 70, 0), 1, "Trash", new Vector2Int(40, 50));
                deleteBtn.name = "DeleteBtn";

                // Adjust the button color with red color variants.
                UIButtonColor deleteButtonColor = deleteBtn.GetComponent<UIButtonColor>();
                deleteButtonColor.defaultColor = new Color(0.8f, 0f, 0f, 1f);
                deleteButtonColor.hover = new Color(1f, 0f, 0f, 1f);
                deleteButtonColor.pressed = new Color(0.5f, 0f, 0f, 1f);
                deleteButtonColor.SetState(UIButtonColor.State.Normal, true);

                // Adjust what should the button execute when clicked.
                deleteBtn.onClick += () => ShowDeleteLevelPopup(levelFileNameWithoutExtension);
                #endregion

                // The edit button won't work in invalid level files.
                if (data != null)
                {
                    #region Create Edit Button
                    UIButtonPatcher renameBtn = NGUI_Utils.CreateButtonWithSprite(lvlButtonParent.transform, new Vector3(650, 0), new Vector3Int(70, 70, 0), 1, "Pencil", new Vector2Int(40, 50));
                    renameBtn.name = "EditBtn";

                    // Adjust the button color with blue color variants.
                    UIButtonColor renameButtonColor = renameBtn.GetComponent<UIButtonColor>();
                    renameButtonColor.defaultColor = new Color(0f, 0f, 0.8f, 1f);
                    renameButtonColor.hover = new Color(0f, 0f, 1f, 1f);
                    renameButtonColor.pressed = new Color(0f, 0f, 0.5f, 1f);
                    renameButtonColor.SetState(UIButtonColor.State.Normal, true);

                    // Adjust what should the button execute when clicked.
                    renameBtn.onClick += () => OnRenameLevelButtonClick(levelFileNameWithoutExtension, lvlButton.buttonLabel.gameObject);
                    #endregion
                    #region Create Play Button
                    // --- Create Play Button (Green, First) ---
                    UIButtonPatcher playBtn = NGUI_Utils.CreateButtonWithSprite(
                        lvlButtonParent.transform,
                        new Vector3(550, 0), // leftmost, adjust as needed
                        new Vector3Int(70, 70, 0),
                        1,
                        "Triangle", // Use your play icon sprite name
                        new Vector2Int(40, 50)
                    );
                    playBtn.name = "PlayBtn";

                    playBtn.buttonSprite.transform.localEulerAngles = new Vector3(0, 0, -90);

                    // Set green color
                    UIButtonColor playButtonColor = playBtn.GetComponent<UIButtonColor>();
                    playButtonColor.defaultColor = new Color(0f, 0.8f, 0f, 1f);
                    playButtonColor.hover = new Color(0f, 1f, 0f, 1f);
                    playButtonColor.pressed = new Color(0f, 0.5f, 0f, 1f);
                    playButtonColor.SetState(UIButtonColor.State.Normal, true);

					playBtn.onClick += () =>
					{
						// Skip editor load and go straight to play mode
						Melon<Core>.Instance.loadCustomLevelOnSceneLoad = true;
						Melon<Core>.Instance.levelFileNameWithoutExtensionToLoad = levelFileNameWithoutExtension;

						// Close menus and load level directly
						SwitchBetweenMenuAndLEMenu(false);
						MenuController.SoftInputAuthorized = true;
						MenuController.InputAuthorized = true;
						MenuController.GetInstance().ButtonPressed(ButtonController.Type.CHAPTER_4);
					};
					#endregion
				}
            }

            // If there are more than 5 levels, create the buttons to travel between lists.
            if (levels.Count > 5)
            {
                if (!previousPageButton && !nextPageButton)
                {
                    CreatePreviousListButton();
                    CreateNextListButton();
                }
            }

            // Activate the current grid if it exists
            if (lvlButtonsGrids.Count > 0)
            {
                // If current grid is beyond available grids, go to last grid
                if (currentLevelsGridID >= lvlButtonsGrids.Count)
                {
                    currentLevelsGridID = lvlButtonsGrids.Count - 1;
                }
                lvlButtonsGrids[currentLevelsGridID].SetActive(true);
            }


            // Doesn't matter if the buttons don't exit yet, in that case, the function won't do anything.
            RefreshChangePageButtons();
        }
		public void EnterEditor(bool isLoadingLevel = false, string levelFileNameWithoutExtension = "", string levelName = "")
        {
            if (levelButtonsWasClicked) return;
            levelButtonsWasClicked = true;

            MelonCoroutines.Start(EnterEditorRoutine(isLoadingLevel, levelFileNameWithoutExtension, levelName));
        }
        IEnumerator EnterEditorRoutine(bool isLoadingLevel = false, string levelFileNameWithoutExtension = "", string levelName = "")
        {
            // We don't need to close any menu if we're going back to LE, since we aren't going to see the main menu.
            if (!isGoingBackToLE) SwitchBetweenMenuAndLEMenu(false);

            if (isLoadingLevel && isGoingBackToLE)
            {
                // If it's going back to LE, start total fade out again to overwrite the official one so it looks like a smooth transition.
                yield return new WaitForSecondsRealtime(0.1f);
                InGameUIManager.Instance.StartTotalFadeOut(0.1f, true);
                yield return new WaitForSecondsRealtime(0.2f);
            }
            else
            {
                // It seems even if you specify te fade to be 3 seconds long, the fade lasts less time, so I need to "split" the wait instruction.
                InGameUIManager.Instance.StartTotalFadeOut(3, true);
                yield return new WaitForSecondsRealtime(1.5f);
            }

            // Remove menu music while in LE.
            GameObject.Find("MusicManager/MenuSource").GetComponent<AudioSource>().Stop();

            mainMenu.SetActive(true);
            leMenuPanel.SetActive(false);

            Melon<Core>.Instance.SetupTheWholeEditor(isLoadingLevel);

            // Once SetupTheWholeEditor is done, there's a EditorController instance already.
            if (isLoadingLevel)
            {
                EditorController.Instance.levelName = levelName;
                EditorController.Instance.levelFileNameWithoutExtension = levelFileNameWithoutExtension;
                LevelData.LoadLevelDataInEditor(levelFileNameWithoutExtension);

                if (isGoingBackToLE) // Reset the going to LE variables.
                {
                    isGoingBackToLE = false;
                    levelFileNameWithoutExtensionWhileGoingBackToLE = "";
                    levelNameWhileGoingBackToLE = "";
                }
            }
            else
            {
                string newLevelName = string.IsNullOrEmpty(levelName) ? LevelData.GetAvailableLevelName() : levelName;
                EditorController.Instance.levelName = newLevelName;
                EditorController.Instance.levelFileNameWithoutExtension = newLevelName;
                LevelData.SaveLevelData(newLevelName, newLevelName);
            }

            yield return new WaitForSecondsRealtime(1.5f);
            InGameUIManager.Instance.StartTotalFadeIn(3, true);
        }

        public void GoBackToLEWhileInPlayMode(string levelFileNameWithoutExtension, string levelName)
        {
            // If it's invoking that's probably because the player already reached an end trigger, cancel it.
            if (MenuController.GetInstance().IsInvoking("ReturnToMainMenu"))
            {
                MenuController.GetInstance().CancelInvoke("ReturnToMainMenu");
            }
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
            Destroy(onDeletePopupBackButton.GetChild("Label").GetComponent<UILocalize>());
            onDeletePopupBackButton.GetChild("Label").GetComponent<UILabel>().text = "No";
            onDeletePopupBackButton.GetComponent<UIButton>().onClick.Clear();
            onDeletePopupBackButton.GetComponent<UIButton>().onClick.Add(new EventDelegate(this, nameof(OnDeletePopupBackButton)));
            onDeletePopupBackButton.SetActive(true);

            // Same with delete button.
            onDeletePopupDeleteButton = Instantiate(popupSmallButtonsParent.GetChildAt("1_No"), popupSmallButtonsParent.transform);
            onDeletePopupDeleteButton.name = "2_Delete";
            onDeletePopupDeleteButton.transform.localPosition = new Vector3(200f, 0f, 0f);
            Destroy(onDeletePopupDeleteButton.GetComponent<ButtonController>());
            Destroy(onDeletePopupDeleteButton.GetChild("Label").GetComponent<UILocalize>());
            onDeletePopupDeleteButton.GetChild("Label").GetComponent<UILabel>().text = "Delete";
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

            int currentGridBeforeDelete = currentLevelsGridID;
            LevelData.DeleteLevel(levelFileNameWithoutExtension);

            // Rebuild list staying on current page unless it's empty
            CreateLevelsList(currentGridBeforeDelete);
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

            RefreshChangePageButtons();
        }
        public void NextLevelsList()
        {
            if (currentLevelsGridID >= lvlButtonsGrids.Count - 1) return;
            currentLevelsGridID++;

            lvlButtonsGrids.ForEach(grid => grid.SetActive(false));

            lvlButtonsGrids[currentLevelsGridID].SetActive(true);

            RefreshChangePageButtons();
        }
        void RefreshChangePageButtons()
        {
            if (!previousPageButton || !nextPageButton) return;

            // Only enable both of the buttons when we have more than one page.
            previousPageButton.gameObject.SetActive(lvlButtonsGrids.Count > 1);
            nextPageButton.gameObject.SetActive(lvlButtonsGrids.Count > 1);

            // Enable or disable the buttons depending on the current page.
            previousPageButton.button.isEnabled = currentLevelsGridID > 0;
            nextPageButton.button.isEnabled = currentLevelsGridID < lvlButtonsGrids.Count - 1;

            //Why leave them on the screen if you're on the first or last page?
            previousPageButton.gameObject.SetActive(previousPageButton.button.isEnabled);
            nextPageButton.gameObject.SetActive(nextPageButton.button.isEnabled);
        }
    }
    //It's in the name, go figure.
    public static class PlayFromMenuHelper
    {
        public static bool PlayImmediatelyOnEditorLoad = false;
        public static string LevelToPlay = null;
    }
}
