using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace FS_LevelEditor.Playmode.Patches
{
    [HarmonyPatch(typeof(Controls), nameof(Controls.GetLevelDeathYLimit))]
    public static class DeathLimitPatch
    {
        public static bool Prefix(ref float __result)
        {
            if (PlayModeController.Instance)
            {
                __result = (float)PlayModeController.Instance.globalProperties["DeathYLimit"];
                return false;
            }

            return true;
        }
    }
}
