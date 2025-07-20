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
        UILabel titleLabel;
        UILabel currentOptionLabel;

        List<string> options;
        public Action<int> onClick;

        public int currentSelectedID { get; private set; }
        public string currentSelectedText
        {
            get
            {
                return options[currentSelectedID];
            }
        }

        public void Init()
        {
            button = GetComponent<UIButton>();
            titleLabel = gameObject.GetChildWithName("Label").GetComponent<UILabel>();
            currentOptionLabel = gameObject.GetChildAt("Background/Label").GetComponent<UILabel>();

            // FUCKING UILOCALIZE
            Destroy(titleLabel.GetComponent<UILocalize>());
            // Good thing the currentOptionLabel doesn't have one :)

            options = new List<string>();
        }

        public void SetTitle(string newTitle)
        {
            titleLabel.text = newTitle;
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
            currentOptionLabel.text = optionText;

            if (onClick != null && executeOnChange)
            {
                onClick.Invoke(optionID);
            }
        }

        void OnClick()
        {
            currentSelectedID++;
            if (currentSelectedID > options.Count - 1) currentSelectedID = 0;

            SelectOption(currentSelectedID);
        }
    }
}
