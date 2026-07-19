using FS_LevelEditor.Editor;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Power_Core : LE_Object
    {
        public BlocScript blocScript;

        public bool insertToPowerSlotOnStart = false;
        public LE_Power_Slot powerSlotToPreInsertTo = null;

        public LE_Power_Slot currentlyInsertedSlot = null;

        void Awake()
        {
            if (EditorController.Instance)
            {
                // COME ON, STUPID *POWER CORE PHYSICS, I HATE YOU!!!!
                Destroy(gameObject.GetChild("Content").GetComponent<Rigidbody>());
            }
        }

        public override void InitComponent()
        {
            contentObject.SetActive(false);

            contentObject.tag = "Bloc";

            blocScript = contentObject.AddComponent<BlocScript>();
            blocScript.activateSwitches = false;
            blocScript.rigidBodiesInContact = new Il2CppSystem.Collections.Generic.List<UnityEngine.Rigidbody>();
            blocScript.useMeshSwap = false;
            blocScript.useErrorDifferentMat = true;
            blocScript.mainTransparentMeshRenderer = contentObject.GetChild("PowerCore_TransparentMesh").GetComponent<MeshRenderer>();
            blocScript.normalTransparentMat = t_powerCore.normalTransparentMat;
            blocScript.errorMat = t_powerCore.errorMat;
            blocScript.playFirstWrongInsert = true;
            blocScript.interactionDistanceMultiplier = 0.8f;
            blocScript.m_light = contentObject.GetChild("Light").GetComponent<Light>();
            blocScript.lightIntensity = 2;
            blocScript.m_iconAnim = blocScript.m_iconAnim;
            blocScript.useSwitchPosRespawn = true;
            blocScript.respawnPosOffsetFromSafeSwitchPos = t_powerCore.respawnPosOffsetFromSafeSwitchPos;
            blocScript.respawnPosOffsetFromInitialPos = Vector3.zero;
            blocScript.m_rigidbody = contentObject.GetComponent<Rigidbody>();
            blocScript.defaultTransparentColor = t_powerCore.defaultTransparentColor;
            blocScript.defaultMirrorColor = t_powerCore.defaultMirrorColor;
            blocScript.redTransparentColor = t_powerCore.redTransparentColor;
            blocScript.redMirrorColor = t_powerCore.redMirrorColor;
            blocScript.invalidLightColor = t_powerCore.invalidLightColor;
            blocScript.normalLightColor = t_powerCore.normalLightColor;
            blocScript.m_boxCollider = contentObject.GetChild("SimplifiedCollider").GetComponent<BoxCollider>();
            blocScript.compoundColliders = contentObject.GetChild("CompoundColliders");
            blocScript.playerCollisionOnly = contentObject.GetChild("PlayerCollisionOnly");
            blocScript.m_audioSource = contentObject.GetComponent<AudioSource>();
            blocScript.m_authorizeRespawn = true;
            blocScript.killZonesToIgnore = new Il2CppSystem.Collections.Generic.List<GameObject>();
            blocScript.m_activateSwitchesWhileZeroG = true;
            blocScript.onPickup = new UnityEngine.Events.UnityEvent();
            blocScript.onDrop = new UnityEngine.Events.UnityEvent();
            blocScript.onRespawn = new UnityEngine.Events.UnityEvent();
            blocScript.onFirstPickup = new UnityEngine.Events.UnityEvent();
            blocScript.isFirstPickupEver = true;
            blocScript.firstEnableEver = false;
            blocScript.firstInitSinceLevelLoad = false;
            blocScript.m_defaultObject = contentObject.GetChild("PowerCore_DefaultMesh");
            blocScript.m_transparentObject = contentObject.GetChild("PowerCore_TransparentMesh");
            blocScript.disableWhenInHands = contentObject.GetChild("InteractionAdditionalCollider");
            blocScript.disabledCollidersWhenInHands = new Collider[0];
            blocScript.dropStopVelTransferMultiplier = 0.5f;
            blocScript.enableWhenInHands = new GameObject[0];
            blocScript.moreDisableWhenInHands = new GameObject[0];
            blocScript.m_collisionAudioSource = contentObject.GetChild("Audio").GetComponent<AudioSource>();
            blocScript.m_collisionAudioSource2 = contentObject.GetChild("Audio2").GetComponent<AudioSource>();
            blocScript.m_collisionSounds = t_powerCore.m_collisionSounds;
            blocScript.m_powerCoreLayerCheck = t_powerCore.m_powerCoreLayerCheck;
            blocScript.targetScale = Vector3.one;
            blocScript.m_iconPosition2 = contentObject.GetChild("IconPosition2").transform;
            blocScript.respawnHeight = -115.73f;
            blocScript.currentWaterState = t_powerCore.currentWaterState;
            blocScript.onStartFloating = new UnityEngine.Events.UnityEvent();
            blocScript.onStartSinking = new UnityEngine.Events.UnityEvent();
            blocScript.iconActivationSound = t_powerCore.iconActivationSound;
            blocScript.iconDeactivationSound = t_powerCore.iconDeactivationSound;
            blocScript.ActivateButtonSound = t_powerCore.ActivateButtonSound;
            blocScript.transparentMaterial = t_powerCore.transparentMaterial;
            blocScript.isPowerCore = true;
            blocScript.allCompoundColliders = new Il2CppSystem.Collections.Generic.List<Collider>();
            blocScript.character = Controls.Instance.player;
            blocScript.hand = HandController.Instance.gameObject;
            blocScript.handBook = HandController.Instance.handBook;
            blocScript.handLog = HandController.Instance.handLog;
            blocScript.handPandora = HandController.Instance.handPandora;
            blocScript.handPowerCore = HandController.Instance.handPowerCore;
            blocScript.handTablet = HandController.Instance.handTablet;
            blocScript.respawnPosition = blocScript.transform.position;
            blocScript.respawnEulerAngles = blocScript.transform.eulerAngles;

            blocScript.m_audioSource.outputAudioMixerGroup = t_powerCore.m_audioSource.outputAudioMixerGroup;

            PowerCoreBlocController coreBloc = contentObject.AddComponent<PowerCoreBlocController>();
            coreBloc.blocScript = blocScript;
            coreBloc.m_defaultMats = t_powerCore.powerCoreBlocScript.m_defaultMats;
            coreBloc.m_activatedMats = t_powerCore.powerCoreBlocScript.m_activatedMats;
            coreBloc.m_transparentMats = t_powerCore.powerCoreBlocScript.m_transparentMats;
            coreBloc.m_mesh1 = contentObject.GetChild("PowerCore_DefaultMesh").GetComponent<MeshRenderer>();
            coreBloc.m_light1 = blocScript.m_light;
            coreBloc.m_lightDefault = t_powerCore.powerCoreBlocScript.m_lightDefault;
            coreBloc.m_lightActive = t_powerCore.powerCoreBlocScript.m_lightActive;
            blocScript.powerCoreBlocScript = coreBloc;

            MovingPlatformProxy proxy = contentObject.AddComponent<MovingPlatformProxy>();
            proxy.dynamicProxy = true;
            blocScript.platformProxy = proxy;

            //DeactivateOnStart deactivate = contentObject.AddComponent<DeactivateOnStart>();
            //deactivate.cachedGO = contentObject;

            DisolveOnEnable disolve = contentObject.AddComponent<DisolveOnEnable>();
            disolve.appearSpeed = 3;
            disolve.currentOffset = -1;
            disolve.disableAfterDisappear = true;
            disolve.dissolveMaterials = t_powerCore.m_dissolve.dissolveMaterials;
            disolve.endOffset = 1.5f;
            disolve.finalMaterials = t_powerCore.m_dissolve.finalMaterials;
            disolve.ignoreTimeScale = true;
            disolve.m_renderer = coreBloc.m_mesh1; // PowerCore_DefaultMesh
            disolve.onDissolveAppearFinished = new UnityEngine.Events.UnityEvent();
            disolve.onDissolveDisappearFinished = new UnityEngine.Events.UnityEvent();
            disolve.onEnable = false;
            disolve.speedrunModeMultiplier = 1;
            disolve.startOffset = -1;
            disolve.useGlobal = false;
            disolve.useLineRenderer = false;
            blocScript.m_dissolve = disolve;

            blocScript.m_collisionAudioSource.outputAudioMixerGroup = t_powerCore.m_collisionAudioSource.outputAudioMixerGroup;
            blocScript.m_collisionAudioSource2.outputAudioMixerGroup = t_powerCore.m_collisionAudioSource2.outputAudioMixerGroup;

            #region Compound Colliders
            var compoundColliders = contentObject.GetChild("CompoundColliders");
            List<GameObject> objectsToSetAsActiveElement = new List<GameObject>();

            var centerCompound = compoundColliders.GetChild("Center");
            centerCompound.tag = "Bloc";
            centerCompound.layer = LayerMask.NameToLayer("IgnorePlayerCollision");
            centerCompound.GetComponent<MeshCollider>().material = t_powerCore.compoundColliders.GetChild("Center").GetComponent<MeshCollider>().material;
            centerCompound.AddComponent<ForwardPhysicsEvents>().forwardTarget = blocScript.m_rigidbody;
            objectsToSetAsActiveElement.Add(centerCompound);

            var rightCompound = compoundColliders.GetChild("Right");
            rightCompound.layer = LayerMask.NameToLayer("IgnorePlayerCollision");

            var leftCompound = compoundColliders.GetChild("Left");
            leftCompound.layer = LayerMask.NameToLayer("IgnorePlayerCollision");

            foreach (var collider in rightCompound.GetChilds()) // Right colliders.
            {
                collider.tag = "Bloc";
                if (collider.name.Contains("Physics"))
                {
                    collider.layer = LayerMask.NameToLayer("PhysicsOnly");
                }
                else if (collider.name.Contains("Laser"))
                {
                    collider.layer = LayerMask.NameToLayer("LaserObstructionOnly");
                }
                else
                {
                    collider.layer = LayerMask.NameToLayer("IgnorePlayerCollision");
                }
                collider.AddComponent<ForwardPhysicsEvents>().forwardTarget = blocScript.m_rigidbody;
                blocScript.allCompoundColliders.Add(collider.GetComponent<Collider>());
                objectsToSetAsActiveElement.Add(collider);
            }
            foreach (var collider in leftCompound.GetChilds()) // Left colliders.
            {
                collider.tag = "Bloc";
                if (collider.name.Contains("Physics"))
                {
                    collider.layer = LayerMask.NameToLayer("PhysicsOnly");
                }
                else if (collider.name.Contains("Laser"))
                {
                    collider.layer = LayerMask.NameToLayer("LaserObstructionOnly");
                }
                else
                {
                    collider.layer = LayerMask.NameToLayer("IgnorePlayerCollision");
                }
                collider.AddComponent<ForwardPhysicsEvents>().forwardTarget = blocScript.m_rigidbody;
                blocScript.allCompoundColliders.Add(collider.GetComponent<Collider>());
                objectsToSetAsActiveElement.Add(collider);
            }

            blocScript.moreObjectsToSetActiveElement = objectsToSetAsActiveElement.ToArray();
            #endregion

            blocScript.m_boxCollider.material = t_powerCore.m_boxCollider.material;
            blocScript.playerCollisionOnly.GetComponent<BoxCollider>().material = t_powerCore.playerCollisionOnly.GetComponent<BoxCollider>().material;

            // ---------- TAGS & LAYERS ----------
            blocScript.disableWhenInHands.tag = "InteractionCollider";
            blocScript.disableWhenInHands.layer = LayerMask.NameToLayer("ActivableCheck");
            coreBloc.m_mesh1.transform.GetChild(0).tag = "Bloc";
            coreBloc.m_mesh1.transform.GetChild(0).gameObject.layer = LayerMask.NameToLayer("PlayerCollisionOnly");

            blocScript.m_transparentObject.gameObject.tag = "Untagged";
            blocScript.m_transparentObject.gameObject.layer = LayerMask.NameToLayer("ActiveElement");

            contentObject.GetChild("ActivateTrigger").tag = "ActivateTrigger";
            contentObject.GetChild("ActivateTrigger").layer = LayerMask.NameToLayer("Ignore Raycast");

            blocScript.compoundColliders.tag = "Untagged";
            blocScript.compoundColliders.layer = LayerMask.NameToLayer("IgnorePlayerCollision");

            blocScript.m_boxCollider.tag = "Bloc";
            blocScript.m_boxCollider.gameObject.layer = LayerMask.NameToLayer("ActiveElement");

            blocScript.playerCollisionOnly.tag = "Bloc";
            blocScript.playerCollisionOnly.layer = LayerMask.NameToLayer("PlayerCollisionOnly");

            contentObject.SetActive(true);

            initialized = true;
        }

        public override void ObjectStart(LEScene scene)
        {
            if (scene == LEScene.Playmode && insertToPowerSlotOnStart)
            {
                contentObject.transform.position = powerSlotToPreInsertTo.powerCore.m_powerCoreHolder.transform.position;
                contentObject.transform.rotation = powerSlotToPreInsertTo.powerCore.m_powerCoreHolder.transform.rotation;

                // Make sure to save the respawn position.
                blocScript.respawnPosition = contentObject.transform.position;
                blocScript.respawnEulerAngles = contentObject.transform.eulerAngles;

                ForceInsertion(powerSlotToPreInsertTo, blocScript, true);
            }

            base.ObjectStart(scene);
        }

        public static void ForceInsertion(LE_Power_Slot slot, BlocScript core, bool executeEvents)
        {
            // If we want the OnInsert and OnRemove actions to be executed, _fromSave needs to be false.
            slot.powerCore.OnInsert(core, !executeEvents);
            core.m_currentlyInsertedPowerCore = slot.powerCore;
            core.m_rigidbody.isKinematic = true;
            core.SetDisabledWhileInHands(false);
            core.SetEnabledWhenInHands(false);
            if (core.playerCollisionOnly)
                core.playerCollisionOnly.SetActive(false);
        }
    }

    // Forces pre-inserted cores to insert themselves again into their original slot after respawning.
    [HarmonyLib.HarmonyPatch(typeof(BlocScript), nameof(BlocScript.RespawnCubeNow), [typeof(bool)])]
    public static class PowerCoreRespawnPatch
    {
        public static void Postfix(BlocScript __instance)
        {
            // Check if the object is from LE
            LE_Power_Core core = __instance.GetComponentInParent<LE_Power_Core>();

            // If not inserted into a slot, respawn in the main one, PRE-INSERTED CORES ONLY.
            if (core && core.powerSlotToPreInsertTo)
            {
                // Force the respawn values, in case the damn FS code adds an offset or something.
                core.contentObject.transform.position = core.powerSlotToPreInsertTo.powerCore.m_powerCoreHolder.transform.position;
                core.contentObject.transform.rotation = core.powerSlotToPreInsertTo.powerCore.m_powerCoreHolder.transform.rotation;

                LE_Power_Core.ForceInsertion(core.powerSlotToPreInsertTo, core.blocScript, true);
            }
        }
    }
}
