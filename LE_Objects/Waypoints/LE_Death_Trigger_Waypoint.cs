using FS_LevelEditor.WaypointSupports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static FS_LevelEditor.LE_Death_Trigger;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Death_Trigger_Waypoint : LE_Waypoint
    {
        GameObject sprite;

        // Override the LE_Waypoint implementation.
        public override string[] EventsIDs => [];

        public override WaypointSupport GetMainSupport()
        {
            return transform.parent.parent.GetComponent<DeathTriggerWaypointSupport>();
        }

        internal override void Awake()
        {
            sprite = gameObject.GetChildAt("Content/Sprite");

            base.Awake();
        }

        public static Dictionary<string, object> GetDefaultProperties()
        {
            return new Dictionary<string, object>()
            {
                { "RotatePlayer", true }
            };
        }

        void Update()
        {
            if (sprite && Camera.main)
            {
                sprite.transform.rotation = Camera.main.transform.rotation;
            }
        }

        public override bool SetProperty(string name, object value)
        {
            if (name == "RotatePlayer")
			{
				if (value is bool)
				{
					properties["RotatePlayer"] = (bool)value;
					return true;
				}
			}

            return base.SetProperty(name, value);
        }
    }
}
