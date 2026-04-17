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
    public class GroupsUI : MonoBehaviour
    {
        public static GroupsUI Instance;

        public GameObject editorPanel;
        UILabel windowTitle;

        public GroupsUI(IntPtr ptr) : base(ptr) { }

        public static void Create()
        {
            if (Instance)
            {
                Logger.Error("Another instance of GroupsUI is already created.");
                return;
            }

            Instance = new GameObject("GroupsUI").AddComponent<GroupsUI>();
        }

        void Awake()
        {
            CreateGroupsPanel();
        }

        void CreateGroupsPanel()
        {
            editorPanel = Instantiate(NGUI_Utils.optionsPanel, EditorUIManager.Instance.editorUIParent.transform);
            editorPanel.name = "GroupsPanel";

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
            windowTitle.GetComponent<UILabel>().text = "Groups";

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

        public void ShowGroupsPanel()
        {
            EditorController.Instance.SetCurrentEditorState(EditorState.PAUSED);
            EditorUIManager.Instance.SetEditorUIContext(EditorUIContext.GROUPS_PANEL);
        }
        public void HideGroupsPanel()
        {
            EditorController.Instance.SetCurrentEditorState(EditorState.NORMAL);
            EditorUIManager.Instance.SetEditorUIContext(EditorUIContext.NORMAL);
        }
    }
}
