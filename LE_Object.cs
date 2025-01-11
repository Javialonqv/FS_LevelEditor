using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Object : MonoBehaviour
    {
        public enum ObjectType
        {
            Ground,
            LargeGround
        }
        public static readonly Dictionary<string, ObjectType> typeNameToEnum = new Dictionary<string, ObjectType>()
        {
            { "Ground", ObjectType.Ground },
            { "Large Ground", ObjectType.LargeGround }
        };
        public static readonly Dictionary<ObjectType, string> typeEnumToName = new Dictionary<ObjectType, string>()
        {
            { ObjectType.Ground, "Ground" },
            { ObjectType.LargeGround, "Large Ground" }
        };

        public ObjectType objectType;
        public int objectID;
        public string objectName;
        public string objectFullNameWithID
        {
            get { return typeEnumToName[objectType] + " " + objectID; }
        }

        public void Init(string originalObjName)
        {
            objectType = typeNameToEnum[originalObjName];
            objectName = originalObjName;

            int id = 0;
            LE_Object[] objects = EditorController.Instance.levelObjectsParent.GetComponentsInChildren<LE_Object>();
            while (objects.Any(x => x.objectID == id))
            {
                id++;
            }
            objectID = id;

            gameObject.name = objectFullNameWithID;
        }
    }
}
