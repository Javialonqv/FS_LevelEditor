using UnityEngine;
using Object = UnityEngine.Object;

namespace FS_LevelEditor
{
    /// <summary>
    /// The level's global fog (the "GlobalFog*" global properties). It's a screen-space depth fog (post-process on the world camera), NOT Unity's
    /// RenderSettings fog: the game's shaders ignore that one, so it was only visible on the water.
    /// It's independent from the water fog: LE_Water has its own post effect on the same camera, and both can be active at the same time.
    /// The shader source is in Misc/LevelFog.shader.txt, it's compiled into the embedded "level_fog" asset bundle.
    /// </summary>
    public static class LevelFog
    {
        // The scene's own fog settings, to put them back when the fog is cleared (see ApplySceneFog).
        static bool hasSavedSceneFog;
        static bool oldFog;
        static Color oldFogColor;
        static FogMode oldFogMode;
        static float oldFogDensity;

        /// <summary>
        /// The camera that renders the world: the game's env camera in playmode, the main camera in the editor.
        /// </summary>
        static Camera GetTargetCamera()
        {
            if (Controls.Instance != null && Controls.Instance.envCamera != null) return Controls.Instance.envCamera;
            return Camera.main;
        }

        public static void Apply(bool enabled, string colorHex, float density, float start, float end)
        {
            if (!enabled)
            {
                Clear();
                return;
            }

            Camera cam = GetTargetCamera();
            if (cam == null) return;

            LevelFogEffect effect = cam.GetComponent<LevelFogEffect>();
            if (effect == null) effect = cam.gameObject.AddComponent<LevelFogEffect>();

            effect.color = Utils.HexToColor(colorHex, false, null) ?? Color.gray;
            effect.density = density;
            effect.startDistance = start;
            effect.endDistance = Mathf.Max(end, start + 0.1f);
            effect.enabled = true;

            ApplySceneFog(effect.color, density);
        }

        /// <summary>
        /// The post effect only fogs what's drawn before transparent objects (see LevelFogEffect), so transparent things like the water surface and the
        /// rain use Unity's own scene fog instead. Exponential mode is the same curve as the post effect's (minus the start distance).
        /// </summary>
        static void ApplySceneFog(Color color, float density)
        {
            if (!hasSavedSceneFog)
            {
                oldFog = RenderSettings.fog;
                oldFogColor = RenderSettings.fogColor;
                oldFogMode = RenderSettings.fogMode;
                oldFogDensity = RenderSettings.fogDensity;
                hasSavedSceneFog = true;
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = color;
            RenderSettings.fogDensity = density;
        }

        /// <summary>
        /// Applies the fog from a global properties dictionary. Falls back to the defaults for missing/invalid values so an odd save can't break loading.
        /// </summary>
        public static void Apply(Dictionary<string, object> globalProperties)
        {
            Apply(Get(globalProperties, "GlobalFog", false),
                Get(globalProperties, "GlobalFogColor", "#808080"),
                Get(globalProperties, "GlobalFogDensity", 0.01f),
                Get(globalProperties, "GlobalFogStartDistance", 0f),
                Get(globalProperties, "GlobalFogEndDistance", 300f));
        }
        public static bool IsEnabled(Dictionary<string, object> globalProperties) => Get(globalProperties, "GlobalFog", false);

        /// <summary>
        /// Removes the fog.
        /// </summary>
        public static void Clear()
        {
            foreach (var effect in Object.FindObjectsOfType<LevelFogEffect>())
            {
                Object.Destroy(effect);
            }

            if (hasSavedSceneFog)
            {
                RenderSettings.fog = oldFog;
                RenderSettings.fogColor = oldFogColor;
                RenderSettings.fogMode = oldFogMode;
                RenderSettings.fogDensity = oldFogDensity;
                hasSavedSceneFog = false;
            }
        }

        static T Get<T>(Dictionary<string, object> properties, string key, T fallback)
        {
            if (properties != null && properties.TryGetValue(key, out var value) && value is T typed) return typed;
            return fallback;
        }
    }

    public class LevelFogEffect : MonoBehaviour
    {
        public Color color = Color.gray;
        public float density = 0.01f;
        public float startDistance = 0f;
        public float endDistance = 300f;

        static Shader shader;
        Material material;

        void OnEnable()
        {
            Camera cam = GetComponent<Camera>();
            if (cam != null) cam.depthTextureMode |= DepthTextureMode.Depth;

            if (shader == null) shader = AssetBundleLoader.LoadAsset<Shader>("LevelFog", "level_fog");
            if (shader != null && material == null) material = new Material(shader);
        }

        void OnDisable()
        {
            if (material != null) Destroy(material);
            material = null;
        }

        // ImageEffectOpaque: runs after the opaque geometry and the sky but BEFORE the transparent objects are drawn, so things like the rain are drawn
        // on top of the fog instead of being covered by it (the depth buffer knows nothing about transparent objects, so they'd get fogged as the sky).
        [ImageEffectOpaque]
        void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (material == null)
            {
                Graphics.Blit(src, dest);
                return;
            }

            // The color comes from a hex value (gamma space), the shader needs it in the color space the game renders in,
            // otherwise in linear color space it comes out much lighter (7A7A7A looked like FFFFFF).
            material.SetColor("_FogColor", QualitySettings.activeColorSpace == ColorSpace.Linear ? color.linear : color);
            material.SetFloat("_FogDensity", density);
            material.SetFloat("_FogStart", startDistance);
            material.SetFloat("_FogEnd", endDistance);
            Graphics.Blit(src, dest, material);
        }
    }
}
