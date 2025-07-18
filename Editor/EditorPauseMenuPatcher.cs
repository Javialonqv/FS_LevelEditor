using FS_LevelEditor.Editor.UI;
using FS_LevelEditor.SaveSystem;
using FS_LevelEditor.UI_Related;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace FS_LevelEditor.Editor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class EditorPauseMenuPatcher : MonoBehaviour
    {
        public static EditorPauseMenuPatcher patcher;

        public GameObject pauseMenu;
        bool isAboutToDestroyThisObj;

        UIButtonPatcher resumeButton;
        UIButtonPatcher exitButton;
        UIButtonPatcher saveButton;
        UIButtonPatcher playButton;

        public static void Create(GameObject pauseMenu)
        {
            patcher = pauseMenu.AddComponent<EditorPauseMenuPatcher>();
            patcher.pauseMenu = pauseMenu;
            patcher.CheckForExistingButtons();
            patcher.SetupPauseWhenInEditor();
        }

        // If for some reason the pause menu already has the buttons, destroy them, just in case something bad happens.
        void CheckForExistingButtons()
        {
            GameObject largeButtons = pauseMenu.GetChildWithName("LargeButtons");

            if (largeButtons.ExistsChildWithName("1_ResumeWienInEditor")) Destroy(largeButtons.GetChildWithName("1_ResumeWhenInEditor"));
            if (largeButtons.ExistsChildWithName("2_PlayLevel")) Destroy(largeButtons.GetChildWithName("2_PlayLevel"));
            if (largeButtons.ExistsChildWithName("3_SaveLevel")) Destroy(largeButtons.GetChildWithName("3_SaveLevel"));
            if (largeButtons.ExistsChildWithName("7_ExitWhenInEditor")) Destroy(largeButtons.GetChildWithName("7_ExitWhenInEditor"));
        }

        void SetupPauseWhenInEditor()
        {
            GameObject originalResumeBtn = pauseMenu.GetChildAt("LargeButtons/1_Resume");

            #region Resume Button
            // Setup the resume button, to actually resume the editor scene and not load another scene, which is the defualt behaviour of that button.
            GameObject resumeBtnWhenInsideLE = Instantiate(originalResumeBtn, originalResumeBtn.transform.parent);
            resumeBtnWhenInsideLE.name = "1_ResumeWhenInEditor";
            Destroy(resumeBtnWhenInsideLE.GetComponent<ButtonController>());
            resumeBtnWhenInsideLE.AddComponent<UIButtonPatcher>().onClick += EditorUIManager.Instance.Resume;
            // This two more lines are used just in case the original resume button is disabled, that may happen when you didn't start a new game yet.
            if (!resumeBtnWhenInsideLE.GetComponent<UIButton>().isEnabled)
            {
                resumeBtnWhenInsideLE.GetComponent<UIButton>().isEnabled = true;
                resumeBtnWhenInsideLE.GetComponent<UIButton>().ResetDefaultColor();
            }
            resumeBtnWhenInsideLE.SetActive(true);
            resumeButton = resumeBtnWhenInsideLE.GetComponent<UIButtonPatcher>();
            #endregion

            #region Exit Button
            // Same with exit button.
            GameObject originalExitBtn = pauseMenu.GetChildAt("LargeButtons/8_ExitGame");
            GameObject exitBtnWhenInsideLE = Instantiate(originalExitBtn, originalExitBtn.transform.parent);
            exitBtnWhenInsideLE.name = "7_ExitWhenInEditor";
            Destroy(exitBtnWhenInsideLE.GetComponent<ButtonController>());
            exitBtnWhenInsideLE.AddComponent<UIButtonPatcher>().onClick += EditorUIManager.Instance.ShowExitPopup;
            exitBtnWhenInsideLE?.SetActive(true);
            exitButton = exitBtnWhenInsideLE.GetComponent<UIButtonPatcher>();
            #endregion

            #region Save Button
            // Create a save level button.
            GameObject saveLevelButton = Instantiate(originalResumeBtn, originalResumeBtn.transform.parent);
            saveLevelButton.name = "3_SaveLevel";
            Destroy(saveLevelButton.GetComponent<ButtonController>());
            Destroy(saveLevelButton.GetChildWithName("Label").GetComponent<UILocalize>());
            saveLevelButton.GetChildWithName("Label").GetComponent<UILabel>().text = "Save Level";
            saveLevelButton.AddComponent<UIButtonPatcher>().onClick += SaveLevelWithPauseMenuButton;
            saveLevelButton.SetActive(true);
            saveButton = saveLevelButton.GetComponent<UIButtonPatcher>();
            #endregion

            #region Play Button
            // Create a PLAY level button.
            //GameObject playLevelButtonTemplate = pauseMenu.GetChildAt("LargeButtons/2_Chapters");
            GameObject playLevelButton = Instantiate(originalResumeBtn, originalResumeBtn.transform.parent);
            playLevelButton.name = "2_PlayLevel";
            Destroy(playLevelButton.GetComponent<ButtonController>());
            Destroy(playLevelButton.GetChildWithName("Label").GetComponent<UILocalize>());
            playLevelButton.GetChildWithName("Label").GetComponent<UILabel>().text = "Play Level";
            playLevelButton.AddComponent<UIButtonPatcher>().onClick += EditorUIManager.Instance.PlayLevel;
            playLevelButton.SetActive(true);
            playButton = playLevelButton.GetComponent<UIButtonPatcher>();
            #endregion
        }

        public void SaveLevelWithPauseMenuButton()
        {
            Logger.Log("Saving Level Data from pause menu...");
            LevelData.SaveLevelData(EditorController.Instance.levelName, EditorController.Instance.levelFileNameWithoutExtension);
            EditorUIManager.Instance.PlaySavingLevelLabel();
            EditorController.Instance.levelHasBeenModified = false;

            // Refresh the pause menu patch after saving...
            OnEnable();
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
