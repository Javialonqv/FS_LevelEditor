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
        // FUCK CHARP COMPILER FOR NOT LETTING ME MODIFY A STRUCT WHILE ITERATING A LIST!!!
        // Now I need to put this as a class!!!
        class EditorLink
        {
            public LE_Event originalEvent;
            public LE_Switch originalSwitch;
            public LE_Object targetObj;
            public LineRenderer editorLinkRenderer;

            public EditorLink(LE_Event originalEvent,LE_Switch originalSwitch, LE_Event @event, LineRenderer editorLinkRenderer)
            {
                this.originalEvent = originalEvent;
                this.originalSwitch = originalSwitch;
                targetObj = EditorController.Instance.currentInstantiatedObjects.Find(x => string.Equals(x.objectFullNameWithID, @event.targetObjName,
                    StringComparison.OrdinalIgnoreCase));
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

        InterrupteurController controller;

        void Awake()
        {
            properties = new Dictionary<string, object>
            {
                { "UsableOnce", false },
                { "CanUseTaser", true },
                { "WhenActivatingEvents", new List<LE_Event>() },
                { "WhenDeactivatingEvents", new List<LE_Event>() },
                { "WhenInvertingEvents", new List<LE_Event>() }
            };

            CreateEditorLinksParent();
        }

        public override void OnInstantiated(LEScene scene)
        {
            // A few days ago I put this on the Awake() function, but if I did that, then the links weren't created
            // correctly at the start of the editor, already fixed...
            if (scene == LEScene.Editor)
            {
                CreateInEditorLinksToTargetObjects();
            }

            base.OnInstantiated(scene);
        }

        void Update()
        {
            if (editorLinksParent)
            {
                if (editorLinksParent.activeSelf && !EditorUIManager.IsCurrentUIContext(EditorUIContext.EVENTS_PANEL))
                {
                    UpdateEditorLinksPositions();
                }
            }
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
                CreateInEditorLinksToTargetObjects();
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

        public override void OnSelect()
        {
            base.OnSelect();
            ReValidateEditorLinks();
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

            string[] eventKeys = { "WhenActivatingEvents", "WhenDeactivatingEvents", "WhenInvertingEvents" };
            foreach (string eventKey in eventKeys)
            {
                foreach (var @event in (List<LE_Event>)properties[eventKey])
                {
                    // [IGNORE] Not make a link if the target obj name in the event isn't valid, or it'll throw an error.
                    // For optimization purposes, also don't create a link to an already linked object in another event,
                    // doesn't matter the event type (On Activated, On Deactivated...).
                    // ALSO, don't create editor links for the player related events.
                    // UPDATE: CREATE links even for INVALID objects, what if the user adds an object and the event becomes valid?
                    if (alreadyLinkedObjectsNames.Contains(@event.targetObjName) ||
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
                    editorLinks.Add(new EditorLink(@event, this, @event, linkRender));
                }
            }

            if (!dontDisableLinksParentWhenCreating) editorLinksParent.SetActive(false);
        }
        void UpdateEditorLinksPositions()
        {
            foreach (var editorLink in editorLinks)
            {
                if (editorLink.originalEvent.isValid)
                {
                    editorLink.editorLinkRenderer.gameObject.SetActive(true);
                    editorLink.UpdateLinkPositions();
                }
                else
                {
                    editorLink.editorLinkRenderer.gameObject.SetActive(false);
                }
            }
        }
        void ReValidateEditorLinks()
        {
            foreach (var editorLink in editorLinks)
            {
                // Check if the event is REALLY valid, the event may NOT be valid, but if the player already added an object that mades
                // it valid, then, check that when the switch is selected, to show the links.
                LE_Object targetObj = EditorController.Instance.currentInstantiatedObjects.FirstOrDefault(x => string.Equals(x.objectFullNameWithID,
                    editorLink.originalEvent.targetObjName, StringComparison.OrdinalIgnoreCase));
                bool isReallyValid = targetObj != null;

                // If the event wasn't valid before, that means the target obj didn't exist, which menas it was null, assign it.
                if (!editorLink.originalEvent.isValid)
                {
                    editorLink.targetObj = targetObj;
                }
                editorLink.originalEvent.isValid = isReallyValid;
            }
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
            ExecuteEvents((List<LE_Event>)properties["WhenActivatingEvents"]);
        }
        void ExecuteWhenDeactivatingEvents()
        {
            ExecuteEvents((List<LE_Event>)properties["WhenDeactivatingEvents"]);
        }
        void ExecuteWhenInvertingEvents()
        {
            ExecuteEvents((List<LE_Event>)properties["WhenInvertingEvents"]);
        }

        void ExecuteEvents(List<LE_Event> events)
        {
            foreach (LE_Event @event in events)
            {
                if (!@event.isValid)
                {
                    Logger.Warning($"Event of name \"{@event.eventName}\" is NOT valid! Target obj \"{@event.targetObjName}\" doesn't exists!");
                    continue;
                }

                if (@event.targetObjName == "Player")
                {
                    if (@event.enableOrDisableZeroG)
                    {
                        if (Controls.Instance.IsInZeroGravity()) Controls.Instance.DisableZeroGravityFromButton();
                        else Controls.Instance.EnableZeroGravityFromButton();
                    }
                    else if (@event.invertGravity)
                    {
                        Controls.Instance.InverseGravity();
                    }
                    continue;
                }
                LE_Object targetObj =
                    PlayModeController.Instance.currentInstantiatedObjects.Find(x => string.Equals(x.objectFullNameWithID, @event.targetObjName,
                    StringComparison.OrdinalIgnoreCase));

                switch (@event.spawn)
                {
                    case LE_Event.SpawnState.Spawn:
                        targetObj.TriggerAction("SetActive_True");
                        break;

                    case LE_Event.SpawnState.Despawn:
                        targetObj.TriggerAction("SetActive_False");
                        break;

                    case LE_Event.SpawnState.Toggle:
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
                else if (targetObj is LE_Cube)
                {
                    if (@event.respawnCube)
                    {
                        ((LE_Cube)targetObj).TriggerAction("RespawnCube");
                    }
                }
                else if (targetObj is LE_Laser)
                {
                    switch (@event.laserState)
                    {
                        case LE_Event.LaserState.Activate:
                            ((LE_Laser)targetObj).TriggerAction("Activate");
                            break;

                        case LE_Event.LaserState.Deactivate:
                            ((LE_Laser)targetObj).TriggerAction("Deactivate");
                            break;

                        case LE_Event.LaserState.Toggle_State:
                            ((LE_Laser)targetObj).TriggerAction("ToggleActivated");
                            break;
                    }
                }
                else if (targetObj is LE_Directional_Light || targetObj is LE_Point_Light)
                {
                    if (@event.changeLightColor)
                    {
                        targetObj.SetProperty("Color", Utilities.HexToColor(@event.newLightColor, false, null));
                    }
                }
                else if (targetObj is LE_Ceiling_Light)
                {
                    switch (@event.ceilingLightState)
                    {
                        case LE_Event.CeilingLightState.On:
                            ((LE_Ceiling_Light)targetObj).TriggerAction("Activate");
                            break;

                        case LE_Event.CeilingLightState.Off:
                            ((LE_Ceiling_Light)targetObj).TriggerAction("Deactivate");
                            break;

                        case LE_Event.CeilingLightState.ToggleOnOff:
                            ((LE_Ceiling_Light)targetObj).TriggerAction("ToggleActivated");
                            break;
                    }

                    if (@event.changeCeilingLightColor)
                    {
                        targetObj.SetProperty("Color", Utilities.HexToColor(@event.newCeilingLightColor, false, null));
                    }
                }
                else if (targetObj is LE_Health_Pack || targetObj is LE_Ammo_Pack)
                {
                    if (@event.changePackRespawnTime)
                    {
                        targetObj.SetProperty("RespawnTime", @event.packRespawnTime);
                    }

                    if (@event.spawnPackNow)
                    {
                        targetObj.TriggerAction("SpawnNow");
                    }
                }
                else if (targetObj is LE_Switch)
                {
                    switch (@event.switchState)
                    {
                        case LE_Event.SwitchState.Activated:
                            targetObj.TriggerAction("Activate");
                            if (@event.executeSwitchActions) targetObj.TriggerAction("ExecuteWhenActivatingActions");
                            break;

                        case LE_Event.SwitchState.Deactivated:
                            targetObj.TriggerAction("Deactivate");
                            if (@event.executeSwitchActions) targetObj.TriggerAction("ExecuteWhenDeactivatingActions");
                            break;

                        case LE_Event.SwitchState.Toggle:
                            targetObj.TriggerAction("ToggleActivated");
                            if (@event.executeSwitchActions) targetObj.TriggerAction("ExecuteWhenInvertingActions");
                            break;
                    }

                    switch (@event.switchUsableState)
                    {
                        case LE_Event.SwitchUsableState.Usable:
                            targetObj.TriggerAction("SetUsable");
                            break;

                        case LE_Event.SwitchUsableState.Unusable:
                            targetObj.TriggerAction("SetUnusable");
                            break;

                        case LE_Event.SwitchUsableState.Toggle:
                            targetObj.TriggerAction("ToggleUsable");
                            break;
                    }
                }
            }
        }
    }
}
