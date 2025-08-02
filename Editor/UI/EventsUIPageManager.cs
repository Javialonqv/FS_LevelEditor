using FS_LevelEditor;
using FS_LevelEditor.UI_Related;
using Il2Cpp;
using Il2CppSystem.Runtime.Remoting.Messaging;
using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        GameObject eventsButtonsParent;
        UIButtonPatcher previousEventPageButton, nextEventPageButton;
        GameObject currentEventPageLabel;
        GameObject noEventsLabel;

        Transform topButtonsParent;
        UIButtonPatcher firstEventsListButton;
        UIButtonPatcher secondEventsListButton;
        UIButtonPatcher thirdEventsListButton;
        UILabel oneEventTypeLabel;

        const int eventsPerPage = 6;
        GameObject eventsListBg;
        GameObject eventsListsParent;
        List<GameObject> eventsPagesList = new List<GameObject>();
        int currentEventsPage = 0;

        /// <summary>
        /// Contains all of the options of an event, including the target object name field.
        /// </summary>
        GameObject eventSettingsPanel;
        UICustomInputField targetObjInputField;
        /// <summary>
        /// Contains all of the options of an event, EXCEPT the target object name field.
        /// </summary>
        GameObject eventOptionsParent;
        GameObject defaultObjectsSettings;
        UIDropdownPatcher spawnOptionsDropdown;
        UIDropdownPatcher colliderStateDropdown;
        //-----------------------------------
        GameObject sawObjectsSettings;
        UIButtonMultiple sawStateButton;
        //-----------------------------------
        GameObject playerSettings;
        UIToggle zeroGToggle;
        UIToggle invertGravityToggle;
        //-----------------------------------
        GameObject cubeObjectsSettings;
        UIToggle respawnCubeToggle;
        //-----------------------------------
        GameObject laserObjectsSettings;
        UIButtonMultiple laserStateButton;
        //-----------------------------------
        GameObject lightObjectsSettings;
        UIToggle changeLightColorToggle;
        UILabel newLightColorTitleLabel;
        UIInput newLightColorInputField;
        //-----------------------------------
        GameObject ceilingLightObjectsSettings;
        UIButtonMultiple ceilingLightStateButton;
        UIToggle changeCeilingLightColorToggle;
        UIInput newCeilingLightColorInputField;
        //-----------------------------------
        GameObject healthAmmoPacksObjectsSettings;
        UIToggle changePackRespawnTimeToggle;
        UILabel newPackRespawnTimeTitleLabel;
        UICustomInputField newPackRespawnTimeInputField;
        UIToggle spawnPackNowToggle;
        //-----------------------------------
        GameObject switchObjectsSettings;
        UIButtonMultiple switchStateButton;
        UIToggle executeSwitchActionsToggle;
        UIButtonMultiple switchUsableStateButton;
        //-----------------------------------
        GameObject flameTrapObjectsSettings;
        UIButtonMultiple flameTrapStateButton;
        //-----------------------------------
        GameObject screenObjectsSettings;
        UIToggle changeScreenColorTypeToggle;
        UISmallButtonMultiple screenColorTypeButton;
        UIToggle changeScreenTextToggle;
        UICustomInputField screenNewTextField;
        //-----------------------------------
        GameObject doorObjectsSettings;
        UIButtonMultiple setDoorStateButton;


        List<string> eventsListsNames = new List<string>();
        int currentEventsListID;
        string currentEventsListName;
        bool eventSelected;
        int currentSelectedEventID;
        LE_Event currentSelectedEvent;
        UIButton currentSelectedEventButton;

        ContextMenu eventsContextMenu;
        int selectedEventIDForContextMenu;

        LE_Object targetObj;

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

                Instance.CreateEventSettingsPanelAndOptionsParent();
                Instance.CreateTargetObjectINSTRUCTIONLabel();
                Instance.CreateTargetObjectInputField();
                Instance.CreateSelectTargetObjectButton();

                Instance.CreateDefaultObjectSettings();
                Instance.CreateSawObjectSettings();
                Instance.CreatePlayerSettings();
                Instance.CreateCubeObjectSettings();
                Instance.CreateLaserObjectSettings();
                Instance.CreateLightObjectSettings();
                Instance.CreateCeilingLightObjectSettings();
                Instance.CreateHealthAndAmmoPacksObjectSettings();
                Instance.CreateSwitchObjectSettings();
                Instance.CreateFlameTrapObjectSettings();
                Instance.CreateScreenObjectSettings();
                Instance.CreateDoorObjectSettings();

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

            // For some reason sometimes the window sprite can be transparent, force it to be opaque.
            eventsPanel.GetChild("Window").GetComponent<UISprite>().alpha = 1f;

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
            eventsBgSprite.atlas = NGUI_Utils.fractalSpaceAtlas;
            eventsBgSprite.spriteName = "Square";
            eventsBgSprite.depth = 1;
            eventsBgSprite.color = new Color(0.0509f, 0.3333f, 0.3764f);
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
            GameObject labelTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Label");

            currentEventPageLabel = Instantiate(labelTemplate, eventsListBg.transform);
            currentEventPageLabel.name = "CurrentEventPageLabel";
            currentEventPageLabel.transform.localScale = Vector3.one;

            Destroy(currentEventPageLabel.GetComponent<UILocalize>());

            UILabel label = currentEventPageLabel.GetComponent<UILabel>();
            label.pivot = UIWidget.Pivot.Center;
            label.alignment = NGUIText.Alignment.Center;
            label.height = 30;
            label.width = 800;
            label.fontSize = 30;
            label.text = "0/0";

            // Change the label position AFTER changing the pivot.
            currentEventPageLabel.transform.localPosition = new Vector3(0f, 300f, 0f);

            currentEventPageLabel.SetActive(false);
        }
        void CreateNoEventsLabel()
        {
            GameObject labelTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Label");

            noEventsLabel = Instantiate(labelTemplate, eventsListBg.transform);
            noEventsLabel.name = "NoEventsLabel";
            noEventsLabel.transform.localScale = Vector3.one;

            Destroy(noEventsLabel.GetComponent<UILocalize>());

            UILabel label = noEventsLabel.GetComponent<UILabel>();
            label.pivot = UIWidget.Pivot.Center;
            label.alignment = NGUIText.Alignment.Center;
            label.height = 50;
            label.width = 700;
            label.fontSize = 30;
            label.color = new Color(1f, 1f, 0f, 1f);
            label.text = "No Events Yet";

            // Change the label position AFTER changing the pivot.
            noEventsLabel.transform.localPosition = new Vector3(0f, 220f, 0f);

            noEventsLabel.SetActive(false);
        }

        void CreateEventsListsParent()
        {
            eventsListsParent = new GameObject("Lists");
            eventsListsParent.transform.parent = eventsListBg.transform;
            eventsListsParent.transform.localPosition = Vector3.zero;
            eventsListsParent.transform.localScale = Vector3.one;
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

            #region Move Up
            ContextMenuOption moveUpOption = new ContextMenuOption()
            {
                name = "Move Up",
                isEnabled = selectedEventIDForContextMenu > 0,
                onClick = () => MoveEventUp(selectedEventIDForContextMenu)
            };
            #endregion

            #region Move Down
            ContextMenuOption moveDownOption = new ContextMenuOption()
            {
                name = "Move Down",
                isEnabled = selectedEventIDForContextMenu < GetEventsList().Count - 1,
                onClick = () => MoveEventDown(selectedEventIDForContextMenu)
            };
            #endregion

            #region Delete
            ContextMenuOption deleteOption = new ContextMenuOption()
            {
                name = "Delete",
                onClick = () => DeleteEvent(selectedEventIDForContextMenu)
            };
            #endregion

            eventsContextMenu = ContextMenu.Create(eventsPanel.transform, depth: 3);
            eventsContextMenu.AddOption(copyToOption);
            eventsContextMenu.AddOption(moveToOption);
            eventsContextMenu.AddOption(duplicateOption);
            eventsContextMenu.AddOption(moveUpOption);
            eventsContextMenu.AddOption(moveDownOption);
            eventsContextMenu.AddOption(deleteOption);
        }
        #endregion

        void Update()
        {
            if (Input.GetMouseButtonDown(1) && EditorUIManager.IsCurrentUIContext(EditorUIContext.EVENTS_PANEL))
            {
                if (UICamera.selectedObject.TryGetComponent<EventButton>(out var eventBtn))
                {
                    selectedEventIDForContextMenu = eventBtn.eventID;
                    CreateContextMenu();
                    eventsContextMenu.Show();
                }
            }
        }

        #region Events List Related
        void RenameEvent(int eventID, UICustomInputField inputRef)
        {
            // GetEventsList should return the same events list that when creating the events list, it should be fine :)
            LE_Event eventToRename = GetEventsList()[eventID];
            eventToRename.eventName = inputRef.GetText();

            Logger.Log("RENAMED " + eventID + " TO: " + inputRef.GetText());
        }

        void SetupTopButtons()
        {
            eventsListsNames = targetObj.GetAvailableEventsIDs();

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
            CreateAllEventsPagesForList(0);
        }
        void SecondEventsListBtnClick()
        {
            Utils.PlayFSUISound(Utils.FS_UISound.INTERACTION_AVAILABLE);

            firstEventsListButton.GetComponent<UIButton>().defaultColor = new Color(0.218f, 0.6464f, 0.6509f, 1f);
            secondEventsListButton.GetComponent<UIButton>().defaultColor = new Color(0f, 1f, 0f, 1f);
            thirdEventsListButton.GetComponent<UIButton>().defaultColor = new Color(0.218f, 0.6464f, 0.6509f, 1f);

            OnEventSelect(null);
            CreateAllEventsPagesForList(1);
        }
        void ThirdEventsListBtnClick()
        {
            Utils.PlayFSUISound(Utils.FS_UISound.INTERACTION_AVAILABLE);

            firstEventsListButton.GetComponent<UIButton>().defaultColor = new Color(0.218f, 0.6464f, 0.6509f, 1f);
            secondEventsListButton.GetComponent<UIButton>().defaultColor = new Color(0.218f, 0.6464f, 0.6509f, 1f);
            thirdEventsListButton.GetComponent<UIButton>().defaultColor = new Color(0f, 1f, 0f, 1f);

            OnEventSelect(null);
            CreateAllEventsPagesForList(2);
        }

        void CreateAllEventsPagesForList(int listID)
        {
            currentEventsListID = listID;
            currentEventsListName = eventsListsNames[listID];

            eventsListsParent.DeleteAllChildren();
            eventsPagesList.Clear();

            List<LE_Event> events = GetEventsList(listID);
            int eventsGridCount = Mathf.CeilToInt((float)events.Count / eventsPerPage);

            for (int i = 0; i < eventsGridCount; i++)
            {
                CreateEventsPage(i);
            }

            // The first one's selected by default, always.
            ShowEventPage(0);

            // Refresh the pages buttons state, the "No Events" label, etc.
            RefreshStateOfEventsListUIElements();
        }
        void CreateEventsPage(int gridID, bool showPage = true, bool deselectCurrentSelectedEvent = true)
        {
            #region Get Or Create Page
            GameObject pageObj;
            if (eventsPagesList.Count > gridID) // Page already exists, just delete its childs.
            {
                pageObj = eventsPagesList[gridID];
                pageObj.DeleteAllChildren(true);
            }
            else // Page doesn't exist yet, create it.
            {
                // Create a page.
                pageObj = new GameObject($"Grid {gridID}");
                pageObj.transform.parent = eventsListsParent.transform;
                pageObj.transform.localPosition = new Vector3(0f, 220f, 0f);
                pageObj.transform.localScale = Vector3.one;

                // Add the UIGrid component, ofc.
                UIGrid grid = pageObj.AddComponent<UIGrid>();
                grid.arrangement = UIGrid.Arrangement.Vertical;
                grid.cellWidth = 780f;
                grid.cellHeight = 80f;

                pageObj.SetActive(false);

                eventsPagesList.Add(pageObj);
            }
            #endregion

            List<LE_Event> events = GetEventsList();
            int startIndex = gridID * eventsPerPage;
            int eventsCount = Mathf.Clamp(events.Count - (gridID * eventsPerPage), 0, 6);
            events = events.GetRange(startIndex, eventsCount);

            if (events.Count == 0) // In case the page is empty... destroy it and create the page BEFORE it :)
            {
                eventsPagesList.Remove(pageObj);
                Destroy(pageObj);
                CreateEventsPage(gridID - 1);
                return;
            }

            for (int i = 0; i < events.Count; i++)
            {
                int realEventID = (gridID * 6) + i;

                // Create the event button PARENT, since inside of it are the button, the name label, and delete btn.
                GameObject eventButtonParent = new GameObject($"Event {i}");
                eventButtonParent.transform.parent = pageObj.transform;
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
                eventScript.eventTypeID = currentEventsListID;
                eventScript.eventID = realEventID;

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

                deleteBtn.onClick += () => DeleteEvent(realEventID);
                #endregion

                #region Name Input Field
                var nameInput = NGUI_Utils.CreateInputField(eventButtonParent.transform, new Vector3(-150, 0), new Vector3Int(450, 50, 0), 27, "", true, depth: 4);
                nameInput.name = "NameInputField";
                UISprite outlineSprite = nameInput.GetComponents<UISprite>()[1];
                outlineSprite.width = 455;
                outlineSprite.height = 55;

                nameInput.SetText(events[i].eventName);
                nameInput.onSubmit += () => RenameEvent(realEventID, nameInput);

                nameInput.GetComponents<UISprite>()[0].Invoke("MarkAsChanged", 0.01f);
                #endregion
            }
            pageObj.GetComponent<UIGrid>().Invoke("Reposition", 0.01f);

            if (showPage) ShowEventPage(gridID, deselectCurrentSelectedEvent);
            else RefreshStateOfEventsListUIElements(); // If we don't show it, at least refresh the UI.
        }
        void CreateEventsPageForEventOfID(int eventID, bool showPage = true)
        {
            if ((eventID / eventsPerPage) != currentEventsPage)
            {
                CreateEventsPage((eventID / eventsPerPage), showPage);
            }
            else
            {
                CreateEventsPage(currentEventsPage, showPage);
            }
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
        void ShowEventPage(int pageID, bool deselectCurrentSelectedEvent = true)
        {
            if (pageID < 0 || (pageID >= eventsPagesList.Count && pageID != int.MaxValue)) return;

            if (pageID == int.MaxValue)
            {
                pageID = eventsPagesList.Count - 1;
            }

            currentEventsPage = pageID;

            eventsPagesList.ForEach(x => x.SetActive(false));
            eventsPagesList[currentEventsPage].SetActive(true);

            if (deselectCurrentSelectedEvent) OnEventSelect(null);
            RefreshStateOfEventsListUIElements();
        }
        void PreviousEventsPage()
        {
            if (currentEventsPage <= 0) return;

            ShowEventPage(currentEventsPage - 1);
        }
        void NextEventsPage()
        {
            if (currentEventsPage >= eventsPagesList.Count - 1) return;

            ShowEventPage(currentEventsPage + 1);
        }
        void RefreshStateOfEventsListUIElements()
        {
            // Only enable the page buttons and the page label once they're are more than 1 grid (1 event page).
            previousEventPageButton.gameObject.SetActive(eventsPagesList.Count > 1);
            nextEventPageButton.gameObject.SetActive(eventsPagesList.Count > 1);
            currentEventPageLabel.SetActive(eventsPagesList.Count > 1);

            // Enable the No Events Label in case there aren't any events...
            noEventsLabel.SetActive(eventsPagesList.Count == 0);

            // Update the state of the page buttons and the page label in case now they're enabled.
            previousEventPageButton.button.isEnabled = currentEventsPage > 0;
            nextEventPageButton.button.isEnabled = currentEventsPage < eventsPagesList.Count - 1;
            currentEventPageLabel.GetComponent<UILabel>().text = GetCurrentEventPageText();
        }
        string GetCurrentEventPageText()
        {
            return currentEventsPage + 1 + "/" + eventsPagesList.Count;
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
                //GameObject[] test2 = eventsPagesList[currentSelectedEventID / eventsPerPage].GetChilds();
                //Transform test = eventsPagesList[currentSelectedEventID / eventsPerPage].transform.GetChild(currentSelectedEventID % eventsPerPage);
                currentSelectedEventButton = eventsPagesList[currentSelectedEventID / eventsPerPage].transform.GetChild(currentSelectedEventID % eventsPerPage).
                    GetChild(0).GetComponent<UIButton>();
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
            int pagesInCurrentListBeforeMove = GetEventsPagesCountForCurrentListID();

            List<LE_Event> originList = GetEventsList();
            LE_Event toMove = originList[eventID];
            List<LE_Event> targetList = GetEventsList(targetListID);

            originList.Remove(toMove);
            targetList.Add(toMove);

            if (originList.Count > 0) // Update the current page we're on.
            {
                // Update the target list in case is the current one.
                if (targetListID == currentEventsListID) CreateEventsPageForEventOfID(targetList.Count - 1, false);

                for (int i = currentEventsPage; i < GetEventsPagesCountForCurrentListID(); i++)
                {
                    CreateEventsPage(i, false); // Update the whole pages after the current one.
                }
                if (pagesInCurrentListBeforeMove > GetEventsPagesCountForCurrentListID()) CreateEventsPage(GetEventsPagesCountForCurrentListID(), false);
                if (eventSelected) OnEventSelect(eventID > 0 ? eventID - 1 : 0);
            }
            else // Hide everything, fuck it.
            {
                // If there are no events on this list, 100% the target list wasn't the current one, no update shit.

                CreateAllEventsPagesForList(currentEventsListID);
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
                    CreateEventsPage(currentEventsPage, false, false);
                    CreateEventsPage(currentEventsPage - 1, true, false);
                    OnEventSelect(eventID - 1);
                }
                else
                {
                    // If wasn't select, just update both of the affected pages.
                    CreateEventsPage(currentEventsPage - 1, false);
                    CreateEventsPage(currentEventsPage, true);
                }
            }
            else // The event still in the current page.
            {
                // Update the current page and select the event only if it was selected before.
                CreateEventsPage(currentEventsPage, deselectCurrentSelectedEvent: !(eventSelected && currentSelectedEvent == toMoveUp));
                if (eventSelected && currentSelectedEvent == toMoveUp) OnEventSelect(eventID - 1);
            }

            //if (eventID % 6 == 0)
            //{
            //    PreviousEventsPage();
            //    OnEventSelect(eventID - 1);
            //}
            //else
            //{
            //    CreateEventsPage(currentEventsPage);
            //    OnEventSelect(eventID - 1);
            //}
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
                    CreateEventsPage(currentEventsPage, false, false);
                    CreateEventsPage(currentEventsPage + 1, true, false);
                    OnEventSelect(eventID + 1);
                }
                else
                {
                    // If wasn't select, just update both of the affected pages.
                    CreateEventsPage(currentEventsPage + 1, false);
                    CreateEventsPage(currentEventsPage, true);
                }
            }
            else // The event still in the current page.
            {
                // Update the current page and select the event only if it was selected before.
                CreateEventsPage(currentEventsPage, deselectCurrentSelectedEvent: !(eventSelected && currentSelectedEvent == toMoveDown));
                if (eventSelected && currentSelectedEvent == toMoveDown) OnEventSelect(eventID + 1);
            }

            //if ((eventID + 7) % 6 == 0)
            //{
            //    NextEventsPage();
            //    OnEventSelect(eventID + 1);
            //}
            //else
            //{
            //    CreateEventsPage(currentEventsPage);
            //    OnEventSelect(eventID + 1);
            //}
        }
        void DeleteEvent(int eventID)
        {
            int pagesBeforeRemove = GetEventsPagesCountForCurrentListID();

            OnEventSelect(null);
            GetEventsList().RemoveAt(eventID);
            if (GetEventsList().Count > 0)
            {
                // Is the current page the last one? Since the event was already removed, don't substract -1 to the GetEventsPagesCountForCurrentListID() result.
                if (currentEventsPage == GetEventsPagesCountForCurrentListID())
                {
                    CreateEventsPage(GetEventsPagesCountForCurrentListID());
                }
                else
                {
                    for (int i = currentEventsPage; i < GetEventsPagesCountForCurrentListID(); i++)
                    {
                        CreateEventsPage(i, i == currentEventsPage);
                    }
                    if (pagesBeforeRemove > GetEventsPagesCountForCurrentListID()) CreateEventsPage(GetEventsPagesCountForCurrentListID(), false);
                }
            }
            else
            {
                // This won't do shit, besides deleting the last remaining button and refresh the UI elements.
                CreateAllEventsPagesForList(currentEventsListID);
            }
        }

        int GetPageIDForEvent(int eventID)
        {
            return eventID / eventsPerPage;
        }
        int GetEventsPagesCountForCurrentListID()
        {
            List<LE_Event> events = GetEventsList();
            return Mathf.CeilToInt((float)events.Count / eventsPerPage);
        }
        int GetEventsCountForPage(int pageID)
        {
            List<LE_Event> events = GetEventsList();
            int eventsCount = Mathf.Clamp(events.Count - (pageID * eventsPerPage), 0, 6);
            return eventsCount;
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
            GameObject labelTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Label");

            GameObject targetObjectLabel = Instantiate(labelTemplate, eventSettingsPanel.transform);
            targetObjectLabel.name = "TargetObjectLabel";
            targetObjectLabel.transform.localScale = Vector3.one;

            Destroy(targetObjectLabel.GetComponent<UILocalize>());

            UILabel label = targetObjectLabel.GetComponent<UILabel>();
            label.pivot = UIWidget.Pivot.Center;
            label.alignment = NGUIText.Alignment.Center;
            label.height = 30;
            label.width = 700;
            label.fontSize = 30;
            label.text = "Enter the target object name:";

            // Change the label position AFTER changing the pivot.
            targetObjectLabel.transform.localPosition = new Vector3(0f, 290f, 0f);
        }
        void CreateTargetObjectInputField()
        {
            targetObjInputField = NGUI_Utils.CreateInputField(eventSettingsPanel.transform, new Vector3(0f, 230f, 0f), new Vector3Int(500, 60, 0), 34,
                "", true, NGUIText.Alignment.Center);

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
                else
                {
                    targetObjInputField.SetText(currentSelectedEvent.targetObjName);
                }
            }

            spawnOptionsDropdown.SelectOption((int)currentSelectedEvent.spawn);
            colliderStateDropdown.SelectOption((int)currentSelectedEvent.colliderState);
            sawStateButton.SelectOption((int)currentSelectedEvent.sawState);
            zeroGToggle.Set(currentSelectedEvent.enableOrDisableZeroG);
            invertGravityToggle.Set(currentSelectedEvent.invertGravity);
            respawnCubeToggle.Set(currentSelectedEvent.respawnCube);
            laserStateButton.SelectOption((int)currentSelectedEvent.laserState);
            changeLightColorToggle.Set(currentSelectedEvent.changeLightColor);
            newLightColorTitleLabel.gameObject.SetActive(currentSelectedEvent.changeLightColor);
            newLightColorInputField.gameObject.SetActive(currentSelectedEvent.changeLightColor);
            newLightColorInputField.text = currentSelectedEvent.newLightColor;
            ceilingLightStateButton.SelectOption((int)currentSelectedEvent.ceilingLightState);
            changeCeilingLightColorToggle.Set(currentSelectedEvent.changeCeilingLightColor);
            newCeilingLightColorInputField.text = currentSelectedEvent.newCeilingLightColor;
            changePackRespawnTimeToggle.Set(currentSelectedEvent.changePackRespawnTime);
            newPackRespawnTimeTitleLabel.gameObject.SetActive(currentSelectedEvent.changePackRespawnTime);
            newPackRespawnTimeInputField.gameObject.SetActive(currentSelectedEvent.changePackRespawnTime);
            newPackRespawnTimeInputField.SetText(currentSelectedEvent.packRespawnTime);
            spawnPackNowToggle.Set(currentSelectedEvent.spawnPackNow);
            switchStateButton.SelectOption((int)currentSelectedEvent.switchState);
            executeSwitchActionsToggle.Set(currentSelectedEvent.executeSwitchActions);
            switchUsableStateButton.SelectOption((int)currentSelectedEvent.switchUsableState);
            flameTrapStateButton.SelectOption((int)currentSelectedEvent.flameTrapState);
            changeScreenColorTypeToggle.Set(currentSelectedEvent.changeScreenColorType);
            screenColorTypeButton.SetOption((int)currentSelectedEvent.screenColorType, true);
            changeScreenTextToggle.Set(currentSelectedEvent.changeScreenText);
            screenNewTextField.SetText(currentSelectedEvent.screenNewText);
            setDoorStateButton.SelectOption((int)currentSelectedEvent.doorState);

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
            LE_Object targetObj = null;
            bool objIsValid = false;

            #region Check if the object is valid
            if (string.Equals(inputText, Loc.Get("Player"), StringComparison.OrdinalIgnoreCase))
            {
                objIsValid = true;
            }
            else
            {
                targetObj = EditorController.Instance.currentInstantiatedObjects.FirstOrDefault(obj => string.Equals(obj.objectFullNameWithID, inputText,
                    StringComparison.OrdinalIgnoreCase));
                if (targetObj)
                {
                    if (targetObj.canBeUsedInEventsTab)
                    {
                        objIsValid = true;
                    }
                }
            }
            #endregion

            // If the object name that the user put there is valid and exists...
            if (objIsValid)
            {
                fieldSprite.color = new Color(0.0588f, 0.3176f, 0.3215f, 0.9412f);
                eventOptionsParent.SetActive(true);
                eventOptionsParent.DisableAllChildren();

                if (string.Equals(inputText, Loc.Get("Player"), StringComparison.OrdinalIgnoreCase))
                {
                    currentSelectedEvent.isForPlayer = true;
                    currentSelectedEvent.targetObjType = null;
                    currentSelectedEvent.targetObjID = 0;
                    currentSelectedEvent.targetObjName = "";
                }
                else
                {
                    currentSelectedEvent.isForPlayer = false;
                    currentSelectedEvent.targetObjType = targetObj.objectType;
                    currentSelectedEvent.targetObjID = targetObj.objectID;
                    currentSelectedEvent.targetObjName = ""; // While the object is valid, don't use the name, use the type and ID instead.
                }

                if (!currentSelectedEvent.isForPlayer) defaultObjectsSettings.SetActive(true);
                if (currentSelectedEvent.isForPlayer)
                {
                    playerSettings.SetActive(true);
                }
                else if (targetObj is LE_Saw)
                {
                    sawObjectsSettings.SetActive(true);
                }
                else if (targetObj is LE_Cube)
                {
                    cubeObjectsSettings.SetActive(true);
                }
                else if (targetObj is LE_Laser)
                {
                    laserObjectsSettings.SetActive(true);
                }
                else if (targetObj is LE_Directional_Light || targetObj is LE_Point_Light)
                {
                    lightObjectsSettings.SetActive(true);
                }
                else if (targetObj is LE_Ceiling_Light)
                {
                    ceilingLightObjectsSettings.SetActive(true);
                }
                else if (targetObj is LE_Health_Pack || targetObj is LE_Ammo_Pack)
                {
                    healthAmmoPacksObjectsSettings.SetActive(true);
                }
                else if (targetObj is LE_Switch)
                {
                    switchObjectsSettings.SetActive(true);
                }
                else if (targetObj is LE_Flame_Trap)
                {
                    flameTrapObjectsSettings.SetActive(true);
                }
                else if (targetObj is LE_Screen || targetObj is LE_Small_Screen)
                {
                    screenObjectsSettings.SetActive(true);
                }
                else if (targetObj is LE_Door || targetObj is LE_Door_V2)
                {
                    doorObjectsSettings.SetActive(true);
                }
            }
            else
            {
                fieldSprite.color = new Color(0.3215f, 0.2156f, 0.0588f, 0.9415f);
                eventOptionsParent.SetActive(false);
                eventOptionsParent.DisableAllChildren();

                currentSelectedEvent.isForPlayer = false;
                currentSelectedEvent.targetObjType = null;
                currentSelectedEvent.targetObjID = 0;
                currentSelectedEvent.targetObjName = inputText;
            }

            currentSelectedEvent.isValid = objIsValid;
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
            OnTargetObjectFieldChanged(targetObjInputField, targetObjInputField.GetComponent<UISprite>());
        }

        #region Create UI Elements For Objects
        void CreateDefaultObjectSettings()
        {
            defaultObjectsSettings = new GameObject("Default");
            defaultObjectsSettings.transform.parent = eventOptionsParent.transform;
            defaultObjectsSettings.transform.localPosition = Vector3.zero;
            defaultObjectsSettings.transform.localScale = Vector3.one;
            defaultObjectsSettings.SetActive(false);

            CreateSpawnOptionsDropdown();
            CreateColliderStateDropdown();
        }
        void CreateSpawnOptionsDropdown()
        {
            UIDropdownPatcher spawnOptionsDropdown = NGUI_Utils.CreateDropdown(defaultObjectsSettings.transform, new Vector3(-190, 105), Vector3.one * 0.8f);
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
            var colliderStateDropdown = NGUI_Utils.CreateDropdown(defaultObjectsSettings.transform, new Vector3(190, 105), Vector3.one * 0.8f);
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
        // -----------------------------------------
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
            GameObject labelTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Label");

            GameObject titleLabel = Instantiate(labelTemplate, sawObjectsSettings.transform);
            titleLabel.name = "TitleLabel";
            titleLabel.transform.localScale = Vector3.one;

            Destroy(titleLabel.GetComponent<UILocalize>());

            UILabel label = titleLabel.GetComponent<UILabel>();
            label.pivot = UIWidget.Pivot.Center;
            label.alignment = NGUIText.Alignment.Center;
            label.height = 40;
            label.width = 700;
            label.fontSize = 35;
            label.text = "SAW OPTIONS";

            // Change the label position AFTER changing the pivot.
            titleLabel.transform.localPosition = new Vector3(0f, 40f, 0f);
        }
        void CreateSawStateDropdown()
        {
            UIButtonMultiple button = NGUI_Utils.CreateButtonMultiple(sawObjectsSettings.transform, new Vector3(0, -10), Vector3.one * 0.8f);
            button.SetTitle("Saw State");
            button.ClearOptions();
            button.AddOption("Do Nothing", true);
            button.AddOption("Activate", false);
            button.AddOption("Deactivate", false);
            button.AddOption("Toggle State", false);
            button.onClick += (option) => OnSawStateDropdownChanged();

            sawStateButton = button;
            button.gameObject.SetActive(true);
        }
        // -----------------------------------------
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
        }
        void CreatePlayerSettingsTitleLabel()
        {
            GameObject labelTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Label");

            GameObject titleLabel = Instantiate(labelTemplate, playerSettings.transform);
            titleLabel.name = "TitleLabel";
            titleLabel.transform.localScale = Vector3.one;

            Destroy(titleLabel.GetComponent<UILocalize>());

            UILabel label = titleLabel.GetComponent<UILabel>();
            label.pivot = UIWidget.Pivot.Center;
            label.alignment = NGUIText.Alignment.Center;
            label.height = 40;
            label.width = 700;
            label.fontSize = 35;
            label.text = "PLAYER OPTIONS";

            // Change the label position AFTER changing the pivot.
            titleLabel.transform.localPosition = new Vector3(0f, 120f, 0f);
        }
        void CreateZeroGToggle()
        {
            GameObject toggleTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles");

            GameObject toggle = NGUI_Utils.CreateToggle(playerSettings.transform, new Vector3(-380f, 50f, 0f),
                new Vector3Int(250, 48, 1), "Enable/Disable Zero G");
            toggle.name = "EnableOrDisableZeroGToggle";
            zeroGToggle = toggle.GetComponent<UIToggle>();
            zeroGToggle.onChange.Clear();
            zeroGToggle.onChange.Add(new EventDelegate(this, nameof(OnZeroGToggleChanged)));
        }
        void CreateInvertGravityToggle()
        {
            GameObject toggleTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles");

            GameObject toggle = NGUI_Utils.CreateToggle(playerSettings.transform, new Vector3(50f, 50f, 0f),
                new Vector3Int(250, 48, 1), "Invert Gravity");
            toggle.name = "InvertGravityToggle";
            invertGravityToggle = toggle.GetComponent<UIToggle>();
            invertGravityToggle.onChange.Clear();
            invertGravityToggle.onChange.Add(new EventDelegate(this, nameof(OnInvertGravityToggleChanged)));
        }
        // -----------------------------------------
        void CreateCubeObjectSettings()
        {
            cubeObjectsSettings = new GameObject("Cube");
            cubeObjectsSettings.transform.parent = eventOptionsParent.transform;
            cubeObjectsSettings.transform.localPosition = Vector3.zero;
            cubeObjectsSettings.transform.localScale = Vector3.one;
            cubeObjectsSettings.SetActive(false);

            CreateCubeObjectsTitleLabel();
            CreateRespawnCubeToggle();
        }
        void CreateCubeObjectsTitleLabel()
        {
            GameObject labelTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Label");

            GameObject titleLabel = Instantiate(labelTemplate, cubeObjectsSettings.transform);
            titleLabel.name = "TitleLabel";
            titleLabel.transform.localScale = Vector3.one;

            Destroy(titleLabel.GetComponent<UILocalize>());

            UILabel label = titleLabel.GetComponent<UILabel>();
            label.pivot = UIWidget.Pivot.Center;
            label.alignment = NGUIText.Alignment.Center;
            label.height = 40;
            label.width = 700;
            label.fontSize = 35;
            label.text = "CUBE OPTIONS";

            // Change the label position AFTER changing the pivot.
            titleLabel.transform.localPosition = new Vector3(0f, 40f, 0f);
        }
        void CreateRespawnCubeToggle()
        {
            GameObject toggleTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles");

            GameObject toggle = NGUI_Utils.CreateToggle(cubeObjectsSettings.transform, new Vector3(-140f, -30f, 0f),
                new Vector3Int(250, 48, 1), "Respawn Cube");
            toggle.name = "RespawnCubeToggle";
            respawnCubeToggle = toggle.GetComponent<UIToggle>();
            respawnCubeToggle.onChange.Clear();
            respawnCubeToggle.onChange.Add(new EventDelegate(this, nameof(OnRespawnCubeChanged)));
        }
        // -----------------------------------------
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
            GameObject labelTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Label");

            GameObject titleLabel = Instantiate(labelTemplate, laserObjectsSettings.transform);
            titleLabel.name = "TitleLabel";
            titleLabel.transform.localScale = Vector3.one;

            Destroy(titleLabel.GetComponent<UILocalize>());

            UILabel label = titleLabel.GetComponent<UILabel>();
            label.pivot = UIWidget.Pivot.Center;
            label.alignment = NGUIText.Alignment.Center;
            label.height = 40;
            label.width = 700;
            label.fontSize = 35;
            label.text = "LASER OPTIONS";

            // Change the label position AFTER changing the pivot.
            titleLabel.transform.localPosition = new Vector3(0f, 40f, 0f);
        }
        void CreateLaserStateDropdown()
        {
            UIButtonMultiple button = NGUI_Utils.CreateButtonMultiple(laserObjectsSettings.transform, new Vector3(0, -10), Vector3.one * 0.8f);
            button.Init();
            button.SetTitle("Laser State");
            button.ClearOptions();
            button.AddOption("Do Nothing", true);
            button.AddOption("Activate", false);
            button.AddOption("Deactivate", false);
            button.AddOption("Toggle State", false);
            button.onClick += (option) => OnLaserStateDropdownChanged();

            laserStateButton = button;
            button.gameObject.SetActive(true);
        }
        // -----------------------------------------
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
            GameObject labelTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Label");

            GameObject titleLabel = Instantiate(labelTemplate, lightObjectsSettings.transform);
            titleLabel.name = "TitleLabel";
            titleLabel.transform.localScale = Vector3.one;

            Destroy(titleLabel.GetComponent<UILocalize>());

            UILabel label = titleLabel.GetComponent<UILabel>();
            label.pivot = UIWidget.Pivot.Center;
            label.alignment = NGUIText.Alignment.Center;
            label.height = 40;
            label.width = 700;
            label.fontSize = 35;
            label.text = "LIGHT OPTIONS";

            // Change the label position AFTER changing the pivot.
            titleLabel.transform.localPosition = new Vector3(0f, 40f, 0f);
        }
        void CreateChangeLightColorToggle()
        {
            GameObject toggle = NGUI_Utils.CreateToggle(lightObjectsSettings.transform, new Vector3(-380f, -30f, 0f),
                new Vector3Int(250, 48, 1), "Change Color");
            toggle.name = "ChangeLightColorToggle";
            changeLightColorToggle = toggle.GetComponent<UIToggle>();
            changeLightColorToggle.onChange.Clear();
            changeLightColorToggle.onChange.Add(new EventDelegate(this, nameof(OnChangeLightColorToggleChanged)));
        }
        void CreateNewLightColorTitleLabel()
        {
            GameObject labelTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Label");

            GameObject titleLabel = Instantiate(labelTemplate, lightObjectsSettings.transform);
            titleLabel.name = "NewLightColorTitleLabel";
            titleLabel.transform.localScale = Vector3.one;

            Destroy(titleLabel.GetComponent<UILocalize>());

            UILabel label = titleLabel.GetComponent<UILabel>();
            label.pivot = UIWidget.Pivot.Center;
            label.alignment = NGUIText.Alignment.Center;
            label.height = 40;
            label.width = 150;
            label.fontSize = 27;
            label.text = "New Color";

            // Change the label position AFTER changing the pivot.
            titleLabel.transform.localPosition = new Vector3(50f, -30f, 0f);

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
        // -----------------------------------------
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
            GameObject labelTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Label");

            GameObject titleLabel = Instantiate(labelTemplate, ceilingLightObjectsSettings.transform);
            titleLabel.name = "TitleLabel";
            titleLabel.transform.localScale = Vector3.one;

            Destroy(titleLabel.GetComponent<UILocalize>());

            UILabel label = titleLabel.GetComponent<UILabel>();
            label.pivot = UIWidget.Pivot.Center;
            label.alignment = NGUIText.Alignment.Center;
            label.height = 40;
            label.width = 700;
            label.fontSize = 35;
            label.text = "CEILING LIGHT OPTIONS";

            // Change the label position AFTER changing the pivot.
            titleLabel.transform.localPosition = new Vector3(0f, 40f, 0f);
        }
        void CreateCeilingLightStateDropdown()
        {
            UIButtonMultiple button = NGUI_Utils.CreateButtonMultiple(ceilingLightObjectsSettings.transform, new Vector3(-200, -10), Vector3.one * 0.8f);
            button.Init();
            button.SetTitle("Turn");
            button.ClearOptions();
            button.AddOption("Do Nothing", true);
            button.AddOption("On", false);
            button.AddOption("Off", false);
            button.AddOption("Toggle On/Off", false);
            button.onClick += (option) => OnCeilingLightStateDropdownChanged();

            ceilingLightStateButton = button;
            button.gameObject.SetActive(true);
        }
        void CreateChangeCeilingLightColorToggle()
        {
            GameObject toggle = NGUI_Utils.CreateToggle(ceilingLightObjectsSettings.transform, new Vector3(20f, -17f, 0f),
                new Vector3Int(250, 48, 1), "Change Color");
            toggle.name = "ChangeCeilingLightColorToggle";

            changeCeilingLightColorToggle = toggle.GetComponent<UIToggle>();
            changeCeilingLightColorToggle.onChange.Clear();
            changeCeilingLightColorToggle.onChange.Add(new EventDelegate(this, nameof(OnChangeCeilingLightColorToggleChanged)));
        }
        void CreateNewCeilingLightColorInputField()
        {
            UICustomInputField inputField = NGUI_Utils.CreateInputField(ceilingLightObjectsSettings.transform, new Vector3(160f, -70f, 0f),
                new Vector3Int(250, 40, 1), 27, "FFFFFF", inputType: UICustomInputField.UIInputType.HEX_COLOR);
            inputField.name = "NewCeilingLightColorInputField";
            inputField.onChange += OnNewCeilingLightColorInputFieldChanged;

            newCeilingLightColorInputField = inputField.GetComponent<UIInput>();
        }
        // -----------------------------------------
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
            GameObject labelTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Label");

            GameObject titleLabel = Instantiate(labelTemplate, healthAmmoPacksObjectsSettings.transform);
            titleLabel.name = "TitleLabel";
            titleLabel.transform.localScale = Vector3.one;

            Destroy(titleLabel.GetComponent<UILocalize>());

            UILabel label = titleLabel.GetComponent<UILabel>();
            label.pivot = UIWidget.Pivot.Center;
            label.alignment = NGUIText.Alignment.Center;
            label.height = 40;
            label.width = 700;
            label.fontSize = 35;
            label.text = "HEALTH & AMMO PACK OPTIONS";

            // Change the label position AFTER changing the pivot.
            titleLabel.transform.localPosition = new Vector3(0f, 40f, 0f);
        }
        void CreateChangePackRespawnTimeToggle()
        {
            GameObject toggle = NGUI_Utils.CreateToggle(healthAmmoPacksObjectsSettings.transform, new Vector3(-380f, -30f, 0f),
                new Vector3Int(250, 48, 1), "Change Respawn Time");
            toggle.name = "ChangeRespawnTimeToggle";
            changePackRespawnTimeToggle = toggle.GetComponent<UIToggle>();
            changePackRespawnTimeToggle.onChange.Clear();
            changePackRespawnTimeToggle.onChange.Add(new EventDelegate(this, nameof(OnChangePackRespawnTimeToggleChanged)));
        }
        void CreateNewPackRespawnTimeTitleLabel()
        {
            GameObject labelTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Label");

            GameObject titleLabel = Instantiate(labelTemplate, healthAmmoPacksObjectsSettings.transform);
            titleLabel.name = "NewRespawnTimeTitleLabel";
            titleLabel.transform.localScale = Vector3.one;

            Destroy(titleLabel.GetComponent<UILocalize>());

            UILabel label = titleLabel.GetComponent<UILabel>();
            label.pivot = UIWidget.Pivot.Center;
            label.alignment = NGUIText.Alignment.Center;
            label.height = 40;
            label.width = 150;
            label.fontSize = 27;
            label.text = "Time";

            // Change the label position AFTER changing the pivot.
            titleLabel.transform.localPosition = new Vector3(50f, -30f, 0f);

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
            GameObject toggle = NGUI_Utils.CreateToggle(healthAmmoPacksObjectsSettings.transform, new Vector3(-140f, -100f, 0f),
                new Vector3Int(250, 48, 1), "Spawn Pack Now");
            toggle.name = "SpawnPackNowToggle";
            spawnPackNowToggle = toggle.GetComponent<UIToggle>();
            spawnPackNowToggle.onChange.Clear();
            spawnPackNowToggle.onChange.Add(new EventDelegate(this, nameof(OnSpawnPackNowToggleChanged)));
        }
        // -----------------------------------------
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
        }
        void CreateSwitchObjectsTitleLabel()
        {
            GameObject labelTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Label");

            GameObject titleLabel = Instantiate(labelTemplate, switchObjectsSettings.transform);
            titleLabel.name = "TitleLabel";
            titleLabel.transform.localScale = Vector3.one;

            Destroy(titleLabel.GetComponent<UILocalize>());

            UILabel label = titleLabel.GetComponent<UILabel>();
            label.pivot = UIWidget.Pivot.Center;
            label.alignment = NGUIText.Alignment.Center;
            label.height = 40;
            label.width = 700;
            label.fontSize = 35;
            label.text = "SWITCH OPTIONS";

            // Change the label position AFTER changing the pivot.
            titleLabel.transform.localPosition = new Vector3(0f, 40f, 0f);
        }
        void CreateSwitchStateSettings()
        {
            UIButtonMultiple button = NGUI_Utils.CreateButtonMultiple(switchObjectsSettings.transform, new Vector3(-200, -10), Vector3.one * 0.8f);
            button.Init();
            button.SetTitle("Set Active State");
            button.ClearOptions();
            button.AddOption("Do Nothing", true);
            button.AddOption("Activated", false);
            button.AddOption("Deactivated", false);
            button.AddOption("Toggle", false);
            button.onClick += (option) => OnSwitchStateDropdownChanged();

            switchStateButton = button;
            button.gameObject.SetActive(true);
        }
        void CreateExecuteSwitchActionsToggle()
        {
            GameObject toggle = NGUI_Utils.CreateToggle(switchObjectsSettings.transform, new Vector3(-350f, -120f, 0f),
                new Vector3Int(250, 48, 1), "Execute Actions");
            toggle.name = "ExecuteActionsToggle";

            executeSwitchActionsToggle = toggle.GetComponent<UIToggle>();
            executeSwitchActionsToggle.onChange.Clear();
            executeSwitchActionsToggle.onChange.Add(new EventDelegate(this, nameof(OnExecuteSwitchActionsToggleChanged)));
        }
        void CreateSwitchUsableStateSettings()
        {
            UIButtonMultiple button = NGUI_Utils.CreateButtonMultiple(switchObjectsSettings.transform, new Vector3(200, -10), Vector3.one * 0.8f);
            button.name = "SwitchUsableStateDropdownPanel";
            button.Init();
            button.SetTitle("Set Usable State");
            button.ClearOptions();
            button.AddOption("Do Nothing", true);
            button.AddOption("Usable", false);
            button.AddOption("Unusable", false);
            button.AddOption("Toggle", false);
            button.onClick += (option) => OnSwitchUsableStateDropdownChanged();

            switchUsableStateButton = button;
            button.gameObject.SetActive(true);
        }
        // -----------------------------------------
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
            GameObject labelTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Label");

            GameObject titleLabel = Instantiate(labelTemplate, flameTrapObjectsSettings.transform);
            titleLabel.name = "TitleLabel";
            titleLabel.transform.localScale = Vector3.one;

            Destroy(titleLabel.GetComponent<UILocalize>());

            UILabel label = titleLabel.GetComponent<UILabel>();
            label.pivot = UIWidget.Pivot.Center;
            label.alignment = NGUIText.Alignment.Center;
            label.height = 40;
            label.width = 700;
            label.fontSize = 35;
            label.text = "FLAME TRAP OPTIONS";

            // Change the label position AFTER changing the pivot.
            titleLabel.transform.localPosition = new Vector3(0f, 40f, 0f);
        }
        void CreateFlameTrapStateDropdown()
        {
            UIButtonMultiple button = NGUI_Utils.CreateButtonMultiple(flameTrapObjectsSettings.transform, new Vector3(0, -10), Vector3.one * 0.8f);
            button.Init();
            button.SetTitle("Flame State");
            button.ClearOptions();
            button.AddOption("Do Nothing", true);
            button.AddOption("Activate", false);
            button.AddOption("Deactivate", false);
            button.AddOption("Toggle State", false);
            button.onClick += (option) => OnFlameTrapStateDropdownChanged();

            flameTrapStateButton = button;
            button.gameObject.SetActive(true);
        }
        // -----------------------------------------
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
            GameObject toggleObj = NGUI_Utils.CreateToggle(screenObjectsSettings.transform, new Vector3(-380, -10), new Vector3Int(300, 48, 0), "Change Color Type");
            toggleObj.name = "ChangeColorTypeToggle";
            changeScreenColorTypeToggle = toggleObj.GetComponent<UIToggle>();
            changeScreenColorTypeToggle.onChange.Clear();
            changeScreenColorTypeToggle.onChange.Add(new EventDelegate(this, nameof(OnChangeScreenColorTypeToggleChanged)));
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
            GameObject toggleObj = NGUI_Utils.CreateToggle(screenObjectsSettings.transform, new Vector3(-180, -65), new Vector3Int(300, 48, 0), "Change Text");
            toggleObj.name = "ChangeTextToggle";
            changeScreenTextToggle = toggleObj.GetComponent<UIToggle>();
            changeScreenTextToggle.onChange.Clear();
            changeScreenTextToggle.onChange.Add(new EventDelegate(this, nameof(OnChangeScreenTextToggleChanged)));
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
        // -----------------------------------------
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
            UIButtonMultiple button = NGUI_Utils.CreateButtonMultiple(doorObjectsSettings.transform, new Vector3(0, -10), Vector3.one * 0.8f);
            button.name = "DoorStateButton";
            button.Init();
            button.SetTitle("Set Door State");
            button.ClearOptions();
            button.AddOption("Do Nothing", true);
            button.AddOption("Closed", false);
            button.AddOption("Closed Fast", false);
            button.AddOption("Open", false);
            button.AddOption("Toggle", false);
            button.onClick += (option) => OnDoorStateButtonChanged();
            button.SetTooltip("EventsDoorStateTooltip");

            setDoorStateButton = button;
            button.gameObject.SetActive(true);
        }
        #endregion


        #region Logic For Objects UI Options
        void OnSpawnOptionsDropdownChanged()
        {
            currentSelectedEvent.spawn = (LE_Event.SpawnState)spawnOptionsDropdown.currentlySelectedID;
        }
        void OnColliderStateDropdownChanged()
        {
            currentSelectedEvent.colliderState = (LE_Event.ColliderState)colliderStateDropdown.currentlySelectedID;
        }
        // -----------------------------------------
        void OnSawStateDropdownChanged()
        {
            currentSelectedEvent.sawState = (LE_Event.SawState)sawStateButton.currentSelectedID;
        }
        // -----------------------------------------
        void OnZeroGToggleChanged()
        {
            currentSelectedEvent.enableOrDisableZeroG = zeroGToggle.isChecked;
            // Both toggles can't be enabled!
            if (zeroGToggle.isChecked && invertGravityToggle.isChecked)
            {
                invertGravityToggle.Set(false);
                OnInvertGravityToggleChanged();
            }
        }
        void OnInvertGravityToggleChanged()
        {
            currentSelectedEvent.invertGravity = invertGravityToggle.isChecked;
            // Both toggles can't be enabled!
            if (invertGravityToggle.isChecked && zeroGToggle.isChecked)
            {
                zeroGToggle.Set(false);
                OnZeroGToggleChanged();
            }
        }
        // -----------------------------------------
        void OnRespawnCubeChanged()
        {
            currentSelectedEvent.respawnCube = respawnCubeToggle.isChecked;
        }
        // -----------------------------------------
        void OnLaserStateDropdownChanged()
        {
            currentSelectedEvent.laserState = (LE_Event.LaserState)laserStateButton.currentSelectedID;
        }
        // -----------------------------------------
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
        // -----------------------------------------
        void OnCeilingLightStateDropdownChanged()
        {
            currentSelectedEvent.ceilingLightState = (LE_Event.CeilingLightState)ceilingLightStateButton.currentSelectedID;
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
        // -----------------------------------------
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
        // -----------------------------------------
        void OnSwitchStateDropdownChanged()
        {
            currentSelectedEvent.switchState = (LE_Event.SwitchState)switchStateButton.currentSelectedID;

            executeSwitchActionsToggle.gameObject.SetActive(currentSelectedEvent.switchState != LE_Event.SwitchState.Do_Nothing);
        }
        void OnExecuteSwitchActionsToggleChanged()
        {
            currentSelectedEvent.executeSwitchActions = executeSwitchActionsToggle.isChecked;
        }
        void OnSwitchUsableStateDropdownChanged()
        {
            currentSelectedEvent.switchUsableState = (LE_Event.SwitchUsableState)switchUsableStateButton.currentSelectedID;
        }
        // -----------------------------------------
        void OnFlameTrapStateDropdownChanged()
        {
            currentSelectedEvent.flameTrapState = (LE_Event.FlameTrapState)flameTrapStateButton.currentSelectedID;
        }
        // -----------------------------------------
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
        // -----------------------------------------
        void OnDoorStateButtonChanged()
        {
            currentSelectedEvent.doorState = (LE_Event.DoorState)setDoorStateButton.currentSelectedID;
        }
        #endregion

        public void ShowEventsPage(LE_Object targetObj)
        {
            if (targetObj.GetAvailableEventsIDs().Count <= 0)
            {
                Logger.Error("Requested to show Events Panel but the target object has NO Events List. IT'S NOT COMPATIBLE!");
                return;
            }
            this.targetObj = targetObj;

            // Change the title of the panel.
            eventsWindowTitle.text = "Events for " + targetObj.objectFullNameWithID;
            
            EditorController.Instance.SetCurrentEditorState(EditorState.PAUSED); // Just to stop camera movement and such.
            EditorUIManager.Instance.SetEditorUIContext(EditorUIContext.EVENTS_PANEL);

            SetupTopButtons();
            FirstEventsListBtnClick(false);
            // CreateEventsList();
        }
        public void HideEventsPage()
        {
            targetObj.TriggerAction("OnEventsTabClose");

            EditorController.Instance.SetCurrentEditorState(EditorState.NORMAL);
            EditorUIManager.Instance.SetEditorUIContext(EditorUIContext.NORMAL);

            OnEventSelect(null);
        }

        List<LE_Event> GetEventsList()
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
        public int eventTypeID;
        public int eventID;

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
            property.SetValue(this, property.GetValue(toCopy));
        }
    }

    public bool isValid { get; set; } = false;

    // Yeah, why should I put a name to a freaking event? Dunno, may be useful :)
    public string eventName { get; set; } = "New Event";

    public bool isForPlayer { get; set; } = false;

    public string targetObjName { get; set; } = "";
    public LE_Object.ObjectType? targetObjType { get; set; } = null;
    public int targetObjID { get; set; } = 0;

    public enum SpawnState { Do_Nothing, Spawn, Despawn, Toggle }
    public SpawnState spawn { get; set; } = SpawnState.Toggle;
    public enum ColliderState { Do_Nothing, Enable, Disable, Toggle }
    public ColliderState colliderState { get; set; } = ColliderState.Do_Nothing;

    #region Saw Options
    public enum SawState { Do_Nothing, Activate, Deactivate, Toggle_State }
    public SawState sawState { get; set; } = SawState.Toggle_State;
    #endregion

    #region Player Options
    public bool enableOrDisableZeroG { get; set; } = false;
    public bool invertGravity { get; set; } = false;
    #endregion

    #region Cube Options
    public bool respawnCube { get; set; } = false;
    #endregion

    #region Laser Options
    public enum LaserState { Do_Nothing, Activate, Deactivate, Toggle_State }
    public LaserState laserState { get; set; } = LaserState.Toggle_State;
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

    #region Switch Options
    public enum SwitchState { Do_Nothing, Activated, Deactivated, Toggle }
    public SwitchState switchState { get; set; } = SwitchState.Do_Nothing;
    public bool executeSwitchActions { get; set; } = true;
    public enum SwitchUsableState { Do_Nothing, Usable, Unusable, Toggle }
    public SwitchUsableState switchUsableState { get; set; } = SwitchUsableState.Do_Nothing;
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
    public enum DoorState { Do_Nothing, Closed, ClosedFast, Open, Toggle }
    public DoorState doorState { get; set; }
    #endregion
}