using System;
using System.Collections.Generic;
using UnityEngine;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Rgb_Wall : LE_Object
    {
        MeshRenderer wallRenderer;

        void Awake()
        {
            // Get the wall's mesh renderer to access its material
            wallRenderer = gameObject.GetChildAt("Content/SubMesh_0").GetComponent<MeshRenderer>();
        }

        public static Dictionary<string, object> GetDefaultProperties()
        {
            return new Dictionary<string, object>()
            {
                { "Color", Color.white } // Default white color as Color object
            };
        }

        public override void OnInstantiated(LEScene scene)
        {
            if (scene == LEScene.Playmode)
            {
                Logger.Log(GetProperty<Color>("Color").ToString());
            }
            base.OnInstantiated(scene);
        }

        public override bool SetProperty(string name, object value)
        {
            if (name == "Color")
            {
                if (value is Color)
                {
                    SetEmissionColor((Color)value);
                    properties["Color"] = (Color)value;
                    return true;
                }
                else if (value is string)
                {
                    Color? color = Utils.HexToColor((string)value, false, null);
                    if (color != null)
                    {
                        SetEmissionColor((Color)color);
                        properties["Color"] = (Color)color;
                        return true;
                    }
                }
            }
            Debug.Log($"{name}, {value}");
            return base.SetProperty(name, value);
        }

        void SetEmissionColor(Color color)
        {
            if (wallRenderer != null)
            {
                Material mat = wallRenderer.material;

                // Enable emission
                mat.EnableKeyword("_EMISSION");

                // Set emission color with HDR intensity to ensure it's not mixed with base color
                // Multiply by 2 to make it brighter and more visible
                Color emissionColor = color * 2f;
                mat.SetColor("_EmissionColor", emissionColor);
            }
        }
    }
}
