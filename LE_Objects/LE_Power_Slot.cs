using FS_LevelEditor.Editor;
using FS_LevelEditor.Misc;
using FS_LevelEditor.Playmode;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using static MelonLoader.MelonLaunchOptions;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Power_Slot : LE_Object
    {
        public enum PowerSlotState
        {
            DEACTIVATED,
            ACTIVATED
        }
        public PowerCoreController powerCore;
        MeshRenderer mesh;
        GameObject editorPowerCore;

        BlocScript activePowerCore;
        LE_Power_Core activePowerCoreLE;

        public override string[] EventsIDs =>
        [
            "OnBoth",
            "OnInsert",
            "OnRemove"
        ];

        public static Dictionary<string, object> GetDefaultProperties()
        {
            return new Dictionary<string, object>
            {
                { "InitialState", PowerSlotState.DEACTIVATED },
                { "OnBoth", new List<LE_Event>() },
                { "OnInsert", new List<LE_Event>() },
                { "OnRemove", new List<LE_Event>() }
            };
        }

        void Awake()
        {
            mesh = contentObject.GetChild("Mesh").GetComponent<MeshRenderer>();
            editorPowerCore = contentObject.GetChild("Editor_PowerCore");
        }

        public override void ObjectStart(LEScene scene)
        {
            if (scene == LEScene.Editor)
                SetStateOnEditor(GetProperty<PowerSlotState>("InitialState"));

            if (scene == LEScene.Playmode)
            {
                if (GetProperty<PowerSlotState>("InitialState") == PowerSlotState.ACTIVATED)
                {
                    GameObject initialPowerCore = PlayModeController.Instance.PlaceObject(ObjectType.POWER_CORE, transform.position, transform.eulerAngles, transform.localScale, false);
                    LE_Power_Core initialPowerCoreScript = initialPowerCore.GetComponent<LE_Power_Core>();

                    // Set this variables, so we wait till LE_Power_Core.ObjectStart is called, and THEN it'll initialize itself into this power slot.
                    initialPowerCoreScript.insertToPowerSlotOnStart = true;
                    initialPowerCoreScript.powerSlotToPreInsertTo = this;
                }
            }

            base.ObjectStart(scene);
        }

        public override void InitComponent()
        {
            contentObject.SetActive(false);
            contentObject.tag = "PowerCoreSlot";

            powerCore = contentObject.AddComponent<PowerCoreController>();
            powerCore.randomKeys = new Il2CppSystem.Collections.Generic.List<string>();
            powerCore.interactionColliders = new Il2CppSystem.Collections.Generic.List<UnityEngine.Collider>();
            powerCore.m_powerCoreHolder = contentObject.GetChild("PowerCoreHolder").transform;
            powerCore.m_insertSound = t_powerSlot.m_insertSound;
            powerCore.m_removeSound = t_powerSlot.m_removeSound;
            powerCore.m_audioSource = contentObject.GetChild("Audio").GetComponent<AudioSource>();
            powerCore.m_activateOnFirstInsert = true;
            powerCore.m_inactiveColor = t_powerSlot.m_inactiveColor;
            powerCore.m_activeColor = t_powerSlot.m_activeColor;
            powerCore.m_validKeyColor = t_powerSlot.m_validKeyColor;
            powerCore.m_invalidKeyColor = t_powerSlot.m_invalidKeyColor;
            powerCore.objectsToActivate = new GameObject[0];
            powerCore.objectsToEnableOnly = new GameObject[0];
            powerCore.objectsToDestroy = new GameObject[0];
            powerCore.m_defaultMats = t_powerSlot.m_defaultMats;
            powerCore.m_activatedMats = t_powerSlot.m_activatedMats;
            powerCore.m_mesh1 = contentObject.GetChild("Mesh").GetComponent<MeshRenderer>();
            ConfigureEvents(powerCore);
            powerCore.m_onFirstInsert = new UnityEngine.Events.UnityEvent();
            powerCore.m_onCorrectKeyInsert = new UnityEngine.Events.UnityEvent();
            powerCore.m_onWrongKeyInsert = new UnityEngine.Events.UnityEvent();
            powerCore.m_particles = contentObject.GetChild("Sparks").GetComponent<ParticleSystem>();
            powerCore.interactionDistanceMultiplier = 0.8f;
            powerCore.visibilityPoint = contentObject.GetChild("VisibilityPoint").transform;

            #region Ceiling Light
            GameObject lightObj = contentObject.GetChild("GroundSpot_Realtime_PowerCoreSlot");

            RealtimeCeilingLight lightComp = lightObj.AddComponent<RealtimeCeilingLight>();
            lightComp.m_light = lightObj.GetChild("Light").GetComponent<Light>();
            lightComp.active = false;
            lightComp.activeEditorState = false;
            lightComp.allLightConePlanesRenderers = new Il2CppSystem.Collections.Generic.List<MeshRenderer>();
            lightComp.allLightConePlanesRenderers.Add(lightObj.GetChildAt("LightConePlanes/LightConePlane").GetComponent<MeshRenderer>());
            lightComp.allLightConePlanesRenderers.Add(lightObj.GetChildAt("LightConePlanes/LightConePlane (1)").GetComponent<MeshRenderer>());
            lightComp.animStateBeforeShot = true;
            lightComp.audioSource = lightObj.GetComponent<AudioSource>();
            lightComp.canBeDestroyedByHS = true;
            lightComp.currentColor = RealtimeCeilingLight.LightColor.DEFAULT;
            lightComp.editorIntensity = 2;
            lightComp.frameCount = 2;
            lightComp.idleAnim = "CeilingLight_Blink_MediumIntensity";
            lightComp.idleOnIntensity = -1;
            lightComp.intensityEditorValue = 2;
            lightComp.isBakedOnly = false;
            lightComp.isDestroyed = false;
            lightComp.keepProbeEnabled = true;
            lightComp.lightConePlane_default = t_ceilingLight.lightConePlane_default;
            lightComp.lightConePlane_greenColor = t_ceilingLight.lightConePlane_greenColor;
            lightComp.lightConePlane_redColor = t_ceilingLight.lightConePlane_redColor;
            lightComp.lightConePlanes = lightObj.GetChild("LightConePlanes");
            lightComp.m_animationComp = lightObj.GetComponent<Animation>();
            lightComp.m_defaultColor = Color.white;
            lightComp.m_defaultColorNeonMesh = t_ceilingLight.m_defaultColorNeonMesh;
            lightComp.m_flareMultiplier = 7;
            lightComp.m_greenColor = new Color(0.3309f, 1f, 0.4186f, 1f);
            lightComp.m_greenColorNeonMesh = t_ceilingLight.m_greenColorNeonMesh;
            lightComp.m_lensFlare = lightObj.GetChild("Flare").GetComponent<LensFlare>();
            lightComp.m_light = lightObj.GetChildAt("Light").GetComponent<Light>();
            lightComp.m_maxFlair = 1.5f;
            lightComp.m_redColor = new Color(1f, 0.3162f, 0.3162f, 1f);
            lightComp.m_redColorNeonMesh = t_ceilingLight.m_redColorNeonMesh;
            lightComp.neonOnMeshFilter = lightObj.GetChildAt("Mesh/NeonOn").GetComponent<MeshFilter>();
            lightComp.offProbeIntensity = 0.4f;
            lightComp.offProbeIntensity_shot = 0.2f;
            lightComp.onProbeIntensity = 0.7f;
            lightComp.rangeEditorValue = 15;
            lightComp.reactToTaserShot = true;
            lightComp.rendererNeonOff = lightObj.GetChildAt("Mesh/NeonOff").GetComponent<MeshRenderer>();
            lightComp.rendererNeonOn = lightObj.GetChildAt("Mesh/NeonOn").GetComponent<MeshRenderer>();
            lightComp.saveColor = true;
            lightComp.soundOff = t_ceilingLight.soundOff;
            lightComp.soundOn = t_ceilingLight.soundOn;
            lightComp.useLightConePlanes = true;
            lightComp.useTurnOn = true;
            lightComp.stateAtStart = true;

            lightObj.GetChildAt("ActivateTrigger").tag = "ActivateTrigger";
            lightObj.GetChildAt("ActivateTrigger").layer = LayerMask.NameToLayer("Ignore Raycast");
            lightObj.GetChildAt("Mesh/Body/LightBase").tag = "RealtimeLight";
            // This thing is actually meant to use "IgnorePlayerCollision" layer, but Chris wanted me to add collision to lamps, so, fuck it.
            lightObj.GetChildAt("Mesh/Body/LightBase").layer = LayerMask.NameToLayer("Default");

            foreach (var flareCollider in lightObj.GetChildAt("Mesh/Body/LightBase").GetChilds()) flareCollider.layer = LayerMask.NameToLayer("AllExceptPlayer");

            lightObj.GetChildAt("Mesh/NeonOff").layer = LayerMask.NameToLayer("IgnoreLighting");
            lightObj.GetChildAt("Mesh/NeonOn").layer = LayerMask.NameToLayer("IgnoreLighting");
            lightObj.GetChildAt("LightConePlanes/LightConePlane").layer = LayerMask.NameToLayer("TransparentFX");
            lightObj.GetChildAt("LightConePlanes/LightConePlane (1)").layer = LayerMask.NameToLayer("TransparentFX");

            // Add ceiling lights animations.
            foreach (var state in t_ceilingLight.GetComponent<Animation>())
            {
                var animState = state.Cast<AnimationState>();
                lightComp.GetComponent<Animation>().AddClipFixed(animState.clip, animState.name);
            }
            #endregion

            powerCore.m_attachedLight = lightComp;

            // ---------- TAGS & LAYERS ----------

            contentObject.GetChild("PlayerCollider").layer = LayerMask.NameToLayer("PlayerCollisionOnly");
            contentObject.GetChild("PlayerCollider").GetComponent<BoxCollider>().material = t_powerSlot.gameObject.GetChild("PlayerCollider").GetComponent<BoxCollider>().material;

            contentObject.GetChild("InteractionOccluder_PowerCore_Back").tag = "InteractionOccluder";
            contentObject.GetChild("InteractionOccluder_PowerCore_Back").layer = LayerMask.NameToLayer("ActivableCheck");

            powerCore.m_audioSource.outputAudioMixerGroup = t_powerSlot.m_audioSource.outputAudioMixerGroup;
            powerCore.m_particles.GetComponent<ParticleSystemRenderer>().material = t_powerSlot.m_particles.GetComponent<ParticleSystemRenderer>().material;

            powerCore.m_particles.gameObject.layer = LayerMask.NameToLayer("TransparentFX");

            contentObject.GetChild("AdditionalInteractionCollider").tag = "InteractionCollider";
            contentObject.GetChild("AdditionalInteractionCollider").layer = LayerMask.NameToLayer("ActivableCheck");

            contentObject.GetChild("PerfectCollider").layer = LayerMask.NameToLayer("AllExceptPlayer");
            contentObject.GetChild("Collider_PlayerCollisionOnly").layer = LayerMask.NameToLayer("PlayerCollisionOnly");

            contentObject.GetChild("BasicInteractionCollider").tag = "InteractionCollider";
            contentObject.GetChild("BasicInteractionCollider").layer = LayerMask.NameToLayer("ActivableCheck");

            contentObject.GetChild("InteractionOccluder_PowerCore_Side1").tag = "InteractionOccluder";
            contentObject.GetChild("InteractionOccluder_PowerCore_Side1").layer = LayerMask.NameToLayer("ActivableCheck");
            contentObject.GetChild("InteractionOccluder_PowerCore_Side2").tag = "InteractionOccluder";
            contentObject.GetChild("InteractionOccluder_PowerCore_Side2").layer = LayerMask.NameToLayer("ActivableCheck");
            contentObject.GetChild("InteractionOccluder_PowerCore_Side3").tag = "InteractionOccluder";
            contentObject.GetChild("InteractionOccluder_PowerCore_Side3").layer = LayerMask.NameToLayer("ActivableCheck");
            contentObject.GetChild("InteractionOccluder_PowerCore_Side4").tag = "InteractionOccluder";
            contentObject.GetChild("InteractionOccluder_PowerCore_Side4").layer = LayerMask.NameToLayer("ActivableCheck");

            contentObject.SetActive(true);

            initialized = true;
        }

        void SetStateOnEditor(PowerSlotState state)
        {
            Material newMat = state == PowerSlotState.ACTIVATED ? MaterialUtils.newPropsv3Mat : MaterialUtils.newPropsv2Mat;
            mesh.sharedMaterial = newMat;

            editorPowerCore.SetActive(state == PowerSlotState.ACTIVATED);
        }

        void ConfigureEvents(PowerCoreController powerCore)
        {
            powerCore.m_onInsert = new UnityEvent();
            powerCore.m_onInsert.AddListener((UnityAction)ExecuteOnInsertEvents);
            powerCore.m_onInsert.AddListener((UnityAction)ExecuteOnBothEventsActivating);

            powerCore.m_onRemove = new UnityEvent();
            powerCore.m_onRemove.AddListener((UnityAction)ExecuteOnRemoveEvents);
            powerCore.m_onRemove.AddListener((UnityAction)ExecuteOnBothEventsDeactivating);
        }
        void ExecuteOnInsertEvents()
        {
            activePowerCore = powerCore.m_activePowerCore; // Make sure to cache this variable.
            activePowerCoreLE = activePowerCore.transform.parent.GetComponent<LE_Power_Core>();

            activePowerCoreLE.currentlyInsertedSlot = this;

            // The power core will inherit position/rotation/scale from this slot.
            activePowerCoreLE.transform.parent = contentObject.transform;

            // In case the user's Chris and he's MOVING the slot, force the core positions so it's not misplaced.
            activePowerCoreLE.contentObject.transform.position = powerCore.m_powerCoreHolder.transform.position;
            activePowerCoreLE.contentObject.transform.rotation = powerCore.m_powerCoreHolder.transform.rotation;

            eventExecuter.ExecuteEventsWithAndLogic((List<LE_Event>)properties["OnInsert"], "OnInsert", true);
        }
        void ExecuteOnRemoveEvents()
        {
            // Here, powerCore.m_activePowerCore is already null, use our cached variable.
            activePowerCoreLE.transform.parent = activePowerCoreLE.objectParent;

            activePowerCoreLE.currentlyInsertedSlot = null;

            activePowerCore = null;
            activePowerCoreLE = null;

            eventExecuter.ExecuteEventsWithAndLogic((List<LE_Event>)properties["OnRemove"], "OnRemove", false);
        }
        void ExecuteOnBothEventsActivating()
        {
            eventExecuter.ExecuteEventsWithAndLogic((List<LE_Event>)properties["OnBoth"], "OnBoth", true);
        }
        void ExecuteOnBothEventsDeactivating()
        {
            eventExecuter.ExecuteEventsWithAndLogic((List<LE_Event>)properties["OnBoth"], "OnBoth", false);
        }

        public override bool SetProperty(string name, object value)
        {
            if (name == "InitialState")
            {
                if (value is int)
                {
                    properties["InitialState"] = (PowerSlotState)value;
                    if (EditorController.Instance)
                        SetStateOnEditor((PowerSlotState)value);
                    return true;
                }
                else if (value is PowerSlotState)
                {
                    properties["InitialState"] = value;
                    if (EditorController.Instance)
                        SetStateOnEditor((PowerSlotState)value);
                    return true;
                }
            }
            if (GetAvailableEventsIDs().Contains(name))
            {
                if (value is List<LE_Event>)
                {
                    properties[name] = (List<LE_Event>)value;
                    return true;
                }
            }

            return base.SetProperty(name, value);
        }
    }
}
