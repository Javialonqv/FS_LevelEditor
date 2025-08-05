using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor.WaypointSupports
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class SawWaypointSupport : WaypointSupport
    {
        public override List<WaypointData> targetWaypointsData =>  targetObject.GetProperty<List<WaypointData>>("waypoints");
        public override LE_Object.ObjectType waypointTypeToUse => LE_Object.ObjectType.SAW_WAYPOINT;
        public override bool needsEmptyWaypointAtStart => true;
        public override bool usesCustomMoveSystem => true;
        public override Color editorLineColor => Color.yellow;
        public override GameObject waypointTemplate => Core.LoadOtherObjectInBundle("Saw Waypoint");

        public override void SetupForCustomSystem()
        {
            ScieScript sawScript = gameObject.GetChild("Content").GetComponent<ScieScript>();

            sawScript.currentWaypoint = spawnedWaypoints[0].gameObject;
            sawScript.currentWaypointScript = spawnedWaypoints[0].GetComponent<Waypoint>();
            sawScript.movingSaw = true;
        }

        public override WaypointMode GetWaypointMode()
        {
            LE_Saw saw = GetComponent<LE_Saw>();

            if (saw.GetProperty<bool>("TravelBack") && !saw.GetProperty<bool>("Loop"))
            {
                return WaypointMode.TRAVEL_BACK;
            }
            else if (!saw.GetProperty<bool>("TravelBack") && saw.GetProperty<bool>("Loop"))
            {
                return WaypointMode.LOOP;
            }

            return WaypointMode.NONE;
        }
    }
}
