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

        public Action onClick;
        bool clickedRecently = false;

        //void Awake()
        //{
        //    button.onClick.Clear();
        //    button.onClick.Add(new EventDelegate(this, nameof(OnButtonClick)));
        //}

        public void OnClick()
        {
            if (clickedRecently) return;
            clickedRecently = true;

            if (onClick != null)
            {
                onClick.Invoke();
            }

            MelonCoroutines.Start(ResetClick());
        }

        IEnumerator ResetClick()
        {
            yield return null;
            clickedRecently = false;
        }
    }
}
