using UnityEngine;

namespace FS_LevelEditor.Editor
{
	// Draws Unity-style gizmo arrows with cones for each axis for object movement.
	public class EditorGizmo
	{
		private GameObject root;
		private GameObject xArrow, yArrow, zArrow;
		private GameObject xCone, yCone, zCone; // restored cone references
		private Material arrowMat;
		private Vector3 pivotPosition; // logical position
		private float arrowLength = 1.7f;
		private float arrowThickness = 0.12f; // diameter, not radius
		private float coneHeight = 0.45f;
		private float coneRadius = 0.16f;

		// Constants for improved collider behavior
		private const float minWorldColliderRadius = 0.25f; // Minimum world-space hit radius
		private const float colliderRadiusMultiplier = 3.0f; // Increase hit area significantly

		public GameObject Root => root;
		public GameObject XArrow => xArrow;
		public GameObject YArrow => yArrow;
		public GameObject ZArrow => zArrow;
		public GameObject XCone => xCone; // public accessors for EditorController
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
		}

		private void CreateGizmo()
		{
			root = new GameObject("EditorGizmo");
			root.SetActive(false);
			arrowMat = FS_LevelEditor.Editor.EditorController.GizmoArrowMaterial != null
				? new Material(FS_LevelEditor.Editor.EditorController.GizmoArrowMaterial.shader)
				: CreateGizmoMaterial();

			Color xColor = new Color(0.89f, 0.27f, 0.20f, 1f);
			Color yColor = new Color(0.25f, 0.78f, 0.35f, 1f);
			Color zColor = new Color(0.20f, 0.52f, 0.89f, 1f);

			xArrow = CreateAxis(Vector3.right, xColor, "X", out xCone);
			xArrow.transform.parent = root.transform;
			yArrow = CreateAxis(Vector3.up, yColor, "Y", out yCone);
			yArrow.transform.parent = root.transform;
			zArrow = CreateAxis(Vector3.forward, zColor, "Z", out zCone);
			zArrow.transform.parent = root.transform;
		}

		private Material CreateGizmoMaterial()
		{
			var mat = new Material(Shader.Find("Unlit/Color"));
			mat.SetInt("_ZWrite", 1); // write depth to avoid fighting lines from transparency
			mat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
			mat.renderQueue = 2000; // opaque
			mat.SetOverrideTag("RenderType", "Opaque");
			return mat;
		}

		// Cylinder + cone version (no LineRenderer => no billboard artifacts)
		private GameObject CreateAxis(Vector3 dir, Color color, string axisName, out GameObject coneObj)
		{
			GameObject axisRoot = new GameObject(axisName);

			// Shaft
			GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
			shaft.name = "Shaft";
			shaft.transform.SetParent(axisRoot.transform, false);
			// Remove collider from primitive
			var col = shaft.GetComponent<Collider>();
			if (col) UnityEngine.Object.DestroyImmediate(col);
			// Orient & position so base at origin, tip at arrowLength
			float shaftRadius = arrowThickness * 0.5f; // radius not diameter
			shaft.transform.localScale = new Vector3(shaftRadius, arrowLength * 0.5f, shaftRadius);
			shaft.transform.localPosition = dir * (arrowLength * 0.5f);
			// Rotate so cylinder Y axis aligns with dir
			shaft.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);

			var shaftRenderer = shaft.GetComponent<MeshRenderer>();
			shaftRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			shaftRenderer.receiveShadows = false;
			shaftRenderer.material = new Material(arrowMat);
			shaftRenderer.material.color = color;

			// Cone at end
			coneObj = CreateCone(color);
			coneObj.name = axisName + "_Cone";
			coneObj.transform.SetParent(axisRoot.transform, false);
			coneObj.transform.localScale = new Vector3(coneRadius, coneHeight, coneRadius);
			coneObj.transform.localPosition = dir * (arrowLength + coneHeight * 0.5f);
			coneObj.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir) * Quaternion.Euler(180, 0, 0);

			AddArrowCollider(axisRoot, dir);
			// Add separate cone collider for better picking of the tip
			AddConeCollider(coneObj, dir);
			return axisRoot;
		}

		private GameObject CreateCone(Color color)
		{
			var go = new GameObject("Cone");
			var mf = go.AddComponent<MeshFilter>();
			var mr = go.AddComponent<MeshRenderer>();
			mf.mesh = CreateClosedConeMesh(32);
			mr.material = new Material(arrowMat);
			mr.material.color = color;
			mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			mr.receiveShadows = false;
			return go;
		}

		private Mesh CreateClosedConeMesh(int segments)
		{
			Mesh mesh = new Mesh();
			Vector3[] vertices = new Vector3[segments + 2];
			int[] triangles = new int[segments * 3 + segments * 3];
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
			if (root != null) root.SetActive(active);
		}

		public void SetPosition(Vector3 pos)
		{
			pivotPosition = pos;
			if (root != null) root.transform.position = pos;
		}

		public void SetRotation(Quaternion rot)
		{
			if (root != null) root.transform.rotation = rot;
		}

		public void SetScale(Vector3 scale)
		{
			if (root != null) root.transform.localScale = scale;
		}

		private const float cameraFacingOffset = 0.02f;

		public void UpdateScaleByCamera(Camera camera, float baseDistance = 10f, float minScale = 0.7f, float maxScale = 3.5f)
		{
			if (root == null || camera == null) return;
			float dist = Vector3.Distance(camera.transform.position, pivotPosition);
			float scale = Mathf.Clamp(dist / baseDistance, minScale, maxScale);
			root.transform.position = pivotPosition + camera.transform.forward * (cameraFacingOffset * scale);
			root.transform.localScale = Vector3.one * scale;

			// Adjust shaft radius smoothly (world constant picking feel)
			float t = Mathf.Clamp01(dist / 10f);
			float baseRadius = arrowThickness * 0.5f;
			float radius = Mathf.Lerp(baseRadius * 0.7f, baseRadius, t);
			AdjustAxisVisual(xArrow, radius);
			AdjustAxisVisual(yArrow, radius);
			AdjustAxisVisual(zArrow, radius);

			// Update colliders keeping constant world radius with minimum size guarantee
			foreach (var arrow in new[] { xArrow, yArrow, zArrow })
			{
				if (arrow == null) continue;
				var axisCollider = arrow.transform.Find("AxisCollider");
				if (axisCollider != null)
				{
					var capsule = axisCollider.GetComponent<CapsuleCollider>();
					if (capsule != null)
					{
						// Ensure minimum world-space picking radius
						float desiredWorldRadius = Mathf.Max(minWorldColliderRadius, baseRadius * colliderRadiusMultiplier);
						// Convert to local space - divide by scale
						capsule.radius = desiredWorldRadius / scale;
						capsule.height = (arrowLength + coneHeight * 0.9f) / scale;
					}
				}
			}

			// Update cone colliders separately for better tip picking
			foreach (var cone in new[] { xCone, yCone, zCone })
			{
				if (cone == null) continue;
				var coneCollider = cone.transform.Find("ConeCollider");
				if (coneCollider != null)
				{
					var sphereCol = coneCollider.GetComponent<SphereCollider>();
					if (sphereCol != null)
					{
						// Larger sphere for easier tip selection
						float desiredWorldRadius = Mathf.Max(minWorldColliderRadius * 1.5f, coneRadius * colliderRadiusMultiplier);
						sphereCol.radius = desiredWorldRadius / scale;
					}
				}
			}
		}

		private void AdjustAxisVisual(GameObject axis, float radius)
		{
			if (axis == null) return;
			var shaft = axis.transform.Find("Shaft");
			if (shaft != null)
			{
				// Cylinder height along local Y = 2 * scaleY
				shaft.localScale = new Vector3(radius, arrowLength * 0.5f, radius);
				shaft.localPosition = shaft.localRotation * (Vector3.up * (arrowLength * 0.5f)); // ensure remains centered if rotation changed
			}
			// Reposition cone in case of scale changes
			var cone = axis.transform.Find(axis.name + "_Cone");
			if (cone != null)
			{
				Vector3 dir = axis == xArrow ? Vector3.right : axis == yArrow ? Vector3.up : Vector3.forward;
				cone.localPosition = dir * (arrowLength + coneHeight * 0.5f);
			}
		}

		private void AddArrowCollider(GameObject arrow, Vector3 dir)
		{
			var old = arrow.transform.Find("AxisCollider");
			if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
			GameObject colObj = new GameObject("AxisCollider");
			colObj.transform.SetParent(arrow.transform, false);
			float totalLength = arrowLength + coneHeight * 0.9f;
			colObj.transform.localPosition = dir * (totalLength / 2f);
			var capsule = colObj.AddComponent<CapsuleCollider>();
			// Start with larger radius - will be adjusted dynamically in UpdateScaleByCamera
			capsule.radius = arrowThickness * 0.5f * colliderRadiusMultiplier;
			capsule.height = totalLength;
			capsule.isTrigger = false;
			if (dir == Vector3.right) capsule.direction = 0; 
			else if (dir == Vector3.up) capsule.direction = 1; 
			else capsule.direction = 2;
		}

		private void AddConeCollider(GameObject cone, Vector3 dir)
		{
			var old = cone.transform.Find("ConeCollider");
			if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
			GameObject colObj = new GameObject("ConeCollider");
			colObj.transform.SetParent(cone.transform, false);
			// Use sphere collider for the cone - easier to hit from all angles
			var sphereCol = colObj.AddComponent<SphereCollider>();
			// Positioned at tip of cone
			colObj.transform.localPosition = Vector3.zero;
			// Larger sphere for better picking
			sphereCol.radius = coneRadius * colliderRadiusMultiplier;
			sphereCol.isTrigger = false;
		}

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
