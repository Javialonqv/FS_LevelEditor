using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;

namespace FS_LevelEditor.Playmode.Patches
{
    [HarmonyPatch(typeof(InGameUIManager), nameof(InGameUIManager.GetChapterTitle))]
    public static class ChapterTextTitlePatch
    {
        public static bool Prefix(ref string __result)
        {
            if (PlayModeController.Instance != null)
            {
                __result = "CUSTOM LEVEL";
                return false;
            }
            else
            {
                return true;
            }
        }
    }
    [HarmonyPatch(typeof(InGameUIManager), nameof(InGameUIManager.GetChapterName))]
    public static class ChapterTextNamePatch
    {
        public static bool Prefix(ref string __result)
        {
            if (PlayModeController.Instance != null)
            {
                __result = PlayModeController.Instance.levelName.ToUpper();
                return false;
            }
            else
            {
                return true;
            }
        }

        public static void Postfix()
        {
            if (PlayModeController.Instance != null)
            {
                // For some STUPID reason, the chapter display doesn't show "CUSTOM LEVEL" as title, it seems the GetChapterTitle function isn't patched at all after FS 0.604.
                // Anyways, if it doesn't work, then modify the text directly when this function of get chapter name is called ;)
                GameObject.Find("(singleton) InGameUIManager/Camera/Panel/ChapterDisplay/Holder/ChapterTitleLabel").GetComponent<UILabel>().text = "CUSTOM LEVEL";
            }
        }
    }
}
