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
        void OnEnable()
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

            pauseMenu.GetChildAt("LargeButtons").GetComponent<UITable>().Reposition();
            #endregion

            // Change exit button behaviour in the navigation bar.
            NavigationAction exitButtonFromNavigation = navigation.GetChildAt("Holder/Bar/ActionsHolder").transform.GetChild(0).GetComponent<NavigationAction>();
            exitButtonFromNavigation.onButtonClick = new Action<NavigationBarController.ActionType>(EditorUIManager.Instance.ExitToMenuFromNavigationBarButton);
        }

        void OnDisable()
        {
            GameObject navigation = transform.parent.GetChildWithName("Navigation").gameObject;

            // Reset the exit button behaviour when in another menu instead of the main one.
            NavigationAction exitButtonFromNavigation = navigation.GetChildAt("Holder/Bar/ActionsHolder").transform.GetChild(0).GetComponent<NavigationAction>();
            exitButtonFromNavigation.onButtonClick = new Action<NavigationBarController.ActionType>(NavigationBarController.Instance.ManualButtonPressed);
        }

        void OnDestroy()
        {
            Destroy(gameObject.GetChildAt("LargeButtons/1_ResumeWhenInEditor"));
            Destroy(gameObject.GetChildAt("LargeButtons/7_ExitWhenInEditor"));
            Destroy(gameObject.GetChildAt("LargeButtons/2_SaveLevel"));
        }
    }
}
