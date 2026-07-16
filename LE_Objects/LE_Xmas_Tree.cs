using FS_LevelEditor.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Xmas_Tree : LE_Object
    {
        GameObject presentsParent;
        GameObject light;
        GameObject ballsParent;

        public static Dictionary<string, object> GetDefaultProperties()
        {
            return new Dictionary<string, object>
            {
                { "Presents", true },
                { "Light", true },
                { "Balls", true }
            };
        }

        void Awake()
        {
            presentsParent = contentObject.GetChild("PresentsPack");
            light = contentObject.GetChild("Garland_lights");
            ballsParent = contentObject.GetChild("Balls");
        }

        public override void ObjectStart(LEScene scene)
        {
            SetPresentsState(GetProperty<bool>("Presents"));
            SetLightState(GetProperty<bool>("Light"));
            SetBallsState(GetProperty<bool>("Balls"));

            base.ObjectStart(scene);
        }

        public override bool SetProperty(string name, object value)
        {
            if (name == "Presents")
            {
                if (value is bool boolValue)
                {
                    properties["Presents"] = boolValue;
                    if (EditorController.Instance)
                        SetPresentsState(boolValue);
                    return true;
                }
            }
            else if (name == "Light")
            {
                if (value is bool boolValue)
                {
                    properties["Light"] = boolValue;
                    if (EditorController.Instance)
                        SetLightState(boolValue);
                    return true;
                }
            }
            else if (name == "Balls")
            {
                if (value is bool boolValue)
                {
                    properties["Balls"] = boolValue;
                    if (EditorController.Instance)
                        SetBallsState(boolValue);
                    return true;
                }
            }

            return base.SetProperty(name, value);
        }

        void SetPresentsState(bool enabled)
        {
            presentsParent.SetActive(enabled);
            if (EditorController.Instance)
            {
                foreach (var waypoint in waypointSupport.spawnedWaypoints)
                {
                    waypoint.contentObject.GetChild("PresentsPack").SetActive(enabled);
                }
            }
        }
        void SetLightState(bool enabled)
        {
            light.SetActive(enabled);
            if (EditorController.Instance)
            {
                foreach (var waypoint in waypointSupport.spawnedWaypoints)
                {
                    waypoint.contentObject.GetChild("Garland_lights").SetActive(enabled);
                }
            }
        }
        void SetBallsState(bool enabled)
        {
            ballsParent.SetActive(enabled);
            if (EditorController.Instance)
            {
                foreach (var waypoint in waypointSupport.spawnedWaypoints)
                {
                    waypoint.contentObject.GetChild("Balls").SetActive(enabled);
                }
            }
        }
    }
}
