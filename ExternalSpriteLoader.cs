using System;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Il2Cpp;
using MelonLoader;
using UnityEngine.UIElements;
using System.Runtime.CompilerServices;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class ExternalSpriteLoader : MonoBehaviour
    {
        public static ExternalSpriteLoader Instance;

        Il2CppAssetBundle assetBundle;
        Sprite[] bundleSprites;
        Texture spriteTexture;
        public UIAtlas spriteAtlas;

        void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAssetBundle();

            spriteAtlas = CreateAtlas(spriteTexture, bundleSprites);
        }

        void LoadAssetBundle()
        {
            Stream assetStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FS_LevelEditor.level_editor_sprites");
            byte[] assetBytes = new byte[assetStream.Length];
            assetStream.Read(assetBytes);

            assetBundle = Il2CppAssetBundleManager.LoadFromMemory(assetBytes);

            bundleSprites = assetBundle.LoadAll<Sprite>();
            foreach (var sprite in bundleSprites)
            {
                sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
            }
            spriteTexture = bundleSprites[0].texture;

            assetStream.Close();
            assetBundle.Unload(false);
        }

        UIAtlas CreateAtlas(Texture mainTexture, Sprite[] sprites)
        {
            // Create atlas.
            UIAtlas atlas = gameObject.AddComponent<UIAtlas>();

            // Create a material for the atlas.
            Material material = new Material(Shader.Find("Unlit/Transparent Colored"));
            material.mainTexture = mainTexture;

            // Asign the material to the atlas
            atlas.spriteMaterial = material;

            foreach (var sprite in sprites)
            {
                int realYPos = (int)(mainTexture.height - (sprite.textureRect.y + sprite.textureRect.height));

                UISpriteData spriteData = new UISpriteData
                {
                    name = sprite.name,
                    x = (int)sprite.textureRect.x,
                    //y = (int)sprite.textureRect.y,
                    y = realYPos,
                    width = (int)sprite.textureRect.width,
                    height = (int)sprite.textureRect.height,
                    borderLeft = (int)sprite.border.x,
                    borderRight = (int)sprite.border.z,
                    borderTop = (int)sprite.border.w,
                    borderBottom = (int)sprite.border.y,
                    paddingLeft = 0,
                    paddingRight = 0,
                    paddingTop = 0,
                    paddingBottom = 0,
                };

                atlas.spriteList.Add(spriteData);
            }

            atlas.MarkAsChanged();

            return atlas;
        }
    }

    public static class ExternalSpriteLoaderExtension
    {
        public static void SetExternalSprite(this UISprite sprite, string spriteName)
        {
            if (ExternalSpriteLoader.Instance == null)
            {
                Melon<Core>.Logger.Error("External Sprite Loader not initialized yet.");
                return;
            }

            if (ExternalSpriteLoader.Instance.spriteAtlas.GetSprite(spriteName) != null)
            {
                sprite.atlas = ExternalSpriteLoader.Instance.spriteAtlas;
                sprite.name = spriteName;
            }
        }
    }
}
