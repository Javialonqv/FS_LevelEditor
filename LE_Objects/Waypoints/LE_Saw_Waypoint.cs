using FS_LevelEditor;
using Il2Cpp;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using FS_LevelEditor.Editor;
using FS_LevelEditor.SaveSystem.SerializableTypes;
using FS_LevelEditor.WaypointSupports;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Saw_Waypoint : LE_Waypoint
    {
        public override WaypointSupport GetMainSupport()
        {
            return transform.parent.parent.GetComponent<SawWaypointSupport>();
        }

        public override void InitComponent()
        {
            Waypoint script = gameObject.AddComponent<Waypoint>();

            script.speedMultiplier = 1f;
            if (isLastWaypoint && mainSupport.GetWaypointMode() == WaypointMode.NONE)
            {
                script.waitHere = -1;
            }
            else
            {
                script.waitHere = isFirstWaypoint ? mainSupport.targetObject.GetProperty<float>("WaitTime") : GetProperty<float>("WaitTime");
            }

            if (nextWaypoint)
            {
                script.nextWaypoint = nextWaypoint.gameObject;
            }
            else
            {
                // If this waypoint is the last one, then set the next waypoint as the first waypoint that is right after the saw itself.
                if (mainSupport.spawnedWaypoints.Last() == this)
                {
                    script.nextWaypoint = mainSupport.spawnedWaypoints[0].gameObject;
                }
            }
            script.checkpoints = mainSupport.spawnedWaypoints.Select(x => x.gameObject).ToArray();

            initialized = true;
        }
    }
}

[Serializable]
public class LE_SawWaypointSerializable
{
    public float waitTime { get; set; }

    public int objectID { get; set; }
    public Vector3Serializable waypointPosition { get; set; }
    public Vector3Serializable waypointRotation { get; set; }

    public LE_SawWaypointSerializable() { }
    public LE_SawWaypointSerializable(LE_SawWaypointSerializable toCopy)
    {
        objectID = toCopy.objectID;
        waypointPosition = toCopy.waypointPosition;
        waypointRotation = toCopy.waypointRotation;
    }
    public LE_SawWaypointSerializable(LE_Saw_Waypoint waypoint)
    {
        waitTime = (float)waypoint.GetProperty("WaitTime");

        objectID = waypoint.objectID;
        waypointPosition = waypoint.transform.localPosition;
        waypointRotation = waypoint.transform.localEulerAngles;
    }
}
