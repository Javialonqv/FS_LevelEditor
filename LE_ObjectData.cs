using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor
{
    [Serializable]
    public class LE_ObjectData
    {
        public LE_Object.ObjectType? objectType { get; set; }
        public int objectID { get; set; }
        public string objectOriginalName { get; set; }

        public Dictionary<string, object> properties { get; set; } = new Dictionary<string, object>();

        public Vector3Serializable objPosition { get; set; }
        public Vector3Serializable objRotation { get; set; }
        
        public LE_ObjectData()
        {

        }
        public LE_ObjectData(LE_Object originalObj)
        {
            objectType = originalObj.objectType;
            objectID = originalObj.objectID;
            objectOriginalName = originalObj.objectOriginalName;

            foreach (var property in originalObj.properties)
            {
                if (property.Value is Vector3)
                {
                    properties.Add(property.Key, new Vector3Serializable((Vector3)property.Value));
                }
                else if (property.Value is Color)
                {
                    properties.Add(property.Key, new ColorSerializable((Color)property.Value));
                }
                else
                {
                    properties.Add(property.Key, property.Value);
                }
            }

            objPosition = originalObj.transform.localPosition;
            objRotation = originalObj.transform.localEulerAngles;
        }
    }
}
