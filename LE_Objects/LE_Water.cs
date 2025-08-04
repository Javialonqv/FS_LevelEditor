using FS_LevelEditor.Editor.UI;
using FS_LevelEditor.Playmode;
using Il2Cpp;
using MelonLoader;
using UnityEngine;
using UnityEngine.Events;
using HarmonyLib;
using System.Collections;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Water : LE_Object
    {
        private GameObject envCam;
        public static float waterLevelY;
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

            // Setup the main water mesh (Content)
            var waterMeshFilter = water.GetComponent<MeshFilter>();
            var waterMeshRenderer = water.GetComponent<MeshRenderer>();
            Material waterMat = Utils.LoadAsset<Material>("Water_Cyan_IntroFlood", "leveleditoricons");
            waterMat.shader = Shader.Find("Lux Water/WaterSurface");
            waterMeshRenderer.material = waterMat;

            // Setup the trigger mesh (Content/Mesh)
            GameObject triggerMesh = water.GetChild("Mesh");
            var triggerMeshFilter = triggerMesh.GetComponent<MeshFilter>();
            var triggerMeshRenderer = triggerMesh.GetComponent<MeshRenderer>();

            // Remove any existing collider to avoid duplicates
            var existingCollider = water.GetComponent<BoxCollider>();
            if (existingCollider != null)
                DestroyImmediate(existingCollider);

            // Add collider to water object using trigger mesh bounds
            var collider = water.AddComponent<BoxCollider>();
            collider.isTrigger = true;

            // Use the trigger mesh bounds for the collider
            var bounds = triggerMeshRenderer.bounds;
            // Convert world bounds to local space of water object
            Vector3 localCenter = water.transform.InverseTransformPoint(bounds.center);
            Vector3 worldSize = bounds.size;
            // Convert world size to local size
            Vector3 localSize = new Vector3(
                worldSize.x / water.transform.lossyScale.x,
                worldSize.y / water.transform.lossyScale.y,
                worldSize.z / water.transform.lossyScale.z
            );

            collider.center = localCenter;
            collider.size = localSize;

            // Add WaterPatch to the water object (not the trigger mesh)
            WaterPatch waterPatch = water.AddComponent<WaterPatch>();
            float surfaceY = GetMeshTopY();
            waterPatch.waterSurfaceY = surfaceY;
            waterLevelY = surfaceY;

            SetupUnderwaterCamera();
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

        public override List<string> GetAvailableEventsIDs()
        {
            return new List<string>()
            {
                "OnEnter",
                "OnExit"
            };
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

        private void SetupUnderwaterCamera()
        {
            if (_cameraSetupDone) return;

            Debug.Log("[UnderwaterSetup] Starting camera setup...");

            MelonCoroutines.Start(SetupEnvCam());
        }

        IEnumerator SetupEnvCam()
        {
            while (envCam == null)
            {
                envCam = GameObject.Find("EnvCam");
                yield return null;
            }
            Camera cam = envCam.GetComponent<Camera>();


            Debug.Log($"[UnderwaterSetup] Camera found: {cam.name}");

            // Check render pipeline
            var renderPipeline = UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset;
            // Enable depth texture
            cam.depthTextureMode = DepthTextureMode.Depth;
            Debug.Log("[UnderwaterSetup] Depth texture enabled");

            // Load shader
            Shader underwaterShader = Utils.LoadAsset<Shader>("Underwater", "leveleditoricons");

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
            float surfaceY = GetMeshTopY();
            postEffect.SetWaterState(true, surfaceY);

            // Force enable for testing
            postEffect.enabled = true;
            _cameraSetupDone = true;
        }
        [MelonLoader.RegisterTypeInIl2Cpp]
        public class WaterPatch : MonoBehaviour
        {
            public float waterSurfaceY;
            public LayerMask groundLayerMask = 1; // What layers count as ground
            private bool playerInWater = false; // Track water state
            private float lastWaterStateChange = 0f; // Cooldown timer
            private const float WATER_STATE_COOLDOWN = 0.5f; // Half second cooldown
            private bool hadJetpack = false;
            private void OnTriggerStay(Collider other)
            {
                if (!other.CompareTag("Player")) return;
                EnterWaterState();
                // Cooldown to prevent rapid state changes
                if (Time.time - lastWaterStateChange < WATER_STATE_COOLDOWN) return;

                Vector3 playerPos = other.transform.position;
                bool shouldBeInWater = playerPos.y <= waterSurfaceY;

                // Player should enter water
                if (shouldBeInWater && !playerInWater)
                {
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

            private void OnTriggerExit(Collider other)
            {
                if (other.CompareTag("Player") && playerInWater)
                {
                    // Force exit when leaving trigger completely
                    ExitWaterState();
                }
            }
            private void ExitWaterState()
            {
                if (!playerInWater) return; // Already exited

                MelonLogger.Msg("Player exiting water");

                GameObject envCam = Controls.Instance.gameObject.GetChild("PlayerCamera/EnvCam");
                if (envCam != null)
                {
                    UnderwaterPostEffect postEffect = envCam.GetComponent<UnderwaterPostEffect>();
                    if (postEffect != null && Mathf.Approximately(postEffect.surfaceYLevel, waterSurfaceY))
                    {
                        postEffect.SetWaterState(false, waterSurfaceY);
                    }
                }

                // Restore player state
                Controls.Instance.OnWaterExit(false, false);
                Controls.Instance.SetFlashlightAllowed();
                playerInWater = false;
                lastWaterStateChange = Time.time;
                if (hadJetpack) Controls.Instance.hasJetPack = true; hadJetpack = false;
                MelonLogger.Msg("Water exit complete - flashlight should be restored");
            }

            private void EnterWaterState()
            {
                if (playerInWater) return; // Already in water

                GameObject envCam = Controls.Instance.gameObject.GetChild("PlayerCamera/EnvCam");
                if (envCam == null) return;

                UnderwaterPostEffect postEffect = envCam.GetComponent<UnderwaterPostEffect>();
                if (postEffect == null) return;

                // Set water state to IN
                postEffect.SetWaterState(true, waterSurfaceY);

                // Player state management
                Controls.Instance.OnWaterEnter(waterSurfaceY);
                Controls.Instance.SetFlashlightNotAllowed();
                playerInWater = true;
                lastWaterStateChange = Time.time;
                if (Controls.Instance.hasJetPack) hadJetpack = true; Controls.Instance.hasJetPack = false;
                MelonLogger.Msg("Player entered water");
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
                float controllerHeight = .82f; // Default fallback

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
            }
        }
        [RegisterTypeInIl2Cpp]
        public class CubeFloatSystem : MonoBehaviour
        {
            public float waterLevel = -34.6f;
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
                originalDrag = rb.drag;
                originalAngularDrag = rb.angularDrag;
            }

            void FixedUpdate()
            {
                if (rb == null) return;

                float cubeBottom = transform.position.y - (transform.localScale.y * 0.5f);
                float cubeTop = transform.position.y + (transform.localScale.y * 0.5f);
                float waterSurface = waterLevel;

                // Check if cube is touching or below water
                if (cubeBottom < waterSurface + detectionOffset)
                {
                    if (!isInWater)
                    {
                        isInWater = true;
                        rb.drag = waterDrag;
                        rb.angularDrag = waterAngularDrag;
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
                        rb.drag = originalDrag;
                        rb.angularDrag = originalAngularDrag;
                    }
                }
            }


            [HarmonyPatch(typeof(InGameUIManager), nameof(InGameUIManager.ShowOxygenGauge))]
            public static class GaugeFix
            {
                public static void Postfix(InGameUIManager __instance, bool _state)
                {
                    if (__instance.oxygenGaugeObj)
                    {
                        if (_state)
                        {
                            __instance.oxygenGaugeObj.SetActive(true);
                            __instance.oxygenGaugeObj.gameObject.GetChild("Holder/Background").transform.localPosition = new Vector3(323, -20, 0);
                            __instance.oxygenGaugeObj.gameObject.GetChild("Holder/O2Icon").transform.localPosition = new Vector3(315.2346f, -34.1f, 0);
                            return;
                        }
                        __instance.oxygenGaugeObj.SetActive(false);
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
                public static bool Prefix(Controls __instance)
                {
                    if (PlayModeController.Instance)
                    {
                        if (!__instance.GetPause() && (Controls.gameInFocus || Il2Cpp.Utils.isMobilePlatform) && Controls.QuickloadFinished && !__instance.GetInMiniGame() && !__instance.inKinematic && !__instance.GetCameraTransitionState())
                        {
                            bool flag = true;
                            if (__instance.isSwimming && !__instance.isAtWaterSurface)
                            {
                                if (!__instance.debug)
                                {
                                    __instance.remainingOxygen -= Time.unscaledDeltaTime * __instance.oxygenConsumptionMultiplier;
                                }
                                flag = false;
                                __instance.timeSinceLastOxygenReduction = 0f;
                                if (__instance.remainingOxygen <= 0f)
                                {
                                    __instance.remainingOxygen = 0f;
                                    if (__instance.lastOxyDmgTime >= 1f)
                                    {
                                        __instance.DamageCharacter(Mathf.CeilToInt(__instance.noOxyDamage));
                                        __instance.lastOxyDmgTime = 0f;
                                    }
                                    else
                                    {
                                        __instance.lastOxyDmgTime += Time.unscaledDeltaTime;
                                    }
                                }
                            }
                            if (flag && __instance.remainingOxygen < __instance.maxOxygenTime)
                            {
                                __instance.timeSinceLastOxygenReduction += Time.unscaledDeltaTime;

                                if (__instance.timeSinceLastOxygenReduction >= __instance.oxygenRechargeCooldown)
                                {
                                    __instance.remainingOxygen += Time.unscaledDeltaTime * __instance.oxygenRechargeMultiplier;
                                }
                            }
                            if (__instance.remainingOxygen >= __instance.maxOxygenTime)
                            {
                                __instance.remainingOxygen = __instance.maxOxygenTime;
                                __instance.timeSinceLastOxygenReduction = 0f;
                            }
                        }
                        return false;
                    }
                    return true;
                }
            }

        }

        [MelonLoader.RegisterTypeInIl2Cpp]
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
                surfaceYLevel = LE_Water.waterLevelY;
                _camera = GetComponent<Camera>();
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