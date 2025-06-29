using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Il2Cpp;
using UnityEngine.UI;

namespace FS_LevelEditor.Editor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class EditorPauseMenuPatcher : MonoBehaviour
    {
        public static EditorPauseMenuPatcher patcher;

        bool isAboutToDestroyThisObj;

        void Awake()
        {
            patcher = this;
        }

        public void OnEnable()
        {
            Logger.DebugLog("LE pause menu enabled, patching!");
            GameObject pauseMenu = gameObject;
            GameObject navigation = transform.parent.GetChildWithName("Navigation").gameObject;

            #region Large Buttons Stuff
            pauseMenu.GetChildAt("LargeButtons/1_Resume").SetActive(false);
            pauseMenu.GetChildAt("LargeButtons/1_ResumeWhenInEditor").SetActive(true);
            pauseMenu.GetChildAt("LargeButtons/1_ResumeWhenInEditor/LevelToResumeLabel").GetComponent<UILabel>().text = "Level Editor";

            pauseMenu.GetChildAt("LargeButtons/2_Chapters").SetActive(false);
            pauseMenu.GetChildAt("LargeButtons/3_NewGamePlus").SetActive(false);
            pauseMenu.GetChildAt("LargeButtons/4_NewGame").SetActive(false);
            pauseMenu.GetChildAt("LargeButtons/6_Javi's LevelEditor").SetActive(false);

            pauseMenu.GetChildAt("LargeButtons/7_ReturnToMenu").SetActive(false);
            pauseMenu.GetChildAt("LargeButtons/8_ExitGame").SetActive(false);
            pauseMenu.GetChildAt("LargeButtons/7_ExitWhenInEditor").SetActive(true);

            PatchPlayLevelButton();
            PatchSaveLevelButton();

            pauseMenu.GetChildAt("LargeButtons").GetComponent<UITable>().Reposition();
            #endregion

            // The logic for changing the navigation bar buttons and their "on click" actions it's in Patches.cs ;)
            NavigationBarController.Instance.RefreshNavigationBarActions();
        }

        void PatchPlayLevelButton()
        {
            GameObject playLevelBtn = gameObject.GetChildAt("LargeButtons/2_PlayLevel");

            if (EditorController.Instance.currentInstantiatedObjects.Any(x => x is LE_Player_Spawn && x.gameObject.activeSelf))
            {
                playLevelBtn.GetComponent<UISprite>().height = 80;
                playLevelBtn.GetComponent<UISprite>().pivot = UIWidget.Pivot.Center;
                playLevelBtn.GetComponent<UIButton>().isEnabled = true;
                playLevelBtn.GetComponent<BoxCollider>().center = Vector3.zero;
                playLevelBtn.GetChildWithName("Label").GetComponent<UILabel>().height = 50;
                playLevelBtn.GetChildWithName("Label").GetComponent<UILabel>().transform.localPosition = Vector3.zero;
                playLevelBtn.GetChildWithName("LevelToResumeLabel").SetActive(false);
            }
            else
            {
                playLevelBtn.GetComponent<UISprite>().height = 100;
                playLevelBtn.GetComponent<UISprite>().pivot = UIWidget.Pivot.Top;
                playLevelBtn.GetComponent<UIButton>().isEnabled = false;
                playLevelBtn.GetChildWithName("Label").GetComponent<UILabel>().height = 50;
                playLevelBtn.GetChildWithName("Label").GetComponent<UILabel>().transform.localPosition = new Vector3(0f, -32.5f, 0f);
                playLevelBtn.GetChildWithName("LevelToResumeLabel").GetComponent<UILabel>().text = "There isn't any player spawn obj in the scene.";
                playLevelBtn.GetChildWithName("LevelToResumeLabel").SetActive(true);
            }
        }

        void PatchSaveLevelButton()
        {
            GameObject saveLevelBtn = gameObject.GetChildAt("LargeButtons/3_SaveLevel");

            if (EditorController.Instance.levelHasBeenModified)
            {
                saveLevelBtn.GetComponent<UISprite>().height = 80;
                saveLevelBtn.GetComponent<UISprite>().pivot = UIWidget.Pivot.Center;
                saveLevelBtn.GetComponent<UIButton>().isEnabled = true;
                saveLevelBtn.GetComponent<BoxCollider>().center = Vector3.zero;
                saveLevelBtn.GetChildWithName("Label").GetComponent<UILabel>().height = 50;
                saveLevelBtn.GetChildWithName("Label").GetComponent<UILabel>().transform.localPosition = Vector3.zero;
                saveLevelBtn.GetChildWithName("LevelToResumeLabel").SetActive(false);
            }
            else
            {
                saveLevelBtn.GetComponent<UISprite>().height = 100;
                saveLevelBtn.GetComponent<UISprite>().pivot = UIWidget.Pivot.Top;
                saveLevelBtn.GetComponent<UIButton>().isEnabled = false;
                saveLevelBtn.GetChildWithName("Label").GetComponent<UILabel>().height = 50;
                saveLevelBtn.GetChildWithName("Label").GetComponent<UILabel>().transform.localPosition = new Vector3(0f, -32.5f, 0f);
                saveLevelBtn.GetChildWithName("LevelToResumeLabel").GetComponent<UILabel>().text = "There are no changes to save.";
                saveLevelBtn.GetChildWithName("LevelToResumeLabel").SetActive(true);
            }
        }

        void OnDisable()
        {
            if (isAboutToDestroyThisObj) return;

            Logger.DebugLog("LE pause menu disabled, patching!");

            GameObject navigation = transform.parent.GetChildWithName("Navigation").gameObject;
        }

        public void BeforeDestroying()
        {
            Logger.DebugLog("About to destroy LE pause menu patcher!");
            isAboutToDestroyThisObj = true;

            Destroy(gameObject.GetChildAt("LargeButtons/1_ResumeWhenInEditor"));
            Destroy(gameObject.GetChildAt("LargeButtons/7_ExitWhenInEditor"));
            Destroy(gameObject.GetChildAt("LargeButtons/3_SaveLevel"));
            Destroy(gameObject.GetChildAt("LargeButtons/2_PlayLevel"));
        }
    }
}
