using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Heal_Area : LE_Object
    {
        void Awake()
        {
            properties = new Dictionary<string, object>
            {
                { "HealValue", 3 },
                { "HealInterval", .1f },
                { "MaxHealth", 60 }
            };
        }

        public override void OnInstantiated(LEScene scene)
        {
            if (scene == LEScene.Playmode)
            {
                gameObject.GetChildAt("Content/Mesh").SetActive(false);
            }

            base.OnInstantiated(scene);
        }

        public override void InitComponent()
        {
            GameObject areaObj = gameObject.GetChildAt("Content/Area");
            areaObj.tag = "HealArea";
            areaObj.layer = LayerMask.NameToLayer("Ignore Raycast");

            HealArea script = areaObj.AddComponent<HealArea>();
            script.halfStatusObj = new GameObject();
            script.emptyStatusObj = new GameObject();
            script.vfx = new GameObject().AddComponent<ParticleSystem>();
            script.m_light = new GameObject().AddComponent<Light>();
            script.healValue = GetProperty<int>("HealValue");
            script.healInterval = GetProperty<float>("HealInterval");
            script.maxHealthToGive = GetProperty<int>("MaxHealth");

            initialized = true;
        }

        public override bool SetProperty(string name, object value)
        {
            if (name == "HealInterval")
            {
                if (value is string)
                {
                    if (Utils.TryParseFloat((string)value, out float result))
                    {
                        properties["HealInterval"] = result;
                        return true;
                    }
                }
                else if (value is float)
                {
                    properties["HealInterval"] = (float)value;
                    return true;
                }
            }
            else if (name == "HealValue")
            {
                if (value is string)
                {
                    if (int.TryParse((string)value, out int result))
                    {
                        properties["HealValue"] = result;
                        return true;
                    }
                }
                else if (value is int)
                {
                    properties["HealValue"] = (int)value;
                    return true;
                }
            }
            else if (name == "MaxHealth")
            {
                if (value is string)
                {
                    if (int.TryParse((string)value, out int result))
                    {
                        properties["MaxHealth"] = result;
                        return true;
                    }
                }
                else if (value is int)
                {
                    properties["MaxHealth"] = (int)value;
                    return true;
                }
            }
            return base.SetProperty(name, value);
        }

        public static new Color GetDefaultObjectColor(LEObjectContext context)
        {
            return new Color(0f, 1f, 0.65098039215686274509803921568627f);
        }
    }
}
