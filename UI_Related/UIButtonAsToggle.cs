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
    public class UIButtonAsToggle : MonoBehaviour
    {
        UIButton button;
        bool isChecked;
        public Action<bool> onClick { get; private set; }

        void Awake()
        {
            button = GetComponent<UIButton>();
        }

        void OnClick()
        {
            SetToggleState(!isChecked);
            if (onClick != null)
            {
                onClick.Invoke(isChecked); // At this point, the new value is already setted.
            }
        }

        public void SetToggleState(bool newState)
        {
            isChecked = newState;

            button.defaultColor = newState ? NGUI_Utils.fsButtonsPressedColor : NGUI_Utils.fsButtonsDefaultColor;
        }
    }
}
