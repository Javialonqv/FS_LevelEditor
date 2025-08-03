using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using FS_LevelEditor.SaveSystem.SerializableTypes;

namespace FS_LevelEditor.SaveSystem
{
    [Serializable]
    public class LE_ObjectData
    {
        public LE_Object.ObjectType? objectType { get; set; }
        public int objectID { get; set; }
        public bool setActiveAtStart { get; set; } = true;
        public bool collision { get; set; } = true;
        public float movingSpeed { get; set; } = 5f;
        public WaypointMode wayMode { get; set; } = WaypointMode.NONE;

        public Dictionary<string, object> properties { get; set; } = new Dictionary<string, object>();
        public List<WaypointData> waypoints { get; set; } = new List<WaypointData>();

        public Vector3Serializable objPosition { get; set; }
        public Vector3Serializable objRotation { get; set; }
        public Vector3Serializable objScale { get; set; } = new Vector3Serializable(Vector3.one);
        
        public LE_ObjectData()
        {

        }
        public LE_ObjectData(LE_Object originalObj)
        {
            objectType = originalObj.objectType;
            objectID = originalObj.objectID;
            setActiveAtStart = originalObj.setActiveAtStart;
            collision = originalObj.collision;
            movingSpeed = originalObj.movingSpeed;
            wayMode = originalObj.waypointMode;

            SavePatches.AddPropertiesToObjectToSave(this, originalObj);
            waypoints = new (originalObj.waypoints);

            objPosition = originalObj.transform.localPosition;
            objRotation = originalObj.transform.localEulerAngles;
            objScale = originalObj.transform.localScale;
        }

        static Dictionary<string, object> ParseEventsData(LE_Event eventToParse)
        {
            Dictionary<string, object> simplifiedData = new();
            LE_Event defaultEvent = new LE_Event();

            foreach (var property in eventToParse.GetType().GetProperties())
            {
                var defaultValue = property.GetValue(defaultEvent);
                var value = property.GetValue(eventToParse);

                if (!defaultValue.Equals(value))
                {
                    simplifiedData.Add(property.Name, value);
                }
            }

            return simplifiedData;
        }
    }
}
