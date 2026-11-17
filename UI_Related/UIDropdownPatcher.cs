using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace FS_LevelEditor
{

    public class UIDropdownPatcher : MonoBehaviour
    {
        static List<UIDropdownPatcher> instances = new List<UIDropdownPatcher>();

        public string titleLocKey;
        public List<string> optionsLocKeys = new List<string>();

        UIPopupList popupScript;
        public bool isNarrowed { get; private set; }
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


        void Awake()
        {
            instances.Add(this);
        }
        void OnDestroy()
        {
            instances.Remove(this);

            optionsLocKeys.Clear();
            onClickAlt = null;
        }

        public void Init()
        {
            popupScript = transform.GetChild(0).GetComponent<UIPopupList>();

            // Destroy UILocalize of the dropdown title.
            GameObject.Destroy(popupScript.gameObject.GetChild("LanguageTite").GetComponent<UILocalize>());

            popupScript.onChange.Add(new EventDelegate(this, nameof(OnDropdownChange)));
        }

        /// <summary>
        /// Makes the dropdown narrower/wider without changing its scale (so the text keeps its size).
        /// Resizes every widget (backgrounds, borders, labels) and the click collider.
        /// </summary>
        /// <param name="multiplier">1 = original width.</param>
        public void SetWidthMultiplier(float multiplier)
        {
            isNarrowed = !Mathf.Approximately(multiplier, 1f);

            foreach (UIWidget widget in GetComponentsInChildren<UIWidget>(true))
            {
                widget.width = Mathf.RoundToInt(widget.width * multiplier);
            }
            foreach (BoxCollider collider in GetComponentsInChildren<BoxCollider>(true))
            {
                Vector3 size = collider.size;
                size.x *= multiplier;
                collider.size = size;

                Vector3 center = collider.center;
                center.x *= multiplier;
                collider.center = center;
            }
        }

        public void SetTitle(string title)
        {
            titleLocKey = title;

            // Doesn't matter if the title is not a valid key.
            popupScript.gameObject.GetChild("LanguageTite").GetComponent<UILabel>().text = Loc.Get(title, false);
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
            popupScript.gameObject.GetChild("LanguageTite").GetComponent<UILabel>().text = Loc.Get(titleLocKey, false);

            // Doesn't matter if the optionName is not a valid key.
            popupScript.gameObject.GetChildAt("CurrentLanguageBG/CurrentLanguageLabel").GetComponent<UILabel>().text =
                Loc.Get(optionsLocKeys[currentlySelectedID], false);
        }

        public static void RefreshLocalizationForAll()
        {
            foreach (var instance in instances)
            {
                if (instance != null) instance.RefreshLocalization();
            }
        }
    }

    /// <summary>
    /// UIPopupList opens its list at the left edge of the popup's own widget bounds, which still match the template's width
    /// for dropdowns narrowed with SetWidthMultiplier, so the list appeared offset. This aligns it with the dropdown's title bar.
    /// </summary>
    [HarmonyPatch(typeof(UIPopupList), nameof(UIPopupList.Show))]
    public static class UIPopupListNarrowedDropdownPatch
    {
        static readonly FieldInfo childField = AccessTools.Field(typeof(UIPopupList), "mChild");
        static readonly FieldInfo panelField = AccessTools.Field(typeof(UIPopupList), "mPanel");

        static void Postfix(UIPopupList __instance)
        {
            UIDropdownPatcher dropdown = __instance.GetComponentInParent<UIDropdownPatcher>();
            if (dropdown == null || !dropdown.isNarrowed) return;

            GameObject list = (GameObject)childField.GetValue(null);
            UIPanel panel = (UIPanel)panelField.GetValue(__instance);
            Transform background = __instance.transform.Find("Background");
            if (list == null || panel == null || background == null) return;

            UIWidget backgroundWidget = background.GetComponent<UIWidget>();
            if (backgroundWidget == null) return;

            // worldCorners[0] is the bottom-left corner.
            Vector3 left = panel.cachedTransform.InverseTransformPoint(backgroundWidget.worldCorners[0]);
            Vector3 position = list.transform.localPosition;
            position.x = Mathf.Round(left.x);
            list.transform.localPosition = position;
        }
    }
}
