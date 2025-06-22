using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Point_Light : LE_Object
    {
        Light light;
        GameObject lightBulbSprite;

        void Awake()
        {
            light = gameObject.GetChildAt("Content/Light").GetComponent<Light>();
            lightBulbSprite = gameObject.GetChildAt("Content/Sprite");

            properties = new Dictionary<string, object>()
            {
                { "Color", Color.white },
                { "Intensity", 1f }
            };
        }

        public override void Start()
        {
            if (EditorController.Instance)
            {
                SetEditorCollider(true);
            }


            if (PlayModeController.Instance != null)
            {
                SetEditorCollider(false);
                Destroy(lightBulbSprite);
                Destroy(gameObject.GetChildWithName("Arrow"));
            }
        }

        void Update()
        {
            // If the light sprite is null is probaly because we're already in playmode and the light sprite was destroyed.
            if (lightBulbSprite != null)
            {
                lightBulbSprite.transform.rotation = Camera.main.transform.rotation;
            }
        }

        public override bool SetProperty(string name, object value)
        {
            if (name == "Color")
            {
                if (value is Color)
                {
                    light.color = (Color)value;
                    properties["Color"] = (Color)value;
                    return true;
                }
                else if (value is string)
                {
                    Color? color = Utilities.HexToColor((string)value, false, null);
                    if (color != null)
                    {
                        light.color = (Color)color;
                        properties["Color"] = (Color)color;
                        return true;
                    }
                }
                else
                {
                    Logger.Error($"Tried to set \"Color\" property with value of type \"{value.GetType().Name}\".");
                    return false;
                }
            }
            else if (name == "Intensity")
            {
                if (value is float)
                {
                    light.intensity = (float)value;
                    properties["Intensity"] = (float)value;
                    return true;
                }
                else if (value is string)
                {
                    if (Utilities.TryParseFloat((string)value, out float result))
                    {
                        light.intensity = result;
                        properties["Intensity"] = result;
                        return true;
                    }
                }
                else
                {
                    Logger.Error($"Tried to set \"Intensity\" property with value of type \"{value.GetType().Name}\".");
                    return false;
                }
            }

            return false;
        }
    }
}
