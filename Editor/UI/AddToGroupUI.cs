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
    public class AddToGroupUI : MonoBehaviour
    {
        public static AddToGroupUI Instance;

        public GameObject addPanel;
        UICustomInputField existingField;
        UIButtonPatcher addToNewGroupButton;
        UIButtonPatcher addToExistingGroupButton;

        LE_Object[] targetObjs;

        public static void Create()
        {
            if (Instance)
            {
                Logger.Error("Another instance of AddToGroupUI is already created.");
                return;
            }

            Instance = new GameObject("AddToGroupUI").AddComponent<AddToGroupUI>();
        }

        void Awake()
        {
            CreatePanel();
            CreateExistingInputField();
            CreateExistingGroupButton();
            CreateNewGroupButton();
        }

        void CreatePanel()
        {
            addPanel = Instantiate(NGUI_Utils.optionsPanel, EditorUIManager.Instance.editorUIParent.transform);
            addPanel.name = "AddToGroupPanel";

            var windowTitle = this.addPanel.GetChild("Title").GetComponent<UILabel>();
            windowTitle.gameObject.RemoveComponent<UILocalize>();

            foreach (var child in this.addPanel.GetChilds())
            {
                string[] notDelete = { "Window", "Title" };
                if (notDelete.Contains(child.name)) continue;

                Destroy(child);
            }

            addPanel.transform.GetChild("Window").transform.localPosition = Vector3.zero;
            windowTitle.transform.localPosition = new Vector3(0f, 386.4f, 0f);

            // Remove the OptionsController and UILocalize components so I can change the title of the panel. Also the TweenAlpha since it won't be needed.
            addPanel.RemoveComponent<OptionsController>();
            addPanel.RemoveComponent<TweenAlpha>();

            // Change the title properties of the panel.
            windowTitle.transform.localPosition = new Vector3(0, 157, 0);
            windowTitle.GetComponent<UILabel>().width = 1650;
            windowTitle.GetComponent<UILabel>().height = 50;
            windowTitle.GetComponent<UILabel>().text = "Add To Group";

            // Reset the scale of the new custom menu to one.
            addPanel.transform.localScale = Vector3.one;

            // Add a UIPanel so the TweenScale can work.
            // UPDATE: It already has an UIPanel LOL.
            UIPanel panel = addPanel.GetComponent<UIPanel>();
            panel.alpha = 1f;
            panel.depth = 1;
            panel.GetComponent<TweenAlpha>().mRect = panel;

            // Change the animation.
            addPanel.GetComponent<TweenScale>().from = Vector3.zero;
            addPanel.GetComponent<TweenScale>().to = Vector3.one;

            // For some reason sometimes the window sprite can be transparent, force it to be opaque.
            addPanel.GetChild("Window").GetComponent<UISprite>().alpha = 1f;
            addPanel.GetChild("Window").GetComponent<UISprite>().width = 800;
            addPanel.GetChild("Window").GetComponent<UISprite>().height = 400;

            // Add a collider so the user can't interact with the other objects.
            addPanel.AddComponent<BoxCollider>().size = new Vector3(100000f, 100000f, 1f);

            // We use the occluder from the pause menu, since when you open this editor, we set the editor state to paused.
        }
        void CreateExistingInputField()
        {
            existingField = NGUI_Utils.CreateInputField(addPanel.transform, new Vector3(-200, 50), new Vector3Int(300, 60, 0), defaultText: "Enter existing group ID", inputType: UICustomInputField.UIInputType.INT, depth:2);
            existingField.name = "ExistingGroupInputField";
            existingField.setFieldColorAutomatically = false;
            existingField.onChange += OnExistingFieldChanged;
        }
        void CreateExistingGroupButton()
        {
            addToExistingGroupButton = NGUI_Utils.CreateButton(addPanel.transform, new Vector3(200, 50), new Vector3Int(350, 60, 0), "AddToExistingGroup", 2);
            addToExistingGroupButton.name = "AddToExistingGroupButton";
            addToExistingGroupButton.onClick += AddToExistingGroup;

            UIButtonScale scale = addToExistingGroupButton.GetComponent<UIButtonScale>();
            scale.mScale = Vector3.one;
            scale.hover = Vector3.one * 1.02f;
            scale.pressed = Vector3.one * 0.98f;
        }
        void CreateNewGroupButton()
        {
            addToNewGroupButton = NGUI_Utils.CreateButton(addPanel.transform, new Vector3(0, -100), new Vector3Int(750, 60, 0), "AddToNewGroup", 2);
            addToNewGroupButton.name = "AddToNewGroupButton";
            addToNewGroupButton.onClick += AddToNewGroup;

            UIButtonScale scale = addToNewGroupButton.GetComponent<UIButtonScale>();
            scale.mScale = Vector3.one;
            scale.hover = Vector3.one * 1.02f;
            scale.pressed = Vector3.one * 0.98f;
        }

        void OnExistingFieldChanged()
        {
            if (!int.TryParse(existingField.GetText(), out var inputID))
            {
                existingField.Set(false);
                return;
            }

            bool groupIsValid = LE_Object.objectsPerGroup.ContainsKey(inputID);

            existingField.Set(groupIsValid);
        }
        void AddToExistingGroup()
        {
            if (!existingField.isValid) return;

            int newGroupID = int.Parse(existingField.GetText());
            foreach (var obj in targetObjs)
                obj.SetGroup(newGroupID);

            Hide();
            EditorController.Instance.SetMultipleObjectsAsSelected(LE_Object.objectsPerGroup[newGroupID].Select(x => x.gameObject).ToList());
        }
        void AddToNewGroup()
        {
            int newGroupID = LE_Object.objectsPerGroup.Count;
            foreach (var obj in targetObjs)
                obj.SetGroup(newGroupID);

            Hide();
            EditorController.Instance.SetMultipleObjectsAsSelected(LE_Object.objectsPerGroup[newGroupID].Select(x => x.gameObject).ToList());
        }

        void Refresh()
        {
            addToNewGroupButton.buttonLabel.text = Loc.Get("AddToNewGroup") + $" ({LE_Object.objectsPerGroup.Count})";
        }

        public void Show(params LE_Object[] targetObjs)
        {
            this.targetObjs = targetObjs;

            Refresh();

            EditorController.Instance.SetCurrentEditorState(EditorState.PAUSED);
            EditorUIManager.Instance.SetEditorUIContext(EditorUIContext.ADD_TO_GROUP_PANEL);
        }
        public void Hide()
        {
            EditorController.Instance.SetCurrentEditorState(EditorState.NORMAL);
            EditorUIManager.Instance.SetEditorUIContext(EditorUIContext.NORMAL);
        }
    }
}
