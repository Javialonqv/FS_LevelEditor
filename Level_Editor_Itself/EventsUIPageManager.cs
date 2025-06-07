using FS_LevelEditor.UI_Related;
using Il2Cpp;
using Il2CppDiscord;
using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Il2CppSystem.Globalization.CultureInfo;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class EventsUIPageManager : MonoBehaviour
    {
        public static EventsUIPageManager Instance { get; private set; }

        GameObject eventsPanel;
        GameObject eventsButtonsParent;
        GameObject occluder;
        GameObject previousEventPageButton, nextEventPageButton;
        GameObject currentEventPageLabel;
        GameObject noEventsLabel;

        GameObject onActivatedButton;
        GameObject onDeactivatedButton;
        GameObject onChangeButton;

        GameObject eventsListBg;
        List<GameObject> eventsGridList = new List<GameObject>();
        int currentEventsGrid = 0;

        /// <summary>
        /// Contains all of the options of an event, including the target object name field.
        /// </summary>
        GameObject eventSettingsPanel;
        UIInput targetObjInputField;
        /// <summary>
        /// Contains all of the options of an event, EXCEPT the target object name field.
        /// </summary>
        GameObject eventOptionsParent;
        GameObject defaultObjectsSettings;
        UIDropdownPatcher setActiveDropdown;
        //-----------------------------------
        GameObject sawObjectsSettings;
        UIDropdownPatcher sawStateDropdown;
        //-----------------------------------
        GameObject playerSettings;
        UIToggle zeroGToggle;
        UIToggle invertGravityToggle;
        //-----------------------------------
        GameObject cubeObjectsSettings;
        UIToggle respawnCubeToggle;
        //-----------------------------------
        GameObject laserObjectsSettings;
        UIDropdownPatcher laserStateDropdown;
        //-----------------------------------
        GameObject lightObjectsSettings;
        UIToggle changeLightColorToggle;
        UILabel newLightColorTitleLabel;
        UIInput newLightColorInputField;

        enum CurrentEventType { OnEnable, OnDisable, OnChange }
        CurrentEventType currentEventType;
        LE_Event currentSelectedEvent;

        public bool isShowingPage;

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

                // The event page buttons are created inside of the CreateEventsList() function, but only once.

                Instance.CreateEventSettingsPanelAndOptionsParent();
                Instance.CreateTargetObjectINSTRUCTIONLabel();
                Instance.CreateTargetObjectInputField();

                Instance.CreateDefaultObjectSettings();
                Instance.CreateSawObjectSettings();
                Instance.CreatePlayerSettings();
                Instance.CreateCubeObjectSettings();
                Instance.CreateLaserObjectSettings();
                Instance.CreateLightObjectSettings();

                Instance.CreateDetails();
            }
        }

        // Method copied from LE_MenuUIManager xD
        void CreateEventsPanel()
        {
            // Get the Options menu and create a copy.
            GameObject originalOptionsMenu = GameObject.Find("MainMenu/Camera/Holder/Options");
            eventsPanel = GameObject.Instantiate(originalOptionsMenu, EditorUIManager.Instance.editorUIParent.transform);

            // Change the name of the copy.
            eventsPanel.name = "EventsPanel";

            eventsPanel.transform.GetChildWithName("Window").transform.localPosition = Vector3.zero;
            eventsPanel.transform.GetChild(2).localPosition = new Vector3(0f, 386.4f, 0f);

            // Remove the OptionsController and UILocalize components so I can change the title of the panel. Also the TweenAlpha since it won't be needed.
            eventsPanel.RemoveComponent<OptionsController>();
            eventsPanel.RemoveComponent<TweenAlpha>();
            eventsPanel.transform.GetChild(2).gameObject.RemoveComponent<UILocalize>();

            // Change the title properties of the panel.
            eventsPanel.transform.GetChild(2).transform.localPosition = new Vector3(0, 387, 0);
            eventsPanel.transform.GetChild(2).GetComponent<UILabel>().width = 1650;
            eventsPanel.transform.GetChild(2).GetComponent<UILabel>().height = 50;
            eventsPanel.transform.GetChild(2).GetComponent<UILabel>().text = "Events";

            // Destroy the tabs and disable everything inside of the Game_Options object.
            GameObject.Destroy(eventsPanel.GetChildWithName("Tabs"));
            eventsPanel.GetChildWithName("Game_Options").SetActive(true);
            eventsButtonsParent = eventsPanel.GetChildAt("Game_Options/Buttons");
            eventsButtonsParent.DisableAllChildren();

            // Disable the damn lines.
            eventsPanel.GetChildAt("Game_Options/HorizontalLine").SetActive(false);
            eventsPanel.GetChildAt("Game_Options/VerticalLine").SetActive(false);

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
            eventsPanel.GetChildWithName("Window").GetComponent<UISprite>().alpha = 1f;

            // Add a collider so the user can't interact with the other objects.
            eventsPanel.AddComponent<BoxCollider>().size = new Vector3(100000f, 100000f, 1f);

            occluder = Instantiate(GameObject.Find("MainMenu/Camera/Holder/Occluder"), eventsPanel.transform);
            occluder.SetActive(true);
        }
        void CreateTopButtons()
        {
            onActivatedButton = NGUI_Utils.CreateButton(eventsPanel.transform, new Vector3(-500f, 300f, 0f), new Vector3Int(480, 55, 0), "On Activated");
            onActivatedButton.name = "OnActivatedButton";
            onActivatedButton.GetComponent<UISprite>().depth = 1;
            EventDelegate onActivatedDelegate = NGUI_Utils.CreateEvenDelegate(this, nameof(OnEnableBtnClick),
                NGUI_Utils.CreateEventDelegateParamter(this, "playSound", true));
            onActivatedButton.GetComponent<UIButton>().onClick.Add(onActivatedDelegate);
            onActivatedButton.GetComponent<UIButtonScale>().hover = Vector3.one * 1.05f;
            onActivatedButton.GetComponent<UIButtonScale>().pressed = Vector3.one * 0.95f;

            onDeactivatedButton = NGUI_Utils.CreateButton(eventsPanel.transform, new Vector3(0f, 300f, 0f), new Vector3Int(480, 55, 0), "On Deactivated");
            onDeactivatedButton.name = "OnDeactivatedButton";
            onDeactivatedButton.GetComponent<UISprite>().depth = 1;
            onDeactivatedButton.GetComponent<UIButton>().onClick.Add(new EventDelegate(this, nameof(OnDisableBtnClick)));
            onDeactivatedButton.GetComponent<UIButtonScale>().hover = Vector3.one * 1.05f;
            onDeactivatedButton.GetComponent<UIButtonScale>().pressed = Vector3.one * 0.95f;

            onChangeButton = NGUI_Utils.CreateButton(eventsPanel.transform, new Vector3(500f, 300f, 0f), new Vector3Int(480, 55, 0), "On Change");
            onChangeButton.name = "OnChangeButton";
            onChangeButton.GetComponent<UISprite>().depth = 1;
            onChangeButton.GetComponent<UIButton>().onClick.Add(new EventDelegate(this, nameof(OnChangeBtnClick)));
            onChangeButton.GetComponent<UIButtonScale>().hover = Vector3.one * 1.05f;
            onChangeButton.GetComponent<UIButtonScale>().pressed = Vector3.one * 0.95f;
        }
        void CreateEventsListBackground()
        {
            eventsListBg = new GameObject("EventsList");
            eventsListBg.transform.parent = eventsPanel.transform;
            eventsListBg.transform.localScale = Vector3.one;
            eventsListBg.layer = LayerMask.NameToLayer("2D GUI");

            UISprite eventsBgSprite = eventsListBg.AddComponent<UISprite>();
            eventsBgSprite.transform.localPosition = new Vector3(-400f, -90f, 0f);
            eventsBgSprite.atlas = occluder.GetComponent<UISprite>().atlas;
            eventsBgSprite.spriteName = "Square";
            eventsBgSprite.depth = 1;
            eventsBgSprite.color = new Color(0.0509f, 0.3333f, 0.3764f);
            eventsBgSprite.width = 800;
            eventsBgSprite.height = 540;
        }
        void CreateDetails()
        {
            GameObject horizontalLine = Instantiate(eventsPanel.GetChildAt("Game_Options/HorizontalLine"), eventsPanel.transform);
            horizontalLine.GetComponent<UISprite>().pivot = UIWidget.Pivot.Center;
            horizontalLine.transform.localPosition = new Vector3(0f, 250f, 0f);
            horizontalLine.GetComponent<UISprite>().width = 1600;
            horizontalLine.SetActive(true);

            GameObject verticalLine = Instantiate(eventsPanel.GetChildAt("Game_Options/VerticalLine"), eventsPanel.transform);
            verticalLine.GetComponent<UISprite>().pivot = UIWidget.Pivot.Center;
            verticalLine.transform.localPosition = new Vector3(70f, -100f, 0f);
            verticalLine.GetComponent<UISprite>().height = 580;
            verticalLine.SetActive(true);

            GameObject horizontalLine2 = Instantiate(eventsPanel.GetChildAt("Game_Options/HorizontalLine"), eventSettingsPanel.transform);
            horizontalLine2.GetComponent<UISprite>().pivot = UIWidget.Pivot.Center;
            horizontalLine2.transform.localPosition = new Vector3(0f, 170f, 0f);
            horizontalLine2.GetComponent<UISprite>().width = 700;
            horizontalLine2.SetActive(true);
        }
        void CreateAddEventButton()
        {
            GameObject addEventButton = NGUI_Utils.CreateButton(eventsPanel.transform, new Vector3(-400f, -388f, 0f), new Vector3Int(800, 50, 0), "+ Add New Event");
            addEventButton.name = "AddEventButton";
            addEventButton.GetComponent<UISprite>().depth = 1;
            addEventButton.GetComponent<UIButtonScale>().hover = Vector3.one;
            addEventButton.GetComponent<UIButtonScale>().pressed = Vector3.one * 0.95f;
            addEventButton.GetComponent<UIButton>().onClick.Add(new EventDelegate(this, nameof(AddNewEvent)));
        }

        void CreateEventsList(int eventsPage)
        {
            GameObject btnTemplate = LE_MenuUIManager.Instance.leMenuPanel.GetChildAt("Controls_Options/Buttons/RemapControls");
            GameObject labelTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Label");

            // Create page buttons in case they don't exist yet.
            if (previousEventPageButton == null && nextEventPageButton == null)
            {
                CreatePreviousEventsPageButton();
                CreateNextEventsPageButton();
                CreateCurrentEventsPageLabel();
                CreateNoEventsLabel();
            }

            List<LE_Event> events = GetEventsList();

            // Destroy the whole grids, but skip the first four objects which are the previous & next event page button
            // AND the current page label AND the no events label.
            for (int i = 4; i < eventsListBg.transform.childCount; i++)
            {
                Destroy(eventsListBg.transform.GetChild(i).gameObject);
            }
            eventsGridList.Clear();

            GameObject currentGrid = null;
            for (int i = 0; i < events.Count; i++)
            {
                if (i % 6 == 0 || i == 0) // Idk bro, this is literally copied from the OST mod LOL.
                {
                    // Create a grid.
                    currentGrid = new GameObject($"Grid {(int)(i / 6)}");
                    currentGrid.transform.parent = eventsListBg.transform;
                    currentGrid.transform.localPosition = new Vector3(0f, 220f, 0f);
                    currentGrid.transform.localScale = Vector3.one;

                    // Add the UIGrid component, ofc.
                    UIGrid grid = currentGrid.AddComponent<UIGrid>();
                    grid.arrangement = UIGrid.Arrangement.Vertical;
                    grid.cellWidth = 780f;
                    grid.cellHeight = 80f;

                    currentGrid.SetActive(false);

                    eventsGridList.Add(currentGrid);
                }

                // Create the event button PARENT, since inside of it are the button, the name label, and delete btn.
                GameObject eventButtonParent = new GameObject($"Event {i}");
                eventButtonParent.transform.parent = currentGrid.transform;
                eventButtonParent.transform.localScale = Vector3.one;

                // Create the EVENT BUTTON itself...
                GameObject eventButton = Instantiate(btnTemplate, eventButtonParent.transform);
                eventButton.name = "Button";
                eventButton.transform.localPosition = Vector3.zero;
                eventButton.layer = LayerMask.NameToLayer("2D GUI");

                // Remove innecesary components.
                Destroy(eventButton.GetComponent<ButtonController>());
                Destroy(eventButton.GetComponent<OptionsButton>());
                // Remove the SECOND UIButtonColor component, and then I ask, why did Charles add TWO UIButtonColor to the buttons
                // if they target to the same object?
                Destroy(eventButton.GetComponents<UIButtonColor>()[1]);

                // Set the sprite's size, as well in the BoxCollider.
                UISprite sprite = eventButton.GetComponent<UISprite>();
                sprite.width = 780;
                sprite.height = 70;
                sprite.depth = 2;
                BoxCollider collider = eventButton.GetComponent<BoxCollider>();
                collider.size = new Vector3(780f, 100f);

                // Change button scale options, because with the default values it looks too big.
                UIButtonScale scale = eventButton.GetComponent<UIButtonScale>();
                scale.mScale = Vector3.one;
                scale.hover = Vector3.one;
                scale.pressed = Vector3.one * 0.95f;

                // Destroy the "original" label, since it's going to be replaced with the other name label.
                Destroy(eventButton.GetChildAt("Background/Label"));

                UIButton button = eventButton.GetComponent<UIButton>();
                button.onClick.Clear();
                EventDelegate.Parameter buttonParm = NGUI_Utils.CreateEventDelegateParamter(this, "selectedID", i);
                EventDelegate buttonDelegate = NGUI_Utils.CreateEvenDelegate(this, nameof(OnEventSelect), buttonParm);
                button.onClick.Add(buttonDelegate);

                if (currentSelectedEvent == events[i])
                {
                    button.defaultColor = new Color(0f, 0.6f, 0f, 1f);
                }
                else
                {
                    button.defaultColor = new Color(0.218f, 0.6464f, 0.6509f, 1f);
                }

                #region Delete Button
                // Create the button and set its name and positon.
                GameObject deleteBtn = Instantiate(btnTemplate, eventButtonParent.transform);
                deleteBtn.name = "DeleteBtn";
                deleteBtn.transform.localPosition = new Vector3(350f, 0f, 0f);

                // Destroy some unnecesary components and the label, since we're going to add a SPRITE.
                Destroy(deleteBtn.GetComponent<ButtonController>());
                Destroy(deleteBtn.GetComponent<OptionsButton>());
                Destroy(deleteBtn.GetChildAt("Background/Label"));

                // Adjust the button sprite and create the BoxCollider as well.
                UISprite deleteSprite = deleteBtn.GetComponent<UISprite>();
                deleteSprite.width = 60;
                deleteSprite.height = 60;
                deleteSprite.depth = 3;
                BoxCollider deleteCollider = deleteBtn.GetComponent<BoxCollider>();
                deleteCollider.size = new Vector3(60f, 60f, 0f);

                // Adjust the button color with red color variants.
                UIButtonColor deleteButtonColor = deleteBtn.GetComponent<UIButtonColor>();
                deleteButtonColor.defaultColor = new Color(0.8f, 0f, 0f, 1f);
                deleteButtonColor.hover = new Color(1f, 0f, 0f, 1f);
                deleteButtonColor.pressed = new Color(0.5f, 0f, 0f, 1f);

                // Create another sprite "inside" of the button one.
                UISprite trashSprite = deleteBtn.GetChildWithName("Background").GetComponent<UISprite>();
                trashSprite.name = "Trash";
                trashSprite.SetExternalSprite("Trash");
                trashSprite.width = 30;
                trashSprite.height = 40;
                trashSprite.depth = 4;
                trashSprite.color = Color.white;
                trashSprite.transform.localPosition = Vector3.zero;
                trashSprite.enabled = true;

                UIButton deleteBtnScript = deleteBtn.GetComponent<UIButton>();
                EventDelegate.Parameter deleteBtnParm1 = NGUI_Utils.CreateEventDelegateParamter(this, "eventID", i);
                EventDelegate deleteBtnDelegate = NGUI_Utils.CreateEvenDelegate(this, nameof(DeleteEvent), deleteBtnParm1);
                deleteBtnScript.onClick.Add(deleteBtnDelegate);
                #endregion

                #region Name Input Field
                GameObject nameObj = new GameObject("NameInputField");
                nameObj.transform.parent = eventButtonParent.transform;
                nameObj.transform.localScale = Vector3.one;
                nameObj.transform.localPosition = new Vector3(-150f, 0f, 0f);
                nameObj.layer = LayerMask.NameToLayer("2D GUI");

                // This is just, to make a small outline in the name label "box".
                UISprite nameBGSprite = nameObj.AddComponent<UISprite>();
                nameBGSprite.atlas = occluder.GetComponent<UISprite>().atlas;
                nameBGSprite.spriteName = "Square";
                nameBGSprite.width = 455;
                nameBGSprite.height = 55;
                nameBGSprite.color = Color.black;
                nameBGSprite.depth = 3;

                // This is the box where the name label is.
                UISprite nameSprite = nameObj.AddComponent<UISprite>();
                nameSprite.atlas = occluder.GetComponent<UISprite>().atlas;
                nameSprite.spriteName = "Square";
                nameSprite.width = 450;
                nameSprite.height = 50;
                nameSprite.color = new Color(0.0509f, 0.3333f, 0.3764f);
                nameSprite.depth = 4;

                // The label itself.
                UILabel nameLabel = nameObj.AddComponent<UILabel>();
                nameLabel.font = labelTemplate.GetComponent<UILabel>().font;
                nameLabel.fontSize = 27;
                nameLabel.width = 440; // Smaller width so the text doesn't look like it's outside of the box sprite.
                nameLabel.height = 50;
                nameLabel.alignment = NGUIText.Alignment.Left;
                nameLabel.depth = 5;

                UIInput nameInputScript = nameObj.AddComponent<UIInput>();
                nameInputScript.label = nameLabel;
                nameInputScript.text = events[i].eventName;
                EventDelegate.Parameter onNameInputSubmitParm1 = NGUI_Utils.CreateEventDelegateParamter(this, "eventID", i);
                EventDelegate.Parameter onNameInputSubmitParm2 = NGUI_Utils.CreateEventDelegateParamter(this, "newName", nameInputScript);
                EventDelegate onNameInputSubmit =
                    NGUI_Utils.CreateEvenDelegate(this, nameof(RenameEvent), onNameInputSubmitParm1, onNameInputSubmitParm2);
                nameInputScript.onSubmit.Clear();
                nameInputScript.onSubmit.Add(onNameInputSubmit);

                // GOD BLESS OLD ME FOR CREATING THIS FIX!!
                nameInputScript.gameObject.AddComponent<UIInputSubmitFix>();

                BoxCollider nameInputCollider = nameObj.AddComponent<BoxCollider>();
                nameInputCollider.size = new Vector3(450f, 50f, 0f);
                #endregion
            }

            // Enable the right grid.
            if (eventsPage == int.MaxValue && eventsGridList.Count > 0)
            {
                eventsGridList.Last().SetActive(true);
                currentEventsGrid = eventsGridList.Count - 1;
            }
            else if (eventsGridList.Count > 0)
            {
                eventsGridList[eventsPage].SetActive(true);
                currentEventsGrid = eventsPage;
            }

            // Only enable the page buttons and the page label once they're are more than 1 grid (1 event page).
            previousEventPageButton.SetActive(eventsGridList.Count > 1);
            nextEventPageButton.SetActive(eventsGridList.Count > 1);
            currentEventPageLabel.SetActive(eventsGridList.Count > 1);

            // Enable the No Events Label in case there aren't any events...
            noEventsLabel.SetActive(eventsGridList.Count == 0);

            // Update the state of the page buttons and the page label in case now they're enabled.
            previousEventPageButton.GetComponent<UIButton>().isEnabled = currentEventsGrid > 0;
            nextEventPageButton.GetComponent<UIButton>().isEnabled = currentEventsGrid < eventsGridList.Count - 1;
            currentEventPageLabel.GetComponent<UILabel>().text = GetCurrentEventPageText();
        }
        void CreatePreviousEventsPageButton()
        {
            // Create the button.
            GameObject btnTemplate = LE_MenuUIManager.Instance.leMenuPanel.GetChildAt("Controls_Options/Buttons/RemapControls");
            previousEventPageButton = Instantiate(btnTemplate, eventsListBg.transform);
            previousEventPageButton.name = "PreviousEventsPageButton";
            previousEventPageButton.transform.localPosition = new Vector3(-430f, 0f, 0f);

            // Remove unnecesary components.
            GameObject.Destroy(previousEventPageButton.GetComponent<ButtonController>());
            GameObject.Destroy(previousEventPageButton.GetComponent<OptionsButton>());
            GameObject.Destroy(previousEventPageButton.GetComponent<FractalTooltip>());

            // Adjust the sprite and the collider as well.
            UISprite sprite = previousEventPageButton.GetComponent<UISprite>();
            sprite.width = 50;
            sprite.height = 50;
            sprite.depth = 1;
            BoxCollider collider = previousEventPageButton.GetComponent<BoxCollider>();
            collider.size = new Vector3(50f, 50f);

            // Adjust the label, removing the FUCKING UILocalize.
            GameObject.Destroy(previousEventPageButton.GetChildAt("Background/Label").GetComponent<UILocalize>());
            UILabel label = previousEventPageButton.GetChildAt("Background/Label").GetComponent<UILabel>();
            label.depth = 2;
            label.width = 60;
            label.height = 60;
            label.fontSize = 40;
            label.text = "<";

            // Set the button on click action.
            UIButton button = previousEventPageButton.GetComponent<UIButton>();
            button.onClick.Clear();
            button.onClick.Add(new EventDelegate(this, nameof(PreviousEventsPage)));
        }
        void CreateNextEventsPageButton()
        {
            // Create the button.
            GameObject btnTemplate = LE_MenuUIManager.Instance.leMenuPanel.GetChildAt("Controls_Options/Buttons/RemapControls");
            nextEventPageButton = Instantiate(btnTemplate, eventsListBg.transform);
            nextEventPageButton.name = "PreviousEventsPageButton";
            nextEventPageButton.transform.localPosition = new Vector3(430f, 0f, 0f);

            // Remove unnecesary components.
            GameObject.Destroy(nextEventPageButton.GetComponent<ButtonController>());
            GameObject.Destroy(nextEventPageButton.GetComponent<OptionsButton>());
            GameObject.Destroy(nextEventPageButton.GetComponent<FractalTooltip>());

            // Adjust the sprite and the collider as well.
            UISprite sprite = nextEventPageButton.GetComponent<UISprite>();
            sprite.width = 50;
            sprite.height = 50;
            sprite.depth = 1;
            BoxCollider collider = nextEventPageButton.GetComponent<BoxCollider>();
            collider.size = new Vector3(50f, 50f);

            // Adjust the label, removing the FUCKING UILocalize.
            GameObject.Destroy(nextEventPageButton.GetChildAt("Background/Label").GetComponent<UILocalize>());
            UILabel label = nextEventPageButton.GetChildAt("Background/Label").GetComponent<UILabel>();
            label.depth = 2;
            label.width = 60;
            label.height = 60;
            label.fontSize = 40;
            label.text = ">";

            // Set the button on click action.
            UIButton button = nextEventPageButton.GetComponent<UIButton>();
            button.onClick.Clear();
            button.onClick.Add(new EventDelegate(this, nameof(NextEventsPage)));
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
        }

        void RenameEvent(int eventID, UIInput inputRef)
        {
            // GetEventsList should return the same events list that when creating the events list, it should be fine :)
            LE_Event eventToRename = GetEventsList()[eventID];
            eventToRename.eventName = inputRef.text;

            Logger.Log("RENAMED " + eventID + " TO: " + inputRef.text);
        }
        void OnEnableBtnClick(bool playSound = true)
        {
            // This method is the only one with the playSound parm because it's the only one I wanna call when
            // opening the events windows with NO sound at all.
            if (playSound) Utilities.PlayFSUISound(Utilities.FS_UISound.INTERACTION_AVAILABLE);

            onActivatedButton.GetComponent<UIButton>().defaultColor = new Color(0f, 1f, 0f, 1f);
            onDeactivatedButton.GetComponent<UIButton>().defaultColor = new Color(0.218f, 0.6464f, 0.6509f, 1f);
            onChangeButton.GetComponent<UIButton>().defaultColor = new Color(0.218f, 0.6464f, 0.6509f, 1f);

            currentEventType = CurrentEventType.OnEnable;

            HideEventSettings();
            CreateEventsList(0);
        }
        void OnDisableBtnClick()
        {
            Utilities.PlayFSUISound(Utilities.FS_UISound.INTERACTION_AVAILABLE);

            onActivatedButton.GetComponent<UIButton>().defaultColor = new Color(0.218f, 0.6464f, 0.6509f, 1f);
            onDeactivatedButton.GetComponent<UIButton>().defaultColor = new Color(0f, 1f, 0f, 1f);
            onChangeButton.GetComponent<UIButton>().defaultColor = new Color(0.218f, 0.6464f, 0.6509f, 1f);

            currentEventType = CurrentEventType.OnDisable;

            HideEventSettings();
            CreateEventsList(0);
        }
        void OnChangeBtnClick()
        {
            Utilities.PlayFSUISound(Utilities.FS_UISound.INTERACTION_AVAILABLE);

            onActivatedButton.GetComponent<UIButton>().defaultColor = new Color(0.218f, 0.6464f, 0.6509f, 1f);
            onDeactivatedButton.GetComponent<UIButton>().defaultColor = new Color(0.218f, 0.6464f, 0.6509f, 1f);
            onChangeButton.GetComponent<UIButton>().defaultColor = new Color(0f, 1f, 0f, 1f);

            currentEventType = CurrentEventType.OnChange;

            HideEventSettings();
            CreateEventsList(0);
        }
        void AddNewEvent()
        {
            Utilities.PlayFSUISound(Utilities.FS_UISound.INTERACTION_UNAVAILABLE);

            switch (currentEventType)
            {
                case CurrentEventType.OnEnable:
                    ((List<LE_Event>)targetObj.properties["OnActivatedEvents"]).Add(new LE_Event());
                    break;

                case CurrentEventType.OnDisable:
                    ((List<LE_Event>)targetObj.properties["OnDeactivatedEvents"]).Add(new LE_Event());
                    break;

                case CurrentEventType.OnChange:
                    ((List<LE_Event>)targetObj.properties["OnChangeEvents"]).Add(new LE_Event());
                    break;
            }

            // The int max value will stand for "the last damn grid you find!"
            CreateEventsList(int.MaxValue);
        }
        void PreviousEventsPage()
        {
            if (currentEventsGrid <= 0) return;

            currentEventsGrid--;

            eventsGridList.ForEach(x => x.SetActive(false));
            eventsGridList[currentEventsGrid].SetActive(true);

            // Update the state of the page buttons AND the current events page label.
            previousEventPageButton.GetComponent<UIButton>().isEnabled = currentEventsGrid > 0;
            nextEventPageButton.GetComponent<UIButton>().isEnabled = currentEventsGrid < eventsGridList.Count - 1;
            currentEventPageLabel.GetComponent<UILabel>().text = GetCurrentEventPageText();
        }
        void NextEventsPage()
        {
            if (currentEventsGrid >= eventsGridList.Count - 1) return;

            currentEventsGrid++;

            eventsGridList.ForEach(x => x.SetActive(false));
            eventsGridList[currentEventsGrid].SetActive(true);

            // Update the state of the page buttons AND the current events page label.
            previousEventPageButton.GetComponent<UIButton>().isEnabled = currentEventsGrid > 0;
            nextEventPageButton.GetComponent<UIButton>().isEnabled = currentEventsGrid < eventsGridList.Count - 1;
            currentEventPageLabel.GetComponent<UILabel>().text = GetCurrentEventPageText();
        }
        string GetCurrentEventPageText()
        {
            return (currentEventsGrid + 1) + "/" + (eventsGridList.Count);
        }
        void DeleteEvent(int eventID)
        {
            HideEventSettings();
            GetEventsList().RemoveAt(eventID);
            CreateEventsList(int.MaxValue);
        }
        void OnEventSelect(int selectedID)
        {
            Utilities.PlayFSUISound(Utilities.FS_UISound.INTERACTION_UNAVAILABLE);

            // GetEventsList should return the same events list that when creating the events list, it should be fine :)
            // *Comment copied from RenameEvent() LOL.
            currentSelectedEvent = GetEventsList()[selectedID];
            ShowEventSettings();

            CreateEventsList(currentEventsGrid);
        }

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
            GameObject labelTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Label");

            GameObject targetObjField = NGUI_Utils.CreateInputField(eventSettingsPanel.transform, new Vector3(0f, 230f, 0f), new Vector3Int(500, 60, 0), 34,
                "", true, NGUIText.Alignment.Center);

            targetObjInputField = targetObjField.GetComponent<UIInput>();
            EventDelegate.Parameter inputScriptParm1 = NGUI_Utils.CreateEventDelegateParamter(this, "input", targetObjInputField);
            EventDelegate.Parameter inputScriptParm2 = NGUI_Utils.CreateEventDelegateParamter(this, "fieldSprite", targetObjField.GetComponent<UISprite>());
            EventDelegate inputScriptDelegate =
                NGUI_Utils.CreateEvenDelegate(this, nameof(OnTargetObjectFieldChanged), inputScriptParm1, inputScriptParm2);
            targetObjInputField.onChange.Add(inputScriptDelegate);
        }

        void ShowEventSettings()
        {
            targetObjInputField.text = currentSelectedEvent.targetObjName;

            setActiveDropdown.SelectOption((int)currentSelectedEvent.setActive);
            sawStateDropdown.SelectOption((int)currentSelectedEvent.sawState);
            zeroGToggle.Set(currentSelectedEvent.enableOrDisableZeroG);
            invertGravityToggle.Set(currentSelectedEvent.invertGravity);
            respawnCubeToggle.Set(currentSelectedEvent.respawnCube);
            laserStateDropdown.SelectOption((int)currentSelectedEvent.laserState);
            changeLightColorToggle.Set(currentSelectedEvent.changeLightColor);
            newLightColorTitleLabel.gameObject.SetActive(currentSelectedEvent.changeLightColor);
            newLightColorInputField.gameObject.SetActive(currentSelectedEvent.changeLightColor);
            newLightColorInputField.text = currentSelectedEvent.newLightColor;

            eventSettingsPanel.SetActive(true);
            eventOptionsParent.DisableAllChildren();
            OnTargetObjectFieldChanged(targetObjInputField, targetObjInputField.GetComponent<UISprite>());
        }
        void HideEventSettings()
        {
            currentSelectedEvent = null;
            eventSettingsPanel.SetActive(false);
        }
        void OnTargetObjectFieldChanged(UIInput input, UISprite fieldSprite)
        {
            string inputText = input.text;
            LE_Object targetObj = null;
            bool objIsValid = false;

            #region Check if the object is valid
            if (inputText == "Player")
            {
                objIsValid = true;
            }
            else
            {
                targetObj = EditorController.Instance.currentInstantiatedObjects.FirstOrDefault(obj => obj.objectFullNameWithID == inputText);
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

                if (inputText != "Player") defaultObjectsSettings.SetActive(true);
                if (inputText == "Player")
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
            }
            else
            {
                fieldSprite.color = new Color(0.3215f, 0.2156f, 0.0588f, 0.9415f);
                eventOptionsParent.SetActive(false);
                eventOptionsParent.DisableAllChildren();
            }

            currentSelectedEvent.isValid = objIsValid;
            currentSelectedEvent.targetObjName = inputText;
        }

        void CreateDefaultObjectSettings()
        {
            defaultObjectsSettings = new GameObject("Default");
            defaultObjectsSettings.transform.parent = eventOptionsParent.transform;
            defaultObjectsSettings.transform.localPosition = Vector3.zero;
            defaultObjectsSettings.transform.localScale = Vector3.one;
            defaultObjectsSettings.SetActive(false);

            CreateSetActiveDropdown();
        }
        void CreateSetActiveDropdown()
        {
            GameObject setActiveDropdownPanel = Instantiate(eventsPanel.GetChildAt("Game_Options/Buttons/LanguagePanel"), defaultObjectsSettings.transform);
            setActiveDropdownPanel.name = "SetActiveDropdownPanel";
            setActiveDropdownPanel.transform.localPosition = new Vector3(0f, 105f, 0f);
            setActiveDropdownPanel.transform.localScale = Vector3.one * 0.8f;

            UIDropdownPatcher patcher = setActiveDropdownPanel.AddComponent<UIDropdownPatcher>();
            patcher.Init();
            patcher.SetTitle("Set Active");
            patcher.ClearOptions();
            patcher.AddOption("Do Nothing", true);
            patcher.AddOption("Enable", false);
            patcher.AddOption("Disable", false);
            patcher.AddOption("Toggle", false);

            patcher.ClearOnChangeOptions();
            patcher.AddOnChangeOption(new EventDelegate(this, nameof(OnSetActiveDropdownChanged)));

            setActiveDropdown = patcher;
            setActiveDropdownPanel.SetActive(true);
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
            GameObject sawStateDropdownPanel = Instantiate(eventsPanel.GetChildAt("Game_Options/Buttons/LanguagePanel"), sawObjectsSettings.transform);
            sawStateDropdownPanel.name = "SawStateDropdownPanel";
            sawStateDropdownPanel.transform.localPosition = new Vector3(0f, -50f, 0f);
            sawStateDropdownPanel.transform.localScale = Vector3.one * 0.8f;

            UIDropdownPatcher patcher = sawStateDropdownPanel.AddComponent<UIDropdownPatcher>();
            patcher.Init();
            patcher.SetTitle("Saw State");
            patcher.ClearOptions();
            patcher.AddOption("Do Nothing", true);
            patcher.AddOption("Activate", false);
            patcher.AddOption("Deactivate", false);
            patcher.AddOption("Toggle State", false);

            patcher.ClearOnChangeOptions();
            patcher.AddOnChangeOption(new EventDelegate(this, nameof(OnSawStateDropdownChanged)));

            sawStateDropdown = patcher;
            sawStateDropdownPanel.SetActive(true);
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
            GameObject laserStateDropdownPanel = Instantiate(eventsPanel.GetChildAt("Game_Options/Buttons/LanguagePanel"), laserObjectsSettings.transform);
            laserStateDropdownPanel.name = "LaserStateDropdownPanel";
            laserStateDropdownPanel.transform.localPosition = new Vector3(0f, -50f, 0f);
            laserStateDropdownPanel.transform.localScale = Vector3.one * 0.8f;

            UIDropdownPatcher patcher = laserStateDropdownPanel.AddComponent<UIDropdownPatcher>();
            patcher.Init();
            patcher.SetTitle("Laser State");
            patcher.ClearOptions();
            patcher.AddOption("Do Nothing", true);
            patcher.AddOption("Activate", false);
            patcher.AddOption("Deactivate", false);
            patcher.AddOption("Toggle State", false);

            patcher.ClearOnChangeOptions();
            patcher.AddOnChangeOption(new EventDelegate(this, nameof(OnLaserStateDropdownChanged)));

            laserStateDropdown = patcher;
            laserStateDropdownPanel.SetActive(true);
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
            GameObject inputField = NGUI_Utils.CreateInputField(lightObjectsSettings.transform, new Vector3(270f, -30f, 0f),
                new Vector3Int(250, 40, 1), 27, "FFFFFF");
            inputField.name = "NewLightColorInputField";
            inputField.GetComponent<UIInput>().characterLimit = 6;
            inputField.GetComponent<UIInput>().onChange.Add(new EventDelegate(this, nameof(OnNewLightColorInputFieldChanged)));

            newLightColorInputField = inputField.GetComponent<UIInput>();
        }


        void OnSetActiveDropdownChanged()
        {
            currentSelectedEvent.setActive = (LE_Event.SetActiveState)setActiveDropdown.currentlySelectedID;
        }
        // -----------------------------------------
        void OnSawStateDropdownChanged()
        {
            currentSelectedEvent.sawState = (LE_Event.SawState)sawStateDropdown.currentlySelectedID;
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
            currentSelectedEvent.laserState = (LE_Event.LaserState)laserStateDropdown.currentlySelectedID;
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
            Color? outputColor = Utilities.HexToColor(newLightColorInputField.text, false, null);
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

        public void ShowEventsPage(LE_Object targetObj)
        {
            this.targetObj = targetObj;

            // Change the title of the panel.
            eventsPanel.transform.GetChild(2).GetComponent<UILabel>().text = "Events for " + targetObj.objectFullNameWithID;

            eventsPanel.SetActive(true);
            eventsPanel.GetComponent<TweenScale>().PlayIgnoringTimeScale(false);
            Utilities.PlayFSUISound(Utilities.FS_UISound.POPUP_UI_SHOW);

            isShowingPage = true;

            OnEnableBtnClick(false);
            // CreateEventsList();
        }
        public void HideEventsPage()
        {
            targetObj.TriggerAction("OnEventsTabClose");

            eventsPanel.GetComponent<TweenScale>().PlayIgnoringTimeScale(true);
            Utilities.PlayFSUISound(Utilities.FS_UISound.POPUP_UI_HIDE);

            eventsPanel.SetActive(true);
            GameObject.Find("MainMenu/Camera/Holder/Main").SetActive(false);
            isShowingPage = false;

            HideEventSettings();
        }

        List<LE_Event> GetEventsList()
        {
            switch (currentEventType)
            {
                case CurrentEventType.OnEnable:
                    return (List<LE_Event>)targetObj.GetProperty("OnActivatedEvents");

                case CurrentEventType.OnDisable:
                    return (List<LE_Event>)targetObj.GetProperty("OnDeactivatedEvents");

                case CurrentEventType.OnChange:
                    return (List<LE_Event>)targetObj.GetProperty("OnChangeEvents");
            }

            return null;
        }
    }
}

public class LE_Event
{
    public bool isValid { get; set; } = false;

    // Yeah, why should I put a name to a freaking event? Dunno, may be useful :)
    public string eventName { get; set; } = "New Event";
    public string targetObjName { get; set; } = "";

    public enum SetActiveState { Do_Nothing, Enable, Disable, Toggle }
    public SetActiveState setActive { get; set; } = SetActiveState.Do_Nothing;

    #region Saw Options
    public enum SawState { Do_Nothing, Activate, Deactivate, Toggle_State }
    public SawState sawState { get; set; } = SawState.Do_Nothing;
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
    public LaserState laserState { get; set; } = LaserState.Do_Nothing;
    #endregion

    #region Light Options
    public bool changeLightColor { get; set; } = false;
    public string newLightColor { get; set; } = "FFFFFF";
    #endregion
}