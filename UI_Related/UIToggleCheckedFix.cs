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
    public class UIToggleCheckedFix : MonoBehaviour
    {
        UIToggle toggle;

        void Awake()
        {
            toggle = GetComponent<UIToggle>();
        }

        void OnEnable()
        {
            toggle.Set(toggle.isChecked);
        }
    }
}
