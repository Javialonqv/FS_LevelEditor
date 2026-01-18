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
        public Action<bool> onClick;
        bool executeOnChange = true;

        public bool isChecked => toggle.isChecked;

        public UITogglePatcher(IntPtr ptr) : base (ptr) { }

        void Awake()
        {
            if (!initialized) Init();
        }
        internal void Init()
        {
            toggle = GetComponent<UIToggle>();
            toggle.onChange.Add(new EventDelegate(this, nameof(OnToggleChange)));

            initialized = true;
        }

        void OnToggleChange()
        {
            if (!executeOnChange) return;

            if (onClick != null) onClick(toggle.isChecked);
        }

        public void Set(bool newState, bool executeOnChange = true)
        {
            this.executeOnChange = executeOnChange;
            toggle.Set(newState);
            this.executeOnChange = true;
        }
    }
}
