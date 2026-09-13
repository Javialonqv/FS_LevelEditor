using UnityEngine;

namespace FS_LevelEditor.UI_Related
{

    public class UIToggleCheckedFix : MonoBehaviour
    {
        UITogglePatcher toggle;

        void Awake()
        {
            toggle = GetComponent<UITogglePatcher>();
        }

        void OnEnable()
        {
            if (!toggle.isUndefined) toggle.Set(toggle.isChecked, false);
        }
    }
}
