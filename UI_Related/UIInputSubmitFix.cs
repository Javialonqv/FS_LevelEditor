using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class UIInputSubmitFix : MonoBehaviour
    {
        UIInput input;
        bool submitOnDeselect = false;

        void Awake()
        {
            input = GetComponent<UIInput>();
        }

        void Update()
        {
            if (input.isSelected)
            {
                submitOnDeselect = true;
            }

            if (!input.isSelected && submitOnDeselect)
            {
                input.Submit();
                submitOnDeselect = false;

                // Clear the renaming flag in case this is used for level renaming.
                if (LE_MenuUIManager.Instance != null)
                {
                    LE_MenuUIManager.Instance.isRenamingLevel = false;
                }
            }
        }
    }
}
