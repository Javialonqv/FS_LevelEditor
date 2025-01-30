using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Il2Cpp;
using UnityEngine.UI;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class EditorPauseMenuPatcher : MonoBehaviour
    {
        bool isAboutToDestroyThisObj;

        public void OnEnable()
        {
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

            pauseMenu.GetChildAt("LargeButtons/7_Exit").SetActive(false);
            pauseMenu.GetChildAt("LargeButtons/7_ExitWhenInEditor").SetActive(true);

            PatchSaveLevelButton();

            pauseMenu.GetChildAt("LargeButtons").GetComponent<UITable>().Reposition();
            #endregion

            // Change exit button behaviour in the navigation bar.
            NavigationAction exitButtonFromNavigation = navigation.GetChildAt("Holder/Bar/ActionsHolder").transform.GetChild(0).GetComponent<NavigationAction>();
            exitButtonFromNavigation.onButtonClick = new Action<NavigationBarController.ActionType>(EditorUIManager.Instance.ExitToMenuFromNavigationBarButton);
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

            GameObject navigation = transform.parent.GetChildWithName("Navigation").gameObject;

            // Reset the exit button behaviour when in another menu instead of the main one.
            NavigationAction exitButtonFromNavigation = navigation.GetChildAt("Holder/Bar/ActionsHolder").transform.GetChild(0).GetComponent<NavigationAction>();
            exitButtonFromNavigation.onButtonClick = new Action<NavigationBarController.ActionType>(NavigationBarController.Instance.ManualButtonPressed);
        }

        public void BeforeDestroying()
        {
            isAboutToDestroyThisObj = true;

            Destroy(gameObject.GetChildAt("LargeButtons/1_ResumeWhenInEditor"));
            Destroy(gameObject.GetChildAt("LargeButtons/7_ExitWhenInEditor"));
            Destroy(gameObject.GetChildAt("LargeButtons/3_SaveLevel"));
            Destroy(gameObject.GetChildAt("LargeButtons/2_PlayLevel"));
        }
    }
}
