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
        UITogglePatcher hasFlashlight;
        UITogglePatcher debugAllowed;
        UICustomInputField deathYLimitField;
		UIButtonAsToggle visualizeDeathYLimitButton;
		UIDropdownPatcher skyboxDropdown;
		UIDropdownPatcher musicDropdown;

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
			CreateHasFlashlightToggle();
			CreateAllowDebugToggle();
			CreateDeathYLimitField();
			CreateLevelSkyboxDropdown();
			CreateLevelMusicDropdown();
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

			TweenPosition tween = gameObject.AddComponent<TweenPosition>();
			tween.from = new Vector2(1320, 0);
            tween.to = new Vector2(600, 0);
			tween.duration = 0.2f;
			tween.Play(false);
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
			hasTaserToggle.onClick += (state) => SetGlobalProperty("HasTaser", hasTaserToggle.isChecked);
		}
		void CreateHasJetpackToggle()
		{
			hasJetpackToggle = NGUI_Utils.CreateToggle(transform, new Vector3(40f, 350f), new Vector3Int(200, 42, 1), "HasJetpack");
			hasJetpackToggle.gameObject.name = "HasJetpackToggle";
			hasJetpackToggle.onClick += (state) => SetGlobalProperty("HasJetpack", hasJetpackToggle.isChecked);
		}
        void CreateHasFlashlightToggle()
        {
            hasFlashlight = NGUI_Utils.CreateToggle(transform, new Vector3(-300f, 270f), new Vector3Int(200, 42, 1), "HasFlashlight");
            hasFlashlight.gameObject.name = "hasFlashlightToggle";
            hasFlashlight.onClick += (state) => SetGlobalProperty("HasFlashlight", hasFlashlight.isChecked);
        }
        void CreateAllowDebugToggle()
        {
            debugAllowed = NGUI_Utils.CreateToggle(transform, new Vector3(40f, 270f), new Vector3Int(200, 42, 1), "DebugAllowed");
            debugAllowed.gameObject.name = "debugAllowedToggle";
            debugAllowed.onClick += (state) => SetGlobalProperty("DebugAllowed", debugAllowed.isChecked);
        }
        void CreateDeathYLimitField()
		{
			UILabel deathYLimitLabel = NGUI_Utils.CreateLabel(transform, new Vector3(-300, 160), new Vector3Int(350, 50, 0), "DeathYLimit");
			deathYLimitLabel.name = "DeathYLimitLabel";
			deathYLimitLabel.depth = 1;
			deathYLimitLabel.fontSize = 30;

			deathYLimitField = NGUI_Utils.CreateInputField(transform, new Vector3(150f, 160f, 0f),
				new Vector3Int(200, 50, 0), 30, "100", inputType: UICustomInputField.UIInputType.NON_NEGATIVE_FLOAT);
			deathYLimitField.name = "DeathYLimit";
			deathYLimitField.onChange += () => SetGlobalPropertyWithInput("DeathYLimit", deathYLimitField);

			visualizeDeathYLimitButton = NGUI_Utils.CreateButtonAsToggleWithSprite(transform,
				new Vector3(285f, 160f, 0f), new Vector3Int(48, 48, 1), 1, "WhiteSquare", Vector2Int.one * 20);
			visualizeDeathYLimitButton.name = "VisualizeDeathYLimitBtnToggle";
			visualizeDeathYLimitButton.GetComponent<UIButtonScale>().hover = Vector3.one * 1.05f;
			visualizeDeathYLimitButton.GetComponent<UIButtonScale>().pressed = Vector3.one * 1.02f;
			visualizeDeathYLimitButton.onClick += OnVisualizeDeathYLimitToggleClick;
		}
        void CreateLevelSkyboxDropdown()
        {
            skyboxDropdown = NGUI_Utils.CreateDropdown(transform, new Vector3(0f, 60f), Vector3.one * 0.8f);
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
            skyboxDropdown.AddOnChangeOption((id) => SetGlobalProperty("Skybox", id));
        }
        void CreateLevelMusicDropdown()
        {
            musicDropdown = NGUI_Utils.CreateDropdown(transform, new Vector3(0f, -40f), Vector3.one * 0.8f);
            musicDropdown.gameObject.name = "MusicDropdown";
            musicDropdown.SetTitle("Music");
            musicDropdown.AddOption("Chapter 1 PE", false);
            musicDropdown.AddOption("Chapter 2 OLD", false);
            musicDropdown.AddOption("Chapter 2", false);
            musicDropdown.AddOption("Chapter 3", false);
            musicDropdown.AddOption("Chapter 4", true);
            musicDropdown.AddOption("Chapter 5 PE", false);
            musicDropdown.AddOption("Fractaloween", false);
            musicDropdown.AddOption("Fractalentine", false);
            musicDropdown.AddOption("FractalXMAS", false);
            musicDropdown.AddOption("Space Run 3D", false);

            musicDropdown.AddOnChangeOption((id) => SetGlobalProperty("Music", id));
        }
        void CreateUpgradesButton()
        {
            UIButtonPatcher upgradesButton = NGUI_Utils.CreateButton(transform, new Vector3(0f, -110f), new Vector3Int(300, 50, 0), "Player Upgrades");
            upgradesButton.name = "UpgradesButton";
            upgradesButton.buttonSprite.depth = 1;
            upgradesButton.buttonLabel.fontSize = 28;
            upgradesButton.GetComponent<UIButtonScale>().hover = Vector3.one * 1.05f;
            upgradesButton.GetComponent<UIButtonScale>().pressed = Vector3.one * 0.95f;
            upgradesButton.onClick += () => UpgradesPanel.Instance.ShowUpgradesPanel();
        }

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

			hasTaserToggle.Set((bool)GetGlobalProperty("HasTaser"), false, true);
			hasJetpackToggle.Set((bool)GetGlobalProperty("HasJetpack"), false, true);
            hasFlashlight.Set((bool)GetGlobalProperty("HasFlashlight"), false, true);
            debugAllowed.Set((bool)GetGlobalProperty("DebugAllowed"), false, true);
            deathYLimitField.SetText((float)GetGlobalProperty("DeathYLimit"), false);
			skyboxDropdown.SelectOption((int)GetGlobalProperty("Skybox"));
            musicDropdown.SelectOption((int)GetGlobalProperty("Music"));
        }

		public void SetGlobalPropertyWithInput(string propertyName, UICustomInputField inputField)
		{
			if (Utils.TryParseFloat(inputField.GetText(), out float parsedData))
			{
				EditorController.Instance.levelHasBeenModified = true;
				SetGlobalProperty(propertyName, parsedData);
			}
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
					else if (name == "Music")
					{
						EditorController.Instance.SetupLevelMusic((int)value);
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