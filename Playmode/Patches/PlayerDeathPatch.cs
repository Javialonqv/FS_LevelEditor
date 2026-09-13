using HarmonyLib;

namespace FS_LevelEditor.Playmode.Patches
{
    [HarmonyPatch(typeof(Controls), nameof(Controls.KillCharacter), [typeof(bool), typeof(bool)])]
    public static class PlayerDeathPatch
    {
        public static void Prefix()
        {
            if (PlayModeController.Instance != null)
            {
                Core.Instance.totalDeathsInCurrentPlaymodeSession++;

                PlayModeController.Instance.CleanupAllObjectives();

                // Set this variable true again so when the scene is reloaded, the custom level is as well.
                // The level file name inside of the Core class still there for cases like this one, so we don't need to get it again.
                Core.Instance.loadCustomLevelOnSceneLoad = true;
            }
        }
    }
}