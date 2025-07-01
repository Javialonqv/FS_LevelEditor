using FS_LevelEditor.Editor;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Pressure_Plate : LE_Object
    {
        void Awake()
        {
            properties = new Dictionary<string, object>
            {
                { "OnlyOnce", false },
                { "OnDrop", new List<LE_Event>() },
                { "OnRemove", new List<LE_Event>() },
                { "OnBoth", new List<LE_Event>() }
            };
        }

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
            //script.onDropEvent = new UnityEngine.Events.UnityEvent();
            //script.onPandoraDropped = new UnityEngine.Events.UnityEvent();
            //script.onRemoveEvent = new UnityEngine.Events.UnityEvent();
            script.stayDownAfterOnce = false;
            script.usableEditorState = true;
            script.onlyOnce = GetProperty<bool>("OnlyOnce");
            script.stayDownAfterOnce = GetProperty<bool>("OnlyOnce");

            script.m_audioSource.outputAudioMixerGroup = t_pressurePlate.m_audioSource.outputAudioMixerGroup;

            script.m_animation.clip = t_pressurePlate.m_animation.clip;
            foreach (var clip in t_pressurePlate.m_animation)
            {
                AnimationState state = clip.Cast<AnimationState>();
                script.m_animation.AddClip(state.clip, state.name);
            }
            content.GetChildWithName("MeshDynamic").GetComponent<BoxCollider>().material =
            t_pressurePlate.gameObject.GetChildWithName("MeshDynamic").GetComponent<BoxCollider>().material;

            ConfigureEvents(script);

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

        public override bool SetProperty(string name, object value)
        {
            if (name == "OnlyOnce")
            {
                if (value is bool)
                {
                    properties["OnlyOnce"] = (bool)value;
                    return true;
                }
                else
                {
                    Logger.Error($"Tried to set \"OnlyOnce\" property with value of type \"{value.GetType().Name}\".");
                    return false;
                }
            }
            else if (GetAvailableEventsIDs().Contains(name))
            {
                if (value is List<LE_Event>)
                {
                    properties[name] = (List<LE_Event>)value;
                }
            }

            return false;
        }
        public override bool TriggerAction(string actionName)
        {
            if (actionName == "ManageEvents")
            {
                EventsUIPageManager.Instance.ShowEventsPage(this);
                return true;
            }
            else if (actionName == "OnEventsTabClose")
            {
                eventExecuter.CreateInEditorLinksToTargetObjects();
                return true;
            }

            return base.TriggerAction(actionName);
        }

        void ConfigureEvents(BlocSwitchScript script)
        {
            script.onDropEvent = new UnityEngine.Events.UnityEvent();
            script.onDropEvent.AddListener((UnityAction)ExecuteOnDropEvents);
            script.onDropEvent.AddListener((UnityAction)ExecuteOnBothEvents);

            script.onRemoveEvent = new UnityEngine.Events.UnityEvent();
            script.onRemoveEvent.AddListener((UnityAction)ExecuteOnRemoveEvents);
            script.onRemoveEvent.AddListener((UnityAction)ExecuteOnBothEvents);
        }

        void ExecuteOnDropEvents()
        {
            eventExecuter.ExecuteEvents((List<LE_Event>)properties["OnDrop"]);
        }
        void ExecuteOnRemoveEvents()
        {
            eventExecuter.ExecuteEvents((List<LE_Event>)properties["OnRemove"]);
        }
        void ExecuteOnBothEvents()
        {
            eventExecuter.ExecuteEvents((List<LE_Event>)properties["OnBoth"]);
        }

        public override List<string> GetAvailableEventsIDs()
        {
            return new List<string>()
            {
                "OnDrop",
                "OnRemove",
                "OnBoth"
            };
        }
    }
}
