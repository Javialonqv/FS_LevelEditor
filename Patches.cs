using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;

namespace FS_LevelEditor
{
    // I need this path since for some reason the UIInput is causing an error in the Start() method, for fortunely that function doesn't have anything important.
    [HarmonyPatch(typeof(UIInput), nameof(UIInput.Start))]
    public static class UIInputPatch
    {
        public static bool Prefix()
        {
            return false;
        }
    }
}
