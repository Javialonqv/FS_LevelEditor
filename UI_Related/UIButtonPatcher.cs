using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using UnityEngine;
using System.Collections;

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

        UISprite _buttonSprite;
        public UISprite buttonSprite
        {
            get
            {
                if (!_buttonSprite) _buttonSprite = GetComponent<UISprite>();

                return _buttonSprite;
            }
        }

        UILabel _buttonLabel;
        public UILabel buttonLabel
        {
            get
            {
                if (!_buttonLabel) _buttonLabel = gameObject.GetChildAt("Background/Label").GetComponent<UILabel>();
                return _buttonLabel;
            }
        }

        public Action onClick;

        public void OnClick()
        {
            if (onClick != null)
            {
                onClick.Invoke();
            }
        }
    }
}
