using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Services.Analytics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Switch : LE_Object
    {
        InterrupteurController controller;

        void Awake()
        {
            properties = new Dictionary<string, object>
            {
                { "UsableOnce", false },
                { "CanUseTaser", true },
                { "WhenInvertingEvents", new List<LE_Event>() },
                { "WhenActivatingEvents", new List<LE_Event>() },
                { "WhenDeactivatingEvents", new List<LE_Event>() }
            };
        }

        public override void InitComponent()
        {
            GameObject button = gameObject.GetChildWithName("Content");

            #region Setup tags and layers
            button.tag = "Interrupteur";
            button.GetChildWithName("ActivateTrigger").tag = "ActivateTrigger";
            button.GetChildWithName("ActivateTrigger").layer = LayerMask.NameToLayer("Ignore Raycast");

            button.GetChildWithName("AdditionalInteractionCollider_Sides").tag = "InteractionCollider";
            button.GetChildWithName("AdditionalInteractionCollider_Sides").layer = LayerMask.NameToLayer("ActivableCheck");
            button.GetChildWithName("AdditionalInteractionCollider_Radial").tag = "InteractionCollider";
            button.GetChildWithName("AdditionalInteractionCollider_Radial").layer = LayerMask.NameToLayer("ActivableCheck");
            button.GetChildWithName("AdditionalInteractionCollider_Vertical").tag = "InteractionCollider";
            button.GetChildWithName("AdditionalInteractionCollider_Vertical").layer = LayerMask.NameToLayer("ActivableCheck");

            button.GetChildWithName("InteractionOccluder").tag = "InteractionOccluder";
            button.GetChildWithName("InteractionOccluder").layer = LayerMask.NameToLayer("ActivableCheck");

            button.GetChildWithName("AutoAimCollider").tag = "AutoAim";
            button.GetChildWithName("AutoAimCollider").layer = LayerMask.NameToLayer("Water");
            #endregion

            controller = button.AddComponent<InterrupteurController>();

            controller.ActivateButtonSound = t_switch.ActivateButtonSound;
            controller.additionalInteractionGO = button.GetChildWithName("AdditionalInteractionCollider_Sides");
            controller.allowManualInteractAnim = true;
            controller.allowWhenSwitchingUIContext = true;
            controller.canBeUsed = true;
            controller.controlScript = Controls.Instance;
            controller.cyanLightbandPlane = button.GetChildAt("ButtonMesh/Switch_LightBands_Top/Lightbands_Top_Cyan").GetComponent<MeshRenderer>();
            controller.cyanPlane = button.GetChildAt("ButtonMesh/CyanPlaneButton").GetComponent<MeshRenderer>();
            controller.greenLightbandPlane = button.GetChildAt("ButtonMesh/Switch_LightBands_Top/Lightbands_Top_Green").GetComponent<MeshRenderer>();
            controller.greenPlane = button.GetChildAt("ButtonMesh/GreenPlaneButton").GetComponent<MeshRenderer>();
            controller.handleAnimator = button.GetChildAt("ButtonMesh/HandleHolder").GetComponent<Animator>();
            controller.iconActivationSound = t_switch.iconActivationSound;
            controller.iconDeactivationSound = t_switch.iconDeactivationSound;
            controller.IGCType = Controls.InGamePlayerKineType.MANUAL_BUTTON_INTERACTION;
            controller.interactableWhileDodge = true;
            controller.leverSound = t_switch.leverSound;
            controller.localizedInteractionString = "Activate";
            controller.lockboxAnimTrigger = "IGC_Open";
            controller.m_audioSource = button.GetComponent<AudioSource>();
            controller.m_meshRenderer = button.GetChildWithName("ButtonMesh").GetComponent<MeshRenderer>();
            controller.m_meshTransform = button.GetChildWithName("ButtonMesh").transform;
            controller.offColor = InterrupteurController.ColorType.RED;
            controller.offMaterials = t_switch.offMaterials;
            controller.onColor = InterrupteurController.ColorType.GREEN;
            controller.onMaterials = t_switch.onMaterials;
            controller.redLightbandPlane = button.GetChildAt("ButtonMesh/Switch_LightBands_Bottom/Lightbands_Bottom_Red").GetComponent<MeshRenderer>();
            controller.redPlane = button.GetChildAt("ButtonMesh/RedButtonPlane").GetComponent<MeshRenderer>();
            controller.unusableColor = InterrupteurController.ColorType.BLACK;
            controller.unusableCoverAnimator = button.GetChildAt("ButtonMesh/UnusableCoverHolder").GetComponent<Animator>();
            controller.unusableMaterials = t_switch.unusableMaterials;
            controller.objectsToActivate = new GameObject[0];
            controller.objectsToDestroy = new GameObject[0];
            controller.objectsToEnableOnly = new GameObject[0];
            controller.objectToActivate = gameObject;
            //controller.m_onActivate = new UnityEngine.Events.UnityEvent();
            //controller.m_onActivate_HandOnly = new UnityEngine.Events.UnityEvent();
            //controller.m_onActivate_TaserOnly = new UnityEngine.Events.UnityEvent();
            controller.messagesOnActivate = new Messenger[0];
            controller.dialogToActivate = new string[0];

            controller.usableOnce = (bool)GetProperty("UsableOnce");
            controller.ignoreLaser = !(bool)GetProperty("CanUseTaser");

            ConfigureEvents(controller);

            initialized = true;
        }

        public override List<string> GetAvailableEventsIDs()
        {
            return new List<string>
            {
                "WhenInvertingEvents",
                "WhenActivatingEvents",
                "WhenDeactivatingEvents"
            };
        }

        public override bool SetProperty(string name, object value)
        {
            if (name == "UsableOnce")
            {
                if (value is bool)
                {
                    properties["UsableOnce"] = (bool)value;
                    return true;
                }
                else
                {
                    Logger.Error($"Tried to set \"UsableOnce\" property with value of type \"{value.GetType().Name}\".");
                    return false;
                }
            }
            else if (name == "CanUseTaser")
            {
                if (value is bool)
                {
                    properties["CanUseTaser"] = (bool)value;
                    return true;
                }
                else
                {
                    Logger.Error($"Tried to set \"CanUseTaser\" property with value of type \"{value.GetType().Name}\".");
                    return false;
                }
            }
            else if (name == "WhenActivatingEvents")
            {
                if (value is List<LE_Event>)
                {
                    properties["WhenActivatingEvents"] = (List<LE_Event>)value;
                }
            }
            else if (name == "WhenDeactivatingEvents")
            {
                if (value is List<LE_Event>)
                {
                    properties["WhenDeactivatingEvents"] = (List<LE_Event>)value;
                }
            }
            else if (name == "WhenInvertingEvents")
            {
                if (value is List<LE_Event>)
                {
                    properties["WhenInvertingEvents"] = (List<LE_Event>)value;
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

            else if (actionName == "Activate")
            {
                UnityEvent onActivate = controller.m_onActivate;
                controller.m_onActivate = new UnityEvent();
                controller.ActivateSwitch();
                controller.m_onActivate = onActivate;
                return true;
            }
            else if (actionName == "Deactivate")
            {
                UnityEvent onDeactivate = controller.m_onDeactivate;
                controller.m_onDeactivate = new UnityEvent();
                controller.DeactivateSwitch();
                controller.m_onDeactivate = onDeactivate;
                return true;
            }
            else if (actionName == "ToggleActivated")
            {
                if (controller.activated)
                {
                    UnityEvent onDeactivate = controller.m_onDeactivate;
                    controller.m_onDeactivate = new UnityEvent();
                    controller.DeactivateSwitch();
                    controller.m_onDeactivate = onDeactivate;
                }
                else
                {
                    UnityEvent onActivate = controller.m_onActivate;
                    controller.m_onActivate = new UnityEvent();
                    controller.ActivateSwitch();
                    controller.m_onActivate = onActivate;
                }
                return true;
            }
            else if (actionName == "ExecuteWhenActivatingActions")
            {
                ExecuteWhenActivatingEvents();
            }
            else if (actionName == "ExecuteWhenDeactivatingActions")
            {
                ExecuteWhenDeactivatingEvents();
            }
            else if (actionName == "ExecuteWhenInvertingActions")
            {
                ExecuteWhenInvertingEvents();
            }

            else if (actionName == "SetUsable")
            {
                controller.IsNowUsable();
            }
            else if (actionName == "SetUnusable")
            {
                controller.IsNowUnusable();
            }
            else if (actionName == "ToggleUsable")
            {
                controller.InvertUsableState();
            }

            return base.TriggerAction(actionName);
        }

        void ConfigureEvents(InterrupteurController controller)
        {
            controller.m_onActivate = new UnityEngine.Events.UnityEvent();
            controller.m_onActivate.AddListener((UnityAction)ExecuteWhenActivatingEvents);
            controller.m_onActivate.AddListener((UnityAction)ExecuteWhenInvertingEvents);

            controller.m_onDeactivate = new UnityEngine.Events.UnityEvent();
            controller.m_onDeactivate.AddListener((UnityAction)ExecuteWhenDeactivatingEvents);
            controller.m_onDeactivate.AddListener((UnityAction)ExecuteWhenInvertingEvents);
        }

        void ExecuteWhenActivatingEvents()
        {
            eventExecuter.ExecuteEvents((List<LE_Event>)properties["WhenActivatingEvents"]);
        }
        void ExecuteWhenDeactivatingEvents()
        {
            eventExecuter.ExecuteEvents((List<LE_Event>)properties["WhenDeactivatingEvents"]);
        }
        void ExecuteWhenInvertingEvents()
        {
            eventExecuter.ExecuteEvents((List<LE_Event>)properties["WhenInvertingEvents"]);
        }
    }
}
