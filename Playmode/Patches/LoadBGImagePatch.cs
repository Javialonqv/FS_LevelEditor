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
    [HarmonyPatch(typeof(MenuController), nameof(MenuController.ShowMenuBG))]
    public static class PlaymodeLoadBGImagePatch
    {
        public static void Postfix(MenuController __instance)
        {
            if (Melon<Core>.Instance.loadCustomLevelOnSceneLoad)
            {
                __instance.menuBGTexture.mainTexture = null;
            }
        }
    }
}
