using System;
using System.Collections.Generic;
using UnityEngine;

namespace FS_LevelEditor
{
    public class LE_Point_Light : LE_Object
    {
        public enum Type { POINT, SPOT };

        Light light;
        GameObject lightBulbSprite;
        GameObject rangeSphere;

        // Custom dynamically generated visualizers for the Editor
        GameObject spotVisualizer;
        bool isSelected = false;

        void Awake()
        {
            light = gameObject.GetChildAt("Content/Light").GetComponent<Light>();
            lightBulbSprite = gameObject.GetChildAt("Content/Sprite");

            Transform sphereTransform = gameObject.transform.Find("Content/RangeSphere");
            if (sphereTransform) rangeSphere = sphereTransform.gameObject;

            CreateCustomVisualizers();
        }

        public static Dictionary<string, object> GetDefaultProperties()
        {
            return new Dictionary<string, object>()
            {
                { "Type", Type.POINT },
                { "Color", Color.white },
                { "Intensity", 1f },
                { "Range", 10f },
                { "SpotAngle", 30f }
            };
        }

        public override void OnInstantiated(LEScene scene)
        {
            if (scene == LEScene.Playmode)
            {
                // Destroy editor-only visuals
                if (lightBulbSprite) Destroy(lightBulbSprite);
                if (rangeSphere) Destroy(rangeSphere);
                if (spotVisualizer) Destroy(spotVisualizer);
            }

            base.OnInstantiated(scene);
        }

        void Update()
        {
            if (lightBulbSprite && Camera.main)
            {
                lightBulbSprite.transform.rotation = Camera.main.transform.rotation;
            }
        }

        public override bool SetProperty(string name, object value)
        {
            bool propertyChanged = false;

            if (name == "Type")
            {
                if (value is int)
                {
                    properties["Type"] = (Type)value;
                    ApplyLightType((Type)value);
                    propertyChanged = true;
                }
                else if (value is Type)
                {
                    properties["Type"] = value;
                    ApplyLightType((Type)value);
                    propertyChanged = true;
                }
                else if (value is string strValue && Enum.TryParse(strValue, true, out Type parsedType))
                {
                    properties["Type"] = parsedType;
                    ApplyLightType(parsedType);
                    propertyChanged = true;
                }
            }
            else if (name == "Color")
            {
                if (value is Color colorVal)
                {
                    light.color = colorVal;
                    properties["Color"] = colorVal;
                    propertyChanged = true;
                }
                else if (value is string strVal)
                {
                    Color? color = Utils.HexToColor(strVal, false, null);
                    if (color != null)
                    {
                        light.color = (Color)color;
                        properties["Color"] = (Color)color;
                        propertyChanged = true;
                    }
                }
            }
            else if (name == "Intensity")
            {
                if (TryParseFloatOrString(value, out float result))
                {
                    light.intensity = result;
                    properties["Intensity"] = result;
                    propertyChanged = true;
                }
            }
            else if (name == "Range")
            {
                if (TryParseFloatOrString(value, out float result))
                {
                    light.range = result;
                    properties["Range"] = result;
                    propertyChanged = true;
                }
            }
            else if (name == "SpotAngle")
            {
                if (TryParseFloatOrString(value, out float result))
                {
                    light.spotAngle = result;
                    properties["SpotAngle"] = result;
                    propertyChanged = true;
                }
            }

            if (propertyChanged)
            {
                UpdateVisualizersScale();
                RefreshVisibility();
                return true;
            }

            return base.SetProperty(name, value);
        }

        void ApplyLightType(Type newType)
        {
            if (newType == Type.SPOT)
                light.type = LightType.Spot;
            else
                light.type = LightType.Point;
        }

        private bool TryParseFloatOrString(object value, out float result)
        {
            result = 0f;
            if (value is float f)
            {
                result = f;
                return true;
            }
            else if (value is string s && Utils.TryParseFloat(s, out float parsed))
            {
                result = parsed;
                return true;
            }
            return false;
        }

        // --- Custom Visualization Logic (Editor Only) ---

        void CreateCustomVisualizers()
        {
            Material visMaterial = null;
            if (rangeSphere)
            {
                Renderer rend = rangeSphere.GetComponent<Renderer>();
                if (rend) visMaterial = rend.sharedMaterial;
            }

            // Generate a true Cone Mesh via code
            spotVisualizer = new GameObject("SpotVisualizer");
            spotVisualizer.transform.SetParent(transform);

            MeshFilter filter = spotVisualizer.AddComponent<MeshFilter>();
            filter.mesh = CreateNormalizedConeMesh();

            MeshRenderer renderer = spotVisualizer.AddComponent<MeshRenderer>();
            if (visMaterial != null) renderer.sharedMaterial = visMaterial;

            spotVisualizer.SetActive(false);
        }

        // Creates a simple cone pointing along the +Z axis.
        // Tip is at (0,0,0) and the base is at Z=1 with a Radius of 1.
        Mesh CreateNormalizedConeMesh(int segments = 24)
        {
            Mesh mesh = new Mesh();
            mesh.name = "GeneratedCone";

            Vector3[] vertices = new Vector3[segments + 2];
            int[] triangles = new int[(segments * 2) * 3];

            // Vertex 0 is the Tip
            vertices[0] = Vector3.zero;
            // Vertex 1 is the center of the base
            vertices[1] = new Vector3(0, 0, 1);

            // Generate the circular base vertices
            for (int i = 0; i < segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                vertices[i + 2] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 1);
            }

            int t = 0;
            for (int i = 0; i < segments; i++)
            {
                int current = i + 2;
                int next = (i + 1 == segments) ? 2 : current + 1;

                // Side triangles (Tip to Base)
                triangles[t++] = 0;
                triangles[t++] = next;
                triangles[t++] = current;

                // Bottom triangles (Base Center to Rim)
                triangles[t++] = 1;
                triangles[t++] = current;
                triangles[t++] = next;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            return mesh;
        }

        void UpdateVisualizersScale()
        {
            if (rangeSphere)
            {
                rangeSphere.transform.localScale = Vector3.one * light.range * 2;
                rangeSphere.transform.localPosition = Vector3.zero;
            }

            if (spotVisualizer)
            {
                // SpotAngle represents the entire cone angle. 
                // We use half the angle to calculate the radius of the base using basic trig.
                float radius = Mathf.Tan(light.spotAngle * 0.5f * Mathf.Deg2Rad) * light.range;

                // Because our custom mesh originates at (0,0,0) and extends 1 unit forward:
                // - Scaling X and Y scales the base radius perfectly.
                // - Scaling Z scales the exact distance (range).
                spotVisualizer.transform.localScale = new Vector3(radius, radius, light.range);

                // Keep it aligned perfectly with the light source.
                spotVisualizer.transform.localPosition = Vector3.zero;
                spotVisualizer.transform.localRotation = Quaternion.identity;
            }
        }

        void RefreshVisibility()
        {
            if (rangeSphere) rangeSphere.SetActive(false);
            if (spotVisualizer) spotVisualizer.SetActive(false);

            if (!isSelected) return;

            Type currentType = properties.ContainsKey("Type") ? (Type)properties["Type"] : Type.POINT;

            if (currentType == Type.SPOT && spotVisualizer)
                spotVisualizer.SetActive(true);
            else if (currentType == Type.POINT && rangeSphere)
                rangeSphere.SetActive(true);
        }

        public override void OnSelect()
        {
            base.OnSelect();
            isSelected = true;

            UpdateVisualizersScale();
            RefreshVisibility();
        }

        public override void OnDeselect(GameObject nextSelectedObj)
        {
            base.OnDeselect(nextSelectedObj);
            isSelected = false;
            RefreshVisibility();
        }

        public static new Color GetDefaultObjectColor(LEObjectContext context)
        {
            return new Color(0.7735849f, 0.7735849f, 0.1131185f, 0.03921569f);
        }
    }
}