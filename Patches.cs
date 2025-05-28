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
}
