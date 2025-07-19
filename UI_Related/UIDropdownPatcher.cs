using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Il2CppMono.Security.X509.X520;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class UIDropdownPatcher : MonoBehaviour
    {
        public string titleLocKey;
        public List<string> optionsLocKeys = new List<string>();

        UIPopupList popupScript;
        Action<int> onClickAlt;

        public string currentlySelected
        {
            get
            {
                return popupScript.selection;
            }
        }
        public int currentlySelectedID
        {
            get
            {
                return popupScript.items.IndexOf(popupScript.selection);
            }
        }

        public UIDropdownPatcher(IntPtr ptr) : base(ptr) { }

        public void Init()
        {
            popupScript = transform.GetChild(0).GetComponent<UIPopupList>();

            // Destroy UILocalize of the dropdown title.
            GameObject.Destroy(popupScript.gameObject.GetChildWithName("LanguageTite").GetComponent<UILocalize>());

            popupScript.onChange.Add(new EventDelegate(this, nameof(OnDropdownChange)));
        }

        public void SetTitle(string title)
        {
            titleLocKey = title;

            // Doesn't matter if the title is not a valid key.
            popupScript.gameObject.GetChildWithName("LanguageTite").GetComponent<UILabel>().text = Loc.Get(title, false);
        }

        public void ClearOptions()
        {
            optionsLocKeys.Clear();
            popupScript.items.Clear();
            popupScript.selection = null;
        }
        public void AddOption(string option, bool setAsSelected)
        {
            optionsLocKeys.Add(option);
            popupScript.items.Add(option);

            if (setAsSelected)
            {
                // Select the last option, which is the one we just added.
                SelectOption(popupScript.items.Count - 1);
            }
        }

        public void SelectOption(int optionID)
        {
            string optionName = popupScript.items[optionID];

            popupScript.selection = optionName;
            // Doesn't matter if the optionName is not a valid key.
            popupScript.gameObject.GetChildAt("CurrentLanguageBG/CurrentLanguageLabel").GetComponent<UILabel>().text = Loc.Get(optionName, false);
        }
        public void SelectOption(string optionName)
        {
            popupScript.selection = optionName;
            // Doesn't matter if the optionName is not a valid key.
            popupScript.gameObject.GetChildAt("CurrentLanguageBG/CurrentLanguageLabel").GetComponent<UILabel>().text = Loc.Get(optionName, false);
        }

        public void ClearOnChangeOptions()
        {
            popupScript.onChange.Clear();
            popupScript.onChange.Add(new EventDelegate(this, nameof(OnDropdownChange)));
        }
        public void AddOnChangeOption(EventDelegate @delegate)
        {
            popupScript.onChange.Add(@delegate);
        }
        public void AddOnChangeOption(Action<int> action)
        {
            onClickAlt += action;
        }

        void OnDropdownChange()
        {
            SelectOption(popupScript.selection);

            if (onClickAlt != null)
            {
                onClickAlt.Invoke(currentlySelectedID);
            }
        }

        public void RefreshLocalization()
        {
            // Doesn't matter if the title is not a valid key.
            popupScript.gameObject.GetChildWithName("LanguageTite").GetComponent<UILabel>().text = Loc.Get(titleLocKey, false);

            // Doesn't matter if the optionName is not a valid key.
            popupScript.gameObject.GetChildAt("CurrentLanguageBG/CurrentLanguageLabel").GetComponent<UILabel>().text =
                Loc.Get(optionsLocKeys[currentlySelectedID], false);
        }
    }
}
