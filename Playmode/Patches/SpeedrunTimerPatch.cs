using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using UnityEngine.SceneManagement;

namespace FS_LevelEditor.Playmode.Patches
{
    [HarmonyPatch(typeof(InGameUIManager), "ShowTimers")]
    public class SpeedrunTimerFix
    {
        public static void Postfix(InGameUIManager __instance)
        {
            if (PlayModeController.Instance != null)
            {
                //Guess what it does BASED on the class name.
                __instance.currentLevelRankLabel.gameObject.SetActive(false);
                if (FractalSave.HasKey($"{PlayModeController.Instance.levelName}_Time"))
                {
                    __instance.currentLevelBestTimeLabel.bestTimeLabel.text = Controls.GetFormattedElapsedTimeFromSeconds(FractalSave.GetInt($"{PlayModeController.Instance.levelName}_Time"));
                   
                }

            }
        }
    }
}
