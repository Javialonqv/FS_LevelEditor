using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FS_LevelEditor
{
    [Serializable]
    public class LE_ObjectData
    {
        public LE_Object.ObjectType objectType { get; set; }
        public int objectID { get; set; }
        public string objectOriginalName { get; set; }
        public string objectFullNameWithID
        {
            get { return objectOriginalName + " " + objectID; }
        }

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

            objPosition = originalObj.transform.localPosition;
            objRotation = originalObj.transform.localEulerAngles;
        }
    }
}
