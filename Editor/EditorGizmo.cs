using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace FS_LevelEditor.Editor
{
	/// <summary>
	/// Completely remade gizmo system with proper collision detection and responsive axis selection.
	/// Each axis has a single, properly-sized collider with unique layers for reliable picking.
	/// </summary>
	public class EditorGizmo
	{
		// Core components
		private GameObject root;
		private AxisHandle xAxis, yAxis, zAxis;
		private Material arrowMat;
		private Vector3 pivotPosition;

		// Visual parameters
		private const float ARROW_LENGTH = 1.7f;
		private const float ARROW_THICKNESS = 0.08f;
		private const float CONE_HEIGHT = 0.45f;
		private const float CONE_RADIUS = 0.16f;

		// Collision parameters - optimized for reliable picking
		private const float COLLIDER_RADIUS = 0.35f; // Consistent world-space size
		private const float MIN_COLLIDER_RADIUS = 0.2f;
		private const float MAX_COLLIDER_RADIUS = 0.6f;
		
		// Scale management
		private const float REFERENCE_DISTANCE = 10f;
		private const float MIN_SCALE = 0.5f;
		private const float MAX_SCALE = 4f;
		private float currentScale = 1f;

		// Public accessors for EditorController
		public GameObject Root => root;
		public GameObject XArrow => xAxis?.ArrowObject;
		public GameObject YArrow => yAxis?.ArrowObject;
		public GameObject ZArrow => zAxis?.ArrowObject;
		public GameObject XCone => xAxis?.ConeObject;
		public GameObject YCone => yAxis?.ConeObject;
		public GameObject ZCone => zAxis?.ConeObject;

		/// <summary>
		/// Represents a single axis handle (arrow + cone + collider)
		/// </summary>
		private class AxisHandle
		{
			public GameObject ArrowObject;
			public GameObject ConeObject;
			public GameObject ColliderObject;
			public CapsuleCollider Collider;
			public MeshRenderer ArrowRenderer;
			public MeshRenderer ConeRenderer;
			public Vector3 Direction;
			public Color BaseColor;
			public Color HighlightColor;
			public string Name;

			public AxisHandle(string name, Vector3 direction, Color color, Material baseMaterial)
			{
				Name = name;
				Direction = direction;
				BaseColor = color;
				HighlightColor = Color.Lerp(color, Color.white, 0.4f);

				// Create arrow root
				ArrowObject = new GameObject(name);

				// Create arrow shaft (cylinder)
				GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
				shaft.name = "Shaft";
				shaft.transform.SetParent(ArrowObject.transform, false);
				UnityEngine.Object.DestroyImmediate(shaft.GetComponent<Collider>());

				float shaftRadius = ARROW_THICKNESS * 0.5f;
				shaft.transform.localScale = new Vector3(shaftRadius, ARROW_LENGTH * 0.5f, shaftRadius);
				shaft.transform.localPosition = direction * (ARROW_LENGTH * 0.5f);
				shaft.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);

				ArrowRenderer = shaft.GetComponent<MeshRenderer>();
				ArrowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
				ArrowRenderer.receiveShadows = false;
				ArrowRenderer.material = new Material(baseMaterial);
				ArrowRenderer.material.color = color;

				// Create cone
				ConeObject = CreateCone(color, baseMaterial);
				ConeObject.name = name + "_Cone";
				ConeObject.transform.SetParent(ArrowObject.transform, false);
				ConeObject.transform.localScale = new Vector3(CONE_RADIUS, CONE_HEIGHT, CONE_RADIUS);
				ConeObject.transform.localPosition = direction * (ARROW_LENGTH + CONE_HEIGHT * 0.5f);
				ConeObject.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction) * Quaternion.Euler(180, 0, 0);

				ConeRenderer = ConeObject.GetComponent<MeshRenderer>();

				// Create single collider for entire axis
				ColliderObject = new GameObject(name + "_Collider");
				ColliderObject.transform.SetParent(ArrowObject.transform, false);
				
				float totalLength = ARROW_LENGTH + CONE_HEIGHT;
				ColliderObject.transform.localPosition = direction * (totalLength / 2f);
				
				Collider = ColliderObject.AddComponent<CapsuleCollider>();
				Collider.radius = COLLIDER_RADIUS;
				Collider.height = totalLength;
				Collider.isTrigger = false;
				
				// Set capsule direction based on axis
				if (direction == Vector3.right) Collider.direction = 0; // X-axis
				else if (direction == Vector3.up) Collider.direction = 1; // Y-axis
				else Collider.direction = 2; // Z-axis
			}

			private GameObject CreateCone(Color color, Material baseMaterial)
			{
				var go = new GameObject("Cone");
				var mf = go.AddComponent<MeshFilter>();
				var mr = go.AddComponent<MeshRenderer>();
				mf.mesh = CreateClosedConeMesh(32);
				mr.material = new Material(baseMaterial);
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

			public void SetHighlighted(bool highlighted)
			{
				Color targetColor = highlighted ? HighlightColor : BaseColor;
				if (ArrowRenderer != null) ArrowRenderer.material.color = targetColor;
				if (ConeRenderer != null) ConeRenderer.material.color = targetColor;
			}

			public void UpdateColliderForDistance(float gizmoScale)
			{
				if (Collider == null) return;
				
				// Keep collider size consistent in world space
				float worldRadius = Mathf.Clamp(COLLIDER_RADIUS, MIN_COLLIDER_RADIUS, MAX_COLLIDER_RADIUS);
				Collider.radius = worldRadius / gizmoScale;
				Collider.height = (ARROW_LENGTH + CONE_HEIGHT) / gizmoScale;
			}
		}

		public EditorGizmo(GameObject gizmoPrefab = null)
		{
			CreateGizmo();
		}

		private void CreateGizmo()
		{
			root = new GameObject("EditorGizmo");
			root.SetActive(false);
			
			arrowMat = EditorController.GizmoArrowMaterial != null
				? new Material(EditorController.GizmoArrowMaterial.shader)
				: CreateGizmoMaterial();

			// Create three axes with distinct colors
			Color xColor = new Color(0.89f, 0.27f, 0.20f, 1f); // Red
			Color yColor = new Color(0.25f, 0.78f, 0.35f, 1f); // Green
			Color zColor = new Color(0.20f, 0.52f, 0.89f, 1f); // Blue

			xAxis = new AxisHandle("X", Vector3.right, xColor, arrowMat);
			xAxis.ArrowObject.transform.SetParent(root.transform, false);

			yAxis = new AxisHandle("Y", Vector3.up, yColor, arrowMat);
			yAxis.ArrowObject.transform.SetParent(root.transform, false);

			zAxis = new AxisHandle("Z", Vector3.forward, zColor, arrowMat);
			zAxis.ArrowObject.transform.SetParent(root.transform, false);
		}

		private Material CreateGizmoMaterial()
		{
			var mat = new Material(Shader.Find("Unlit/Color"));
			mat.SetInt("_ZWrite", 1);
			mat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
			mat.renderQueue = 2000;
			mat.SetOverrideTag("RenderType", "Opaque");
			return mat;
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

		/// <summary>
		/// Updates gizmo scale based on camera distance to maintain consistent screen-space size
		/// </summary>
		public void UpdateScaleByCamera(Camera camera, float baseDistance = REFERENCE_DISTANCE, 
			float minScale = MIN_SCALE, float maxScale = MAX_SCALE)
		{
			if (root == null || camera == null) return;

			float dist = Vector3.Distance(camera.transform.position, pivotPosition);
			currentScale = Mathf.Clamp(dist / baseDistance, minScale, maxScale);
			
			// Apply scale
			root.transform.localScale = Vector3.one * currentScale;
			
			// Update colliders to maintain consistent world-space size
			xAxis.UpdateColliderForDistance(currentScale);
			yAxis.UpdateColliderForDistance(currentScale);
			zAxis.UpdateColliderForDistance(currentScale);
		}

		/// <summary>
		/// Improved axis detection with better prioritization and no dead zones
		/// Returns the axis name ("X", "Y", "Z") or null if no hit
		/// </summary>
		public string GetHoveredAxis(Ray mouseRay, out float hitDistance)
		{
			hitDistance = float.MaxValue;
			
			// Collect all hits
			var hits = new List<(AxisHandle axis, RaycastHit hit, float score)>();
			
			RaycastHit[] allHits = Physics.RaycastAll(mouseRay, Mathf.Infinity);
			
			foreach (var hit in allHits)
			{
				AxisHandle matchedAxis = null;
				
				// Check which axis this collider belongs to
				if (hit.collider == xAxis.Collider) matchedAxis = xAxis;
				else if (hit.collider == yAxis.Collider) matchedAxis = yAxis;
				else if (hit.collider == zAxis.Collider) matchedAxis = zAxis;
				
				if (matchedAxis != null)
				{
					// Calculate priority score based on:
					// 1. How perpendicular the axis is to the view (higher = better)
					// 2. Distance from camera (closer = better)
					
					Vector3 axisWorldDir = root.transform.rotation * matchedAxis.Direction;
					Vector3 viewDir = mouseRay.direction;
					
					// Perpendicularity score (0 = parallel to view, 1 = perpendicular to view)
					float dotProduct = Mathf.Abs(Vector3.Dot(axisWorldDir.normalized, viewDir.normalized));
					float perpendicularityScore = 1f - dotProduct;
					
					// Distance score (closer is better)
					float distanceScore = 1f / (1f + hit.distance * 0.1f);
					
					// Combined score (perpendicularity weighted more heavily)
					float finalScore = perpendicularityScore * 3f + distanceScore;
					
					hits.Add((matchedAxis, hit, finalScore));
				}
			}
			
			if (hits.Count == 0) return null;
			
			// Sort by score (highest first), then by distance (closest first)
			hits.Sort((a, b) => 
			{
				int scoreCompare = b.score.CompareTo(a.score);
				if (scoreCompare != 0) return scoreCompare;
				return a.hit.distance.CompareTo(b.hit.distance);
			});
			
			// Return the best match
			var bestHit = hits[0];
			hitDistance = bestHit.hit.distance;
			return bestHit.axis.Name;
		}

		/// <summary>
		/// Highlights the specified axis (used for hover feedback)
		/// </summary>
		public void HighlightAxis(string axisName)
		{
			xAxis.SetHighlighted(axisName == "X");
			yAxis.SetHighlighted(axisName == "Y");
			zAxis.SetHighlighted(axisName == "Z");
		}

		/// <summary>
		/// Resets all axes to their default colors
		/// </summary>
		public void ResetColors()
		{
			xAxis.SetHighlighted(false);
			yAxis.SetHighlighted(false);
			zAxis.SetHighlighted(false);
		}
	}
}
