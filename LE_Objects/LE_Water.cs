using FS_LevelEditor.Editor.UI;
using FS_LevelEditor.Playmode;
using UnityEngine;
using UnityEngine.Events;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System;
using LuxWater;
using FS_LevelEditor.SaveSystem.SerializableTypes;

namespace FS_LevelEditor
{
    // ============================================================================
    // WATER REWRITE
    //
    // Dynamically tracks the moving boundaries of the water volume to support
    // Waypoint movement, rotation, and strict area-based camera effects.
    //
    // PROPERTIES: confirmed against the actual shader sources.
    //   - "PlaneColor" -> _Color on the WaterSurface material (the base tint;
    //     _MainTex is a Texture property, not a Color, so it can't be SetColor'd).
    //   - "WaterColor" -> _WaterColor on the WaterSurface material. Confirmed used
    //     in surf(): c = tex2D(_MainTex,...) * _Color * _WaterColor.
    //   - "DepthColor" -> _DepthColor on the WaterSurface material. The property
    //     EXISTS on the shader but surf() never reads it yet -- it's a declared,
    //     inert slot. Setting it will compile/run fine but do nothing visually
    //     until the shader's surf() is edited to actually blend it in (see the
    //     fresnel snippet from chat -- not applied here since it's a .shader edit,
    //     not a .cs one).
    //   - "FogColor" -> NOT the WaterSurface shader's own (also inert) _FogColor.
    //     It targets the separate "Hidden/Underwater" post-process shader's
    //     _FogColor, which IS read in frag() and genuinely blended into the
    //     final pixel color. That material is shared by the whole level (one
    //     camera, one post effect), so it gets pushed dynamically by
    //     WaterVolumeManager onto whichever water volume currently governs the
    //     effect for the player, rather than being set once per water block.
    //   - "UnderwaterEffect" -> gates whether THIS block's FogColor/effect is
    //     allowed to trigger at all, independent of whether the player is
    //     physically swimming (swim state / oxygen / jetpack are never gated by
    //     this -- it's purely visual).
    // ============================================================================

    public class LE_Water : LE_Object
    {
        private GameObject envCam;
        private GameObject waterRoot;
        private Vector3 cachedLocalSurfacePoint;

        // Per-block instance of the water material. MUST be a unique instance
        // (not the shared asset from AssetBundleLoader) or setting WaterColor on
        // one block would repaint every water block in the level that uses the
        // same source asset.
        private Material waterMaterialInstance;

        private const string PROP_RESPECT_ROTATION = "RespectRotation";

        public override string[] EventsIDs =>
        [
            "OnEnter",
            "OnExit"
        ];

        void Awake()
        {
            properties = new Dictionary<string, object>
            {
                { "UnderwaterEffect", true },
                { "PlaneColor", new Color(0.1804f, 0.1804f, 0.2196f, 1f) },
                { "WaterColor", new Color(0.0980f, 0.4000f, 0.6000f, 1f) },
                { "DepthColor", new Color(0.0000f, 0.0980f, 0.2000f, 1f) },
                { "FogColor", new Color(0.2000f, 0.4000f, 0.5020f, 1f) },
                { "OnEnter", new List<LE_Event>() },
                { "OnExit", new List<LE_Event>() },
                { PROP_RESPECT_ROTATION, false }
            };
        }

        // Kept in sync with Awake() on purpose -- anything that builds a fresh
        // object from this (loading from save, duplicating in the editor) needs
        // the same keys, or GetProperty<Color>("WaterColor") etc. will throw.
        public static Dictionary<string, object> GetDefaultProperties()
        {
            return new Dictionary<string, object>
            {
                { "UnderwaterEffect", true },
                { "PlaneColor", new Color(0.1804f, 0.1804f, 0.2196f, 1f) },
                { "WaterColor", new Color(0.0980f, 0.4000f, 0.6000f, 1f) },
                { "DepthColor", new Color(0.0000f, 0.0980f, 0.2000f, 1f) },
                { "FogColor", new Color(0.2000f, 0.4000f, 0.5020f, 1f) },
                { "OnEnter", new List<LE_Event>() },
                { "OnExit", new List<LE_Event>() },
                { PROP_RESPECT_ROTATION, false },
            };
        }

        public override void OnInstantiated(LEScene scene)
        {
            if (scene == LEScene.Playmode)
            {
                gameObject.GetChildAt("Content/Mesh").SetActive(false);
            }

            base.OnInstantiated(scene);
        }

        private bool IsRespectRotationEnabled()
        {
            return properties != null
                && properties.TryGetValue(PROP_RESPECT_ROTATION, out var value)
                && value is bool enabled
                && enabled;
        }

        public override void InitComponent()
        {
            GameObject water = gameObject.GetChildAt("Content");
            waterRoot = water;
            water.tag = "Water";
            water.layer = LayerMask.NameToLayer("Water");
            water.SetActive(false);

            GameObject triggerMesh = water.GetChild("Mesh");
            Mesh sharedMesh = triggerMesh.GetComponent<MeshFilter>().sharedMesh;
            Bounds meshLocalBounds = sharedMesh.bounds;
            Vector3 extents = meshLocalBounds.extents;

            Bounds localBoundsInWater = new Bounds(
                water.transform.InverseTransformPoint(triggerMesh.transform.TransformPoint(meshLocalBounds.center)),
                Vector3.zero);
            Bounds worldBounds = new Bounds(
                triggerMesh.transform.TransformPoint(meshLocalBounds.center),
                Vector3.zero);

            for (int sx = -1; sx <= 1; sx += 2)
                for (int sy = -1; sy <= 1; sy += 2)
                    for (int sz = -1; sz <= 1; sz += 2)
                    {
                        Vector3 localCorner = meshLocalBounds.center + Vector3.Scale(extents, new Vector3(sx, sy, sz));
                        Vector3 worldCorner = triggerMesh.transform.TransformPoint(localCorner);
                        worldBounds.Encapsulate(worldCorner);
                        localBoundsInWater.Encapsulate(water.transform.InverseTransformPoint(worldCorner));
                    }

            bool respectRotation = IsRespectRotationEnabled();
            GameObject waterVisuals = GameObject.CreatePrimitive(PrimitiveType.Quad);
            waterVisuals.name = "WaterVisuals";
            DestroyImmediate(waterVisuals.GetComponent<Collider>());

            if (respectRotation)
            {
                waterVisuals.transform.SetParent(water.transform, false);
                waterVisuals.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                waterVisuals.transform.localPosition = localBoundsInWater.center + Vector3.up * localBoundsInWater.extents.y;
                waterVisuals.transform.localScale = new Vector3(localBoundsInWater.size.x, localBoundsInWater.size.z, 1f);

                cachedLocalSurfacePoint = localBoundsInWater.center + Vector3.up * localBoundsInWater.extents.y;
            }
            else
            {
                waterVisuals.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                waterVisuals.transform.position = new Vector3(worldBounds.center.x, worldBounds.max.y, worldBounds.center.z);
                waterVisuals.transform.localScale = new Vector3(worldBounds.size.x, worldBounds.size.z, 1f);
                waterVisuals.transform.SetParent(water.transform, true);

                cachedLocalSurfacePoint = water.transform.InverseTransformPoint(
                    new Vector3(worldBounds.center.x, worldBounds.max.y, worldBounds.center.z));
            }

            var waterMeshRenderer = waterVisuals.GetComponent<MeshRenderer>();
            Material waterMat = AssetBundleLoader.LoadAsset<Material>("Water_Cyan_IntroFlood", "level_editor");
            if (waterMat != null)
            {
                // Per-instance copy -- without this, WaterColor/DepthColor/PlaneColor
                // on one block would repaint every block sharing the source asset.
                waterMat = new Material(waterMat);
                waterMaterialInstance = waterMat;

                waterMat.shader = Shader.Find("Lux Water/WaterSurface");
                waterMeshRenderer.sharedMaterial = waterMat;

                waterMat.SetVector("_FinalBumpSpeed01", new Vector4(.2f, .2f, .2f, .2f));
                waterMat.SetVector("_FinalBumpSpeed23", new Vector4(.2f, .2f, .2f, .2f));

                var planarReflection = waterVisuals.AddComponent<LuxWater.LuxWater_PlanarReflection>();
                planarReflection.WaterMaterials = new Material[] { waterMat };
                planarReflection.reflectionMask = 32771;
                planarReflection.farClipPlane = 300f;
                planarReflection.renderShadows = false;

                ApplyWaterMaterialColors();
            }
            else
            {
                Debug.LogError("[LE_Water] Failed to load Water_Cyan_IntroFlood material from asset bundle!");
            }

            var existingCollider = water.GetComponent<BoxCollider>();
            if (existingCollider != null) DestroyImmediate(existingCollider);
            var collider = water.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.center = localBoundsInWater.center;
            collider.size = localBoundsInWater.size;

            WaterPatch waterPatch = water.AddComponent<WaterPatch>();
            waterPatch.parentWater = this;

            SetupUnderwaterCamera();
            water.SetActive(true);

            initialized = true;
        }

        // Only touches the WaterSurface material's own inputs (PlaneColor,
        // WaterColor, DepthColor). FogColor is handled separately by
        // WaterVolumeManager since it targets the shared underwater post material,
        // not this per-block instance.
        private void ApplyWaterMaterialColors()
        {
            if (waterMaterialInstance == null) return;

            if (properties.TryGetValue("PlaneColor", out var pc) && pc is Color planeColor)
                waterMaterialInstance.SetColor("_Color", planeColor); // base tint -- _MainTex is a Texture, not a Color

            if (properties.TryGetValue("WaterColor", out var wc) && wc is Color waterColor)
                waterMaterialInstance.SetColor("_WaterColor", waterColor); // confirmed used in surf()

            if (properties.TryGetValue("DepthColor", out var dc) && dc is Color depthColor)
                waterMaterialInstance.SetColor("_DepthColor", depthColor); // confirmed name, but inert until surf() is edited to read it
        }

        public Color GetFogColor() =>
            properties.TryGetValue("FogColor", out var v) && v is Color c ? c : new Color(0.2f, 0.4f, 0.502f, 1f);
        public bool GetUnderwaterEffectEnabled() =>
            properties.TryGetValue("UnderwaterEffect", out var v) && v is bool b && b;

        public override bool SetProperty(string name, object value)
        {
            if (GetAvailableEventsIDs().Contains(name))
            {
                if (value is List<LE_Event>)
                {
                    properties[name] = (List<LE_Event>)value;
                }
            }
            else if (name == PROP_RESPECT_ROTATION && value is bool)
            {
                properties[name] = value;
            }
            else if (name == "UnderwaterEffect" && value is bool)
            {
                properties["UnderwaterEffect"] = value;
                return true;
            }
            else if ((name == "PlaneColor" || name == "WaterColor" || name == "DepthColor" || name == "FogColor")
                && (value is Color || value is string))
            {
                // The type check above is load-bearing: on level load, the base
                // LE_Object.SetProperty hands raw JToken values in first to convert
                // them, then re-calls this override with the real Color. Matching on
                // name alone (as before) caught that JToken here too, failed both
                // casts below, and returned false -- silently discarding the loaded
                // value instead of letting it fall through to base.SetProperty, which
                // is why colors reset to their Awake() defaults on every load.
                Color? color = value is Color c ? c : Utils.HexToColor((string)value, false, null);
                if (color == null) return false;
                properties[name] = color.Value;

                // FogColor doesn't live on this block's own material (see header
                // comment) -- no per-block material push needed for it, the
                // WaterVolumeManager reads GetFogColor() live each time it decides
                // this volume governs the effect.
                if (name != "FogColor") ApplyWaterMaterialColors();

                return true;
            }
            return base.SetProperty(name, value);
        }

        public void ExecuteOnEnterEvents() => eventExecuter.ExecuteEvents((List<LE_Event>)properties["OnEnter"]);
        public void ExecuteOnExitEvents() => eventExecuter.ExecuteEvents((List<LE_Event>)properties["OnExit"]);
        public static new Color GetDefaultObjectColor(LEObjectContext context) => new Color(1f, 1f, 0.07843138f);

        public Vector3 GetSurfaceUp() => IsRespectRotationEnabled() ? waterRoot.transform.up : Vector3.up;
        public Vector3 GetSurfaceWorldPoint() => waterRoot.transform.TransformPoint(cachedLocalSurfacePoint);

        [Obsolete("Kept for backward compatibility -- use GetSurfaceWorldPoint()/GetSurfaceUp() instead.")]
        public float GetMeshTopY() => GetSurfaceWorldPoint().y;

        private void SetupUnderwaterCamera()
        {
            NativeModLoader.Instance.StartCoroutine(SetupEnvCam());
        }

        IEnumerator SetupEnvCam()
        {
            while (envCam == null)
            {
                envCam = GameObject.Find("EnvCam");
                yield return null;
            }

            // Verify if EnvCam already has the post effect component in this scene session
            if (envCam.GetComponent<UnderwaterPostEffect>() == null)
            {
                Camera cam = envCam.GetComponent<Camera>();
                if (cam != null) cam.depthTextureMode = DepthTextureMode.Depth;

                Shader underwaterShader = AssetBundleLoader.LoadAsset<Shader>("Underwater", "level_editor");
                if (underwaterShader != null)
                {
                    Material underwaterMat = new Material(underwaterShader);
                    underwaterMat.SetColor("_TintColor", new Color(0.5f, 0.8f, 1f, 1f));
                    underwaterMat.SetColor("_FogColor", new Color(0, 0.344f, 0.344f, 1f)); // default until a volume overrides it live
                    underwaterMat.SetFloat("_DistortionIntensity", 0.001f);
                    underwaterMat.SetFloat("_FogDensity", 5.0f);
                    underwaterMat.SetFloat("_FogStart", 5f);
                    underwaterMat.SetFloat("_FogPower", 1.0f);
                    underwaterMat.SetFloat("_TransitionWidth", 2f);
                    underwaterMat.SetFloat("_MaxFogIntensity", 0.95f);
                    underwaterMat.SetFloat("_TransitionSmoothness", 2.0f);
                    underwaterMat.SetFloat("_LineDistortion", .05f);

                    UnderwaterPostEffect postEffect = envCam.AddComponent<UnderwaterPostEffect>();
                    postEffect.postMaterial = underwaterMat;
                }
            }
        }

        // ========================================================================
        // WATERPATCH SYSTEM
        // ========================================================================

        public class WaterPatch : MonoBehaviour
        {
            public LE_Water parentWater;
            public BoxCollider volumeCollider;

            private void Awake()
            {
                volumeCollider = GetComponent<BoxCollider>();
            }

            private void Update()
            {
                WaterVolumeManager.ProcessActiveVolumes();
            }

            private void OnTriggerEnter(Collider other)
            {
                if (other.CompareTag("Player"))
                {
                    WaterVolumeManager.RegisterVolume(this);
                }
                else
                {
                    var rb = other.GetComponent<Rigidbody>();
                    if (rb != null && !rb.isKinematic)
                    {
                        var floater = other.GetComponent<CubeFloatSystem>();
                        if (floater == null) floater = other.gameObject.AddComponent<CubeFloatSystem>();

                        floater.enabled = true;
                        floater.AssignWaterVolume(this);
                    }
                }
            }

            private void OnTriggerExit(Collider other)
            {
                if (other.CompareTag("Player"))
                {
                    WaterVolumeManager.UnregisterVolume(this);
                }
                else
                {
                    var floater = other.GetComponent<CubeFloatSystem>();
                    if (floater != null) floater.RemoveWaterVolume(this);
                }
            }

            public Vector3 GetSurfaceUp() => parentWater != null ? parentWater.GetSurfaceUp() : Vector3.up;
            public Vector3 GetSurfaceWorldPoint() => parentWater != null ? parentWater.GetSurfaceWorldPoint() : transform.position;

            public void FireEnterEvents() => parentWater?.ExecuteOnEnterEvents();
            public void FireExitEvents() => parentWater?.ExecuteOnExitEvents();
        }

        // ========================================================================
        // WATER VOLUME MANAGER
        // ========================================================================

        public static class WaterVolumeManager
        {
            private static readonly HashSet<WaterPatch> activeVolumes = new HashSet<WaterPatch>();
            private static bool isSwimming = false;
            private static bool jetpackWasEnabled = false;
            private static float lastStateChangeTime = -999f;
            private static int lastProcessedFrame = -1;

            private const float ENTER_MARGIN = 0.2f;
            private const float EXIT_MARGIN = 0.4f;
            private const float STATE_CHANGE_COOLDOWN = 0.15f;

            public static bool isManagedStateChange = false;
            private static System.Reflection.FieldInfo cachedWaterField = null;
            private static System.Reflection.PropertyInfo cachedWaterProp = null;
            private static float lastSetWaterLevel = -9999f;
            private static bool reflectionFailed = false;

            public static void RegisterVolume(WaterPatch patch)
            {
                if (activeVolumes.Add(patch)) patch.FireEnterEvents();
            }

            public static void UnregisterVolume(WaterPatch patch)
            {
                if (activeVolumes.Remove(patch)) patch.FireExitEvents();
                if (activeVolumes.Count == 0 && isSwimming) ExitSwimState();
            }

            public static void ProcessActiveVolumes()
            {
                if (Time.frameCount == lastProcessedFrame || activeVolumes.Count == 0) return;
                lastProcessedFrame = Time.frameCount;

                Transform playerTransform = Controls.Instance?.transform;
                if (playerTransform != null) UpdatePlayerSwimAndEffectState(playerTransform.position);
            }

            private static void UpdatePlayerSwimAndEffectState(Vector3 playerPos)
            {
                bool shouldBeSwimming = false;
                float highestWorldY = float.NegativeInfinity;
                WaterPatch highestPatch = null;

                foreach (var patch in activeVolumes)
                {
                    if (patch == null) continue;

                    Vector3 surfacePoint = patch.GetSurfaceWorldPoint();
                    Vector3 surfaceUp = patch.GetSurfaceUp();

                    float surfaceProj = Vector3.Dot(surfacePoint, surfaceUp);
                    float playerProj = Vector3.Dot(playerPos, surfaceUp);
                    float margin = isSwimming ? EXIT_MARGIN : ENTER_MARGIN;

                    if (playerProj <= surfaceProj + margin)
                    {
                        shouldBeSwimming = true;
                        if (surfacePoint.y > highestWorldY)
                        {
                            highestWorldY = surfacePoint.y;
                            highestPatch = patch;
                        }
                    }
                }

                var postEffect = GetPostEffect();
                if (postEffect != null)
                {
                    // UnderwaterEffect is purely visual -- it never gates swim
                    // state/oxygen/jetpack below, only whether the screen effect
                    // itself is allowed to show for this specific volume.
                    bool effectAllowed = highestPatch != null
                        && highestPatch.parentWater != null
                        && highestPatch.parentWater.GetUnderwaterEffectEnabled();

                    if (effectAllowed && postEffect.postMaterial != null)
                    {
                        postEffect.postMaterial.SetColor("_FogColor", highestPatch.parentWater.GetFogColor());
                    }

                    postEffect.SetWaterState(shouldBeSwimming && effectAllowed, highestWorldY);
                }

                if (Time.time - lastStateChangeTime < STATE_CHANGE_COOLDOWN)
                {
                    if (isSwimming) SyncOngoingSwimState(highestWorldY);
                    return;
                }

                if (shouldBeSwimming && !isSwimming)
                {
                    EnterSwimState(highestWorldY);
                }
                else if (!shouldBeSwimming && isSwimming)
                {
                    ExitSwimState();
                }
                else if (isSwimming)
                {
                    SyncOngoingSwimState(highestWorldY);
                }
            }

            private static void SyncOngoingSwimState(float dynamicSurfaceY)
            {
                if (Mathf.Abs(dynamicSurfaceY - lastSetWaterLevel) > 0.001f)
                {
                    UpdateNativeWaterLevel(dynamicSurfaceY);
                }
            }

            private static void UpdateNativeWaterLevel(float newLevel)
            {
                var controls = Controls.Instance;
                if (controls == null) return;

                if (cachedWaterField != null)
                {
                    cachedWaterField.SetValue(controls, newLevel);
                    lastSetWaterLevel = newLevel;
                    return;
                }
                if (cachedWaterProp != null)
                {
                    cachedWaterProp.SetValue(controls, newLevel, null);
                    lastSetWaterLevel = newLevel;
                    return;
                }
                if (reflectionFailed)
                {
                    isManagedStateChange = true;
                    controls.OnWaterEnter(newLevel);
                    isManagedStateChange = false;
                    lastSetWaterLevel = newLevel;
                    return;
                }

                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
                bool found = false;
                System.Reflection.FieldInfo bestFieldMatch = null;

                foreach (var field in typeof(Controls).GetFields(flags))
                {
                    if (field.FieldType == typeof(float))
                    {
                        float val = (float)field.GetValue(controls);
                        if (Mathf.Abs(val - lastSetWaterLevel) < 0.001f)
                        {
                            string name = field.Name.ToLower();
                            if (name.Contains("water") || name.Contains("level") || name.Contains("surf"))
                            {
                                bestFieldMatch = field;
                                break;
                            }
                            if (bestFieldMatch == null) bestFieldMatch = field;
                        }
                    }
                }

                if (bestFieldMatch != null)
                {
                    cachedWaterField = bestFieldMatch;
                    cachedWaterField.SetValue(controls, newLevel);
                    lastSetWaterLevel = newLevel;
                    found = true;
                }

                if (!found)
                {
                    System.Reflection.PropertyInfo bestPropMatch = null;
                    foreach (var prop in typeof(Controls).GetProperties(flags))
                    {
                        if (prop.PropertyType == typeof(float) && prop.CanWrite)
                        {
                            float val = (float)prop.GetValue(controls, null);
                            if (Mathf.Abs(val - lastSetWaterLevel) < 0.001f)
                            {
                                string name = prop.Name.ToLower();
                                if (name.Contains("water") || name.Contains("level") || name.Contains("surf"))
                                {
                                    bestPropMatch = prop;
                                    break;
                                }
                                if (bestPropMatch == null) bestPropMatch = prop;
                            }
                        }
                    }

                    if (bestPropMatch != null)
                    {
                        cachedWaterProp = bestPropMatch;
                        cachedWaterProp.SetValue(controls, newLevel, null);
                        lastSetWaterLevel = newLevel;
                        found = true;
                    }
                }

                if (!found)
                {
                    Debug.LogWarning("[FS_LevelEditor_Water] Could not dynamically map native water height. Defaulting to standard call.");
                    reflectionFailed = true;
                    isManagedStateChange = true;
                    controls.OnWaterEnter(newLevel);
                    isManagedStateChange = false;
                    lastSetWaterLevel = newLevel;
                }
            }

            private static void EnterSwimState(float dynamicSurfaceY)
            {
                isSwimming = true;
                lastStateChangeTime = Time.time;

                jetpackWasEnabled = Controls.Instance.hasJetPack;
                Controls.Instance.hasJetPack = false;

                lastSetWaterLevel = dynamicSurfaceY;

                isManagedStateChange = true;
                Controls.Instance.OnWaterEnter(dynamicSurfaceY);
                isManagedStateChange = false;
            }

            private static void ExitSwimState()
            {
                isSwimming = false;
                lastStateChangeTime = Time.time;

                isManagedStateChange = true;
                Controls.Instance.OnWaterExit(false, false);
                isManagedStateChange = false;

                Controls.Instance.SetFlashlightAllowed();

                if (jetpackWasEnabled) Controls.Instance.hasJetPack = true;
                jetpackWasEnabled = false;
            }

            private static UnderwaterPostEffect cachedPostEffect;
            private static UnderwaterPostEffect GetPostEffect()
            {
                if (cachedPostEffect == null)
                {
                    GameObject envCamObj = GameObject.Find("EnvCam");
                    if (envCamObj != null) cachedPostEffect = envCamObj.GetComponent<UnderwaterPostEffect>();
                }
                return cachedPostEffect;
            }
        }

        // ========================================================================
        // QUARANTINE PATCHES
        // ========================================================================

        [HarmonyPatch(typeof(Controls), nameof(Controls.OnWaterEnter), new Type[] { typeof(float) })]
        public static class PreventNativeWaterEnter_Float
        {
            public static bool Prefix()
            {
                if (PlayModeController.Instance && !WaterVolumeManager.isManagedStateChange) return false;
                return true;
            }
        }

        [HarmonyPatch(typeof(Controls), nameof(Controls.OnWaterEnter), new Type[] { typeof(float), typeof(bool), typeof(bool) })]
        public static class PreventNativeWaterEnter_FloatBoolBool
        {
            public static bool Prefix()
            {
                if (PlayModeController.Instance && !WaterVolumeManager.isManagedStateChange) return false;
                return true;
            }
        }

        [HarmonyPatch(typeof(Controls), nameof(Controls.OnWaterExit), new Type[] { typeof(bool), typeof(bool) })]
        public static class PreventNativeWaterExit
        {
            public static bool Prefix()
            {
                if (PlayModeController.Instance && !WaterVolumeManager.isManagedStateChange) return false;
                return true;
            }
        }

        [HarmonyPatch(typeof(GunController), nameof(GunController.RequestExamineTaser), new Type[] { typeof(bool) })]
        public static class FixExamine
        {
            public static bool Prefix()
            {
                if (PlayModeController.Instance && Controls.IsSwimming())
                {
                    InGameUIManager.Instance.ShowNotification(InGameUIManager.NotificationType.TASER_WATER_UNUSABLE, InGameUIManager.NotificationColor.Red, 0f, 4f, true, true);
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Controls), nameof(Controls.StartZeroGBoost))]
        public static class FixBoost
        {
            public static bool Prefix()
            {
                if (PlayModeController.Instance && Controls.IsSwimming())
                {
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(BlocScript), nameof(BlocScript.SetFloatingState))]
        public static class CustomFloatingState
        {
            public static void Postfix(BlocScript __instance)
            {
                var rb = __instance.GetComponent<Rigidbody>();
                if (rb == null) return;

                var floater = __instance.gameObject.GetComponent<CubeFloatSystem>();
                if (floater == null)
                {
                    floater = __instance.gameObject.AddComponent<CubeFloatSystem>();
                }
                floater.enabled = true;
            }
        }

        // ========================================================================
        // CUBE FLOAT SYSTEM
        // ========================================================================

        public class CubeFloatSystem : MonoBehaviour
        {
            public float floatForce = 12f;
            public float waterDrag = 4f;
            public float waterAngularDrag = 0.95f;
            public float detectionOffset = 0.5f;

            public HashSet<WaterPatch> activeVolumes = new HashSet<WaterPatch>();
            private Rigidbody rb;
            private float originalDrag;
            private float originalAngularDrag;
            private bool isInWater = false;

            void Start()
            {
                rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    originalDrag = rb.linearDamping;
                    originalAngularDrag = rb.angularDamping;
                }
            }

            public void AssignWaterVolume(WaterPatch patch) => activeVolumes.Add(patch);

            public void RemoveWaterVolume(WaterPatch patch)
            {
                activeVolumes.Remove(patch);
                if (activeVolumes.Count == 0 && isInWater && rb != null)
                {
                    isInWater = false;
                    rb.linearDamping = originalDrag;
                    rb.angularDamping = originalAngularDrag;
                }
            }

            void FixedUpdate()
            {
                if (rb == null || activeVolumes.Count == 0) return;

                bool anySubmerged = false;
                float maxSubmersion = 0f;

                foreach (var patch in activeVolumes)
                {
                    if (patch == null) continue;

                    Vector3 surfaceUp = patch.GetSurfaceUp();
                    Vector3 surfacePoint = patch.GetSurfaceWorldPoint();

                    float surfaceProj = Vector3.Dot(surfacePoint, surfaceUp);
                    float cubeProj = Vector3.Dot(transform.position, surfaceUp);

                    float radius = transform.localScale.y * 0.5f;
                    float cubeBottom = cubeProj - radius;

                    if (cubeBottom < surfaceProj + detectionOffset)
                    {
                        anySubmerged = true;
                        float depth = surfaceProj - cubeBottom;
                        float ratio = Mathf.Clamp01(depth / (radius * 2));
                        if (ratio > maxSubmersion) maxSubmersion = ratio;
                    }
                }

                if (anySubmerged)
                {
                    if (!isInWater)
                    {
                        isInWater = true;
                        rb.linearDamping = waterDrag;
                        rb.angularDamping = waterAngularDrag;
                    }

                    Vector3 buoyancyForce = Vector3.up * floatForce * maxSubmersion * rb.mass;
                    Vector3 gravityCounter = -Physics.gravity * rb.mass * maxSubmersion * 0.8f;

                    rb.AddForce(buoyancyForce + gravityCounter, ForceMode.Force);
                }
                else if (isInWater)
                {
                    isInWater = false;
                    rb.linearDamping = originalDrag;
                    rb.angularDamping = originalAngularDrag;
                }
            }
        }

        [HarmonyPatch(typeof(InGameUIManager), nameof(InGameUIManager.ShowOxygenGauge))]
        public static class GaugeFix
        {
            public static void Postfix(InGameUIManager __instance, bool _state)
            {
                if (__instance != null && __instance.oxygenGaugeObj != null)
                {
                    if (_state)
                    {
                        __instance.oxygenGaugeObj.SetActive(true);

                        Transform bg = __instance.oxygenGaugeObj.transform.Find("Holder/Background");
                        if (bg != null) bg.localPosition = new Vector3(323, -20, 0);

                        Transform icon = __instance.oxygenGaugeObj.transform.Find("Holder/O2Icon");
                        if (icon != null) icon.localPosition = new Vector3(315.2346f, -34.1f, 0);
                    }
                    else
                    {
                        __instance.oxygenGaugeObj.SetActive(false);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(InGameUIManager), nameof(InGameUIManager.ShowNotification))]
        public static class FlashlightFix
        {
            public static bool Prefix(InGameUIManager __instance, InGameUIManager.NotificationType _type)
            {
                if (_type == InGameUIManager.NotificationType.FlashlightOperational && PlayModeController.Instance)
                {
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Controls), nameof(Controls.ProcessOxygen))]
        public static class ProcessOxygenFPSFix
        {
            public static bool Prefix(Controls __instance, ref float ___timeSinceLastOxygenReduction, ref float ___lastOxyDmgTime)
            {
                if (PlayModeController.Instance)
                {
                    if (!__instance.GetPause() && (Controls.gameInFocus) && Controls.QuickloadFinished && !__instance.GetInMiniGame() && !__instance.inKinematic && !__instance.GetCameraTransitionState())
                    {
                        bool flag = true;
                        if (__instance.isSwimming && !__instance.isAtWaterSurface)
                        {
                            if (!__instance.debug)
                            {
                                __instance.remainingOxygen -= Time.unscaledDeltaTime * __instance.oxygenConsumptionMultiplier;
                            }
                            flag = false;
                            ___timeSinceLastOxygenReduction = 0f;

                            if (__instance.remainingOxygen <= 0f)
                            {
                                __instance.remainingOxygen = 0f;
                                if (___lastOxyDmgTime >= 1f)
                                {
                                    __instance.DamageCharacter(Mathf.CeilToInt(__instance.noOxyDamage));
                                    ___lastOxyDmgTime = 0f;
                                }
                                else
                                {
                                    ___lastOxyDmgTime += Time.unscaledDeltaTime;
                                }
                            }
                        }
                        if (flag && __instance.remainingOxygen < __instance.maxOxygenTime)
                        {
                            ___timeSinceLastOxygenReduction += Time.unscaledDeltaTime;
                            if (___timeSinceLastOxygenReduction >= __instance.oxygenRechargeCooldown)
                            {
                                __instance.remainingOxygen += Time.unscaledDeltaTime * __instance.oxygenRechargeMultiplier;
                            }
                        }
                        if (__instance.remainingOxygen >= __instance.maxOxygenTime)
                        {
                            __instance.remainingOxygen = __instance.maxOxygenTime;
                            ___timeSinceLastOxygenReduction = 0f;
                        }
                    }
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Controls), nameof(Controls.SetUnderwaterEffects))]
        public static class PreventUnderwaterEffectsTeardownCrash
        {
            public static Exception Finalizer(Exception __exception)
            {
                if (__exception is NullReferenceException)
                {
                    return null;
                }
                return __exception;
            }
        }

        // ========================================================================
        // UNDERWATER POST EFFECT
        // ========================================================================

        public class UnderwaterPostEffect : MonoBehaviour
        {
            public Material postMaterial;
            public float surfaceYLevel;
            private bool isCameraInWaterVolume = false;

            public float depthScale = 1.0f;
            public float depthExponent = 0.7f;

            private void Awake()
            {
                enabled = false;
            }

            public void SetWaterState(bool inWater, float liveSurfaceLevel)
            {
                surfaceYLevel = liveSurfaceLevel;
                isCameraInWaterVolume = inWater;
                enabled = inWater;
            }

            private void OnRenderImage(RenderTexture src, RenderTexture dest)
            {
                if (postMaterial != null && isCameraInWaterVolume)
                {
                    float activeCamY = Camera.main != null ? Camera.main.transform.position.y : transform.position.y;
                    float rawDepth = (surfaceYLevel - activeCamY) / depthScale;

                    float depthFactor = Mathf.Clamp01(rawDepth);
                    depthFactor = Mathf.Pow(depthFactor, depthExponent);

                    if (depthFactor > 0.01f)
                    {
                        postMaterial.SetFloat("_DepthFactor", depthFactor);
                        postMaterial.SetFloat("_WorldDepth", rawDepth); // still inert -- frag() never reads it, see chat
                        Graphics.Blit(src, dest, postMaterial);
                        return;
                    }
                }
                Graphics.Blit(src, dest);
            }
        }
    }
}