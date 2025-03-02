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
    public class LE_Switch : LE_Object
    {
        void Awake()
        {
            properties = new Dictionary<string, object>
            {
                { "UsableOnce", false }
            };
        }

        void Start()
        {
            if (PlayModeController.Instance != null)
            {
                InitComponent();
            }
        }

        void InitComponent()
        {
            GameObject button = gameObject.GetChildWithName("Button");

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

            InterrupteurController controller = button.AddComponent<InterrupteurController>();

            controller.ActivateButtonSound = FindObjectOfType<InterrupteurController>().ActivateButtonSound;
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
            controller.iconActivationSound = FindObjectOfType<InterrupteurController>().iconActivationSound;
            controller.iconDeactivationSound = FindObjectOfType<InterrupteurController>().iconDeactivationSound;
            controller.IGCType = Controls.InGamePlayerKineType.MANUAL_BUTTON_INTERACTION;
            controller.interactableWhileDodge = true;
            controller.leverSound = FindObjectOfType<InterrupteurController>().leverSound;
            controller.localizedInteractionString = "Activate";
            controller.lockboxAnimTrigger = "IGC_Open";
            controller.m_audioSource = button.GetComponent<AudioSource>();
            controller.m_meshRenderer = button.GetChildWithName("ButtonMesh").GetComponent<MeshRenderer>();
            controller.m_meshTransform = button.GetChildWithName("ButtonMesh").transform;
            controller.offColor = InterrupteurController.ColorType.RED;
            controller.offMaterials = FindObjectOfType<InterrupteurController>().offMaterials;
            controller.onColor = InterrupteurController.ColorType.GREEN;
            controller.onMaterials = FindObjectOfType<InterrupteurController>().onMaterials;
            controller.redLightbandPlane = button.GetChildAt("ButtonMesh/Switch_LightBands_Bottom/Lightbands_Bottom_Red").GetComponent<MeshRenderer>();
            controller.redPlane = button.GetChildAt("ButtonMesh/RedButtonPlane").GetComponent<MeshRenderer>();
            controller.unusableColor = InterrupteurController.ColorType.BLACK;
            controller.unusableCoverAnimator = button.GetChildAt("ButtonMesh/UnusableCoverHolder").GetComponent<Animator>();
            controller.unusableMaterials = FindObjectOfType<InterrupteurController>().unusableMaterials;
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

            return false;
        }
    }
}
