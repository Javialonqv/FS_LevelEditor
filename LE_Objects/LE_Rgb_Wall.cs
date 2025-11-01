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

            properties = new Dictionary<string, object>()
            {
            { "Color", "FFFFFF" } // Default white color
            };
        }

        public override bool SetProperty(string name, object value)
        {
            // Handle both "Color" and "ColorHex" property names
            if (name == "Color")
            {
                if (value is Color color)
                {
                    SetEmissionColor(color);
                    properties["Color"] = color;
                    return true;
                }
                else if (value is string hexString)
                {
                    Color? parsedColor = Utils.HexToColor(hexString, false, null);
                    if (parsedColor != null)
                    {
                        Color colorValue = parsedColor.Value;
                        SetEmissionColor(colorValue);
                        properties["Color"] = colorValue;
                        return true;
                    }
                }
            }
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

                // Also set the base color to white to prevent color mixing
                if (mat.HasProperty("_Color"))
                {
                    mat.SetColor("_Color", Color.white);
                }
            }
        }

        public override object GetProperty(string name)
        {
            // Handle the ColorHex property name by returning the Color property
            if (name == "Color")
            {
                return base.GetProperty("Color");
            }

            return base.GetProperty(name);
        }

        public override T GetProperty<T>(string name)
        {
            // Handle the ColorHex property name by returning the Color property
            if (name == "Color")
            {
                return base.GetProperty<T>("Color");
            }

            return base.GetProperty<T>(name);
        }
    }
}
