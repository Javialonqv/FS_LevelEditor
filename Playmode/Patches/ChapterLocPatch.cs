using Il2Cpp;
using MelonLoader;
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
			// Check if we're in a custom level by verifying both PlayModeController exists 
			// and Core.loadCustomLevelOnSceneLoad was true when loading
			if (key == "Chapter4" && PlayModeController.Instance &&
				Melon<Core>.Instance.levelFileNameWithoutExtensionToLoad != null)
			{
				__result = PlayModeController.Instance.levelName;
				return false; // Skip the original method
			}
            if (key == "Notification_FlashlightNotOperational")
            {
                __result = TranslationsManager.GetTranslation("FlashlightNotOperational", false);
                return false;
            }
            if (key == "Notification_FlashlightOperational")
            {
                __result = TranslationsManager.GetTranslation("FlashlightOperational", false);
                return false;
            }
            if (key == "Notification_TASER_WATER_UNUSABLE")
            {
                __result = TranslationsManager.GetTranslation("TASER_WATER_UNUSABLE", false);
                return false;
            }


            return true;
		}
	}
}