using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor.UI_Related
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class UIButtonMultiple : MonoBehaviour
    {
        static List<UIButtonMultiple> instances = new List<UIButtonMultiple>();

        UIButton button;
        UILabel titleLabel;
        UILabel currentOptionLabel;

        string titleLocKey;
        List<string> options;
        public Action<int> onClick;
        public Func<int, string> onLocalize = (id) => { return null; };

        public int currentSelectedID { get; private set; }
        public string currentSelectedText
        {
            get
            {
                return options[currentSelectedID];
            }
        }

        void Awake()
        {
            instances.Add(this);
        }
        void OnDestroy()
        {
            instances.Remove(this);
        }

        public void Init()
        {
            button = GetComponent<UIButton>();
            titleLabel = gameObject.GetChild("Label").GetComponent<UILabel>();
            currentOptionLabel = gameObject.GetChildAt("Background/Label").GetComponent<UILabel>();

            // FUCKING UILOCALIZE
            Destroy(titleLabel.GetComponent<UILocalize>());
            // Good thing the currentOptionLabel doesn't have one :)

            options = new List<string>();
        }

        public void SetTitle(string newTitle)
        {
            titleLocKey = newTitle;

            // Doesn't matter if the title is not a valid key.
            titleLabel.text = Loc.Get(newTitle, false);
        }

        public void ClearOptions()
        {
            options.Clear();
            currentOptionLabel.text = "";
        }
        public void AddOption(string optionText, bool setAsSelected)
        {
            options.Add(optionText);

            if (setAsSelected)
            {
                SelectOption(options.Count - 1);
            }
        }

        public void SelectOption(int optionID, bool executeOnChange = true)
        {
            string optionText = options[optionID];
            currentSelectedID = optionID;
            if (onLocalize(optionID) != null)
            {
                currentOptionLabel.text = onLocalize(optionID);
            }
            else
            {
                currentOptionLabel.text = optionText;
            }

            if (onClick != null && executeOnChange)
            {
                onClick.Invoke(optionID);
            }
        }

        public void SetTooltip(string tooltipKey)
        {
            FractalTooltip tooltip = GetComponent<FractalTooltip>();
            if (!tooltip) tooltip = gameObject.AddComponent<FractalTooltip>();

            tooltip.toolTipLocKey = tooltipKey;
        }

        void OnClick()
        {
            currentSelectedID++;
            if (currentSelectedID > options.Count - 1) currentSelectedID = 0;

            SelectOption(currentSelectedID);
        }

        public void RefreshLocalization()
        {
            titleLabel.text = Loc.Get(titleLocKey, false);

            currentOptionLabel.text = onLocalize(currentSelectedID);
        }

        public static void RefreshLocalizationForAll()
        {
            foreach (var instance in instances)
            {
                if (instance != null) instance.RefreshLocalization();
            }
        }
    }
}
