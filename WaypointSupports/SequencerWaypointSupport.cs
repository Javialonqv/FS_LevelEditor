using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor.WaypointSupports
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class SequencerWaypointSupport : WaypointSupport
    {
        public override List<WaypointData> targetWaypointsData => targetObject.GetProperty<List<WaypointData>>("waypoints");
        public override LE_Object.ObjectType waypointTypeToUse => LE_Object.ObjectType.SEQUENCE_WAYPOINT;
        public override bool needsEmptyWaypointAtStart => false;
        public override bool usesCustomMoveSystem => true;
        public override Color editorLineColor => Color.yellow;
        public override GameObject waypointTemplate => Core.LoadOtherObjectInBundle("Sequence Step");
        public override bool showWaypointsOnPlaymode => true;

        public override void SetupForCustomSystem()
        {
            LE_Sequence sequence = (LE_Sequence)targetObject;

            foreach (var step in spawnedWaypoints)
            {
                sequence.sequence.requiredSequence.Add(step.GetProperty<SequenceSwitchController.SwitchType>("Color"));
            }

            sequence.FinishedSettingUpSteps();
        }

        public override WaypointMode GetWaypointMode()
        {
            return WaypointMode.NONE;
        }
    }
}
