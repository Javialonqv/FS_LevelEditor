using FS_LevelEditor.SaveSystem;
using HarmonyLib;
using UnityEngine;

namespace FS_LevelEditor.Playmode.Patches
{
    [HarmonyPatch(typeof(MenuController), nameof(MenuController.ShowMenuBG))]
    public static class PlaymodeLoadBGImagePatch
    {
        public static void Postfix(MenuController __instance)
        {
            if (Core.Instance.loadCustomLevelOnSceneLoad)
            {
                string levelFileName = Core.Instance.levelFileNameWithoutExtensionToLoad;
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
