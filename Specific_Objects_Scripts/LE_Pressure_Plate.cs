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
    public class LE_Pressure_Plate : LE_Object
    {
        public override void ObjectStart(LEScene scene)
        {
            if (scene == LEScene.Playmode)
            {
                // The on mesh is disabled by default, enable it when playmode starts.
                gameObject.GetChildAt("Content/MeshDynamic/MeshOnStatic").SetActive(true);
            }
        }

        public override void InitComponent()
        {
            GameObject content = gameObject.GetChildWithName("Content");

            BlocSwitchScript script = content.AddComponent<BlocSwitchScript>();
            script.boxCollider = content.GetComponent<BoxCollider>();
            script.objectsToActivate = new GameObject[0];
            script.m_dropOnSound = t_pressurePlate.m_dropOnSound;
            script.m_dropOffSound = t_pressurePlate.m_dropOffSound;
            script.m_audioSource = content.GetComponent<AudioSource>();
            script.m_activatedMaterials = t_pressurePlate.m_activatedMaterials;
            script.m_deactivatedMaterials = t_pressurePlate.m_deactivatedMaterials;
            script.canBeUsed = true;
            script.onDrop = new Messenger();
            script.onDropElements = new Messenger[0];
            script.onRemoveElements = new Messenger[0];
            script.m_meshRenderer = content.GetChildWithName("MeshDynamic").GetComponent<MeshRenderer>();
            script.m_animation = content.GetChildWithName("MeshDynamic").GetComponent<Animation>();
            script.meshOff = content.GetChildAt("MeshDynamic/MeshOffStatic").GetComponent<MeshRenderer>();
            script.meshOn = content.GetChildAt("MeshDynamic/MeshOnStatic").GetComponent<MeshRenderer>();
            script.meshDynamic = content.GetChildWithName("MeshDynamic").GetComponent<MeshRenderer>();
            script.onRemove = new Messenger();
            script.canBeCancelled = true;
            script.worksWithCubes = true;
            script.switchType = SequenceSwitchController.SwitchType.RED;
            script.onDropEvent = new UnityEngine.Events.UnityEvent();
            script.onPandoraDropped = new UnityEngine.Events.UnityEvent();
            script.onRemoveEvent = new UnityEngine.Events.UnityEvent();
            script.stayDownAfterOnce = false;
            script.usableEditorState = true;

            script.m_audioSource.outputAudioMixerGroup = t_pressurePlate.m_audioSource.outputAudioMixerGroup;

            script.m_animation.clip = t_pressurePlate.m_animation.clip;
            foreach (var clip in t_pressurePlate.m_animation)
            {
                AnimationState state = clip.Cast<AnimationState>();
                script.m_animation.AddClip(state.clip, state.name);
            }
            content.GetChildWithName("MeshDynamic").GetComponent<BoxCollider>().material =
            t_pressurePlate.gameObject.GetChildWithName("MeshDynamic").GetComponent<BoxCollider>().material;

            // ---------- SETUP TAGS & LAYERS ----------

            content.tag = "BlocSwitch";
            content.layer = LayerMask.NameToLayer("Ignore Raycast");

            content.GetChildWithName("MeshDynamic").layer = LayerMask.NameToLayer("AllExceptPlayer");
            content.GetChildAt("CompoundColliders").layer = LayerMask.NameToLayer("AllExceptPlayer");
            content.GetChildAt("CompoundColliders/Edge1").layer = LayerMask.NameToLayer("AllExceptPlayer");
            content.GetChildAt("CompoundColliders/Edge2").layer = LayerMask.NameToLayer("AllExceptPlayer");
            content.GetChildAt("CompoundColliders/Edge3").layer = LayerMask.NameToLayer("AllExceptPlayer");
            content.GetChildAt("CompoundColliders/Edge4").layer = LayerMask.NameToLayer("AllExceptPlayer");

            content.GetChildAt("PlayerCollisionOnly").layer = LayerMask.NameToLayer("PlayerCollisionOnly");

            initialized = true;
        }

       
    }
}
