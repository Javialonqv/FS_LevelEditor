using UnityEngine;

namespace FS_LevelEditor.WaypointSupports
{
    public class TriggerLookAreaWaypointSupport : WaypointSupport
    {
        public override List<WaypointData> targetWaypointsData => targetObject.GetProperty<List<WaypointData>>("waypoints");
        public override LE_Object.ObjectType waypointTypeToUse => LE_Object.ObjectType.TRIGGER_LOOK_AREA_WAYPOINT;
        public override bool needsEmptyWaypointAtStart => false;
        public override bool usesCustomMoveSystem => true;
        public override Color editorLineColor => Color.yellow;
        public override int? maxWaypointsCount => 1;

        public override WaypointMode GetWaypointMode()
        {
            return WaypointMode.NONE;
        }
    }
}
