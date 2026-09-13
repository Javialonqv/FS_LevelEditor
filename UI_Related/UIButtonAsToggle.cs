using UnityEngine;

namespace FS_LevelEditor.UI_Related
{

    public class UIButtonAsToggle : MonoBehaviour
    {
        UIButton button;
        bool isChecked;
        public Action<bool> onClick;

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

        void OnDestroy()
        {
            onClick = null;
        }

        public void SetToggleState(bool newState, bool executeOnClick = false)
        {
            isChecked = newState;

            button.defaultColor = newState ? NGUI_Utils.fsButtonsPressedColor : NGUI_Utils.fsButtonsDefaultColor;

            if (executeOnClick)
            {
                onClick.Invoke(isChecked);
            }
        }
    }
}
