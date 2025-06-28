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
        GameObject rangeSphere;

        void Awake()
        {
            light = gameObject.GetChildAt("Content/Light").GetComponent<Light>();
            lightBulbSprite = gameObject.GetChildAt("Content/Sprite");
            rangeSphere = gameObject.GetChildAt("Content/RangeSphere");

            properties = new Dictionary<string, object>()
            {
                { "Color", Color.white },
                { "Intensity", 1f },
                { "Range", 10f }
            };
        }

        public override void OnInstantiated(LEScene scene)
        {
            if (scene == LEScene.Playmode)
            {
                Destroy(lightBulbSprite);
                Destroy(rangeSphere);
            }

            base.OnInstantiated(scene);
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
            else if (name == "Range")
            {
                if (value is float)
                {
                    light.range = (float)value;
                    SetRangeSphereScale((float)value);
                    properties["Range"] = (float)value;
                    return true;
                }
                else if (value is string)
                {
                    if (Utilities.TryParseFloat((string)value, out float result))
                    {
                        light.range = result;
                        SetRangeSphereScale(result);
                        properties["Range"] = result;
                        return true;
                    }
                }
                else
                {
                    Logger.Error($"Tried to set \"Range\" property with value of type \"{value.GetType().Name}\".");
                    return false;
                }
            }

            return false;
        }

        void SetRangeSphereScale(float range)
        {
            Vector3 rangeSpherescale = Vector3.one * light.range * 2;
            rangeSphere.transform.localScale = rangeSpherescale;
        }

        public override void OnSelect()
        {
            base.OnSelect();

            rangeSphere.SetActive(true);
        }
        public override void OnDeselect(GameObject nextSelectedObj)
        {
            base.OnDeselect(nextSelectedObj);

            rangeSphere.SetActive(false);
        }

        public static new Color GetObjectColor(LEObjectContext context)
        {
            return new Color(0.7735849f, 0.7735849f, 0.1131185f, 0.03921569f);
        }
    }
}
