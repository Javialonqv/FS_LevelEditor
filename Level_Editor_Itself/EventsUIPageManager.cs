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

        GameObject onEnableButton;
        GameObject onDisableButton;
        GameObject onChangeButton;

        GameObject eventsListBg;
        List<GameObject> eventsGridList = new List<GameObject>();
        int currentEventsGrid = 0;

        enum CurrentEventType { OnEnable, OnDisable, OnChange }
        CurrentEventType currentEventType;

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
                Instance.CreateDetails();
                Instance.CreateAddEventButton();

                // The event page buttons are created inside of the CreateEventsList() function, but only once.
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

            // Change the title of the panel.
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
            UIPanel panel = eventsPanel.GetComponent<UIPanel>();
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
            onEnableButton = NGUI_Utils.CreateButton(eventsPanel.transform, new Vector3(-500f, 300f, 0f), new Vector3Int(480, 55, 0), "On Enable");
            onEnableButton.name = "OnEnableButton";
            onEnableButton.GetComponent<UISprite>().depth = 1;
            onEnableButton.GetComponent<UIButton>().onClick.Add(new EventDelegate(this, nameof(OnEnableBtnClick)));
            onEnableButton.GetComponent<UIButtonScale>().hover = Vector3.one * 1.05f;
            onEnableButton.GetComponent<UIButtonScale>().pressed = Vector3.one * 0.95f;

            onDisableButton = NGUI_Utils.CreateButton(eventsPanel.transform, new Vector3(0f, 300f, 0f), new Vector3Int(480, 55, 0), "On Disable");
            onDisableButton.name = "OnDisableButton";
            onDisableButton.GetComponent<UISprite>().depth = 1;
            onDisableButton.GetComponent<UIButton>().onClick.Add(new EventDelegate(this, nameof(OnDisableBtnClick)));
            onDisableButton.GetComponent<UIButtonScale>().hover = Vector3.one * 1.05f;
            onDisableButton.GetComponent<UIButtonScale>().pressed = Vector3.one * 0.95f;

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

        void CreateEventsList()
        {
            GameObject btnTemplate = LE_MenuUIManager.Instance.leMenuPanel.GetChildAt("Controls_Options/Buttons/RemapControls");
            GameObject labelTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Label");

            // Create page buttons in case they don't exist yet.
            if (previousEventPageButton == null && nextEventPageButton == null)
            {
                CreatePreviousEventsPageButton();
                CreateNextEventsPageButton();
            }

            List<LE_Event> events = GetEventsList();

            // Destroy the whole grids, but skip the first two objects which are the previous & next event page buttons.
            for (int i = 2; i < eventsListBg.transform.childCount; i++)
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

                    if (i != 0) currentGrid.SetActive(false);

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

                //#region Delete Button
                //GameObject deleteObj = new GameObject("DeleteButton");
                //deleteObj.transform.parent = eventButton.transform;
                //deleteObj.transform.localScale = Vector3.one;
                //deleteObj.transform.localPosition = new Vector3(350f, 0f, 0f);

                //UISprite deleteSprite = deleteObj.AddComponent<UISprite>();
                //deleteSprite.atlas = occluder.GetComponent<UISprite>().atlas;
                //deleteSprite.spriteName = "Square";
                //deleteSprite.width = 50;
                //deleteSprite.height = 50;
                //deleteSprite.color = Color.red;
                //deleteSprite.depth = 3;
                //#endregion

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

            // Only enable the page buttons once they're are more than 1 grid (1 event page).
            previousEventPageButton.SetActive(eventsGridList.Count > 1);
            nextEventPageButton.SetActive(eventsGridList.Count > 1);

            // Update the state of the page buttons in case now they're enabled.
            previousEventPageButton.GetComponent<UIButton>().isEnabled = currentEventsGrid > 0;
            nextEventPageButton.GetComponent<UIButton>().isEnabled = currentEventsGrid < eventsGridList.Count - 1;
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

        void RenameEvent(int eventID, UIInput inputRef)
        {
            // GetEventsList should return the same events list that when creating the events list, it should be fine :)
            LE_Event eventToRename = GetEventsList()[eventID];
            eventToRename.eventName = inputRef.text;

            Logger.DebugLog("RENAMED " + eventID + " TO: " + inputRef.text);
        }
        void OnEnableBtnClick()
        {
            onEnableButton.GetComponent<UIButton>().defaultColor = new Color(0f, 1f, 0f, 1f);
            onDisableButton.GetComponent<UIButton>().defaultColor = new Color(0.218f, 0.6464f, 0.6509f, 1f);
            onChangeButton.GetComponent<UIButton>().defaultColor = new Color(0.218f, 0.6464f, 0.6509f, 1f);

            currentEventType = CurrentEventType.OnEnable;

            CreateEventsList();
        }
        void OnDisableBtnClick()
        {
            onEnableButton.GetComponent<UIButton>().defaultColor = new Color(0.218f, 0.6464f, 0.6509f, 1f);
            onDisableButton.GetComponent<UIButton>().defaultColor = new Color(0f, 1f, 0f, 1f);
            onChangeButton.GetComponent<UIButton>().defaultColor = new Color(0.218f, 0.6464f, 0.6509f, 1f);

            currentEventType = CurrentEventType.OnDisable;

            CreateEventsList();
        }
        void OnChangeBtnClick()
        {
            onEnableButton.GetComponent<UIButton>().defaultColor = new Color(0.218f, 0.6464f, 0.6509f, 1f);
            onDisableButton.GetComponent<UIButton>().defaultColor = new Color(0.218f, 0.6464f, 0.6509f, 1f);
            onChangeButton.GetComponent<UIButton>().defaultColor = new Color(0f, 1f, 0f, 1f);

            currentEventType = CurrentEventType.OnChange;

            CreateEventsList();
        }
        void AddNewEvent()
        {
            switch (currentEventType)
            {
                case CurrentEventType.OnEnable:
                    ((List<LE_Event>)targetObj.properties["OnEnableEvents"]).Add(new LE_Event());
                    break;

                case CurrentEventType.OnDisable:
                    ((List<LE_Event>)targetObj.properties["OnDisableEvents"]).Add(new LE_Event());
                    break;

                case CurrentEventType.OnChange:
                    ((List<LE_Event>)targetObj.properties["OnChangeEvents"]).Add(new LE_Event());
                    break;
            }

            CreateEventsList();
        }
        void PreviousEventsPage()
        {
            if (currentEventsGrid <= 0) return;

            currentEventsGrid--;

            eventsGridList.ForEach(x => x.SetActive(false));
            eventsGridList[currentEventsGrid].SetActive(true);

            // Update the state of the page buttons.
            previousEventPageButton.GetComponent<UIButton>().isEnabled = currentEventsGrid > 0;
            nextEventPageButton.GetComponent<UIButton>().isEnabled = currentEventsGrid < eventsGridList.Count - 1;
        }
        void NextEventsPage()
        {
            if (currentEventsGrid >= eventsGridList.Count - 1) return;

            currentEventsGrid++;

            eventsGridList.ForEach(x => x.SetActive(false));
            eventsGridList[currentEventsGrid].SetActive(true);

            // Update the state of the page buttons.
            previousEventPageButton.GetComponent<UIButton>().isEnabled = currentEventsGrid > 0;
            nextEventPageButton.GetComponent<UIButton>().isEnabled = currentEventsGrid < eventsGridList.Count - 1;
        }

        public void ShowEventsPage(LE_Object targetObj)
        {
            this.targetObj = targetObj;

            eventsPanel.SetActive(true);
            eventsPanel.GetComponent<TweenScale>().PlayIgnoringTimeScale(false);
            Utilities.PlayFSUISound(Utilities.FS_UISound.POPUP_UI_SHOW);

            isShowingPage = true;

            OnEnableBtnClick();
            // CreateEventsList();
        }

        public void HideEventsPage()
        {
            eventsPanel.GetComponent<TweenScale>().PlayIgnoringTimeScale(true);
            Utilities.PlayFSUISound(Utilities.FS_UISound.POPUP_UI_HIDE);

            eventsPanel.SetActive(true);
            GameObject.Find("MainMenu/Camera/Holder/Main").SetActive(false);
            isShowingPage = false;
        }

        List<LE_Event> GetEventsList()
        {
            switch (currentEventType)
            {
                case CurrentEventType.OnEnable:
                    return (List<LE_Event>)targetObj.GetProperty("OnEnableEvents");

                case CurrentEventType.OnDisable:
                    return (List<LE_Event>)targetObj.GetProperty("OnDisableEvents");

                case CurrentEventType.OnChange:
                    return (List<LE_Event>)targetObj.GetProperty("OnChangeEvents");
            }

            return null;
        }
    }
}

public class LE_Event
{
    // Yeah, why should I put a name to a freaking event? Dunno, may be useful :)
    public string eventName { get; set; } = "New Event";

    public string targetObjName { get; set; } = "";
}