using FS_LevelEditor.SaveSystem;
using FS_LevelEditor.UI_Related;
using Il2Cpp;
using Il2CppInControl.NativeDeviceProfiles;
using Il2CppVLB;
using MelonLoader;
using Mono.Cecil;
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

namespace FS_LevelEditor.Editor.UI
{
	public enum EditorUIContext
	{
		NORMAL,
		HELP_PANEL,
		EVENTS_PANEL,
		SELECTING_TARGET_OBJ,
		GLOBAL_PROPERTIES,
		TEXT_EDITOR,
		GROUPS_PANEL,
		ADD_TO_GROUP_PANEL,
		UPGRADES_PANEL,
		SAVE_METADATA_PANEL,
		FIND_OBJECT
	}

	[RegisterTypeInIl2Cpp]
	public class EditorUIManager : MonoBehaviour
	{
		public static EditorUIManager Instance;

		public GameObject editorUIParent;

		EditorUIContext previousUIContext;
		EditorUIContext currentUIContext;

		UILabel savingLevelLabel;
		UILabel savingLevelLabelInPauseMenu;
		Coroutine savingLevelLabelRoutine;

		public UILabel currentModeLabel;
		GameObject modeNavigationPanel;
		UIButtonPatcher previousButtonObj, nextButtonObj;

		public GameObject helpPanel;

		GameObject hittenTargetObjPanel;
		UILabel hittenTargetObjLabel;

		UIButtonPatcher groupsButton;
		UIButtonPatcher findObjectButton;

		// Misc
		GameObject occluderForWhenPaused;
		public GameObject pauseMenu;
		public GameObject navigation;

		GameObject bulkSelectionPanel;
		UIButtonPatcher bulkPreviousButtonObj, bulkNextButtonObj;
		UILabel bulkSelectionLabel;

		public UILabel statsLabel;

        public EditorUIManager(IntPtr ptr) : base(ptr) { }

		void Awake()
		{
			Instance = this;

			MenuController.GetInstance().m_uiCamera.submitKey0 = KeyCode.Return;
		}

		void Start()
		{
			SetupEditorUI();

			EditorController.Instance.ChangeMode(EditorController.Mode.Selection);
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
			Invoke("DisableFuckingPauseMenu", 0.1f); // FUCKING PAUSE MENU, DISABLE!!

			editorUIParent = new GameObject("LevelEditor");
			editorUIParent.transform.parent = GameObject.Find("MainMenu/Camera/Holder").transform;
			editorUIParent.transform.localScale = Vector3.one;


			// A custom script to make the damn large buttons be the correct ones, resume, options and exit, that's all.
			// EDIT: Also to patch and do some stuff in the pause menu while in LE.
			EditorPauseMenuPatcher.Create(pauseMenu);

			EditorObjectsToBuildUI.Create(editorUIParent.transform);
			SelectedObjPanel.Create(editorUIParent.transform);
			GlobalPropertiesPanel.Create(editorUIParent.transform);
			CreateModeNavigationPanel();
			CreateHelpPanel();
			CreateBulkSelectionPanel();

			EventsUIPageManager.Create();
			TextEditorUI.Create();
			GroupsUI.Create();
			AddToGroupUI.Create();
			UpgradesPanel.Create();
			SaveMetadataPopup.Create();
			FindObjectUI.Create();

			CreateHittenTargetObjPanel();

			// Create the notification system
			NotificationSystem.Create(editorUIParent.transform);

            CreateStatsLabel();

			CreateGroupsButton();
			CreateFindObjectButton();

            // To fix the bug where sometimes the LE UI elements are "covered" by an object if it's too close to the editor camera, set the depth HIGHER.
            GameObject.Find("MainMenu/Camera").GetComponent<Camera>().depth = 12;
		}
		void DisableFuckingPauseMenu() => pauseMenu.SetActive(false);

		void GetReferences()
		{
			GameObject uiParentObj = GameObject.Find("MainMenu/Camera/Holder/");

			occluderForWhenPaused = uiParentObj.GetChild("Occluder");
			pauseMenu = uiParentObj.GetChild("Main");
			navigation = uiParentObj.GetChild("Navigation");
		}


		#region Current Mode Label / Buttons
		void CreateModeNavigationPanel()
		{
			// Create the main panel container at the original label position
			modeNavigationPanel = new GameObject("ModeNavigationPanel");
			modeNavigationPanel.transform.parent = editorUIParent.transform;
			modeNavigationPanel.transform.localPosition = new Vector3(800, -500, 0); // Moved left to accommodate buttons
			modeNavigationPanel.transform.localScale = Vector3.one;

			previousButtonObj = NGUI_Utils.CreateButton(modeNavigationPanel.transform, new Vector3(-160, 0), new Vector3Int(50, 50, 1), "<");
			previousButtonObj.gameObject.RemoveComponent<UIButtonScale>();
			previousButtonObj.buttonSprite.depth = 1;
			previousButtonObj.onClick += SwitchToPreviousMode;

			// Create the current mode label (center) - using original alignment and pivot
			currentModeLabel = NGUI_Utils.CreateLabel(modeNavigationPanel.transform, new Vector3(-35, 0, 0), new Vector3Int(400, 50, 0), "", NGUIText.Alignment.Center,
				UIWidget.Pivot.Center);
			currentModeLabel.fontSize = 35;
			SetCurrentModeLabelText(EditorController.Mode.Building);

			nextButtonObj = NGUI_Utils.CreateButton(modeNavigationPanel.transform, new Vector3(90, 0), new Vector3Int(50, 50, 1), ">");
			nextButtonObj.gameObject.RemoveComponent<UIButtonScale>();
			nextButtonObj.buttonSprite.depth = 1;
			nextButtonObj.onClick += SwitchToNextMode;

			modeNavigationPanel.SetActive(true);
		}
		void SwitchToPreviousMode()
		{
			EditorController.Mode currentMode = EditorController.Instance.currentMode;
			EditorController.Mode[] modes = (EditorController.Mode[])Enum.GetValues(typeof(EditorController.Mode));

			int currentIndex = Array.IndexOf(modes, currentMode);
			int previousIndex = (currentIndex - 1 + modes.Length) % modes.Length;

			EditorController.Instance.ChangeMode(modes[previousIndex]);
			SetCurrentModeLabelText(modes[previousIndex]);

		}
		void SwitchToNextMode()
		{
			UnityEngine.Debug.Log("Switching next");
			EditorController.Mode currentMode = EditorController.Instance.currentMode;
			EditorController.Mode[] modes = (EditorController.Mode[])Enum.GetValues(typeof(EditorController.Mode));

			int currentIndex = Array.IndexOf(modes, currentMode);
			int nextIndex = (currentIndex + 1) % modes.Length;

			EditorController.Instance.ChangeMode(modes[nextIndex]);
			SetCurrentModeLabelText(modes[nextIndex]);
		}
		public void SetCurrentModeLabelText(EditorController.Mode mode)
		{
			string text = Loc.Get(mode.ToString());
			currentModeLabel.text = "[ffff00]" + text + "[-]";
		}
		#endregion

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

		void CreateBulkSelectionPanel()
		{
			// Create the main panel container
			bulkSelectionPanel = new GameObject("BulkSelectionPanel");
			bulkSelectionPanel.transform.parent = editorUIParent.transform;
			bulkSelectionPanel.transform.localPosition = new Vector3(800, -440, 0); // Just above the mode panel
			bulkSelectionPanel.transform.localScale = Vector3.one;

			bulkPreviousButtonObj = NGUI_Utils.CreateButton(bulkSelectionPanel.transform, new Vector3(-160, 0), new Vector3Int(50, 50, 1), "<");
			bulkPreviousButtonObj.gameObject.RemoveComponent<UIButtonScale>();
			bulkPreviousButtonObj.buttonSprite.depth = 1;
			bulkPreviousButtonObj.onClick += SwitchToPreviousBulkSelectionMode;

			bulkSelectionLabel = NGUI_Utils.CreateLabel(bulkSelectionPanel.transform, new Vector3(-35, 0, 0), new Vector3Int(400, 50, 0), "", NGUIText.Alignment.Center,
				UIWidget.Pivot.Center);
			bulkSelectionLabel.fontSize = 28;
			SetBulkSelectionLabelText(EditorController.Instance.GetBulkSelectionMode());

			bulkNextButtonObj = NGUI_Utils.CreateButton(bulkSelectionPanel.transform, new Vector3(90, 0), new Vector3Int(50, 50, 1), ">");
			bulkNextButtonObj.gameObject.RemoveComponent<UIButtonScale>();
			bulkNextButtonObj.buttonSprite.depth = 1;
			bulkNextButtonObj.onClick += SwitchToNextBulkSelectionMode;

			bulkSelectionPanel.SetActive(true);
		}

        void SwitchToPreviousBulkSelectionMode()
		{
			var modes = (BulkSelectionMode[])Enum.GetValues(typeof(BulkSelectionMode));
			int currentIndex = Array.IndexOf(modes, EditorController.Instance.GetBulkSelectionMode());
			int previousIndex = (currentIndex - 1 + modes.Length) % modes.Length;
			EditorController.Instance.SetBulkSelectionMode(modes[previousIndex]);
			SetBulkSelectionLabelText(modes[previousIndex]);
		}

		public void SwitchToNextBulkSelectionMode()
		{
			var modes = (BulkSelectionMode[])Enum.GetValues(typeof(BulkSelectionMode));
			int currentIndex = Array.IndexOf(modes, EditorController.Instance.GetBulkSelectionMode());
			int nextIndex = (currentIndex + 1) % modes.Length;
			EditorController.Instance.SetBulkSelectionMode(modes[nextIndex]);
			SetBulkSelectionLabelText(modes[nextIndex]);
		}

		public void SetBulkSelectionLabelText(BulkSelectionMode mode)
		{
			string text = mode switch
			{
				BulkSelectionMode.Everything => "Everything",
				BulkSelectionMode.ObjectsOnly => "Objects Only",
				BulkSelectionMode.WaypointsAndObjectsWithWaypoints => "Waypoints",
				_ => mode.ToString()
			};
			bulkSelectionLabel.text = "[00ffff]" + text + "[-]";
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

        void CreateStatsLabel()
        {
            // Create grid size label
            statsLabel = NGUI_Utils.CreateLabel(
                editorUIParent.transform,
                new Vector3(0f, -540f, 0f),
                new Vector3Int(400, 90, 0),
                "Stats",
                NGUIText.Alignment.Center,
                UIWidget.Pivot.Bottom
            );
            statsLabel.fontSize = 22;
            statsLabel.color = Color.white;
            statsLabel.name = "StatsLabel";

			UpdateStatsLabel();
        }
		public void UpdateStatsLabel()
		{
			StringBuilder stats = new StringBuilder();

			stats.AppendLine($"Camera Speed: {EditorCameraMovement.Instance.moveSpeed:0.###}");
			stats.AppendLine($"Grid Size: {EditorController.Instance.GetGridSize():0.###}");
			stats.AppendLine("Waypoint Rotation: " + (EditorController.Instance.waypointRotation ? "[c][00FF00]Enabled[-][/c]" : "[c][FF0000]Disabled[-][/c]"));

			statsLabel.text = stats.ToString();
		}

		void CreateGroupsButton()
		{
			groupsButton = NGUI_Utils.CreateButton(editorUIParent.transform, new Vector3(-935, 200), Vector3Int.one * 50, "...");
			groupsButton.name = "GroupsButton";
			groupsButton.onClick += GroupsUI.Instance.ShowGroupsPanel;
		}
        void CreateFindObjectButton()
        {
            findObjectButton = NGUI_Utils.CreateButton(editorUIParent.transform, new Vector3(-935, 140), Vector3Int.one * 50, "...");
            findObjectButton.name = "FindObjectButton";
            findObjectButton.onClick += FindObjectUI.Instance.Show;
        }

        public void ShowPause()
		{
			// Disable the editor UI and enable the navigation bar.
			editorUIParent.SetActive(false);
			navigation.SetActive(true);

			Utils.PlayFSUISound(Utils.FS_UISound.SHOW_NEW_PAGE_SOUND);

			// Set the occluder color, it's opaque by defualt for some reason (Anyways, Charles and his weird systems...).
			occluderForWhenPaused.GetComponent<UISprite>().color = new Color(0f, 0f, 0f, 0.9f);

			// Enable the pause panel and play its animations.
			pauseMenu.SetActive(true);
			TweenAlpha pauseTween = pauseMenu.GetComponent<TweenAlpha>();
			pauseTween.delay = 0f;
			pauseTween.duration = 0.3f;
			pauseTween.ignoreTimeScale = true;
			pauseTween.PlayForward();
			//TweenAlpha.Begin(pauseMenu, 0.2f, 1f);

			// Set the paused variable in the LE controller.
			EditorController.Instance.SetCurrentEditorState(EditorState.PAUSED);

			Logger.Log("LE paused!");
		}
		public void Resume()
		{
			// If you're resuming BUT if the pause menu is disabled itself, then is likely cause the user is in another submenu (like options), in that cases.. don't do anything.
			if (!pauseMenu.activeSelf) return;

			// If the user is in the exit confirmation popup, just hide it and do nothing.
			if (EditorPauseMenuPatcher.patcher.exitPopupEnabled)
			{
				EditorPauseMenuPatcher.patcher.OnExitPopupButtonClicked(false, false);
				return;
			}

			MelonCoroutines.Start(Coroutine());

			IEnumerator Coroutine()
			{
				// Disable the navigation bar.
				navigation.SetActive(false);

				// Play the pause menu animations backwards.
				TweenAlpha pauseTween = pauseMenu.GetComponent<TweenAlpha>();
				pauseTween.delay = 0f;
				pauseTween.ignoreTimeScale = true;
				pauseTween.PlayReverse();
				//TweenAlpha.Begin(pauseMenu, 0.2f, 0f);

				// Threshold to wait for the pause animation to end.
				yield return new WaitForSecondsRealtime(0.3f);

				if (!EditorController.Instance.enteringPlayMode) // The user may've pressed the play button right before the pause menu dissapeared.
				{
					// Enable the LE UI and disable the pause menu.
					editorUIParent.SetActive(true);
					pauseMenu.SetActive(false);

					// And set the paused variable in the controller as false.
					EditorController.Instance.SetCurrentEditorState(EditorState.NORMAL);
				}
			}

			Logger.Log("LE resumed!");
		}

		public void ShowExitPopup() => EditorPauseMenuPatcher.patcher.ShowExitPopup();
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
			Destroy(pauseMenu.GetChild("SavingLevelInPauseMenu"));

			if (statsLabel) Destroy(statsLabel.gameObject);

			Logger.Log("LE UI deleted!");
		}

        #region Set Editor UI Context
        public void SetEditorUIContext(EditorUIContext newContext)
		{
			if (newContext == currentUIContext) return;

			bool showInstantly = false;

            #region Hide The Previous Context
            // Cases where only the previous context needs to be hidden instatly.
            if ((newContext == EditorUIContext.HELP_PANEL && currentUIContext == EditorUIContext.GLOBAL_PROPERTIES) || // Global Properties --> Help Panel
					 (newContext == EditorUIContext.SELECTING_TARGET_OBJ && currentUIContext == EditorUIContext.EVENTS_PANEL) || // Events Panel --> Selecting Target Object
                     (newContext == EditorUIContext.UPGRADES_PANEL && currentUIContext == EditorUIContext.GLOBAL_PROPERTIES)) // Global Properties --> Upgrades Panel
            {
                HideEditorUIContext(currentUIContext, true);
            }
			// Cases where the previous and the new context need to be shown/hidden instantly.
            else if ((newContext == EditorUIContext.EVENTS_PANEL && currentUIContext == EditorUIContext.SELECTING_TARGET_OBJ)) // Selecting Target Object --> Events Panel
            {
				HideEditorUIContext(currentUIContext, true);
				showInstantly = true;
			}
			else
			{
				HideEditorUIContext(currentUIContext, false);
			}
            #endregion

            GameObject target = null;
			bool playEntrySFX = true;

            #region Decide Da Target
            switch (newContext)
			{
				case EditorUIContext.HELP_PANEL:
					target = helpPanel;
					playEntrySFX = false;
					break;

				case EditorUIContext.EVENTS_PANEL:
					target = EventsUIPageManager.Instance.eventsPanel;
                    break;

				case EditorUIContext.SELECTING_TARGET_OBJ:
					target = hittenTargetObjPanel;
					playEntrySFX = false;
                    break;

				case EditorUIContext.GLOBAL_PROPERTIES:
					target = GlobalPropertiesPanel.Instance.gameObject;
					playEntrySFX = false;
                    break;

				case EditorUIContext.TEXT_EDITOR:
					target = TextEditorUI.Instance.editorPanel;
                    break;

				case EditorUIContext.GROUPS_PANEL:
					target = GroupsUI.Instance.editorPanel;
					break;

				case EditorUIContext.ADD_TO_GROUP_PANEL:
					target = AddToGroupUI.Instance.addPanel;
					break;

				case EditorUIContext.UPGRADES_PANEL:
					target = UpgradesPanel.Instance.upgradesPanel;
                    break;

				case EditorUIContext.SAVE_METADATA_PANEL:
					target = SaveMetadataPopup.Instance.popupPanel;
					break;

				case EditorUIContext.FIND_OBJECT:
					target = FindObjectUI.Instance.findPanel;
					break;
			}
            #endregion

            #region Execute Specific Actions Depending On The Target
			switch (newContext)
			{
				case EditorUIContext.GLOBAL_PROPERTIES:
                    GlobalPropertiesPanel.Instance.RefreshGlobalPropertiesPanelValues();
                    break;
			}
            #endregion

            #region Play The Animation
            if (newContext != EditorUIContext.NORMAL) // NORMAL is the only one that doesn't have a target obj.
			{
                if (target.TryGetComponent<TweenScale>(out var tweenScale))
                {
					tweenScale.SetDirection(Il2CppAnimationOrTween.Direction.Forward);

                    if (showInstantly)
                    {
                        tweenScale.SetSample(1f, true); // Set to end.
                        target.SetActive(true);
                        //target.transform.localScale = tweenScale.to;
                    }
                    else
                    {
                        tweenScale.SetSample(0f, true); // Set to beginning.
                        target.SetActive(true);
                        tweenScale.PlayIgnoringTimeScale(false);
                        if (playEntrySFX) Utils.PlayFSUISound(Utils.FS_UISound.POPUP_UI_SHOW);
                    }
                }
                else if (target.TryGetComponent<TweenPosition>(out var tweenPosition))
                {
                    tweenPosition.SetDirection(Il2CppAnimationOrTween.Direction.Forward);

                    if (showInstantly)
                    {
                        tweenPosition.SetSample(1f, true); // Set to end.
                        target.SetActive(true);
                        //target.transform.localPosition = tweenPosition.to;
                    }
                    else
                    {
                        tweenPosition.SetSample(0f, true); // Set to beginning.
                        target.SetActive(true);
                        tweenPosition.PlayIgnoringTimeScale(false);
                        if (playEntrySFX) Utils.PlayFSUISound(Utils.FS_UISound.POPUP_UI_SHOW);
                    }
                }
                else
                {
                    // No fancy tween, just instant.
                    target.SetActive(true);
                }
            }
            #endregion

            #region UI Elements To Only Enable When Normal
            EditorObjectsToBuildUI.Instance.root.SetActive(newContext == EditorUIContext.NORMAL && EditorController.Instance.currentMode == EditorController.Mode.Building);
            SelectedObjPanel.Instance.gameObject.SetActive(newContext == EditorUIContext.NORMAL && EditorController.Instance.currentMode != EditorController.Mode.Building);

            bulkSelectionPanel.SetActive(newContext == EditorUIContext.NORMAL);
            bulkNextButtonObj.gameObject.SetActive(newContext == EditorUIContext.NORMAL);
            bulkPreviousButtonObj.gameObject.SetActive(newContext == EditorUIContext.NORMAL);
            bulkSelectionLabel.gameObject.SetActive(newContext == EditorUIContext.NORMAL);
            currentModeLabel.gameObject.SetActive(newContext == EditorUIContext.NORMAL);
            nextButtonObj.gameObject.SetActive(newContext == EditorUIContext.NORMAL);
            previousButtonObj.gameObject.SetActive(newContext == EditorUIContext.NORMAL);
            statsLabel.gameObject.SetActive(newContext == EditorUIContext.NORMAL);
			groupsButton.gameObject.SetActive(newContext == EditorUIContext.NORMAL);
			#endregion

			// Make sure any object on the other UI is deselected properly to avoid bugs.
			UICamera.selectedObject = null;
			UICamera.hoveredObject = null;

            previousUIContext = currentUIContext;
			currentUIContext = newContext;

			Logger.Log($"Switched Editor UI Context from {previousUIContext} to {currentUIContext}.");
		}
		void HideEditorUIContext(EditorUIContext context, bool instant = false)
		{
			if (context == EditorUIContext.NORMAL) return;

			GameObject target = null;
			bool playExitSFX = true;

            #region Decide Da Target
            switch (context)
			{
				case EditorUIContext.HELP_PANEL:
					target = helpPanel;
					playExitSFX = false;
					break;

				case EditorUIContext.EVENTS_PANEL:
					target = EventsUIPageManager.Instance.eventsPanel;
                    break;

				case EditorUIContext.SELECTING_TARGET_OBJ:
					target = hittenTargetObjPanel;
					playExitSFX = false;
					break;

				case EditorUIContext.GLOBAL_PROPERTIES:
					target = GlobalPropertiesPanel.Instance.gameObject;
					playExitSFX = false;
                    break;

				case EditorUIContext.TEXT_EDITOR:
					target = TextEditorUI.Instance.editorPanel;
                    break;

                case EditorUIContext.GROUPS_PANEL:
                    target = GroupsUI.Instance.editorPanel;
                    break;

                case EditorUIContext.ADD_TO_GROUP_PANEL:
                    target = AddToGroupUI.Instance.addPanel;
                    break;

                case EditorUIContext.UPGRADES_PANEL:
					target = UpgradesPanel.Instance.upgradesPanel;
                    break;

				case EditorUIContext.SAVE_METADATA_PANEL:
					target = SaveMetadataPopup.Instance.popupPanel;
					break;

                case EditorUIContext.FIND_OBJECT:
                    target = FindObjectUI.Instance.findPanel;
                    break;
            }
            #endregion

            #region Play The Animation
			if (target.TryGetComponent<TweenScale>(out var tweenScale))
			{
                tweenScale.SetDirection(Il2CppAnimationOrTween.Direction.Forward);

                if (instant)
				{
                    tweenScale.SetSample(0f, true); // Set to beginning.
                    target.SetActive(false);
					//target.transform.localScale = tweenScale.from;
				}
				else
				{
                    tweenScale.SetSample(1f, true); // Set to end.
                    tweenScale.PlayIgnoringTimeScale(true);
                    if (playExitSFX) Utils.PlayFSUISound(Utils.FS_UISound.POPUP_UI_HIDE);
                }
			}
			else if (target.TryGetComponent<TweenPosition>(out var tweenPosition))
			{
                tweenPosition.SetDirection(Il2CppAnimationOrTween.Direction.Forward);

                if (instant)
				{
					tweenPosition.SetSample(0f, false); // Set to the beginning.
                    target.SetActive(false);
					//target.transform.localPosition = tweenPosition.from;
				}
				else
				{
                    tweenPosition.SetSample(1f, true); // Set to end.
                    tweenPosition.PlayIgnoringTimeScale(true);
					if (playExitSFX) Utils.PlayFSUISound(Utils.FS_UISound.POPUP_UI_HIDE);
				}
			}
			else
			{
                // No fancy tween, just instant.
                target.SetActive(false);
            }
            #endregion
        }
        #endregion

        public static bool IsCurrentUIContext(EditorUIContext context)
		{
			if (Instance == null) return false;

			return Instance.currentUIContext == context;
		}

		public void OnLanguageChanged()
		{
			SetCurrentModeLabelText(EditorController.Instance.currentMode);
			UIDropdownPatcher.RefreshLocalizationForAll();
			UIButtonMultiple.RefreshLocalizationForAll();
			UISmallButtonMultiple.RefreshLocalizationForAll();

			if (SelectedObjPanel.Instance) SelectedObjPanel.Instance.UpdateHeaderTitle();
		}

		void OnDestroy()
		{
			if (MenuController.GetInstance() && MenuController.GetInstance().m_uiCamera)
			{
				// Revert this just in case it breaks something LOL.
				MenuController.GetInstance().m_uiCamera.submitKey0 = KeyCode.None;
			}

            Instance = null;
        }
	}
}