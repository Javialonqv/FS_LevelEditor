using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor.SaveSystem.SerializableTypes
{
    [Serializable]
    public struct ColorSerializable
    {
        public float r { get; set; }
        public float g { get; set; }
        public float b { get; set; }

        public ColorSerializable(float r, float g, float b)
        {
            this.r = r;
            this.g = g;
            this.b = b;
        }
        public ColorSerializable(Color color)
        {
            r = color.r;
            g = color.g;
            b = color.b;
        }

        public static explicit operator ColorSerializable(Color color)
        {
            return new ColorSerializable(color.r, color.g, color.b);
        }
        public static explicit operator Color(ColorSerializable color)
        {
            return new Color(color.r, color.g, color.b);
        }

        public Color ToColor()
        {
            return new Color(r, g, b);
        }
    }
}
