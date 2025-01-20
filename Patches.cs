using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FS_LevelEditor
{
    // I need this path since for some reason the UIInput is causing an error in the Start() method, for fortunely that function doesn't have anything important.
    [HarmonyLib.HarmonyPatch(typeof(UIInput), nameof(UIInput.Start))]
    public static class UIInputPatch
    {
        public static bool Prefix()
        {
            return false;
        }
    }
}
