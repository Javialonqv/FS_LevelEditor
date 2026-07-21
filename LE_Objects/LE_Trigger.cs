using FS_LevelEditor.Editor.UI;
using FS_LevelEditor.Playmode;
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
	public enum TriggerMode
	{
		ONCE = 0,        // Default, can only be triggered once by player
		MULTIPLE = 1,    // Can be triggered multiple times by player
		CUBE_ONLY = 2     // Only triggered by cube
	}

	[MelonLoader.RegisterTypeInIl2Cpp]
	public class LE_Trigger : LE_Object
	{
		private bool hasBeenTriggered = false; // Track if trigger has been activated (for Once mode)
		private HashSet<GameObject> cubesInTrigger = new HashSet<GameObject>(); // Track cubes currently in trigger

		TriggerScript triggerScript;
		BoxCollider trigger;
		public bool skipTriggerWithPlayerThisFrame = false;

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
						triggerScript.canBeReactivated = true;
                    });
                    // And only call OnTriggerExit (to execute events) if the prop is true.
                    if (GetProperty<bool>("ExecIfDespawned"))
					{
                        player.OnTriggerExit(trigger);
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
            if (mode == TriggerMode.CUBE_ONLY) return;

            // Check if this is Once mode and already triggered
            if (mode == TriggerMode.ONCE && hasBeenTriggered)
            {
                // Special case: If this trigger creates an objective, check if the objective still exists
                // If it doesn't exist (was failed/completed), allow re-triggering
                if (ShouldAllowRetriggerForObjective())
                {
                    hasBeenTriggered = false; // Reset so it can trigger again
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
			if (mode == TriggerMode.CUBE_ONLY) return;

			// For Once and Multiple modes, trigger exit events
			// Note: Exit events can still trigger even if OnEnter was already used in Once mode
			if (mode == TriggerMode.ONCE || mode == TriggerMode.MULTIPLE)
			{
				eventExecuter.ExecuteEventsWithAndLogic((List<LE_Event>)properties["OnExit"], "OnExit", false);
			}
		}

		public static new Color GetDefaultObjectColor(LEObjectContext context)
		{
			return new Color(1f, 1f, 0.07843138f);
		}
	}

	// Custom component to detect cube collisions for cube-only triggers
	[MelonLoader.RegisterTypeInIl2Cpp]
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
		}

		private void OnTriggerExit(Collider other)
		{
			// Check if the colliding object is a cube
			if (IsCube(other.gameObject))
			{
				parentTrigger.OnCubeExit(other.gameObject);
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