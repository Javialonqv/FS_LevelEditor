using FS_LevelEditor.Editor;
using FS_LevelEditor.Editor.UI;
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
        MeshRenderer redPlane, greenPlane;
        void Awake()
        {
            redPlane = gameObject.GetChildAt("Content/MeshDynamic/MeshOffStatic").GetComponent<MeshRenderer>();
            greenPlane = gameObject.GetChildAt("Content/MeshDynamic/MeshOnStatic").GetComponent<MeshRenderer>();
        }

        public enum PlateState
        {
            DEACTIVATED,
            ACTIVATED
        }
        public static Dictionary<string, object> GetDefaultProperties()
        {
            return new Dictionary<string, object>
            {
                { "OnlyOnce", false },
                { "Unuseable", false },
                { "PressedState", PlateState.DEACTIVATED },
                { "OnDrop", new List<LE_Event>() },
                { "OnRemove", new List<LE_Event>() },
                { "OnBoth", new List<LE_Event>() }
            };
        }
        void SetMeshInEditor(PlateState newState)
        {
            redPlane.enabled = newState == PlateState.DEACTIVATED;
            greenPlane.enabled = newState == PlateState.ACTIVATED;
        }

        public override void ObjectStart(LEScene scene)
        {
            SetMeshInEditor(GetProperty<PlateState>("PressedState"));
            gameObject.GetChildAt("Content/MeshDynamic/MeshOnStatic").SetActive(true);

            base.ObjectStart(scene);
        }

        public override void InitComponent()
        {
            GameObject content = gameObject.GetChild("Content");

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
            script.m_meshRenderer = content.GetChild("MeshDynamic").GetComponent<MeshRenderer>();
            script.m_animation = content.GetChild("MeshDynamic").GetComponent<Animation>();
            script.meshOff = content.GetChildAt("MeshDynamic/MeshOffStatic").GetComponent<MeshRenderer>();
            script.meshOn = content.GetChildAt("MeshDynamic/MeshOnStatic").GetComponent<MeshRenderer>();
            script.meshDynamic = content.GetChild("MeshDynamic").GetComponent<MeshRenderer>();
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
            script.unavailble = GetProperty<bool>("Unuseable");

            script.m_audioSource.outputAudioMixerGroup = t_pressurePlate.m_audioSource.outputAudioMixerGroup;

            script.m_animation.clip = t_pressurePlate.m_animation.clip;

            switch(GetProperty<PlateState>("PressedState"))
            {
                case PlateState.DEACTIVATED:
                    break;
                case PlateState.ACTIVATED:  
                    //script.ForceActivateWithoutEvents();
                    break;
                
            }

            foreach (var clip in t_pressurePlate.m_animation)
            {
                AnimationState state = clip.Cast<AnimationState>();
                script.m_animation.AddClip(state.clip, state.name);
            }
            content.GetChild("MeshDynamic").GetComponent<BoxCollider>().material =
            t_pressurePlate.gameObject.GetChild("MeshDynamic").GetComponent<BoxCollider>().material;

            ConfigureEvents(script);

            // ---------- SETUP TAGS & LAYERS ----------

            content.tag = "BlocSwitch";
            content.layer = LayerMask.NameToLayer("Ignore Raycast");

            content.GetChild("MeshDynamic").layer = LayerMask.NameToLayer("AllExceptPlayer");
            content.GetChildAt("MeshDynamic/CompoundColliders").layer = LayerMask.NameToLayer("AllExceptPlayer");
            content.GetChildAt("MeshDynamic/CompoundColliders/Edge1").layer = LayerMask.NameToLayer("AllExceptPlayer");
            content.GetChildAt("MeshDynamic/CompoundColliders/Edge2").layer = LayerMask.NameToLayer("AllExceptPlayer");
            content.GetChildAt("MeshDynamic/CompoundColliders/Edge3").layer = LayerMask.NameToLayer("AllExceptPlayer");
            content.GetChildAt("MeshDynamic/CompoundColliders/Edge4").layer = LayerMask.NameToLayer("AllExceptPlayer");

            content.GetChildAt("MeshDynamic/PlayerCollisionOnly").layer = LayerMask.NameToLayer("PlayerCollisionOnly");

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
            }
            else if (name == "Unuseable")
            {
                if (value is bool)
                {
                    properties["Unuseable"] = (bool)value;
                    return true;
                }
            }
            else if (name == "PressedState")
            {
                if (value is int)
                {
                    properties["PressedState"] = (PlateState)value;
                    if (EditorController.Instance) SetMeshInEditor((PlateState)value);
                    return true;
                }
                else if (value is PlateState)
                {
                    properties["PressedState"] = value;
                    if (EditorController.Instance) SetMeshInEditor((PlateState)value);
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

        void ConfigureEvents(BlocSwitchScript script)
        {
            script.onDropEvent = new UnityEngine.Events.UnityEvent();
            script.onDropEvent.AddListener((UnityAction)ExecuteOnDropEvents);
            script.onDropEvent.AddListener((UnityAction)ExecuteOnBothEventsActivating);

            script.onRemoveEvent = new UnityEngine.Events.UnityEvent();
            script.onRemoveEvent.AddListener((UnityAction)ExecuteOnRemoveEvents);
            script.onRemoveEvent.AddListener((UnityAction)ExecuteOnBothEventsDeactivating);
        }

        void ExecuteOnDropEvents()
        {
            eventExecuter.ExecuteEventsWithAndLogic((List<LE_Event>)properties["OnDrop"], "OnDrop", true);
        }
        void ExecuteOnRemoveEvents()
        {
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
