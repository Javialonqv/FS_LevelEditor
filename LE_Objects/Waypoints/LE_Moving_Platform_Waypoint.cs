using FS_LevelEditor.WaypointSupports;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Moving_Platform_Waypoint : LE_Waypoint
    {
        public override WaypointSupport GetMainSupport()
        {
            return transform.parent.parent.GetComponent<MovingPlatformWaypointSupport>();
        }

        public override void InitComponent()
        {
            Waypoint script = gameObject.AddComponent<Waypoint>();

            script.speedMultiplier = 1f;
            script.waitHere = 2f;

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
