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
        UIToggle hasTaserToggle;
        UIToggle hasJetpackToggle;
        UICustomInputField deathYLimitField;
        UIButtonAsToggle visualizeDeathYLimitButton;
        UIDropdownPatcher skyboxDropdown;
        // -----------------------------
        GameObject upgradesParent;
        int createdUpgradesUICount = 0;

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

            CreateUpgradesParent();
            CreateUpgradesTitle();
            CreateDodgeUI();
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
            GameObject hasTaserToggle = NGUI_Utils.CreateToggle(transform, new Vector3(-300f, 350f), new Vector3Int(200, 42, 1), "HasTaser");
            hasTaserToggle.name = "HasTaserToggle";
            EventDelegate hasTaserDelegate = NGUI_Utils.CreateEvenDelegate(this, nameof(SetGlobalPropertyWithToggle),
                NGUI_Utils.CreateEventDelegateParamter(this, "name", "HasTaser"),
                NGUI_Utils.CreateEventDelegateParamter(this, "toggle", hasTaserToggle.GetComponent<UIToggle>()));
            hasTaserToggle.GetComponent<UIToggle>().onChange.Clear();
            hasTaserToggle.GetComponent<UIToggle>().onChange.Add(hasTaserDelegate);
            this.hasTaserToggle = hasTaserToggle.GetComponent<UIToggle>();
        }
        void CreateHasJetpackToggle()
        {
            GameObject hasJetpackToggle = NGUI_Utils.CreateToggle(transform,
                new Vector3(40f, 350f), new Vector3Int(200, 42, 1), "HasJetpack");
            hasJetpackToggle.name = "HasJetpackToggle";
            EventDelegate hasJetpackDelegate = NGUI_Utils.CreateEvenDelegate(this, nameof(SetGlobalPropertyWithToggle),
                NGUI_Utils.CreateEventDelegateParamter(this, "name", "HasJetpack"),
                NGUI_Utils.CreateEventDelegateParamter(this, "toggle", hasJetpackToggle.GetComponent<UIToggle>()));
            hasJetpackToggle.GetComponent<UIToggle>().onChange.Clear();
            hasJetpackToggle.GetComponent<UIToggle>().onChange.Add(hasJetpackDelegate);
            this.hasJetpackToggle = hasJetpackToggle.GetComponent<UIToggle>();
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
            skyboxDropdown.AddOption("Chapter 1", true);
            skyboxDropdown.AddOption("Chapter 2", false);
            skyboxDropdown.AddOption("Chapter 3 & 4", false);

            skyboxDropdown.AddOnChangeOption((id) => SetGlobalPropertyWithDropdown("Skybox", id));
        }
        #region Upgrades UI
        void CreateUpgradesParent()
        {
            upgradesParent = new GameObject("Upgrades");
            upgradesParent.transform.parent = transform;
            upgradesParent.transform.localPosition = Vector3.zero;
            upgradesParent.transform.localScale = Vector3.one;
        }
        void CreateUpgradesTitle()
        {
            UILabel title = NGUI_Utils.CreateLabel(upgradesParent.transform, new Vector3(0, 100), new Vector3Int(600, 48, 0),
                "Upgrades", NGUIText.Alignment.Center, UIWidget.Pivot.Center);
            title.name = "Title";
            title.fontSize = 35;
            title.color = NGUI_Utils.fsLabelDefaultColor;
        }
        void CreateDodgeUI()
        {
            CreateUpgradeUI(UpgradeType.DODGE);
        }
        void CreateUpgradeUI(UpgradeType type)
        {
            GameObject parent = new GameObject(type.ToString());
            parent.transform.parent = upgradesParent.transform;
            parent.transform.localPosition = new Vector3(0, 20 - (100 * createdUpgradesUICount));
            parent.transform.localScale = Vector3.one;

            string locKey = "Upgrade_" + UpgradeSaveData.ConvertTypeToFSType(type) + "_Title";
            bool isAOneTimeSkill = Controls.IsSkill(UpgradeSaveData.ConvertTypeToFSType(type).Value);

            Vector3 togglePos = isAOneTimeSkill ? Vector3.zero : new Vector3(-300, 0);
            GameObject toggle = NGUI_Utils.CreateToggle(parent.transform, togglePos, new Vector3Int(200, 48, 0), locKey);
            EventDelegate toggleDelegate = NGUI_Utils.CreateEvenDelegate(this, nameof(SetUpgradeEnabledState),
                NGUI_Utils.CreateEventDelegateParamter(this, "type", (int)type),
                NGUI_Utils.CreateEventDelegateParamter(this, "toggle", toggle.GetComponent<UIToggle>()));
            toggle.GetComponent<UIToggle>().onChange.Add(toggleDelegate);

            if (!isAOneTimeSkill) // One-Time Skills don't have levels.
            {
                UIButtonMultiple levelButton = NGUI_Utils.CreateButtonMultiple(parent.transform, new Vector3(160, 15), Vector3.one * 0.8f, 1);
                levelButton.name = "LevelButton";
                levelButton.SetTitle("Level");
                for (int i = 0; i < Controls.GetMaxLevelFor(UpgradeSaveData.ConvertTypeToFSType(type).Value); i++)
                {
                    bool setAsSelected = i == 0;
                    levelButton.AddOption("Level " + (i + 1), setAsSelected);
                }

                levelButton.onClick += (id) => SetUpgradeLevel((int)type, levelButton);
            }

            createdUpgradesUICount++;
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

            panel.GetChildWithName("HasTaserToggle").GetComponent<UIToggle>().Set((bool)GetGlobalProperty("HasTaser"));
            panel.GetChildWithName("HasJetpackToggle").GetComponent<UIToggle>().Set((bool)GetGlobalProperty("HasJetpack"));
            panel.GetChildWithName("DeathYLimit").GetComponent<UIInput>().text = (float)GetGlobalProperty("DeathYLimit") + "";
            panel.GetChildWithName("SkyboxDropdown").GetComponent<UIDropdownPatcher>().SelectOption((int)GetGlobalProperty("Skybox"));

            UpdateUpgradesUI();
        }
        void UpdateUpgradesUI()
        {
            var upgradesParent = gameObject.GetChildWithName("Upgrades");

            // Start at 1 to skip the title.
            for (int i = 1; i < upgradesParent.transform.childCount; i++)
            {
                var upgradeParent = upgradesParent.transform.GetChild(i);
                var upgradeType = Enum.Parse<UpgradeType>(upgradeParent.name);
                var upgradeData = ((List<UpgradeSaveData>)EditorController.Instance.globalProperties["Upgrades"]).Find(x => x.type == upgradeType);

                upgradeParent.gameObject.GetChildWithName("Toggle").GetComponent<UIToggle>().Set(upgradeData.active);
                upgradeParent.gameObject.GetChildWithName("LevelButton").GetComponent<UIButtonMultiple>().SelectOption(upgradeData.level - 1);
            }
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
                    bool toReturn = Utilities.TryParseFloat(fieldText, out float result);
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

        public void SetUpgradeEnabledState(int typeID, UIToggle toggle)
        {
            var list = (List<UpgradeSaveData>)EditorController.Instance.globalProperties["Upgrades"];
            var typeToModify = list.Find(x => x.type == (UpgradeType)typeID);

            typeToModify.active = toggle.isChecked;
        }
        // Make the STUPID BUTTON VARIABLE AN OBJECT, CAUSE OTHERWISE ML GETS MAD (PD: Fuck ML).
        public void SetUpgradeLevel(int typeID, object button)
        {
            var list = (List<UpgradeSaveData>)EditorController.Instance.globalProperties["Upgrades"];
            var typeToModify = list.Find(x => x.type == (UpgradeType)typeID);

            // The selected ID is according to the upgrade level.
            typeToModify.level = ((UIButtonMultiple)button).currentSelectedID + 1;
        }

        // Methods for "special" UI elements, such as buttons.
        void OnVisualizeDeathYLimitToggleClick(bool newState)
        {
            EditorController.Instance.deathYPlane.gameObject.SetActive(newState);
        }

        public void RefreshLocalization()
        {
            skyboxDropdown.RefreshLocalization();
        }
    }
}
