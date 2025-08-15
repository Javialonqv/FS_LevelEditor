using UnityEngine;

namespace FS_LevelEditor.Editor
{
    // Draws Unity-style gizmo arrows with cones for each axis for object movement.
    public class EditorGizmo
    {
        private GameObject root;
        private GameObject xArrow, yArrow, zArrow;
        private GameObject xCone, yCone, zCone;
        private Material arrowMat;
        // Unity-like proportions, but bigger and thicker as requested
        private float arrowLength = 1.7f; // Bigger arrow
        private float arrowThickness = 0.12f; // Thicker line
        private float coneHeight = 0.45f; // Bigger cone
        private float coneRadius = 0.16f; // Wider cone

        public GameObject Root => root;
        public GameObject XArrow => xArrow;
        public GameObject YArrow => yArrow;
        public GameObject ZArrow => zArrow;
        public GameObject XCone => xCone;
        public GameObject YCone => yCone;
        public GameObject ZCone => zCone;

        public EditorGizmo(GameObject gizmoPrefab = null)
        {
            if (gizmoPrefab != null)
            {
                root = UnityEngine.Object.Instantiate(gizmoPrefab);
                root.name = gizmoPrefab.name;
                root.SetActive(false);
                xArrow = FindChildByName(root, "X");
                yArrow = FindChildByName(root, "Y");
                zArrow = FindChildByName(root, "Z");
                xCone = FindChildByName(root, "X_Cone");
                yCone = FindChildByName(root, "Y_Cone");
                zCone = FindChildByName(root, "Z_Cone");
            }
            else
            {
                CreateGizmo();
            }
            // --- Ensure cones are rendered after lines ---
            if (xCone != null) xCone.transform.SetAsLastSibling();
            if (yCone != null) yCone.transform.SetAsLastSibling();
            if (zCone != null) zCone.transform.SetAsLastSibling();
        }

        private void CreateGizmo()
        {
            root = new GameObject("EditorGizmo");
            root.SetActive(false);
            arrowMat = FS_LevelEditor.Editor.EditorController.GizmoArrowMaterial != null
                ? new Material(FS_LevelEditor.Editor.EditorController.GizmoArrowMaterial.shader)
                : CreateGizmoMaterial();

            // Unity gizmo colors
            Color xColor = new Color(0.89f, 0.27f, 0.20f, 1f); // Red
            Color yColor = new Color(0.25f, 0.78f, 0.35f, 1f); // Green
            Color zColor = new Color(0.20f, 0.52f, 0.89f, 1f); // Blue

            // X Axis
            xArrow = CreateAxis(Vector3.right, xColor, "X", arrowMat, out xCone);
            xArrow.transform.parent = root.transform;
            AddArrowCollider(xArrow, Vector3.right);
            AddConeCollider(xCone);
            // Y Axis
            yArrow = CreateAxis(Vector3.up, yColor, "Y", arrowMat, out yCone);
            yArrow.transform.parent = root.transform;
            AddArrowCollider(yArrow, Vector3.up);
            AddConeCollider(yCone);
            // Z Axis
            zArrow = CreateAxis(Vector3.forward, zColor, "Z", arrowMat, out zCone);
            zArrow.transform.parent = root.transform;
            AddArrowCollider(zArrow, Vector3.forward);
            AddConeCollider(zCone);
        }

        private Material CreateGizmoMaterial()
        {
            // Fallback: Use Unlit/Color if no custom material is available
            var mat = new Material(Shader.Find("Unlit/Color"));
            mat.SetInt("_ZWrite", 0);
            mat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            mat.renderQueue = 5000;
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.SetOverrideTag("RenderType", "Opaque");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            return mat;
        }

        // Creates an axis with a line and a cone at the end, returns the arrow GameObject and outputs the cone GameObject
        private GameObject CreateAxis(Vector3 dir, Color color, string axisName, Material sharedMat, out GameObject coneObj)
        {
            var go = new GameObject(axisName);
            var lr = go.AddComponent<LineRenderer>();
            float lineEnd = arrowLength; // End at cone base
            lr.positionCount = 2;
            lr.SetPosition(0, Vector3.zero);
            lr.SetPosition(1, dir * lineEnd);
            lr.startWidth = arrowThickness;
            lr.endWidth = arrowThickness;
            lr.material = new Material(sharedMat.shader);
            lr.material.CopyPropertiesFromMaterial(sharedMat);
            lr.material.color = color;
            // (Revert emission change: do not set _EmissionColor or enable keyword)
            lr.useWorldSpace = false;
            lr.numCapVertices = 8;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.sortingOrder = 32767; // Draw below cone
            // Add collider to the axis line itself (for raycast picking)
            AddArrowCollider(go, dir);
            // Cone at the end (drawn after line for render order)
            coneObj = CreateCone(dir, color, axisName + "_Cone", sharedMat);
            coneObj.transform.parent = go.transform;
            coneObj.transform.localPosition = dir * (lineEnd + coneHeight * 0.5f);
            coneObj.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir) * Quaternion.Euler(180, 0, 0); // 180 deg rotation
            coneObj.transform.localScale = new Vector3(coneRadius, coneHeight, coneRadius);
            // Set cone renderer to higher sorting order so it draws on top
            var coneRenderer = coneObj.GetComponent<Renderer>();
            if (coneRenderer != null) coneRenderer.sortingOrder = 32768;
            return go;
        }

        // Create a cone mesh for the arrow tip, facing the correct direction
        private GameObject CreateCone(Vector3 dir, Color color, string name, Material sharedMat = null)
        {
            var go = new GameObject(name);
            var meshFilter = go.AddComponent<MeshFilter>();
            var meshRenderer = go.AddComponent<MeshRenderer>();
            meshFilter.mesh = CreateClosedConeMesh(32);
            var coneMat = new Material((sharedMat ?? arrowMat).shader);
            coneMat.CopyPropertiesFromMaterial(sharedMat ?? arrowMat);
            coneMat.color = color;
            meshRenderer.material = coneMat;
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
        public void UpdateScaleByCamera(Camera camera, float baseDistance = 10f, float minScale = 0.7f, float maxScale = 3.5f)
        {
            if (root == null || camera == null) return;
            float dist = Vector3.Distance(camera.transform.position, root.transform.position);
            float scale = Mathf.Clamp(dist / baseDistance, minScale, maxScale);
            root.transform.localScale = Vector3.one * scale;
            float minConeScale = 0.22f;
            foreach (var cone in new[] { xCone, yCone, zCone })
            {
                if (cone != null)
                {
                    float coneScale = Mathf.Max(scale, minConeScale);
                    cone.transform.localScale = new Vector3(coneRadius, coneHeight, coneRadius) * coneScale / scale;
                    var parentArrow = cone.transform.parent;
                    if (parentArrow != null)
                    {
                        Vector3 dir = (cone == xCone) ? Vector3.right : (cone == yCone) ? Vector3.up : Vector3.forward;
                        float lineEnd = arrowLength;
                        cone.transform.localPosition = dir * (lineEnd + coneHeight * 0.5f * coneScale / scale);
                    }
                    // --- Set cones' renderQueue higher than lines ---
                    var meshRenderer = cone.GetComponent<MeshRenderer>();
                    if (meshRenderer != null)
                    {
                        meshRenderer.material.renderQueue = 3100;
                    }
                }
            }
            float minWidth = arrowThickness * 0.7f;
            float maxWidth = arrowThickness;
            float t = Mathf.Clamp01(dist / 10f);
            float lineWidth = Mathf.Lerp(minWidth, maxWidth, t);
            foreach (var lr in root.GetComponentsInChildren<LineRenderer>(true))
            {
                lr.startWidth = lineWidth;
                lr.endWidth = lineWidth;
                lr.material.renderQueue = 3000;
            }
            foreach (var arrow in new[] { xArrow, yArrow, zArrow })
            {
                var axisCollider = arrow.transform.Find("AxisCollider");
                if (axisCollider != null)
                {
                    var capsule = axisCollider.GetComponent<CapsuleCollider>();
                    if (capsule != null)
                    {
                        capsule.radius = Mathf.Max(arrowThickness * 1.2f * scale, 0.12f);
                        capsule.height = arrowLength + coneHeight * 0.9f;
                        capsule.enabled = true;
                    }
                }
            }
        }

        // Add a CapsuleCollider to the arrow for raycast detection
        private void AddArrowCollider(GameObject arrow, Vector3 dir)
        {
            // Remove all colliders from the arrow (no collider on the line itself)
            foreach (var col in arrow.GetComponents<Collider>())
                UnityEngine.Object.DestroyImmediate(col);
            var rb = arrow.GetComponent<Rigidbody>();
            if (rb) UnityEngine.Object.DestroyImmediate(rb);

            // Remove any previous AxisCollider child
            var oldAxisCollider = arrow.transform.Find("AxisCollider");
            if (oldAxisCollider != null)
                UnityEngine.Object.DestroyImmediate(oldAxisCollider.gameObject);

            // Create a new child for the collider
            var axisColliderObj = new GameObject("AxisCollider");
            axisColliderObj.transform.SetParent(arrow.transform, false);
            // Center the collider along the axis
            float totalLength = arrowLength + coneHeight * 0.9f; // cover line and most of cone
            axisColliderObj.transform.localPosition = dir * (totalLength / 2f);
            axisColliderObj.transform.localRotation = Quaternion.identity;
            // Add CapsuleCollider
            var capsule = axisColliderObj.AddComponent<CapsuleCollider>();
            capsule.radius = arrowThickness * 1.2f;
            capsule.height = totalLength;
            capsule.isTrigger = false;
            // Set direction: 0=X, 1=Y, 2=Z
            if (dir == Vector3.right) capsule.direction = 0;
            else if (dir == Vector3.up) capsule.direction = 1;
            else capsule.direction = 2;
            // Set layer to Default (0) for raycast
            axisColliderObj.layer = 0;
        }

        private void AddConeCollider(GameObject cone)
        {
            // Only visual, no collider on cone itself
            foreach (var col in cone.GetComponents<Collider>())
                UnityEngine.Object.DestroyImmediate(col);
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
    }
}
