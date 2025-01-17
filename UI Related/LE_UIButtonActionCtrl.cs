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
            MelonCoroutines.Start(Init());

            IEnumerator Init()
            {
                StartCoroutine(MenuController.m_instance.ExitGameRoutine());
                yield return new WaitForSeconds(0.5f);
                Melon<Core>.Instance.SetupTheWholeEditor();
            }
        }
    }
}
