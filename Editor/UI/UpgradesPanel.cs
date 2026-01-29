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
		UILabel noUpgradesLabel;

		/// <summary>
		/// Contains all upgrades UI components
		/// </summary>
		GameObject upgradesUIParent;

		// Layout tuning constants (shared)
		const float ColumnOffsetX = 360f; // half width for two columns
		const float RowStartY = 250f; // lowered to visually center
		const float RowSpacing = 74f; // adjust spacing for larger font
		const float LabelX = -180f;   // label start
		const float TickToLabelOffset = -30f; // tick positioned this far left from label
		const float ButtonX = 150f;   // level button position

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
			colA.transform.localPosition = new Vector3(-ColumnOffsetX, 0, 0);
			colA.transform.localScale = Vector3.one;

			GameObject colB = new GameObject("ColumnB");
			colB.transform.parent = upgradesUIParent.transform;
			colB.transform.localPosition = new Vector3(ColumnOffsetX, 0, 0);
			colB.transform.localScale = Vector3.one;

			var allUpgrades = new List<UpgradeType>
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

			int half = (allUpgrades.Count + 1) / 2;
			for (int i = 0; i < half; i++)
				CreateUpgradeUI(allUpgrades[i], colA.transform, i);
			for (int i = half; i < allUpgrades.Count; i++)
				CreateUpgradeUI(allUpgrades[i], colB.transform, i - half);
		}

		void CreateUpgradeUI(UpgradeType type, Transform parentColumn, int indexInColumn)
		{
			GameObject parent = new GameObject(type.ToString());
			parent.transform.parent = parentColumn;
			parent.transform.localPosition = new Vector3(0, RowStartY - (RowSpacing * indexInColumn), 0);
			parent.transform.localScale = Vector3.one;

			var fsType = UpgradeSaveData.ConvertTypeToFSType(type);
			string displayName = GetUpgradeDisplayName(type);

			// Define which upgrades can have level 0 (should show ticks) - ADDED SPRINT AND HYPER_SPEED
			var upgradesWithLevel0 = new HashSet<UpgradeType>
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

			bool canHaveLevel0 = upgradesWithLevel0.Contains(type);
			bool isOneTimeSkill = fsType != null ? Controls.IsSkill(fsType.Value) : false;

			// Optional tick (only for items that can be disabled)
			if (canHaveLevel0)
			{
				UITogglePatcher tickIcon = NGUI_Utils.CreateToggle(parent.transform, new Vector3(LabelX + TickToLabelOffset, 0), new Vector3Int(26, 26, 0), "");
				tickIcon.name = "TickIcon"; // keep consistent with lookups
				var tickSprite = tickIcon.toggle;
				tickSprite.startsActive = false; // Start unchecked

				var checkmark = tickIcon.transform.Find("Checkmark");
				if (checkmark != null)
				{
					var checkmarkSprite = checkmark.GetComponent<UISprite>();
					if (checkmarkSprite != null)
					{
						checkmarkSprite.depth = 2;
						checkmarkSprite.color = Color.white;
					}
				}

				// Reverted: use default background sprite & size (no manual square / border edits)
				// Reverted: no custom scale on checkmark

				EventDelegate tickDelegate = NGUI_Utils.CreateEvenDelegate(this, nameof(SetUpgradeEnabledState),
					NGUI_Utils.CreateEventDelegateParamter(this, "type", (int)type),
					NGUI_Utils.CreateEventDelegateParamter(this, "toggle", tickSprite));
				tickSprite.onChange.Add(tickDelegate);
			}

			// Name label (same position regardless of tick)
			UILabel nameLabel = NGUI_Utils.CreateLabel(parent.transform, new Vector3(LabelX, 0), new Vector3Int(300, 40, 0), displayName, NGUIText.Alignment.Left, UIWidget.Pivot.Left);
			nameLabel.name = "NameLabel";
			nameLabel.fontSize = 22;
			nameLabel.font = NGUI_Utils.juraFont ?? NGUI_Utils.labelFont;
			nameLabel.color = NGUI_Utils.fsLabelDefaultColor;
			nameLabel.overflowMethod = UILabel.Overflow.ClampContent; // avoid overlap
			nameLabel.depth = 1;

			// Level button (if not an one-time skill) – always aligned to same X
			// UV_FLASHLIGHT should act as simple on/off (no level cycling)
			if (type == UpgradeType.UV_FLASHLIGHT)
			{
				// Ensure when enabled it always stays at level 1
				// (Handled in SetUpgradeEnabledState when toggled on; no button needed here)
			}
			else if (!isOneTimeSkill)
			{
				UIButtonMultiple levelButton = NGUI_Utils.CreateButtonMultiple(parent.transform, new Vector3(ButtonX, 0), Vector3.one * 0.7f, 1);
				levelButton.name = "LevelButton";
				levelButton.SetTitle("Level");
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

				int maxLevel = LevelData.GetUpgradeMaxLevel(type);
				for (int i = 1; i <= maxLevel; i++)
				{
					levelButton.AddOption("Level " + i, i == 1);
				}

				// Route to the right setter based on whether it can be disabled
				if (canHaveLevel0)
					levelButton.onClick += (id) => SetUpgradeLevel((int)type, levelButton);
				else
					levelButton.onClick += (id) => SetUpgradeLevelOnly((int)type, levelButton);

				levelButton.onLocalize = (id) => "Level " + (id + 1);
			}
		}
		#endregion

		public void ShowUpgradesPanel()
		{
			EditorController.Instance.SetCurrentEditorState(EditorState.PAUSED);
			EditorUIManager.Instance.SetEditorUIContext(EditorUIContext.UPGRADES_PANEL);

			UpdateUpgradesUI();
		}
		public void HideUpgradesPanel()
		{
			EditorController.Instance.SetCurrentEditorState(EditorState.NORMAL);
			EditorUIManager.Instance.SetEditorUIContext(EditorUIContext.NORMAL);
		}

		void UpdateUpgradesUI()
		{
			var colA = upgradesUIParent.transform.Find("ColumnA");
			if (colA != null)
				for (int i = 0; i < colA.childCount; i++) UpdateUpgradeUI(colA.GetChild(i));

			var colB = upgradesUIParent.transform.Find("ColumnB");
			if (colB != null)
				for (int i = 0; i < colB.childCount; i++) UpdateUpgradeUI(colB.GetChild(i));
		}
		void UpdateUpgradeUI(Transform upgradeParent)
		{
			var upgradeType = Enum.Parse<UpgradeType>(upgradeParent.name);
			var upgradeData = ((List<UpgradeSaveData>)EditorController.Instance.globalProperties["Upgrades"]).Find(x => x.type == upgradeType);

			if (upgradeData == null) return; // Safety check

			// Update tick icon ONLY if it exists (only for upgrades that can have level 0)
			if (upgradeParent.gameObject.ExistsChild("TickIcon"))
			{
				upgradeParent.gameObject.GetChild("TickIcon").GetComponent<UIToggle>().Set(upgradeData.active);
			}

			// Update level button if it exists
			if (upgradeParent.gameObject.ExistsChild("LevelButton"))
			{
				var levelButton = upgradeParent.gameObject.GetChild("LevelButton").GetComponent<UIButtonMultiple>();

				// Determine if this upgrade can have level 0 - UPDATED to include Sprint and Hyper-Speed
				var upgradesWithLevel0 = new HashSet<UpgradeType>
				{
					UpgradeType.HEALTH_BACKPACK,
					UpgradeType.TASER_BACKPACK,
					UpgradeType.DODGE,
					UpgradeType.SPRINT,           // ADDED
                    UpgradeType.HYPER_SPEED,      // ADDED
                    UpgradeType.TASER_POWER,      // ADDED
                    UpgradeType.AIM_STABILIZER,
					UpgradeType.HOVER,
					UpgradeType.SCOPE,
					UpgradeType.SAFE_LANDING,
					UpgradeType.UV_FLASHLIGHT,
					UpgradeType.SCANNER
				};

				bool canHaveLevel0 = upgradesWithLevel0.Contains(upgradeType);

				int targetOption = 0;
				int maxLevel = LevelData.GetUpgradeMaxLevel(upgradeType);

				if (canHaveLevel0)
				{
					// For upgrades that can have level 0, only show level if active
					if (upgradeData.active && upgradeData.level > 0)
					{
						targetOption = Mathf.Clamp(upgradeData.level, 1, maxLevel) - 1; // clamp to max
					}
					else
					{
						targetOption = 0; // Default to first level
					}
				}
				else
				{
					// For upgrades that can't have level 0, show the actual level
					targetOption = Mathf.Clamp(upgradeData.level, 1, maxLevel) - 1; // clamp to max
				}

				// Safe call to SelectOption with try-catch to prevent exceptions
				try
				{
					if (targetOption >= 0)
					{
						levelButton.SelectOption(targetOption, false); // Don't execute onChange
					}
				}
				catch (System.ArgumentOutOfRangeException)
				{
					// If we get out of range, just select option 0
					try
					{
						levelButton.SelectOption(0, false);
					}
					catch
					{
						// If even option 0 fails, there might be no options - skip
					}
				}
			}
		}

		public void SetUpgradeEnabledState(int typeID, UIToggle toggle)
		{
			var list = (List<UpgradeSaveData>)EditorController.Instance.globalProperties["Upgrades"];
			var typeToModify = list.Find(x => x.type == (UpgradeType)typeID);

			typeToModify.active = toggle.isChecked;

			// Define which upgrades can have level 0 - UPDATED to include Sprint and Hyper-Speed
			var upgradesWithLevel0 = new HashSet<UpgradeType>
			{
				UpgradeType.HEALTH_BACKPACK,
				UpgradeType.TASER_BACKPACK,
				UpgradeType.DODGE,
				UpgradeType.SPRINT,           // ADDED
                UpgradeType.HYPER_SPEED,      // ADDED
                UpgradeType.TASER_POWER,      // ADDED
                UpgradeType.AIM_STABILIZER,
				UpgradeType.HOVER,
				UpgradeType.SCOPE,
				UpgradeType.SAFE_LANDING,
				UpgradeType.UV_FLASHLIGHT,
				UpgradeType.SCANNER
			};

			bool canHaveLevel0 = upgradesWithLevel0.Contains((UpgradeType)typeID);

			if (toggle.isChecked)
			{
				// If enabling the upgrade and it has level 0, set it to level 1
				if (canHaveLevel0 && typeToModify.level == 0)
				{
					typeToModify.level = 1;
				}
				else if (!canHaveLevel0 && typeToModify.level == 0)
				{
					// For upgrades that can't be level 0, ensure they have at least level 1
					typeToModify.level = 1;
				}

				// Force UV flashlight to level 1 (no cycling)
				if ((UpgradeType)typeID == UpgradeType.UV_FLASHLIGHT)
				{
					typeToModify.level = 1;
				}

				// Update level button to show current level with proper bounds checking
				var upgradeParent = FindUpgradeParent((UpgradeType)typeID);
				if (upgradeParent != null && upgradeParent.gameObject.ExistsChild("LevelButton"))
				{
					var levelButton = upgradeParent.gameObject.GetChild("LevelButton").GetComponent<UIButtonMultiple>();
					int targetIndex = Math.Max(typeToModify.level - 1, 0); // Level 1 is at index 0

					// Safe call to SelectOption with try-catch to prevent exceptions
					try
					{
						levelButton.SelectOption(targetIndex, false); // Don't execute onChange to avoid infinite loop
					}
					catch (System.ArgumentOutOfRangeException)
					{
						// If we get out of range, just select option 0 (Level 1)
						try
						{
							levelButton.SelectOption(0, false);
						}
						catch
						{
							// If even option 0 fails, there might be no options - skip update
						}
					}
				}
			}
			else
			{
				// If disabling the upgrade, set level to 0 only for upgrades that can have level 0
				if (canHaveLevel0)
				{
					typeToModify.level = 0;
				}
				// For upgrades that can't be level 0, keep them at level 1 but mark as inactive
			}

			EditorController.Instance.levelHasBeenModified = true;
		}

		public void SetUpgradeLevel(int typeID, object button)
		{
			var list = (List<UpgradeSaveData>)EditorController.Instance.globalProperties["Upgrades"];
			var typeToModify = list.Find(x => x.type == (UpgradeType)typeID);

			// If the upgrade is currently disabled (tick off), do not auto-enable when cycling level.
			if (!typeToModify.active)
			{
				return; // Ignore level change requests while disabled.
			}

			int selectedLevel = ((UIButtonMultiple)button).currentSelectedID + 1; // +1 because levels start from 1
			selectedLevel = Mathf.Clamp(selectedLevel, 1, LevelData.GetUpgradeMaxLevel((UpgradeType)typeID));

			// Only update the level (do NOT force active=true or tick)
			typeToModify.level = selectedLevel;

			EditorController.Instance.levelHasBeenModified = true;
		}

		public void SetUpgradeLevelOnly(int typeID, object button)
		{
			var list = (List<UpgradeSaveData>)EditorController.Instance.globalProperties["Upgrades"];
			var typeToModify = list.Find(x => x.type == (UpgradeType)typeID);

			int selectedLevel = ((UIButtonMultiple)button).currentSelectedID + 1; // +1 because levels start from 1
			selectedLevel = Mathf.Clamp(selectedLevel, 1, LevelData.GetUpgradeMaxLevel((UpgradeType)typeID));

			// Always keep these upgrades active and just change the level
			typeToModify.active = true;
			typeToModify.level = selectedLevel;

			EditorController.Instance.levelHasBeenModified = true;
		}

		Transform FindUpgradeParent(UpgradeType upgradeType)
		{
			var colA = upgradesUIParent.transform.Find("ColumnA");
			if (colA != null)
			{
				var upgradeParent = colA.Find(upgradeType.ToString());
				if (upgradeParent != null) return upgradeParent;
			}
			var colB = upgradesUIParent.transform.Find("ColumnB");
			if (colB != null)
			{
				var upgradeParent = colB.Find(upgradeType.ToString());
				if (upgradeParent != null) return upgradeParent;
			}

			return null;
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
}