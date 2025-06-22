using Il2Cpp;
using Il2CppInControl.UnityDeviceProfiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Cube : LE_Object
    {
        BlocScript blocScript;

        void Awake()
        {
            if (EditorController.Instance)
            {
                // COME ON, STUPID CUBE PHYSICS, I HATE YOU!!!!
                Destroy(gameObject.GetChildWithName("Content").GetComponent<Rigidbody>());
            }
        }

        public override void Start()
        {
            if (EditorController.Instance)
            {
                SetCollidersState(false);
                SetEditorCollider(true);
            }

            if (PlayModeController.Instance)
            {
                SetEditorCollider(false);

                if (!initialized) InitComponent();
            }
        }

        void InitComponent()
        {
            gameObject.GetChildWithName("Content").SetActive(false);
            gameObject.GetChildWithName("Content").tag = "Bloc";

            //BlocScript template = FindObjectsOfType<BlocScript>().Where(x => x.IsCube()).ToArray()[0];
            BlocScript template = t_cube;

            blocScript = gameObject.GetChildWithName("Content").AddComponent<BlocScript>();
            blocScript.allCompoundColliders = new Il2CppSystem.Collections.Generic.List<Collider>();
            blocScript.transparentMeshFilter = gameObject.GetChildAt("Content/Bloc_TransparentMesh").GetComponent<MeshFilter>();
            blocScript.normalMesh = template.normalMesh;
            blocScript.errorMesh = template.errorMesh;
            blocScript.playFirstWrongInsert = true;
            blocScript.interactionDistanceMultiplier = 0.91f;
            blocScript.m_light = gameObject.GetChildAt("Content/OnlyForPC/Light").GetComponent<Light>();
            blocScript.lightIntensity = 2;
            blocScript.useSwitchPosRespawn = true;
            blocScript.respawnPosOffsetFromSafeSwitchPos = new Vector3(0f, 1.8f, 0f);
            blocScript.m_rigidbody = blocScript.GetComponent<Rigidbody>();
            blocScript.defaultTransparentColor = new Color(1f, 1f, 1f, 0.5098f);
            blocScript.defaultMirrorColor = new Color(0f, 0f, 0f, 0f);
            blocScript.disabledCollidersWhenInHands = new Collider[0];
            blocScript.enableWhenInHands = new GameObject[0];
            blocScript.redTransparentColor = new Color(1f, 1f, 1f, 0.7059f);
            blocScript.redMirrorColor = new Color(0f, 0f, 0f, 0f);
            blocScript.invalidLightColor = new Color(1f, 0.3892f, 0.2594f, 1f);
            blocScript.normalLightColor = new Color(0.2431f, 0.8431f, 1f, 1f);
            blocScript.m_boxCollider = blocScript.GetComponent<BoxCollider>();
            blocScript.m_audioSource = blocScript.GetComponent<AudioSource>();
            blocScript.m_authorizeRespawn = true;
            blocScript.isFirstPickupEver = true;
            blocScript.m_defaultObject = gameObject.GetChildAt("Content/Bloc_DefaultMesh");
            blocScript.m_transparentObject = gameObject.GetChildAt("Content/Bloc_TransparentMesh");
            blocScript.disableWhenInHands = gameObject.GetChildAt("Content/AdditionalInteractionCollider");
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
            blocScript.activateSwitches = true;
            blocScript.dropMaxDropHorizontalVel = 10;
            blocScript.dropMaxDropHorizontalVel_HS = 60;
            blocScript.dropStopVelTransferMultiplier = 0.8f;
            blocScript.dropVelTransferMultiplier = 0.8f;
            blocScript.FromAboveInteractDistMulti = 1.1f;
            blocScript.moreDisableWhenInHands = new GameObject[0];
            blocScript.moreObjectsToSetActiveElement = new GameObject[0];
            blocScript.moreObjectsToSetActiveOriginalLayerMasks = new Il2CppSystem.Collections.Generic.List<int>();
            blocScript.onDrop = new UnityEngine.Events.UnityEvent();
            blocScript.onFirstPickup = new UnityEngine.Events.UnityEvent();
            blocScript.onPickup = new UnityEngine.Events.UnityEvent();
            blocScript.onRespawn = new UnityEngine.Events.UnityEvent();
            blocScript.onStartFloating = new UnityEngine.Events.UnityEvent();
            blocScript.onStartSinking = new UnityEngine.Events.UnityEvent();
            blocScript.respawnEulerAngles = new Vector3(0f, 300.7243f, 0f);
            blocScript.respawnPosition = blocScript.transform.position;
            blocScript.useContinuousOnDrop = true;
            blocScript.useMeshSwap = true;

            blocScript.m_audioSource.outputAudioMixerGroup = template.m_audioSource.outputAudioMixerGroup;

            DisolveOnEnable disolve = gameObject.GetChildWithName("Content").AddComponent<DisolveOnEnable>();
            disolve.m_renderer = gameObject.GetChildAt("Content/Bloc_DefaultMesh").GetComponent<MeshRenderer>();
            disolve.dissolveMaterials = template.m_dissolve.dissolveMaterials;
            disolve.finalMaterials = template.m_dissolve.finalMaterials;
            disolve.appearSpeed = 3f;
            disolve.startOffset = -1.3f;
            disolve.endOffset = 1.4f;
            disolve.onDissolveAppearFinished = new UnityEngine.Events.UnityEvent();
            disolve.onDissolveDisappearFinished = new UnityEngine.Events.UnityEvent();
            disolve.onEnable = false;
            disolve.useGlobal = false;

            OnlyForPC pcOnly = gameObject.GetChildWithName("Content").AddComponent<OnlyForPC>();
            pcOnly.PC_ExclusiveChild = gameObject.GetChildAt("Content/OnlyForPC");
            pcOnly.allowOnHDEdition = true;
            pcOnly.allowOniOS = true;
            pcOnly.minLightingLevel = 1;
            pcOnly.lightForImportance = gameObject.GetChildAt("Content/OnlyForPC/Light").GetComponent<Light>();
            pcOnly.forceLightMode = true;

            MovingPlatformProxy platformProxy = gameObject.GetChildWithName("Content").AddComponent<MovingPlatformProxy>();
            platformProxy.dynamicProxy = true;

            blocScript.m_dissolve = disolve;
            blocScript.platformProxy = platformProxy;

            blocScript.m_collisionAudioSource.outputAudioMixerGroup = template.m_collisionAudioSource.outputAudioMixerGroup;
            blocScript.m_collisionAudioSource2.outputAudioMixerGroup = template.m_collisionAudioSource2.outputAudioMixerGroup;

            gameObject.GetChildWithName("Content").SetActive(true);

            initialized = true;
        }

        public override bool TriggerAction(string actionName)
        {
            if (actionName == "RespawnCube")
            {
                blocScript.RespawnCubeNow();
                return true;
            }

            return base.TriggerAction(actionName);
        }
    }
}
