using FS_LevelEditor.SaveSystem;
using HarmonyLib;
using Il2Cpp;
using Il2CppInControl;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FS_LevelEditor.Playmode.Patches
{
    [HarmonyPatch(typeof(FractalSave), nameof(FractalSave.SaveKey), [typeof(string), typeof(string), typeof(bool), typeof(bool)])]
    public static class SaveKeyIntPatch
    {
        public static bool Prefix(string _key)
        {
            // Don't save when the user is ending the level in playmode.
            if (PlayModeController.Instance && PlayModeController.Instance.endTriggerReached)
            {
                // Except when it's saving time.
                // ??? This thing is not even being used, explain GrAI.
                if (_key.EndsWith("_LETime"))
                {
                    return true;
                }

                return false;
            }

            // Don't save the current level when you're loading playmode (which will be Chapter 4).
            if (PlayModeController.Instance || Melon<Core>.Instance.loadCustomLevelOnSceneLoad)
            {
                if (_key == "Current_Level" || _key == "Last_Checkpoint")
                {
                    return false;
                }
            }

            return true;
        }
    }
    [HarmonyPatch(typeof(FractalSave), nameof(FractalSave.SaveLevelKey), [typeof(string), typeof(string), typeof(bool), typeof(bool)])]
    public static class SaveLevelKeyIntPatch
    {
        public static bool Prefix(string _key)
        {
            // Don't save when the user is ending the level in playmode.
            if (PlayModeController.Instance && PlayModeController.Instance.endTriggerReached)
            {
                return false;
            }

            // Don't save the current level when you're loading playmode (which will be Chapter 4).
            if (PlayModeController.Instance || Melon<Core>.Instance.loadCustomLevelOnSceneLoad)
            {
                if (_key == "Current_Level" || _key == "Last_Checkpoint")
                {
                    return false;
                }
            }

            return true;
        }
    }
    [HarmonyPatch(typeof(FractalSave), nameof(FractalSave.DeleteQuickSaveSaveForAllLevels))]
    public static class DeleteQuickSaveFilesPatch
    {
        public static bool Prefix()
        {
            // Don't delete quick save files when about to play an LE level.
            if (PlayModeController.Instance || Melon<Core>.Instance.loadCustomLevelOnSceneLoad)
            {
                return false;
            }

            return true;
        }
    }
    [HarmonyPatch(typeof(FractalSave), nameof(FractalSave.DeleteAutoSaveSaveForAllLevels))]
    public static class DeleteAutoSaveFilesPatch
    {
        public static bool Prefix()
        {
            // Don't delete auto save files when about to play an LE level.
            if (PlayModeController.Instance || Melon<Core>.Instance.loadCustomLevelOnSceneLoad)
            {
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(FractalSave), nameof(FractalSave.GetInt))]
    public static class FractalSaveGetIntPatches
    {
        public static bool Prefix(ref int __result, string _key)
        {
            if (PlayModeController.Instance)
            {
                if (_key == "Total_Deaths")
                {
                    __result = PlayModeController.Instance.deathsInCurrentLevel;
                    return false;
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(FractalSave), nameof(FractalSave.SetSaveFileName))]
    public static class SaveFileNamePatch
    {
        public static void Postfix(FractalSave __instance, string _new)
        {
            if (PlayModeController.Instance)
            {
                __instance.m_saveFileName = $"{PlayModeController.Instance.levelName}.dat";
            }
        }
    }
}
