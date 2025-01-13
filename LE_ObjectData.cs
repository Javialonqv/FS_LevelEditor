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
        public LE_Object.ObjectType objectType;
        public int objectID;
        public string objectOriginalName;
        public string objectFullNameWithID
        {
            get { return objectOriginalName + " " + objectID; }
        }

        public Vector3Serializable objPosition;
        public Vector3Serializable objRotation;

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
