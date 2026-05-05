using FS_LevelEditor.SaveSystem;
using FS_LevelEditor.UI_Related;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor.Editor.UI
{
	[MelonLoader.RegisterTypeInIl2Cpp]
	public class UpgradesPanel : MonoBehaviour
	{
		public static UpgradesPanel Instance;

		public GameObject upgradesPanel;
		UILabel upgradesPanelTitle;
		GameObject upgradesListParent;

		/// <summary>
		/// Contains all upgrades UI components
		/// </summary>
		GameObject upgradesUIParent;

		// Layout tuning constants (shared)
		const float COLUMN_OFFSET_X = 360f; // half width for two columns
		const float ROW_START_Y = 250f; // lowered to visually center
		const float ROW_SPACING = 74f; // adjust spacing for larger font
		const float LABEL_X = -180f;   // label start
		const float TICK_TO_LABEL_OFFSET = -30f; // tick positioned this far left from label
		const float BUTTON_X = 150f;   // level button position

		static readonly List<UpgradeType> allUpgrades = new List<UpgradeType>()
		{
			UpgradeType.DODGE,
			UpgradeType.SPRINT,
			UpgradeType.HYPER_SPEED,
			UpgradeType.JETPACK,
			UpgradeType.HEALTH,
			UpgradeType.SPEED,
			UpgradeType.TASER_CAPACITY,
			UpgradeType.HEALTH_BACKPACK,
			UpgradeType.TASER_BACKPACK,
			UpgradeType.TASER_POWER,
			UpgradeType.STEALTH,
			UpgradeType.AIM_STABILIZER,
			UpgradeType.HOVER,
			UpgradeType.SCOPE,
			UpgradeType.SAFE_LANDING,
			UpgradeType.UV_FLASHLIGHT,
			UpgradeType.SCANNER
		};
		// Optional upgrades can have a checkbox to disable them.
		// NOTE: Jetpack not included here because it is disabled in the global properties panel, and not in this menu.
		static readonly List<UpgradeType> optionalUpgrades = new List<UpgradeType>()
		{
			UpgradeType.HEALTH_BACKPACK,
			UpgradeType.TASER_BACKPACK,
			UpgradeType.DODGE,
			UpgradeType.SPRINT,           // ADDED - Sprint should have tick
            UpgradeType.HYPER_SPEED,      // ADDED - Hyper-Speed should have tick
            UpgradeType.TASER_POWER,      // ADDED - Taser Power now toggleable
            UpgradeType.AIM_STABILIZER,
			UpgradeType.HOVER,
			UpgradeType.SCOPE,
			UpgradeType.SAFE_LANDING,
			UpgradeType.UV_FLASHLIGHT,
			UpgradeType.SCANNER
		};

		Dictionary<UpgradeType, UpgradeUIButton> upgradeButtons = new Dictionary<UpgradeType, UpgradeUIButton>();
		List<UpgradeSaveData> targetSaveData;
		LE_Object targetObject;

        public static void Create()
		{
			if (Instance == null)
			{
				Instance = new GameObject("UpgradesUIPageManager").AddComponent<UpgradesPanel>();
				Instance.CreateUpgradesPanel();
				Instance.CreateUpgradesListParent();
				Instance.CreateUpgradesUI();
			}
		}

		public UpgradesPanel(IntPtr ptr) : base(ptr) { }

		#region Create UI
		void CreateUpgradesPanel()
		{
			upgradesPanel = Instantiate(NGUI_Utils.optionsPanel, EditorUIManager.Instance.editorUIParent.transform);
			upgradesPanel.name = "UpgradesPanel";

			upgradesPanelTitle = upgradesPanel.GetChild("Title").GetComponent<UILabel>();
			upgradesPanelTitle.gameObject.RemoveComponent<UILocalize>();

			foreach (var child in upgradesPanel.GetChilds())
			{
				string[] notDelete = { "Window", "Title" };
				if (notDelete.Contains(child.name)) continue;

				Destroy(child);
			}

			upgradesPanel.transform.GetChild("Window").transform.localPosition = Vector3.zero;
			upgradesPanelTitle.transform.localPosition = new Vector3(0f, 386.4f, 0f);

			// Remove components and set properties
			upgradesPanel.RemoveComponent<OptionsController>();
			upgradesPanel.RemoveComponent<TweenAlpha>();

			// Set title properties
			upgradesPanelTitle.transform.localPosition = new Vector3(0, 387, 0);
			upgradesPanelTitle.width = 1650;
			upgradesPanelTitle.height = 60;
			upgradesPanelTitle.fontSize = 42;
			upgradesPanelTitle.font = NGUI_Utils.juraFont ?? NGUI_Utils.labelFont;
			upgradesPanelTitle.text = "Upgrades";

			// Reset scale
			upgradesPanel.transform.localScale = Vector3.one;

			// Add UIPanel for animations
			UIPanel panel = upgradesPanel.GetComponent<UIPanel>();
			panel.alpha = 1f;
			panel.depth = 1;
			upgradesPanel.GetComponent<TweenAlpha>().mRect = panel;

			// Setup animations
			upgradesPanel.GetComponent<TweenScale>().from = Vector3.zero;
			upgradesPanel.GetComponent<TweenScale>().to = Vector3.one;

			// Make window transparent
			upgradesPanel.GetChild("Window").GetComponent<UISprite>().alpha = 0.3f;

			// Add collider for interaction blocking
			upgradesPanel.AddComponent<BoxCollider>().size = new Vector3(100000f, 100000f, 1f);

			// Close button removed - ESC key only for closing

			upgradesPanel.SetActive(false);
		}
		void CreateUpgradesListParent()
		{
			upgradesListParent = new GameObject("UpgradesList");
			upgradesListParent.transform.parent = upgradesPanel.transform;
			upgradesListParent.transform.localPosition = new Vector3(0f, 0f, 0f);
			upgradesListParent.transform.localScale = Vector3.one;
		}

		void CreateUpgradesUI()
		{
			upgradesUIParent = new GameObject("UpgradesUI");
			upgradesUIParent.transform.parent = upgradesListParent.transform;
			upgradesUIParent.transform.localPosition = Vector3.zero;
			upgradesUIParent.transform.localScale = Vector3.one;

			// Create 2 column containers
			GameObject colA = new GameObject("ColumnA");
			colA.transform.parent = upgradesUIParent.transform;
			colA.transform.localPosition = new Vector3(-COLUMN_OFFSET_X, 0, 0);
			colA.transform.localScale = Vector3.one;

			GameObject colB = new GameObject("ColumnB");
			colB.transform.parent = upgradesUIParent.transform;
			colB.transform.localPosition = new Vector3(COLUMN_OFFSET_X, 0, 0);
			colB.transform.localScale = Vector3.one;

			int half = (allUpgrades.Count + 1) / 2;
			for (int i = 0; i < half; i++) // First half
				CreateUpgradeUI(allUpgrades[i], colA.transform, i);
			for (int i = half; i < allUpgrades.Count; i++) // Second half.
				CreateUpgradeUI(allUpgrades[i], colB.transform, i - half);
		}
		void CreateUpgradeUI(UpgradeType type, Transform parentColumn, int indexInColumn)
		{
			GameObject parent = new GameObject(type.ToString());
			parent.transform.parent = parentColumn;
			parent.transform.localPosition = new Vector3(0, ROW_START_Y - (ROW_SPACING * indexInColumn), 0);
			parent.transform.localScale = Vector3.one;

			var fsType = UpgradeSaveData.ConvertTypeToFSType(type);
			string displayName = GetUpgradeDisplayName(type);

			bool isOptional = optionalUpgrades.Contains(type);
			bool isOneTimeSkill = fsType != null ? Controls.IsSkill(fsType.Value) : false;

			UpgradeUIButton upgradeButton = parent.AddComponent<UpgradeUIButton>();
			upgradeButton.type = type;

            #region Toggle If Optional
            // Upgrade that can be disabled with a checkbox.
            if (isOptional)
			{
				UITogglePatcher togglePatcher = NGUI_Utils.CreateToggle(parent.transform, new Vector3(LABEL_X + TICK_TO_LABEL_OFFSET, 0), new Vector3Int(26, 26, 0), "");
				togglePatcher.name = "TickIcon"; // keep consistent with lookups
				togglePatcher.toggle.startsActive = false; // Start unchecked. Too afraid to remove this line and screw everything up.

                #region Fix Checkmark Depth
                var checkmark = togglePatcher.transform.Find("Checkmark");
				if (checkmark != null)
				{
					var checkmarkSprite = checkmark.GetComponent<UISprite>();
					if (checkmarkSprite != null)
					{
						checkmarkSprite.depth = 2;
						checkmarkSprite.color = Color.white;
					}
				}
				#endregion

				togglePatcher.onClick += (state) => SetUpgradeEnabledState((int)type, upgradeButton);

				upgradeButton.activeToggle = togglePatcher;
			}
            #endregion

            #region Name Label
            // Name label (same position regardless of tick)
            UILabel nameLabel = NGUI_Utils.CreateLabel(parent.transform, new Vector3(LABEL_X, 0), new Vector3Int(300, 40, 0), displayName, NGUIText.Alignment.Left, UIWidget.Pivot.Left);
			nameLabel.name = "NameLabel";
			nameLabel.fontSize = 22;
			nameLabel.font = NGUI_Utils.juraFont ?? NGUI_Utils.labelFont;
			nameLabel.color = NGUI_Utils.fsLabelDefaultColor;
			nameLabel.overflowMethod = UILabel.Overflow.ClampContent; // avoid overlap
			nameLabel.depth = 1;
            #endregion

            #region Level Button If Has
            // Skills don't have level cycling.
            // NOTE: Exclude UV Flashlight because it's the only upgrade (for now) that is not a skill and has no levels.
            if (!isOneTimeSkill && type != UpgradeType.UV_FLASHLIGHT)
			{
				UIButtonMultiple levelButton = NGUI_Utils.CreateButtonMultiple(parent.transform, new Vector3(BUTTON_X, 0), Vector3.one * 0.7f, 1);
				levelButton.name = "LevelButton";
				levelButton.SetTitle("Level");

                #region Title Label Size
                Transform titleTf = levelButton.transform.Find("Title");
				if (titleTf == null) titleTf = levelButton.transform.Find("Title/Label");
				if (titleTf != null)
				{
					var titleLabelField = titleTf.GetComponent<UILabel>();
					if (titleLabelField != null)
					{
						titleLabelField.font = NGUI_Utils.juraFont ?? NGUI_Utils.labelFont;
						titleLabelField.fontSize = 22;
					}
				}
                #endregion

                int maxLevel = LevelData.GetUpgradeMaxLevel(type);
				for (int i = 1; i <= maxLevel; i++)
				{
					levelButton.AddOption("Level " + i, i == 1);
				}

				// Route to the right setter based on whether it can be disabled
				if (isOptional)
					levelButton.onClick += (id) => SetUpgradeLevel((int)type, upgradeButton);
				else
					levelButton.onClick += (id) => SetUpgradeLevelOnly((int)type, upgradeButton);

				levelButton.onLocalize = (id) => "Level " + (id + 1);

				upgradeButton.levelButton = levelButton;
			}
            #endregion

            upgradeButtons.Add(type, upgradeButton);
		}
		#endregion

		public void ShowUpgradesPanel(List<UpgradeSaveData> upgrades, string targetName, LE_Object targetObj = null)
		{
			EditorController.Instance.SetCurrentEditorState(EditorState.PAUSED);
			EditorUIManager.Instance.SetEditorUIContext(EditorUIContext.UPGRADES_PANEL);

			targetSaveData = upgrades;

			RefreshTitle(targetName);
			targetObject = targetObj;

			AttachSaveDataToUpgradeButtons();
            UpdateUpgradesUI();
		}
		public void HideUpgradesPanel()
		{
			if (targetObject)
			{
				EventsUIPageManager.Instance.ShowEventsPage(targetObject, false);
				targetObject = null;
			}
			else
			{
                EditorController.Instance.SetCurrentEditorState(EditorState.NORMAL);
                EditorUIManager.Instance.SetEditorUIContext(EditorUIContext.NORMAL);
            }
		}

		void RefreshTitle(string targetName)
		{
			if (targetName == "Global")
			{
				upgradesPanelTitle.text = "Global Upgrades";
			}
			else
			{
				upgradesPanelTitle.text = "Upgrades for " + targetName;
			}
		}

		void AttachSaveDataToUpgradeButtons()
		{
			foreach (var button in upgradeButtons)
			{
				UpgradeSaveData saveData = targetSaveData.FirstOrDefault(x => x.type == button.Key);
				if (saveData == null) // Target list may not have the save data for that one.
				{
					saveData = new UpgradeSaveData(button.Key, false, 0);
					targetSaveData.Add(saveData);
				}

				button.Value.attachedSaveData = saveData;
			}
        }
		void UpdateUpgradesUI()
		{
			foreach (var upgradeButton in upgradeButtons)
			{
				UpdateUpgradeUI(upgradeButton.Value);
			}
		}
		void UpdateUpgradeUI(UpgradeUIButton upgradeButton)
		{
			UpgradeSaveData saveData = upgradeButton.attachedSaveData;
			if (saveData == null) return; // Safety check

			// Update active toggle ONLY if it exists (optional upgrades).
			if (upgradeButton.activeToggle)
                upgradeButton.activeToggle.Set(saveData.active);

            // Update level button if it exists
            if (upgradeButton.levelButton)
			{
				int maxLevel = LevelData.GetUpgradeMaxLevel(upgradeButton.type);
                int optionsCount = upgradeButton.levelButton.OptionsCount;
                int targetIndex = Math.Clamp(saveData.level - 1, 0, optionsCount - 1); // Level 1 at index 0.

                upgradeButton.levelButton.SelectOption(targetIndex, false); // Don't execute onChange.
			}
		}

		public void SetUpgradeEnabledState(int typeID, UpgradeUIButton upgradeButton)
		{
			UpgradeSaveData saveData = upgradeButton.attachedSaveData;
			saveData.active = upgradeButton.activeToggle.isChecked;

			bool isOptional = optionalUpgrades.Contains((UpgradeType)typeID);

			if (upgradeButton.activeToggle.isChecked)
			{
				// Make sure the level is within the range.
				saveData.level = Mathf.Clamp(saveData.level, 1, LevelData.GetUpgradeMaxLevel(upgradeButton.type));

				// Force UV flashlight to level 1 (no cycling)
				if (upgradeButton.type == UpgradeType.UV_FLASHLIGHT)
                    saveData.level = 1;

                // Update level button to show current level with proper bounds checking
                if (upgradeButton.levelButton)
				{
					int optionsCount = upgradeButton.levelButton.OptionsCount;
					int targetIndex = Math.Clamp(saveData.level - 1, 0, optionsCount - 1); // Level 1 at index 0.
					upgradeButton.levelButton.SelectOption(targetIndex, false); // Don't execute onChange to avoid infinite loop.
				}
			}

			EditorController.Instance.levelHasBeenModified = true;
		}

		public void SetUpgradeLevel(int typeID, UpgradeUIButton upgradeButton)
		{
			UpgradeSaveData saveData = upgradeButton.attachedSaveData;

			int selectedLevel = upgradeButton.levelButton.currentSelectedID + 1; // +1 because levels start from 1
			selectedLevel = Mathf.Clamp(selectedLevel, 1, LevelData.GetUpgradeMaxLevel((UpgradeType)typeID));

            // Only update the level (do NOT force active=true or tick)
            saveData.level = selectedLevel;

			EditorController.Instance.levelHasBeenModified = true;
		}
		public void SetUpgradeLevelOnly(int typeID, UpgradeUIButton upgradeButton)
		{
			UpgradeSaveData saveData = upgradeButton.attachedSaveData;

			int selectedLevel = upgradeButton.levelButton.currentSelectedID + 1; // +1 because levels start from 1
			selectedLevel = Mathf.Clamp(selectedLevel, 1, LevelData.GetUpgradeMaxLevel((UpgradeType)typeID));

			// Always keep these upgrades active and just change the level
			saveData.active = true;
			saveData.level = selectedLevel;

			EditorController.Instance.levelHasBeenModified = true;
		}

		// Helper method to get display names
		string GetUpgradeDisplayName(UpgradeType type)
		{
			switch (type)
			{
				case UpgradeType.DODGE: return "Dodge";
				case UpgradeType.SPRINT: return "Sprint";
				case UpgradeType.HYPER_SPEED: return "Hyper-Speed";
				case UpgradeType.JETPACK: return "Jetpack";
				case UpgradeType.HEALTH: return "Health";
				case UpgradeType.SPEED: return "Speed";
				case UpgradeType.TASER_CAPACITY: return "Ammo Capacity";
				case UpgradeType.HEALTH_BACKPACK: return "Health Backpack";
				case UpgradeType.TASER_BACKPACK: return "Taser Backpack";
				case UpgradeType.TASER_POWER: return "Hyper-Shot";
				case UpgradeType.STEALTH: return "Stealth";
				case UpgradeType.AIM_STABILIZER: return "Aim Stabilizer";
				case UpgradeType.HOVER: return "Hover";
				case UpgradeType.SCOPE: return "Scope";
				case UpgradeType.SAFE_LANDING: return "Safe Landing";
				case UpgradeType.UV_FLASHLIGHT: return "UV";
				case UpgradeType.SCANNER: return "Scanner";
				default: return type.ToString().Replace("_", " ");
			}
		}
	}

	[MelonLoader.RegisterTypeInIl2Cpp]
	public class UpgradeUIButton : MonoBehaviour
	{
		public UpgradeType type;
		public UITogglePatcher activeToggle;
		public UIButtonMultiple levelButton;

		public UpgradeSaveData attachedSaveData;

		public UpgradeUIButton(IntPtr ptr) : base(ptr) { }
	}
}