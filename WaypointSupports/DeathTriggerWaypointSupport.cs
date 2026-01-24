using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor.WaypointSupports
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class DeathTriggerWaypointSupport : WaypointSupport
    {
        public override List<WaypointData> targetWaypointsData => targetObject.GetProperty<List<WaypointData>>("waypoints");
        public override LE_Object.ObjectType waypointTypeToUse => LE_Object.ObjectType.DEATH_TRIGGER_WAYPOINT;
        public override bool needsEmptyWaypointAtStart => false;
        public override bool usesCustomMoveSystem => true;
        public override Color editorLineColor => Color.yellow;
        public override GameObject waypointTemplate => Core.LoadOtherObjectInBundle("Death Trigger Respawn Point");
        public override int? maxWaypointsCount => 1;

        public override void SetupForCustomSystem()
        {
            LE_Death_Trigger script = (LE_Death_Trigger)targetObject;

            script.respawnPosition = spawnedWaypoints[0].transform.position + LE_Death_Trigger.RESPAWN_POINT_POS_OFFSET;
            script.respawnRotation = spawnedWaypoints[0].transform.eulerAngles;
            script.UpdateRespawnPointPositionAndRotation();
        }

        public override WaypointMode GetWaypointMode()
        {
            return WaypointMode.NONE;
        }
    }
}
