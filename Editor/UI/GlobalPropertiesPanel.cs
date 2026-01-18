using FS_LevelEditor.SaveSystem;
using FS_LevelEditor.UI_Related;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace FS_LevelEditor.Editor.UI
{
	[MelonLoader.RegisterTypeInIl2Cpp]
	public class GlobalPropertiesPanel : MonoBehaviour
	{
		public static GlobalPropertiesPanel Instance;

		UILabel titleLabel;
		UITogglePatcher hasTaserToggle;
		UITogglePatcher hasJetpackToggle;
		UICustomInputField deathYLimitField;
		UIButtonAsToggle visualizeDeathYLimitButton;
		UIDropdownPatcher skyboxDropdown;

		public static void Create(Transform parent)
		{
			GameObject root = new GameObject("GlobalPropertiesPanel");
			root.transform.parent = parent;
			root.transform.localPosition = new Vector3(1320f, 0f, 0f);
			root.transform.localScale = Vector3.one;

			root.AddComponent<GlobalPropertiesPanel>();
		}

		public GlobalPropertiesPanel(IntPtr ptr) : base(ptr) { }

		void Awake()
		{
			Instance = this;

			CreatePanelBackground();
			CreateTitle();
			CreateHasTaserToggle();
			CreateHasJetpackToggle();
			CreateDeathYLimitField();
			CreateLevelSkyboxDropdown();
			CreateUpgradesButton();
		}
		void Start()
		{
			RefreshGlobalPropertiesPanelValues();
		}

		void CreatePanelBackground()
		{
			UISprite background = gameObject.AddComponent<UISprite>();
			background.atlas = NGUI_Utils.UITexturesAtlas;
			background.spriteName = "Square_Border_Beveled_HighOpacity";
			background.type = UIBasicSprite.Type.Sliced;
			background.color = new Color(0.218f, 0.6464f, 0.6509f, 1f);
			background.width = 650;
			background.height = 1010;

			BoxCollider collider = gameObject.AddComponent<BoxCollider>();
			collider.size = new Vector2(650f, 1010f);
		}
		void CreateTitle()
		{
			titleLabel = NGUI_Utils.CreateLabel(transform, new Vector3(0, 460), new Vector3Int(600, 50, 0), "GlobalProperties",
				NGUIText.Alignment.Center, UIWidget.Pivot.Center);
			titleLabel.name = "Title";
			titleLabel.depth = 1;
			titleLabel.fontSize = 30;
		}
		void CreateHasTaserToggle()
		{
			hasTaserToggle = NGUI_Utils.CreateToggle(transform, new Vector3(-300f, 350f), new Vector3Int(200, 42, 1), "HasTaser");
			hasTaserToggle.gameObject.name = "HasTaserToggle";
			hasTaserToggle.onClick += (state) => SetGlobalPropertyWithToggle("HasTaser", hasTaserToggle.toggle);
		}
		void CreateHasJetpackToggle()
		{
			hasJetpackToggle = NGUI_Utils.CreateToggle(transform, new Vector3(40f, 350f), new Vector3Int(200, 42, 1), "HasJetpack");
			hasJetpackToggle.gameObject.name = "HasJetpackToggle";
			hasJetpackToggle.onClick += (state) => SetGlobalPropertyWithToggle("HasJetpack", hasJetpackToggle.toggle);
		}
		void CreateDeathYLimitField()
		{
			UILabel deathYLimitLabel = NGUI_Utils.CreateLabel(transform, new Vector3(-300, 270), new Vector3Int(350, 50, 0), "DeathYLimit");
			deathYLimitLabel.name = "DeathYLimitLabel";
			deathYLimitLabel.depth = 1;
			deathYLimitLabel.fontSize = 30;

			deathYLimitField = NGUI_Utils.CreateInputField(transform, new Vector3(150f, 270f, 0f),
				new Vector3Int(200, 50, 0), 30, "100", inputType: UICustomInputField.UIInputType.NON_NEGATIVE_FLOAT);
			deathYLimitField.name = "DeathYLimit";
			deathYLimitField.onChange += () => SetGlobalPropertyWithInput("DeathYLimit", deathYLimitField);

			visualizeDeathYLimitButton = NGUI_Utils.CreateButtonAsToggleWithSprite(transform,
				new Vector3(285f, 270f, 0f), new Vector3Int(48, 48, 1), 1, "WhiteSquare", Vector2Int.one * 20);
			visualizeDeathYLimitButton.name = "VisualizeDeathYLimitBtnToggle";
			visualizeDeathYLimitButton.GetComponent<UIButtonScale>().hover = Vector3.one * 1.05f;
			visualizeDeathYLimitButton.GetComponent<UIButtonScale>().pressed = Vector3.one * 1.02f;
			visualizeDeathYLimitButton.onClick += OnVisualizeDeathYLimitToggleClick;
		}
		void CreateLevelSkyboxDropdown()
		{
			skyboxDropdown = NGUI_Utils.CreateDropdown(transform, new Vector3(0f, 160f), Vector3.one * 0.8f);
			skyboxDropdown.gameObject.name = "SkyboxDropdown";
			skyboxDropdown.SetTitle("Skybox");
			skyboxDropdown.AddOption("Chapter 1", false);
			skyboxDropdown.AddOption("Chapter 2", false);
			skyboxDropdown.AddOption("Chapter 3 & 4", false);
			skyboxDropdown.AddOption("Menu", true);
			skyboxDropdown.AddOption("Chapter 1 (0.53)", false);
			skyboxDropdown.AddOption("Chapter 2 (0.53)", false);
			skyboxDropdown.AddOption("Chapter 3 (0.53)", false);
			skyboxDropdown.AddOption("Chapter 4 & Menu (0.53)", false);
			skyboxDropdown.AddOption("Chapter 1 (PE)", false);
			skyboxDropdown.AddOption("Chapter 2 (PE)", false);
			skyboxDropdown.AddOption("Chapter 3 (PE)", false);
			skyboxDropdown.AddOption("Chapter 4 (PE)", false);
			skyboxDropdown.AddOption("Chapter 5 (PE)", false);
			skyboxDropdown.AddOnChangeOption((id) => SetGlobalPropertyWithDropdown("Skybox", id));
		}
		#region Upgrades UI
		void CreateUpgradesButton()
		{
			UIButtonPatcher upgradesButton = NGUI_Utils.CreateButton(transform, new Vector3(0f, 50f), new Vector3Int(300, 50, 0), "Player Upgrades");
			upgradesButton.name = "UpgradesButton";
			upgradesButton.buttonSprite.depth = 1;
			upgradesButton.buttonLabel.fontSize = 28;
			upgradesButton.GetComponent<UIButtonScale>().hover = Vector3.one * 1.05f;
			upgradesButton.GetComponent<UIButtonScale>().pressed = Vector3.one * 0.95f;
			upgradesButton.onClick += () => UpgradesPanel.Instance.ShowUpgradesPanel();
		}
		#endregion

		public void ShowOrHideGlobalPropertiesPanel()
		{
			if (!EditorUIManager.IsCurrentUIContext(EditorUIContext.GLOBAL_PROPERTIES))
			{
				EditorUIManager.Instance.SetEditorUIContext(EditorUIContext.GLOBAL_PROPERTIES);
			}
			else
			{
				EditorUIManager.Instance.SetEditorUIContext(EditorUIContext.NORMAL);
			}
		}
		public void RefreshGlobalPropertiesPanelValues()
		{
			GameObject panel = gameObject;

			panel.GetChild("HasTaserToggle").GetComponent<UIToggle>().Set((bool)GetGlobalProperty("HasTaser"));
			panel.GetChild("HasJetpackToggle").GetComponent<UIToggle>().Set((bool)GetGlobalProperty("HasJetpack"));
			panel.GetChild("DeathYLimit").GetComponent<UIInput>().text = (float)GetGlobalProperty("DeathYLimit") + "";
			panel.GetChild("SkyboxDropdown").GetComponent<UIDropdownPatcher>().SelectOption((int)GetGlobalProperty("Skybox"));
		}

		public void SetGlobalPropertyWithToggle(string name, UIToggle toggle)
		{
			SetGlobalProperty(name, toggle.isChecked);
		}
		public void SetGlobalPropertyWithInput(string propertyName, UICustomInputField inputField)
		{
			// ParseInputFieldData returns true if the introduced data CAN be parsed.
			if (ParseInputFieldData(inputField.name, inputField.GetText(), out object parsedData))
			{
				EditorController.Instance.levelHasBeenModified = true;
				SetGlobalProperty(propertyName, parsedData);
				inputField.Set(true);
			}
			else
			{
				inputField.Set(false);
			}
		}
		bool ParseInputFieldData(string inputFieldName, string fieldText, out object parsedData)
		{
			switch (inputFieldName)
			{
				case "DeathYLimit":
					bool toReturn = Utils.TryParseFloat(fieldText, out float result);
					parsedData = result;
					return toReturn;
			}

			parsedData = null;
			return false;
		}
		public void SetGlobalPropertyWithDropdown(string propertyName, int selectedID)
		{
			SetGlobalProperty(propertyName, selectedID);
		}
		public void SetGlobalProperty(string name, object value)
		{
			if (EditorController.Instance.globalProperties.ContainsKey(name))
			{
				if (EditorController.Instance.globalProperties[name].GetType().Name == value.GetType().Name)
				{
					EditorController.Instance.globalProperties[name] = value;
					EditorController.Instance.levelHasBeenModified = true;

					if (name == "Skybox")
					{
						EditorController.Instance.SetupSkybox((int)value);
					}
				}
			}
		}
		public object GetGlobalProperty(string name)
		{
			if (EditorController.Instance.globalProperties.ContainsKey(name))
			{
				return EditorController.Instance.globalProperties[name];
			}

			return null;
		}

		// Methods for "special" UI elements, such as buttons.
		void OnVisualizeDeathYLimitToggleClick(bool newState)
		{
			EditorController.Instance.deathYPlane.gameObject.SetActive(newState);
		}
	}
}