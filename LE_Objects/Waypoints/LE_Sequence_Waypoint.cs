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
                    return true;
                }
                else if (value is int typeInt)
                {
                    properties["Color"] = (SequenceSwitchController.SwitchType)typeInt;
                    ((LE_Sequence)mainSupport.targetObject).UpdateLinkedScreen();
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
    }
}
