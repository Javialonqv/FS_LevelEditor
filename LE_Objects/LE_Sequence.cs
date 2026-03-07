using FS_LevelEditor.Editor;
using FS_LevelEditor.SingleObjectLinks;
using Il2Cpp;
using Il2CppTMPro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using static Il2Cpp.Interop;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Sequence : LE_Object
    {
        public SequenceSwitchController sequence;
        public MeshRenderer renderer;

        public static Dictionary<string, object> GetDefaultProperties()
        {
            return new Dictionary<string, object>
            {
                { "Color", SequenceSwitchController.SwitchType.RED },
                { "waypoints", new List<WaypointData>() }, // In order to not fuck up any waypoints related code in LE, just call this "waypoints".
                { "OnSuccess", new List<LE_Event>() }
            };
        }

        void Awake()
        {
            renderer = contentObject.GetChildAt("SequenceSwitch/Mesh").GetComponent<MeshRenderer>();
        }

        public override void InitComponent()
        {
            contentObject.SetActive(false);

            LEDIndicator ledIndicator = contentObject.GetChildAt("SequenceSwitchController/LEDIndicatorPrefab").AddComponent<LEDIndicator>();
            ledIndicator.m_offMaterial = t_sequenceController.m_LEDIndicators[0].m_offMaterial;
            // On material is already set when InitializeLEDIndicators() is called.
            ledIndicator.m_renderer = ledIndicator.gameObject.GetChild("Mesh").GetComponent<MeshRenderer>();
            ledIndicator.m_textMesh = ledIndicator.gameObject.GetChild("LEDTextMesh").GetComponent<TextMeshPro>();
            // Fucking mesh, assigning it from the Unity proj doesn't work... do it from here.
            ledIndicator.m_renderer.GetComponent<MeshFilter>().mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");

            sequence = contentObject.GetChild("SequenceSwitchController").AddComponent<SequenceSwitchController>();
            sequence.invertDisplayOrder = false;
            sequence.useNumbers = false;
            sequence.requiredSequence = new Il2CppSystem.Collections.Generic.List<SequenceSwitchController.SwitchType>();
            sequence.requiredSequence.Add(GetProperty<SequenceSwitchController.SwitchType>("Color"));
            sequence.resetOnMistake = true;
            sequence.associatedLights = new Il2CppSystem.Collections.Generic.List<RealtimeCeilingLight>();
            sequence.m_screenLight = t_sequenceController.m_screenLight;
            sequence.onSequenceComplete = new UnityEngine.Events.UnityEvent();
            sequence.onSequenceReset = new UnityEngine.Events.UnityEvent();
            sequence.onStepSuccess = new UnityEngine.Events.UnityEvent();
            sequence.onStepMistake = new UnityEngine.Events.UnityEvent();
            sequence.audioSource = sequence.GetComponent<AudioSource>();
            sequence.mistakeSound = t_sequenceController.mistakeSound;
            sequence.resetSound = t_sequenceController.resetSound;
            sequence.stepSuccessSound = t_sequenceController.stepSuccessSound;
            sequence.sequenceSuccessSound = t_sequenceController.sequenceSuccessSound;
            if (otherObjThisIsLinkedTo)
            {
                LE_Sequence_Screen screen = otherObjThisIsLinkedTo.mainObject as LE_Sequence_Screen;
                sequence.screenObject = screen.screenObject;
                sequence.LEDHolder = screen.LEDHolder.transform;
            }
            else
            {
                sequence.screenObject = sequence.gameObject.GetChild("ScreenObject");
                sequence.LEDHolder = sequence.gameObject.GetChild("LEDHolder").transform;
            }
            sequence.LEDindicatorPrefab = ledIndicator.gameObject;
            sequence.indicatorsInitialized = true;
            sequence.m_LEDIndicators = new Il2CppSystem.Collections.Generic.List<LEDIndicator>();
            sequence.redOffMaterial = t_sequenceController.redOffMaterial;
            sequence.redOnMaterial = t_sequenceController.redOnMaterial;
            sequence.greenOffMaterial = t_sequenceController.greenOffMaterial;
            sequence.greenOnMaterial = t_sequenceController.greenOnMaterial;
            sequence.blueOffMaterial = t_sequenceController.blueOffMaterial;
            sequence.blueOnMaterial = t_sequenceController.blueOnMaterial;
            sequence.cyanOffMaterial = t_sequenceController.cyanOffMaterial;
            sequence.cyanOnMaterial = t_sequenceController.cyanOnMaterial;
            sequence.orangeOffMaterial = t_sequenceController.orangeOffMaterial;
            sequence.orangeOnMaterial = t_sequenceController.orangeOnMaterial;
            sequence.yellowOffMaterial = t_sequenceController.yellowOffMaterial;
            sequence.yellowOnMaterial = t_sequenceController.yellowOnMaterial;
            sequence.whiteOffMaterial = t_sequenceController.whiteOffMaterial;
            sequence.whiteOnMaterial = t_sequenceController.whiteOnMaterial;
            sequence.magentaOffMaterial = t_sequenceController.magentaOffMaterial;
            sequence.magentaOnMaterial = t_sequenceController.magentaOnMaterial;
            sequence.m_currentlyDownTypes = new Il2CppSystem.Collections.Generic.List<SequenceSwitchController.SwitchType>();

            sequence.audioSource.outputAudioMixerGroup = t_sequenceController.audioSource.outputAudioMixerGroup;

            BlocSwitchScript blocScript = contentObject.GetChild("SequenceSwitch").AddComponent<BlocSwitchScript>();
            blocScript.activated = false;
            blocScript.objectsToActivate = new GameObject[0];
            blocScript.m_dropOnSound = t_blocSwitchScript.m_dropOnSound;
            blocScript.m_dropOffSound = t_blocSwitchScript.m_dropOffSound;
            blocScript.m_audioSource = blocScript.GetComponent<AudioSource>();
            blocScript.eventsWereCalled = false;
            blocScript.m_activatedMaterials = t_blocSwitchScript.m_activatedMaterials;
            blocScript.m_deactivatedMaterials = t_blocSwitchScript.m_deactivatedMaterials;
            blocScript.canBeUsed = true;
            blocScript.currentDroppedBlocs = new Il2CppSystem.Collections.Generic.List<BlocScript>();
            blocScript.onDropElements = new Messenger[0];
            blocScript.onRemoveElements = new Messenger[0];
            blocScript.m_meshRenderer = blocScript.gameObject.GetChild("Mesh").GetComponent<MeshRenderer>();
            blocScript.m_animation = blocScript.gameObject.GetChild("Mesh").GetComponent<Animation>();
            blocScript.meshOff = null;
            blocScript.meshOn = null;
            blocScript.meshDynamic = null;
            blocScript.hasOnMaterials = false;
            blocScript.isIntroBlocSwitch = false;
            blocScript.unavailble = false;
            blocScript.forceDeactivateOnUnavailable = false;
            blocScript.canBeCancelled = true;
            blocScript.worksWithPlayer = true;
            blocScript.worksWithCubes = false;
            blocScript.useSwitchType = true;
            blocScript.switchType = SequenceSwitchController.SwitchType.RED;
            blocScript.onDrop = new Messenger();
            blocScript.onRemove = new Messenger();
            blocScript.onDropEvent = new UnityEngine.Events.UnityEvent();
            blocScript.onRemoveEvent = new UnityEngine.Events.UnityEvent();
            blocScript.onPandoraDropped = new UnityEngine.Events.UnityEvent();
            blocScript.m_associatedSequencer = sequence;
            blocScript.switchType = GetProperty<SequenceSwitchController.SwitchType>("Color");

            blocScript.m_audioSource.outputAudioMixerGroup = t_blocSwitchScript.m_audioSource.outputAudioMixerGroup;
            blocScript.m_animation.clip = t_blocSwitchScript.m_animation.clip;
            foreach (var clip in t_blocSwitchScript.m_animation)
            {
                AnimationState state = clip.Cast<AnimationState>();
                blocScript.m_animation.AddClip(state.clip, state.name);
            }

            ConfigureEvents();

            contentObject.SetActive(true);
        }
        public void FinishedSettingUpSteps()
        {
            sequence.indicatorsInitialized = false;
            sequence.InitializeLEDIndicators();

            foreach (var led in sequence.m_LEDIndicators)
            {
                led.gameObject.SetActive(true); // They're disabled by default for some reason.
            }
        }

        public override bool SetProperty(string name, object value)
        {
            if (name == "Color")
            {
                if (value is SequenceSwitchController.SwitchType type)
                {
                    properties["Color"] = type;
                    UpdateLinkedScreen();
                    UpdateBlocColor();
                    return true;
                }
                else if (value is int typeInt)
                {
                    properties["Color"] = (SequenceSwitchController.SwitchType)typeInt;
                    UpdateLinkedScreen();
                    UpdateBlocColor();
                    return true;
                }
            }
            else if (name == "waypoints")
            {
                if (value is List<WaypointData>)
                {
                    properties["waypoints"] = (List<WaypointData>)value;
                    return true;
                }
            }
            else if (GetAvailableEventsIDs().Contains(name))
            {
                if (value is List<LE_Event>)
                {
                    properties[name] = (List<LE_Event>)value;
                }
            }

            return base.SetProperty(name, value);
        }
        public override bool TriggerAction(string actionName)
        {
            if (actionName == "AddWaypoint")
            {
                customWaypointSupport.AddWaypoint();
                UpdateLinkedScreen();
                return true;
            }

            return base.TriggerAction(actionName);
        }

        void ConfigureEvents()
        {
            sequence.onSequenceComplete = new UnityEngine.Events.UnityEvent();
            sequence.onSequenceComplete.AddListener((UnityAction)ExecuteOnSuccessEvents);
        }
        void ExecuteOnSuccessEvents()
        {
            eventExecuter.ExecuteEventsWithAndLogic((List<LE_Event>)properties["OnSuccess"], "OnSuccess", true);
        }
        public override List<string> GetAvailableEventsIDs()
        {
            return new List<string>()
            {
                "OnSuccess"
            };
        }

        public void UpdateLinkedScreen()
        {
            if (!EditorController.Instance) return;

            if (otherObjThisIsLinkedTo)
            {
                ((LE_Sequence_Screen)otherObjThisIsLinkedTo.mainObject).UpdateScreen();
            }
        }
        public void UpdateBlocColor()
        {
            if (!EditorController.Instance) return;

            SequenceSwitchController.SwitchType color = GetProperty<SequenceSwitchController.SwitchType>("Color");
            var material = EditorController.Instance.GetMaterial($"NewProps_v1_Light_{color}", true);

            var sharedMaterials = renderer.sharedMaterials;
            sharedMaterials[1] = material;
            renderer.sharedMaterials = sharedMaterials;
        }

        // Skip the material which contains the color of the bloc.
        public override void SetObjectColor(LEObjectContext context)
        {
            foreach (var renderer in gameObject.TryGetComponents<MeshRenderer>())
            {
                // Skip waypoints
                if (canHaveWaypoints)
                {
                    if (waypointSupport && renderer.transform.IsChildOf(waypointSupport.waypointsParent)) continue;
                    if (customWaypointSupport && renderer.transform.IsChildOf(customWaypointSupport.waypointsParent)) continue;
                }

                foreach (var material in renderer.materials)
                {
                    if (!material.HasProperty("_Color")) continue;
                    if (material.name.Contains("NewProps_v1_Light")) continue;

                    Color toSet = LE_Object.GetObjectColorForObject(objectType.Value, context);
                    toSet.a = material.color.a;
                    material.color = toSet;
                }
            }
        }
    }
}
