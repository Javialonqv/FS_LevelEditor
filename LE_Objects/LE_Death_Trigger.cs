using Newtonsoft.Json.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace FS_LevelEditor
{

    public class LE_Death_Trigger : LE_Object
    {
        public enum TriggerType { RELOCATION, IMMINENT }

        public static Vector3 RESPAWN_POINT_POS_OFFSET => new Vector3(0f, 0.3f);

        public ContainmentBox script;
        public Vector3 RespawnPosition => script != null && script.m_resetTransform != null ? script.m_resetTransform.position : Vector3.zero;
        public Vector3 RespawnRotation => script != null && script.m_resetTransform != null ? script.m_resetTransform.eulerAngles : Vector3.zero;

        public int lastInstantTeleportFrame = -1;

        public bool RotatePlayer
        {
            get
            {
                if (customWaypointSupport.targetWaypointsData != null && customWaypointSupport.targetWaypointsData.Count > 0)
                {
                    // Since it's the waypoint DATA itself and not the spawned one, it's stored as JsonElement.
                    return ((JToken)customWaypointSupport.targetWaypointsData[0].properties["RotatePlayer"]).Value<bool>();
                }

                return false;
            }
        }

        public override string[] EventsIDs =>
        [
            "OnTeleport"
        ];

        public static Dictionary<string, object> GetDefaultProperties()
        {
            return new Dictionary<string, object>()
            {
                { "Type", TriggerType.RELOCATION },
                { "Delay", 0f },
                { "WithOffset", false },
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

            if (customWaypointSupport != null && customWaypointSupport.targetWaypointsData != null && customWaypointSupport.targetWaypointsData.Count > 0)
            {
                if (customWaypointSupport.spawnedWaypoints != null && customWaypointSupport.spawnedWaypoints.Count > 0)
                {
                    script.m_resetTransform.SetParent(customWaypointSupport.spawnedWaypoints[0].transform);
                }
            }

            // If not using custom coords, and since respawnPosition uses GLOBAL coords, use this object itself pivot as the respawn coords.
            if (customWaypointSupport.targetWaypointsData == null || customWaypointSupport.targetWaypointsData.Count == 0)
            {
                SetRespawnPointPositionAndRotation(transform.position, transform.eulerAngles);
            }

            script.playDialogs = false;
            script.selectivePlayDialogs = false;
            script.dialogsUpperLimit = false;
            script.killPlayer = GetProperty<TriggerType>("Type") == TriggerType.IMMINENT;
            script.useSeparateKillPlayer = false;
            script.isAreaDenial = false;
            script.considerPlayer = true;
            script.m_collider = script.GetComponent<BoxCollider>();

            if (customWaypointSupport.targetWaypointsData != null && customWaypointSupport.targetWaypointsData.Count > 0)
            {
                if (RotatePlayer)
                {
                    if (GetProperty<float>("Delay") != 0 && GetProperty<LE_Death_Trigger.TriggerType>("Type") == LE_Death_Trigger.TriggerType.RELOCATION)
                        script.gameObject.AddComponent<DeathTriggerRespawnRotationPatcher>();
                }
            }

            script.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

            ConfigureEvents(script);

            content.SetActive(true);

            initialized = true;
        }
        // Add this method so DeathTriggerWaypointSupport.SetupForCustomSystem can call it to update the respawn point, since it's called after InitComponent().
        public void SetRespawnPointPositionAndRotation(Vector3 position, Vector3 rotation)
        {
            if (script != null && script.m_resetTransform != null)
            {
                script.m_resetTransform.position = position + RESPAWN_POINT_POS_OFFSET;
                script.m_resetTransform.eulerAngles = rotation;
            }
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
            else if (name == "WithOffset")
            {
                if (value is bool boolValue)
                {
                    properties["WithOffset"] = boolValue;
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

        void ConfigureEvents(ContainmentBox script)
        {
            script.onTeleport = new UnityEngine.Events.UnityEvent();
            script.onTeleport.AddListener((UnityAction)ExecuteOnTeleportEvents);
        }
        public void ExecuteOnTeleportEvents()
        {
            // OnTeleport is a one-shot activating event for AND logic purposes
            eventExecuter.ExecuteEventsWithAndLogic((List<LE_Event>)properties["OnTeleport"], "OnTeleport", true);
        }
        public static new Color GetDefaultObjectColor(LEObjectContext context)
        {
            return new Color(1f, 0f, 0f, 0.05f);
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(Controls), nameof(Controls.TeleportPlayerToPosition))]
    public static class WithOffsetRespawnPatch
    {
        public static LE_Death_Trigger AboutToTeleportTo;

        public static void Prefix(ref Vector3 desiredNewPosition, bool syncTransformsNow)
        {
            if (!AboutToTeleportTo)
                return;

            Vector3 diff = Controls.Instance.transform.position - AboutToTeleportTo.transform.position;
            desiredNewPosition += diff - LE_Death_Trigger.RESPAWN_POINT_POS_OFFSET;

            AboutToTeleportTo = null;
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(Controls), "OnTriggerExit")]
    public static class InstantRespawnPatch
    {
        public static bool Prefix(Collider collider)
        {
            LE_Death_Trigger t = collider.GetComponentInParent<LE_Death_Trigger>();
            if (!t) return true;

            bool isInstantRelocation =
                t.GetProperty<float>("Delay") == 0 &&
                t.GetProperty<LE_Death_Trigger.TriggerType>("Type") == LE_Death_Trigger.TriggerType.RELOCATION;

            if (isInstantRelocation)
            {
                // Duplicate prefix invocation in the same frame: swallow it.
                if (t.lastInstantTeleportFrame == Time.frameCount)
                    return false;
                t.lastInstantTeleportFrame = Time.frameCount;

                if (t.GetProperty<bool>("WithOffset"))
                    WithOffsetRespawnPatch.AboutToTeleportTo = t;

                NativeModLoader.Instance.StartCoroutine(TeleportInstantlySafe(t));
                return false;
            }

            if (t.GetProperty<bool>("WithOffset"))
                WithOffsetRespawnPatch.AboutToTeleportTo = t;

            return true;
        }

        private static IEnumerator TeleportInstantlySafe(LE_Death_Trigger t)
        {
            yield return new WaitForEndOfFrame();

            Controls.Instance.TeleportPlayerToPosition(t.script.m_resetTransform.position, true);
            if (t.RotatePlayer)
                DeathTriggerRespawnRotationPatcher.RotatePlayerNow(t);
            t.ExecuteOnTeleportEvents();
        }
    }


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
            // Only respond to the player, not other objects like mines or debris
            if (collider.tag != "Player") return;

            if (patchRoutine != null)
            {
                NativeModLoader.Instance.StopCoroutine(patchRoutine);
            }
        }
        void OnTriggerExit(Collider collider)
        {
            // Only respond to the player, not other objects like mines or debris
            if (collider.tag != "Player") return;

            patchRoutine = (Coroutine)NativeModLoader.Instance.StartCoroutine(PatchRoutine());
        }

        IEnumerator PatchRoutine()
        {
            // Simulate the delay.
            yield return new WaitForSecondsRealtime(script.GetProperty<float>("Delay")); // Small offset added.

            RotatePlayerNow(script);
        }

        public static void RotatePlayerNow(LE_Death_Trigger script)
        {
            // Don't ever ask me why, but since FS uses those yaw and pitch values, I need to pass these eulerAngles values inverted.
            // I've always struggled with rotations. - Jav.
            Transform player = Controls.Instance.player.transform;
            Transform playerCam = Controls.Instance.gameCamera.transform;

            Vector3 globalMovement = Controls.Instance.transform.InverseTransformDirection(Controls.Instance.m_walkingMovement);

            player.localEulerAngles = new Vector3(0, script.RespawnRotation.y, 0);
            playerCam.localEulerAngles = new Vector3(script.RespawnRotation.x, playerCam.localEulerAngles.y, playerCam.localEulerAngles.z);
            Controls.Instance.AdjustYawPitchBasedOnCurrent(false, true, true);
            Controls.Instance.m_walkingMovement = Controls.Instance.transform.TransformDirection(globalMovement);
        }
    }
}
