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
    [HarmonyPatch(typeof(Controls), nameof(Controls.KillCharacter), [typeof(bool), typeof(bool)])]
    public static class PlayerDeathPatch
    {
        public static void Prefix()
        {
            if (PlayModeController.Instance != null)
            {
				Melon<Core>.Instance.totalDeathsInCurrentPlaymodeSession++;

				// Ensure proper cleanup before reload
				if (PlayModeController.Instance.levelObjectsParent != null)
				{
					UnityEngine.Object.Destroy(PlayModeController.Instance.levelObjectsParent);
				}

				UnityEngine.Object.Destroy(PlayModeController.Instance.gameObject);

				// Set this variable true again so when the scene is reloaded, the custom level is as well.
				// The level file name inside of the Core class still there for cases like this one, so we don't need to get it again.
				Melon<Core>.Instance.loadCustomLevelOnSceneLoad = true;
			}
        }
    }
}
