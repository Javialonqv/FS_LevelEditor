using UnityEngine;

namespace FS_LevelEditor.Editor
{
    // Draws Unity/SEUM-style gizmo arrows with cones for each axis for object movement.
    public class EditorGizmo
    {
        private GameObject root;
        private GameObject xArrow, yArrow, zArrow;
        private GameObject xCone, yCone, zCone;
        private Material arrowMat;
        private float arrowLength = 1.6f; // Smaller for compact gizmo
        private float arrowThickness = 0.11f;
        private float coneHeight = 0.16f; // Smaller cone
        private float coneRadius = 0.13f; // Smaller cone

        public GameObject Root => root;
        public GameObject XArrow => xArrow;
        public GameObject YArrow => yArrow;
        public GameObject ZArrow => zArrow;
        public GameObject XCone => xCone;
        public GameObject YCone => yCone;
        public GameObject ZCone => zCone;

        // New: Accept a prefab for the gizmo root
        public EditorGizmo(GameObject gizmoPrefab = null)
        {
            if (gizmoPrefab != null)
            {
                // Instantiate the prefab and set up references
                root = UnityEngine.Object.Instantiate(gizmoPrefab);
                root.name = gizmoPrefab.name;
                root.SetActive(false);
                // Try to find arrows and cones by name
                xArrow = FindChildByName(root, "X");
                yArrow = FindChildByName(root, "Y");
                zArrow = FindChildByName(root, "Z");
                xCone = FindChildByName(root, "X_Cone");
                yCone = FindChildByName(root, "Y_Cone");
                zCone = FindChildByName(root, "Z_Cone");
                // Set correct shader/material for all renderers
                ApplyCorrectShaderToAllRenderers(root);
            }
            else
            {
                CreateGizmo();
            }
        }

        private void CreateGizmo()
        {
            root = new GameObject("EditorGizmo");
            root.SetActive(false);
            arrowMat = CreateGizmoMaterial();

            // X Axis
            Color xColor = new Color(1f, 0.5f, 0.5f, 0.85f);
            xArrow = CreateAxis(Vector3.right, xColor, "X_Axis");
            xArrow.transform.parent = root.transform;
            xCone = CreateCone(Vector3.right, xColor, "X_Cone");
            xCone.transform.parent = root.transform;
            // Y Axis
            Color yColor = new Color(0.7f, 1f, 0.7f, 0.85f);
            yArrow = CreateAxis(Vector3.up, yColor, "Y_Axis");
            yArrow.transform.parent = root.transform;
            yCone = CreateCone(Vector3.up, yColor, "Y_Cone");
            yCone.transform.parent = root.transform;
            // Z Axis
            Color zColor = new Color(0.7f, 0.9f, 1f, 0.85f);
            zArrow = CreateAxis(Vector3.forward, zColor, "Z_Axis");
            zArrow.transform.parent = root.transform;
            zCone = CreateCone(Vector3.forward, zColor, "Z_Cone");
            zCone.transform.parent = root.transform;
        }

        private Material CreateGizmoMaterial()
        {
            var mat = new Material(Shader.Find("Unlit/Color"));
            mat.SetInt("_ZWrite", 0); // Don't write to depth
            mat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always); // Always render on top
            mat.renderQueue = 5000; // Overlay
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            return mat;
        }

        private GameObject CreateAxis(Vector3 dir, Color color, string name)
        {
            var go = new GameObject(name);
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, Vector3.zero);
            lr.SetPosition(1, dir * (arrowLength - coneHeight * 0.7f));
            lr.startWidth = arrowThickness;
            lr.endWidth = arrowThickness;
            lr.material = CreateGizmoMaterial();
            lr.material.color = color;
            lr.useWorldSpace = false;
            lr.numCapVertices = 16;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.sortingOrder = 32767;
            return go;
        }

        // Create a cone mesh for the arrow tip, facing the correct direction
        private GameObject CreateCone(Vector3 dir, Color color, string name)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder); // Use cylinder as fallback
            go.name = name;
            Mesh coneMesh = CreateConeMesh(24);
            go.GetComponent<MeshFilter>().mesh = coneMesh;
            go.transform.localScale = new Vector3(coneRadius, coneHeight, coneRadius);
            go.transform.localPosition = dir * (arrowLength - coneHeight * 0.5f);
            // Rotate 180 degrees around the axis to flip the tip
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir) * Quaternion.AngleAxis(180, Vector3.right);
            go.GetComponent<Renderer>().material = CreateGizmoMaterial();
            go.GetComponent<Renderer>().material.color = color;
            go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.GetComponent<Renderer>().receiveShadows = false;
            UnityEngine.Object.Destroy(go.GetComponent<Collider>());
            return go;
        }

        // Standard cone mesh, tip at Vector3.zero, base at y=1
        private Mesh CreateConeMesh(int segments)
        {
            Mesh mesh = new Mesh();
            Vector3[] vertices = new Vector3[segments + 2];
            int[] triangles = new int[segments * 3];
            float angleStep = 2 * Mathf.PI / segments;
            vertices[0] = Vector3.zero; // tip
            for (int i = 0; i < segments; i++)
            {
                float angle = i * angleStep;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle), 1, Mathf.Sin(angle));
            }
            vertices[segments + 1] = new Vector3(0, 1, 0); // base center
            for (int i = 0; i < segments; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 1 == segments ? 1 : i + 2;
            }
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            return mesh;
        }

        public void SetActive(bool active)
        {
            if (root != null)
                root.SetActive(active);
        }

        public void SetPosition(Vector3 pos)
        {
            if (root != null)
                root.transform.position = pos;
        }

        public void SetRotation(Quaternion rot)
        {
            if (root != null)
                root.transform.rotation = rot;
        }

        public void SetScale(Vector3 scale)
        {
            if (root != null)
                root.transform.localScale = scale;
        }

        // Helper: Find child by name recursively
        private GameObject FindChildByName(GameObject parent, string name)
        {
            foreach (Transform child in parent.transform)
            {
                if (child.name == name) return child.gameObject;
                var found = FindChildByName(child.gameObject, name);
                if (found != null) return found;
            }
            return null;
        }

        // Helper: Set correct shader/material for all renderers
        private void ApplyCorrectShaderToAllRenderers(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                foreach (var mat in renderer.materials)
                {
                    if (mat.shader.name != "Unlit/Color")
                    {
                        mat.shader = Shader.Find("Unlit/Color");
                        mat.SetInt("_ZWrite", 0);
                        mat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
                        mat.renderQueue = 5000;
                        mat.EnableKeyword("_ALPHABLEND_ON");
                        mat.SetOverrideTag("RenderType", "Transparent");
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    }
                }
            }
        }
    }
}
