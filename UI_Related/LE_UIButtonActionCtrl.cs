using UnityEngine;

namespace FS_LevelEditor
{

    public class LE_UIButtonActionCtrl : MonoBehaviour
    {
        public void OnClick()
        {
            if (!LE_MenuUIManager.Instance.levelButtonsWasClicked)
            {
                LE_MenuUIManager.Instance.SwitchBetweenMenuAndLEMenu();
            }
        }
    }
}
