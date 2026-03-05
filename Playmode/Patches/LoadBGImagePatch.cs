using Il2Cpp;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using FS_LevelEditor.SaveSystem;
using UnityEngine;

namespace FS_LevelEditor.Playmode.Patches
{
    [HarmonyPatch(typeof(MenuController), nameof(MenuController.ShowMenuBG))]
    public static class PlaymodeLoadBGImagePatch
    {
        public static void Postfix(MenuController __instance)
        {
            if (Melon<Core>.Instance.loadCustomLevelOnSceneLoad)
            {
                string levelFileName = Melon<Core>.Instance.levelFileNameWithoutExtensionToLoad;
                LevelData levelData = LevelData.GetLevelData(levelFileName);

                if (levelData != null && !string.IsNullOrEmpty(levelData.thumbnailBase64))
                {
                    byte[] imageBytes = Convert.FromBase64String(levelData.thumbnailBase64);
                    Texture2D texture = new Texture2D(2, 2);
                    if (texture.LoadImage(imageBytes))
                    {
                        __instance.menuBGTexture.mainTexture = texture;
                        return;
                    }
                }

                __instance.menuBGTexture.mainTexture = null;
            }
        }
    }
}
