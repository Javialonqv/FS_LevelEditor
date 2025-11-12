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
		Once = 0,        // Default, can only be triggered once by player
		Multiple = 1,    // Can be triggered multiple times by player
		CubeOnly = 2     // Only triggered by cube
	}

	[MelonLoader.RegisterTypeInIl2Cpp]
	public class LE_Trigger : LE_Object
	{
		private bool hasBeenTriggered = false; // Track if trigger has been activated (for Once mode)
		private HashSet<GameObject> cubesInTrigger = new HashSet<GameObject>(); // Track cubes currently in trigger

		void Awake()
		{
			properties = new Dictionary<string, object>
			{
				{ "OnEnter", new List<LE_Event>() },
				{ "OnExit", new List<LE_Event>() },
				{ "TriggerMode", (int)TriggerMode.Once }  // Default to Once mode
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

			TriggerScript trigger = triggerObj.AddComponent<TriggerScript>();
			trigger.onEnter = new UnityEvent();
			trigger.onEnter.AddListener((UnityAction)ExecuteOnEnterEvents);
			trigger.onExit = new UnityEngine.Events.UnityEvent();
			trigger.onExit.AddListener((UnityAction)ExecuteOnExitEvents);
			trigger.onDestroy = new UnityEvent();
			trigger.BlocSwitchs = new GameObject[0];
			trigger.objectsToActivate = new GameObject[0];
			trigger.objectsToDeactivate = new GameObject[0];
			trigger.objectsToEnableOnly = new GameObject[0];
			trigger.objectsToDestroy = new GameObject[0];
			trigger.doorsToClose = new GameObject[0];
			trigger.lasersToEnable = new Laser_H_Controller[0];
			trigger.lasersToDisable = new Laser_H_Controller[0];
			trigger.dialogToActivate = new string[0];
			trigger.m_messages = new Messenger[0];
			trigger.keepActivated = true;

			initialized = true;
		}

		public override bool SetProperty(string name, object value)
		{
			if (name == "TriggerMode" && value is int)
			{
				properties[name] = value;
				// No need to reconfigure collision detection since we handle it in the detector component
				return true;
			}

			if (GetAvailableEventsIDs().Contains(name))
			{
				if (value is List<LE_Event>)
				{
					properties[name] = (List<LE_Event>)value;
					return true;
				}
			}

			return base.SetProperty(name, value);
		}

		public override List<string> GetAvailableEventsIDs()
		{
			return new List<string>()
			{
				"OnEnter",
				"OnExit"
			};
		}

		// Called when cube enters trigger (only for cube-only mode)
		public void OnCubeEnter(GameObject cube)
		{
			TriggerMode mode = (TriggerMode)properties["TriggerMode"];
			if (mode != TriggerMode.CubeOnly) return;

			cubesInTrigger.Add(cube);
			eventExecuter.ExecuteEvents((List<LE_Event>)properties["OnEnter"]);
		}

		// Called when cube exits trigger (only for cube-only mode)  
		public void OnCubeExit(GameObject cube)
		{
			TriggerMode mode = (TriggerMode)properties["TriggerMode"];
			if (mode != TriggerMode.CubeOnly) return;

			cubesInTrigger.Remove(cube);
			eventExecuter.ExecuteEvents((List<LE_Event>)properties["OnExit"]);
		}

        void ExecuteOnEnterEvents()
        {
            TriggerMode mode = (TriggerMode)properties["TriggerMode"];

            // Skip player triggers when in cube-only mode (cubes are handled by OnCubeEnter)
            if (mode == TriggerMode.CubeOnly) return;

            // Check if this is Once mode and already triggered
            if (mode == TriggerMode.Once && hasBeenTriggered)
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
            if ((mode == TriggerMode.Once && !hasBeenTriggered) || mode == TriggerMode.Multiple)
            {
                eventExecuter.ExecuteEvents((List<LE_Event>)properties["OnEnter"]);

                // Mark as triggered for Once mode
                if (mode == TriggerMode.Once)
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
			if (mode == TriggerMode.CubeOnly) return;

			// For Once and Multiple modes, trigger exit events
			// Note: Exit events can still trigger even if OnEnter was already used in Once mode
			if (mode == TriggerMode.Once || mode == TriggerMode.Multiple)
			{
				eventExecuter.ExecuteEvents((List<LE_Event>)properties["OnExit"]);
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
}