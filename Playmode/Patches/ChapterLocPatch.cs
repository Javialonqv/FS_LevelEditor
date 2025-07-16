using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FS_LevelEditor.Playmode.Patches
{
    [HarmonyLib.HarmonyPatch(typeof(Localization), nameof(Localization.Get))]
    public static class ChapterLocPatch
    {
        public static bool Prefix(ref string __result, string key)
        {
            if (key == "Chapter4" && PlayModeController.Instance)
            {
                __result = PlayModeController.Instance.levelName;
                return false; // Skip the original method
            }

            return true;
        }
    }
}
