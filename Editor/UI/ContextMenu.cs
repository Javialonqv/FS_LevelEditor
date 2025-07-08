using FS_LevelEditor.UI_Related;
using Il2Cpp;
using Il2CppVLB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor.Editor.UI
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class ContextMenu : MonoBehaviour
    {
        UIPanel mainPanel;

        int optionsWidth = 200;
        int optionsHeight = 50;
        int depth = 0;
        List<ContextMenuOption> menuOptions = new List<ContextMenuOption>();
        internal List<ContextMenuButton> createdMenuButtons = new List<ContextMenuButton>();

        public static ContextMenu Create(Transform parent, int optionsWidth = 200, int optionsHeight = 50, int depth = 0)
        {
            var context = new GameObject("ContextMenu").AddComponent<ContextMenu>();
            context.transform.parent = parent;
            context.transform.localPosition = Vector3.zero;
            context.transform.localScale = Vector3.one;

            context.optionsWidth = optionsWidth;
            context.optionsHeight = optionsHeight;
            context.depth = depth;

            context.Init();

            return context;
        }

        void Init()
        {
            gameObject.layer = LayerMask.NameToLayer("2D GUI");
            mainPanel = gameObject.AddComponent<UIPanel>();
            mainPanel.alpha = 0;
            mainPanel.depth = depth;
        }

        public void AddOption(ContextMenuOption option)
        {
            menuOptions.Add(option);
        }

        public void Show(bool instant = false)
        {
            RefreshOptions();

            UpdateContextMenuPosition();

            if (instant)
            {
                mainPanel.alpha = 1;
            }
            else
            {
                TweenAlpha.Begin(gameObject, 0.1f, 1f);
            }
        }
        void UpdateContextMenuPosition()
        {
            Vector3 mousePos = Input.mousePosition;
            Vector3 worldPos = NGUI_Utils.mainMenuCamera.ScreenToWorldPoint(mousePos);
            Vector3 localPos = gameObject.transform.parent.InverseTransformPoint(worldPos);

            transform.position = worldPos;
        }

        void RefreshOptions()
        {
            gameObject.DeleteAllChildren();

            for (int i = 0; i < menuOptions.Count; i++)
            {
                var button = CreateOptionButton(menuOptions[i], false);
                button.transform.localPosition = new Vector3(0, -(optionsHeight * i));
            }
        }
        ContextMenuButton CreateOptionButton(ContextMenuOption option, bool isSubOption)
        {
            string GOName = option.name.ToUpper().Replace(' ', '_');
            GameObject optionGO = new GameObject(GOName);
            optionGO.transform.parent = transform;
            optionGO.transform.localPosition = Vector3.zero;
            optionGO.transform.localScale = Vector3.one;

            BoxCollider collider = optionGO.AddComponent<BoxCollider>();
            collider.size = new Vector3(optionsWidth, optionsHeight);
            collider.center = new Vector3(optionsWidth / 2, -(optionsHeight / 2));

            UISprite sprite = optionGO.AddComponent<UISprite>();
            sprite.atlas = NGUI_Utils.fractalSpaceAtlas;
            sprite.spriteName = "Square";
            sprite.color = option.mainColor;
            sprite.width = optionsWidth;
            sprite.height = optionsHeight;
            sprite.pivot = UIWidget.Pivot.TopLeft;
            sprite.depth = 0;

            UIButton button = optionGO.AddComponent<UIButton>();
            button.defaultColor = option.mainColor;
            button.hover = option.hoveredColor;
            button.pressed = option.pressedColor;
            button.duration = 0.1f;

            ContextMenuButton script = optionGO.AddComponent<ContextMenuButton>();
            script.main = this;
            script.isSubOption = isSubOption;
            script.onClick += () => ExecuteButtonAction(option);

            // ---------- CREATE LABEL ----------

            GameObject labelObj = new GameObject("Label");
            labelObj.transform.parent = optionGO.transform;
            labelObj.transform.localScale = Vector3.one;

            UILabel label = labelObj.AddComponent<UILabel>();
            label.font = NGUI_Utils.labelFont;
            label.fontSize = 27;
            label.width = optionsWidth - 10;
            label.height = optionsHeight;
            label.color = Color.white;
            label.depth = 1;
            label.text = option.name;
            label.pivot = UIWidget.Pivot.Left;

            labelObj.transform.localPosition = new Vector3(10, -(optionsHeight / 2));

            // ---------- CREATE SUBOPTIONS ----------
            
            Transform subOptionsParent = null;
            for (int i = 0; i < option.subOptions.Count; i++)
            {
                if (i == 0)
                {
                    script.hasSubOptions = true;

                    subOptionsParent = new GameObject("SubOptions").transform;
                    subOptionsParent.transform.parent = optionGO.transform;
                    subOptionsParent.transform.localPosition = new Vector3(optionsWidth, 0);
                    subOptionsParent.transform.localScale = Vector3.one;
                }

                ContextMenuButton subOption = CreateOptionButton(option.subOptions[i], true);
                subOption.parentOption = script;
                subOption.transform.parent = subOptionsParent;
                subOption.transform.localPosition = new Vector3(0, -(optionsHeight * i));
                subOption.transform.localScale = Vector3.one;

                script.subOptionsGOs.Add(subOption.gameObject);
            }

            if (!isSubOption) createdMenuButtons.Add(script);
            return script;
        }
        void ExecuteButtonAction(ContextMenuOption option)
        {
            if (option.onClick != null)
            {
                option.onClick();
            }
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = UICamera.currentCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.collider.transform.IsChildOf(transform)) return;
                }

                HideContextMenu();
            }
        }

        public void HideContextMenu(bool instant = false)
        {
            if (instant)
            {
                mainPanel.alpha = 0;
            }
            else
            {
                TweenAlpha.Begin(gameObject, 0.1f, 0f);
            }
        }
    }

    public class ContextMenuOption
    {
        public string name;
        public Color mainColor;
        public Color hoveredColor;
        public Color pressedColor;
        public Action onClick;
        public List<ContextMenuOption> subOptions;

        public ContextMenuOption()
        {
            mainColor = new Color(0.0588f, 0.3176f, 0.3215f, 1f);
            hoveredColor = new Color(0f, 0.451f, 0.459f, 1f);
            pressedColor = new Color(0.082f, 0.376f, 0.38f, 1f);
            subOptions = new List<ContextMenuOption>();
        }
    }

    [MelonLoader.RegisterTypeInIl2Cpp]
    public class ContextMenuButton : MonoBehaviour
    {
        internal ContextMenu main;
        internal bool isSubOption;
        internal ContextMenuButton parentOption;
        internal bool hasSubOptions;
        internal List<GameObject> subOptionsGOs = new List<GameObject>();
        internal Action onClick;

        void Start()
        {
            // Disable the suboption by default.
            if (isSubOption)
            {
                gameObject.SetActive(false);
            }
        }
        void OnHover(bool isHover)
        {
            if (!isSubOption && isHover)
            {
                Invoke(nameof(ForceSubOptionsDisable), 0.01f);
            }

            if (hasSubOptions)
            {
                gameObject.SetActive(true);
                foreach (var subOption in subOptionsGOs)
                {
                    subOption.SetActive(true);
                    float newAlpha = isHover ? 1f : 0f;
                    TweenAlpha.Begin(subOption, 0.1f, newAlpha);
                }
            }

            if (isSubOption && parentOption)
            {
                // This is to keep the other suboptions visible even when we are only hovering one.
                parentOption.OnHover(isHover);
            }
        }
        void OnClick()
        {
            if (onClick != null)
            {
                onClick();
            }
        }

        void ForceSubOptionsDisable()
        {
            // Force the other suboptions to close when you're in a "main" option.
            foreach (var button in main.createdMenuButtons)
            {
                if (button == this) continue;
                button.subOptionsGOs.ForEach(subOptionGO => TweenAlpha.Begin(subOptionGO, 0.1f, 0f));
            }
        }
    }
}