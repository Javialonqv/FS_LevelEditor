using FS_LevelEditor.Editor;
using FS_LevelEditor.WaypointSupports;
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
    public class LE_Sequence_Waypoint : LE_Waypoint
    {
        public MeshRenderer renderer;

        public override WaypointSupport GetMainSupport()
        {
            return transform.parent.parent.GetComponent<SequencerWaypointSupport>();
        }

        public static Dictionary<string, object> GetDefaultProperties()
        {
            return new Dictionary<string, object>()
            {
                { "Color", SequenceSwitchController.SwitchType.RED }
            };
        }

        void Awake()
        {
            renderer = contentObject.GetChildAt("SequenceSwitch/Mesh").GetComponent<MeshRenderer>();
        }

        public override void InitComponent()
        {
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
            blocScript.m_associatedSequencer = ((LE_Sequence)mainSupport.targetObject).sequence;
            blocScript.switchType = GetProperty<SequenceSwitchController.SwitchType>("Color");

            blocScript.m_audioSource.outputAudioMixerGroup = t_blocSwitchScript.m_audioSource.outputAudioMixerGroup;
            blocScript.m_animation.clip = t_blocSwitchScript.m_animation.clip;
            foreach (var clip in t_blocSwitchScript.m_animation)
            {
                AnimationState state = clip.Cast<AnimationState>();
                blocScript.m_animation.AddClip(state.clip, state.name);
            }

            initialized = true;
        }

        public override bool SetProperty(string name, object value)
        {
            if (name == "Color")
            {
                if (value is SequenceSwitchController.SwitchType type)
                {
                    properties["Color"] = type;
                    ((LE_Sequence)mainSupport.targetObject).UpdateLinkedScreen();
                    UpdateBlocColor();
                    return true;
                }
                else if (value is int typeInt)
                {
                    properties["Color"] = (SequenceSwitchController.SwitchType)typeInt;
                    ((LE_Sequence)mainSupport.targetObject).UpdateLinkedScreen();
                    UpdateBlocColor();
                    return true;
                }
            }

            return base.SetProperty(name, value);
        }

        public override void OnDelete()
        {
            // Execute the base FIRST, so the waypoint gets deleted of the spawnedWaypoints list and everything, and then UpdateLinkedScreen() ignores it.
            base.OnDelete();

            ((LE_Sequence)mainSupport.targetObject).UpdateLinkedScreen();
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
