using FS_LevelEditor.Editor;
using UnityEngine;

namespace FS_LevelEditor
{

    public class LE_Pocket_Mirror : LE_Object
    {
        BlocScript blocScript;
        LightMirrorController lightMirrorController;
        GameObject light;

        void Awake()
        {
            gameObject.GetChildAt("Content/OnlyForPC").SetActive(true);
            light = gameObject.GetChildAt("Content/OnlyForPC/Light");
            if (EditorController.Instance)
            {
                Destroy(gameObject.GetChild("Content").GetComponent<Rigidbody>());
            }
        }
        public static Dictionary<string, object> GetDefaultProperties()
        {
            return new Dictionary<string, object>()
            {
                { "Light", false }
            };
        }
        public override void ObjectStart(LEScene scene)
        {
            if (scene == LEScene.Editor)
            {
                SetLightState(GetProperty<bool>("Light"));
            }

            base.ObjectStart(scene);
        }
        public override void InitComponent()
        {
            GameObject content = gameObject.GetChild("Content");
            GameObject lightMirror = gameObject.GetChildAt("Content/LightMirror");

            content.SetActive(false);
            content.tag = "Bloc";
            lightMirror.tag = "LightMirror";
            gameObject.GetChildAt("Content/ActivateTrigger").tag = "ActivateTrigger";
            gameObject.GetChildAt("Content/ReflectionObstructor").tag = "ReflectionObstructor";
            gameObject.GetChildAt("Content/AdditionnalInteractionCollider").tag = "InteractionCollider";

            BlocScript template = t_mirror;

            blocScript = content.AddComponent<BlocScript>();
            blocScript.allCompoundColliders = new List<Collider>();
            blocScript.playFirstWrongInsert = true;
            blocScript.interactionDistanceMultiplier = 0.8f;
            blocScript.m_light = gameObject.GetChildAt("Content/OnlyForPC/Light").GetComponent<Light>();
            blocScript.lightIntensity = 2;
            blocScript.useSwitchPosRespawn = true;
            blocScript.respawnPosOffsetFromSafeSwitchPos = new Vector3(0f, 1.8f, 0f);
            blocScript.m_rigidbody = blocScript.GetComponent<Rigidbody>();
            blocScript.defaultTransparentColor = new Color(1f, 1f, 1f, 0.0784f);
            blocScript.defaultMirrorColor = new Color(0f, 0.851f, 1f, 0.1176f);
            blocScript.redTransparentColor = new Color(1f, 0f, 0f, 0.5098f);
            blocScript.redMirrorColor = new Color(1f, 0f, 0f, 0.7843f);
            blocScript.disabledCollidersWhenInHands = new Collider[0];
            blocScript.enableWhenInHands = new GameObject[0];
            blocScript.invalidLightColor = new Color(1f, 0.3114f, 0.2972f, 1f);
            blocScript.normalLightColor = new Color(0.2431f, 0.8431f, 1f, 1f);
            blocScript.m_boxCollider = blocScript.GetComponent<BoxCollider>();
            blocScript.m_audioSource = blocScript.GetComponent<AudioSource>();
            blocScript.m_authorizeRespawn = true;
            blocScript.isFirstPickupEver = true;
            blocScript.isMirror = true;
            blocScript.mirrorMesh = gameObject.GetChildAt("Content/LightMirror/Mesh").GetComponent<MeshRenderer>();
            blocScript.mirrorMat = template.mirrorMat;
            blocScript.mirrorTransparentMat = template.mirrorTransparentMat;
            blocScript.m_defaultObject = gameObject.GetChildAt("Content/LightMirror/RearBlock");
            blocScript.m_transparentObject = gameObject.GetChildAt("Content/LightMirror/RearBlockTransparent");
            blocScript.cubePickupSound = template.cubePickupSound;
            blocScript.m_collisionAudioSource = gameObject.GetChildAt("Content/Audio").GetComponent<AudioSource>();
            blocScript.m_collisionAudioSource2 = gameObject.GetChildAt("Content/Audio2").GetComponent<AudioSource>();
            blocScript.m_collisionSounds = template.m_collisionSounds;
            blocScript.targetScale = Vector3.one;
            blocScript.respawnHeight = -115.73f;
            blocScript.currentWaterState = BlocScript.WaterState.SINK;
            blocScript.iconActivationSound = template.iconActivationSound;
            blocScript.iconDeactivationSound = template.iconDeactivationSound;
            blocScript.ActivateButtonSound = template.ActivateButtonSound;
            blocScript.transparentMaterial = template.transparentMaterial;
            blocScript.activateSwitches = false;
            blocScript.dropMaxDropHorizontalVel = 10;
            blocScript.dropMaxDropHorizontalVel_HS = 60;
            blocScript.dropStopVelTransferMultiplier = 0.25f;
            blocScript.dropVelTransferMultiplier = 1f;
            blocScript.FromAboveInteractDistMulti = 1f;
            blocScript.moreDisableWhenInHands = new GameObject[0];
            blocScript.moreObjectsToSetActiveElement = new GameObject[0];
            blocScript.moreObjectsToSetActiveOriginalLayerMasks = new List<int>();
            blocScript.onDrop = new UnityEngine.Events.UnityEvent();
            blocScript.onFirstPickup = new UnityEngine.Events.UnityEvent();
            blocScript.onPickup = new UnityEngine.Events.UnityEvent();
            blocScript.onRespawn = new UnityEngine.Events.UnityEvent();
            blocScript.onStartFloating = new UnityEngine.Events.UnityEvent();
            blocScript.onStartSinking = new UnityEngine.Events.UnityEvent();
            blocScript.respawnEulerAngles = blocScript.transform.eulerAngles;
            blocScript.respawnPosition = blocScript.transform.position;
            blocScript.useContinuousOnDrop = false;
            blocScript.useMeshSwap = false;

            blocScript.m_audioSource.outputAudioMixerGroup = template.m_audioSource.outputAudioMixerGroup;

            OnlyForPC pcOnly = content.AddComponent<OnlyForPC>();
            pcOnly.PC_ExclusiveChild = gameObject.GetChildAt("Content/OnlyForPC");
            pcOnly.allowOnHDEdition = false;
            pcOnly.allowOniOS = false;
            pcOnly.minLightingLevel = 0;
            pcOnly.forceLightMode = false;

            ZeroGConfigurator zeroG = content.AddComponent<ZeroGConfigurator>();
            zeroG.m_rigidBody = blocScript.m_rigidbody;
            zeroG.defaultDrag = 0.1f;
            zeroG.zeroGDrag = 2f;
            zeroG.defaultAngularDrag = 0.05f;
            zeroG.zeroGAngularDrag = 2f;

            MovingPlatformProxy platformProxy = content.AddComponent<MovingPlatformProxy>();
            platformProxy.dynamicProxy = true;

            blocScript.platformProxy = platformProxy;

            blocScript.m_collisionAudioSource.outputAudioMixerGroup = template.m_collisionAudioSource.outputAudioMixerGroup;
            blocScript.m_collisionAudioSource2.outputAudioMixerGroup = template.m_collisionAudioSource2.outputAudioMixerGroup;

            LightMirrorController templateMirrorController = template.GetComponentInChildren<LightMirrorController>(true);
            lightMirrorController = lightMirror.AddComponent<LightMirrorController>();
            lightMirrorController.rotationSpeed = 1f;
            lightMirrorController.speedrunModeMultiplier = 1f;
            lightMirrorController.rotationDuration = 1f;
            lightMirrorController.rotateSounds = templateMirrorController.rotateSounds;
            lightMirror.GetComponent<AudioSource>().outputAudioMixerGroup = templateMirrorController.GetComponent<AudioSource>().outputAudioMixerGroup;

            ForwardPhysicsEvents physicsEvents = gameObject.GetChildAt("Content/ReflectionObstructor").AddComponent<ForwardPhysicsEvents>();
            physicsEvents.forwardTarget = blocScript.m_rigidbody;
            physicsEvents.shootForceMulti = 1f;

            content.SetActive(true);

            SetLightState(GetProperty<bool>("Light"));
            gameObject.GetChildAt("Content/OnlyForPC").SetActive(true);

            blocScript.InvokeRepeating(nameof(BlocScript.CheckForRespawn), 2f, 2f);

            initialized = true;
        }

        public override bool TriggerAction(string actionName)
        {
            if (actionName == "RespawnCube")
            {
                blocScript.RespawnCubeNow();
                return true;
            }
            else if (actionName == "RespawnCubeFromStartPoint")
            {
                blocScript.useSwitchPosRespawn = false;
                blocScript.RespawnCubeNow();
                blocScript.useSwitchPosRespawn = true;
                return true;
            }
            else if (actionName == "RotateMirror")
            {
                lightMirrorController.Activate();
                return true;
            }

            return base.TriggerAction(actionName);
        }
        public override bool SetProperty(string name, object value)
        {
            if (name == "Light")
            {
                if (value is bool boolValue)
                {
                    properties["Light"] = boolValue;
                    if (EditorController.Instance) SetLightState(boolValue);
                }
            }

            return base.SetProperty(name, value);
        }
        void SetLightState(bool enabled)
        {
            light.SetActive(enabled);

            if (EditorController.Instance)
            {
                foreach (var waypoint in waypointSupport.spawnedWaypoints)
                {
                    waypoint.gameObject.GetChildAt("Content/OnlyForPC/Light").SetActive(enabled);
                }
            }
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(BlocScript), nameof(BlocScript.RespawnCubeNow), [typeof(bool)])]
    public static class PocketMirrorWithoutCollisionAfterRespawnPatch
    {
        public static void Postfix(BlocScript __instance)
        {
            if (__instance.transform.parent && __instance.transform.parent.TryGetComponent<LE_Pocket_Mirror>(out LE_Pocket_Mirror mirror))
            {
                if (!mirror.currentCollisionState) mirror.SetCollidersState(false);
            }
        }
    }
}
