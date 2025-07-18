using Il2Cpp;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor.Playmode
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class PlaymodePauseMenuPatcher : MonoBehaviour
    {
        public static PlaymodePauseMenuPatcher Instance;

        GameObject newGamePauseStats;

        public static void Create()
        {
            MenuController.GetInstance().m_mainHolder.AddComponent<PlaymodePauseMenuPatcher>();
        }

        void Awake()
        {
            Instance = this;

            GetReferences();
        }
        void GetReferences()
        {
            newGamePauseStats = MenuController.GetInstance().pausePlayerStats.transform.GetChild(0).gameObject;
        }

        void OnEnable()
        {
            newGamePauseStats.SetActive(false);
        }

        public static void DestroyPatcher()
        {
            Destroy(Instance);
        }
    }
}
