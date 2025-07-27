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
    public class TextEditorUI : MonoBehaviour
    {
        public static TextEditorUI Instance;
        LE_Object targetObj;

        public GameObject editorPanel;
        UILabel windowTitle;
        UILabel textFieldTitle;
        UICustomInputField textField;
        UITogglePatcher autoFontSizeToggle;
        UILabel fontSizeLabel;
        UICustomInputField fontSizeField;
        UILabel minFontSizeLabel;
        UICustomInputField minFontSizeField;
        UILabel maxFontSizeLabel;
        UICustomInputField maxFontSizeField;

        public static void Create()
        {
            if (Instance)
            {
                Logger.Error("Another instance of TextEditorUI is already created.");
                return;
            }

            Instance = new GameObject("TextEditorUI").AddComponent<TextEditorUI>();
        }

        void Awake()
        {
            CreateTextEditorPanel();
            CreateTextFieldTitle();
            CreateTextField();
            CreateAutoFontSizeToggle();
            CreateFontSizeField();
            CreateMinFontSizeField();
            CreateMaxFontSizeField();
        }

        void CreateTextEditorPanel()
        {
            editorPanel = Instantiate(NGUI_Utils.optionsPanel, EditorUIManager.Instance.editorUIParent.transform);
            editorPanel.name = "TextEditorPanel";

            windowTitle = editorPanel.GetChild("Title").GetComponent<UILabel>();
            windowTitle.gameObject.RemoveComponent<UILocalize>();

            foreach (var child in editorPanel.GetChilds())
            {
                string[] notDelete = { "Window", "Title" };
                if (notDelete.Contains(child.name)) continue;

                Destroy(child);
            }

            editorPanel.transform.GetChild("Window").transform.localPosition = Vector3.zero;
            windowTitle.transform.localPosition = new Vector3(0f, 386.4f, 0f);

            // Remove the OptionsController and UILocalize components so I can change the title of the panel. Also the TweenAlpha since it won't be needed.
            editorPanel.RemoveComponent<OptionsController>();
            editorPanel.RemoveComponent<TweenAlpha>();

            // Change the title properties of the panel.
            windowTitle.transform.localPosition = new Vector3(0, 387, 0);
            windowTitle.GetComponent<UILabel>().width = 1650;
            windowTitle.GetComponent<UILabel>().height = 50;
            windowTitle.GetComponent<UILabel>().text = "Events";

            // Reset the scale of the new custom menu to one.
            editorPanel.transform.localScale = Vector3.one;

            // Add a UIPanel so the TweenScale can work.
            // UPDATE: It already has an UIPanel LOL.
            UIPanel panel = editorPanel.GetComponent<UIPanel>();
            panel.alpha = 1f;
            panel.depth = 1;
            editorPanel.GetComponent<TweenAlpha>().mRect = panel;

            // Change the animation.
            editorPanel.GetComponent<TweenScale>().from = Vector3.zero;
            editorPanel.GetComponent<TweenScale>().to = Vector3.one;

            // For some reason sometimes the window sprite can be transparent, force it to be opaque.
            editorPanel.GetChild("Window").GetComponent<UISprite>().alpha = 1f;

            // Add a collider so the user can't interact with the other objects.
            editorPanel.AddComponent<BoxCollider>().size = new Vector3(100000f, 100000f, 1f);

            // We use the occluder from the pause menu, since when you open this editor, we set the editor state to paused.
        }
        void CreateTextFieldTitle()
        {
            textFieldTitle = NGUI_Utils.CreateLabel(editorPanel.transform, Vector3.up * 125, new Vector3Int(1600, 38, 0), "TEXT",
                NGUIText.Alignment.Center, UIWidget.Pivot.Center);
            textFieldTitle.fontSize = 40;
        }
        void CreateTextField()
        {
            textField = NGUI_Utils.CreateInputField(editorPanel.transform, new Vector3(0, -150), new Vector3Int(1600, 500, 0),
                27, "", false, inputType: UICustomInputField.UIInputType.PLAIN_TEXT, depth: 5);
            textField.name = "TextField";
            textField.input.mPivot = UIWidget.Pivot.TopLeft;
            textField.input.onReturnKey = UIInput.OnReturnKey.NewLine;

            textField.onSubmit += OnTextFieldSubmited;
        }
        void CreateAutoFontSizeToggle()
        {
            GameObject toggle = NGUI_Utils.CreateToggle(editorPanel.transform, new Vector3(-600, 250), new Vector3Int(250, 48, 0),
                "Auto Font Size");
            toggle.name = "AutoFontSizeToggle";
            autoFontSizeToggle = toggle.AddComponent<UITogglePatcher>();
            autoFontSizeToggle.onClick += OnAutoFontSizeToggleChanged;
        }
        void CreateFontSizeField()
        {
            fontSizeLabel = NGUI_Utils.CreateLabel(editorPanel.transform, Vector3.up * 265, new Vector3Int(200,
                NGUI_Utils.defaultLabelSize.y, 0), "Font Size", NGUIText.Alignment.Center, UIWidget.Pivot.Center);
            fontSizeLabel.name = "FontSizeLabel";

            fontSizeField = NGUI_Utils.CreateInputField(editorPanel.transform, Vector3.up * 225, new Vector3Int(200,
                NGUI_Utils.defaultLabelSize.y, 0), 27, "185", false, NGUIText.Alignment.Left, UICustomInputField.UIInputType.NON_NEGATIVE_FLOAT);
            fontSizeField.name = "FontSizeField";
            fontSizeField.onChange = OnFontSizeFieldChanged;
        }
        void CreateMinFontSizeField()
        {
            minFontSizeLabel = NGUI_Utils.CreateLabel(editorPanel.transform, Vector3.up * 265, new Vector3Int(200,
                NGUI_Utils.defaultLabelSize.y, 0), "Min Font Size", NGUIText.Alignment.Center, UIWidget.Pivot.Center);
            minFontSizeLabel.name = "MinFontSizeLabel";

            minFontSizeField = NGUI_Utils.CreateInputField(editorPanel.transform, Vector3.up * 225, new Vector3Int(200,
                NGUI_Utils.defaultLabelSize.y, 0), 27, "185", false, NGUIText.Alignment.Left, UICustomInputField.UIInputType.NON_NEGATIVE_FLOAT);
            minFontSizeField.name = "MinFontSizeField";
            minFontSizeField.onChange = OnMinFontSizeFieldChanged;
        }
        void CreateMaxFontSizeField()
        {
            maxFontSizeLabel = NGUI_Utils.CreateLabel(editorPanel.transform, new Vector3(300, 265), new Vector3Int(200,
                NGUI_Utils.defaultLabelSize.y, 0), "Max Font Size", NGUIText.Alignment.Center, UIWidget.Pivot.Center);
            maxFontSizeLabel.name = "MaxFontSizeLabel";

            maxFontSizeField = NGUI_Utils.CreateInputField(editorPanel.transform, new Vector3(300, 225), new Vector3Int(200,
                NGUI_Utils.defaultLabelSize.y, 0), 27, "185", false, NGUIText.Alignment.Left, UICustomInputField.UIInputType.NON_NEGATIVE_FLOAT);
            maxFontSizeField.name = "MaxFontSizeField";
            maxFontSizeField.onChange = OnMaxFontSizeFieldChanged;
        }

        void UpdateTextEditorUIValues()
        {
            textField.SetText(targetObj.GetProperty<string>("Text"));
            autoFontSizeToggle.toggle.Set(targetObj.GetProperty<bool>("AutoFontSize"));
            fontSizeField.SetText(targetObj.GetProperty<float>("FontSize"));
            minFontSizeField.SetText(targetObj.GetProperty<float>("MinFontSize"));
            maxFontSizeField.SetText(targetObj.GetProperty<float>("MaxFontSize"));
        }

        void OnTextFieldSubmited()
        {
            targetObj.SetProperty("Text", textField.GetText());
        }
        void OnAutoFontSizeToggleChanged()
        {
            targetObj.SetProperty("AutoFontSize", autoFontSizeToggle.toggle.isChecked);

            if (autoFontSizeToggle.toggle.isChecked)
            {
                fontSizeLabel.gameObject.SetActive(false);
                fontSizeField.gameObject.SetActive(false);

                minFontSizeLabel.gameObject.SetActive(true);
                minFontSizeField.gameObject.SetActive(true);

                maxFontSizeLabel.gameObject.SetActive(true);
                maxFontSizeField.gameObject.SetActive(true);
            }
            else
            {
                fontSizeLabel.gameObject.SetActive(true);
                fontSizeField.gameObject.SetActive(true);

                minFontSizeLabel.gameObject.SetActive(false);
                minFontSizeField.gameObject.SetActive(false);

                maxFontSizeLabel.gameObject.SetActive(false);
                maxFontSizeField.gameObject.SetActive(false);
            }
        }
        void OnFontSizeFieldChanged()
        {
            targetObj.SetProperty("FontSize", fontSizeField.GetText());
        }
        void OnMinFontSizeFieldChanged()
        {
            targetObj.SetProperty("MinFontSize", minFontSizeField.GetText());
        }
        void OnMaxFontSizeFieldChanged()
        {
            targetObj.SetProperty("MaxFontSize", maxFontSizeField.GetText());
        }

        public void ShowTextEditor(LE_Object targetObj)
        {
            this.targetObj = targetObj;
            windowTitle.text = "Text Editor for " + targetObj.objectFullNameWithID;

            EditorController.Instance.SetCurrentEditorState(EditorState.PAUSED);
            EditorUIManager.Instance.SetEditorUIContext(EditorUIContext.TEXT_EDITOR);

            UpdateTextEditorUIValues();
        }
        public void HideTextEditor()
        {
            textField.input.Submit(); // Force it to submit unsaved changes.

            targetObj.TriggerAction("OnTextEditorClose");

            EditorController.Instance.SetCurrentEditorState(EditorState.NORMAL);
            EditorUIManager.Instance.SetEditorUIContext(EditorUIContext.NORMAL);
        }
    }
}
