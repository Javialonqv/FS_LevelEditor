using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Directional_Light : LE_Object
    {
        // Since there's only ONE directional light per level, dont' even put the id on it.
        public override string objectFullNameWithID
        {
            get { return objectOriginalName; }
        }

        Light light;
        GameObject lightSprite;

        static int currentInstances = 0;
        const int maxInstances = 1;

        void Awake()
        {
            currentInstances++;

            light = gameObject.GetChildAt("Content/Light").GetComponent<Light>();
            lightSprite = gameObject.GetChildAt("Content/Sprite");

            properties = new Dictionary<string, object>()
            {
                { "Color", Color.white },
                { "Intensity", 1f }
            };
        }

        public override void OnInstantiated(LEScene scene)
        {
            if (scene == LEScene.Playmode)
            {
                Destroy(lightSprite);
                Destroy(gameObject.GetChildAt("Content/Arrow"));
            }

            base.OnInstantiated(scene);
        }

        void Update()
        {
            // If the light sprite is null is probaly because we're already in playmode and the light sprite was destroyed.
            if (lightSprite != null)
            {
                lightSprite.transform.rotation = Camera.main.transform.rotation;
            }
        }

        void OnDestroy()
        {
            currentInstances--;
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
