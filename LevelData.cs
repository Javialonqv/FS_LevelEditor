using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;

namespace FS_LevelEditor
{
    [Serializable]
    public class LevelData
    {
        public List<LE_ObjectData> objects = new List<LE_ObjectData>();

        LevelData()
        {

        }

        public static LevelData CreateLevelData()
        {
            LevelData data = new LevelData();

            GameObject objectsParent = EditorController.Instance.levelObjectsParent;

            foreach (GameObject obj in objectsParent.GetChilds())
            {
                LE_ObjectData objData = new LE_ObjectData(obj.GetComponent<LE_Object>());
                data.objects.Add(objData);
            }

            return data;
        }

        public static void SaveLevelData(LevelData data = null)
        {
            if (data == null)
            {
                data = CreateLevelData();
            }

            FileStream stream = File.Create(Path.Combine(Application.persistentDataPath, "test_level.dat"));
            BinaryFormatter bf = new BinaryFormatter();
#pragma warning disable SYSLIB0011
            bf.Serialize(stream, data);
        }

        public static void LoadLevelData()
        {
            GameObject objectsParent = EditorController.Instance.levelObjectsParent;
            objectsParent.DeleteAllChildren();

            FileStream stream = File.Open(Path.Combine(Application.persistentDataPath, "test_level.dat"), FileMode.Open);
            BinaryFormatter bf = new BinaryFormatter();

            LevelData data = (LevelData)bf.Deserialize(stream);

            foreach (LE_ObjectData obj in data.objects)
            {
                GameObject objInstance = EditorController.Instance.PlaceObject(obj.objectOriginalName, obj.objPosition, obj.objRotation, false);
                LE_Object objClassInstance = objInstance.GetComponent<LE_Object>();

                objClassInstance.objectID = obj.objectID;
            }
        }
    }

    [Serializable]
    public struct Vector3Serializable
    {
        public float x;
        public float y;
        public float z;

        public Vector3Serializable(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static implicit operator Vector3Serializable(Vector3 v)
        {
            return new Vector3Serializable(v.x, v.y, v.z);
        }
        public static implicit operator Vector3(Vector3Serializable v)
        {
            return new Vector3(v.x, v.y, v.z);
        }
    }
}
