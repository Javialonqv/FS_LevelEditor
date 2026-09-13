using FS_LevelEditor.WaypointSupports;
using UnityEngine;

namespace FS_LevelEditor
{

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

        public override void OnInstantiated(LEScene scene)
        {
            if (scene == LEScene.Editor)
            {
                transform.localScale = Vector3.one;
            }

            base.OnInstantiated(scene);
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
