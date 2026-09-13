using UnityEngine;

namespace FS_LevelEditor.Playmode
{

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
