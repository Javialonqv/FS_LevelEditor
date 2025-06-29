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
        public UIToggle toggle;
        public Action onClick;

        void Awake()
        {
            toggle = GetComponent<UIToggle>();
            toggle.onChange.Add(new EventDelegate(this, nameof(OnToggleChange)));
        }

        void OnToggleChange()
        {
            if (onClick != null) onClick();
        }
    }
}
