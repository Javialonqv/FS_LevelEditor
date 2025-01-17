using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using MelonLoader;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace FS_LevelEditor
{
    [Serializable]
    public class LevelData
    {
        public List<LE_ObjectData> objects { get; set; } = new List<LE_ObjectData>();
        static readonly string testLevelFilePath = Path.Combine(Application.persistentDataPath, "test_level.dat");

        public LevelData()
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
                Melon<Core>.Logger.Msg("Creating save data!");
            }

            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                };
                File.WriteAllText(testLevelFilePath, JsonSerializer.Serialize(data, options));

                Melon<Core>.Logger.Msg("Level saved! Path: " + testLevelFilePath);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public static void LoadLevelData()
        {
            GameObject objectsParent = EditorController.Instance.levelObjectsParent;
            objectsParent.DeleteAllChildren();

            LevelData data = JsonSerializer.Deserialize<LevelData>(File.ReadAllText(testLevelFilePath));

            foreach (LE_ObjectData obj in data.objects)
            {
                GameObject objInstance = EditorController.Instance.PlaceObject(obj.objectOriginalName, obj.objPosition, obj.objRotation, false);
                LE_Object objClassInstance = objInstance.GetComponent<LE_Object>();

                objClassInstance.objectID = obj.objectID;
            }

            Melon<Core>.Logger.Msg("Level loaded!");
        }
    }

    [Serializable]
    public struct Vector3Serializable
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }

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
