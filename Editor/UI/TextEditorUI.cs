using FS_LevelEditor.UI_Related;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace FS_LevelEditor.Editor.UI
{

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


        GameObject textAlignButtonsContainer;
        UIButtonAsToggle textTopLeft, textTop, textTopRight;
        UIButtonAsToggle textLeft, textCenter, textRight;
        UIButtonAsToggle textBottomLeft, textBottom, textBottomRight;


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
            CreateTextAlignmentStuff();
        }

        void OnDestroy()
        {
            Instance = null;
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
            AccessTools.Field(typeof(TweenAlpha), "mRect").SetValue(editorPanel.GetComponent<TweenAlpha>(), panel);

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
            AccessTools.Field(typeof(UIInput), "mPivot").SetValue(textField.input, UIWidget.Pivot.TopLeft);
            textField.input.onReturnKey = UIInput.OnReturnKey.NewLine;
            textField.input.selectAllTextOnFocus = false;

            textField.onSubmit += OnTextFieldSubmited;
        }
        void CreateAutoFontSizeToggle()
        {
            autoFontSizeToggle = NGUI_Utils.CreateToggle(editorPanel.transform, new Vector3(-600, 250), new Vector3Int(250, 48, 0),
                "Auto Font Size");
            autoFontSizeToggle.gameObject.name = "AutoFontSizeToggle";
            autoFontSizeToggle.onClick += (state) => OnAutoFontSizeToggleClicked(state);
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

        void CreateTextAlignmentStuff()
        {
            CreateTextAlignmentButtonsContainer();
            CreateTextAlignmentButtons();
        }
        void CreateTextAlignmentButtonsContainer()
        {
            textAlignButtonsContainer = new GameObject("TextAlignButtons");
            textAlignButtonsContainer.transform.parent = editorPanel.transform;
            textAlignButtonsContainer.transform.localPosition = new Vector3(600, 240);
            textAlignButtonsContainer.transform.localScale = Vector3.one;
        }
        void CreateTextAlignmentButtons()
        {
            textTopLeft = NGUI_Utils.CreateButtonAsToggleWithSprite(textAlignButtonsContainer.transform, new Vector3(-70, 70), Vector3Int.one * 50, 1, "Text_TopLeft", Vector2Int.one * 40);
            textTopLeft.name = "TopLeft";
            textTopLeft.onClick += (isChecked) => OnTextAlignmentButtonClicked(TextAlignmentOptions.TopLeft);
            //-----------------------------------
            textTop = NGUI_Utils.CreateButtonAsToggleWithSprite(textAlignButtonsContainer.transform, new Vector3(0, 70), Vector3Int.one * 50, 1, "Text_Top", Vector2Int.one * 40);
            textTop.name = "Top";
            textTop.onClick += (isChecked) => OnTextAlignmentButtonClicked(TextAlignmentOptions.Top);
            //-----------------------------------
            textTopRight = NGUI_Utils.CreateButtonAsToggleWithSprite(textAlignButtonsContainer.transform, new Vector3(70, 70), Vector3Int.one * 50, 1, "Text_TopRight", Vector2Int.one * 40);
            textTopRight.name = "TopRight";
            textTopRight.onClick += (isChecked) => OnTextAlignmentButtonClicked(TextAlignmentOptions.TopRight);


            textLeft = NGUI_Utils.CreateButtonAsToggleWithSprite(textAlignButtonsContainer.transform, new Vector3(-70, 0), Vector3Int.one * 50, 1, "Text_Left", Vector2Int.one * 40);
            textLeft.name = "Left";
            textLeft.onClick += (isChecked) => OnTextAlignmentButtonClicked(TextAlignmentOptions.Left);
            //-----------------------------------
            textCenter = NGUI_Utils.CreateButtonAsToggleWithSprite(textAlignButtonsContainer.transform, new Vector3(0, 0), Vector3Int.one * 50, 1, "Text_Center", Vector2Int.one * 40);
            textCenter.name = "Center";
            textCenter.onClick += (isChecked) => OnTextAlignmentButtonClicked(TextAlignmentOptions.Center);
            //-----------------------------------
            textRight = NGUI_Utils.CreateButtonAsToggleWithSprite(textAlignButtonsContainer.transform, new Vector3(70, 0), Vector3Int.one * 50, 1, "Text_Right", Vector2Int.one * 40);
            textRight.name = "Right";
            textRight.onClick += (isChecked) => OnTextAlignmentButtonClicked(TextAlignmentOptions.Right);


            textBottomLeft = NGUI_Utils.CreateButtonAsToggleWithSprite(textAlignButtonsContainer.transform, new Vector3(-70, -70), Vector3Int.one * 50, 1, "Text_BottomLeft", Vector2Int.one * 40);
            textBottomLeft.name = "Left";
            textBottomLeft.onClick += (isChecked) => OnTextAlignmentButtonClicked(TextAlignmentOptions.BottomLeft);
            //-----------------------------------
            textBottom = NGUI_Utils.CreateButtonAsToggleWithSprite(textAlignButtonsContainer.transform, new Vector3(0, -70), Vector3Int.one * 50, 1, "Text_Bottom", Vector2Int.one * 40);
            textBottom.name = "Center";
            textBottom.onClick += (isChecked) => OnTextAlignmentButtonClicked(TextAlignmentOptions.Bottom);
            //-----------------------------------
            textBottomRight = NGUI_Utils.CreateButtonAsToggleWithSprite(textAlignButtonsContainer.transform, new Vector3(70, -70), Vector3Int.one * 50, 1, "Text_BottomRight", Vector2Int.one * 40);
            textBottomRight.name = "Right";
            textBottomRight.onClick += (isChecked) => OnTextAlignmentButtonClicked(TextAlignmentOptions.BottomRight);
        }

        void UpdateTextEditorUIValues()
        {
            textField.SetText(targetObj.GetProperty<string>("Text"));
            bool isAuto = targetObj.GetProperty<bool>("AutoFontSize");
            autoFontSizeToggle.Set(isAuto);

            fontSizeField.SetText(targetObj.GetProperty<float>("FontSize"));
            minFontSizeField.SetText(targetObj.GetProperty<float>("MinFontSize"));
            maxFontSizeField.SetText(targetObj.GetProperty<float>("MaxFontSize"));
            UpdateTextAlignmentButtons(targetObj.GetProperty<TextAlignmentOptions>("TextAlign"));

            // Update the visibility of fields based on the AutoFontSize toggle state
            UpdateFontSizeFieldsVisibility(isAuto);
        }

        void OnTextFieldSubmited()
        {
            targetObj.SetProperty("Text", textField.GetText());
        }
        void OnAutoFontSizeToggleClicked(bool state)
        {
            // Save the data to the object
            targetObj.SetProperty("AutoFontSize", state);

            // Update the UI fields
            UpdateFontSizeFieldsVisibility(state);
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
        void OnTextAlignmentButtonClicked(TextAlignmentOptions alignment)
        {
            targetObj.SetProperty("TextAlign", alignment);

            UpdateTextAlignmentButtons(alignment);
        }

        void UpdateTextAlignmentButtons(TextAlignmentOptions alignment)
        {
            textTopLeft.SetToggleState(alignment == TextAlignmentOptions.TopLeft);
            textTop.SetToggleState(alignment == TextAlignmentOptions.Top);
            textTopRight.SetToggleState(alignment == TextAlignmentOptions.TopRight);
            textLeft.SetToggleState(alignment == TextAlignmentOptions.Left);
            textCenter.SetToggleState(alignment == TextAlignmentOptions.Center);
            textRight.SetToggleState(alignment == TextAlignmentOptions.Right);
            textBottomLeft.SetToggleState(alignment == TextAlignmentOptions.BottomLeft);
            textBottom.SetToggleState(alignment == TextAlignmentOptions.Bottom);
            textBottomRight.SetToggleState(alignment == TextAlignmentOptions.BottomRight);
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
        void UpdateFontSizeFieldsVisibility(bool isAuto)
        {
            fontSizeLabel.gameObject.SetActive(!isAuto);
            fontSizeField.gameObject.SetActive(!isAuto);

            minFontSizeLabel.gameObject.SetActive(isAuto);
            minFontSizeField.gameObject.SetActive(isAuto);

            maxFontSizeLabel.gameObject.SetActive(isAuto);
            maxFontSizeField.gameObject.SetActive(isAuto);
        }
    }
}
