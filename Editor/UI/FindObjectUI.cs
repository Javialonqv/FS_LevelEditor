using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Il2Cpp;
using FS_LevelEditor.UI_Related;

namespace FS_LevelEditor.Editor.UI
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class FindObjectUI : MonoBehaviour
    {
        public static FindObjectUI Instance;

        public GameObject findPanel;
        UICustomInputField objectNameField;
        UIButtonPatcher selectButton;

        LE_Object.ObjectType? targetObjectType;
        int? targetObjectID;
        List<LE_Object> targetObjects = new();

        public static void Create()
        {
            if (Instance)
            {
                Logger.Error("Another instance of FindObjectUI is already created.");
                return;
            }

            Instance = new GameObject("FindObjectUI").AddComponent<FindObjectUI>();
        }

        void Awake()
        {
            CreatePanel();
            CreateObjectInputField();
            CreateSelectButton();
        }

        void CreatePanel()
        {
            findPanel = Instantiate(NGUI_Utils.optionsPanel, EditorUIManager.Instance.editorUIParent.transform);
            findPanel.name = "FindObjectPanel";

            var windowTitle = this.findPanel.GetChild("Title").GetComponent<UILabel>();

            foreach (var child in this.findPanel.GetChilds())
            {
                string[] notDelete = { "Window", "Title" };
                if (notDelete.Contains(child.name)) continue;

                Destroy(child);
            }

            findPanel.transform.GetChild("Window").transform.localPosition = Vector3.zero;
            windowTitle.transform.localPosition = new Vector3(0f, 386.4f, 0f);

            // Remove the OptionsController and UILocalize components so I can change the title of the panel. Also the TweenAlpha since it won't be needed.
            findPanel.RemoveComponent<OptionsController>();
            findPanel.RemoveComponent<TweenAlpha>();

            // Change the title properties of the panel.
            windowTitle.transform.localPosition = new Vector3(0, 157, 0);
            windowTitle.GetComponent<UILabel>().width = 1650;
            windowTitle.GetComponent<UILabel>().height = 50;
            windowTitle.GetComponent<UILocalize>().key = "FindObject";

            // Reset the scale of the new custom menu to one.
            findPanel.transform.localScale = Vector3.one;

            // Add a UIPanel so the TweenScale can work.
            // UPDATE: It already has an UIPanel LOL.
            UIPanel panel = findPanel.GetComponent<UIPanel>();
            panel.alpha = 1f;
            panel.depth = 1;
            panel.GetComponent<TweenAlpha>().mRect = panel;

            // Change the animation.
            findPanel.GetComponent<TweenScale>().from = Vector3.zero;
            findPanel.GetComponent<TweenScale>().to = Vector3.one;

            // For some reason sometimes the window sprite can be transparent, force it to be opaque.
            findPanel.GetChild("Window").GetComponent<UISprite>().alpha = 1f;
            findPanel.GetChild("Window").GetComponent<UISprite>().width = 800;
            findPanel.GetChild("Window").GetComponent<UISprite>().height = 400;

            // Add a collider so the user can't interact with the other objects.
            findPanel.AddComponent<BoxCollider>().size = new Vector3(100000f, 100000f, 1f);

            // We use the occluder from the pause menu, since when you open this editor, we set the editor state to paused.
        }
        void CreateObjectInputField()
        {
            objectNameField = NGUI_Utils.CreateInputField(findPanel.transform, new Vector3(0, 50), new Vector3Int(750, 60, 0), defaultText: "EnterAnObjectName", depth: 2);
            objectNameField.name = "ObjectNameInputField";
            objectNameField.setFieldColorAutomatically = false;
            objectNameField.onChange += OnObjectNameFieldChanged;
        }
        void CreateSelectButton()
        {
            // Leave the default text blank, so no UILocalize is created.
            selectButton = NGUI_Utils.CreateButton(findPanel.transform, new Vector3(0, -100), new Vector3Int(750, 60, 0), "SelectObject", 2);
            selectButton.name = "SelectButton";
            selectButton.onClick += OnSelectButtonPressed;

            UIButtonScale scale = selectButton.GetComponent<UIButtonScale>();
            scale.mScale = Vector3.one;
            scale.hover = Vector3.one * 1.02f;
            scale.pressed = Vector3.one * 0.98f;
        }

        void OnObjectNameFieldChanged()
        {
            string input = objectNameField.input.text.Trim();

            // Safety check, prevent Enum.TryParse to also returning true when the input is a number (object type ID in the ObjectType enum).
            if (int.TryParse(input, out _))
            {
                targetObjectType = null;
                targetObjectID = null;
            }
            // Is searching for all objects of one type
            else if (Enum.TryParse<LE_Object.ObjectType>(input, true, out var objectType))
            {
                targetObjectType = objectType;
                targetObjectID = null;
            }
            else // Is searching for a specific object.
            {
                string[] splitted = input.Split(' ');
                if (splitted.Length == 2)
                {
                    if (Enum.TryParse<LE_Object.ObjectType>(splitted[0], true, out objectType) &&
                        int.TryParse(splitted[1], out int objectID))
                    {
                        targetObjectType = objectType;
                        targetObjectID = objectID;
                    }
                }
                else
                {
                    targetObjectType = null;
                    targetObjectID = null;
                }
            }

            Refresh();
        }
        void OnSelectButtonPressed()
        {
            Hide();

            if (targetObjects.Count > 1)
            {
                EditorController.Instance.SetMultipleObjectsAsSelected(targetObjects.Select(x => x.gameObject).ToList());
            }
            else if (targetObjects.Count == 1)
            {
                EditorController.Instance.SetSelectedObj(targetObjects[0].gameObject, EditorController.SelectionType.ForceSingle);
            }
        }

        bool FindTargetObjects(LE_Object.ObjectType? objectType, int? objectID = null)
        {
            targetObjects.Clear();

            // Object type is MANDATORY.
            if (!objectType.HasValue)
                return false;

            foreach (var obj in EditorController.Instance.currentInstantiatedObjects)
            {
                // ID is optional, if null, search for all of the objects of the specified type.
                if (obj.objectType == objectType && (objectID == null || obj.objectID == objectID))
                {
                    targetObjects.Add(obj);
                }
            }

            return targetObjects.Count > 0;
        }
        void Refresh(bool clearObjectNameField = false)
        {
            if (clearObjectNameField)
                objectNameField.SetText("", false);

            bool valid = FindTargetObjects(targetObjectType, targetObjectID);

            objectNameField.Set(valid);
            objectNameField.input.defaultText = Loc.Get("EnterAnObjectName");
            selectButton.button.isEnabled = valid;
            if (valid)
                selectButton.buttonLabel.text = targetObjects.Count > 1 ? Loc.Get("SelectAllObjects") + $" ({targetObjects.Count})" : Loc.Get("SelectObject");
            else
                selectButton.buttonLabel.text = Loc.Get("SelectObject");
        }


        public void Show()
        {
            Refresh(true);

            EditorController.Instance.SetCurrentEditorState(EditorState.PAUSED);
            EditorUIManager.Instance.SetEditorUIContext(EditorUIContext.FIND_OBJECT);
        }
        public void Hide()
        {
            EditorController.Instance.SetCurrentEditorState(EditorState.NORMAL);
            EditorUIManager.Instance.SetEditorUIContext(EditorUIContext.NORMAL);
        }
    }
}
