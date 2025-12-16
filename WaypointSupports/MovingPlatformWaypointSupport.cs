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
        public override Vector3 waypointsPositionOffsetInPlaymode => new Vector3(0, 0.135f, 0); // Match every waypoint with the offset needed for MPs.
        public override bool usesCustomMoveSystem => true;
        public override Color editorLineColor => Color.yellow;
        public override GameObject waypointTemplate => Core.LoadOtherObjectInBundle("Moving Platform Waypoint");

        public override void SetupForCustomSystem()
        {
            MovingPlatformController platformScript = gameObject.GetChild("Content").GetComponent<MovingPlatformController>();

            platformScript.currentWaypoint = spawnedWaypoints[0].gameObject;
            platformScript.currentWaypointScript = spawnedWaypoints[0].GetComponent<Waypoint>();

            // CRITICAL FIX: Add rotation applier that will rotate the platform to match waypoint rotation
            // AFTER reaching each waypoint, not during transit. This prevents waypoint rotation from
            // affecting the platform's heading while moving.
            var rotationApplier = gameObject.AddComponent<WaypointRotationApplier>();
            rotationApplier.targetTransform = gameObject.GetChild("Content").transform;
            rotationApplier.waypointSupport = this;
        }

        public override WaypointMode GetWaypointMode()
        {
            LE_Moving_Platform platform = GetComponent<LE_Moving_Platform>();

            return platform.GetProperty<WaypointMode>("MovementMode");
        }
    }
}
