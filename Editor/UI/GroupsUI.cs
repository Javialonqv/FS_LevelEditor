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

        #region Groups Grid
        const int GROUPS_LIST_COLLUMNS = 7;
        const int GROUPS_LIST_ROWS = 9;
        const int GROUPS_PER_PAGE = GROUPS_LIST_COLLUMNS * GROUPS_LIST_ROWS;

        #region UI References
        GameObject groupsListBg;

        UIButtonPatcher previousPageBtn;
        UILabel currentPageLabel;
        UIButtonPatcher nextPageBtn;

        List<UIButtonAsToggle> groupButtons = new();
        #endregion

        int currentGroupsPage;
        int currentSelectedGroup;
        #endregion

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
            CreateVerticalLine();
            CreateGroupsButtonsBackground();
            CreateCurrentGroupsPageLabel();
            CreatePreviousPageButton();
            CreateNextPageButton();
        }

        #region Create UI
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
        void CreateVerticalLine()
        {
            GameObject verticalLine = Instantiate(NGUI_Utils.optionsPanel.GetChildAt("Game_Options/VerticalLine"), editorPanel.transform);
            verticalLine.GetComponent<UISprite>().pivot = UIWidget.Pivot.Center;
            verticalLine.transform.localPosition = new Vector3(0, -35, 0);
            verticalLine.GetComponent<UISprite>().height = 700;
            verticalLine.SetActive(true);
        }

        #region Groups Grid
        void CreateGroupsButtonsBackground()
        {
            groupsListBg = new GameObject("EventsList");
            groupsListBg.transform.parent = editorPanel.transform;
            groupsListBg.transform.localScale = Vector3.one;
            groupsListBg.layer = LayerMask.NameToLayer("2D GUI");

            UISprite sprite = groupsListBg.AddComponent<UISprite>();
            sprite.transform.localPosition = new Vector3(-430f, 15f, 0f);
            sprite.atlas = NGUI_Utils.fractalSpaceAtlas;
            sprite.spriteName = "Square";
            sprite.depth = 1;
            sprite.color = Color.black;
            sprite.width = 800;
            sprite.height = 600;

            UIGrid grid = groupsListBg.AddComponent<UIGrid>();
            grid.arrangement = UIGrid.Arrangement.Horizontal;
            grid.cellWidth = 110;
            grid.cellHeight = 60;
            grid.maxPerLine = 7;
            grid.pivot = UIWidget.Pivot.Center;
        }
        void CreateCurrentGroupsPageLabel()
        {
            currentPageLabel = NGUI_Utils.CreateLabel(editorPanel.transform, new Vector3(-430, -335), new Vector3Int(100, 30, 0), "0/0", NGUIText.Alignment.Center, UIWidget.Pivot.Center);
            currentPageLabel.name = "CurrentGroupsPageLabel";
            currentPageLabel.fontSize = 30;
        }
        void CreatePreviousPageButton()
        {
            previousPageBtn = NGUI_Utils.CreateButton(editorPanel.transform, new Vector3(-530, -335), new Vector3Int(50, 50, 0), "<", 1, 40);
            previousPageBtn.name = "PreviousPageButton";

            previousPageBtn.onClick += PreviousGroupsPage;

            previousPageBtn.gameObject.SetActive(true);
        }
        void CreateNextPageButton()
        {
            nextPageBtn = NGUI_Utils.CreateButton(editorPanel.transform, new Vector3(-330, -335), new Vector3Int(50, 50, 0), ">", 1, 40);
            nextPageBtn.name = "NextPageButton";

            nextPageBtn.onClick += NextGroupsPage;

            nextPageBtn.gameObject.SetActive(true);
        }

        void CreateGroupsList()
        {
            int startValue = currentGroupsPage * GROUPS_PER_PAGE;
            int endValue = (currentGroupsPage + 1) * GROUPS_PER_PAGE;
            endValue = Mathf.Clamp(endValue, 0, LE_Object.objectsPerGroup.Count); // Clamp the value in case endValue is greather than the available groups, otherwise the whole grid would fill up.

            groupsListBg.DeleteAllChildren();
            groupButtons.Clear();
            for (int i = startValue; i < endValue; i++)
            {
                int groupID = i;

                var button = NGUI_Utils.CreateButtonAsToggle(groupsListBg.transform, Vector3.zero, new Vector3Int(100, 50, 0), i.ToString(), 2);
                button.onClick += (state) => SelectGroup(state, groupID);
                groupButtons.Add(button);
            }

            groupsListBg.GetComponent<UIGrid>().repositionNow = true;
        }
        #endregion

        #region Objects Per Group

        #endregion

        #endregion

        #region UI Implementation

        #region Groups Grid
        void RefreshGroupsPagesUI()
        {
            int groupPages = GetGroupsPagesCount();
            currentPageLabel.gameObject.SetActive(groupPages > 1);
            previousPageBtn.gameObject.SetActive(groupPages > 1 && currentGroupsPage > 0);
            nextPageBtn.gameObject.SetActive(groupPages > 1 && currentGroupsPage < groupPages - 1);

            currentPageLabel.text = (currentGroupsPage + 1) + "/" + groupPages;

            CreateGroupsList();
        }

        int GetGroupsPagesCount()
        {
            return Mathf.CeilToInt((float)LE_Object.objectsPerGroup.Count / GROUPS_PER_PAGE);
        }
        void PreviousGroupsPage()
        {
            currentGroupsPage--;
            RefreshGroupsPagesUI();
        }
        void NextGroupsPage()
        {
            currentGroupsPage++;
            RefreshGroupsPagesUI();
        }

        void SelectGroup(bool selecting, int groupID)
        {
            if (!selecting)
            {
                currentSelectedGroup = 0;
                return;
            }

            foreach (var button in groupButtons)
                button.SetToggleState(false, false);

            groupButtons[groupID].SetToggleState(true, false);
        }
        #endregion

        #region Objects Per Group
        #endregion

        #endregion

        void RefreshUI()
        {
            RefreshGroupsPagesUI();
        }

        public void ShowGroupsPanel()
        {
            EditorController.Instance.SetCurrentEditorState(EditorState.PAUSED);
            EditorUIManager.Instance.SetEditorUIContext(EditorUIContext.GROUPS_PANEL);

            RefreshUI();
        }
        public void HideGroupsPanel()
        {
            EditorController.Instance.SetCurrentEditorState(EditorState.NORMAL);
            EditorUIManager.Instance.SetEditorUIContext(EditorUIContext.NORMAL);
        }
    }
}
