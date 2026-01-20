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
    public class UITogglePatcher : MonoBehaviour
    {
        bool initialized = false;

        public UIToggle toggle;
        GameObject undefinedLine;

        public Action<bool> onClick;
        bool executeOnChange = true;

        public bool isChecked => toggle.isChecked;
        public bool isUndefined = false;

        public UITogglePatcher(IntPtr ptr) : base (ptr) { }

        void Awake()
        {
            if (!initialized) Init();
        }
        internal void Init()
        {
            toggle = GetComponent<UIToggle>();
            undefinedLine = gameObject.GetChildAt("Background/Line");

            toggle.onChange.Add(new EventDelegate(this, nameof(OnToggleChange)));

            initialized = true;
        }

        void OnToggleChange()
        {
            if (!executeOnChange) return;

            if (isUndefined)
            {
                undefinedLine.SetActive(false);
                isUndefined = false;
            }

            if (onClick != null) onClick(toggle.isChecked);
        }

        public void Set(bool newState, bool executeOnChange = true, bool instant = false)
        {
            if (isUndefined)
            {
                undefinedLine.SetActive(false);
                isUndefined = false;
            }

            this.executeOnChange = executeOnChange;
            toggle.instantTween = instant;
            toggle.Set(newState);
            toggle.instantTween = false;

            // Re-enable after a small delay to avoid a bug where OnToggleChange() was called from an still unknown code, and onClick was executed when it shouldn't.
            Utils.Invoke(() => {
                this.executeOnChange = true;
            }, 0.1f);
        }
        public void SetAsUndefined()
        {
            if (isChecked) Set(false, false, true);

            undefinedLine.SetActive(true);
            isUndefined = true;
        }
    }
}
