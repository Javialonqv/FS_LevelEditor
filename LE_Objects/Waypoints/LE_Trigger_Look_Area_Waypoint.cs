using FS_LevelEditor.WaypointSupports;

namespace FS_LevelEditor
{
    public class LE_Trigger_Look_Area_Waypoint : LE_Waypoint
    {
        public override string[] EventsIDs => [];

        public override WaypointSupport GetMainSupport()
        {
            return transform.parent.parent.GetComponent<TriggerLookAreaWaypointSupport>();
        }

        public static Dictionary<string, object> GetDefaultProperties()
        {
            return new Dictionary<string, object>();
        }
    }
}
