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
        UIButton button;
        UILabel buttonLabel;
        UIButtonColor buttonColor;

        public Action<int> onChange;
        List<(string text, Color color)> options = new List<(string text, Color color)>();
        public int currentOption;

        public UIButtonMultiple(IntPtr ptr) : base(ptr) { }

        void Awake()
        {
            button = GetComponent<UIButton>();
            buttonLabel = gameObject.GetChildAt("Background/Label").GetComponent<UILabel>();
            buttonColor = GetComponent<UIButtonColor>();
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

            buttonLabel.text = toSet.text;
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
    }
}
