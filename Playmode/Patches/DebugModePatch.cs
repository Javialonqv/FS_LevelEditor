using HarmonyLib;
using Il2Cpp;

namespace FS_LevelEditor.Playmode.Patches
{
    [HarmonyPatch(typeof(Controls), nameof(Controls.ToggleDevMode))]
    public static class  DebudModePatch
    {
        public static bool DebugAllowed { get; set; } = true;
        static bool Prefix()
        {
            if(PlayModeController.Instance)
            {
                return DebugAllowed;
            }
            return true;
        }
    }
}