using FS_LevelEditor.Editor.UI;
using FS_LevelEditor.Playmode;
using UnityEngine;
using UnityEngine.Events;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System;
using LuxWater;

namespace FS_LevelEditor
{
    public class LE_Water : LE_Object
    {
        private GameObject envCam;

        public override string[] EventsIDs =>
        [
            "OnEnter",
            "OnExit"
        ];

        void Awake()
        {
            properties = new Dictionary<string, object>
            {
                { "OnEnter", new List<LE_Event>() },
                { "OnExit", new List<LE_Event>() }
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

        public override void InitComponent()
        {
            GameObject water = gameObject.GetChildAt("Content");
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

            // --- Visual quad: plain world-space values, always flat/level regardless
            // of how the trigger box itself is rotated. Reparent with worldPositionStays
            // so Unity (not us) derives the correct local scale under a rotated parent.
            GameObject waterVisuals = GameObject.CreatePrimitive(PrimitiveType.Quad);
            waterVisuals.name = "WaterVisuals";
            DestroyImmediate(waterVisuals.GetComponent<Collider>());

            waterVisuals.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            waterVisuals.transform.position = new Vector3(worldBounds.center.x, worldBounds.max.y, worldBounds.center.z);
            waterVisuals.transform.localScale = new Vector3(worldBounds.size.x, worldBounds.size.z, 1f);
            waterVisuals.transform.SetParent(water.transform, true); // worldPositionStays = true

            var waterMeshRenderer = waterVisuals.GetComponent<MeshRenderer>();
            Material waterMat = AssetBundleLoader.LoadAsset<Material>("Water_Cyan_IntroFlood", "level_editor");
            if (waterMat != null)
            {
                waterMat.shader = Shader.Find("Lux Water/WaterSurface");
                waterMeshRenderer.material = waterMat;
                LuxWater_PlanarReflection planarReflection = waterVisuals.AddComponent<LuxWater_PlanarReflection>();
                planarReflection.WaterMaterials = new Material[] { waterMat };
            }
            else
            {
                Debug.LogError("[LE_Water] Failed to load Water_Cyan_IntroFlood material from asset bundle!");
            }

            // --- Trigger collider: local space is fine here, a BoxCollider is already
            // rotation-aware, no "which axis is up" ambiguity.
            var existingCollider = water.GetComponent<BoxCollider>();
            if (existingCollider != null) DestroyImmediate(existingCollider);
            var collider = water.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.center = localBoundsInWater.center;
            collider.size = localBoundsInWater.size;

            // --- Surface Y: plain world-space top of the box. No local-axis guessing.
            WaterPatch waterPatch = water.AddComponent<WaterPatch>();
            float surfaceY = worldBounds.max.y;
            waterPatch.waterSurfaceY = surfaceY;

            SetupUnderwaterCamera(surfaceY);
            water.SetActive(true);

            initialized = true;
        }

        public override bool SetProperty(string name, object value)
        {
            if (GetAvailableEventsIDs().Contains(name))
            {
                if (value is List<LE_Event>)
                {
                    properties[name] = (List<LE_Event>)value;
                }
            }

            return base.SetProperty(name, value);
        }

        void ExecuteOnEnterEvents()
        {
            eventExecuter.ExecuteEvents((List<LE_Event>)properties["OnEnter"]);
        }

        void ExecuteOnExitEvents()
        {
            eventExecuter.ExecuteEvents((List<LE_Event>)properties["OnExit"]);
        }

        public static new Color GetDefaultObjectColor(LEObjectContext context)
        {
            return new Color(1f, 1f, 0.07843138f);
        }

        private bool _cameraSetupDone = false;

        private void SetupUnderwaterCamera(float localizedSurfaceY)
        {
            if (_cameraSetupDone) return;

            Debug.Log("[UnderwaterSetup] Starting camera setup...");

            NativeModLoader.Instance.StartCoroutine(SetupEnvCam(localizedSurfaceY));
        }

        IEnumerator SetupEnvCam(float localizedSurfaceY)
        {
            while (envCam == null)
            {
                envCam = GameObject.Find("EnvCam");
                yield return null;
            }
            Camera cam = envCam.GetComponent<Camera>();

            Debug.Log($"[UnderwaterSetup] Camera found: {cam.name}");

            // Check render pipeline
            var renderPipeline = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;

            // Enable depth texture
            cam.depthTextureMode = DepthTextureMode.Depth;
            Debug.Log("[UnderwaterSetup] Depth texture enabled");

            // Load shader
            Shader underwaterShader = AssetBundleLoader.LoadAsset<Shader>("Underwater", "level_editor");

            // Create material
            Material underwaterMat = new Material(underwaterShader);
            underwaterMat.SetColor("_TintColor", new Color(0.5f, 0.8f, 1f, 1f));
            underwaterMat.SetColor("_FogColor", new Color(0, 0.344f, 0.344f, 1f));
            underwaterMat.SetFloat("_DistortionIntensity", 0.001f);
            underwaterMat.SetFloat("_FogDensity", 5.0f);
            underwaterMat.SetFloat("_FogStart", 5f);
            underwaterMat.SetFloat("_FogPower", 1.0f);
            underwaterMat.SetFloat("_TransitionWidth", 2f);
            underwaterMat.SetFloat("_MaxFogIntensity", 0.95f);
            underwaterMat.SetFloat("_TransitionSmoothness", 2.0f);
            underwaterMat.SetFloat("_LineDistortion", .05f);

            Debug.Log("[UnderwaterSetup] Material properties set successfully");

            // Check if component already exists
            UnderwaterPostEffect existingEffect = envCam.GetComponent<UnderwaterPostEffect>();
            if (existingEffect != null)
            {
                Debug.Log("[UnderwaterSetup] Removing existing UnderwaterPostEffect component");
                DestroyImmediate(existingEffect);
            }

            // Add post effect component
            UnderwaterPostEffect postEffect = envCam.AddComponent<UnderwaterPostEffect>();
            postEffect.postMaterial = underwaterMat;

            // Initializing dynamically based on local surface height
            postEffect.SetWaterState(true, localizedSurfaceY);

            // Force enable for testing
            postEffect.enabled = true;
            _cameraSetupDone = true;
        }

        public class WaterPatch : MonoBehaviour
        {
            public float waterSurfaceY;
            public LayerMask groundLayerMask = 1; // What layers count as ground
            private bool playerInWater = false; // Track water state
            private float lastWaterStateChange = 0f; // Cooldown timer
            private const float WATER_STATE_COOLDOWN = 0.5f; // Half second cooldown
            private bool hadJetpack = false;

            private void OnTriggerEnter(Collider other)
            {
                // Give dynamic physics objects buoyancy immediately when entering local water bounds
                if (!other.CompareTag("Player"))
                {
                    var rb = other.GetComponent<Rigidbody>();
                    if (rb != null && !rb.isKinematic)
                    {
                        var floater = other.GetComponent<CubeFloatSystem>();
                        if (floater == null)
                            floater = other.gameObject.AddComponent<CubeFloatSystem>();

                        floater.enabled = true;
                        floater.waterLevel = waterSurfaceY; // Dynamically pass this volume's exact surface height
                    }
                }
            }

            private void OnTriggerStay(Collider other)
            {
                if (other.CompareTag("Player"))
                {
                    if (Time.time - lastWaterStateChange < WATER_STATE_COOLDOWN) return;

                    Vector3 playerPos = other.transform.position;
                    // Use standard bounding to ensure player enters state securely
                    bool shouldBeInWater = playerPos.y <= waterSurfaceY + 0.2f;

                    // Player should enter water
                    if (shouldBeInWater && !playerInWater)
                    {
                        EnterWaterState();
                    }
                    // Player should exit water
                    else if (!shouldBeInWater && playerInWater)
                    {
                        bool canExit = CanPlayerExitWater(Controls.Instance.transform);
                        if (canExit)
                        {
                            ExitWaterState();
                        }
                    }
                }
                else
                {
                    // Ensure props retain dynamic surface height if water surface is moving or modified
                    var floater = other.GetComponent<CubeFloatSystem>();
                    if (floater != null)
                    {
                        floater.waterLevel = waterSurfaceY;
                    }
                }
            }

            private void OnTriggerExit(Collider other)
            {
                if (other.CompareTag("Player"))
                {
                    if (playerInWater)
                    {
                        // Force exit when leaving trigger completely
                        ExitWaterState();
                    }
                }
                else
                {
                    // Prop leaves water volume; cancel local floating behavior
                    var floater = other.GetComponent<CubeFloatSystem>();
                    if (floater != null)
                    {
                        floater.waterLevel = -99999f; // Moves threshold securely out of bounds to restore natural physics gravity
                    }
                }
            }

            private void ExitWaterState()
            {
                if (!playerInWater) return; // Already exited

                // 1. Attempt to remove camera effects, but DO NOT abort if camera isn't found
                GameObject envCamObj = GameObject.Find("EnvCam");
                if (envCamObj != null)
                {
                    UnderwaterPostEffect postEffect = envCamObj.GetComponent<UnderwaterPostEffect>();
                    if (postEffect != null && Mathf.Approximately(postEffect.surfaceYLevel, waterSurfaceY))
                    {
                        postEffect.SetWaterState(false, waterSurfaceY);
                    }
                }

                // 2. ALWAYS execute player mechanical state restore
                Controls.Instance.OnWaterExit(false, false);
                Controls.Instance.SetFlashlightAllowed();
                playerInWater = false;
                lastWaterStateChange = Time.time;

                if (hadJetpack) Controls.Instance.hasJetPack = true;
                hadJetpack = false;
            }

            private void EnterWaterState()
            {
                if (playerInWater) return; // Already in water

                // 1. Attempt to apply visual camera effects, but DO NOT abort if camera isn't found
                GameObject envCamObj = GameObject.Find("EnvCam");
                if (envCamObj != null)
                {
                    UnderwaterPostEffect postEffect = envCamObj.GetComponent<UnderwaterPostEffect>();
                    if (postEffect != null)
                    {
                        postEffect.SetWaterState(true, waterSurfaceY);
                    }
                }

                // 2. ALWAYS execute player mechanical state (this is what allows swimming)
                Controls.Instance.OnWaterEnter(waterSurfaceY);
                Controls.Instance.SetFlashlightNotAllowed();
                playerInWater = true;
                lastWaterStateChange = Time.time;

                if (Controls.Instance.hasJetPack) hadJetpack = true;
                Controls.Instance.hasJetPack = false;
            }

            private bool CanPlayerExitWater(Transform playerTransform)
            {
                Vector3 playerPos = playerTransform.position;

                // Check if player is above water surface
                if (playerPos.y <= waterSurfaceY)
                {
                    return false;
                }

                // Get player controller height
                CharacterController controller = playerTransform.GetComponent<CharacterController>();
                float controllerHeight = 0.82f; // Default fallback

                // Cast downward from player position to check for ground within controller height
                RaycastHit hit;
                if (Physics.Raycast(playerPos, Vector3.down, out hit, controllerHeight, groundLayerMask))
                {
                    return true; // Player can touch ground below
                }

                return false; // No ground within reach
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

                // Add floating component if not already present
                var floater = __instance.gameObject.GetComponent<CubeFloatSystem>();
                if (floater == null)
                {
                    floater = __instance.gameObject.AddComponent<CubeFloatSystem>();
                }
                floater.enabled = true;
                // No waterLevel assignment here anymore! It will be assigned natively by the WaterPatch trigger
            }
        }

        public class CubeFloatSystem : MonoBehaviour
        {
            // Replaced hardcoded value. Start far below bounds so it behaves physically correct if accidentally spawned outside water.
            public float waterLevel = -99999f;
            public float floatForce = 12f;
            public float waterDrag = 4f;
            public float waterAngularDrag = 0.95f;
            public float objectMass = 2.6f;

            public float detectionOffset = 0.5f; // How deep before floating starts

            private Rigidbody rb;
            private float originalDrag;
            private float originalAngularDrag;
            private bool isInWater = false;

            void Start()
            {
                rb = GetComponent<Rigidbody>();
                if (rb == null)
                {
                    Debug.LogError("CubeFloatSystem requires a Rigidbody component!");
                    return;
                }

                // Set object mass
                rb.mass = objectMass;

                // Store original drag values
                originalDrag = rb.linearDamping;
                originalAngularDrag = rb.angularDamping;
            }

            void FixedUpdate()
            {
                if (rb == null) return;

                float cubeBottom = transform.position.y - (transform.localScale.y * 0.5f);
                float cubeTop = transform.position.y + (transform.localScale.y * 0.5f);
                float waterSurface = waterLevel;

                // Check if cube is touching or below dynamically determined local water
                if (cubeBottom < waterSurface + detectionOffset)
                {
                    if (!isInWater)
                    {
                        isInWater = true;
                        rb.linearDamping = waterDrag;
                        rb.angularDamping = waterAngularDrag;
                    }

                    // Calculate how much of the cube is submerged
                    float submersionDepth;
                    if (cubeTop < waterSurface)
                    {
                        // Fully submerged
                        submersionDepth = transform.localScale.y;
                    }
                    else
                    {
                        // Partially submerged
                        submersionDepth = waterSurface - cubeBottom;
                        submersionDepth = Mathf.Clamp(submersionDepth, 0f, transform.localScale.y);
                    }

                    // Calculate buoyancy force based on submersion
                    float submersionRatio = submersionDepth / transform.localScale.y;

                    // Stronger buoyancy force to overcome gravity and mass
                    Vector3 buoyancyForce = Vector3.up * floatForce * submersionRatio * rb.mass;

                    // Counter gravity when in water
                    Vector3 gravityCounter = -Physics.gravity * rb.mass * submersionRatio * 0.8f;

                    // Apply the forces
                    rb.AddForce(buoyancyForce + gravityCounter, ForceMode.Force);
                }
                else
                {
                    if (isInWater)
                    {
                        isInWater = false;
                        rb.linearDamping = originalDrag;
                        rb.angularDamping = originalAngularDrag;
                    }
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

                        // Safely find and position the Background
                        Transform bg = __instance.oxygenGaugeObj.transform.Find("Holder/Background");
                        if (bg != null)
                        {
                            bg.localPosition = new Vector3(323, -20, 0);
                        }

                        // Safely find and position the O2Icon
                        Transform icon = __instance.oxygenGaugeObj.transform.Find("Holder/O2Icon");
                        if (icon != null)
                        {
                            icon.localPosition = new Vector3(315.2346f, -34.1f, 0);
                        }
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
            // Inject the private fields using ref and ___ (three underscores)
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

                            // Accessing private field
                            ___timeSinceLastOxygenReduction = 0f;

                            if (__instance.remainingOxygen <= 0f)
                            {
                                __instance.remainingOxygen = 0f;

                                // Accessing private field
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
                            // Accessing private field
                            ___timeSinceLastOxygenReduction += Time.unscaledDeltaTime;

                            if (___timeSinceLastOxygenReduction >= __instance.oxygenRechargeCooldown)
                            {
                                __instance.remainingOxygen += Time.unscaledDeltaTime * __instance.oxygenRechargeMultiplier;
                            }
                        }
                        if (__instance.remainingOxygen >= __instance.maxOxygenTime)
                        {
                            __instance.remainingOxygen = __instance.maxOxygenTime;

                            // Accessing private field
                            ___timeSinceLastOxygenReduction = 0f;
                        }
                    }
                    return false;
                }
                return true;
            }
        }

        public class UnderwaterPostEffect : MonoBehaviour
        {
            public Material postMaterial;
            public float surfaceYLevel;
            private Camera _camera;
            private bool _isInWaterVolume = false;
            public float depthScale = 1.0f; // Reduced default for stronger effect
            public float depthExponent = 0.7f; // Controls depth curve intensity

            private void Awake()
            {
                _camera = GetComponent<Camera>();
                // Removed static hardcode assignment. It remains disabled until SetWaterState defines its level.
                enabled = false;
            }

            public void SetWaterState(bool inWater, float surfaceLevel)
            {
                surfaceYLevel = surfaceLevel;
                _isInWaterVolume = inWater;
                enabled = inWater;
            }

            private void OnRenderImage(RenderTexture src, RenderTexture dest)
            {
                if (postMaterial != null && _isInWaterVolume)
                {
                    float rawDepth = (surfaceYLevel - transform.position.y) / depthScale;
                    float depthFactor = Mathf.Clamp01(rawDepth);
                    depthFactor = Mathf.Pow(depthFactor, depthExponent); // Apply exponential curve

                    bool isUnderwater = depthFactor > 0.01f;
                    if (isUnderwater)
                    {
                        // Set all depth-related parameters
                        postMaterial.SetFloat("_DepthFactor", depthFactor);
                        postMaterial.SetFloat("_WorldDepth", rawDepth);
                        Graphics.Blit(src, dest, postMaterial);
                        return;
                    }
                }
                Graphics.Blit(src, dest);
            }
        }

        // Utility to get the mesh's top Y in world space
        public float GetMeshTopY()
        {
            var water = gameObject.GetChildAt("Content");
            var triggerMesh = water.GetChild("Mesh");
            var meshRenderer = triggerMesh.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
                return gameObject.transform.position.y; // fallback

            return meshRenderer.bounds.max.y;
        }
    }
}