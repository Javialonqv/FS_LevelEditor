using Il2Cpp;
using Microsoft.Diagnostics.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor.WaypointSupports
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class MovingPlatformWaypointSupport : WaypointSupport
    {
        public override List<WaypointData> targetWaypointsData => targetObject.GetProperty<List<WaypointData>>("waypoints");
        public override LE_Object.ObjectType waypointTypeToUse => LE_Object.ObjectType.MOVING_PLATFORM_WAYPOINT;
        public override bool needsEmptyWaypointAtStart => true;
        public override bool usesCustomMoveSystem => true;
        public override Color editorLineColor => Color.yellow;
        public override GameObject waypointTemplate => Core.LoadOtherObjectInBundle("Moving Platform Waypoint");

        public override void SetupForCustomSystem()
        {
            MovingPlatformController platformScript = gameObject.GetChild("Content").GetComponent<MovingPlatformController>();

            // Make the first waypoint match with the start position of the platform.
            spawnedWaypoints[0].transform.localPosition = new Vector3(0, 0.135f, 0);

            platformScript.currentWaypoint = spawnedWaypoints[0].gameObject;
            platformScript.currentWaypointScript = spawnedWaypoints[0].GetComponent<Waypoint>();
        }
    }
}
