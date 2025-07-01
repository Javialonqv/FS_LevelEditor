using Il2Cpp;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using FS_LevelEditor.Editor;

namespace FS_LevelEditor
{
    // FUCK CHARP COMPILER FOR NOT LETTING ME MODIFY A STRUCT WHILE ITERATING A LIST!!!
    // Now I need to put this as a class!!!
    public class EditorLink
    {
        public LE_Event originalEvent;
        public LE_Object originalObject;
        public LE_Object targetObj;
        public LineRenderer editorLinkRenderer;

        public EditorLink(LE_Event originalEvent, LE_Object originalObject, LE_Event @event, LineRenderer editorLinkRenderer)
        {
            this.originalEvent = originalEvent;
            this.originalObject = originalObject;
            targetObj = EditorController.Instance.currentInstantiatedObjects.Find(x => string.Equals(x.objectFullNameWithID, @event.targetObjName,
                StringComparison.OrdinalIgnoreCase));
            this.editorLinkRenderer = editorLinkRenderer;
        }

        public void UpdateLinkPositions()
        {
            editorLinkRenderer.SetPosition(0, originalObject.transform.position);
            editorLinkRenderer.SetPosition(1, targetObj.transform.position);
        }
    }

    [RegisterTypeInIl2Cpp]
    public class EventExecuter : MonoBehaviour
    {
        LE_Object originalObject;

        GameObject editorLinksParent;
        List<EditorLink> editorLinks = new();
        bool dontDisableLinksParentWhenCreating;

        void Awake()
        {
            originalObject = GetComponent<LE_Object>();

            CreateEditorLinksParent();
        }
        public void OnInstantiated(LEScene scene)
        {
            if (scene == LEScene.Editor)
            {
                CreateInEditorLinksToTargetObjects();
            }
        }
        public void OnSelect()
        {
            ReValidateEditorLinks();
            editorLinksParent.SetActive(true);
            dontDisableLinksParentWhenCreating = true;
        }
        public void OnDeselect()
        {
            editorLinksParent.SetActive(false);
            dontDisableLinksParentWhenCreating = false;
        }

        public void CreateEditorLinksParent()
        {
            editorLinksParent = new GameObject("EditorLinks");
            editorLinksParent.transform.parent = transform;
            editorLinksParent.transform.localPosition = Vector3.zero;
        }

        public void CreateInEditorLinksToTargetObjects()
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

            foreach (string eventKey in originalObject.GetAvailableEventsIDs())
            {
                foreach (var @event in (List<LE_Event>)originalObject.properties[eventKey])
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
                    editorLinks.Add(new EditorLink(@event, originalObject, @event, linkRender));
                }
            }

            if (!dontDisableLinksParentWhenCreating) editorLinksParent.SetActive(false);
        }
        public void UpdateEditorLinksPositions()
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
        public void ReValidateEditorLinks()
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

        public void ExecuteEvents(List<LE_Event> events)
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
                            targetObj.TriggerAction("Activate");
                            break;

                        case LE_Event.SawState.Deactivate:
                            targetObj.TriggerAction("Deactivate");
                            break;

                        case LE_Event.SawState.Toggle_State:
                            targetObj.TriggerAction("ToggleActivated");
                            break;
                    }
                }
                else if (targetObj is LE_Cube)
                {
                    if (@event.respawnCube)
                    {
                        targetObj.TriggerAction("RespawnCube");
                    }
                }
                else if (targetObj is LE_Laser)
                {
                    switch (@event.laserState)
                    {
                        case LE_Event.LaserState.Activate:
                            targetObj.TriggerAction("Activate");
                            break;

                        case LE_Event.LaserState.Deactivate:
                            targetObj.TriggerAction("Deactivate");
                            break;

                        case LE_Event.LaserState.Toggle_State:
                            targetObj.TriggerAction("ToggleActivated");
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
                            targetObj.TriggerAction("Activate");
                            break;

                        case LE_Event.CeilingLightState.Off:
                            targetObj.TriggerAction("Deactivate");
                            break;

                        case LE_Event.CeilingLightState.ToggleOnOff:
                            targetObj.TriggerAction("ToggleActivated");
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
                else if (targetObj is LE_Flame_Trap)
                {
                    switch (@event.flameTrapState)
                    {
                        case LE_Event.FlameTrapState.Activate:
                            targetObj.TriggerAction("Activate");
                            break;

                        case LE_Event.FlameTrapState.Deactivate:
                            targetObj.TriggerAction("Deactivate");
                            break;

                        case LE_Event.FlameTrapState.Toggle_State:
                            targetObj.TriggerAction("ToggleActivated");
                            break;
                    }
                }
            }
        }
    }
}
