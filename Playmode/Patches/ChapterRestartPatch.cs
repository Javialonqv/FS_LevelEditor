using Il2Cpp;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace FS_LevelEditor.Playmode.Patches
{
    [HarmonyPatch(typeof(MenuController), nameof(MenuController.RestartCurrentLevelConfirmed))]
    public static class ChapterRestartPatch
    {
        public static void Prefix()
        {
            if (PlayModeController.Instance != null)
            {
                // The asset bundle will be unloaded automatically in the PlayModeController class, since OnDestroy will be triggered.

                // Set this variable true again so when the scene is restarted, the custom level is as well.
                // The level file name inside of the Core class still there for cases like this one, so we don't need to get it again.
                Melon<Core>.Instance.loadCustomLevelOnSceneLoad = true;
            }
        }
    }
}
