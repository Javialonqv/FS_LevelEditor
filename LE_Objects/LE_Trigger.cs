using FS_LevelEditor.Playmode;
using FS_LevelEditor.WaypointSupports;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;

namespace FS_LevelEditor
{
    public enum TriggerMode
    {
        ONCE = 0,        // Default, can only be triggered once by player
        MULTIPLE = 1,    // Can be triggered multiple times by player
        CUBE_ONLY = 2,    // Only triggered by cube
        LOOK = 3
    }

    public enum LookMode
    {
        ONCE = 0,
        MULTIPLE = 1
    }


    public class LE_Trigger : LE_Object
    {
        private bool hasBeenTriggered = false; // Track if trigger has been activated (for Once mode)
        private bool hasExitBeenTriggered = false;
        private HashSet<GameObject> cubesInTrigger = new HashSet<GameObject>(); // Track cubes currently in trigger

        TriggerScript triggerScript;
        BoxCollider trigger;
        public bool skipTriggerWithPlayerThisFrame = false;
        bool wasLookingAtArea = false;
        float lookHeldTime = 0f;
        bool lookEnterUsed = false;
        bool lookExitUsed = false;

        public override string[] EventsIDs =>
        [
            "OnEnter",
            "OnExit"
        ];

        public static Dictionary<string, object> GetDefaultProperties()
        {
            return new Dictionary<string, object>
            {
                { "TriggerMode", TriggerMode.ONCE },
                { "LookMode", LookMode.ONCE },
                { "LookDelay", 0f },
                { "waypoints", new List<WaypointData>() },
                { "OnEnter", new List<LE_Event>() },
                { "OnExit", new List<LE_Event>() },
                { "ExecIfInside", true },
                { "ExecIfDespawned", false },
            };
        }

        public override void OnInstantiated(LEScene scene)
        {
            if (scene == LEScene.Playmode)
            {
                gameObject.GetChildAt("Content/Mesh").SetActive(false);
            }

            base.OnInstantiated(scene);
        }

        public override void InitComponent()
        {
            GameObject triggerObj = gameObject.GetChildAt("Content/LE_Trigger");
            triggerObj.tag = "Trigger";
            triggerObj.layer = LayerMask.NameToLayer("Ignore Raycast");

            // Add our custom cube detection component for cube-only mode
            CubeTriggerDetector cubeDetector = triggerObj.AddComponent<CubeTriggerDetector>();
            cubeDetector.parentTrigger = this;

            triggerScript = triggerObj.AddComponent<TriggerScript>();
            triggerScript.onEnter = new UnityEvent();
            triggerScript.onEnter.AddListener((UnityAction)ExecuteOnEnterEvents);
            triggerScript.onExit = new UnityEngine.Events.UnityEvent();
            triggerScript.onExit.AddListener((UnityAction)ExecuteOnExitEvents);
            triggerScript.onDestroy = new UnityEvent();
            triggerScript.BlocSwitchs = new GameObject[0];
            triggerScript.objectsToActivate = new GameObject[0];
            triggerScript.objectsToDeactivate = new GameObject[0];
            triggerScript.objectsToEnableOnly = new GameObject[0];
            triggerScript.objectsToDestroy = new GameObject[0];
            triggerScript.doorsToClose = new GameObject[0];
            triggerScript.lasersToEnable = new Laser_H_Controller[0];
            triggerScript.lasersToDisable = new Laser_H_Controller[0];
            triggerScript.dialogToActivate = new string[0];
            triggerScript.m_messages = new Messenger[0];
            triggerScript.keepActivated = true;

            this.trigger = gameObject.GetChildAt("Content/LE_Trigger").GetComponent<BoxCollider>();

            initialized = true;
        }

        void OnEnable()
        {
            if (!initialized || !PlayModeController.Instance)
                return;

            // If the player is inside the trigger when enabled, and ExecIfInside is false, prevent the trigger from... triggering.
            if (!GetProperty<bool>("ExecIfInside"))
            {
                var colliders = Physics.OverlapBox(
                    trigger.bounds.center, // We can use bounds.center since it's world-space.
                    Vector3.Scale(trigger.size, trigger.transform.lossyScale) / 2, // Don't use bounds.size cause that one it's adjusted to obj rotation and it's not what we want.
                    transform.rotation
                );
                foreach (var collider in colliders)
                {
                    if (collider.TryGetComponent<Controls>(out var player))
                    {
                        skipTriggerWithPlayerThisFrame = true;
                        // Then, Controls.OnTriggerEnter is called.
                        break;
                    }
                }
            }
        }
        void OnDisable()
        {
            if (!initialized || !PlayModeController.Instance)
                return;

            var colliders = Physics.OverlapBox(
                trigger.bounds.center, // We can use bounds.center since it's world-space.
                Vector3.Scale(trigger.size, trigger.transform.lossyScale) / 2, // Don't use bounds.size cause that one it's adjusted to obj rotation and it's not what we want.
                transform.rotation
            );
            foreach (var collider in colliders)
            {
                if (collider.TryGetComponent<Controls>(out var player))
                {
                    // OnTriggerExit won't be called automatically, just reset canBeReactivated to true.
                    Utils.InvokeAfterOneFrame(() =>
                    {
                        AccessTools.Field(triggerScript.GetType(), "canBeReactivated").SetValue(triggerScript, true);
                    });
                    // And only call OnTriggerExit (to execute events) if the prop is true.
                    if (GetProperty<bool>("ExecIfDespawned"))
                    {
                        AccessTools.Method(player.GetType(), "OnTriggerExit", [typeof(Collider)])
                            ?.Invoke(player, [trigger]);
                    }
                    break;
                }
            }
        }

        public override bool SetProperty(string name, object value)
        {
            if (name == "TriggerMode")
            {
                if (value is int)
                {
                    properties[name] = (TriggerMode)value;

                    // No need to reconfigure collision detection since we handle it in the detector component
                    return true;
                }
                else if (value is TriggerMode)
                {
                    properties[name] = value;

                    // No need to reconfigure collision detection since we handle it in the detector component
                    return true;
                }
            }
            else if (name == "LookMode")
            {
                if (value is int)
                {
                    properties[name] = (LookMode)value;
                    return true;
                }
                else if (value is LookMode)
                {
                    properties[name] = value;
                    return true;
                }
            }
            else if (name == "LookDelay")
            {
                if (value is string)
                {
                    if (Utils.TryParseFloat((string)value, out float result))
                    {
                        properties[name] = Mathf.Max(0f, result);
                        return true;
                    }
                }
                else if (value is float)
                {
                    properties[name] = Mathf.Max(0f, (float)value);
                    return true;
                }
            }
            else if (name == "waypoints")
            {
                if (value is List<WaypointData>)
                {
                    properties[name] = (List<WaypointData>)value;
                    return true;
                }
            }
            else if (name == "ExecIfInside")
            {
                if (value is bool)
                {
                    properties["ExecIfInside"] = (bool)value;
                    return true;
                }
            }
            else if (name == "ExecIfDespawned")
            {
                if (value is bool)
                {
                    properties["ExecIfDespawned"] = (bool)value;
                    return true;
                }
            }
            else if (GetAvailableEventsIDs().Contains(name))
            {
                if (value is List<LE_Event>)
                {
                    properties[name] = (List<LE_Event>)value;
                    return true;
                }
            }

            return base.SetProperty(name, value);
        }

        public override bool TriggerAction(string actionName)
        {
            if (actionName == "AddWaypoint")
            {
                LE_Waypoint area = customWaypointSupport.AddWaypoint();
                if (area)
                {
                    area.transform.localPosition = Vector3.forward * 3f;
                    area.transform.localScale = Vector3.one * 3f;
                }
                return true;
            }

            return base.TriggerAction(actionName);
        }

        void Update()
        {
            if (!initialized || !PlayModeController.Instance || !Controls.Instance || Time.timeScale == 0f)
                return;

            if ((TriggerMode)properties["TriggerMode"] != TriggerMode.LOOK)
                return;

            if (IsPlayerInsideTrigger() && IsLookingAtArea())
            {
                lookHeldTime += Time.deltaTime;
                if (!wasLookingAtArea && lookHeldTime >= GetProperty<float>("LookDelay"))
                {
                    wasLookingAtArea = true;
                    ExecuteOnLookEnterEvents();
                }
            }
            else
            {
                lookHeldTime = 0f;
                if (wasLookingAtArea)
                {
                    wasLookingAtArea = false;
                    ExecuteOnLookExitEvents();
                }
            }
        }

        bool IsPlayerInsideTrigger()
        {
            Vector3 local = trigger.transform.InverseTransformPoint(Controls.Instance.transform.position) - trigger.center;
            Vector3 half = trigger.size / 2f;

            return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z;
        }

        bool IsLookingAtArea()
        {
            if (customWaypointSupport == null || customWaypointSupport.spawnedWaypoints == null || customWaypointSupport.spawnedWaypoints.Count == 0)
                return false;

            Transform area = customWaypointSupport.spawnedWaypoints[0].transform;
            Transform cam = Controls.Instance.gameCamera.transform;

            Vector3 origin = area.InverseTransformPoint(cam.position);
            Vector3 direction = area.InverseTransformVector(cam.forward);

            float tMin = float.NegativeInfinity;
            float tMax = float.PositiveInfinity;
            for (int axis = 0; axis < 3; axis++)
            {
                if (Mathf.Abs(direction[axis]) < 1e-8f)
                {
                    if (Mathf.Abs(origin[axis]) > 0.5f) return false;
                    continue;
                }

                float t1 = (-0.5f - origin[axis]) / direction[axis];
                float t2 = (0.5f - origin[axis]) / direction[axis];
                if (t1 > t2) (t1, t2) = (t2, t1);

                tMin = Mathf.Max(tMin, t1);
                tMax = Mathf.Min(tMax, t2);
                if (tMin > tMax) return false;
            }

            return tMax >= 0f;
        }

        void ExecuteOnLookEnterEvents()
        {
            if ((LookMode)properties["LookMode"] == LookMode.ONCE)
            {
                if (lookEnterUsed) return;
                lookEnterUsed = true;
            }

            eventExecuter.ExecuteEventsWithAndLogic((List<LE_Event>)properties["OnEnter"], "OnEnter", true);
        }

        void ExecuteOnLookExitEvents()
        {
            if ((LookMode)properties["LookMode"] == LookMode.ONCE)
            {
                if (lookExitUsed) return;
                lookExitUsed = true;
            }

            eventExecuter.ExecuteEventsWithAndLogic((List<LE_Event>)properties["OnExit"], "OnExit", false);
        }

        // Called when cube enters trigger (only for cube-only mode)
        public void OnCubeEnter(GameObject cube)
        {
            TriggerMode mode = (TriggerMode)properties["TriggerMode"];
            if (mode != TriggerMode.CUBE_ONLY) return;

            cubesInTrigger.Add(cube);
            eventExecuter.ExecuteEventsWithAndLogic((List<LE_Event>)properties["OnEnter"], "OnEnter", true);
        }

        // Called when cube exits trigger (only for cube-only mode)  
        public void OnCubeExit(GameObject cube)
        {
            TriggerMode mode = (TriggerMode)properties["TriggerMode"];
            if (mode != TriggerMode.CUBE_ONLY) return;

            cubesInTrigger.Remove(cube);
            eventExecuter.ExecuteEventsWithAndLogic((List<LE_Event>)properties["OnExit"], "OnExit", false);
        }

        void ExecuteOnEnterEvents()
        {
            TriggerMode mode = (TriggerMode)properties["TriggerMode"];

            // Skip player triggers when in cube-only mode (cubes are handled by OnCubeEnter)
            if (mode == TriggerMode.CUBE_ONLY || mode == TriggerMode.LOOK) return;

            // Check if this is Once mode and already triggered
            if (mode == TriggerMode.ONCE && hasBeenTriggered)
            {
                // Special case: If this trigger creates an objective, check if the objective still exists
                // If it doesn't exist (was failed/completed), allow re-triggering
                if (ShouldAllowRetriggerForObjective())
                {
                    hasBeenTriggered = false; // Reset so it can trigger again
                    hasExitBeenTriggered = false;
                }
                else
                {
                    return; // Don't trigger again
                }
            }

            // For Once and Multiple modes, trigger the events
            if ((mode == TriggerMode.ONCE && !hasBeenTriggered) || mode == TriggerMode.MULTIPLE)
            {
                eventExecuter.ExecuteEventsWithAndLogic((List<LE_Event>)properties["OnEnter"], "OnEnter", true);

                // Mark as triggered for Once mode
                if (mode == TriggerMode.ONCE)
                {
                    hasBeenTriggered = true;
                }
            }
        }

        private bool ShouldAllowRetriggerForObjective()
        {
            var onEnterEvents = (List<LE_Event>)properties["OnEnter"];

            foreach (var evt in onEnterEvents)
            {
                // If this event creates an objective, check if it exists
                if (evt.isForObjective && evt.objectiveState == LE_Event.ObjectiveState.Create)
                {
                    // Check if the objective still exists in PlayModeController
                    if (PlayModeController.Instance != null)
                    {
                        bool objectiveExists = PlayModeController.Instance.DoesObjectiveExist(evt.objectiveName);
                        if (!objectiveExists)
                        {
                            return true; // Allow re-trigger since objective no longer exists
                        }
                    }
                }
            }

            return false; // Don't allow re-trigger
        }

        void ExecuteOnExitEvents()
        {
            TriggerMode mode = (TriggerMode)properties["TriggerMode"];

            // Skip player triggers when in cube-only mode (cubes are handled by OnCubeExit)
            if (mode == TriggerMode.CUBE_ONLY || mode == TriggerMode.LOOK) return;

            if (mode == TriggerMode.ONCE && hasExitBeenTriggered)
            {
                if (ShouldAllowRetriggerForObjective())
                {
                    hasExitBeenTriggered = false;
                    hasBeenTriggered = false;
                }
                else
                {
                    return; // Don't trigger exit again
                }
            }

            // For Once and Multiple modes, trigger exit events
            // Note: Exit events can still trigger even if OnEnter was already used in Once mode
            if ((mode == TriggerMode.ONCE && !hasExitBeenTriggered) || mode == TriggerMode.MULTIPLE)
            {
                eventExecuter.ExecuteEventsWithAndLogic((List<LE_Event>)properties["OnExit"], "OnExit", false);

                // NEW: Mark as triggered for Once mode
                if (mode == TriggerMode.ONCE)
                {
                    hasExitBeenTriggered = true;
                }
            }
        }

        public static new Color GetDefaultObjectColor(LEObjectContext context)
        {
            return new Color(1f, 1f, 0.07843138f);
        }
    }

    // Custom component to detect cube collisions for cube-only triggers

    public class CubeTriggerDetector : MonoBehaviour
    {
        public LE_Trigger parentTrigger;

        private void OnTriggerEnter(Collider other)
        {
            // Check if the colliding object is a cube
            if (IsCube(other.gameObject))
            {
                parentTrigger.OnCubeEnter(other.gameObject);
            }
            else if (other.GetComponent<Controls>() != null)
            {
                if(ActivableController.activeCubeForInteraction != null)
                {
                    parentTrigger.OnCubeEnter(ActivableController.activeCubeForInteraction.gameObject);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            // Check if the colliding object is a cube
            if (IsCube(other.gameObject))
            {
                parentTrigger.OnCubeExit(other.gameObject);
            }
            else if (other.GetComponent<Controls>() != null)
            {
                if (ActivableController.activeCubeForInteraction != null)
                {
                    parentTrigger.OnCubeExit(ActivableController.activeCubeForInteraction.gameObject);
                }
            }
        }

        private bool IsCube(GameObject obj)
        {
            // Check if the object has the "Bloc" tag (cube tag)
            if (obj.CompareTag("Bloc"))
                return true;

            // Also check parent in case the collider is a child of the cube
            if (obj.transform.parent != null && obj.transform.parent.CompareTag("Bloc"))
                return true;

            return false;
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(Controls), nameof(Controls.OnTriggerEnter))]
    public static class LE_TriggerPlayerInsidePatch
    {
        public static bool Prefix(Controls __instance, Collider collider)
        {
            if (collider.transform.parent && collider.transform.parent.parent && collider.transform.parent.parent.TryGetComponent<LE_Trigger>(out var leTrigger))
            {
                if (leTrigger.skipTriggerWithPlayerThisFrame)
                {
                    leTrigger.skipTriggerWithPlayerThisFrame = false; // Reset.
                    return false; // Prevent method execution.
                }
            }

            return true;
        }
    }
}