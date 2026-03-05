using FS_LevelEditor.Editor.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FS_LevelEditor.Playmode
{
    /// <summary>
    /// Manages AND logic for events where multiple input objects must be active
    /// for a target object to activate. Tracks input states and handles undo actions.
    /// </summary>
    public static class AndLogicManager
    {
        /// <summary>
        /// Tracks the state of each input for a specific target object and action.
        /// Key: (targetObjType, targetObjID, actionType)
        /// Value: Dictionary of (sourceObjType, sourceObjID, eventListName) -> isActive
        /// </summary>
        private static Dictionary<(LE_Object.ObjectType?, int, string), Dictionary<(LE_Object.ObjectType?, int, string), bool>> andInputStates = new();

        /// <summary>
        /// Tracks what action should be undone when AND condition is no longer met.
        /// Key: (targetObjType, targetObjID, actionType)
        /// Value: The undo action string
        /// </summary>
        private static Dictionary<(LE_Object.ObjectType?, int, string), string> undoActions = new();

        /// <summary>
        /// Tracks whether the AND condition was previously met for a target.
        /// </summary>
        private static Dictionary<(LE_Object.ObjectType?, int, string), bool> previousAndState = new();

        public static void Reset()
        {
            andInputStates.Clear();
            undoActions.Clear();
            previousAndState.Clear();
        }

        /// <summary>
        /// Registers an AND logic connection from a source object to a target object.
        /// </summary>
        public static void RegisterAndConnection(
            LE_Object sourceObj,
            string eventListName,
            LE_Object.ObjectType? targetObjType,
            int targetObjID,
            string actionType,
            string undoAction)
        {
            var targetKey = (targetObjType, targetObjID, actionType);
            var sourceKey = (sourceObj.objectType, sourceObj.objectID, eventListName);

            if (!andInputStates.ContainsKey(targetKey))
            {
                andInputStates[targetKey] = new Dictionary<(LE_Object.ObjectType?, int, string), bool>();
                undoActions[targetKey] = undoAction;
                previousAndState[targetKey] = false;
            }

            if (!andInputStates[targetKey].ContainsKey(sourceKey))
            {
                andInputStates[targetKey][sourceKey] = false;
            }
        }

        /// <summary>
        /// Sets the active state of an input source for AND logic.
        /// Returns whether all AND inputs are now active.
        /// </summary>
        public static bool SetInputState(
            LE_Object sourceObj,
            string eventListName,
            LE_Object.ObjectType? targetObjType,
            int targetObjID,
            string actionType,
            bool isActive)
        {
            var targetKey = (targetObjType, targetObjID, actionType);
            var sourceKey = (sourceObj.objectType, sourceObj.objectID, eventListName);

            if (!andInputStates.ContainsKey(targetKey))
            {
                return isActive; // No AND logic registered, just return the state
            }

            if (andInputStates[targetKey].ContainsKey(sourceKey))
            {
                andInputStates[targetKey][sourceKey] = isActive;
            }

            return AreAllInputsActive(targetKey);
        }

        /// <summary>
        /// Checks if all registered AND inputs for a target are active.
        /// </summary>
        private static bool AreAllInputsActive((LE_Object.ObjectType?, int, string) targetKey)
        {
            if (!andInputStates.ContainsKey(targetKey))
            {
                return true;
            }

            return andInputStates[targetKey].Values.All(state => state);
        }

        /// <summary>
        /// Checks if this event has AND logic and if the condition changed.
        /// Returns the action to execute (activate action, undo action, or null if no change).
        /// </summary>
        public static (bool shouldExecute, string action, bool isUndo) CheckAndCondition(
            LE_Object sourceObj,
            string eventListName,
            LE_Event leEvent,
            bool isActivating)
        {
            if (!leEvent.useAndLogic || leEvent.targetObjType == null)
            {
                // No AND logic, execute normally
                return (true, null, false);
            }

            string actionType = GetActionTypeForEvent(leEvent);
            if (string.IsNullOrEmpty(actionType))
            {
                return (true, null, false);
            }

            var targetKey = (leEvent.targetObjType, leEvent.targetObjID, actionType);

            // Update the input state
            bool allActive = SetInputState(sourceObj, eventListName, leEvent.targetObjType, leEvent.targetObjID, actionType, isActivating);

            // Check if the AND condition state changed
            bool wasAllActive = previousAndState.ContainsKey(targetKey) && previousAndState[targetKey];

            if (allActive && !wasAllActive)
            {
                // Condition now met - execute the forward action
                previousAndState[targetKey] = true;
                string forwardAction = GetForwardAction(actionType);
                return (true, forwardAction, false);
            }
            else if (!allActive && wasAllActive)
            {
                // Condition no longer met - execute undo action
                previousAndState[targetKey] = false;
                if (undoActions.TryGetValue(targetKey, out string undoAction))
                {
                    return (true, undoAction, true);
                }
            }

            // No state change or condition not met
            return (false, null, false);
        }

        /// <summary>
        /// Determines the action type string for an event based on its configuration.
        /// Only checks object-specific states if the target object type matches.
        /// </summary>
        public static string GetActionTypeForEvent(LE_Event leEvent)
        {
            var targetType = leEvent.targetObjType;

            // For doors - only check if target is a door
            if ((targetType == LE_Object.ObjectType.DOOR || targetType == LE_Object.ObjectType.DOOR_V2) 
                && leEvent.doorState != LE_Event.DoorState.Do_Nothing)
            {
                return "Door_" + leEvent.doorState.ToString();
            }
            // For saws - only check if target is a saw
            if (targetType == LE_Object.ObjectType.SAW && leEvent.sawState != LE_Event.SawState.Do_Nothing)
            {
                return "Saw_" + leEvent.sawState.ToString();
            }
            // For lasers - only check if target is a laser
            if (targetType == LE_Object.ObjectType.LASER && leEvent.laserState != LE_Event.LaserState.Do_Nothing)
            {
                return "Laser_" + leEvent.laserState.ToString();
            }
            // For bridges - only check if target is a bridge
            if (targetType == LE_Object.ObjectType.BRIDGE && leEvent.bridgeState != LE_Event.BridgeState.Do_Nothing)
            {
                return "Bridge_" + leEvent.bridgeState.ToString();
            }
            // For moving platforms - only check if target is a moving platform
            if (targetType == LE_Object.ObjectType.MOVING_PLATFORM && leEvent.movingPlatformState != LE_Event.MovingPlatformState.Do_Nothing)
            {
                return "MP_" + leEvent.movingPlatformState.ToString();
            }
            // For switches - only check if target is a switch
            if (targetType == LE_Object.ObjectType.SWITCH && leEvent.switchState != LE_Event.SwitchState.Do_Nothing)
            {
                return "Switch_" + leEvent.switchState.ToString();
            }
            // For flame traps - only check if target is a flame trap
            if (targetType == LE_Object.ObjectType.FLAME_TRAP && leEvent.flameTrapState != LE_Event.FlameTrapState.Do_Nothing)
            {
                return "FlameTrap_" + leEvent.flameTrapState.ToString();
            }
            // For ceiling lights - only check if target is a ceiling light
            if (targetType == LE_Object.ObjectType.CEILING_LIGHT && leEvent.ceilingLightState != LE_Event.CeilingLightState.Do_Nothing)
            {
                return "CeilingLight_" + leEvent.ceilingLightState.ToString();
            }
            // For mines - only check if target is a mine
            if (targetType == LE_Object.ObjectType.MINE && leEvent.mineState != LE_Event.MineState.Do_Nothing)
            {
                return "Mine_" + leEvent.mineState.ToString();
            }

            // Generic actions that apply to any object type:

            // For collider state - check BEFORE spawn since spawn defaults to Toggle
            if (leEvent.colliderState != LE_Event.ColliderState.Do_Nothing)
            {
                return "Collider_" + leEvent.colliderState.ToString();
            }
            // For spawn/despawn - Spawn and Despawn are explicit choices
            if (leEvent.spawn == LE_Event.SpawnState.Spawn || leEvent.spawn == LE_Event.SpawnState.Despawn)
            {
                return "Spawn_" + leEvent.spawn.ToString();
            }
            // Toggle spawn is used as fallback (it's the default, so use it if nothing else matched)
            if (leEvent.spawn == LE_Event.SpawnState.Toggle)
            {
                return "Spawn_Toggle";
            }

            return null;
        }

        /// <summary>
        /// Gets the undo action for a given action type.
        /// </summary>
        public static string GetUndoAction(string actionType)
        {
            if (actionType == null) return null;

            // Door actions
            if (actionType.StartsWith("Door_"))
            {
                if (actionType.Contains("Open")) return "Deactivate";
                if (actionType.Contains("Close")) return "Activate";
                if (actionType.Contains("Toggle")) return "InvertState";
            }
            // Saw actions
            if (actionType.StartsWith("Saw_"))
            {
                if (actionType.Contains("Activate")) return "Deactivate";
                if (actionType.Contains("Deactivate")) return "Activate";
                if (actionType.Contains("Toggle")) return "ToggleActivated";
            }
            // Laser actions
            if (actionType.StartsWith("Laser_"))
            {
                if (actionType.Contains("Activate")) return "Deactivate";
                if (actionType.Contains("Deactivate")) return "Activate";
                if (actionType.Contains("Toggle")) return "ToggleActivated";
            }
            // Bridge actions
            if (actionType.StartsWith("Bridge_"))
            {
                if (actionType.Contains("Extend")) return "Retract";
                if (actionType.Contains("Retract")) return "Deploy";
                if (actionType.Contains("Toggle")) return "Toggle";
            }
            // Moving platform actions
            if (actionType.StartsWith("MP_"))
            {
                if (actionType.Contains("Activate")) return "Deactivate";
                if (actionType.Contains("Deactivate")) return "Activate";
                if (actionType.Contains("Toggle")) return "InvertState";
            }
            // Switch actions
            if (actionType.StartsWith("Switch_"))
            {
                if (actionType.Contains("Activated")) return "Deactivate";
                if (actionType.Contains("Deactivated")) return "Activate";
                if (actionType.Contains("Toggle")) return "ToggleActivated";
            }
            // Flame trap actions
            if (actionType.StartsWith("FlameTrap_"))
            {
                if (actionType.Contains("Activate")) return "Deactivate";
                if (actionType.Contains("Deactivate")) return "Activate";
                if (actionType.Contains("Toggle")) return "ToggleActivated";
            }
            // Ceiling light actions
            if (actionType.StartsWith("CeilingLight_"))
            {
                if (actionType.Contains("On")) return "Deactivate";
                if (actionType.Contains("Off")) return "Activate";
                if (actionType.Contains("Toggle")) return "ToggleActivated";
            }
            // Mine actions
            if (actionType.StartsWith("Mine_"))
            {
                if (actionType.Contains("Activate")) return "Deactivate";
                if (actionType.Contains("Deactivate")) return "Activate";
                if (actionType.Contains("Toggle")) return "ToggleActivated";
            }
            // Spawn actions - check Toggle first since "Toggle" also contains "Spawn"
            if (actionType.StartsWith("Spawn_"))
            {
                if (actionType.Contains("Toggle")) return "ToggleActive";
                if (actionType.Contains("Spawn") && !actionType.Contains("Despawn")) return "SetActive_False";
                if (actionType.Contains("Despawn")) return "SetActive_True";
            }
            // Collider actions
            if (actionType.StartsWith("Collider_"))
            {
                if (actionType.Contains("Enable")) return "SetColliderState_False";
                if (actionType.Contains("Disable")) return "SetColliderState_True";
                if (actionType.Contains("Toggle")) return "ToggleColliderState";
            }

            return null;
        }

        /// <summary>
        /// Gets the forward action (TriggerAction string) for a given action type.
        /// </summary>
        public static string GetForwardAction(string actionType)
        {
            if (actionType == null) return null;

            // Door actions
            if (actionType.StartsWith("Door_"))
            {
                if (actionType.Contains("Open")) return "Activate";
                if (actionType.Contains("Close") && actionType.Contains("Fast")) return "CloseFast";
                if (actionType.Contains("Close")) return "Deactivate";
                if (actionType.Contains("Toggle")) return "InvertState";
            }
            // Saw actions
            if (actionType.StartsWith("Saw_"))
            {
                if (actionType.Contains("Activate")) return "Activate";
                if (actionType.Contains("Deactivate")) return "Deactivate";
                if (actionType.Contains("Toggle")) return "ToggleActivated";
            }
            // Laser actions
            if (actionType.StartsWith("Laser_"))
            {
                if (actionType.Contains("Activate")) return "Activate";
                if (actionType.Contains("Deactivate")) return "Deactivate";
                if (actionType.Contains("Toggle")) return "ToggleActivated";
            }
            // Bridge actions
            if (actionType.StartsWith("Bridge_"))
            {
                if (actionType.Contains("Extend")) return "Deploy";
                if (actionType.Contains("Retract")) return "Retract";
                if (actionType.Contains("Toggle")) return "Toggle";
            }
            // Moving platform actions
            if (actionType.StartsWith("MP_"))
            {
                if (actionType.Contains("Activate")) return "Activate";
                if (actionType.Contains("Deactivate")) return "Deactivate";
                if (actionType.Contains("Toggle")) return "InvertState";
            }
            // Switch actions
            if (actionType.StartsWith("Switch_"))
            {
                if (actionType.Contains("Activated")) return "Activate";
                if (actionType.Contains("Deactivated")) return "Deactivate";
                if (actionType.Contains("Toggle")) return "ToggleActivated";
            }
            // Flame trap actions
            if (actionType.StartsWith("FlameTrap_"))
            {
                if (actionType.Contains("Activate")) return "Activate";
                if (actionType.Contains("Deactivate")) return "Deactivate";
                if (actionType.Contains("Toggle")) return "ToggleActivated";
            }
            // Ceiling light actions
            if (actionType.StartsWith("CeilingLight_"))
            {
                if (actionType.Contains("On")) return "Activate";
                if (actionType.Contains("Off")) return "Deactivate";
                if (actionType.Contains("Toggle")) return "ToggleActivated";
            }
            // Mine actions
            if (actionType.StartsWith("Mine_"))
            {
                if (actionType.Contains("Activate")) return "Activate";
                if (actionType.Contains("Deactivate")) return "Deactivate";
                if (actionType.Contains("Toggle")) return "ToggleActivated";
            }
            // Spawn actions - check Toggle first since "Toggle" also contains "Spawn"
            if (actionType.StartsWith("Spawn_"))
            {
                if (actionType.Contains("Toggle")) return "ToggleActive";
                if (actionType.Contains("Spawn") && !actionType.Contains("Despawn")) return "SetActive_True";
                if (actionType.Contains("Despawn")) return "SetActive_False";
            }
            // Collider actions
            if (actionType.StartsWith("Collider_"))
            {
                if (actionType.Contains("Enable")) return "SetColliderState_True";
                if (actionType.Contains("Disable")) return "SetColliderState_False";
                if (actionType.Contains("Toggle")) return "ToggleColliderState";
            }

            return null;
        }

        /// <summary>
        /// Checks if any AND connections exist for a target object.
        /// </summary>
        public static bool HasAndConnections(LE_Object.ObjectType? targetObjType, int targetObjID)
        {
            return andInputStates.Keys.Any(k => k.Item1 == targetObjType && k.Item2 == targetObjID);
        }

        /// <summary>
        /// Gets the count of AND inputs registered for a specific target and action.
        /// </summary>
        public static int GetAndInputCount(LE_Object.ObjectType? targetObjType, int targetObjID, string actionType)
        {
            var targetKey = (targetObjType, targetObjID, actionType);
            if (andInputStates.ContainsKey(targetKey))
            {
                return andInputStates[targetKey].Count;
            }
            return 0;
        }
    }
}
