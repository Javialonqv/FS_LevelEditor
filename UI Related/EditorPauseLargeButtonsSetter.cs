using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Il2Cpp;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class EditorPauseLargeButtonsSetter : MonoBehaviour
    {
        void OnEnable()
        {
            GameObject pauseMenu = gameObject;

            pauseMenu.GetChildAt("LargeButtons/1_Resume/LevelToResumeLabel").GetComponent<UILabel>().text = "Level Editor";
            pauseMenu.GetChildAt("LargeButtons/2_Chapters").SetActive(false);
            pauseMenu.GetChildAt("LargeButtons/3_NewGamePlus").SetActive(false);
            pauseMenu.GetChildAt("LargeButtons/4_NewGame").SetActive(false);
            pauseMenu.GetChildAt("LargeButtons/6_Javi's LevelEditor").SetActive(false);
            pauseMenu.GetChildAt("LargeButtons").GetComponent<UITable>().Reposition();
        }
    }
}
