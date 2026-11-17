using FS_LevelEditor.UI_Related;
using HarmonyLib;
using UnityEngine;

namespace FS_LevelEditor.Editor.UI
{
    public class FogPanel : MonoBehaviour
    {
        public static FogPanel Instance;

        public GameObject fogPanel;
        UILabel title;

        UITogglePatcher fogToggle;
        UICustomInputField colorField;
        UICustomInputField densityField;
        UICustomInputField startField;
        UICustomInputField endField;
        UIButtonAsToggle previewButton;

        // Preview state. The original RenderSettings are saved when the preview starts and restored when it stops.
        bool previewActive;

        const float WINDOW_WIDTH = 900f;
        const float WINDOW_HEIGHT = 640f;
        const float LABEL_X = -380f;
        const float FIELD_X = 210f;

        public static void Create()
        {
            if (Instance == null)
            {
                Instance = new GameObject("FogPanelManager").AddComponent<FogPanel>();
                Instance.CreatePanel();
                Instance.CreateControls();
            }
        }

        void OnDestroy()
        {
            StopPreview();
            Instance = null;
        }

        void Update()
        {
            // The panel can be hidden by other means than Hide() (context switches), never leave the preview fog on in that case.
            if (previewActive && (fogPanel == null || !fogPanel.activeInHierarchy))
            {
                StopPreview();
                previewButton.SetToggleState(false);
            }
        }

        #region Create UI
        void CreatePanel()
        {
            fogPanel = Instantiate(NGUI_Utils.optionsPanel, EditorUIManager.Instance.editorUIParent.transform);
            fogPanel.name = "FogPanel";

            title = fogPanel.GetChild("Title").GetComponent<UILabel>();
            title.gameObject.RemoveComponent<UILocalize>();

            foreach (var child in fogPanel.GetChilds())
            {
                string[] notDelete = { "Window", "Title" };
                if (notDelete.Contains(child.name)) continue;

                Destroy(child);
            }

            fogPanel.RemoveComponent<OptionsController>();
            fogPanel.RemoveComponent<TweenAlpha>();

            GameObject window = fogPanel.GetChild("Window");
            window.transform.localPosition = Vector3.zero;
            UISprite windowSprite = window.GetComponent<UISprite>();
            windowSprite.width = (int)WINDOW_WIDTH;
            windowSprite.height = (int)WINDOW_HEIGHT;
            windowSprite.alpha = 0.3f; // Semi-transparent so the level (and the fog preview) can be seen behind it.

            title.transform.localPosition = new Vector3(0f, WINDOW_HEIGHT / 2f - 50f, 0f);
            title.width = (int)WINDOW_WIDTH - 40;
            title.height = 60;
            title.fontSize = 42;
            title.font = NGUI_Utils.juraFont ?? NGUI_Utils.labelFont;
            title.text = "Fog";

            fogPanel.transform.localScale = Vector3.one;

            UIPanel panel = fogPanel.GetComponent<UIPanel>();
            panel.alpha = 1f;
            panel.depth = 1;
            AccessTools.Field(typeof(TweenAlpha), "mRect").SetValue(fogPanel.GetComponent<TweenAlpha>(), panel);

            fogPanel.GetComponent<TweenScale>().from = Vector3.zero;
            fogPanel.GetComponent<TweenScale>().to = Vector3.one;

            // Blocks clicks from reaching the editor behind the panel.
            fogPanel.AddComponent<BoxCollider>().size = new Vector3(100000f, 100000f, 1f);

            fogPanel.SetActive(false);
        }

        void CreateControls()
        {
            fogToggle = NGUI_Utils.CreateToggle(fogPanel.transform, new Vector3(LABEL_X, 170f), new Vector3Int(300, 42, 1), "Fog");
            fogToggle.gameObject.name = "FogToggle";
            fogToggle.onClick += (state) =>
            {
                SetProperty("GlobalFog", fogToggle.isChecked);
                RefreshPreview();
            };

            colorField = CreateRow("FogColor", 90f, "#808080", UICustomInputField.UIInputType.HEX_COLOR);
            colorField.onChange += () =>
            {
                Color? color = Utils.HexToColor(colorField.GetText(), false, null);
                if (color.HasValue)
                {
                    SetProperty("GlobalFogColor", Utils.ColorToHex(color.Value));
                    RefreshPreview();
                }
            };

            densityField = CreateRow("FogDensity", 20f, "0.01", UICustomInputField.UIInputType.NON_NEGATIVE_FLOAT, 3);
            densityField.onChange += () => OnFloatFieldChanged("GlobalFogDensity", densityField);

            startField = CreateRow("FogStartDistance", -50f, "0", UICustomInputField.UIInputType.NON_NEGATIVE_FLOAT);
            startField.onChange += () => OnFloatFieldChanged("GlobalFogStartDistance", startField);

            endField = CreateRow("FogEndDistance", -120f, "300", UICustomInputField.UIInputType.NON_NEGATIVE_FLOAT);
            endField.onChange += () => OnFloatFieldChanged("GlobalFogEndDistance", endField);

            previewButton = NGUI_Utils.CreateButtonAsToggle(fogPanel.transform, new Vector3(0f, -210f), new Vector3Int(400, 50, 0), "Preview Fog", 2);
            previewButton.name = "PreviewFogButton";
            previewButton.GetComponent<UIButtonScale>().hover = Vector3.one * 1.05f;
            previewButton.GetComponent<UIButtonScale>().pressed = Vector3.one * 0.95f;
            previewButton.onClick += (state) =>
            {
                if (state) StartPreview();
                else StopPreview();
            };

            UIButtonPatcher doneButton = NGUI_Utils.CreateButton(fogPanel.transform, new Vector3(0f, -275f), new Vector3Int(240, 50, 0), "Done", 2);
            doneButton.name = "DoneButton";
            doneButton.onClick += Hide;
        }

        UICustomInputField CreateRow(string locKey, float y, string defaultText, UICustomInputField.UIInputType inputType, int maxDecimals = 0)
        {
            UILabel label = NGUI_Utils.CreateLabel(fogPanel.transform, new Vector3(LABEL_X, y), new Vector3Int(450, 50, 0), locKey);
            label.name = locKey + "Label";
            label.depth = 2;
            label.fontSize = 30;

            UICustomInputField field = NGUI_Utils.CreateInputField(fogPanel.transform, new Vector3(FIELD_X, y, 0f),
                new Vector3Int(240, 50, 0), 30, defaultText, inputType: inputType, maxDecimals: maxDecimals, depth: 2);
            field.name = locKey;
            return field;
        }
        #endregion

        #region Show / Hide
        public void Show()
        {
            EditorController.Instance.SetCurrentEditorState(EditorState.PAUSED);
            EditorUIManager.Instance.SetEditorUIContext(EditorUIContext.FOG_PANEL);

            RefreshValues();
            previewButton.SetToggleState(false);
        }

        public void Hide()
        {
            StopPreview();
            previewButton.SetToggleState(false);

            EditorController.Instance.SetCurrentEditorState(EditorState.NORMAL);
            EditorUIManager.Instance.SetEditorUIContext(EditorUIContext.NORMAL);
        }
        #endregion

        void RefreshValues()
        {
            fogToggle.Set((bool)GetProperty("GlobalFog"), false, true);
            colorField.SetText((string)GetProperty("GlobalFogColor"), false);
            densityField.SetText((float)GetProperty("GlobalFogDensity"), false);
            startField.SetText((float)GetProperty("GlobalFogStartDistance"), false);
            endField.SetText((float)GetProperty("GlobalFogEndDistance"), false);
        }

        void OnFloatFieldChanged(string property, UICustomInputField field)
        {
            if (Utils.TryParseFloat(field.GetText(), out float value))
            {
                SetProperty(property, value);
                RefreshPreview();
            }
        }

        #region Preview
        void StartPreview()
        {
            previewActive = true;
            RefreshPreview();
        }

        void StopPreview()
        {
            if (!previewActive) return;

            LevelFog.Clear();
            previewActive = false;
        }
        
        void RefreshPreview()
        {
            if (!previewActive) return;

            LevelFog.Apply((bool)GetProperty("GlobalFog"), (string)GetProperty("GlobalFogColor"), (float)GetProperty("GlobalFogDensity"),
                (float)GetProperty("GlobalFogStartDistance"), (float)GetProperty("GlobalFogEndDistance"));
        }
        #endregion

        object GetProperty(string name) => GlobalPropertiesPanel.Instance.GetGlobalProperty(name);
        void SetProperty(string name, object value) => GlobalPropertiesPanel.Instance.SetGlobalProperty(name, value);
    }
}
