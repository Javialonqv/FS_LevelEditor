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
using FS_LevelEditor.Editor.UI;
using FS_LevelEditor.Editor;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Switch : LE_Object
    {
        public enum SwitchState
        {
            DEACTIVATED,
            ACTIVATED,
            UNUSABLE
        }
        InterrupteurController controller;
        MeshRenderer redPlane, greenPlane;

        void Awake()
        {
            properties = new Dictionary<string, object>
            {
                { "InitialState", SwitchState.DEACTIVATED },
                { "UsableOnce", false },
                { "CanUseTaser", true },
                { "WhenInvertingEvents", new List<LE_Event>() },
                { "WhenActivatingEvents", new List<LE_Event>() },
                { "WhenDeactivatingEvents", new List<LE_Event>() }
            };

            redPlane = gameObject.GetChildAt("Content/ButtonMesh/RedButtonPlane").GetComponent<MeshRenderer>();
            greenPlane = gameObject.GetChildAt("Content/ButtonMesh/GreenPlaneButton").GetComponent<MeshRenderer>();
        }

        public override void ObjectStart(LEScene scene)
        {
            SetMeshInEditor(GetProperty<SwitchState>("InitialState"));
        }

        public override void InitComponent()
        {
            GameObject button = gameObject.GetChild("Content");

            #region Setup tags and layers
            button.tag = "Interrupteur";
            button.GetChild("ActivateTrigger").tag = "ActivateTrigger";
            button.GetChild("ActivateTrigger").layer = LayerMask.NameToLayer("Ignore Raycast");

            button.GetChild("AdditionalInteractionCollider_Sides").tag = "InteractionCollider";
            button.GetChild("AdditionalInteractionCollider_Sides").layer = LayerMask.NameToLayer("ActivableCheck");
            button.GetChild("AdditionalInteractionCollider_Radial").tag = "InteractionCollider";
            button.GetChild("AdditionalInteractionCollider_Radial").layer = LayerMask.NameToLayer("ActivableCheck");
            button.GetChild("AdditionalInteractionCollider_Vertical").tag = "InteractionCollider";
            button.GetChild("AdditionalInteractionCollider_Vertical").layer = LayerMask.NameToLayer("ActivableCheck");

            button.GetChild("InteractionOccluder").tag = "InteractionOccluder";
            button.GetChild("InteractionOccluder").layer = LayerMask.NameToLayer("ActivableCheck");

            button.GetChild("AutoAimCollider").tag = "AutoAim";
            button.GetChild("AutoAimCollider").layer = LayerMask.NameToLayer("Water");
            #endregion

            controller = button.AddComponent<InterrupteurController>();

            controller.ActivateButtonSound = t_switch.ActivateButtonSound;
            controller.additionalInteractionGO = button.GetChild("AdditionalInteractionCollider_Sides");
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
            controller.m_audioSource.outputAudioMixerGroup = t_switch.m_audioSource.outputAudioMixerGroup;
            controller.m_meshRenderer = button.GetChild("ButtonMesh").GetComponent<MeshRenderer>();
            controller.m_meshTransform = button.GetChild("ButtonMesh").transform;
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

            // Do all of this BEFORE configuring the switch events.
            switch (GetProperty<SwitchState>("InitialState"))
            {
                case SwitchState.DEACTIVATED:
                    // Switch is already disabled at start by default.
                    break;

                case SwitchState.ACTIVATED:
                    controller.ActivateSwitch();
                    break;

                case SwitchState.UNUSABLE:
                    controller.IsNowUnusable();
                    break;
            }

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
            if (name == "InitialState")
            {
                if (value is int)
                {
                    properties["InitialState"] = (SwitchState)value;
                    if (EditorController.Instance) SetMeshInEditor((SwitchState)value);
                    return true;
                }
                else if (value is SwitchState)
                {
                    properties["InitialState"] = value;
                    if (EditorController.Instance) SetMeshInEditor((SwitchState)value);
                    return true;
                }
            }
            else if (name == "UsableOnce")
            {
                if (value is bool)
                {
                    properties["UsableOnce"] = (bool)value;
                    return true;
                }
            }
            else if (name == "CanUseTaser")
            {
                if (value is bool)
                {
                    properties["CanUseTaser"] = (bool)value;
                    return true;
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

            return base.SetProperty(name, value);
        }
        public override bool TriggerAction(string actionName)
        {
            if (actionName == "Activate")
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

        void SetMeshInEditor(SwitchState newState)
        {
            redPlane.enabled = newState == SwitchState.DEACTIVATED;
            greenPlane.enabled = newState == SwitchState.ACTIVATED;

            // Both will be disabled if newState is UNUSABLE, that should show the UNUSABLE state as expected:)
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
