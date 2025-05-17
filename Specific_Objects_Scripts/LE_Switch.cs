using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Switch : LE_Object
    {
        struct EditorLink
        {
            LE_Switch originalSwitch;
            LE_Object targetObj;
            LineRenderer editorLinkRenderer;

            public EditorLink(LE_Switch originalSwitch, LE_Event @event, LineRenderer editorLinkRenderer)
            {
                this.originalSwitch = originalSwitch;
                targetObj = EditorController.Instance.currentInstantiatedObjects.Find(x => x.objectFullNameWithID == @event.targetObjName);
                this.editorLinkRenderer = editorLinkRenderer;
            }

            public void UpdateLinkPositions()
            {
                editorLinkRenderer.SetPosition(0, originalSwitch.transform.position);
                editorLinkRenderer.SetPosition(1, targetObj.transform.position);
            }
        }

        GameObject editorLinksParent;
        List<EditorLink> editorLinks = new();
        bool dontDisableLinksParentWhenCreating;

        void Awake()
        {
            properties = new Dictionary<string, object>
            {
                { "UsableOnce", false },
                { "CanUseTaser", true },
                { "OnActivatedEvents", new List<LE_Event>() },
                { "OnDeactivatedEvents", new List<LE_Event>() },
                { "OnChangeEvents", new List<LE_Event>() }
            };

            CreateEditorLinksParent();
        }

        void Start()
        {
            // A few days ago I put this on the Awake() function, but if I did that, then the links weren't created
            // correctly at the start of the editor, already fixed...
            if (EditorController.Instance != null)
            {
                CreateInEditorLinksToTargetObjects();
            }

            if (PlayModeController.Instance != null)
            {
                InitComponent();
            }
        }

        void Update()
        {
            if (editorLinksParent)
            {
                if (editorLinksParent.activeSelf && !EventsUIPageManager.Instance.isShowingPage)
                {
                    UpdateEditorLinksPositions();
                }
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
            controller.ignoreLaser = !(bool)GetProperty("CanUseTaser");

            ConfigureEvents(controller);
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
            else if (name == "OnActivatedEvents")
            {
                if (value is List<LE_Event>)
                {
                    properties["OnActivatedEvents"] = (List<LE_Event>)value;
                }
            }
            else if (name == "OnDeactivatedEvents")
            {
                if (value is List<LE_Event>)
                {
                    properties["OnDeactivatedEvents"] = (List<LE_Event>)value;
                }
            }
            else if (name == "OnChangeEvents")
            {
                if (value is List<LE_Event>)
                {
                    properties["OnChangeEvents"] = (List<LE_Event>)value;
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
                CreateInEditorLinksToTargetObjects();
                return true;
            }

            return base.TriggerAction(actionName);
        }

        public override void OnSelect()
        {
            base.OnSelect();
            editorLinksParent.SetActive(true);
            dontDisableLinksParentWhenCreating = true;
        }
        public override void OnDeselect(GameObject nextSelectedObj)
        {
            base.OnDeselect(nextSelectedObj);
            editorLinksParent.SetActive(false);
            dontDisableLinksParentWhenCreating = false;
        }
        void CreateEditorLinksParent()
        {
            editorLinksParent = new GameObject("EditorLinks");
            editorLinksParent.transform.parent = transform;
            editorLinksParent.transform.localPosition = Vector3.zero;
        }
        void CreateInEditorLinksToTargetObjects()
        {
            if (EditorController.Instance == null) return;

            if (editorLinksParent == null)
            {
                CreateEditorLinksParent();
            }
            else
            {
                editorLinksParent.DeleteAllChildren();
                editorLinks.Clear();
            }

            List<string> alreadyLinkedObjectsNames = new List<string>();

            string[] eventKeys = { "OnActivatedEvents", "OnDeactivatedEvents", "OnChangeEvents" };
            foreach (string eventKey in eventKeys)
            {
                foreach (var @event in (List<LE_Event>)properties[eventKey])
                {
                    // Not make a link if the target obj name in the event isn't valid, or it'll throw an error.
                    // For optimization purposes, also don't create a link to an already linked object in another event,
                    // doesn't matter the event type (On Activated, On Deactivated...).
                    // ALSO, don't create editor links for the player related events.
                    if (!@event.isValid || alreadyLinkedObjectsNames.Contains(@event.targetObjName) ||
                        @event.targetObjName == "Player") continue;

                    GameObject linkObj = new GameObject("Link");
                    linkObj.transform.parent = editorLinksParent.transform;
                    linkObj.transform.localPosition = Vector3.zero;

                    LineRenderer linkRender = linkObj.AddComponent<LineRenderer>();
                    linkRender.startWidth = 0.1f;
                    linkRender.endWidth = 0.1f;
                    linkRender.positionCount = 2;

                    linkRender.material = new Material(Shader.Find("Sprites/Default"));
                    linkRender.startColor = Color.white;
                    linkRender.endColor = Color.white;

                    alreadyLinkedObjectsNames.Add(@event.targetObjName);
                    editorLinks.Add(new EditorLink(this, @event, linkRender));
                }
            }

            if (!dontDisableLinksParentWhenCreating) editorLinksParent.SetActive(false);
        }
        void UpdateEditorLinksPositions()
        {
            foreach (var editorLink in editorLinks)
            {
                editorLink.UpdateLinkPositions();
            }
        }

        void ConfigureEvents(InterrupteurController controller)
        {
            controller.m_onActivate = new UnityEngine.Events.UnityEvent();
            controller.m_onActivate.AddListener((UnityAction)ExecuteOnActivatedEvents);
            controller.m_onActivate.AddListener((UnityAction)ExecuteOnChangeEvents);

            controller.m_onDeactivate = new UnityEngine.Events.UnityEvent();
            controller.m_onDeactivate.AddListener((UnityAction)ExecuteOnDeactivatedEvents);
            controller.m_onDeactivate.AddListener((UnityAction)ExecuteOnChangeEvents);
        }

        void ExecuteOnActivatedEvents()
        {
            ExecuteEvents((List<LE_Event>)properties["OnActivatedEvents"]);
        }
        void ExecuteOnDeactivatedEvents()
        {
            ExecuteEvents((List<LE_Event>)properties["OnDeactivatedEvents"]);
        }
        void ExecuteOnChangeEvents()
        {
            ExecuteEvents((List<LE_Event>)properties["OnChangeEvents"]);
        }

        void ExecuteEvents(List<LE_Event> events)
        {
            foreach (LE_Event @event in events)
            {
                if (@event.targetObjName == "Player")
                {
                    if (@event.enableOrDisableZeroG)
                    {
                        if (Controls.Instance.IsInZeroGravity()) Controls.Instance.DisableZeroGravityFromButton();
                        else Controls.Instance.EnableZeroGravityFromButton();
                    }
                    continue;
                }

                LE_Object targetObj =
                    PlayModeController.Instance.currentInstantiatedObjects.Find(x => x.objectFullNameWithID == @event.targetObjName);

                switch (@event.setActive)
                {
                    case LE_Event.SetActiveState.Enable:
                        targetObj.TriggerAction("SetActive_True");
                        break;

                    case LE_Event.SetActiveState.Disable:
                        targetObj.TriggerAction("SetActive_False");
                        break;

                    case LE_Event.SetActiveState.Toggle:
                        if (targetObj.gameObject.activeSelf)
                        {
                            targetObj.TriggerAction("SetActive_False");
                        }
                        else
                        {
                            targetObj.TriggerAction("SetActive_True");
                        }
                        break;
                }

                if (targetObj is LE_Saw)
                {
                    switch (@event.sawState)
                    {
                        case LE_Event.SawState.Activate:
                            ((LE_Saw)targetObj).TriggerAction("Activate");
                            break;

                        case LE_Event.SawState.Deactivate:
                            ((LE_Saw)targetObj).TriggerAction("Deactivate");
                            break;

                        case LE_Event.SawState.Toggle_State:
                            ((LE_Saw)targetObj).TriggerAction("ToggleActivated");
                            break;
                    }
                }
            }
        }
    }
}
