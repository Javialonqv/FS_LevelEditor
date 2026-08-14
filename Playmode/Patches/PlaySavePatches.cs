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
using UnityEngine;

namespace FS_LevelEditor.Playmode.Patches
{
    [HarmonyPatch(typeof(FractalSave), nameof(FractalSave.SaveKey), [typeof(string), typeof(string), typeof(bool), typeof(bool)])]
    public static class SaveKeyPatch
    {
        static readonly string[] bannedPersistentKeys =
        {
            "Last_Checkpoint",
            "Current_Level",
            "Cycle_Count",
            "TotalSecondsPlayed",
            "TotalSecsPlayed_",
            "Current_Health",
            "Current_Ammo",
            "Current_MaxAmmo",
            "Current_Ammo_EndOfLastLevel",
            "Current_MaxAmmo_EndOfLastLevel",
            "Health_Upgrade_Level",
            "Jetpack_Upgrade_Level",
            "Health_Backpack_Upgrade_Level",
            "Taser_Upgrade_Level",
            "TotalUpgrades",
            "TotalUpgradesEver",
            "Upgrades_EndGameMode",
            "Upgrades_Tutorial_Seen",
            "UpgradeTerminalUsed",
            "UpgradeComputer_",
            "Has_Gun",
            "Has_JetPack",
            "Has_Sprint",
            "Has_Dodge",
            "Jetpack_PickedUpAtLeastOnce",
            "firstHealPackPicked",
            "Has_InfraredFlashlight",
            "HasAtLeastOneUpgrade",
            "Available_Healthpacks",
            "Available_TaserClips",
            "NewGame",
            "Finished_Chapter4_AtLeastOnce",
            "Finished_Chapter3_AtLeastOnce"
        };

        public static bool Prefix(string _key, bool _persistent)
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

            if (_persistent && PlayModeController.Instance)
            {
                if (bannedPersistentKeys.Contains(_key))
                    return false;

                if (_key.Contains("TotalSecsPlayed_"))
                    return false;
                if (_key.Contains("UpgradeComputer_"))
                    return false;
                if (_key.Contains("Stat_"))
                    return false;
                if (_key.Contains("Upgrade_"))
                    return false;
                if (_key.Contains("FinishedAlpha_"))
                    return false;

                return true;
            }

            // Prevent saving when in playmode.
            return !PlayModeController.Instance;
        }
    }
    [HarmonyPatch(typeof(FractalSave), nameof(FractalSave.SaveLevelKey), [typeof(string), typeof(string), typeof(bool), typeof(bool)])]
    public static class SaveLevelKeyPatch
    {
        public static bool Prefix(string _key)
        {
            // Don't save when the user is ending the level in playmode.
            if (PlayModeController.Instance && PlayModeController.Instance.endTriggerReached)
            {
                return false;
            }

            //// Don't save the current level when you're loading playmode (which will be Chapter 4).
            //if (PlayModeController.Instance || Melon<Core>.Instance.loadCustomLevelOnSceneLoad)
            //{
            //    if (_key == "Current_Level" || _key == "Last_Checkpoint")
            //    {
            //        return false;
            //    }
            //}

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

        public static void DeleteCurrentLevelAutoSaveFileIfExists(string levelName)
        {
            string path = Path.Combine(Application.persistentDataPath, $"CustomLevel_{levelName}_AutoSave.dat");

            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [HarmonyPatch(typeof(FractalSave), nameof(FractalSave.SetSaveFileName))]
    public static class SaveFileNamePatch
    {
        public static void Postfix(FractalSave __instance, string _new)
        {
            if (PlayModeController.Instance)
            {
                __instance.m_saveFileName = $"CustomLevel_{PlayModeController.Instance.levelName}_AutoSave.dat";
                FractalSave.quickSaveFileName = $"CustomLevel_{PlayModeController.Instance.levelName}_AutoSave.dat";
            }
        }
    }
}
