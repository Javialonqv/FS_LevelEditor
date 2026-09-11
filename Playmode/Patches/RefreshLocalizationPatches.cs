using FS_LevelEditor.Editor;
using HarmonyLib;
using Il2Cpp;
using UnityEngine;

namespace FS_LevelEditor.Playmode.Patches
{
    [HarmonyPatch(typeof(FractalSave), nameof(FractalSave.RefreshLocalizationFromCloud))]
    public static class RefreshLocalizationPatches
    {
        public static bool Prefix()
        {
            // if it's either Editor or Playmode, ignore Shift+L download.
            if (EditorController.Instance != null || PlayModeController.Instance != null)
            {
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(InGameUIManager), nameof(InGameUIManager.ShowNotification))]
    public static class InGameUIRefreshLocalizationPatches
    {
        public static bool Prefix(InGameUIManager.NotificationType _type)
        {
            if ((EditorController.Instance != null || PlayModeController.Instance != null) && _type == InGameUIManager.NotificationType.REFRESHING_LOCALIZATION)
            {
                return false;
            }
            return true;
        }
    }
}
