using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;
using MelonLoader;

namespace FS_LevelEditor
{
    // I need this path since for some reason the UIInput is causing an error in the Start() method, for fortunely that function doesn't have anything important.
    [HarmonyPatch(typeof(UIInput), nameof(UIInput.Start))]
    public static class UIInputPatch
    {
        public static bool Prefix()
        {
            return false;
        }
    }

    // To prevent the exit popup from appearing in the main menu if the user is exiting from the LE pause.
    // Also to prevent it when it's inside of the LE menu (level selection).
    [HarmonyPatch(typeof(MenuController), nameof(MenuController.ShowExitConfirmationPopup))]
    public static class ExitPatch
    {
        public static bool Prefix()
        {
            if (EditorController.Instance != null || LE_MenuUIManager.Instance.inLEMenu || LE_MenuUIManager.Instance.isInMidTransition)
            {
                return false;
            }

            return true;
        }
    }

    // From 0.604, MenuController.ReturnToMainMenu also calls this function, which causes an error when it's called
    // from LE when exiting to menu (since LE is running in the Menu scene and not ingame).
    // Anyways, patch that function so it ISN'T executing when in LE.
    [HarmonyPatch(typeof(InGameUIManager), nameof(InGameUIManager.CancelPlayRequestForDialogs))]
    public static class InGameUIManagerCancelDialogsMethodPatch
    {
        public static bool Prefix()
        {
            return EditorController.Instance == null;
        }
    }

    [HarmonyPatch(typeof(MenuController), nameof(MenuController.ShowMenuBG))]
    public static class PlaymodeLoadBGImagePatch
    {
        public static void Postfix(MenuController __instance)
        {
            if (Melon<Core>.Instance.loadCustomLevelOnSceneLoad)
            {
                __instance.menuBGTexture.mainTexture = null;
            }
        }
    }

    [HarmonyPatch(typeof(NavigationBarController), nameof(NavigationBarController.GetActionsList))]
    public static class NavigationBarControllerPatch
    {
        public static bool Prefix(ref Il2CppSystem.Collections.Generic.List<NavigationBarController.ActionType> __result)
        {
            var toReturn = new Il2CppSystem.Collections.Generic.List<NavigationBarController.ActionType>();
            if (EditorPauseMenuPatcher.patcher)
            {
                // If the patcher exists and the UI context is MainMenu, that means it's in LE and it's paused, since LE is running in Main Menu lol.
                if (MenuController.GetInstance().GetUIContext() == MenuController.UIContext.MAIN_MENU)
                {
                    toReturn.Add(NavigationBarController.ActionType.QUIT);
                }
            }
            else if (LE_MenuUIManager.Instance)
            {
                if (LE_MenuUIManager.Instance.inLEMenu)
                {
                    toReturn.Add(NavigationBarController.ActionType.BACK);
                }
            }

            if (toReturn.Count > 0)
            {
                __result = toReturn;
                return false;
            }
            else
            {
                return true;
            }
        }
    }
    [HarmonyPatch(typeof(NavigationBarController), nameof(NavigationBarController.ManualButtonPressed))]
    public static class NavigabtionButtonPressedPatch
    {
        public static bool Prefix(NavigationBarController.ActionType _type)
        {
            switch (_type)
            {
                case NavigationBarController.ActionType.QUIT:
                    if (EditorPauseMenuPatcher.patcher)
                    {
                        // If the patcher exists and the UI context is MainMenu, that means it's in LE and it's paused, since LE is running in Main Menu lol.
                        if (MenuController.GetInstance().GetUIContext() == MenuController.UIContext.MAIN_MENU)
                        {
                            EditorUIManager.Instance.ShowExitPopup();
                            return false;
                        }
                    }
                    break;

                case NavigationBarController.ActionType.BACK:
                    if (LE_MenuUIManager.Instance)
                    {
                        if (LE_MenuUIManager.Instance.inLEMenu)
                        {
                            LE_MenuUIManager.Instance.SwitchBetweenMenuAndLEMenu();
                            return false;
                        }
                    }
                    break;
            }

            return true;
        }
    }
}
