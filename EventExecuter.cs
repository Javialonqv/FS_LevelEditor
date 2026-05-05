using Il2Cpp;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using FS_LevelEditor.Editor;
using FS_LevelEditor.Editor.UI;
using FS_LevelEditor.Playmode;
using System.Collections;

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
            if (originalEvent.targetObjType != null)
            {
                targetObj = EditorController.Instance.currentInstantiatedObjects.Find(x => x.objectType == originalEvent.targetObjType && x.objectID ==
                    originalEvent.targetObjID);
            }
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

        string coroutinesID => $"EventExecuter_{originalObject.objectFullNameWithID}";

        void Awake()
        {
            originalObject = GetComponent<LE_Object>();

            CreateEditorLinksParent();
        }
        public void OnInstantiated(LEScene scene)
        {
            UpdateLEEventsToTheNewSystem();
            if (scene == LEScene.Editor)
            {
                CreateInEditorLinksToTargetObjects();
            }
            else if (scene == LEScene.Playmode)
            {
                RegisterAndLogicConnections();
            }
        }

        /// <summary>
        /// Registers all AND logic connections for this object's events.
        /// Called during playmode initialization to set up AND tracking.
        /// </summary>
        private void RegisterAndLogicConnections()
        {
            foreach (string eventKey in originalObject.GetAvailableEventsIDs())
            {
                var eventsList = originalObject.properties[eventKey] as List<LE_Event>;
                if (eventsList == null) continue;

                foreach (var @event in eventsList)
                {
                    if (!@event.useAndLogic || @event.targetObjType == null || !@event.isValid) continue;

                    string actionType = AndLogicManager.GetActionTypeForEvent(@event);
                    if (string.IsNullOrEmpty(actionType)) continue;

                    string undoAction = AndLogicManager.GetUndoAction(actionType);
                    AndLogicManager.RegisterAndConnection(
                        originalObject,
                        eventKey,
                        @event.targetObjType,
                        @event.targetObjID,
                        actionType,
                        undoAction);
                }
            }
        }

        void Start()
        {
            ReValidateEditorLinks();
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

        // This method is used to update the LE_Event targetObjType and targetObjID properties in case it comes from a previous version that used targetObjName.
        void UpdateLEEventsToTheNewSystem()
        {
            foreach (string evenKey in originalObject.GetAvailableEventsIDs())
            {
                foreach (var @event in (List<LE_Event>)originalObject.properties[evenKey])
                {
                    bool isPlayer = string.Equals(@event.targetObjName, Loc.Get("Player"), StringComparison.OrdinalIgnoreCase);
                    bool isTaser = string.Equals(@event.targetObjName, Loc.Get("Taser"), StringComparison.OrdinalIgnoreCase);
                    bool isJetpack = string.Equals(@event.targetObjName, Loc.Get("Jetpack"), StringComparison.OrdinalIgnoreCase);
                    bool isObjective = @event.targetObjName.StartsWith("Obj_", StringComparison.OrdinalIgnoreCase);

                    if (!@event.isForPlayer && isPlayer) // If the targetObjName is "Player" but the BOOL is false, it's using the old system.
                    {
                        @event.isForPlayer = true;
                        @event.isForTaser = false;
                        @event.isForJetpack = false;
                        @event.isForObjective = false;
                        @event.targetObjType = null;
                        @event.targetObjID = 0;
                        @event.targetObjName = "";
                    }
                    else if (!@event.isForTaser && isTaser) // If the targetObjName is "Taser" but the BOOL is false, it's using the old system.
                    {
                        @event.isForTaser = true;
                        @event.isForPlayer = false;
                        @event.isForJetpack = false;
                        @event.isForObjective = false;
                        @event.targetObjType = null;
                        @event.targetObjID = 0;
                        @event.targetObjName = "";
                    }
                    else if (!@event.isForJetpack && isJetpack) // If the targetObjName is "Jetpack" but the BOOL is false, it's using the old system.
                    {
                        @event.isForJetpack = true;
                        @event.isForPlayer = false;
                        @event.isForTaser = false;
                        @event.isForObjective = false;
                        @event.targetObjType = null;
                        @event.targetObjID = 0;
                        @event.targetObjName = "";
                    }
                    else if (!@event.isForObjective && isObjective) // If the targetObjName starts with "Objective_" but the BOOL is false, it's using the old system.
                    {
                        @event.isForObjective = true;
                        @event.isForPlayer = false;
                        @event.isForTaser = false;
                        @event.isForJetpack = false;
                        @event.targetObjType = null;
                        @event.targetObjID = 0;
                        @event.objectiveName = @event.targetObjName.Substring(4);
                        @event.targetObjName = "";
                    }
                    else if (@event.targetObjType == null && @event.isValid && !string.IsNullOrEmpty(@event.targetObjName) && !isPlayer)
                    {
                        var objData = Utils.SplitTypeAndId(@event.targetObjName);
                        var objType = LE_Object.ConvertNameToObjectType(objData.type);

                        if (objType != null)
                        {
                            @event.targetObjType = objType;
                            @event.targetObjID = objData.id;
                            @event.targetObjName = ""; // Clear the name, since we are using the type and ID now.
                        }
                    }
                }
            }
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

            List<(LE_Object.ObjectType? objType, int objID)> alreadyLinkedObjects = new();

            foreach (string eventKey in originalObject.GetAvailableEventsIDs())
            {
                foreach (var @event in (List<LE_Event>)originalObject.properties[eventKey])
                {
                    // For optimization purposes, also don't create a link to an already linked object in another event,
                    // doesn't matter the event type (On Activated, On Deactivated...).
                    // ALSO, don't create editor links for the player related events.
                    // UPDATE: CREATE links even for INVALID objects, what if the user adds an object and the event becomes valid?
                    var objData = (@event.targetObjType, @event.targetObjID);
                    if (alreadyLinkedObjects.Contains(objData) || @event.isForPlayer || @event.isForTaser || @event.isForJetpack || @event.isForObjective || @event.isForWait || @event.isForGroup) continue;

                    GameObject linkObj = Instantiate(Core.LoadOtherObjectInBundle("EditorLine"), editorLinksParent.transform);
                    LineRenderer linkRender = linkObj.GetComponent<LineRenderer>();
                    linkRender.startColor = Color.cyan;
                    linkRender.endColor = Color.cyan;

                    alreadyLinkedObjects.Add(objData);
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
                LE_Object targetObj = EditorController.Instance.currentInstantiatedObjects.FirstOrDefault(x => x.objectType == editorLink.originalEvent.targetObjType
                    && x.objectID == editorLink.originalEvent.targetObjID && !x.isDeleted);
                bool isReallyValid = targetObj != null;

                editorLink.targetObj = targetObj;
                editorLink.originalEvent.isValid = isReallyValid;
            }
        }

        void Update()
        {
            if (editorLinksParent)
            {
                if (editorLinksParent.activeSelf && !EditorUIManager.IsCurrentUIContext(EditorUIContext.EVENTS_PANEL) &&
                    !EditorUIManager.IsCurrentUIContext(EditorUIContext.SELECTING_TARGET_OBJ))
                {
                    UpdateEditorLinksPositions();
                }
            }
        }

        void OnDestroy()
        {
            CoroutineUtils.StopAllCoroutines(coroutinesID);
        }

        /// <summary>
        /// Executes events without AND logic support (legacy method).
        /// </summary>
        public void ExecuteEvents(List<LE_Event> events)
        {
            CoroutineUtils.Start(ExecuteEventsInternal(events, null, true), coroutinesID);
        }

        /// <summary>
        /// Executes events with AND logic support.
        /// </summary>
        /// <param name="events">The list of events to execute.</param>
        /// <param name="eventListName">The name of the event list (e.g., "OnDrop", "OnRemove", "WhenActivatingEvents").</param>
        /// <param name="isActivating">True if this is an activating event (OnDrop, WhenActivating), false for deactivating (OnRemove, WhenDeactivating).</param>
        public void ExecuteEventsWithAndLogic(List<LE_Event> events, string eventListName, bool isActivating)
        {
            CoroutineUtils.Start(ExecuteEventsInternal(events, eventListName, isActivating), coroutinesID);
        }

        private IEnumerator ExecuteEventsInternal(List<LE_Event> events, string eventListName, bool isActivating)
        {
            foreach (LE_Event @event in events)
            {
                if (!@event.isValid)
                {
                    Logger.Warning($"Event of name \"{@event.eventName}\" is NOT valid! Type: {@event.targetObjType}. ID: {@event.targetObjID}." +
                        $"Text: \"{@event.targetObjName}\"");
                    continue;
                }

                // Handle AND logic if enabled and we have a valid event list name
                if (@event.useAndLogic && eventListName != null && @event.targetObjType != null)
                {
                    var (shouldExecute, action, isUndo) = AndLogicManager.CheckAndCondition(
                        originalObject,
                        eventListName,
                        @event,
                        isActivating);

                    if (!shouldExecute)
                    {
                        // AND condition not met or no state change, skip this event
                        continue;
                    }

                    // For AND logic events, we use the action returned by the manager
                    if (action != null)
                    {
                        // Execute the action (either the main action or undo action) on target object
                        LE_Object targetObj = PlayModeController.Instance.currentInstantiatedObjects.Find(
                            x => x.objectType == @event.targetObjType && x.objectID == @event.targetObjID);
                        if (targetObj != null)
                        {
                            if (@event.delay > 0 && !isUndo)
                            {
                                // Apply delay only for non-undo actions
                                CoroutineUtils.Start(ExecuteActionWithDelay(targetObj, action, @event.delay), coroutinesID);
                            }
                            else
                            {
                                targetObj.TriggerAction(action);
                            }
                        }
                        continue;
                    }
                }

                if (@event.isForWait) // Evaluate for wait events first.
                {
                    // Wait the designed time AND WAIT UNTIL IT FINISHES.
                    if (@event.waitTimeUnits == LE_Event.WaitTimeUnit.Seconds)
                        yield return new WaitForSeconds(@event.waitTime);
                    else
                        yield return new WaitForSeconds(@event.waitTime / 1000f);
                }
                else if (@event.delay > 0)
                {
                    // Execute with delay, but only this event, other events can continue executing without having to wait.
                    CoroutineUtils.Start(ExecuteEventWithDelay(@event, @event.delay), coroutinesID);
                }
                else
                {
                    // Execute the method directly, immediately.
                    ExecuteSingleEvent(@event);
                }
            }
        }

        private IEnumerator ExecuteActionWithDelay(LE_Object targetObj, string action, float delay)
        {
            yield return new WaitForSeconds(delay);
            targetObj.TriggerAction(action);
        }

        private IEnumerator ExecuteEventWithDelay(LE_Event @event, float delay)
        {
            yield return new WaitForSeconds(delay);
            ExecuteSingleEvent(@event);
        }

        private void ExecuteSingleEvent(LE_Event @event, bool onlyGlobalOptions = false)
        {
            if (@event.isForPlayer)
            {
                // Only one of these can be enabled, it's either Zero-G or Inverse Gravity.
                if (@event.enableOrDisableZeroG)
                {
                    if (Controls.Instance.IsInZeroGravity()) Controls.Instance.DisableZeroGravityFromButton();
                    else Controls.Instance.EnableZeroGravityFromButton();
                }
                else if (@event.invertGravity)
                {
                    PlayModeController.Instance.InvertPlayerGravity();
                }
                
                if(!@event.flashlightEnabled)
                    Controls.Instance.SetFlashlightNotAllowed();
                else
                    Controls.Instance.SetFlashlightAllowed();

                PlaymodeUpgrades.ApplyUpgrades(@event.upgrades);

                return;
            }
            if (@event.isForTaser)
            {
                // Handle giving/taking the taser
                switch (@event.taserState)
                {
                    case LE_Event.TaserState.Give:
                        if (!Controls.Instance.gunActivated)
                        {
                            Controls.Instance.ActivateWeapon();
                        }
                        break;

                    case LE_Event.TaserState.Take_Away:
                        if (Controls.Instance.gunActivated)
                        {
                            Controls.Instance.DeactivateWeaponInstant();
                        }
                        break;
                }

                // Handle ammo changes (only if gun is activated)
                if (Controls.Instance.gunActivated)
                {
                    LE_Gun.isCurrentlyInfinite = @event.infiniteTaser;
                    if (@event.infiniteTaser)
                    {
                        GunController.Instance.SetTutorialMode(true);
                        GunController.Instance.RequestLaserOnNow();
                    }
                    else
                    {
                        GunController.Instance.SetTutorialMode(false);

                        if (@event.changeAmmo)
                        {
                            GunController.Instance.SetAmmos(@event.newAmmo);
                            if (@event.newAmmo > 0)
                            {
                                GunController.Instance.RequestLaserOnNow();
                            }
                        }
                    }
                }
                return;
            }
            if (@event.isForJetpack)
            {
                switch (@event.jetpackState)
                {
                    case LE_Event.JetpackState.Give:
                        Controls.Instance.ActivateJetPack(true, false);
                        break;
                    case LE_Event.JetpackState.Take_Away:
                        Controls.Instance.BreakJetPack();
                        break;
                }
                return;
            }
            if (@event.isForObjective)
            {
                switch (@event.objectiveState)
                {
                    case LE_Event.ObjectiveState.Create:
                        PlayModeController.Instance.CreateObjective(@event.objectiveName);
                        break;

                    case LE_Event.ObjectiveState.Accomplish:
                        PlayModeController.Instance.AccomplishObjective(@event.objectiveName);
                        break;

                    case LE_Event.ObjectiveState.Fail:
                        PlayModeController.Instance.FailObjective(@event.objectiveName);
                        break;
                }
                return;
            }
            // Logic for wait events is on ExecuteEventsInternal().
            if (@event.isForGroup)
            {
                var objects = LE_Object.objectsPerGroup[@event.targetGroupID];

                bool allObjectsInGroupAreTheSame = @event.allObjectsInGroupAreTheSame;
                if (allObjectsInGroupAreTheSame)
                {
                    allObjectsInGroupAreTheSame = LE_Object.ObjectsAreOfTheSameType(objects); // Verify.
                    if (!allObjectsInGroupAreTheSame)
                    {
                        Logger.Warning($"The objects on the event \"{@event.eventName}\" are NOT of the same type {@event.sameObjectType}.");
                    }
                }

                foreach (var obj in objects)
                {
                    LE_Event newEvent = null;
                    if (allObjectsInGroupAreTheSame)
                        newEvent = new LE_Event(@event); // Copy all of the values from the main event, even object-specific ones.
                    else
                        newEvent = new LE_Event(); // Create a new one from scratch, assign the global values manually.

                    newEvent.targetObjType = obj.objectType;
                    newEvent.targetObjID = obj.objectID;
                    newEvent.isForGroup = false;

                    // Only setup global options values manually.
                    newEvent.spawn = @event.spawn;
                    newEvent.colliderState = @event.colliderState;
                    newEvent.moveState = @event.moveState;
                    newEvent.resetMovement = @event.resetMovement;

                    bool onlyExecuteGlobalOptions = !allObjectsInGroupAreTheSame;
                    ExecuteSingleEvent(newEvent, onlyExecuteGlobalOptions);
                }

                return;
            }

            LE_Object targetObj =
                PlayModeController.Instance.currentInstantiatedObjects.Find(x => x.objectType == @event.targetObjType && x.objectID == @event.targetObjID);

            #region Global Options
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
            switch (@event.colliderState)
            {
                case LE_Event.ColliderState.Enable:
                    targetObj.TriggerAction("SetColliderState_True");
                    break;

                case LE_Event.ColliderState.Disable:
                    targetObj.TriggerAction("SetColliderState_False");
                    break;

                case LE_Event.ColliderState.Toggle:
                    if (targetObj.currentCollisionState)
                    {
                        targetObj.TriggerAction("SetColliderState_False");
                    }
                    else
                    {
                        targetObj.TriggerAction("SetColliderState_True");
                    }
                    break;
            }
            if (targetObj.TryGetComponent<WaypointSupport>(out var waypointSupport))
            {
                if (@event.resetMovement)
                    waypointSupport.ResetMovement();

                switch (@event.moveState)
                {
                    case LE_Event.MoveState.Start_Moving:
                        waypointSupport.StartObjectMovement();
                        break;

                    case LE_Event.MoveState.Stop_Moving:
                        waypointSupport.StopObjectMovement();
                        break;

                    case LE_Event.MoveState.Start_Or_Stop_Moving:
                        if (waypointSupport.IsCurrentlyMoving)
                            waypointSupport.StopObjectMovement();
                        else
                            waypointSupport.StartObjectMovement();
                        break;
                }
            }
            #endregion

            if (onlyGlobalOptions)
                return;

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
                    if (@event.respawnCubeOnLastSwitch)
                    {
                        targetObj.TriggerAction("RespawnCube");
                    }
                    else
                    {
                        targetObj.TriggerAction("RespawnCubeFromStartPoint");
                    }
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
            else if (targetObj is LE_Mine)
            {
                if (@event.mineState == LE_Event.MineState.Activate)
                    targetObj.TriggerAction("Activate");
                else if (@event.mineState == LE_Event.MineState.Deactivate)
                    targetObj.TriggerAction("Deactivate");
                else if (@event.mineState == LE_Event.MineState.Toggle_State)
                    targetObj.TriggerAction("ToggleActivated");
            }
            else if (targetObj is LE_Directional_Light || targetObj is LE_Point_Light)
            {
                if (@event.changeLightColor)
                {
                    targetObj.SetProperty("Color", Utils.HexToColor(@event.newLightColor, false, null));
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
                    targetObj.SetProperty("Color", Utils.HexToColor(@event.newCeilingLightColor, false, null));
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

                switch (@event.canBeUsedState)
                {
                    case LE_Event.CanBeUsedState.Enable:
                        targetObj.TriggerAction("SetCanBeUsed_True");
                        break;

                    case LE_Event.CanBeUsedState.Disable:
                        targetObj.TriggerAction("SetCanBeUsed_False");
                        break;

                    case LE_Event.CanBeUsedState.Toggle:
                        targetObj.TriggerAction("ToggleCanBeUsed");
                        break;
                }
            }
            else if (targetObj is LE_Keypad)
            {
                switch (@event.canBeUsedState)
                {
                    case LE_Event.CanBeUsedState.Enable:
                        targetObj.TriggerAction("SetCanBeUsed_True");
                        break;

                    case LE_Event.CanBeUsedState.Disable:
                        targetObj.TriggerAction("SetCanBeUsed_False");
                        break;

                    case LE_Event.CanBeUsedState.Toggle:
                        targetObj.TriggerAction("ToggleCanBeUsed");
                        break;
                }
            }
            else if (targetObj is LE_Pressure_Plate)
            {
                switch (@event.pressurePlateUsableState)
                {
                    case LE_Event.PressurePlateUsableState.Usable:
                        targetObj.TriggerAction("SetUsable");
                        break;

                    case LE_Event.PressurePlateUsableState.Unusable:
                        targetObj.TriggerAction("SetUnusable");
                        break;

                    case LE_Event.PressurePlateUsableState.Toggle:
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
            else if (targetObj is LE_Screen || targetObj is LE_Small_Screen)
            {
                if (@event.changeScreenColorType)
                {
                    targetObj.SetProperty("ColorType", @event.screenColorType);
                }

                if (@event.changeScreenText)
                {
                    targetObj.SetProperty("Text", @event.screenNewText);
                }
            }
            else if (targetObj is LE_Door || targetObj is LE_Door_V2)
            {
                switch (@event.doorState)
                {
                    case LE_Event.DoorState.Close:
                        targetObj.TriggerAction("Deactivate");
                        break;
                    case LE_Event.DoorState.CloseFast:
                        targetObj.TriggerAction("CloseFast");
                        break;
                    case LE_Event.DoorState.Open:
                        targetObj.TriggerAction("Activate");
                        break;
                    case LE_Event.DoorState.Toggle:
                        targetObj.TriggerAction("InvertState");
                        break;
                }
            }
            else if (targetObj is LE_Moving_Platform)
            {
                switch (@event.movingPlatformState)
                {
                    case LE_Event.MovingPlatformState.Activate:
                        targetObj.TriggerAction("Activate");
                        break;
                    case LE_Event.MovingPlatformState.Deactivate:
                        targetObj.TriggerAction("Deactivate");
                        break;
                    case LE_Event.MovingPlatformState.Toggle:
                        targetObj.TriggerAction("InvertState");
                        break;
                }
            }
            else if (targetObj is LE_Bridge)
            {
                switch (@event.bridgeState)
                {
                    case LE_Event.BridgeState.Extend:
                        targetObj.TriggerAction("Deploy");
                        break;
                    case LE_Event.BridgeState.Retract:
                        targetObj.TriggerAction("Retract");
                        break;
                    case LE_Event.BridgeState.Toggle:
                        targetObj.TriggerAction("Toggle");
                        break;
                }
            }
            else if (targetObj is LE_Destructible_Wall)
            {
                if (@event.destructibleWallBreakNow)
                {
                    targetObj.TriggerAction("BreakNow");
                }
            }
            else if (targetObj is LE_Breakable_Window)
            {
                if (@event.fragileWindowBreakNow)
                {
                    targetObj.TriggerAction("BreakNow");
                }
            }
        }
    }
}
