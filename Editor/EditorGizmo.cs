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
        private float arrowLength = 2.2f; // Increased for bigger gizmo
        private float arrowThickness = 0.18f; // Thicker lines
        private float coneHeight = 0.22f; // Bigger cone
        private float coneRadius = 0.18f; // Bigger cone

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
                // Use the X, Y, Z objects from the prefab directly for each axis
                xArrow = FindChildByName(root, "X");
                yArrow = FindChildByName(root, "Y");
                zArrow = FindChildByName(root, "Z");
                xCone = FindChildByName(root, "X_Cone");
                yCone = FindChildByName(root, "Y_Cone");
                zCone = FindChildByName(root, "Z_Cone");
                // Do NOT apply the X axis shader/material to all axes anymore
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
            // Use the extracted material if available, otherwise fallback
            arrowMat = FS_LevelEditor.Editor.EditorController.GizmoArrowMaterial != null
                ? new Material(FS_LevelEditor.Editor.EditorController.GizmoArrowMaterial.shader)
                : CreateGizmoMaterial();

            // X Axis
            Color xColor = new Color(1f, 0.5f, 0.5f, 0.85f);
            xArrow = CreateAxis(Vector3.right, xColor, "X_Axis", arrowMat);
            xArrow.transform.parent = root.transform;
            xCone = CreateCone(Vector3.right, xColor, "X_Cone", arrowMat);
            xCone.transform.parent = root.transform;
            // Y Axis
            Color yColor = new Color(0.5f, 1f, 0.5f, 0.85f);
            yArrow = CreateAxis(Vector3.up, yColor, "Y_Axis", arrowMat);
            yArrow.transform.parent = root.transform;
            yCone = CreateCone(Vector3.up, yColor, "Y_Cone", arrowMat);
            yCone.transform.parent = root.transform;
            // Z Axis (Cyan: must mix red due to base color bug)
            Color zColor = new Color(0.0f, 0.5f, 1.0f, 0.85f); // Pure cyan (green+blue, no red)
            zArrow = CreateAxis(Vector3.forward, zColor, "Z_Axis", arrowMat);
            zArrow.transform.parent = root.transform;
            zCone = CreateCone(Vector3.forward, zColor, "Z_Cone", arrowMat);
            zCone.transform.parent = root.transform;
        }

        private Material CreateGizmoMaterial(bool forCone = false)
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

        private GameObject CreateAxis(Vector3 dir, Color color, string name, Material sharedMat)
        {
            var go = new GameObject(name);
            var lr = go.AddComponent<LineRenderer>();
            float lineEnd = arrowLength - coneHeight * 0.5f; // End at cone base
            lr.positionCount = 2;
            lr.SetPosition(0, Vector3.zero);
            lr.SetPosition(1, dir * lineEnd);
            lr.startWidth = arrowThickness;
            lr.endWidth = arrowThickness;
            lr.material = sharedMat;
            lr.material.color = color;
            lr.useWorldSpace = false;
            lr.numCapVertices = 16;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.sortingOrder = 32767;
            // Create and parent the cone to the end of the line
            var cone = CreateCone(dir, color, name + "_Cone", sharedMat);
            cone.transform.parent = go.transform;
            cone.transform.localPosition = dir * lineEnd;
            cone.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir) * Quaternion.AngleAxis(180, Vector3.right);
            cone.transform.localScale = new Vector3(coneRadius, coneHeight, coneRadius);
            return go;
        }

        // Create a cone mesh for the arrow tip, facing the correct direction
        private GameObject CreateCone(Vector3 dir, Color color, string name, Material sharedMat = null)
        {
            var go = new GameObject(name);
            var meshFilter = go.AddComponent<MeshFilter>();
            var meshRenderer = go.AddComponent<MeshRenderer>();
            meshFilter.mesh = CreateClosedConeMesh(24);
            var coneMat = sharedMat ?? CreateGizmoMaterial();
            meshRenderer.material = coneMat;
            meshRenderer.material.color = color;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            return go;
        }

        // Standard cone mesh, tip at Vector3.zero, base at y=1, with closed base
        private Mesh CreateClosedConeMesh(int segments)
        {
            Mesh mesh = new Mesh();
            Vector3[] vertices = new Vector3[segments + 2];
            int[] triangles = new int[segments * 3 + segments * 3]; // side + base
            float angleStep = 2 * Mathf.PI / segments;
            vertices[0] = Vector3.zero; // tip
            for (int i = 0; i < segments; i++)
            {
                float angle = i * angleStep;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle), 1, Mathf.Sin(angle));
            }
            vertices[segments + 1] = new Vector3(0, 1, 0); // base center
            // Side triangles
            for (int i = 0; i < segments; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 1 == segments ? 1 : i + 2;
            }
            // Base triangles
            int baseOffset = segments * 3;
            for (int i = 0; i < segments; i++)
            {
                triangles[baseOffset + i * 3] = segments + 1;
                triangles[baseOffset + i * 3 + 1] = i + 1 == segments ? 1 : i + 2;
                triangles[baseOffset + i * 3 + 2] = i + 1;
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

        // Call this in LateUpdate from EditorController or wherever the gizmo is managed
        public void UpdateScaleByCamera(Camera camera, float baseDistance = 10f, float minScale = 1.0f, float maxScale = 3.5f)
        {
            if (root == null || camera == null) return;
            float dist = Vector3.Distance(camera.transform.position, root.transform.position);
            float scale = Mathf.Clamp(dist / baseDistance, minScale, maxScale);
            root.transform.localScale = Vector3.one * scale;
            // Keep line width constant regardless of distance
            foreach (var lr in root.GetComponentsInChildren<LineRenderer>(true))
            {
                lr.startWidth = arrowThickness;
                lr.endWidth = arrowThickness;
            }
        }

        // Helper: Find child by name recursively
        private GameObject FindChildByName(GameObject parent, string name)
        {
            var t = parent.transform;
            for (int i = 0; i < t.childCount; i++)
            {
                var child = t.GetChild(i);
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
