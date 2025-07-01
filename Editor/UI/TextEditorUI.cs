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

        GameObject editorPanel;
        UILabel windowTitle;
        GameObject occluder;
        UICustomInputField textField;

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
            CreateTextField();
        }

        void CreateTextEditorPanel()
        {
            editorPanel = Instantiate(NGUI_Utils.optionsPanel, EditorUIManager.Instance.editorUIParent.transform);
            editorPanel.name = "TextEditorPanel";

            windowTitle = editorPanel.GetChildWithName("Title").GetComponent<UILabel>();
            windowTitle.gameObject.RemoveComponent<UILocalize>();

            foreach (var child in editorPanel.GetChilds())
            {
                string[] notDelete = { "Window", "Title" };
                if (notDelete.Contains(child.name)) continue;

                Destroy(child);
            }

            editorPanel.transform.GetChildWithName("Window").transform.localPosition = Vector3.zero;
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
            editorPanel.GetChildWithName("Window").GetComponent<UISprite>().alpha = 1f;

            // Add a collider so the user can't interact with the other objects.
            editorPanel.AddComponent<BoxCollider>().size = new Vector3(100000f, 100000f, 1f);

            occluder = Instantiate(GameObject.Find("MainMenu/Camera/Holder/Occluder"), editorPanel.transform);
            occluder.name = "Occluder";
            occluder.SetActive(true);
        }
        void CreateTextField()
        {
            textField = NGUI_Utils.CreateInputField(editorPanel.transform, new Vector3(0, -100), new Vector3Int(1600, 500, 0),
                27, "", false, inputType: UICustomInputField.UIInputType.PLAIN_TEXT, depth: 5);
            textField.input.mPivot = UIWidget.Pivot.TopLeft;
            textField.input.onReturnKey = UIInput.OnReturnKey.NewLine;

            textField.onSubmit += OnTextFieldChanged;

            MenuController.GetInstance().m_uiCamera.submitKey0 = KeyCode.Return;
        }

        void UpdateTextEditorUIValues()
        {
            textField.SetText(targetObj.GetProperty<string>("Text"));
        }
        void OnTextFieldChanged()
        {
            targetObj.SetProperty("Text", textField.GetText());
        }

        public void ShowTextEditor(LE_Object targetObj)
        {
            this.targetObj = targetObj;
            windowTitle.text = "Text Editor for " + targetObj.objectFullNameWithID;

            editorPanel.SetActive(true);
            editorPanel.GetComponent<TweenScale>().PlayIgnoringTimeScale(false);
            Utilities.PlayFSUISound(Utilities.FS_UISound.POPUP_UI_SHOW);

            EditorController.Instance.SetCurrentEditorState(EditorState.PAUSED);
            EditorUIManager.Instance.SetEditorUIContext(EditorUIContext.TEXT_EDITOR);

            UpdateTextEditorUIValues();
        }
        public void HideTextEditor()
        {
            editorPanel.GetComponent<TweenScale>().PlayIgnoringTimeScale(true);
            Utilities.PlayFSUISound(Utilities.FS_UISound.POPUP_UI_HIDE);

            editorPanel.SetActive(true);
            GameObject.Find("MainMenu/Camera/Holder/Main").SetActive(false);

            targetObj.TriggerAction("OnTextEditorClose");

            EditorController.Instance.SetCurrentEditorState(EditorState.NORMAL);
            EditorUIManager.Instance.SetEditorUIContext(EditorUIContext.NORMAL);
        }
    }
}
