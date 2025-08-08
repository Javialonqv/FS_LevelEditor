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
    public class UISmallButtonMultiple : MonoBehaviour
    {
        static List<UISmallButtonMultiple> instances = new List<UISmallButtonMultiple>();

        UIButton button;
        UILabel buttonLabel;
        UIButtonColor buttonColor;

        public Action<int> onChange;
        List<(string text, Color color)> options = new List<(string text, Color color)>();
        public int currentOption;

        public UISmallButtonMultiple(IntPtr ptr) : base(ptr) { }

        void Awake()
        {
            instances.Add(this);

            button = GetComponent<UIButton>();
            buttonLabel = gameObject.GetChildAt("Background/Label").GetComponent<UILabel>();
            buttonColor = GetComponent<UIButtonColor>();
        }
        void OnDestroy()
        {
            instances.Remove(this);
        }

        public void Setup()
        {
            if (!button)
            {
                button = GetComponent<UIButton>();
                buttonLabel = gameObject.GetChildAt("Background/Label").GetComponent<UILabel>();
                buttonColor = GetComponent<UIButtonColor>();
            }

            button.onClick.Clear();
            EventDelegate.Add(button.onClick, new EventDelegate(this, nameof(OnChange)));
        }

        void OnChange()
        {
            currentOption++;
            if (currentOption >= options.Count) currentOption = 0;

            SetTextAndColor(currentOption);
            if (onChange != null)
            {
                onChange(currentOption);
            }
        }
        void SetTextAndColor(int optionID)
        {
            (string text, Color color) toSet = options[optionID];

            if (Loc.HasKey(toSet.text))
            {
                buttonLabel.text = Loc.Get(toSet.text, false);
            }
            else
            {
                buttonLabel.text = toSet.text;
            }
            buttonColor.defaultColor = toSet.color;
        }

        public void SetTitle(string newTitle)
        {
            buttonLabel.text = newTitle;
        }
        public void AddOption(string buttonText, Color? buttonColor = null)
        {
            Color colorToSet = buttonColor != null ? buttonColor.Value : NGUI_Utils.fsButtonsDefaultColor;
            options.Add((buttonText, colorToSet));
        }
        public void SetOption(int newOption, bool executeActions = true)
        {
            currentOption = newOption;
            if (currentOption >= options.Count) currentOption = 0;

            SetTextAndColor(currentOption);
            if (executeActions && onChange != null)
            {
                onChange(currentOption);
            }
        }

        public void RefreshLocalization()
        {
            buttonLabel.text = Loc.Get(options[currentOption].text, false);
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
