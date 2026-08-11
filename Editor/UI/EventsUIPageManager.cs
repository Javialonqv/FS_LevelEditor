using FS_LevelEditor;
using FS_LevelEditor.Editor;
using FS_LevelEditor.SaveSystem;
using FS_LevelEditor.UI_Related;
using Il2Cpp;
using Il2CppSystem.Runtime.Remoting.Messaging;
using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor.Editor.UI
{
    [RegisterTypeInIl2Cpp]
    public class EventsUIPageManager : MonoBehaviour
    {
        public static EventsUIPageManager Instance { get; private set; }

        public GameObject eventsPanel;
        UILabel eventsWindowTitle;

        #region Top Buttons Related
        Transform topButtonsParent;

        UIButtonPatcher firstEventsListButton;
        UIButtonPatcher secondEventsListButton;
        UIButtonPatcher thirdEventsListButton;

        UILabel oneEventTypeLabel;
        #endregion

        #region Events List Related
        const int EVENTS_PER_PAGE = 6;

        #region UI References
        GameObject eventsListBg;
        GameObject eventsListsParent;
        List<EventButton> eventButtons = new();

        UIButtonPatcher previousEventPageButton, nextEventPageButton;
        UILabel currentEventPageLabel;
        UILabel noEventsLabel;
        #endregion

        List<string> eventsListsNames = new List<string>();
        string currentEventsListName;
        int currentEventsListID;

        int currentEventsPage = 0;

        // Current selected event.
        bool eventSelected;
        int currentSelectedEventID;
        LE_Event currentSelectedEvent;
        UIButton currentSelectedEventButton;
        ContextMenu eventsContextMenu;
        int selectedEventIDForContextMenu;

        LE_Object targetObj;
        #endregion

        #region Events Settings Related
        /// <summary>
        /// Contains all of the options of an event, including the target object name field.
        /// </summary>
        GameObject eventSettingsPanel;
        /// <summary>
        /// Contains all of the options of an event, EXCEPT the target object name field.
        /// </summary>
        GameObject eventOptionsParent;

        UICustomInputField targetObjInputField;
        GameObject currentActiveObjectPanel;
        UIButtonAsToggle moreGlobalOptionsButton;

        #region Object Panels Related
        GameObject defaultObjectsSettings;
        UIDropdownPatcher spawnOptionsDropdown;
        UIDropdownPatcher colliderStateDropdown;
        UITogglePatcher useAndLogicToggle;
        //-----------------------------------
        GameObject globalObjectsSettings;
        bool globalOptionsExpanded = false;
        UIDropdownPatcher movingStateToggle;
        UITogglePatcher resetMovementToggle;
        UICustomInputField delayInputField;
        //-----------------------------------
        GameObject sawObjectsSettings;
        UIDropdownPatcher sawStateButton;
        //-----------------------------------
        GameObject objectiveSettings;
        UIDropdownPatcher objectiveStateButton;
        //-----------------------------------
        GameObject playerSettings;
        UITogglePatcher zeroGToggle;
        UITogglePatcher invertGravityToggle;
        UITogglePatcher flashlightToggle;
        UIButtonPatcher upgradesButton;
        //-----------------------------------
        GameObject taserSettings;
        UIDropdownPatcher taserStateButton;
        UITogglePatcher changeAmmoToggle;
        UICustomInputField newAmmoInputField;
        UITogglePatcher infiniteTaserToggle;
        //-----------------------------------
        GameObject jetpackSettings;
        UIDropdownPatcher jetpackStateButton;
        //-----------------------------------
        GameObject cubeObjectsSettings;
        UITogglePatcher respawnCubeToggle;
        UITogglePatcher respawnOnLastSwitchToggle;
        //-----------------------------------
        GameObject laserObjectsSettings;
        UIDropdownPatcher laserStateButton;
        //-----------------------------------
        GameObject mineObjectsSettings;
        UIDropdownPatcher mineStateButton;
        //-----------------------------------
        GameObject lightObjectsSettings;
        UITogglePatcher changeLightColorToggle;
        UILabel newLightColorTitleLabel;
        UIInput newLightColorInputField;
        //-----------------------------------
        GameObject ceilingLightObjectsSettings;
        UIDropdownPatcher ceilingLightStateButton;
        UITogglePatcher changeCeilingLightColorToggle;
        UIInput newCeilingLightColorInputField;
        //-----------------------------------
        GameObject healthAmmoPacksObjectsSettings;
        UITogglePatcher changePackRespawnTimeToggle;
        UILabel newPackRespawnTimeTitleLabel;
        UICustomInputField newPackRespawnTimeInputField;
        UITogglePatcher spawnPackNowToggle;
        //-----------------------------------
        GameObject switchObjectsSettings;
        UIDropdownPatcher switchStateButton;
        UITogglePatcher executeSwitchActionsToggle;
        UIDropdownPatcher switchUsableStateButton;
        UIDropdownPatcher switchCanBeUsedStateButton;
        //-----------------------------------
        GameObject keypadObjectsSettings;
        UIDropdownPatcher keypadCanBeUsedStateButton;
        //-----------------------------------
        GameObject pressurePlateObjectsSettings;
        UIDropdownPatcher pressurePlateUsableStateButton;
        //-----------------------------------
        GameObject flameTrapObjectsSettings;
        UIDropdownPatcher flameTrapStateButton;
        //-----------------------------------
        GameObject screenObjectsSettings;
        UITogglePatcher changeScreenColorTypeToggle;
        UISmallButtonMultiple screenColorTypeButton;
        UITogglePatcher changeScreenTextToggle;
        UICustomInputField screenNewTextField;
        //-----------------------------------
        GameObject doorObjectsSettings;
        UIDropdownPatcher setDoorStateButton;
        //-----------------------------------
        GameObject movingPlatformObjectsSettings;
        UIDropdownPatcher movingPlatformStateButton;
        //-----------------------------------
        GameObject bridgeObjectsSettings;
        UIDropdownPatcher bridgeStateButton;
        //-----------------------------------
        GameObject destructibleWallObjectsSettings;
        UITogglePatcher destructibleWallBreakNowToggle;
        //-----------------------------------
        GameObject fragileWindowObjectsSettings;
        UITogglePatcher fragileWindowBreakNowToggle;
        //-----------------------------------
        GameObject terminalObjectsSettings;
        UIDropdownPatcher terminalActiveStateButton;
        #endregion

        #endregion

        public EventsUIPageManager(IntPtr ptr) : base (ptr) { }

        public static void Create()
        {
            if (Instance == null)
            {
                Instance = new GameObject("EventsUPageManager").AddComponent<EventsUIPageManager>();
                Instance.CreateEventsPanel();
                Instance.CreateTopButtons();
                Instance.CreateEventsListBackground();
                Instance.CreateAddEventButton();

                Instance.CreatePreviousEventsPageButton();
                Instance.CreateNextEventsPageButton();
                Instance.CreateCurrentEventsPageLabel();
                Instance.CreateNoEventsLabel();

                Instance.CreateEventsListsParent();
                Instance.CreateAllEventsButtons();

                Instance.CreateEventSettingsPanelAndOptionsParent();
                Instance.CreateTargetObjectINSTRUCTIONLabel();
                Instance.CreateTargetObjectInputField();
                Instance.CreateSelectTargetObjectButton();

                Instance.CreateDefaultObjectSettings();
                Instance.CreateGlobalObjectsSettings();
                Instance.CreateSawObjectSettings();
                Instance.CreatePlayerSettings();
                Instance.CreateTaserSettings();
                Instance.CreateJetpackSettings();
                Instance.CreateObjectiveSettings();
                Instance.CreateCubeObjectSettings();
                Instance.CreateLaserObjectSettings();
                Instance.CreateMineObjectSettings();
                Instance.CreateLightObjectSettings();
                Instance.CreateCeilingLightObjectSettings();
                Instance.CreateHealthAndAmmoPacksObjectSettings();
                Instance.CreateSwitchObjectSettings();
                Instance.CreateKeypadObjectSettings();
                Instance.CreatePressurePlateObjectSettings();
                Instance.CreateFlameTrapObjectSettings();
                Instance.CreateScreenObjectSettings();
                Instance.CreateDoorObjectSettings();
                Instance.CreateMovingPlatformObjectSettings();
                Instance.CreateBridgeObjectSettings();
                Instance.CreateDestructibleWallObjectSettings();
                Instance.CreateFragileWindowObjectSettings();
                Instance.CreateUpgradeTerminalObjectSettings();

                Instance.CreateDetails();
            }
        }

        #region Create UI
        // Method copied from LE_MenuUIManager xD
        void CreateEventsPanel()
        {
            eventsPanel = Instantiate(NGUI_Utils.optionsPanel, EditorUIManager.Instance.editorUIParent.transform);
            eventsPanel.name = "EventsPanel";

            eventsWindowTitle = eventsPanel.GetChild("Title").GetComponent<UILabel>();
            eventsWindowTitle.gameObject.RemoveComponent<UILocalize>();

            foreach (var child in eventsPanel.GetChilds())
            {
                string[] notDelete = { "Window", "Title" };
                if (notDelete.Contains(child.name)) continue;

                Destroy(child);
            }

            eventsPanel.transform.GetChild("Window").transform.localPosition = Vector3.zero;
            eventsWindowTitle.transform.localPosition = new Vector3(0f, 386.4f, 0f);

            // Remove the OptionsController and UILocalize components so I can change the title of the panel. Also the TweenAlpha since it won't be needed.
            eventsPanel.RemoveComponent<OptionsController>();
            eventsPanel.RemoveComponent<TweenAlpha>();

            // Change the title properties of the panel.
            eventsWindowTitle.transform.localPosition = new Vector3(0, 387, 0);
            eventsWindowTitle.width = 1650;
            eventsWindowTitle.height = 50;
            eventsWindowTitle.text = "Events";

            // Reset the scale of the new custom menu to one.
            eventsPanel.transform.localScale = Vector3.one;

            // Add a UIPanel so the TweenScale can work.
            // UPDATE: It already has an UIPanel LOL.
            UIPanel panel = eventsPanel.GetComponent<UIPanel>();
            panel.alpha = 1f;
            panel.depth = 1;
            eventsPanel.GetComponent<TweenAlpha>().mRect = panel;

            // Change the animation.
            eventsPanel.GetComponent<TweenScale>().from = Vector3.zero;
            eventsPanel.GetComponent<TweenScale>().to = Vector3.one;

            // Make the window transparent because Gray wants it like that, fuck it.
            eventsPanel.GetChild("Window").GetComponent<UISprite>().alpha = 0.3f;

            // Add a collider so the user can't interact with the other objects.
            eventsPanel.AddComponent<BoxCollider>().size = new Vector3(100000f, 100000f, 1f);

            // We use the occluder from the pause menu, since when you open this panel, we set the editor state to paused.
        }
        void CreateTopButtons()
        {
            topButtonsParent = new GameObject("TopButtons").transform;
            topButtonsParent.parent = eventsPanel.transform;
            topButtonsParent.localPosition = new Vector3(0f, 300f, 0f);
            topButtonsParent.localScale = Vector3.one;
            UIWidget topButtonsParentWidget = topButtonsParent.gameObject.AddComponent<UIWidget>();
            topButtonsParentWidget.width = 1480;
            topButtonsParentWidget.height = 55;

            firstEventsListButton = NGUI_Utils.CreateButton(topButtonsParent, new Vector3(-500f, 0f, 0f), new Vector3Int(480, 55, 0), "First List");
            firstEventsListButton.name = "FirstEventsListButton";
            firstEventsListButton.GetComponent<UISprite>().depth = 1;
            firstEventsListButton.onClick += () => FirstEventsListBtnClick(true);
            firstEventsListButton.GetComponent<UIButtonScale>().hover = Vector3.one * 1.05f;
            firstEventsListButton.GetComponent<UIButtonScale>().pressed = Vector3.one * 0.95f;

            secondEventsListButton = NGUI_Utils.CreateButton(topButtonsParent, new Vector3(0f, 0f, 0f), new Vector3Int(480, 55, 0), "Second List");
            secondEventsListButton.name = "SecondEventsListButton";
            secondEventsListButton.GetComponent<UISprite>().depth = 1;
            secondEventsListButton.onClick += SecondEventsListBtnClick;
            secondEventsListButton.GetComponent<UIButtonScale>().hover = Vector3.one * 1.05f;
            secondEventsListButton.GetComponent<UIButtonScale>().pressed = Vector3.one * 0.95f;

            thirdEventsListButton = NGUI_Utils.CreateButton(topButtonsParent, new Vector3(500f, 0f, 0f), new Vector3Int(480, 55, 0), "Third List");
            thirdEventsListButton.name = "ThirdEventsListButton";
            thirdEventsListButton.GetComponent<UISprite>().depth = 1;
            thirdEventsListButton.onClick += ThirdEventsListBtnClick;
            thirdEventsListButton.GetComponent<UIButtonScale>().hover = Vector3.one * 1.05f;
            thirdEventsListButton.GetComponent<UIButtonScale>().pressed = Vector3.one * 0.95f;

            oneEventTypeLabel = NGUI_Utils.CreateLabel(topButtonsParent, Vector3.zero, new Vector3Int(1480, 55, 0), "One Event Type", NGUIText.Alignment.Center,
                UIWidget.Pivot.Center);
            oneEventTypeLabel.fontSize = 30;
            oneEventTypeLabel.name = "OneEventTypeLabel";
        }
        void CreateEventsListBackground()
        {
            eventsListBg = new GameObject("EventsList");
            eventsListBg.transform.parent = eventsPanel.transform;
            eventsListBg.transform.localScale = Vector3.one;
            eventsListBg.layer = LayerMask.NameToLayer("2D GUI");

            UISprite eventsBgSprite = eventsListBg.AddComponent<UISprite>();
            eventsBgSprite.transform.localPosition = new Vector3(-400f, -90f, 0f);
            eventsBgSprite.depth = 1;
            eventsBgSprite.color = new Color(0.0509f, 0.3333f, 0.3764f, 0f);
            eventsBgSprite.width = 800;
            eventsBgSprite.height = 540;

            UIButton button = eventsListBg.AddComponent<UIButton>();
            button.defaultColor = new Color(0.0509f, 0.3333f, 0.3764f);
            button.hover = new Color(0.0509f, 0.3333f, 0.3764f);
            button.pressed = new Color(0.0509f, 0.3333f, 0.3764f);
            BoxCollider collider = eventsListBg.AddComponent<BoxCollider>();
            collider.center = Vector3.zero;
            collider.size = new Vector3(800, 540, 0);

            UIButtonPatcher patcher = eventsListBg.AddComponent<UIButtonPatcher>();
            patcher.onClick += () => OnEventSelect(null);
        }
        void CreateDetails()
        {
            GameObject optionsPanel = NGUI_Utils.optionsPanel;

            GameObject horizontalLine = Instantiate(optionsPanel.GetChildAt("Game_Options/HorizontalLine"), eventsPanel.transform);
            horizontalLine.GetComponent<UISprite>().pivot = UIWidget.Pivot.Center;
            horizontalLine.transform.localPosition = new Vector3(0f, 250f, 0f);
            horizontalLine.GetComponent<UISprite>().width = 1600;
            horizontalLine.SetActive(true);

            GameObject verticalLine = Instantiate(optionsPanel.GetChildAt("Game_Options/VerticalLine"), eventsPanel.transform);
            verticalLine.GetComponent<UISprite>().pivot = UIWidget.Pivot.Center;
            verticalLine.transform.localPosition = new Vector3(70f, -100f, 0f);
            verticalLine.GetComponent<UISprite>().height = 580;
            verticalLine.SetActive(true);

            GameObject horizontalLine2 = Instantiate(optionsPanel.GetChildAt("Game_Options/HorizontalLine"), eventSettingsPanel.transform);
            horizontalLine2.GetComponent<UISprite>().pivot = UIWidget.Pivot.Center;
            horizontalLine2.transform.localPosition = new Vector3(0f, 170f, 0f);
            horizontalLine2.GetComponent<UISprite>().width = 700;
            horizontalLine2.SetActive(true);
        }
        void CreateAddEventButton()
        {
            UIButtonPatcher addEventButton = NGUI_Utils.CreateButton(eventsPanel.transform, new Vector3(-400f, -388f, 0f), new Vector3Int(800, 50, 0), "+ Add New Event");
            addEventButton.name = "AddEventButton";
            addEventButton.GetComponent<UISprite>().depth = 1;
            addEventButton.GetComponent<UIButtonScale>().hover = Vector3.one;
            addEventButton.GetComponent<UIButtonScale>().pressed = Vector3.one * 0.95f;
            addEventButton.onClick += AddNewEvent;
        }

        void CreatePreviousEventsPageButton()
        {
            previousEventPageButton = NGUI_Utils.CreateButton(eventsListBg.transform, new Vector3(-430, 0), new Vector3Int(50, 50, 0), "<", 1, 40);
            previousEventPageButton.name = "PreviousEventsPageButton";

            previousEventPageButton.onClick += PreviousEventsPage;

            previousEventPageButton.gameObject.SetActive(false);
        }
        void CreateNextEventsPageButton()
        {
            nextEventPageButton = NGUI_Utils.CreateButton(eventsListBg.transform, new Vector3(430, 0), new Vector3Int(50, 50, 0), ">", 1, 40);
            nextEventPageButton.name = "PreviousEventsPageButton";

            nextEventPageButton.onClick += NextEventsPage;

            nextEventPageButton.gameObject.SetActive(false);
        }
        void CreateCurrentEventsPageLabel()
        {
            currentEventPageLabel = NGUI_Utils.CreateLabel(eventsListBg.transform, new Vector3(0, 300), new Vector3Int(800, 30, 0), "0/0", NGUIText.Alignment.Center, UIWidget.Pivot.Center, 30, false);
            currentEventPageLabel.name = "CurrentEventPageLabel";

            currentEventPageLabel.gameObject.SetActive(false);
        }
        void CreateNoEventsLabel()
        {
            noEventsLabel = NGUI_Utils.CreateLabel(eventsListBg.transform, new Vector3(0, 40), new Vector3Int(700, 50, 0), "No Events Yet", NGUIText.Alignment.Center, UIWidget.Pivot.Center, 30, false);
            noEventsLabel.name = "NoEventsLabel";
            noEventsLabel.color = new Color(1f, 1f, 0f, 1f);

            noEventsLabel.gameObject.SetActive(false);
        }

        void CreateEventsListsParent()
        {
            eventsListsParent = new GameObject("EventsList");
            eventsListsParent.transform.parent = eventsListBg.transform;
            eventsListsParent.transform.localPosition = new Vector3(0, 220);
            eventsListsParent.transform.localScale = Vector3.one;

            // Add the UIGrid component, ofc.
            UIGrid grid = eventsListsParent.AddComponent<UIGrid>();
            grid.arrangement = UIGrid.Arrangement.Vertical;
            grid.cellWidth = 780f;
            grid.cellHeight = 80f;
        }
        void CreateAllEventsButtons()
        {
            for (int i = 0; i < EVENTS_PER_PAGE; i++)
            {
                // Create the event button PARENT, since inside of it are the button, the name label, and delete btn.
                GameObject eventButtonParent = new GameObject($"Event {i}");
                eventButtonParent.transform.parent = eventsListsParent.transform;
                eventButtonParent.transform.localPosition = Vector3.zero;
                eventButtonParent.transform.localScale = Vector3.one;

                // Create the EVENT BUTTON itself...
                GameObject eventButton = NGUI_Utils.CreateButton(eventButtonParent.transform, Vector3.zero, new Vector3Int(780, 70, 0)).gameObject;
                eventButton.name = "Button";

                eventButton.GetComponent<UISprite>().depth = 2;

                // Change button scale options, because with the default values it looks too big.
                UIButtonScale scale = eventButton.GetComponent<UIButtonScale>();
                scale.mScale = Vector3.one;
                scale.hover = Vector3.one;
                scale.pressed = Vector3.one * 0.98f;

                // Destroy the "original" label, since it's going to be replaced with the other name label.
                Destroy(eventButton.GetChildAt("Background/Label"));

                // Destroy the UIButtonPatcher, we'll use a custom class instead:
                Destroy(eventButton.GetComponent<UIButtonPatcher>());
                EventButton eventScript = eventButton.AddComponent<EventButton>();
                eventScript.eventsManager = this;
                eventScript.eventID = 0;
                eventScript.uiButton = eventScript.GetComponent<UIButton>();

                eventButtons.Add(eventScript);

                #region Delete Button
                // Create the button and set its name and positon.
                UIButtonPatcher deleteBtn = NGUI_Utils.CreateButton(eventButtonParent.transform, new Vector3(350, 0), Vector3Int.one * 60);
                deleteBtn.name = "DeleteBtn";
                // Destroy the label, since we're going to add a SPRITE.
                Destroy(deleteBtn.gameObject.GetChildAt("Background/Label"));

                deleteBtn.GetComponent<UISprite>().depth = 3;

                // Adjust the button color with red color variants.
                UIButtonColor deleteButtonColor = deleteBtn.GetComponent<UIButtonColor>();
                deleteButtonColor.duration = 0f;
                deleteButtonColor.defaultColor = new Color(0.8f, 0f, 0f, 1f);
                deleteButtonColor.hover = new Color(1f, 0f, 0f, 1f);
                deleteButtonColor.pressed = new Color(0.5f, 0f, 0f, 1f);

                // Create another sprite "inside" of the button one.
                UISprite trashSprite = deleteBtn.gameObject.GetChild("Background").GetComponent<UISprite>();
                trashSprite.name = "Trash";
                trashSprite.SetExternalSprite("Trash");
                trashSprite.width = 30;
                trashSprite.height = 40;
                trashSprite.depth = 4;
                trashSprite.color = Color.white;
                trashSprite.transform.localPosition = Vector3.zero;
                trashSprite.enabled = true;

                eventScript.deleteBtn = deleteBtn;
                #endregion

                #region Name Input Field
                var nameInput = NGUI_Utils.CreateInputField(eventButtonParent.transform, new Vector3(-150, 0), new Vector3Int(450, 50, 0), 27, "", true, depth: 4);
                nameInput.name = "NameInputField";
                UISprite outlineSprite = nameInput.GetComponents<UISprite>()[1];
                outlineSprite.width = 455;
                outlineSprite.height = 55;

                eventScript.nameInput = nameInput;

                nameInput.GetComponents<UISprite>()[0].Invoke("MarkAsChanged", 0.01f);

                eventScript.nameInput = nameInput;
                #endregion
            }
        }

        void CreateContextMenu()
        {
            if (eventsContextMenu)
            {
                Destroy(eventsContextMenu.gameObject);
            }

            #region Copy To
            ContextMenuOption copyToOption = new ContextMenuOption()
            {
                name = "Copy To"
            };
            for (int i = 0; i < eventsListsNames.Count; i++)
            {
                int index = i;
                ContextMenuOption targetOption = new ContextMenuOption()
                {
                    name = Loc.Get("events." + eventsListsNames[index]),
                    onClick = () => CopyEventToList(selectedEventIDForContextMenu, index)
                };
                copyToOption.subOptions.Add(targetOption);
            }
            #endregion

            #region Move To
            ContextMenuOption moveToOption = new ContextMenuOption()
            {
                name = "Move To"
            };
            for (int i = 0; i < eventsListsNames.Count; i++)
            {
                int index = i;
                ContextMenuOption targetOption = new ContextMenuOption()
                {
                    name = Loc.Get("events." + eventsListsNames[index]),
                    onClick = () => MoveEventToList(selectedEventIDForContextMenu, index)
                };
                moveToOption.subOptions.Add(targetOption);
            }
            #endregion

            #region Duplicate
            ContextMenuOption duplicateOption = new ContextMenuOption()
            {
                name = "Duplicate",
                onClick = () => DuplicateEvent(selectedEventIDForContextMenu)
            };
            #endregion

            eventsContextMenu = ContextMenu.Create(eventsPanel.transform, depth: 3);
            eventsContextMenu.AddOption(copyToOption);
            eventsContextMenu.AddOption(moveToOption);
            eventsContextMenu.AddOption(duplicateOption);
        }
        #endregion

        void Update()
        {
            // Open Context Menu.
            if (Input.GetMouseButtonDown(1) && EditorUIManager.IsCurrentUIContext(EditorUIContext.EVENTS_PANEL))
            {
                if (UICamera.selectedObject.TryGetComponent<EventButton>(out var eventBtn))
                {
                    selectedEventIDForContextMenu = eventBtn.eventID;
                    CreateContextMenu();
                    eventsContextMenu.Show();
                }
            }

            // Move events Up or Down.
            if (Input.GetKey(KeyCode.LeftAlt) && (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow)))
            {
                if (currentSelectedEvent != null)
                {
                    if (Input.GetKeyDown(KeyCode.UpArrow))
                        MoveEventUp(currentSelectedEventID);
                    else if (Input.GetKeyDown(KeyCode.DownArrow))
                        MoveEventDown(currentSelectedEventID);
                }
            }
        }

        void OnDestroy()
        {
            eventButtons.Clear();
            eventsListsNames.Clear();

            eventButtons = null;
            eventsListsNames = null;

            Instance = null;
        }

        #region Events List Related
        void SetupTopButtons()
        {
            eventsListsNames = targetObj.GetAvailableEventsIDs().ToList();

            if (eventsListsNames.Count > 1) // Setup with buttons.
            {
                int buttonsCount = eventsListsNames.Count;
                float padding = 15f;

                UIWidget container = topButtonsParent.GetComponent<UIWidget>();
                float containerWidth = container.width;

                float spaceAvailableForButtons = containerWidth - padding * (buttonsCount - 1);
                float widthPerButton = spaceAvailableForButtons / buttonsCount;

                float x = -containerWidth * 0.5f; // Start from the left side of the container.

                for (int i = 0; i < topButtonsParent.childCount; i++)
                {
                    if (i > eventsListsNames.Count - 1 || topButtonsParent.GetChild(i).name == oneEventTypeLabel.name)
                    {
                        topButtonsParent.GetChild(i).gameObject.SetActive(false);
                        continue;
                    }
                    else
                    {
                        topButtonsParent.GetChild(i).gameObject.SetActive(true);
                    }

                    UIWidget buttonWidget = topButtonsParent.GetChild(i).GetComponent<UIWidget>();
                    if (buttonWidget != null)
                    {
                        buttonWidget.width = Mathf.RoundToInt(widthPerButton);
                        // According to ChatGPT, this is used to ensure NGUI draws the object correctly after the width change? Dunno, but I'll leave it as is just in case.
                        buttonWidget.SetDimensions(buttonWidget.width, buttonWidget.height);

                        float mitadAncho = widthPerButton * 0.5f;
                        buttonWidget.transform.localPosition = new Vector3(x + mitadAncho, 0, 0);

                        x += widthPerButton + padding;

                        topButtonsParent.GetChild(i).gameObject.GetChildAt("Background/Label").GetComponent<UILabel>().text = Loc.Get("events." + eventsListsNames[i]);
                    }
                }
            }
            else // Setup with the One Event Type label only.
            {
                topButtonsParent.gameObject.DisableAllChildren();

                oneEventTypeLabel.gameObject.SetActive(true);
                oneEventTypeLabel.text = Loc.Get("events." + eventsListsNames[0]);
            }
        }
        void FirstEventsListBtnClick(bool playSound = true)
        {
            // This method is the only one with the playSound parm because it's the only one I wanna call when
            // opening the events windows with NO sound at all.
            if (playSound) Utils.PlayFSUISound(Utils.FS_UISound.INTERACTION_AVAILABLE);

            firstEventsListButton.GetComponent<UIButton>().defaultColor = new Color(0f, 1f, 0f, 1f);
            secondEventsListButton.GetComponent<UIButton>().defaultColor = new Color(0.218f, 0.6464f, 0.6509f, 1f);
            thirdEventsListButton.GetComponent<UIButton>().defaultColor = new Color(0.218f, 0.6464f, 0.6509f, 1f);

            OnEventSelect(null);
            SelectList(0);
        }
        void SecondEventsListBtnClick()
        {
            Utils.PlayFSUISound(Utils.FS_UISound.INTERACTION_AVAILABLE);

            firstEventsListButton.GetComponent<UIButton>().defaultColor = new Color(0.218f, 0.6464f, 0.6509f, 1f);
            secondEventsListButton.GetComponent<UIButton>().defaultColor = new Color(0f, 1f, 0f, 1f);
            thirdEventsListButton.GetComponent<UIButton>().defaultColor = new Color(0.218f, 0.6464f, 0.6509f, 1f);

            OnEventSelect(null);
            SelectList(1);
        }
        void ThirdEventsListBtnClick()
        {
            Utils.PlayFSUISound(Utils.FS_UISound.INTERACTION_AVAILABLE);

            firstEventsListButton.GetComponent<UIButton>().defaultColor = new Color(0.218f, 0.6464f, 0.6509f, 1f);
            secondEventsListButton.GetComponent<UIButton>().defaultColor = new Color(0.218f, 0.6464f, 0.6509f, 1f);
            thirdEventsListButton.GetComponent<UIButton>().defaultColor = new Color(0f, 1f, 0f, 1f);

            OnEventSelect(null);
            SelectList(2);
        }

        void SelectList(int listID)
        {
            currentEventsListID = listID;
            currentEventsListName = eventsListsNames[listID];

            currentEventsPage = 0;
            CreateEventsPage(0);

            // Refresh the pages buttons state, the "No Events" label, etc.
            RefreshStateOfEventsListUIElements();
        }
        void CreateEventsPage(int pageID, bool showPage = true, bool deselectCurrentSelectedEvent = true)
        {
            if (deselectCurrentSelectedEvent) OnEventSelect(null);

            if (pageID > GetEventsPagesCountForCurrentListID() - 1 && GetEventsPagesCountForCurrentListID() != 0)
            {
                CreateEventsPage(GetEventsPagesCountForCurrentListID() - 1);
                return;
            }

            List<LE_Event> events = GetEventsList();
            int startIndex = pageID * EVENTS_PER_PAGE;
            int endIndex = (pageID + 1) * EVENTS_PER_PAGE;
            endIndex = Mathf.Clamp(endIndex, 0, events.Count); // Clamp the value in case endValue is greather than the available groups, otherwise the whole grid would fill up.

            eventsListsParent.DisableAllChildren();
            for (int i = startIndex; i < endIndex; i++)
            {
                int buttonID = i - startIndex;

                eventsListsParent.transform.GetChild(buttonID).gameObject.SetActive(true);
                eventsListsParent.transform.GetChild(buttonID).GetChild(0).GetComponent<EventButton>().Setup(i);
            }

            eventsListsParent.GetComponent<UIGrid>().repositionNow = true;

            currentEventsPage = pageID;
            RefreshStateOfEventsListUIElements();
        }
        void CreateEventsPageForEventOfID(int eventID, bool showPage = true)
        {
            CreateEventsPage((eventID / EVENTS_PER_PAGE), showPage);
        }

        void AddNewEvent()
        {
            Utils.PlayFSUISound(Utils.FS_UISound.INTERACTION_UNAVAILABLE);

            ((List<LE_Event>)targetObj.properties[currentEventsListName]).Add(new LE_Event());

            List<LE_Event> events = GetEventsList();
            int newEventID = events.Count - 1; // The added event will be always in the last index, duh.

            CreateEventsPageForEventOfID(newEventID);

            OnEventSelect(events.Count - 1);
        }
        void PreviousEventsPage()
        {
            if (currentEventsPage <= 0) return;

            CreateEventsPage(currentEventsPage - 1);
        }
        void NextEventsPage()
        {
            if (currentEventsPage >= GetEventsPagesCountForCurrentListID() - 1) return;

            CreateEventsPage(currentEventsPage + 1);
        }
        void RefreshStateOfEventsListUIElements()
        {
            // Only enable the page buttons and the page label once they're are more than 1 grid (1 event page).
            previousEventPageButton.gameObject.SetActive(GetEventsPagesCountForCurrentListID() > 1);
            nextEventPageButton.gameObject.SetActive(GetEventsPagesCountForCurrentListID() > 1);
            currentEventPageLabel.gameObject.SetActive(GetEventsPagesCountForCurrentListID() > 1);

            // Enable the No Events Label in case there aren't any events...
            noEventsLabel.gameObject.SetActive(GetEventsPagesCountForCurrentListID() == 0);

            // Update the state of the page buttons and the page label in case now they're enabled.
            previousEventPageButton.button.isEnabled = currentEventsPage > 0;
            nextEventPageButton.button.isEnabled = currentEventsPage < GetEventsPagesCountForCurrentListID() - 1;
            currentEventPageLabel.GetComponent<UILabel>().text = GetCurrentEventPageText();
        }
        string GetCurrentEventPageText()
        {
            return currentEventsPage + 1 + "/" + GetEventsPagesCountForCurrentListID();
        }
        internal void OnEventSelect(int? selectedID)
        {
            if (selectedID.HasValue) Utils.PlayFSUISound(Utils.FS_UISound.INTERACTION_UNAVAILABLE);

            // Reset the color of the previous selected button.
            if (currentSelectedEventButton)
            {
                currentSelectedEventButton.defaultColor = new Color(0.218f, 0.6464f, 0.6509f, 1f);
                currentSelectedEventButton.hover = new Color(0f, 0.8314f, 0.8667f, 1f);
                currentSelectedEventButton.UpdateColor(true);
            }

            if (selectedID != null)
            {
                eventSelected = true;

                // GetEventsList should return the same events list that when creating the events list, it should be fine :)
                // *Comment copied from RenameEvent() LOL.
                currentSelectedEventID = selectedID.Value;
                currentSelectedEvent = GetEventsList()[selectedID.Value];
                ShowEventSettings();

                // Set the color of the NEW selected button.
                currentSelectedEventButton = eventButtons[currentSelectedEventID % EVENTS_PER_PAGE].uiButton;
                currentSelectedEventButton.defaultColor = new Color(0f, 0.6f, 0f, 1f);
                currentSelectedEventButton.hover = new Color(0.016f, 0.831f, 0f, 1f);
                currentSelectedEventButton.UpdateColor(true);
            }
            else
            {
                eventSelected = false;

                currentSelectedEventID = 0;
                currentSelectedEvent = null;
                HideEventSettings();
            }
        }

        // Event actions.
        void CopyEventToList(int eventID, int targetListID)
        {
            LE_Event toCopy = GetEventsList()[eventID];
            List<LE_Event> targetList = GetEventsList(targetListID);

            targetList.Add(new LE_Event(toCopy));

            if (targetListID == currentEventsListID)
            {
                CreateEventsPageForEventOfID(targetList.Count - 1, false); // Just update the page where the event is going to be copied.
                if (eventSelected)
                {
                    // In case the updated page is the current one, select the event again so the button is green LOL.
                    // And yeah, I'm not checking if the updated page is the current one cause I'm lazy.
                    OnEventSelect(currentSelectedEventID);
                }
            }
        }
        void MoveEventToList(int eventID, int targetListID)
        {
            List<LE_Event> originList = GetEventsList();
            LE_Event toMove = originList[eventID];
            List<LE_Event> targetList = GetEventsList(targetListID);

            originList.Remove(toMove);
            targetList.Add(toMove);

            if (originList.Count > 0) // Update the current page we're on.
            {
                // Update the target list in case is the current one.
                if (targetListID == currentEventsListID) CreateEventsPageForEventOfID(targetList.Count - 1, false);

                CreateEventsPage(currentEventsListID, false);
                if (eventSelected) OnEventSelect(eventID > 0 ? eventID - 1 : 0);
            }
            else // Hide everything, fuck it.
            {
                // If there are no events on this list, 100% the target list wasn't the current one, no update shit.

                SelectList(currentEventsListID);
                OnEventSelect(null);
            }
        }
        void DuplicateEvent(int eventID)
        {
            List<LE_Event> list = GetEventsList();
            LE_Event toCopy = list[eventID];

            list.Add(new LE_Event(toCopy));

            // The duplicated event is in the last element in the list, only update the page, don't go there.
            CreateEventsPageForEventOfID(list.Count - 1, false);
            if (eventSelected)
            {
                // In case the updated page is the current one, select the event again so the button is green LOL.
                // And yeah, I'm not checking if the updated page is the current one cause I'm lazy.
                OnEventSelect(currentSelectedEventID);
            }
        }
        void MoveEventUp(int eventID)
        {
            if (eventID <= 0)
            {
                Logger.Error("Requested to move event up but it's already the first event.");
                return;
            }

            List<LE_Event> list = GetEventsList();
            LE_Event upEvent = list[eventID - 1];
            LE_Event toMoveUp = list[eventID];

            list[eventID - 1] = toMoveUp;
            list[eventID] = upEvent;

            if (GetPageIDForEvent(eventID - 1) < currentEventsPage) // The event was moved to another page (a previous one).
            {
                if (eventSelected && currentSelectedEvent == toMoveUp)
                {
                    // If the user was currently selecting the event that was moved, switch the page so the user is still selecting it.
                    CreateEventsPage(currentEventsPage - 1, true, false);
                    OnEventSelect(eventID - 1);
                }
                else
                {
                    CreateEventsPage(currentEventsPage, true);
                }
            }
            else // The event still in the current page.
            {
                // Update the current page and select the event only if it was selected before.
                CreateEventsPage(currentEventsPage, deselectCurrentSelectedEvent: !(eventSelected && currentSelectedEvent == toMoveUp));
                if (eventSelected && currentSelectedEvent == toMoveUp) OnEventSelect(eventID - 1);
            }
        }
        void MoveEventDown(int eventID)
        {
            List<LE_Event> list = GetEventsList();
            if (eventID >= list.Count - 1)
            {
                Logger.Error("Requested to move event down but it's already the last event.");
                return;
            }

            LE_Event downEvent = list[eventID + 1];
            LE_Event toMoveDown = list[eventID];

            list[eventID + 1] = toMoveDown;
            list[eventID] = downEvent;

            if (GetPageIDForEvent(eventID + 1) > currentEventsPage) // The event was moved to another page (a next one).
            {
                if (eventSelected && currentSelectedEvent == toMoveDown)
                {
                    // If the user was currently selecting the event that was moved, switch the page so the user is still selecting it.
                    CreateEventsPage(currentEventsPage + 1, true, false);
                    OnEventSelect(eventID + 1);
                }
                else
                {
                    CreateEventsPage(currentEventsPage + 1, false);
                }
            }
            else // The event still in the current page.
            {
                // Update the current page and select the event only if it was selected before.
                CreateEventsPage(currentEventsPage, deselectCurrentSelectedEvent: !(eventSelected && currentSelectedEvent == toMoveDown));
                if (eventSelected && currentSelectedEvent == toMoveDown) OnEventSelect(eventID + 1);
            }
        }
        public void DeleteEvent(int eventID)
        {
            int pagesBeforeRemove = GetEventsPagesCountForCurrentListID();

            OnEventSelect(null);
            GetEventsList().RemoveAt(eventID);

            CreateEventsPage(currentEventsPage);
        }
        public void RenameEvent(int eventID, UICustomInputField inputRef)
        {
            // GetEventsList should return the same events list that when creating the events list, it should be fine :)
            LE_Event eventToRename = GetEventsList()[eventID];
            eventToRename.eventName = inputRef.GetText();

            Logger.Log("RENAMED " + eventID + " TO: " + inputRef.GetText());
        }

        int GetPageIDForEvent(int eventID)
        {
            return eventID / EVENTS_PER_PAGE;
        }
        int GetEventsPagesCountForCurrentListID()
        {
            List<LE_Event> events = GetEventsList();
            return Mathf.CeilToInt((float)events.Count / EVENTS_PER_PAGE);
        }
        #endregion

        void CreateEventSettingsPanelAndOptionsParent()
        {
            eventSettingsPanel = new GameObject("EventSettings");
            eventSettingsPanel.transform.parent = eventsPanel.transform;
            eventSettingsPanel.transform.localScale = Vector3.one;
            eventSettingsPanel.transform.localPosition = new Vector3(465f, -80f, 0f);
            eventSettingsPanel.layer = LayerMask.NameToLayer("2D GUI");

            UIPanel panel = eventSettingsPanel.AddComponent<UIPanel>();
            panel.depth = 2;

            eventSettingsPanel.SetActive(false);

            eventOptionsParent = new GameObject("EventOptions");
            eventOptionsParent.transform.parent = eventSettingsPanel.transform;
            eventOptionsParent.transform.localScale = Vector3.one;
            eventOptionsParent.transform.localPosition = Vector3.zero;
            eventOptionsParent.SetActive(false);
        }
        void CreateTargetObjectINSTRUCTIONLabel()
        {
            UILabel targetObjectLabel = NGUI_Utils.CreateLabel(eventSettingsPanel.transform, new Vector3(0, 290), new Vector3Int(700, 30, 0), "Enter the target object name:", NGUIText.Alignment.Center, UIWidget.Pivot.Center, 30, false);
            targetObjectLabel.name = "TargetObjectLabel";
        }
        void CreateTargetObjectInputField()
        {
            targetObjInputField = NGUI_Utils.CreateInputField(eventSettingsPanel.transform, new Vector3(0f, 230f, 0f), new Vector3Int(500, 60, 0), 34,
                "", true, NGUIText.Alignment.Center);

            var tooltip = targetObjInputField.gameObject.AddComponent<FractalTooltip>();
            tooltip.toolTipLocKey = "EventsTargetObjectFieldTooltip";

            targetObjInputField.onChange += () => OnTargetObjectFieldChanged(targetObjInputField, targetObjInputField.GetComponent<UISprite>());
        }
        void CreateSelectTargetObjectButton()
        {
            UIButtonPatcher button = NGUI_Utils.CreateButtonWithSprite(eventSettingsPanel.transform, new Vector3(300f, 230f, 0f), new Vector3Int(60, 60, 0),
                1, "MouseClickingObj", new Vector2Int(40, 40));
            button.name = "SelectTargetObjectButton";
            button.onClick += OnSelectTargetObjectButtonClick;
        }

        void ShowEventSettings()
        {
            if (currentSelectedEvent.targetObjType != null)
            {
                var nameToSet = Loc.Get("object." + currentSelectedEvent.targetObjType) + " " + currentSelectedEvent.targetObjID;
                targetObjInputField.SetText(nameToSet);
            }
            else
            {
                if (currentSelectedEvent.isForPlayer)
                {
                    targetObjInputField.SetText(Loc.Get("Player"));
                }
                else if (currentSelectedEvent.isForTaser)
                {
                    targetObjInputField.SetText(Loc.Get("Taser"));
                }
                else if (currentSelectedEvent.isForJetpack)
                {
                    targetObjInputField.SetText(Loc.Get("Jetpack"));
                }
                else if (currentSelectedEvent.isForObjective)
                {
                    targetObjInputField.SetText("Obj_" + currentSelectedEvent.objectiveName);
                }
                else if (currentSelectedEvent.isForWait)
                {
                    string unit = currentSelectedEvent.waitTimeUnits == LE_Event.WaitTimeUnit.Seconds ? "s" : "ms";
                    targetObjInputField.SetText($"Wait {currentSelectedEvent.waitTime}{unit}");
                }
                else if (currentSelectedEvent.isForGroup)
                {
                    targetObjInputField.SetText("Group " + currentSelectedEvent.targetGroupID);
                }
                else
                {
                    targetObjInputField.SetText(currentSelectedEvent.targetObjName);
                }
            }

            UpdateEventOptionsWithEvent(currentSelectedEvent);

            eventSettingsPanel.SetActive(true);
            eventOptionsParent.DisableAllChildren();
            OnTargetObjectFieldChanged(targetObjInputField, targetObjInputField.GetComponent<UISprite>());
        }
        void HideEventSettings()
        {
            currentSelectedEvent = null;
            eventSettingsPanel.SetActive(false);
        }
        void OnTargetObjectFieldChanged(UICustomInputField input, UISprite fieldSprite)
        {
            string inputText = input.GetText();

            // This will automatically configure the variables in it, which then are used by the IsValid property.
            currentSelectedEvent.Setup(inputText);

            // If the object name that the user put there is valid and exists...
            if (currentSelectedEvent.IsValid)
            {
                fieldSprite.color = new Color(0.0588f, 0.3176f, 0.3215f, 0.9412f);
                eventOptionsParent.SetActive(true);
                eventOptionsParent.DisableAllChildren();
                currentActiveObjectPanel = null;

                bool hasGlobalOptions = false;
                if (!currentSelectedEvent.isForPlayer && !currentSelectedEvent.isForTaser && !currentSelectedEvent.isForJetpack && !currentSelectedEvent.isForObjective &&
                    !currentSelectedEvent.isForWait)
                {
                    hasGlobalOptions = true;
                }

                if (currentSelectedEvent.isForPlayer)
                {
                    currentActiveObjectPanel = playerSettings;
                }
                else if (currentSelectedEvent.isForTaser)
                {
                    currentActiveObjectPanel = taserSettings;
                }
                else if (currentSelectedEvent.isForJetpack)
                {
                    currentActiveObjectPanel = jetpackSettings;
                }
                else if (currentSelectedEvent.isForObjective)
                {
                    currentActiveObjectPanel = objectiveSettings;
                }
                else if (currentSelectedEvent.isForGroup)
                {
                    var allObjectsInGroup = LE_Object.objectsPerGroup[currentSelectedEvent.targetGroupID];
                    if (currentSelectedEvent.allObjectsInGroupAreTheSame)
                    {
                        currentActiveObjectPanel = GetOptionsPanelForObject(currentSelectedEvent.sameObjectType.Value);
                    }
                }
                else if (currentSelectedEvent.isForWait)
                {
                    // Empty, just is just so GetOptionsPanelForObject is not executed.
                }
                else
                {
                    currentActiveObjectPanel = GetOptionsPanelForObject(currentSelectedEvent.targetObjType.Value);
                }

                if (currentActiveObjectPanel) // User can decided if it shows global options or object-specific options.
                {
                    moreGlobalOptionsButton.gameObject.SetActive(true);

                    if (hasGlobalOptions) defaultObjectsSettings.SetActive(true);

                    if (globalOptionsExpanded && hasGlobalOptions)
                    {
                        globalObjectsSettings.SetActive(true);
                        currentActiveObjectPanel.SetActive(false);
                    }
                    else
                    {
                        globalObjectsSettings.SetActive(false);
                        currentActiveObjectPanel.SetActive(true);
                    }
                }
                else // Force global options to be displayed.
                {
                    moreGlobalOptionsButton.gameObject.SetActive(false);

                    if (hasGlobalOptions) defaultObjectsSettings.SetActive(true);
                    if (hasGlobalOptions) globalObjectsSettings.SetActive(true);
                }

                UpdateEventOptionsWithEvent(currentSelectedEvent);
            }
            else
            {
                fieldSprite.color = new Color(0.3215f, 0.2156f, 0.0588f, 0.9415f);
                eventOptionsParent.SetActive(false);
                eventOptionsParent.DisableAllChildren();

                currentSelectedEvent.isForPlayer = false;
                currentSelectedEvent.isForTaser = false;
                currentSelectedEvent.isForJetpack = false;
                currentSelectedEvent.isForObjective = false;
                currentSelectedEvent.isForWait = false;
                currentSelectedEvent.targetObjType = null;
                currentSelectedEvent.targetObjID = 0;
                currentSelectedEvent.targetObjName = inputText;

                moreGlobalOptionsButton.gameObject.SetActive(false);
            }
        }
        void OnSelectTargetObjectButtonClick()
        {
            EditorUIManager.Instance.SetEditorUIContext(EditorUIContext.SELECTING_TARGET_OBJ);
            EditorController.Instance.SetCurrentEditorState(EditorState.SELECTING_TARGET_OBJ);

            targetObj.TriggerAction("OnSelectTargetObjWithClickBtnClick");
        }

        public void SetTargetObjectWithLE_Object(LE_Object obj)
        {
            targetObjInputField.SetText(obj.objectFullNameWithID);

            UpdateEventOptionsWithEvent(currentSelectedEvent);
            OnTargetObjectFieldChanged(targetObjInputField, targetObjInputField.GetComponent<UISprite>());
        }

        GameObject GetOptionsPanelForObject(LE_Object.ObjectType targetObj)
        {
            if (targetObj == LE_Object.ObjectType.SAW)
            {
                return sawObjectsSettings;
            }
            else if (targetObj == LE_Object.ObjectType.CUBE)
            {
                return cubeObjectsSettings;
            }
            else if (targetObj == LE_Object.ObjectType.LASER)
            {
                return laserObjectsSettings;
            }
            else if (targetObj == LE_Object.ObjectType.MINE)
            {
                return mineObjectsSettings;
            }
            else if (targetObj == LE_Object.ObjectType.DIRECTIONAL_LIGHT || targetObj == LE_Object.ObjectType.POINT_LIGHT)
            {
                return lightObjectsSettings;
            }
            else if (targetObj == LE_Object.ObjectType.CEILING_LIGHT)
            {
                return ceilingLightObjectsSettings;
            }
            else if (targetObj == LE_Object.ObjectType.HEALTH_PACK || targetObj == LE_Object.ObjectType.AMMO_PACK)
            {
                return healthAmmoPacksObjectsSettings;
            }
            else if (targetObj == LE_Object.ObjectType.SWITCH)
            {
                return switchObjectsSettings;
            }
            else if (targetObj == LE_Object.ObjectType.KEYPAD)
            {
                return keypadObjectsSettings;
            }
            else if (targetObj == LE_Object.ObjectType.PRESSURE_PLATE)
            {
                return pressurePlateObjectsSettings;
            }
            else if (targetObj == LE_Object.ObjectType.FLAME_TRAP)
            {
                return flameTrapObjectsSettings;
            }
            else if (targetObj == LE_Object.ObjectType.SCREEN || targetObj == LE_Object.ObjectType.SMALL_SCREEN)
            {
                return screenObjectsSettings;
            }
            else if (targetObj == LE_Object.ObjectType.DOOR || targetObj == LE_Object.ObjectType.DOOR_V2)
            {
                return doorObjectsSettings;
            }
            else if (targetObj == LE_Object.ObjectType.BRIDGE)
            {
                return bridgeObjectsSettings;
            }
            else if (targetObj == LE_Object.ObjectType.MOVING_PLATFORM)
            {
                return movingPlatformObjectsSettings;
            }
            else if (targetObj == LE_Object.ObjectType.DESTRUCTIBLE_WALL)
            {
                return destructibleWallObjectsSettings;
            }
            else if (targetObj == LE_Object.ObjectType.BREAKABLE_WINDOW)
            {
                return fragileWindowObjectsSettings;
            }
            else if (targetObj == LE_Object.ObjectType.UPGRADE_TERMINAL)
            {
                return terminalObjectsSettings;
            }

            return null;
        }

        void UpdateEventOptionsWithEvent(LE_Event @event)
        {
            spawnOptionsDropdown.SelectOption((int)@event.spawn);
            colliderStateDropdown.SelectOption((int)@event.colliderState);
            useAndLogicToggle.Set(@event.useAndLogic);
            movingStateToggle.SelectOption((int)@event.moveState);
            resetMovementToggle.Set(@event.resetMovement);
            delayInputField.SetText(@event.delay, true);

            if (@event.isForPlayer)
            {
                zeroGToggle.Set(@event.enableOrDisableZeroG);
                invertGravityToggle.Set(@event.invertGravity);
                flashlightToggle.Set(@event.flashlightEnabled);
            }
            else if (@event.isForTaser)
            {
                taserStateButton.SelectOption((int)@event.taserState);
                changeAmmoToggle.Set(@event.changeAmmo);
                newAmmoInputField.SetText(@event.newAmmo);
                infiniteTaserToggle.Set(@event.infiniteTaser);
            }
            else if (@event.isForJetpack)
            {
                jetpackStateButton.SelectOption((int)@event.jetpackState);
            }
            else if (@event.isForObjective)
            {
                objectiveStateButton.SelectOption((int)@event.objectiveState);
            }
            else if (@event.isForGroup && @event.allObjectsInGroupAreTheSame)
            {
                LE_Event newEvent = new LE_Event(@event);
                newEvent.isForGroup = false;
                newEvent.targetObjType = @event.sameObjectType;

                UpdateEventOptionsWithEvent(newEvent);
                return;
            }
            else if (@event.targetObjType == LE_Object.ObjectType.SAW)
            {
                sawStateButton.SelectOption((int)@event.sawState);
            }
            else if (@event.targetObjType == LE_Object.ObjectType.CUBE)
            {
                respawnCubeToggle.Set(@event.respawnCube);
                respawnOnLastSwitchToggle.Set(@event.respawnCubeOnLastSwitch);
            }
            else if (@event.targetObjType == LE_Object.ObjectType.LASER)
            {
                laserStateButton.SelectOption((int)@event.laserState);
            }
            else if (@event.targetObjType == LE_Object.ObjectType.MINE)
            {
                mineStateButton.SelectOption((int)@event.mineState);
            }
            else if (@event.targetObjType == LE_Object.ObjectType.DIRECTIONAL_LIGHT || @event.targetObjType == LE_Object.ObjectType.POINT_LIGHT)
            {
                changeLightColorToggle.Set(@event.changeLightColor);
                newLightColorInputField.text = @event.newLightColor;
            }
            else if (@event.targetObjType == LE_Object.ObjectType.CEILING_LIGHT)
            {
                ceilingLightStateButton.SelectOption((int)@event.ceilingLightState);
                changeCeilingLightColorToggle.Set(@event.changeCeilingLightColor, true);
                newCeilingLightColorInputField.text = @event.newCeilingLightColor;
            }
            else if (@event.targetObjType == LE_Object.ObjectType.HEALTH_PACK || @event.targetObjType == LE_Object.ObjectType.AMMO_PACK)
            {
                changePackRespawnTimeToggle.Set(@event.changePackRespawnTime, true);
                newPackRespawnTimeInputField.SetText(@event.packRespawnTime, true);

                spawnPackNowToggle.Set(@event.spawnPackNow);
            }
            else if (@event.targetObjType == LE_Object.ObjectType.SWITCH)
            {
                switchStateButton.SelectOption((int)@event.switchState);
                executeSwitchActionsToggle.Set(@event.executeSwitchActions, instant: true);

                switchUsableStateButton.SelectOption((int)@event.switchUsableState);
                switchCanBeUsedStateButton.SelectOption((int)@event.canBeUsedState);
            }
            else if (@event.targetObjType == LE_Object.ObjectType.KEYPAD)
            {
                keypadCanBeUsedStateButton.SelectOption((int)@event.canBeUsedState);
            }
            else if (@event.targetObjType == LE_Object.ObjectType.PRESSURE_PLATE)
            {
                pressurePlateUsableStateButton.SelectOption((int)@event.pressurePlateUsableState);
            }
            else if (@event.targetObjType == LE_Object.ObjectType.FLAME_TRAP)
            {
                flameTrapStateButton.SelectOption((int)@event.flameTrapState);
            }
            else if (@event.targetObjType == LE_Object.ObjectType.SCREEN || @event.targetObjType == LE_Object.ObjectType.SMALL_SCREEN)
            {
                changeScreenColorTypeToggle.Set(@event.changeScreenColorType);
                screenColorTypeButton.SetOption((int)@event.screenColorType, true);

                changeScreenTextToggle.Set(@event.changeScreenText);
                screenNewTextField.SetText(@event.screenNewText);
            }
            else if (@event.targetObjType == LE_Object.ObjectType.DOOR || @event.targetObjType == LE_Object.ObjectType.DOOR_V2)
            {
                setDoorStateButton.SelectOption((int)@event.doorState);
            }
            else if (@event.targetObjType == LE_Object.ObjectType.BRIDGE)
            {
                bridgeStateButton.SelectOption((int)@event.bridgeState);
            }
            else if (@event.targetObjType == LE_Object.ObjectType.MOVING_PLATFORM)
            {
                movingPlatformStateButton.SelectOption((int)@event.movingPlatformState);
            }
            else if (@event.targetObjType == LE_Object.ObjectType.DESTRUCTIBLE_WALL)
            {
                destructibleWallBreakNowToggle.Set(@event.destructibleWallBreakNow);
            }
            else if (@event.targetObjType == LE_Object.ObjectType.BREAKABLE_WINDOW)
            {
                fragileWindowBreakNowToggle.Set(@event.fragileWindowBreakNow);
            }
            else if (@event.targetObjType == LE_Object.ObjectType.UPGRADE_TERMINAL)
            {
                terminalActiveStateButton.SelectOption((int)@event.terminalActiveState);
            }
        }

        #region Create UI Elements For Objects

        #region Default Options
        void CreateDefaultObjectSettings()
        {
            defaultObjectsSettings = new GameObject("Default");
            defaultObjectsSettings.transform.parent = eventOptionsParent.transform;
            defaultObjectsSettings.transform.localPosition = Vector3.zero;
            defaultObjectsSettings.transform.localScale = Vector3.one;
            defaultObjectsSettings.SetActive(false);

            CreateSpawnOptionsDropdown();
            CreateColliderStateDropdown();
            CreateUseAndLogicToggle();
            CreateMoreGlobalOptionsButton();
        }
        void CreateSpawnOptionsDropdown()
        {
            UIDropdownPatcher spawnOptionsDropdown = NGUI_Utils.CreateDropdown(defaultObjectsSettings.transform, new Vector3(-210, 105), Vector3.one * 0.8f);
            spawnOptionsDropdown.name = "SetActiveDropdownPanel";
            spawnOptionsDropdown.SetTitle("Spawn Options");
            spawnOptionsDropdown.AddOption("Do Nothing", true);
            spawnOptionsDropdown.AddOption("Spawn", false);
            spawnOptionsDropdown.AddOption("Despawn", false);
            spawnOptionsDropdown.AddOption("Toggle", false);

            spawnOptionsDropdown.AddOnChangeOption(new EventDelegate(this, nameof(OnSpawnOptionsDropdownChanged)));

            this.spawnOptionsDropdown = spawnOptionsDropdown;
            spawnOptionsDropdown.gameObject.SetActive(true);
        }
        void CreateColliderStateDropdown()
        {
            var colliderStateDropdown = NGUI_Utils.CreateDropdown(defaultObjectsSettings.transform, new Vector3(150, 105), Vector3.one * 0.8f);
            colliderStateDropdown.name = "ColliderStateDropdown";
            colliderStateDropdown.SetTitle("Collider State");
            colliderStateDropdown.AddOption("Do Nothing", true);
            colliderStateDropdown.AddOption("Enable", false);
            colliderStateDropdown.AddOption("Disable", false);
            colliderStateDropdown.AddOption("Toggle", false);

            colliderStateDropdown.AddOnChangeOption(new EventDelegate(this, nameof(OnColliderStateDropdownChanged)));

            this.colliderStateDropdown = colliderStateDropdown;
            colliderStateDropdown.gameObject.SetActive(true);
        }
        void CreateUseAndLogicToggle()
        {
            useAndLogicToggle = NGUI_Utils.CreateToggle(defaultObjectsSettings.transform, new Vector3(-395, 230f, 0f),
                new Vector3Int(70, 48, 1), "AND");
            useAndLogicToggle.gameObject.name = "UseAndLogicToggle";
            useAndLogicToggle.onClick += (state) => OnUseAndLogicToggleChanged();
        }
        void CreateMoreGlobalOptionsButton()
        {
            moreGlobalOptionsButton = NGUI_Utils.CreateButtonAsToggleWithSprite(defaultObjectsSettings.transform, new Vector3(360, 120), Vector3Int.one * 55, 2, "Global", new Vector2Int(35, 35));
            moreGlobalOptionsButton.onClick += OnMoreGlobalOptionsButtonClicked;
        }
        #endregion

        #region Global Options
        void CreateGlobalObjectsSettings()
        {
            globalObjectsSettings = new GameObject("Global");
            globalObjectsSettings.transform.parent = eventOptionsParent.transform;
            globalObjectsSettings.transform.localPosition = Vector3.zero;
            globalObjectsSettings.transform.localScale = Vector3.one;
            globalObjectsSettings.SetActive(false);

            CreateGlobalObjectsTitleLabel();
            CreateMoveStateDropdown();
            CreateResetMovementToggle();
            CreateDelayInputField();
        }
        void CreateGlobalObjectsTitleLabel()
        {
            UILabel label = NGUI_Utils.CreateLabel(globalObjectsSettings.transform, new Vector3(0, 40), new Vector3Int(700, 60, 0), "GLOBAL OPTIONS", NGUIText.Alignment.Center, UIWidget.Pivot.Center, 35, false);
            label.name = "TitleLabel";
        }
        void CreateMoveStateDropdown()
        {
            movingStateToggle = NGUI_Utils.CreateDropdown(globalObjectsSettings.transform, new Vector3(0f, -40f, 0f), Vector3.one * 0.8f);
            movingStateToggle.gameObject.name = "MoveStateDropdown";
            movingStateToggle.SetTitle("Move State");
            movingStateToggle.AddOption("Do Nothing", true);
            movingStateToggle.AddOption("Start Moving", false);
            movingStateToggle.AddOption("Stop Moving", false);
            movingStateToggle.AddOption("Start/Stop Moving", false);
            movingStateToggle.AddOnChangeOption((state) => OnMoveStateDropdownChanged());
        }
        void CreateResetMovementToggle()
        {
            resetMovementToggle = NGUI_Utils.CreateToggle(globalObjectsSettings.transform, new Vector3(-150, -90), new Vector3Int(250, 48, 1), "Reset Movement");
            resetMovementToggle.name = "ResetMovementToggle";
            resetMovementToggle.onClick += (state) => OnResetMovementToggleChanged();
        }
        void CreateDelayInputField()
        {
            // Create the label for the delay field
            UILabel delayLabel = NGUI_Utils.CreateLabel(globalObjectsSettings.transform, new Vector3(-300f, -150f, 0f),
                new Vector3Int(150, 40, 0), "Delay (s)", NGUIText.Alignment.Left, UIWidget.Pivot.Left);
            delayLabel.name = "DelayLabel";
            delayLabel.color = NGUI_Utils.fsLabelDefaultColor;
            delayLabel.fontSize = 27;

            // Create the input field for the delay value
            delayInputField = NGUI_Utils.CreateInputField(globalObjectsSettings.transform, new Vector3(50f, -150f, 0f),
                new Vector3Int(200, 40, 1), 27, "0", inputType: UICustomInputField.UIInputType.NON_NEGATIVE_FLOAT);
            delayInputField.name = "DelayInputField";
            delayInputField.onChange += OnDelayInputFieldChanged;
        }
        #endregion

        #region Saw Options
        void CreateSawObjectSettings()
        {
            sawObjectsSettings = new GameObject("Saw");
            sawObjectsSettings.transform.parent = eventOptionsParent.transform;
            sawObjectsSettings.transform.localPosition = Vector3.zero;
            sawObjectsSettings.transform.localScale = Vector3.one;
            sawObjectsSettings.SetActive(false);

            CreateSawObjectsTitleLabel();
            CreateSawStateDropdown();
        }
        void CreateSawObjectsTitleLabel()
        {
            UILabel label = NGUI_Utils.CreateLabel(sawObjectsSettings.transform, new Vector3(0, 40), new Vector3Int(700, 40, 0), "SAW OPTIONS", NGUIText.Alignment.Center, UIWidget.Pivot.Center, 35, false);
            label.name = "TitleLabel";
        }
        void CreateSawStateDropdown()
        {
            sawStateButton = NGUI_Utils.CreateDropdown(sawObjectsSettings.transform, new Vector3(0, -50), Vector3.one * 0.8f);
            sawStateButton.SetTitle("Saw State");
            sawStateButton.AddOption("Do Nothing", true);
            sawStateButton.AddOption("Activate", false);
            sawStateButton.AddOption("Deactivate", false);
            sawStateButton.AddOption("Toggle State", false);
            sawStateButton.AddOnChangeOption(new EventDelegate(this, nameof(OnSawStateDropdownChanged)));

            sawStateButton.gameObject.SetActive(true);
        }
        #endregion

        #region Player Options
        void CreatePlayerSettings()
        {
            playerSettings = new GameObject("Player");
            playerSettings.transform.parent = eventOptionsParent.transform;
            playerSettings.transform.localPosition = Vector3.zero;
            playerSettings.transform.localScale = Vector3.one;
            playerSettings.SetActive(false);

            CreatePlayerSettingsTitleLabel();
            CreateZeroGToggle();
            CreateInvertGravityToggle();
            CreateFlashlightToggle();
            CreateUpgradesButton();
        }
        void CreatePlayerSettingsTitleLabel()
        {
            UILabel label = NGUI_Utils.CreateLabel(playerSettings.transform, new Vector3(0, 120), new Vector3Int(700, 40, 0), "PLAYER OPTIONS", NGUIText.Alignment.Center, UIWidget.Pivot.Center, 35, false);
            label.name = "TitleLabel";
        }
        void CreateZeroGToggle()
        {
            zeroGToggle = NGUI_Utils.CreateToggle(playerSettings.transform, new Vector3(-380f, 50f, 0f),
                new Vector3Int(250, 48, 1), "Enable/Disable Zero G");
            zeroGToggle.gameObject.name = "EnableOrDisableZeroGToggle";
            zeroGToggle.onClick += (state) => OnZeroGToggleChanged();
        }
        void CreateInvertGravityToggle()
        {
            invertGravityToggle = NGUI_Utils.CreateToggle(playerSettings.transform, new Vector3(50f, 50f, 0f),
                new Vector3Int(250, 48, 1), "Invert Gravity");
            invertGravityToggle.gameObject.name = "InvertGravityToggle";
            invertGravityToggle.onClick += (state) => OnInvertGravityToggleChanged();
        }
        void CreateFlashlightToggle()
        {
            flashlightToggle = NGUI_Utils.CreateToggle(playerSettings.transform, new Vector3(-380f, -30f, 0f),
                new Vector3Int(250, 48, 1), "Enable/Disable Flashlight");
            flashlightToggle.gameObject.name = "EnableOrDisableFlashlightToggle";
            flashlightToggle.onClick += (state) => OnFlashlightToggleChanged();
        }
        void CreateUpgradesButton()
        {
            upgradesButton = NGUI_Utils.CreateButton(playerSettings.transform, new Vector3(0, -100), new Vector3Int(300, 50, 0), "Player Upgrades");
            upgradesButton.name = "UpgradesButton";
            upgradesButton.onClick += OnUpgradesButtonPressed;
        }
        #endregion

        #region Taser Options
        void CreateTaserSettings()
        {
            taserSettings = new GameObject("Taser");
            taserSettings.transform.parent = eventOptionsParent.transform;
            taserSettings.transform.localPosition = Vector3.zero;
            taserSettings.transform.localScale = Vector3.one;
            taserSettings.SetActive(false);

            CreateTaserSettingsTitleLabel();
            CreateTaserStateButton();
            CreateChangeAmmoToggle();
            CreateNewAmmoInputField();
            CreateInfiniteTaserToggle();
        }
        void CreateTaserSettingsTitleLabel()
        {
            UILabel label = NGUI_Utils.CreateLabel(taserSettings.transform, new Vector3(0, 120), new Vector3Int(700, 40, 0), "TASER OPTIONS", NGUIText.Alignment.Center, UIWidget.Pivot.Center, 35, false);
            label.name = "TitleLabel";
        }
        void CreateTaserStateButton()
        {
            taserStateButton = NGUI_Utils.CreateDropdown(taserSettings.transform, new Vector3(-200, 20), Vector3.one * 0.8f);
            taserStateButton.SetTitle("Taser");
            taserStateButton.AddOption("Do Nothing", true);
            taserStateButton.AddOption("Give", false);
            taserStateButton.AddOption("Take Away", false);
            taserStateButton.AddOnChangeOption(new EventDelegate(this, nameof(OnTaserStateButtonChanged)));

            taserStateButton.gameObject.SetActive(true);
        }
        void CreateChangeAmmoToggle()
        {
            changeAmmoToggle = NGUI_Utils.CreateToggle(taserSettings.transform, new Vector3(54f, 50f, 0f),
                new Vector3Int(250, 48, 1), "Change Ammo");
            changeAmmoToggle.gameObject.name = "ChangeAmmoToggle";
            changeAmmoToggle.onClick += (state) => OnChangeAmmoToggleChanged();
        }
        void CreateNewAmmoInputField()
        {
            newAmmoInputField = NGUI_Utils.CreateInputField(taserSettings.transform, new Vector3(203f, -35f, 0f),
                new Vector3Int(250, 40, 1), 27, "10", inputType: UICustomInputField.UIInputType.NON_NEGATIVE_INT);
            newAmmoInputField.name = "NewAmmoInputField";
            newAmmoInputField.onChange += OnNewAmmoInputFieldChanged;
            newAmmoInputField.gameObject.SetActive(false);
        }
        void CreateInfiniteTaserToggle()
        {
            infiniteTaserToggle = NGUI_Utils.CreateToggle(taserSettings.transform, new Vector3(-300f, -35f, 0f),
                new Vector3Int(250, 48, 1), "Infinite Ammo");
            infiniteTaserToggle.gameObject.name = "InfiniteTaserToggle";
            infiniteTaserToggle.onClick += (state) => OnInfiniteTaserToggleChanged();
            infiniteTaserToggle.gameObject.SetActive(false);
        }
        #endregion

        #region Jetpack Options
        void CreateJetpackSettings()
        {
            jetpackSettings = new GameObject("Jetpack");
            jetpackSettings.transform.parent = eventOptionsParent.transform;
            jetpackSettings.transform.localPosition = Vector3.zero;
            jetpackSettings.transform.localScale = Vector3.one;
            jetpackSettings.SetActive(false);

            CreateJetpackSettingsTitleLabel();
            CreateJetpackStateButton();
        }
        void CreateJetpackSettingsTitleLabel()
        {
            UILabel label = NGUI_Utils.CreateLabel(jetpackSettings.transform, new Vector3(0, 40), new Vector3Int(700, 40, 0), "JETPACK OPTIONS", NGUIText.Alignment.Center, UIWidget.Pivot.Center, 35, false);
            label.name = "TitleLabel";
        }
        void CreateJetpackStateButton()
        {
            jetpackStateButton = NGUI_Utils.CreateDropdown(jetpackSettings.transform, new Vector3(0, -50), Vector3.one * 0.8f);
            jetpackStateButton.SetTitle("Jetpack");
            jetpackStateButton.AddOption("Do Nothing", true);
            jetpackStateButton.AddOption("Give", false);
            jetpackStateButton.AddOption("Take Away", false);
            jetpackStateButton.AddOnChangeOption(new EventDelegate(this, nameof(OnJetpackStateButtonChanged)));

            jetpackStateButton.gameObject.SetActive(true);
        }
        #endregion

        #region Objective Options
        void CreateObjectiveSettings()
        {
            objectiveSettings = new GameObject("Objective");
            objectiveSettings.transform.parent = eventOptionsParent.transform;
            objectiveSettings.transform.localPosition = Vector3.zero;
            objectiveSettings.transform.localScale = Vector3.one;
            objectiveSettings.SetActive(false);

            CreateObjectiveSettingsTitleLabel();
            CreateObjectiveStateButton();
        }
        void CreateObjectiveSettingsTitleLabel()
        {
            UILabel label = NGUI_Utils.CreateLabel(objectiveSettings.transform, new Vector3(0, 120), new Vector3Int(700, 40, 0), "OBJECTIVE OPTIONS", NGUIText.Alignment.Center, UIWidget.Pivot.Center, 35, false);
            label.name = "TitleLabel";
        }
        void CreateObjectiveStateButton()
        {
            objectiveStateButton = NGUI_Utils.CreateDropdown(objectiveSettings.transform, new Vector3(0, -50), Vector3.one * 0.8f);
            objectiveStateButton.SetTitle("Objective");
            objectiveStateButton.AddOption("Do Nothing", false);
            objectiveStateButton.AddOption("Create", true);
            objectiveStateButton.AddOption("Accomplish", false);
            objectiveStateButton.AddOption("Fail", false);
            objectiveStateButton.AddOnChangeOption(new EventDelegate(this, nameof(OnObjectiveStateButtonChanged)));

            objectiveStateButton.gameObject.SetActive(true);
        }
        #endregion

        #region Cube Options
        void CreateCubeObjectSettings()
        {
            cubeObjectsSettings = new GameObject("Cube");
            cubeObjectsSettings.transform.parent = eventOptionsParent.transform;
            cubeObjectsSettings.transform.localPosition = Vector3.zero;
            cubeObjectsSettings.transform.localScale = Vector3.one;
            cubeObjectsSettings.SetActive(false);

            CreateCubeObjectsTitleLabel();
            CreateRespawnCubeToggle();
            CreateRespawnCubeOnLastSwitchToggle();
        }
        void CreateCubeObjectsTitleLabel()
        {
            UILabel label = NGUI_Utils.CreateLabel(cubeObjectsSettings.transform, new Vector3(0, 40), new Vector3Int(700, 40, 0), "CUBE OPTIONS", NGUIText.Alignment.Center, UIWidget.Pivot.Center, 35, false);
            label.name = "TitleLabel";
        }
        void CreateRespawnCubeToggle()
        {
            respawnCubeToggle = NGUI_Utils.CreateToggle(cubeObjectsSettings.transform, new Vector3(-340f, -30f, 0f),
                new Vector3Int(250, 48, 1), "Respawn Cube");
            respawnCubeToggle.gameObject.name = "RespawnCubeToggle";
            respawnCubeToggle.onClick += (state) => OnRespawnCubeChanged();
        }
        void CreateRespawnCubeOnLastSwitchToggle()
        {
            respawnOnLastSwitchToggle = NGUI_Utils.CreateToggle(cubeObjectsSettings.transform, new Vector3(0f, -30f, 0f),
                new Vector3Int(250, 48, 1), "On Last Activated Plate");
            respawnOnLastSwitchToggle.gameObject.name = "OnLastActivatedSwitchToggle";
            respawnOnLastSwitchToggle.onClick += (state) => OnRespawnCubeOnLastActivatedSwitchChanged();
        }
        #endregion

        #region Laser Options
        void CreateLaserObjectSettings()
        {
            laserObjectsSettings = new GameObject("Laser");
            laserObjectsSettings.transform.parent = eventOptionsParent.transform;
            laserObjectsSettings.transform.localPosition = Vector3.zero;
            laserObjectsSettings.transform.localScale = Vector3.one;
            laserObjectsSettings.SetActive(false);

            CreateLaserObjectsTitleLabel();
            CreateLaserStateDropdown();
        }
        void CreateLaserObjectsTitleLabel()
        {
            UILabel label = NGUI_Utils.CreateLabel(laserObjectsSettings.transform, new Vector3(0, 40), new Vector3Int(700, 40, 0), "LASER OPTIONS", NGUIText.Alignment.Center, UIWidget.Pivot.Center, 35, false);
            label.name = "TitleLabel";
        }
        void CreateLaserStateDropdown()
        {
            laserStateButton = NGUI_Utils.CreateDropdown(laserObjectsSettings.transform, new Vector3(0, -50), Vector3.one * 0.8f);
            laserStateButton.SetTitle("Laser State");
            laserStateButton.AddOption("Do Nothing", true);
            laserStateButton.AddOption("Activate", false);
            laserStateButton.AddOption("Deactivate", false);
            laserStateButton.AddOption("Toggle State", false);
            laserStateButton.AddOnChangeOption(new EventDelegate(this, nameof(OnLaserStateDropdownChanged)));

            laserStateButton.gameObject.SetActive(true);
        }
        #endregion

        #region Mine Options
        void CreateMineObjectSettings()
        {
            mineObjectsSettings = new GameObject("Mine");
            mineObjectsSettings.transform.parent = eventOptionsParent.transform;
            mineObjectsSettings.transform.localPosition = Vector3.zero;
            mineObjectsSettings.transform.localScale = Vector3.one;
            mineObjectsSettings.SetActive(false);

            CreateMineObjectsTitleLabel();
            CreateMineStateDropdown();
        }
        void CreateMineObjectsTitleLabel()
        {
            UILabel label = NGUI_Utils.CreateLabel(mineObjectsSettings.transform, new Vector3(0, 40), new Vector3Int(700, 40, 0), "MINE OPTIONS", NGUIText.Alignment.Center, UIWidget.Pivot.Center, 35, false);
            label.name = "TitleLabel";
        }
        void CreateMineStateDropdown()
        {
            mineStateButton = NGUI_Utils.CreateDropdown(mineObjectsSettings.transform, new Vector3(0, -50), Vector3.one * 0.8f);
            mineStateButton.SetTitle("Mine State");
            mineStateButton.AddOption("Do Nothing", false);
            mineStateButton.AddOption("Activate", false);
            mineStateButton.AddOption("Deactivate", false);
            mineStateButton.AddOption("Toggle State", true);
            mineStateButton.AddOnChangeOption(new EventDelegate(this, nameof(OnMineStateDropdownChanged)));

            mineStateButton.gameObject.SetActive(true);
        }
        #endregion

        #region Light Options
        void CreateLightObjectSettings()
        {
            lightObjectsSettings = new GameObject("Light");
            lightObjectsSettings.transform.parent = eventOptionsParent.transform;
            lightObjectsSettings.transform.localPosition = Vector3.zero;
            lightObjectsSettings.transform.localScale = Vector3.one;
            lightObjectsSettings.SetActive(false);

            CreateLightObjectsTitleLabel();
            CreateChangeLightColorToggle();
            CreateNewLightColorTitleLabel();
            CreateNewLightColorInputField();
        }
        void CreateLightObjectsTitleLabel()
        {
            UILabel label = NGUI_Utils.CreateLabel(lightObjectsSettings.transform, new Vector3(0, 40), new Vector3Int(700, 40, 0), "LIGHT OPTIONS", NGUIText.Alignment.Center, UIWidget.Pivot.Center, 35, false);
            label.name = "TitleLabel";
        }
        void CreateChangeLightColorToggle()
        {
            changeLightColorToggle = NGUI_Utils.CreateToggle(lightObjectsSettings.transform, new Vector3(-380f, -30f, 0f),
                new Vector3Int(250, 48, 1), "Change Color");
            changeLightColorToggle.gameObject.name = "ChangeLightColorToggle";
            changeLightColorToggle.onClick += (state) => OnChangeLightColorToggleChanged();
        }
        void CreateNewLightColorTitleLabel()
        {
            UILabel label = NGUI_Utils.CreateLabel(lightObjectsSettings.transform, new Vector3(50, -30), new Vector3Int(150, 40, 0), "New Color", NGUIText.Alignment.Center, UIWidget.Pivot.Center, 27, false);
            label.name = "NewLightColorTitleLabel";

            newLightColorTitleLabel = label;
        }
        void CreateNewLightColorInputField()
        {
            UICustomInputField inputField = NGUI_Utils.CreateInputField(lightObjectsSettings.transform, new Vector3(270f, -30f, 0f),
                new Vector3Int(250, 40, 1), 27, "FFFFFF", inputType: UICustomInputField.UIInputType.HEX_COLOR);
            inputField.name = "NewLightColorInputField";
            inputField.onChange += OnNewLightColorInputFieldChanged;

            newLightColorInputField = inputField.GetComponent<UIInput>();
        }
        #endregion

        #region Ceiling Light Options
        void CreateCeilingLightObjectSettings()
        {
            ceilingLightObjectsSettings = new GameObject("CeilingLight");
            ceilingLightObjectsSettings.transform.parent = eventOptionsParent.transform;
            ceilingLightObjectsSettings.transform.localPosition = Vector3.zero;
            ceilingLightObjectsSettings.transform.localScale = Vector3.one;
            ceilingLightObjectsSettings.SetActive(false);

            CreateCeilingLightObjectsTitleLabel();
            CreateCeilingLightStateDropdown();
            CreateChangeCeilingLightColorToggle();
            CreateNewCeilingLightColorInputField();
        }
        void CreateCeilingLightObjectsTitleLabel()
        {
            UILabel label = NGUI_Utils.CreateLabel(ceilingLightObjectsSettings.transform, new Vector3(0, 40), new Vector3Int(700, 40, 0), "CEILING LIGHT OPTIONS", NGUIText.Alignment.Center, UIWidget.Pivot.Center, 35, false);
            label.name = "TitleLabel";
        }
        void CreateCeilingLightStateDropdown()
        {
            ceilingLightStateButton = NGUI_Utils.CreateDropdown(ceilingLightObjectsSettings.transform, new Vector3(-200, -50), Vector3.one * 0.8f);
            ceilingLightStateButton.SetTitle("Turn");
            ceilingLightStateButton.AddOption("Do Nothing", true);
            ceilingLightStateButton.AddOption("On", false);
            ceilingLightStateButton.AddOption("Off", false);
            ceilingLightStateButton.AddOption("Toggle On/Off", false);
            ceilingLightStateButton.AddOnChangeOption(new EventDelegate(this, nameof(OnCeilingLightStateDropdownChanged)));

            ceilingLightStateButton.gameObject.SetActive(true);
        }
        void CreateChangeCeilingLightColorToggle()
        {
            changeCeilingLightColorToggle = NGUI_Utils.CreateToggle(ceilingLightObjectsSettings.transform, new Vector3(20f, -17f, 0f),
                new Vector3Int(250, 48, 1), "Change Color");
            changeCeilingLightColorToggle.gameObject.name = "ChangeCeilingLightColorToggle";
            changeCeilingLightColorToggle.onClick += (state) => OnChangeCeilingLightColorToggleChanged();
        }
        void CreateNewCeilingLightColorInputField()
        {
            UICustomInputField inputField = NGUI_Utils.CreateInputField(ceilingLightObjectsSettings.transform, new Vector3(160f, -70f, 0f),
                new Vector3Int(250, 40, 1), 27, "FFFFFF", inputType: UICustomInputField.UIInputType.HEX_COLOR);
            inputField.name = "NewCeilingLightColorInputField";
            inputField.onChange += OnNewCeilingLightColorInputFieldChanged;

            newCeilingLightColorInputField = inputField.GetComponent<UIInput>();
        }
        #endregion

        #region Health & Ammo Options
        void CreateHealthAndAmmoPacksObjectSettings()
        {
            healthAmmoPacksObjectsSettings = new GameObject("HealthAndAmmoPcks");
            healthAmmoPacksObjectsSettings.transform.parent = eventOptionsParent.transform;
            healthAmmoPacksObjectsSettings.transform.localPosition = Vector3.zero;
            healthAmmoPacksObjectsSettings.transform.localScale = Vector3.one;
            healthAmmoPacksObjectsSettings.SetActive(false);

            CreateHealthAndAmmoPacksObjectsTitleLabel();
            CreateChangePackRespawnTimeToggle();
            CreateNewPackRespawnTimeTitleLabel();
            CreateNewPackRespawnTimeInputField();
            CreateSpawnPackNowToggle();
        }
        void CreateHealthAndAmmoPacksObjectsTitleLabel()
        {
            UILabel label = NGUI_Utils.CreateLabel(healthAmmoPacksObjectsSettings.transform, new Vector3(0, 40), new Vector3Int(700, 40, 0), "HEALTH & AMMO PACK OPTIONS", NGUIText.Alignment.Center, UIWidget.Pivot.Center, 35, false);
            label.name = "TitleLabel";
        }
        void CreateChangePackRespawnTimeToggle()
        {
            changePackRespawnTimeToggle = NGUI_Utils.CreateToggle(healthAmmoPacksObjectsSettings.transform, new Vector3(-380f, -30f, 0f),
                new Vector3Int(250, 48, 1), "Change Respawn Time");
            changePackRespawnTimeToggle.name = "ChangeRespawnTimeToggle";
            changePackRespawnTimeToggle.onClick += (state) => OnChangePackRespawnTimeToggleChanged();
        }
        void CreateNewPackRespawnTimeTitleLabel()
        {
            UILabel label = NGUI_Utils.CreateLabel(healthAmmoPacksObjectsSettings.transform, new Vector3(50, -30), new Vector3Int(150, 40, 0), "Time", NGUIText.Alignment.Center, UIWidget.Pivot.Center, 27, false);
            label.name = "NewRespawnTimeTitleLabel";

            newPackRespawnTimeTitleLabel = label;
        }
        void CreateNewPackRespawnTimeInputField()
        {
            UICustomInputField inputField = NGUI_Utils.CreateInputField(healthAmmoPacksObjectsSettings.transform, new Vector3(270f, -30f, 0f),
                new Vector3Int(250, 40, 1), 27, "60", inputType: UICustomInputField.UIInputType.NON_NEGATIVE_FLOAT);
            inputField.name = "NewInputField";
            inputField.onChange += OnNewPackRespawnTimeInputFieldChanged;

            newPackRespawnTimeInputField = inputField.GetComponent<UICustomInputField>();
        }
        void CreateSpawnPackNowToggle()
        {
            spawnPackNowToggle = NGUI_Utils.CreateToggle(healthAmmoPacksObjectsSettings.transform, new Vector3(-140f, -100f, 0f),
                new Vector3Int(250, 48, 1), "Spawn Pack Now");
            spawnPackNowToggle.gameObject.name = "SpawnPackNowToggle";
            spawnPackNowToggle.onClick += (state) => OnSpawnPackNowToggleChanged();
        }
        #endregion

        #region Switch Options
        void CreateSwitchObjectSettings()
        {
            switchObjectsSettings = new GameObject("Switch");
            switchObjectsSettings.transform.parent = eventOptionsParent.transform;
            switchObjectsSettings.transform.localPosition = Vector3.zero;
            switchObjectsSettings.transform.localScale = Vector3.one;
            switchObjectsSettings.SetActive(false);

            CreateSwitchObjectsTitleLabel();
            CreateSwitchStateSettings();
            CreateExecuteSwitchActionsToggle();
            CreateSwitchUsableStateSettings();
            CreateSwitchCanBeUsedStateSettings();
        }
        void CreateSwitchObjectsTitleLabel()
        {
            UILabel label = NGUI_Utils.CreateLabel(switchObjectsSettings.transform, new Vector3(0, 40), new Vector3Int(700, 40, 0), "SWITCH OPTIONS", NGUIText.Alignment.Center, UIWidget.Pivot.Center, 35, false);
            label.name = "TitleLabel";
        }
        void CreateSwitchStateSettings()
        {
            switchStateButton = NGUI_Utils.CreateDropdown(switchObjectsSettings.transform, new Vector3(-200, -50), Vector3.one * 0.8f);
            switchStateButton.SetTitle("Set Active State");
            switchStateButton.AddOption("Do Nothing", true);
            switchStateButton.AddOption("Activated", false);
            switchStateButton.AddOption("Deactivated", false);
            switchStateButton.AddOption("Toggle", false);
            switchStateButton.AddOnChangeOption(new EventDelegate(this, nameof(OnSwitchStateDropdownChanged)));

            switchStateButton.gameObject.SetActive(true);
        }
        void CreateExecuteSwitchActionsToggle()
        {
            executeSwitchActionsToggle = NGUI_Utils.CreateToggle(switchObjectsSettings.transform, new Vector3(-350f, -120f, 0f),
                new Vector3Int(250, 48, 1), "Execute Actions");
            executeSwitchActionsToggle.gameObject.name = "ExecuteActionsToggle";
            executeSwitchActionsToggle.onClick += (state) => OnExecuteSwitchActionsToggleChanged();
        }
        void CreateSwitchUsableStateSettings()
        {
            switchUsableStateButton = NGUI_Utils.CreateDropdown(switchObjectsSettings.transform, new Vector3(200, -50), Vector3.one * 0.8f);
            switchUsableStateButton.SetTitle("Set Usable State");
            switchUsableStateButton.AddOption("Do Nothing", true);
            switchUsableStateButton.AddOption("Usable", false);
            switchUsableStateButton.AddOption("Unusable", false);
            switchUsableStateButton.AddOption("Toggle", false);
            switchUsableStateButton.AddOnChangeOption(new EventDelegate(this, nameof(OnSwitchUsableStateDropdownChanged)));

            switchUsableStateButton.gameObject.SetActive(true);
        }
        void CreateSwitchCanBeUsedStateSettings()
        {
            switchCanBeUsedStateButton = NGUI_Utils.CreateDropdown(switchObjectsSettings.transform, new Vector3(0, -220), Vector3.one * 0.8f);
            switchCanBeUsedStateButton.SetTitle("Set Interaction State");
            switchCanBeUsedStateButton.AddOption("Do Nothing", true);
            switchCanBeUsedStateButton.AddOption("Enable", false);
            switchCanBeUsedStateButton.AddOption("Disable", false);
            switchCanBeUsedStateButton.AddOption("Toggle", false);
            switchCanBeUsedStateButton.AddOnChangeOption(new EventDelegate(this, nameof(OnSwitchCanBeUsedStateDropdownChanged)));

            switchCanBeUsedStateButton.gameObject.SetActive(true);
        }
        #endregion

        #region Keypad Options
        void CreateKeypadObjectSettings()
        {
            keypadObjectsSettings = new GameObject("Keypad");
            keypadObjectsSettings.transform.parent = eventOptionsParent.transform;
            keypadObjectsSettings.transform.localPosition = Vector3.zero;
            keypadObjectsSettings.transform.localScale = Vector3.one;
            keypadObjectsSettings.SetActive(false);

            CreateKeypadObjectsTitleLabel();
            CreateKeypadCanBeUsedStateSettings();
        }
        void CreateKeypadObjectsTitleLabel()
        {
            UILabel titleLabel = NGUI_Utils.CreateLabel(keypadObjectsSettings.transform, Vector3.up * 40,
                new Vector3Int(700, 40, 0), "KEYPAD OPTIONS", NGUIText.Alignment.Center, UIWidget.Pivot.Center);
            titleLabel.name = "TitleLabel";
            titleLabel.color = NGUI_Utils.fsLabelDefaultColor;
            titleLabel.fontSize = 35;
        }
        void CreateKeypadCanBeUsedStateSettings()
        {
            keypadCanBeUsedStateButton = NGUI_Utils.CreateDropdown(keypadObjectsSettings.transform, new Vector3(0, -50), Vector3.one * 0.8f);
            keypadCanBeUsedStateButton.SetTitle("Set Can Be Used");
            keypadCanBeUsedStateButton.AddOption("Do Nothing", true);
            keypadCanBeUsedStateButton.AddOption("Enable", false);
            keypadCanBeUsedStateButton.AddOption("Disable", false);
            keypadCanBeUsedStateButton.AddOption("Toggle", false);
            keypadCanBeUsedStateButton.AddOnChangeOption(new EventDelegate(this, nameof(OnKeypadCanBeUsedStateDropdownChanged)));

            keypadCanBeUsedStateButton.gameObject.SetActive(true);
        }
        #endregion

        #region Pressure Plate Options
        void CreatePressurePlateObjectSettings()
        {
            pressurePlateObjectsSettings = new GameObject("Pressure Plate");
            pressurePlateObjectsSettings.transform.parent = eventOptionsParent.transform;
            pressurePlateObjectsSettings.transform.localPosition = Vector3.zero;
            pressurePlateObjectsSettings.transform.localScale = Vector3.one;
            pressurePlateObjectsSettings.SetActive(false);

            CreatePressurePlateObjectsTitleLabel();
            CreatePressurePlateUsableStateSettings();
        }
        void CreatePressurePlateObjectsTitleLabel()
        {
            UILabel titleLabel = NGUI_Utils.CreateLabel(pressurePlateObjectsSettings.transform, Vector3.up * 40,
                new Vector3Int(700, 40, 0), "PRESSURE PLATE OPTIONS", NGUIText.Alignment.Center, UIWidget.Pivot.Center);
            titleLabel.name = "TitleLabel";
            titleLabel.color = NGUI_Utils.fsLabelDefaultColor;
            titleLabel.fontSize = 35;
        }
        void CreatePressurePlateUsableStateSettings()
        {
            pressurePlateUsableStateButton = NGUI_Utils.CreateDropdown(pressurePlateObjectsSettings.transform, new Vector3(0, -50), Vector3.one * 0.8f);
            pressurePlateUsableStateButton.SetTitle("Set Usable State");
            pressurePlateUsableStateButton.AddOption("Do Nothing", true);
            pressurePlateUsableStateButton.AddOption("Usable", false);
            pressurePlateUsableStateButton.AddOption("Unusable", false);
            pressurePlateUsableStateButton.AddOption("Toggle", false);
            pressurePlateUsableStateButton.AddOnChangeOption(new EventDelegate(this, nameof(OnPressurePlateUsableStateDropdownChanged)));

            pressurePlateUsableStateButton.gameObject.SetActive(true);
        }
        #endregion

        #region Flame Trap Options
        void CreateFlameTrapObjectSettings()
        {
            flameTrapObjectsSettings = new GameObject("Flame Trap");
            flameTrapObjectsSettings.transform.parent = eventOptionsParent.transform;
            flameTrapObjectsSettings.transform.localPosition = Vector3.zero;
            flameTrapObjectsSettings.transform.localScale = Vector3.one;
            flameTrapObjectsSettings.SetActive(false);

            CreateFlameTrapObjectsTitleLabel();
            CreateFlameTrapStateDropdown();
        }
        void CreateFlameTrapObjectsTitleLabel()
        {
            UILabel label = NGUI_Utils.CreateLabel(flameTrapObjectsSettings.transform, new Vector3(0, 40), new Vector3Int(700, 40, 0), "FLAME TRAP OPTIONS", NGUIText.Alignment.Center, UIWidget.Pivot.Center, 35, false);
            label.name = "TitleLabel";
        }
        void CreateFlameTrapStateDropdown()
        {
            flameTrapStateButton = NGUI_Utils.CreateDropdown(flameTrapObjectsSettings.transform, new Vector3(0, -50), Vector3.one * 0.8f);
            flameTrapStateButton.SetTitle("Flame State");
            flameTrapStateButton.AddOption("Do Nothing", true);
            flameTrapStateButton.AddOption("Activate", false);
            flameTrapStateButton.AddOption("Deactivate", false);
            flameTrapStateButton.AddOption("Toggle State", false);
            flameTrapStateButton.AddOnChangeOption(new EventDelegate(this, nameof(OnFlameTrapStateDropdownChanged)));

            flameTrapStateButton.gameObject.SetActive(true);
        }
        #endregion

        #region Screen Options
        void CreateScreenObjectSettings()
        {
            screenObjectsSettings = new GameObject("Screen");
            screenObjectsSettings.transform.parent = eventOptionsParent.transform;
            screenObjectsSettings.transform.localPosition = Vector3.zero;
            screenObjectsSettings.transform.localScale = Vector3.one;
            screenObjectsSettings.SetActive(false);

            CreateScreenObjectsTitleLabel();
            CreateChangeScreenColorTypeToggle();
            CreateScreenColorTypeButton();
            CreateChangeScreenTextToggle();
            CreateScreenNewTextField();
        }
        void CreateScreenObjectsTitleLabel()
        {
            UILabel titleLabel = NGUI_Utils.CreateLabel(screenObjectsSettings.transform, Vector3.up * 40, new Vector3Int(700, 40, 0), "SCREEN OPTIONS",
                NGUIText.Alignment.Center, UIWidget.Pivot.Center);
            titleLabel.name = "TitleLabel";
            titleLabel.color = NGUI_Utils.fsLabelDefaultColor;
            titleLabel.fontSize = 35;
        }
        void CreateChangeScreenColorTypeToggle()
        {
            changeScreenColorTypeToggle = NGUI_Utils.CreateToggle(screenObjectsSettings.transform, new Vector3(-380, -10), new Vector3Int(300, 48, 0), "Change Color Type");
            changeScreenColorTypeToggle.gameObject.name = "ChangeColorTypeToggle";
            changeScreenColorTypeToggle.onClick += (state) => OnChangeScreenColorTypeToggleChanged();
        }
        void CreateScreenColorTypeButton()
        {
            screenColorTypeButton = NGUI_Utils.CreateSmallButtonMultiple(screenObjectsSettings.transform, new Vector3(200, -10), new Vector3Int(300, 48, 0), "CYAN");
            screenColorTypeButton.name = "ChangeColorTypeButton";
            screenColorTypeButton.AddOption("CYAN", null); // Use the default button color, which is cyan LOL.
            screenColorTypeButton.AddOption("GREEN", Color.green);
            screenColorTypeButton.AddOption("RED", new Color(0.8f, 0f, 0f));
            screenColorTypeButton.onChange += (option) => OnScreenColorTypeButtonChanged();
        }
        void CreateChangeScreenTextToggle()
        {
            changeScreenTextToggle = NGUI_Utils.CreateToggle(screenObjectsSettings.transform, new Vector3(-180, -65), new Vector3Int(300, 48, 0), "Change Text");
            changeScreenTextToggle.gameObject.name = "ChangeTextToggle";
            changeScreenTextToggle.onClick += (state) => OnChangeScreenTextToggleChanged();
        }
        void CreateScreenNewTextField()
        {
            screenNewTextField = NGUI_Utils.CreateInputField(screenObjectsSettings.transform, Vector3.down * 200, new Vector3Int(750, 200, 0), 27, inputType:
                UICustomInputField.UIInputType.PLAIN_TEXT);
            screenNewTextField.name = "ScreenNewTextField";
            screenNewTextField.input.mPivot = UIWidget.Pivot.TopLeft;
            screenNewTextField.input.onReturnKey = UIInput.OnReturnKey.NewLine;

            screenNewTextField.onChange += OnNewScreenTextFieldChanged;
        }
        #endregion

        #region Door Options
        void CreateDoorObjectSettings()
        {
            doorObjectsSettings = new GameObject("Door");
            doorObjectsSettings.transform.parent = eventOptionsParent.transform;
            doorObjectsSettings.transform.localPosition = Vector3.zero;
            doorObjectsSettings.transform.localScale = Vector3.one;
            doorObjectsSettings.SetActive(false);

            CreateDoorObjectsTitleLabel();
            CreateDoorStateButton();
        }
        void CreateDoorObjectsTitleLabel()
        {
            UILabel titleLabel = NGUI_Utils.CreateLabel(doorObjectsSettings.transform, Vector3.up * 40, new Vector3Int(700, 40, 0), "DOOR OPTIONS",
                NGUIText.Alignment.Center, UIWidget.Pivot.Center);
            titleLabel.name = "TitleLabel";
            titleLabel.color = NGUI_Utils.fsLabelDefaultColor;
            titleLabel.fontSize = 35;
        }
        void CreateDoorStateButton()
        {
            setDoorStateButton = NGUI_Utils.CreateDropdown(doorObjectsSettings.transform, new Vector3(0, -50), Vector3.one * 0.8f);
            setDoorStateButton.SetTitle("Set Door State");
            setDoorStateButton.AddOption("Do Nothing", true);
            setDoorStateButton.AddOption("Close", false);
            setDoorStateButton.AddOption("Close Fast", false);
            setDoorStateButton.AddOption("Open", false);
            setDoorStateButton.AddOption("Toggle", false);
            setDoorStateButton.AddOnChangeOption(new EventDelegate(this, nameof(OnDoorStateButtonChanged)));

            setDoorStateButton.gameObject.SetActive(true);
        }
        #endregion

        #region Bridge Options
        void CreateBridgeObjectSettings()
        {
            bridgeObjectsSettings = new GameObject("Bridge");
            bridgeObjectsSettings.transform.parent = eventOptionsParent.transform;
            bridgeObjectsSettings.transform.localPosition = Vector3.zero;
            bridgeObjectsSettings.transform.localScale = Vector3.one;
            bridgeObjectsSettings.SetActive(false);

            CreateBridgeObjectsTitleLabel();
            CreateBridgeStateButton();
        }
        void CreateBridgeObjectsTitleLabel()
        {
            UILabel titleLabel = NGUI_Utils.CreateLabel(bridgeObjectsSettings.transform, Vector3.up * 40, new Vector3Int(700, 40, 0), "BRIDGE OPTIONS",
                NGUIText.Alignment.Center, UIWidget.Pivot.Center);
            titleLabel.name = "TitleLabel";
            titleLabel.color = NGUI_Utils.fsLabelDefaultColor;
            titleLabel.fontSize = 35;
        }
        void CreateBridgeStateButton()
        {
            bridgeStateButton = NGUI_Utils.CreateDropdown(bridgeObjectsSettings.transform, new Vector3(0, -50), Vector3.one * 0.8f);
            bridgeStateButton.SetTitle("Set Bridge State");
            bridgeStateButton.AddOption("Do Nothing", true);
            bridgeStateButton.AddOption("Extend", false);
            bridgeStateButton.AddOption("Retract", false);
            bridgeStateButton.AddOption("Toggle", false);
            bridgeStateButton.AddOnChangeOption(new EventDelegate(this, nameof(OnBridgeStateButtonChanged)));

            bridgeStateButton.gameObject.SetActive(true);
        }
        #endregion

        #region Moving Platform Options
        void CreateMovingPlatformObjectSettings()
        {
            movingPlatformObjectsSettings = new GameObject("MovingPlatform");
            movingPlatformObjectsSettings.transform.parent = eventOptionsParent.transform;
            movingPlatformObjectsSettings.transform.localPosition = Vector3.zero;
            movingPlatformObjectsSettings.transform.localScale = Vector3.one;
            movingPlatformObjectsSettings.SetActive(false);

            CreateMovingPlatformObjectsTitleLabel();
            CreateMovingPlatformStateButton();
        }
        void CreateMovingPlatformObjectsTitleLabel()
        {
            UILabel titleLabel = NGUI_Utils.CreateLabel(movingPlatformObjectsSettings.transform, Vector3.up * 40,
                new Vector3Int(700, 40, 0), "MOVING PLATFORM OPTIONS", NGUIText.Alignment.Center, UIWidget.Pivot.Center);
            titleLabel.name = "TitleLabel";
            titleLabel.color = NGUI_Utils.fsLabelDefaultColor;
            titleLabel.fontSize = 35;
        }
        void CreateMovingPlatformStateButton()
        {
            movingPlatformStateButton = NGUI_Utils.CreateDropdown(movingPlatformObjectsSettings.transform,
                new Vector3(0, -50), Vector3.one * 0.8f);
            movingPlatformStateButton.SetTitle("Set Platform State");
            movingPlatformStateButton.AddOption("Do Nothing", true);
            movingPlatformStateButton.AddOption("Activate", false);
            movingPlatformStateButton.AddOption("Deactivate", false);
            movingPlatformStateButton.AddOption("Toggle", false);
            movingPlatformStateButton.AddOnChangeOption(new EventDelegate(this, nameof(OnMovingPlatformStateButtonChanged)));

            movingPlatformStateButton.gameObject.SetActive(true);
        }
        #endregion

        #region Destructible Wall Options
        void CreateDestructibleWallObjectSettings()
        {
            destructibleWallObjectsSettings = new GameObject("Destructible Wall");
            destructibleWallObjectsSettings.transform.parent = eventOptionsParent.transform;
            destructibleWallObjectsSettings.transform.localPosition = Vector3.zero;
            destructibleWallObjectsSettings.transform.localScale = Vector3.one;
            destructibleWallObjectsSettings.SetActive(false);

            CreateDestructibleWallObjectTitleLabel();
            CreateDestructibleWallBreakNowToggle();
        }
        void CreateDestructibleWallObjectTitleLabel()
        {
            UILabel titleLabel = NGUI_Utils.CreateLabel(destructibleWallObjectsSettings.transform, Vector3.up * 40, new Vector3Int(700, 40, 0), "DESTRUCTIBLE WALL OPTIONS",
                NGUIText.Alignment.Center, UIWidget.Pivot.Center);
            titleLabel.name = "TitleLabel";
            titleLabel.color = NGUI_Utils.fsLabelDefaultColor;
            titleLabel.fontSize = 35;
        }
        void CreateDestructibleWallBreakNowToggle()
        {
            destructibleWallBreakNowToggle = NGUI_Utils.CreateToggle(destructibleWallObjectsSettings.transform, new Vector3(-150f, -25f, 0f),
                new Vector3Int(250, 48, 1), "Break Now");
            destructibleWallBreakNowToggle.gameObject.name = "BreakNowToggle";
            destructibleWallBreakNowToggle.onClick += (state) => OnDestructibleWallBreakNowChanged();
        }
        #endregion

        #region Fragie Window Options
        void CreateFragileWindowObjectSettings()
        {
            fragileWindowObjectsSettings = new GameObject("Fragile Window");
            fragileWindowObjectsSettings.transform.parent = eventOptionsParent.transform;
            fragileWindowObjectsSettings.transform.localPosition = Vector3.zero;
            fragileWindowObjectsSettings.transform.localScale = Vector3.one;
            fragileWindowObjectsSettings.SetActive(false);

            CreateFragileWindowObjectTitleLabel();
            CreateFragileWindowBreakNowToggle();
        }
        void CreateFragileWindowObjectTitleLabel()
        {
            UILabel titleLabel = NGUI_Utils.CreateLabel(fragileWindowObjectsSettings.transform, Vector3.up * 40, new Vector3Int(700, 40, 0), "BREAKABLE WINDOW OPTIONS",
                NGUIText.Alignment.Center, UIWidget.Pivot.Center);
            titleLabel.name = "TitleLabel";
            titleLabel.color = NGUI_Utils.fsLabelDefaultColor;
            titleLabel.fontSize = 35;
        }
        void CreateFragileWindowBreakNowToggle()
        {
            fragileWindowBreakNowToggle = NGUI_Utils.CreateToggle(fragileWindowObjectsSettings.transform, new Vector3(-150f, -25f, 0f),
                new Vector3Int(250, 48, 1), "Break Now");
            fragileWindowBreakNowToggle.gameObject.name = "BreakNowToggle";
            fragileWindowBreakNowToggle.onClick += (state) => OnFragileWindowBreakNowChanged();
        }
        #endregion

        #region Upgrade Terminal Options
        void CreateUpgradeTerminalObjectSettings()
        {
            terminalObjectsSettings = new GameObject("UpgradeTerminal");
            terminalObjectsSettings.transform.parent = eventOptionsParent.transform;
            terminalObjectsSettings.transform.localPosition = Vector3.zero;
            terminalObjectsSettings.transform.localScale = Vector3.one;
            terminalObjectsSettings.SetActive(false);

            CreateUpgradeTerminalsObjectsTitleLabel();
            CreateTerminalActiveStateButton();
        }
        void CreateUpgradeTerminalsObjectsTitleLabel()
        {
            UILabel titleLabel = NGUI_Utils.CreateLabel(terminalObjectsSettings.transform, Vector3.up * 40,
                new Vector3Int(700, 40, 0), "UPGRADE TERMINAL OPTIONS", NGUIText.Alignment.Center, UIWidget.Pivot.Center);
            titleLabel.name = "TitleLabel";
            titleLabel.color = NGUI_Utils.fsLabelDefaultColor;
            titleLabel.fontSize = 35;
        }
        void CreateTerminalActiveStateButton()
        {
            terminalActiveStateButton = NGUI_Utils.CreateDropdown(terminalObjectsSettings.transform,
                new Vector3(0, -50), Vector3.one * 0.8f);
            terminalActiveStateButton.SetTitle("Set Active State");
            terminalActiveStateButton.AddOption("Do Nothing", true);
            terminalActiveStateButton.AddOption("Active", false);
            terminalActiveStateButton.AddOption("Deactive", false);
            terminalActiveStateButton.AddOption("Toggle", false);
            terminalActiveStateButton.AddOnChangeOption(new EventDelegate(this, nameof(OnTerminalActiveStateButtonChanged)));

            terminalActiveStateButton.gameObject.SetActive(true);
        }
        #endregion

        #endregion

        #region Logic For Objects UI Options

        #region Default Options
        void OnSpawnOptionsDropdownChanged()
        {
            currentSelectedEvent.spawn = (LE_Event.SpawnState)spawnOptionsDropdown.currentlySelectedID;
        }
        void OnColliderStateDropdownChanged()
        {
            currentSelectedEvent.colliderState = (LE_Event.ColliderState)colliderStateDropdown.currentlySelectedID;
        }
        void OnUseAndLogicToggleChanged()
        {
            currentSelectedEvent.useAndLogic = useAndLogicToggle.isChecked;
        }
        void OnMoreGlobalOptionsButtonClicked(bool newState)
        {
            globalOptionsExpanded = !globalOptionsExpanded;

            if (globalOptionsExpanded)
            {
                globalObjectsSettings.SetActive(true);
                if (currentActiveObjectPanel) currentActiveObjectPanel.SetActive(false);
            }
            else
            {
                globalObjectsSettings.SetActive(false);
                if (currentActiveObjectPanel) currentActiveObjectPanel.SetActive(true);
            }
        }
        #endregion

        #region Global Options
        void OnMoveStateDropdownChanged()
        {
            currentSelectedEvent.moveState = (LE_Event.MoveState)movingStateToggle.currentlySelectedID;
        }
        void OnResetMovementToggleChanged()
        {
            currentSelectedEvent.resetMovement = resetMovementToggle.isChecked;
        }
        void OnDelayInputFieldChanged()
        {
            if (delayInputField.isValid)
            {
                currentSelectedEvent.delay = Utils.ParseFloat(delayInputField.GetText());
            }
        }
        #endregion

        #region Saw Options
        void OnSawStateDropdownChanged()
        {
            currentSelectedEvent.sawState = (LE_Event.SawState)sawStateButton.currentlySelectedID;
        }
        #endregion

        #region Player Options
        void OnZeroGToggleChanged()
        {
            currentSelectedEvent.enableOrDisableZeroG = zeroGToggle.isChecked;
            // Both toggles can't be enabled!
            if (zeroGToggle.isChecked && invertGravityToggle.isChecked)
            {
                invertGravityToggle.Set(false, true);
            }
        }
        void OnFlashlightToggleChanged()
        {
            currentSelectedEvent.flashlightEnabled = flashlightToggle.isChecked;
        }
        void OnInvertGravityToggleChanged()
        {
            currentSelectedEvent.invertGravity = invertGravityToggle.isChecked;
            // Both toggles can't be enabled!
            if (invertGravityToggle.isChecked && zeroGToggle.isChecked)
            {
                zeroGToggle.Set(false, true);
            }
        }
        void OnUpgradesButtonPressed()
        {
            if (currentSelectedEvent.upgrades == null)
                currentSelectedEvent.upgrades = new();

            UpgradesPanel.Instance.ShowUpgradesPanel(currentSelectedEvent.upgrades, currentSelectedEvent.eventName, targetObj);
        }
        #endregion

        #region Taser Options
        void OnTaserStateButtonChanged()
        {
            currentSelectedEvent.taserState = (LE_Event.TaserState)taserStateButton.currentlySelectedID;
        }
        void OnChangeAmmoToggleChanged()
        {
            currentSelectedEvent.changeAmmo = changeAmmoToggle.isChecked;
            newAmmoInputField.gameObject.SetActive(changeAmmoToggle.isChecked && !infiniteTaserToggle.isChecked);
            infiniteTaserToggle.gameObject.SetActive(changeAmmoToggle.isChecked);

            if (!changeAmmoToggle.isChecked)
            {
                infiniteTaserToggle.Set(false);
            }
        }
        void OnNewAmmoInputFieldChanged()
        {
            int value = int.Parse(newAmmoInputField.GetText());
            // Clamp the value between 1 and 10
            value = (value >= 10 || newAmmoInputField.GetText() == "") ? 10 : value;
            // If the value was clamped, update the input field to show the clamped value
            if (value.ToString() != newAmmoInputField.GetText())
            {
                newAmmoInputField.SetText(value.ToString());
            }
            currentSelectedEvent.newAmmo = value;
        }
        void OnInfiniteTaserToggleChanged()
        {
            currentSelectedEvent.infiniteTaser = infiniteTaserToggle.isChecked;
            newAmmoInputField.gameObject.SetActive(!infiniteTaserToggle.isChecked && changeAmmoToggle.isChecked);
        }
        #endregion

        #region Jetpack Options
        void OnJetpackStateButtonChanged()
        {
            currentSelectedEvent.jetpackState = (LE_Event.JetpackState)jetpackStateButton.currentlySelectedID;
        }
        #endregion

        #region Objective Options
        void OnObjectiveStateButtonChanged()
        {
            currentSelectedEvent.objectiveState = (LE_Event.ObjectiveState)objectiveStateButton.currentlySelectedID;
        }
        #endregion

        #region Cube Options
        void OnRespawnCubeChanged()
        {
            currentSelectedEvent.respawnCube = respawnCubeToggle.isChecked;
            respawnOnLastSwitchToggle.gameObject.SetActive(respawnCubeToggle.isChecked);
        }
        void OnRespawnCubeOnLastActivatedSwitchChanged()
        {
            currentSelectedEvent.respawnCubeOnLastSwitch = respawnOnLastSwitchToggle.isChecked;
        }
        #endregion

        #region Laser Options
        void OnLaserStateDropdownChanged()
        {
            currentSelectedEvent.laserState = (LE_Event.LaserState)laserStateButton.currentlySelectedID;
        }
        #endregion

        #region Mine Options
        void OnMineStateDropdownChanged()
        {
            currentSelectedEvent.mineState = (LE_Event.MineState)mineStateButton.currentlySelectedID;
        }
        #endregion

        #region Bridge Options
        void OnBridgeStateButtonChanged()
        {
            currentSelectedEvent.bridgeState = (LE_Event.BridgeState)bridgeStateButton.currentlySelectedID;
        }
        #endregion

        #region Light Options
        void OnChangeLightColorToggleChanged()
        {
            currentSelectedEvent.changeLightColor = changeLightColorToggle.isChecked;

            newLightColorTitleLabel.gameObject.SetActive(changeLightColorToggle.isChecked);
            newLightColorInputField.gameObject.SetActive(changeLightColorToggle.isChecked);
        }
        void OnNewLightColorInputFieldChanged()
        {
            // Set the input field color:
            Color? outputColor = Utils.HexToColor(newLightColorInputField.text, false, null);
            if (outputColor != null)
            {
                newLightColorInputField.GetComponent<UISprite>().color = new Color(0.0588f, 0.3176f, 0.3215f, 0.9412f);
            }
            else
            {
                newLightColorInputField.GetComponent<UISprite>().color = new Color(0.3215f, 0.2156f, 0.0588f, 0.9415f);
            }

            currentSelectedEvent.newLightColor = newLightColorInputField.text;
        }
        #endregion

        #region Ceiling Light Options
        void OnCeilingLightStateDropdownChanged()
        {
            currentSelectedEvent.ceilingLightState = (LE_Event.CeilingLightState)ceilingLightStateButton.currentlySelectedID;
        }
        void OnChangeCeilingLightColorToggleChanged()
        {
            currentSelectedEvent.changeCeilingLightColor = changeCeilingLightColorToggle.isChecked;

            newCeilingLightColorInputField.gameObject.SetActive(changeCeilingLightColorToggle.isChecked);
        }
        void OnNewCeilingLightColorInputFieldChanged()
        {
            // Set the input field color:
            Color? outputColor = Utils.HexToColor(newCeilingLightColorInputField.text, false, null);
            if (outputColor != null)
            {
                newCeilingLightColorInputField.GetComponent<UISprite>().color = new Color(0.0588f, 0.3176f, 0.3215f, 0.9412f);
            }
            else
            {
                newCeilingLightColorInputField.GetComponent<UISprite>().color = new Color(0.3215f, 0.2156f, 0.0588f, 0.9415f);
            }

            currentSelectedEvent.newCeilingLightColor = newCeilingLightColorInputField.text;
        }
        #endregion

        #region Health & Ammo Pack Options
        void OnChangePackRespawnTimeToggleChanged()
        {
            currentSelectedEvent.changePackRespawnTime = changePackRespawnTimeToggle.isChecked;

            newPackRespawnTimeTitleLabel.gameObject.SetActive(changePackRespawnTimeToggle.isChecked);
            newPackRespawnTimeInputField.gameObject.SetActive(changePackRespawnTimeToggle.isChecked);
        }
        void OnNewPackRespawnTimeInputFieldChanged()
        {
            if (newPackRespawnTimeInputField.isValid)
            {
                currentSelectedEvent.packRespawnTime = Utils.ParseFloat(newPackRespawnTimeInputField.GetText());
            }
        }
        void OnSpawnPackNowToggleChanged()
        {
            currentSelectedEvent.spawnPackNow = spawnPackNowToggle.isChecked;
        }
        #endregion

        #region Switch Options
        void OnSwitchStateDropdownChanged()
        {
            currentSelectedEvent.switchState = (LE_Event.SwitchState)switchStateButton.currentlySelectedID;

            executeSwitchActionsToggle.gameObject.SetActive(currentSelectedEvent.switchState != LE_Event.SwitchState.Do_Nothing);
        }
        void OnExecuteSwitchActionsToggleChanged()
        {
            currentSelectedEvent.executeSwitchActions = executeSwitchActionsToggle.isChecked;
        }
        void OnSwitchUsableStateDropdownChanged()
        {
            currentSelectedEvent.switchUsableState = (LE_Event.SwitchUsableState)switchUsableStateButton.currentlySelectedID;
        }
        void OnSwitchCanBeUsedStateDropdownChanged()
        {
            currentSelectedEvent.canBeUsedState = (LE_Event.CanBeUsedState)switchCanBeUsedStateButton.currentlySelectedID;
        }
        #endregion

        #region Keypad Options
        void OnKeypadCanBeUsedStateDropdownChanged()
        {
            currentSelectedEvent.canBeUsedState = (LE_Event.CanBeUsedState)keypadCanBeUsedStateButton.currentlySelectedID;
        }
        #endregion

        #region Pressure Plate Options
        void OnPressurePlateUsableStateDropdownChanged()
        {
            currentSelectedEvent.pressurePlateUsableState = (LE_Event.PressurePlateUsableState)pressurePlateUsableStateButton.currentlySelectedID;
        }
        #endregion

        #region Flame Trap Options
        void OnFlameTrapStateDropdownChanged()
        {
            currentSelectedEvent.flameTrapState = (LE_Event.FlameTrapState)flameTrapStateButton.currentlySelectedID;
        }
        #endregion

        #region Screen Options
        void OnChangeScreenColorTypeToggleChanged()
        {
            currentSelectedEvent.changeScreenColorType = changeScreenColorTypeToggle.isChecked;

            screenColorTypeButton.gameObject.SetActive(changeScreenColorTypeToggle.isChecked);
        }
        void OnScreenColorTypeButtonChanged()
        {
            currentSelectedEvent.screenColorType = (ScreenColorType)screenColorTypeButton.currentOption;
        }
        void OnChangeScreenTextToggleChanged()
        {
            currentSelectedEvent.changeScreenText = changeScreenTextToggle.isChecked;
            screenNewTextField.gameObject.SetActive(changeScreenTextToggle.isChecked);
        }
        void OnNewScreenTextFieldChanged()
        {
            currentSelectedEvent.screenNewText = screenNewTextField.GetText();
        }
        #endregion

        #region Moving Platform Options
        void OnMovingPlatformStateButtonChanged()
        {
            currentSelectedEvent.movingPlatformState = (LE_Event.MovingPlatformState)movingPlatformStateButton.currentlySelectedID;
        }
        #endregion

        #region Door Options
        void OnDoorStateButtonChanged()
        {
            currentSelectedEvent.doorState = (LE_Event.DoorState)setDoorStateButton.currentlySelectedID;
        }
        #endregion

        #region Destructive Wall Options
        void OnDestructibleWallBreakNowChanged()
        {
            currentSelectedEvent.destructibleWallBreakNow = destructibleWallBreakNowToggle.isChecked;
        }
        #endregion

        #region Fragie Window Options
        void OnFragileWindowBreakNowChanged()
        {
            currentSelectedEvent.fragileWindowBreakNow = fragileWindowBreakNowToggle.isChecked;
        }
        #endregion

        #region Upgrade Terminal Options
        void OnTerminalActiveStateButtonChanged()
        {
            currentSelectedEvent.terminalActiveState = (LE_Event.TerminalActiveState)terminalActiveStateButton.currentlySelectedID;
        }
        #endregion

        #endregion

        public void ShowEventsPage(LE_Object targetObj, bool refresh = true)
        {
            if (targetObj.GetAvailableEventsIDs().Length <= 0)
            {
                Logger.Error("Requested to show Events Panel but the target object has NO Events List. IT'S NOT COMPATIBLE!");
                return;
            }
            this.targetObj = targetObj;

            // Change the title of the panel.
            eventsWindowTitle.text = "Events for " + targetObj.objectFullNameWithID;

            EditorController.Instance.SetCurrentEditorState(EditorState.PAUSED); // Just to stop camera movement and such.
            EditorUIManager.Instance.SetEditorUIContext(EditorUIContext.EVENTS_PANEL);

            if (refresh)
            {
                SetupTopButtons();
                FirstEventsListBtnClick(false);
            }
            // CreateEventsList();
        }
        public void HideEventsPage()
        {
            targetObj.TriggerAction("OnEventsTabClose");

            EditorController.Instance.SetCurrentEditorState(EditorState.NORMAL);
            EditorUIManager.Instance.SetEditorUIContext(EditorUIContext.NORMAL);

            OnEventSelect(null);
        }

        public List<LE_Event> GetEventsList()
        {
            return (List<LE_Event>)targetObj.GetProperty(currentEventsListName);
        }
        List<LE_Event> GetEventsList(int listID)
        {
            string targetListName = eventsListsNames[listID];
            return (List<LE_Event>)targetObj.GetProperty(targetListName);
        }
    }

    [RegisterTypeInIl2Cpp]
    public class EventButton : MonoBehaviour
    {
        public EventsUIPageManager eventsManager;
        public int eventID;

        public UIButton uiButton;
        public UIButtonPatcher deleteBtn;
        public UICustomInputField nameInput;

        public void Setup(int eventID)
        {
            this.eventID = eventID;
            deleteBtn.onClick = null; // Clear old actions.
            deleteBtn.onClick += () => eventsManager.DeleteEvent(eventID);
            nameInput.SetText(eventsManager.GetEventsList()[eventID].eventName);
            nameInput.onSubmit = null; // Clear old actions.
            nameInput.onSubmit += () => eventsManager.RenameEvent(eventID, nameInput);
        }

        public void OnClick()
        {
            eventsManager.OnEventSelect(eventID);
        }
    }
}

public class LE_Event
{
    public LE_Event() { }
    public LE_Event(LE_Event toCopy)
    {
        var type = typeof(LE_Event);

        foreach (var property in type.GetProperties())
        {
            if (!property.CanWrite)
                continue;

            property.SetValue(this, property.GetValue(toCopy));
        }
    }

    // RUNTIME PROPERTY, DO NOT DECLARE IT AS A PROPERTY BECAUSE WE DON'T WANT IT TO BE SERIALIZED BY JSON.
    public LE_Object targetInstanceObject = null;

    // ------------------------------------------------------
    // Everything here below IS serialized with json.

    [Obsolete("This this is here just to keep the old 'event upgrader' system working for VERY old levels. DO NOT USE THIS!")]
    public bool isValid { get; set; } = false;
    public bool IsValid
    {
        get
        {
            // Special event (player, taser, jetpack, etc.)
            if (!IsNormalEventThatRequriesTargetObject())
            {
                if (!IsAtLeastOneTypeOfEvent())
                    return false; // The event has no type

                // Special case for groups, check that it still exists.
                if (isForGroup)
                {
                    if (!LE_Object.objectsPerGroup.ContainsKey(targetGroupID))
                        return false;
                }

                return true;
            }

            // It's a normal event that requires a target object.
            if (targetInstanceObject)
                return targetInstanceObject;

            return VerifyNormalEventValidity(targetObjType, targetObjID, true);
        }
    }

    // Yeah, why should I put a name to a freaking event? Dunno, may be useful :)
    public string eventName { get; set; } = "New Event";

    public bool isForPlayer { get; set; } = false;
    public bool isForTaser { get; set; } = false;

    public bool isForJetpack { get; set; } = false;

    public string targetObjName { get; set; } = "";
    public LE_Object.ObjectType? targetObjType { get; set; } = null;
    public int targetObjID { get; set; } = 0;

    /// <summary>
    /// When enabled, the target object will only activate when ALL input objects with AND logic enabled are active.
    /// If any of the AND inputs deactivates, the target will deactivate (undo action).
    /// </summary>
    public bool useAndLogic { get; set; } = false;

    /// <summary>
    /// Delay in seconds before executing the event.
    /// For non-AND events: executes after this delay.
    /// For AND events: executes after this delay once all AND conditions are met.
    /// </summary>
    public float delay { get; set; } = 0f;

    public enum SpawnState { Do_Nothing, Spawn, Despawn, Toggle }
    public SpawnState spawn { get; set; } = SpawnState.Toggle;
    public enum ColliderState { Do_Nothing, Enable, Disable, Toggle }
    public ColliderState colliderState { get; set; } = ColliderState.Do_Nothing;
    public enum MoveState { Do_Nothing, Start_Moving, Stop_Moving, Start_Or_Stop_Moving }
    public MoveState moveState { get; set; } = MoveState.Do_Nothing;
    public bool resetMovement { get; set; } = false;

    #region Saw Options
    public enum SawState { Do_Nothing, Activate, Deactivate, Toggle_State }
    public SawState sawState { get; set; } = SawState.Toggle_State;
    #endregion

    #region Player Options
    public bool enableOrDisableZeroG { get; set; } = false;
    public bool invertGravity { get; set; } = false;
    public bool flashlightEnabled { get; set; } = true;
    public List<UpgradeSaveData> upgrades { get; set; } = new();
    #endregion

    #region Taser Options
    public enum TaserState { Do_Nothing, Give, Take_Away }
    public TaserState taserState { get; set; } = TaserState.Do_Nothing;
    public bool changeAmmo { get; set; } = false;
    public int newAmmo { get; set; } = 8;
    public bool infiniteTaser { get; set; } = false;
    #endregion

    #region Jetpack Options
    public enum JetpackState { Do_Nothing, Give, Take_Away }
    public JetpackState jetpackState { get; set; } = JetpackState.Do_Nothing;
    #endregion

    #region Cube Options
    public bool respawnCube { get; set; } = false;
    public bool respawnCubeOnLastSwitch { get; set; } = true;
    #endregion

    #region Laser Options
    public enum LaserState { Do_Nothing, Activate, Deactivate, Toggle_State }
    public LaserState laserState = LaserState.Toggle_State;
    #endregion

    #region Mine Options
    public enum MineState { Do_Nothing, Activate, Deactivate, Toggle_State }
    public MineState mineState { get; set; } = MineState.Toggle_State;
    #endregion

    #region Light Options
    public bool changeLightColor { get; set; } = false;
    public string newLightColor { get; set; } = "FFFFFF";
    #endregion

    #region Ceiling Light Options
    public enum CeilingLightState { Do_Nothing, On, Off, ToggleOnOff }
    public CeilingLightState ceilingLightState { get; set; } = CeilingLightState.ToggleOnOff;
    public bool changeCeilingLightColor { get; set; } = false;
    public string newCeilingLightColor { get; set; } = "FFFFFF";
    #endregion

    #region Health and Ammo Pack Options
    public bool changePackRespawnTime { get; set; } = false;
    public float packRespawnTime { get; set; } = 60;
    public bool spawnPackNow { get; set; } = false;
    #endregion

    #region Objective Options
    public bool isForObjective { get; set; } = false;
    public enum ObjectiveState { Do_Nothing, Create, Accomplish, Fail }
    public ObjectiveState objectiveState { get; set; } = ObjectiveState.Create;
    public string objectiveName { get; set; } = "Obj_Name";
    public enum ObjectiveResult { None, Accomplish, Fail }
    public ObjectiveResult objectiveResult { get; set; } = ObjectiveResult.None;
    #endregion

    #region Switch Options
    public enum SwitchState { Do_Nothing, Activated, Deactivated, Toggle }
    public SwitchState switchState { get; set; } = SwitchState.Do_Nothing;
    public bool executeSwitchActions { get; set; } = true;
    public enum SwitchUsableState { Do_Nothing, Usable, Unusable, Toggle }
    public SwitchUsableState switchUsableState { get; set; } = SwitchUsableState.Do_Nothing;
    public enum CanBeUsedState { Do_Nothing, Enable, Disable, Toggle }
    public CanBeUsedState canBeUsedState { get; set; } = CanBeUsedState.Do_Nothing;
    #endregion

    #region Pressure Plate Options
    public enum PressurePlateUsableState { Do_Nothing, Usable, Unusable, Toggle }
    public PressurePlateUsableState pressurePlateUsableState { get; set; } = PressurePlateUsableState.Do_Nothing;
    #endregion

    #region Flame Trap Options
    public enum FlameTrapState { Do_Nothing, Activate, Deactivate, Toggle_State }
    public FlameTrapState flameTrapState { get; set; } = FlameTrapState.Toggle_State;
    #endregion

    #region Screen Options
    public bool changeScreenColorType { get; set; } = false;
    public ScreenColorType screenColorType { get; set; } = ScreenColorType.CYAN;
    public bool changeScreenText { get; set; } = false;
    public string screenNewText { get; set; } = "";
    #endregion

    #region Door Options
    public enum DoorState { Do_Nothing, Close, CloseFast, Open, Toggle }
    public DoorState doorState { get; set; } = DoorState.Toggle;
    #endregion

    #region MPs Options
    public enum MovingPlatformState
    {
        Do_Nothing,
        Activate,
        Deactivate,
        Toggle
    }
    public MovingPlatformState movingPlatformState { get; set; } = MovingPlatformState.Do_Nothing;
    #endregion

    #region Bridge Options
    public enum BridgeState { Do_Nothing, Extend, Retract, Toggle }
    public BridgeState bridgeState { get; set; } = BridgeState.Toggle;
    #endregion

    #region Destructible Wall Options
    public bool destructibleWallBreakNow { get; set; }
    #endregion

    #region Fragile Window
    public bool fragileWindowBreakNow { get; set; }
    #endregion

    #region Wait Options
    public bool isForWait { get; set; } = false;
    public float waitTime { get; set; }
    public enum WaitTimeUnit { Seconds, Miliseconds }
    public WaitTimeUnit waitTimeUnits { get; set; }
    #endregion

    #region Group Options
    public bool isForGroup { get; set; } = false;
    public int targetGroupID { get; set; }
    public bool allObjectsInGroupAreTheSame { get; set; }
    public LE_Object.ObjectType? sameObjectType { get; set; }
    #endregion

    #region Upgrade Terminal
    public enum TerminalActiveState { Do_Nothing, Active, Deactive, Toggle }
    public TerminalActiveState terminalActiveState { get; set; }
    #endregion


    public bool VerifyNormalEventValidity(string inputText)
    {
        targetInstanceObject = FS_LevelEditor.Editor.EditorController.Instance.currentInstantiatedObjects.FirstOrDefault(obj => string.Equals(obj.objectFullNameWithID, inputText,
                StringComparison.OrdinalIgnoreCase));
        if (targetInstanceObject)
        {
            if (targetInstanceObject.canBeUsedInEventsTab && !targetInstanceObject.isDeleted)
            {
                return true;
            }
        }

        return false;
    }
    public bool VerifyNormalEventValidity(LE_Object.ObjectType? objectType, int objectID, bool setGlobalVar)
    {
        targetInstanceObject = FS_LevelEditor.Utils.GetCurrentInstantiatedObjectsList().FirstOrDefault(x => x.objectType == objectType
            && x.objectID == objectID && !x.isDeleted);
        if (targetInstanceObject)
        {
            if (targetInstanceObject.canBeUsedInEventsTab && !targetInstanceObject.isDeleted)
            {
                return true;
            }
        }

        return false;
    }

    public void Setup(string inputText)
    {
        isForPlayer = false;
        isForTaser = false;
        isForJetpack = false;
        isForObjective = false;
        isForGroup = false;
        isForWait = false;
        targetObjType = null;
        targetObjID = 0;
        targetObjName = "";

        if (string.Equals(inputText, Loc.Get("Player"), StringComparison.OrdinalIgnoreCase))
        {
            isForPlayer = true;
        }
        else if (string.Equals(inputText, Loc.Get("Taser"), StringComparison.OrdinalIgnoreCase))
        {
            isForTaser = true;
        }
        else if (string.Equals(inputText, Loc.Get("Jetpack"), StringComparison.OrdinalIgnoreCase))
        {
            isForJetpack = true;
        }
        else if (inputText.StartsWith("Obj_", StringComparison.OrdinalIgnoreCase))
        {
            isForObjective = true;

            // Extract the objective name after "Objective_"
            objectiveName = inputText.Substring(4); // "Objective_" is 10 characters
        }
        else if (inputText.StartsWith("Wait", StringComparison.OrdinalIgnoreCase))
        {
            // Very dirty code, Ik it, alr?
            if (inputText.EndsWith("ms", StringComparison.OrdinalIgnoreCase))
            {
                waitTimeUnits = WaitTimeUnit.Miliseconds;

                string stripped = inputText.ToLower().Replace("wait", "").Replace("ms", "").Trim();
                waitTime = FS_LevelEditor.Utils.ParseFloat(stripped);
            }
            else if (inputText.EndsWith("s", StringComparison.OrdinalIgnoreCase))
            {
                waitTimeUnits = WaitTimeUnit.Seconds;

                string stripped = inputText.ToLower().Replace("wait", "").Replace("s", "").Trim();
                waitTime = FS_LevelEditor.Utils.ParseFloat(stripped);
            }
            else
            {
                return;
            }

            isForWait = true;
        }
        else if (inputText.StartsWith("Group", StringComparison.OrdinalIgnoreCase))
        {
            string[] splitted = inputText.Split(' ');
            if (splitted.Length != 2)
                return;
            if (!int.TryParse(splitted[1], out int targetGroup))
                return;

            if (!LE_Object.objectsPerGroup.TryGetValue(targetGroup, out var objectsInGroup))
                return;

            targetGroupID = targetGroup;

            if (LE_Object.ObjectsAreOfTheSameType(objectsInGroup))
            {
                allObjectsInGroupAreTheSame = true;
                sameObjectType = objectsInGroup[0].objectType;
            }
            else
            {
                allObjectsInGroupAreTheSame = false;
                sameObjectType = null;
            }

            isForGroup = true;
        }
        // VerifyNormalEventValidity already caches the targetInstanceObject, get its values so they can be serialized.
        else if (VerifyNormalEventValidity(inputText))
        {
            targetObjType = targetInstanceObject.objectType;
            targetObjID = targetInstanceObject.objectID;
        }
        else
        {
            targetObjName = inputText;
        }
    }

    public bool IsNormalEventThatRequriesTargetObject()
    {
        return !isForPlayer && !isForTaser && !isForJetpack && !isForObjective && !isForWait && !isForGroup;
    }
    public bool IsAtLeastOneTypeOfEvent()
    {
        return isForPlayer || isForTaser || isForJetpack || isForObjective || isForWait || isForGroup;
    }
}