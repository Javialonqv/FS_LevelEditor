using FS_LevelEditor.Editor;
using UnityEngine;
using static UnityEngine.ParticleSystem;

namespace FS_LevelEditor
{ 
    public class LE_Window : LE_Object
    {
        GameObject border;
        void Awake()
        {
            border = gameObject.GetChildAt("Content/Border");
        }
        public static Dictionary<string, object> GetDefaultProperties()
        {
            return new Dictionary<string, object>()
            {
                { "Border", true },
            };
        }
        public override void ObjectStart(LEScene scene)
        {
            if (scene == LEScene.Editor)
            {
                UpdateBorderState(GetProperty<bool>("Border"));
            }

            base.ObjectStart(scene);
        }
        public override void InitComponent()
        {

            UpdateBorderState(GetProperty<bool>("Border"));

            base.InitComponent();
        }

        public override bool SetProperty(string name, object value)
        {
            if (name == "Border")
            {
                if (value is bool boolValue)
                {
                    properties["Border"] = boolValue;
                    if (EditorController.Instance) UpdateBorderState(boolValue);
                }
            }

            return base.SetProperty(name, value);
        }
        void UpdateBorderState(bool enabled)
        {
            border.GetComponent<MeshRenderer>().enabled = enabled;
            foreach (var waypoint in waypointSupport.spawnedWaypoints)
            {
                waypoint.gameObject.GetChildAt("Content/Border").GetComponent<MeshRenderer>().enabled = enabled;
            }
        }
    }
}
