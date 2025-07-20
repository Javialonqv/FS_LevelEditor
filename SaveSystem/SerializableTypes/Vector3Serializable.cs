using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor.SaveSystem.SerializableTypes
{
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
        public Vector3Serializable(Vector3 v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
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
