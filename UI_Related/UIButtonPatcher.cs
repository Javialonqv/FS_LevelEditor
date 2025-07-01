using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using UnityEngine;

namespace FS_LevelEditor.UI_Related
{
    [RegisterTypeInIl2Cpp]
    public class UIButtonPatcher : MonoBehaviour
    {
        UIButton _button;
        public UIButton button
        {
            get
            {
                if (!_button) _button = GetComponent<UIButton>();

                return _button;
            }
        }
        public Action onClick;

        void OnClick()
        {
            if (onClick != null)
            {
                onClick.Invoke();
            }
        }
    }
}
