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
        public Action<int> onChange;
        public int maxOptions { get; private set; }
        public int currentOption;

        public UIButtonMultiple(IntPtr ptr) : base(ptr) { }

        void Awake()
        {
            button = GetComponent<UIButton>();
            buttonLabel = gameObject.GetChildAt("Background/Label").GetComponent<UILabel>();
        }

        public void Setup(int maxOptions, int initialOption = 0)
        {
            if (!button)
            {
                button = GetComponent<UIButton>();
                buttonLabel = gameObject.GetChildAt("Background/Label").GetComponent<UILabel>();
            }

            this.maxOptions = maxOptions;
            currentOption = initialOption;

            button.onClick.Clear();
            EventDelegate.Add(button.onClick, new EventDelegate(this, nameof(OnChange)));
        }

        void OnChange()
        {
            currentOption++;
            if (currentOption >= maxOptions) currentOption = 0;

            if (onChange != null)
            {
                onChange(currentOption);
            }
        }

        public void SetTitle(string newTitle)
        {
            buttonLabel.text = newTitle;
        }
        public void SetOption(int newOption, bool executeActions = true)
        {
            currentOption = newOption;
            if (currentOption >= maxOptions) currentOption = 0;

            if (executeActions && onChange != null)
            {
                onChange(currentOption);
            }
        }
    }
}
