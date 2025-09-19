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

        GameObject resumeBtn;
        GameObject chaptersButton;
        GameObject newGamePlusButton;
        GameObject newGameButton;
        GameObject LEButton;
        GameObject returnToMenuButton;
        GameObject exitButton;

        GameObject popup;
        PopupController popupController;
        GameObject popupTitle;
        GameObject popupContentLabel;
        GameObject popupSmallButtonsParent;

        GameObject onExitPopupBackButton;
        GameObject onExitPopupSaveAndExitButton;
        GameObject onExitPopupExitButton;
        public bool exitPopupEnabled = false;

        GameObject resumeButtonLE;
        GameObject playButtonLE;
        GameObject saveButtonLE;
        GameObject exitButtonLE;

        public static void Create(GameObject pauseMenu)
        {
            patcher = pauseMenu.AddComponent<EditorPauseMenuPatcher>();
            patcher.pauseMenu = pauseMenu;
            patcher.GetReferences();
            patcher.CheckForExistingButtons();
            patcher.SetupPauseWhenInEditor();
        }
        void GetReferences()
        {
            GameObject largeButtons = pauseMenu.GetChild("LargeButtons");
            GameObject uiParentObj = GameObject.Find("MainMenu/Camera/Holder/");

            resumeBtn = largeButtons.GetChild("1_Resume");
            chaptersButton = largeButtons.GetChild("2_Chapters");
            newGamePlusButton = largeButtons.GetChild("3_NewGamePlus");
            newGameButton = largeButtons.GetChild("4_NewGame");
            LEButton = largeButtons.GetChild("6_Javi's LevelEditor");
            returnToMenuButton = largeButtons.GetChild("7_ReturnToMenu");
            exitButton = largeButtons.GetChild("8_ExitGame");

            popup = uiParentObj.GetChild("Popup");
            popupController = popup.GetComponent<PopupController>();
            popupTitle = popup.GetChildAt("PopupHolder/Title/Label");
            popupContentLabel = popup.GetChildAt("PopupHolder/Content/Label");
            popupSmallButtonsParent = popup.GetChildAt("PopupHolder/SmallButtons");
        }
        // If for some reason the pause menu already has the buttons, destroy them, just in case something bad happens.
        void CheckForExistingButtons()
        {
            GameObject largeButtons = pauseMenu.GetChild("LargeButtons");

            if (largeButtons.ExistsChild("1_ResumeWienInEditor")) Destroy(largeButtons.GetChild("1_ResumeWhenInEditor"));
            if (largeButtons.ExistsChild("2_PlayLevel")) Destroy(largeButtons.GetChild("2_PlayLevel"));
            if (largeButtons.ExistsChild("3_SaveLevel")) Destroy(largeButtons.GetChild("3_SaveLevel"));
            if (largeButtons.ExistsChild("7_ExitWhenInEditor")) Destroy(largeButtons.GetChild("7_ExitWhenInEditor"));
        }

        void SetupPauseWhenInEditor()
        {
            #region Resume Button
            // Setup the resume button, to actually resume the editor scene and not load another scene, which is the defualt behaviour of that button.
            resumeButtonLE = Instantiate(resumeBtn, resumeBtn.transform.parent);
            resumeButtonLE.name = "1_ResumeWhenInEditor";
            Destroy(resumeButtonLE.GetComponent<ButtonController>());
            resumeButtonLE.GetChild("LevelToResumeLabel").AddComponent<UILocalize>().key = "LevelEditor";
            resumeButtonLE.AddComponent<UIButtonPatcher>().onClick += EditorUIManager.Instance.Resume;
            // This two more lines are used just in case the original resume button is disabled, that may happen when you didn't start a new game yet.
            if (!resumeButtonLE.GetComponent<UIButton>().isEnabled)
            {
                resumeButtonLE.GetComponent<UIButton>().isEnabled = true;
                resumeButtonLE.GetComponent<UIButton>().ResetDefaultColor();
            }
            resumeButtonLE.SetActive(true);
            #endregion

            #region Play Button
            playButtonLE = Instantiate(resumeBtn, resumeBtn.transform.parent);
            playButtonLE.name = "2_PlayLevel";
            Destroy(playButtonLE.GetComponent<ButtonController>());
            playButtonLE.GetChild("Label").GetComponent<UILocalize>().key = "pause.PlayLevel";
            playButtonLE.GetChild("LevelToResumeLabel").AddComponent<UILocalize>().key = "pause.NoSpawnObject";
            playButtonLE.AddComponent<UIButtonPatcher>().onClick += EditorUIManager.Instance.PlayLevel;
            playButtonLE.GetComponent<UIButton>().defaultColor = NGUI_Utils.fsPauseButtonsDefaultColor;
            playButtonLE.SetActive(true);
            #endregion

            #region Save Button
            saveButtonLE = Instantiate(resumeBtn, resumeBtn.transform.parent);
            saveButtonLE.name = "3_SaveLevel";
            Destroy(saveButtonLE.GetComponent<ButtonController>());
            saveButtonLE.GetChild("Label").GetComponent<UILocalize>().key = "pause.SaveLevel";
            saveButtonLE.GetChild("LevelToResumeLabel").AddComponent<UILocalize>().key = "pause.NoChanges";
            saveButtonLE.AddComponent<UIButtonPatcher>().onClick += SaveLevelWithPauseMenuButton;
            saveButtonLE.GetComponent<UIButton>().defaultColor = NGUI_Utils.fsPauseButtonsDefaultColor;
            saveButtonLE.SetActive(true);
            #endregion

            #region Exit Button
            exitButtonLE = Instantiate(exitButton, exitButton.transform.parent);
            exitButtonLE.name = "7_ExitWhenInEditor";
            Destroy(exitButtonLE.GetComponent<ButtonController>());
            exitButtonLE.AddComponent<UIButtonPatcher>().onClick += ShowExitPopup;
            exitButtonLE.SetActive(true);
            #endregion
        }

        public void OnEnable()
        {
            Logger.DebugLog("LE pause menu enabled, patching!");
            GameObject pauseMenu = gameObject;
            GameObject navigation = transform.parent.GetChild("Navigation").gameObject;

            #region Large Buttons Stuff
            resumeBtn.SetActive(false);
            resumeButtonLE.SetActive(true);
            // The label of the "secondary" label is already set to "Level Editor" in the UILocalize comp, not need to set it again.

            chaptersButton.SetActive(false);
            newGamePlusButton.SetActive(false);
            newGameButton.SetActive(false);
            LEButton.SetActive(false);

            returnToMenuButton.SetActive(false);
            exitButton.SetActive(false);
            exitButtonLE.SetActive(true);

            PatchPlayLevelButton();
            PatchSaveLevelButton();

            pauseMenu.GetChildAt("LargeButtons").GetComponent<UITable>().Reposition();
            #endregion

            // The logic for changing the navigation bar buttons and their "on click" actions it's in Patches.cs ;)
            NavigationBarController.Instance.RefreshNavigationBarActions();
        }
        void PatchPlayLevelButton()
        {
            if (EditorController.Instance.currentInstantiatedObjects.Any(x => x is LE_Player_Spawn && x.gameObject.activeSelf))
            {
                playButtonLE.GetComponent<UISprite>().height = 80;
                playButtonLE.GetComponent<UISprite>().pivot = UIWidget.Pivot.Center;
                playButtonLE.GetComponent<UIButton>().isEnabled = true;
                playButtonLE.GetComponent<BoxCollider>().center = Vector3.zero;
                playButtonLE.GetChild("Label").GetComponent<UILabel>().height = 50;
                playButtonLE.GetChild("Label").GetComponent<UILabel>().transform.localPosition = Vector3.zero;
                playButtonLE.GetChild("LevelToResumeLabel").SetActive(false);
            }
            else
            {
                playButtonLE.GetComponent<UISprite>().height = 100;
                playButtonLE.GetComponent<UISprite>().pivot = UIWidget.Pivot.Top;
                playButtonLE.GetComponent<UIButton>().isEnabled = false;
                playButtonLE.GetChild("Label").GetComponent<UILabel>().height = 50;
                playButtonLE.GetChild("Label").GetComponent<UILabel>().transform.localPosition = new Vector3(0f, -32.5f, 0f);
                // Don't set the secondary label text, since it's already in the UILocalize component.
                playButtonLE.GetChild("LevelToResumeLabel").SetActive(true);
            }
        }
        void PatchSaveLevelButton()
        {
            if (EditorController.Instance.levelHasBeenModified)
            {
                saveButtonLE.GetComponent<UISprite>().height = 80;
                saveButtonLE.GetComponent<UISprite>().pivot = UIWidget.Pivot.Center;
                saveButtonLE.GetComponent<UIButton>().isEnabled = true;
                saveButtonLE.GetComponent<BoxCollider>().center = Vector3.zero;
                saveButtonLE.GetChild("Label").GetComponent<UILabel>().height = 50;
                saveButtonLE.GetChild("Label").GetComponent<UILabel>().transform.localPosition = Vector3.zero;
                saveButtonLE.GetChild("LevelToResumeLabel").SetActive(false);
            }
            else
            {
                saveButtonLE.GetComponent<UISprite>().height = 100;
                saveButtonLE.GetComponent<UISprite>().pivot = UIWidget.Pivot.Top;
                saveButtonLE.GetComponent<UIButton>().isEnabled = false;
                saveButtonLE.GetChild("Label").GetComponent<UILabel>().height = 50;
                saveButtonLE.GetChild("Label").GetComponent<UILabel>().transform.localPosition = new Vector3(0f, -32.5f, 0f);
                // Don't set the secondary label text, since it's already in the UILocalize component.
                saveButtonLE.GetChild("LevelToResumeLabel").SetActive(true);
            }
        }

        public void ShowExitPopup()
        {
            if (!EditorController.Instance.levelHasBeenModified)
            {
                EditorUIManager.Instance.ExitToMenu(false);
                return;
            }

            #region Popup Setup
            popupTitle.GetComponent<UILabel>().text = Loc.Get("pause.ExitPopup.Title");
            popupContentLabel.GetComponent<UILabel>().text = Loc.Get("pause.ExitPopup.Content");
            popupSmallButtonsParent.DisableAllChildren();
            popupSmallButtonsParent.transform.localPosition = new Vector3(-10f, -315f, 0f);
            popupSmallButtonsParent.GetComponent<UITable>().padding = new Vector2(10f, 0f);
            #endregion

            #region Back Button
            // Make a copy of the yess button since for some reason the yes button is red as the no button should, that's doesn't make any sense lol.
            onExitPopupBackButton = Instantiate(popupSmallButtonsParent.GetChildAt("3_Yes"), popupSmallButtonsParent.transform);
            onExitPopupBackButton.name = "1_Back";
            onExitPopupBackButton.transform.localPosition = new Vector3(-400f, 0f, 0f);
            onExitPopupBackButton.RemoveComponent<ButtonController>();
            onExitPopupBackButton.GetChild("Label").GetComponent<UILocalize>().key = "pause.ExitPopup.Back";
            onExitPopupBackButton.GetComponent<UIButtonScale>().hover = Vector3.one * 1.05f;
            onExitPopupBackButton.GetComponent<UIButtonScale>().pressed = Vector3.one * 0.95f;
            onExitPopupBackButton.AddComponent<UIButtonPatcher>().onClick += () => OnExitPopupButtonClicked(false, false);
            onExitPopupBackButton.SetActive(true);
            #endregion

            #region Save and Exit Button
            onExitPopupSaveAndExitButton = Instantiate(popupSmallButtonsParent.GetChildAt("3_Yes"), popupSmallButtonsParent.transform);
            onExitPopupSaveAndExitButton.name = "2_SaveAndExit";
            onExitPopupSaveAndExitButton.transform.localPosition = new Vector3(-400f, 0f, 0f);
            onExitPopupSaveAndExitButton.RemoveComponent<ButtonController>();
            onExitPopupSaveAndExitButton.GetChild("Label").GetComponent<UILocalize>().key = "pause.ExitPopup.SaveAndExit";
            onExitPopupSaveAndExitButton.GetComponent<UIButtonScale>().hover = Vector3.one * 1.05f;
            onExitPopupSaveAndExitButton.GetComponent<UIButtonScale>().pressed = Vector3.one * 0.95f;
            onExitPopupSaveAndExitButton.AddComponent<UIButtonPatcher>().onClick += () => OnExitPopupButtonClicked(true, true);
            onExitPopupSaveAndExitButton.SetActive(true);
            #endregion

            #region Exit Without Saving Button
            // Same with exit button.
            onExitPopupExitButton = Instantiate(popupSmallButtonsParent.GetChildAt("1_No"), popupSmallButtonsParent.transform);
            onExitPopupExitButton.name = "3_ExitWithoutSaving";
            onExitPopupExitButton.transform.localPosition = new Vector3(200f, 0f, 0f);
            onExitPopupExitButton.RemoveComponent<ButtonController>();
            onExitPopupExitButton.GetChild("Label").GetComponent<UILocalize>().key = "pause.ExitPopup.ExitNoSave";
            onExitPopupExitButton.GetComponent<UIButtonScale>().hover = Vector3.one * 1.05f;
            onExitPopupExitButton.GetComponent<UIButtonScale>().pressed = Vector3.one * 0.95f;
            onExitPopupExitButton.GetComponent<UIButton>().defaultColor = new Color(0.3897f, 0.212f, 0.212f, 1f);
            onExitPopupExitButton.AddComponent<UIButtonPatcher>().onClick += () => OnExitPopupButtonClicked(true, false);
            onExitPopupExitButton.SetActive(true);
            #endregion

            popupController.Show();
            exitPopupEnabled = true;
            Logger.Log("Showed LE exit popup!");
        }
        public void OnExitPopupButtonClicked(bool exitToMenu, bool saveLevel)
        {
            popupController.Hide();
            exitPopupEnabled = false;

            Destroy(onExitPopupBackButton);
            Destroy(onExitPopupSaveAndExitButton);
            Destroy(onExitPopupExitButton);

            popupSmallButtonsParent.transform.localPosition = new Vector3(-130f, -315f, 0f);
            popupSmallButtonsParent.GetComponent<UITable>().padding = new Vector2(130f, 0f);

            // ------------------------------

            if (exitToMenu)
            {
                EditorUIManager.Instance.ExitToMenu(saveLevel);
            }
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

        public void BeforeDestroying()
        {
            Logger.DebugLog("About to destroy LE pause menu patcher!");

            Destroy(resumeButtonLE);
            Destroy(playButtonLE);
            Destroy(saveButtonLE);
            Destroy(exitButtonLE);
        }
    }
}
