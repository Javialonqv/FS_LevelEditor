using Il2Cpp;
using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor
{
    [RegisterTypeInIl2Cpp]
    public class LE_UIButtonActionCtrl : MonoBehaviour
    {
        public void OnClick()
        {
            LE_MenuUIManager.Instance.SwitchBetweenMenuAndLEMenu();
        }
    }
}
