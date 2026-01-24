using Il2Cpp;
using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Death_Trigger : LE_Object
    {
        public enum TriggerType { RELOCATION, IMMINENT }

        public static Vector3 RESPAWN_POINT_POS_OFFSET => new Vector3(0f, 0.3f);

        ContainmentBox script;
        public Vector3 respawnPosition;
        public Vector3 respawnRotation;

        public static Dictionary<string, object> GetDefaultProperties()
        {
            return new Dictionary<string, object>()
            {
                { "Type", TriggerType.RELOCATION },
                { "Delay", 0f },
                { "CustomCoordinates", false },
                { "waypoints", new List<WaypointData>() }, // In order to not fuck up any waypoints related code in LE, just call this "waypoints", even tho it's just one (the RESPAWN POINT).
                { "OnTeleport", new List<LE_Event>() }
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
            GameObject content = contentObject;

            content.SetActive(false);

            script = content.GetChild("Trigger").AddComponent<ContainmentBox>();
            script.delay = GetProperty<float>("Delay");
            script.useSeparateDelays = false;
            script.warnDistance = 9;
            script.currentRespawnIndex = 0;
            script.m_resetTransform = content.GetChild("Spawn").transform;
            if (GetProperty<bool>("CustomCoordinates"))
            {
                UpdateRespawnPointPositionAndRotation();
			}
            script.playDialogs = false;
            script.selectivePlayDialogs = false;
            script.dialogsUpperLimit = false;
            script.killPlayer = GetProperty<TriggerType>("Type") == TriggerType.IMMINENT;
            script.useSeparateKillPlayer = false;
            script.isAreaDenial = false;
            script.considerPlayer = true;
            script.m_collider = script.GetComponent<BoxCollider>();

            if (((JsonElement)customWaypointSupport.targetWaypointsData[0].properties["RotatePlayer"]).GetBoolean())
            {
                script.gameObject.AddComponent<DeathTriggerRespawnRotationPatcher>();
            }

            script.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

			ConfigureEvents(script);

			content.SetActive(true);

            initialized = true;
        }
        // Add this method so DeathTriggerWaypointSupport.SetupForCustomSystem can call it to update the respawn point, since it's called after InitComponent().
        public void UpdateRespawnPointPositionAndRotation()
        {
            script.m_resetTransform.position = respawnPosition;
            script.m_resetTransform.eulerAngles = respawnRotation;
        }

        public override bool SetProperty(string name, object value)
        {
            if (name == "Type")
            {
                if (value is int)
                {
                    properties["Type"] = (TriggerType)value;
                    return true;
                }
                else if (value is TriggerType)
                {
                    properties["Type"] = value;
                    return true;
                }
            }
			else if (name == "CustomCoordinates")
			{
				if (value is bool)
				{
					properties["CustomCoordinates"] = (bool)value;
					return true;
				}
			}
			else if (name == "Delay")
            {
                if (value is string)
                {
                    if (Utils.TryParseFloat((string)value, out float result))
                    {
                        properties["Delay"] = result;
                        return true;
                    }
                }
                else if (value is float)
                {
                    properties["Delay"] = (float)value;
                    return true;
                }
            }
			else if (name == "waypoints")
			{
				if (value is List<WaypointData>)
				{
					properties["waypoints"] = (List<WaypointData>)value;
					return true;
				}
			}
			else if (GetAvailableEventsIDs().Contains(name))
			{
				if (value is List<LE_Event>)
				{
					properties[name] = (List<LE_Event>)value;
				}
			}
			return base.SetProperty(name, value);
        }
        public override bool TriggerAction(string actionName)
        {
            if (actionName == "AddWaypoint")
            {
                customWaypointSupport.AddWaypoint();
                return true;
            }

            return base.TriggerAction(actionName);
        }

		public override List<string> GetAvailableEventsIDs()
		{
			return new List<string>
			{
				"OnTeleport",
			};
		}
		void ConfigureEvents(ContainmentBox script)
		{
			script.onTeleport = new UnityEngine.Events.UnityEvent();
			script.onTeleport.AddListener((UnityAction)ExecuteOnTeleportEvents);
		}
		void ExecuteOnTeleportEvents()
		{
			eventExecuter.ExecuteEvents((List<LE_Event>)properties["OnTeleport"]);
		}
		public static new Color GetDefaultObjectColor(LEObjectContext context)
        {
            return new Color(1f, 0f, 0f, 0.05f);
        }
    }

    [MelonLoader.RegisterTypeInIl2Cpp]
    public class DeathTriggerRespawnRotationPatcher : MonoBehaviour
    {
        LE_Death_Trigger script;
        Coroutine patchRoutine;

        void Awake()
        {
            script = transform.parent.parent.GetComponent<LE_Death_Trigger>();
        }

        void OnTriggerEnter(Collider collider)
        {
            if (patchRoutine != null)
            {
                MelonCoroutines.Stop(patchRoutine);
            }
        }
        void OnTriggerExit(Collider collider)
        {
            patchRoutine = (Coroutine)MelonCoroutines.Start(PatchRoutine());
        }

        IEnumerator PatchRoutine()
        {
            // Simulate the delay.
            yield return new WaitForSecondsRealtime(script.GetProperty<float>("Delay") + 0.2f); // Small offset added.

            // Don't ever ask me why, but since FS uses those yaw and pitch values, I need to pass these eulerAngles values inverted.
            // I've always struggled with rotations. - Jav.
            Controls.Instance.Angle = new Vector2(script.respawnRotation.y, script.respawnRotation.x);

            // And since Angle doesn't INSTANTLY move the camera, but it moves it slowly when it's drastically changed... force it ourselves :)
            Controls.Instance.transform.eulerAngles = script.respawnRotation;
        }
    }
}
