using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace FS_LevelEditor.Editor
{
	/// <summary>
	/// Completely remade gizmo system with proper collision detection and responsive axis selection.
	/// Each axis has a single, properly-sized visual handle. Picking is done analytically using
	/// distance tests (no reliance on physics colliders) so it's forgiving and works at any angle/distance.
	/// </summary>
	public class EditorGizmo
	{
		// Core components
		private GameObject root;
		private AxisHandle xAxis, yAxis, zAxis;
		private Material arrowMat;
		private Vector3 pivotPosition;

		// Visual parameters
		private const float ARROW_LENGTH =1.7f;
		private const float ARROW_THICKNESS =0.08f;
		private const float CONE_HEIGHT =0.45f;
		private const float CONE_RADIUS =0.16f;

		// Picking tolerances (in screen-space pixels) - forgiving and scaled by distance
		private const float BASE_PIXEL_TOLERANCE =20f;
		private const float MIN_PIXEL_TOLERANCE =8f;
		private const float MAX_PIXEL_TOLERANCE =80f;

		// Scale management
		private const float REFERENCE_DISTANCE =10f;
		private const float MIN_SCALE =0.5f;
		private const float MAX_SCALE =4f;
		private float currentScale =1f;

		// Public accessors for EditorController
		public GameObject Root => root;
		public GameObject XArrow => xAxis?.ArrowObject;
		public GameObject YArrow => yAxis?.ArrowObject;
		public GameObject ZArrow => zAxis?.ArrowObject;
		public GameObject XCone => xAxis?.ConeObject;
		public GameObject YCone => yAxis?.ConeObject;
		public GameObject ZCone => zAxis?.ConeObject;

		/// <summary>
		/// Represents a single axis handle (arrow + cone). No physics colliders are used.
		/// </summary>
		private class AxisHandle
		{
			public GameObject ArrowObject;
			public GameObject ConeObject;
			public MeshRenderer ArrowRenderer;
			public MeshRenderer ConeRenderer;
			public Vector3 Direction;
			public Color BaseColor;
			public Color HighlightColor;
			public string Name;

			public AxisHandle(string name, Vector3 direction, Color color, Material baseMaterial)
			{
				Name = name;
				Direction = direction.normalized;
				BaseColor = color;
				HighlightColor = Color.Lerp(color, Color.white,0.6f);

				// Create arrow root
				ArrowObject = new GameObject(name);

				// Create arrow shaft (cylinder)
				GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
				shaft.name = "Shaft";
				shaft.transform.SetParent(ArrowObject.transform, false);
				UnityEngine.Object.DestroyImmediate(shaft.GetComponent<Collider>());

				float shaftRadius = ARROW_THICKNESS *0.5f;
				shaft.transform.localScale = new Vector3(shaftRadius, ARROW_LENGTH *0.5f, shaftRadius);
				shaft.transform.localPosition = direction * (ARROW_LENGTH *0.5f);
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
				ConeObject.transform.localPosition = direction * (ARROW_LENGTH + CONE_HEIGHT *0.5f);
				ConeObject.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction) * Quaternion.Euler(180,0,0);

				ConeRenderer = ConeObject.GetComponent<MeshRenderer>();
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
				Vector3[] vertices = new Vector3[segments +2];
				int[] triangles = new int[segments *3 + segments *3];
				float angleStep =2 * Mathf.PI / segments;

				vertices[0] = Vector3.zero; // tip
				for (int i =0; i < segments; i++)
				{
					float angle = i * angleStep;
					vertices[i +1] = new Vector3(Mathf.Cos(angle),1, Mathf.Sin(angle));
				}
				vertices[segments +1] = new Vector3(0,1,0); // base center

				// Side triangles
				for (int i =0; i < segments; i++)
				{
					triangles[i *3] =0;
					triangles[i *3 +1] = i +1;
					triangles[i *3 +2] = i +1 == segments ?1 : i +2;
				}

				// Base triangles
				int baseOffset = segments *3;
				for (int i =0; i < segments; i++)
				{
					triangles[baseOffset + i *3] = segments +1;
					triangles[baseOffset + i *3 +1] = i +1 == segments ?1 : i +2;
					triangles[baseOffset + i *3 +2] = i +1;
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
				// No physics colliders used; method kept to maintain API compatibility.
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
			Color xColor = new Color(0.89f,0.27f,0.20f,1f); // Red
			Color yColor = new Color(0.25f,0.78f,0.35f,1f); // Green
			Color zColor = new Color(0.20f,0.52f,0.89f,1f); // Blue

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
			mat.SetInt("_ZWrite",1);
			mat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
			mat.renderQueue =2000;
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

			// No physics colliders but keep API
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
			Camera cam = Camera.main;
			if (cam == null) return null;

			// Prepare world-space lines for each axis (from pivot to tip)
			var axes = new List<(AxisHandle axis, Vector3 worldStart, Vector3 worldEnd)>();

			float totalLen = ARROW_LENGTH + CONE_HEIGHT;
			Vector3 worldPos = root.transform.position;
			axes.Add((xAxis, worldPos, worldPos + root.transform.rotation * (xAxis.Direction * totalLen)));
			axes.Add((yAxis, worldPos, worldPos + root.transform.rotation * (yAxis.Direction * totalLen)));
			axes.Add((zAxis, worldPos, worldPos + root.transform.rotation * (zAxis.Direction * totalLen)));

			// Compute a pixel-space tolerance scaled by distance to pivot
			float distToPivot = Vector3.Distance(cam.transform.position, pivotPosition);
			float pixelTol = Mathf.Clamp(BASE_PIXEL_TOLERANCE * (distToPivot / REFERENCE_DISTANCE), MIN_PIXEL_TOLERANCE, MAX_PIXEL_TOLERANCE);

			var candidates = new List<(AxisHandle axis, float score, float distance, float pixelDistance)>();

			foreach (var entry in axes)
			{
				// Project a dense set of points along the axis to screen space and get minimal distance to mouse
				int samples =6; // keep small but enough to avoid deadzones
				float minPixelDist = float.MaxValue;
				float correspondingWorldDist = float.MaxValue;
				for (int s =0; s <= samples; s++)
				{
					float t = (float)s / samples;
					Vector3 worldPoint = Vector3.Lerp(entry.worldStart, entry.worldEnd, t);
					Vector3 screenPoint = cam.WorldToScreenPoint(worldPoint);
					if (screenPoint.z <0) continue; // behind the camera
					Vector2 sp = new Vector2(screenPoint.x, screenPoint.y);
					Vector2 mousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
					float pd = Vector2.Distance(sp, mousePos);
					if (pd < minPixelDist)
					{
						minPixelDist = pd;
						correspondingWorldDist = Vector3.Distance(cam.transform.position, worldPoint);
					}
				}

				if (minPixelDist <= pixelTol)
				{
					// Favor axes that are more perpendicular to view (more clearly visible), and closer pick
					Vector3 axisDirWorld = (entry.worldEnd - entry.worldStart).normalized;
					float viewDot = Mathf.Abs(Vector3.Dot(cam.transform.forward, axisDirWorld));
					float perpendicularScore =1f - viewDot; // higher when axis is perpendicular to view
					// Score incorporates perpendicularity and how close the pick is in pixels
					float score = perpendicularScore *2f + (pixelTol - minPixelDist) / pixelTol;
					candidates.Add((entry.axis, score, correspondingWorldDist, minPixelDist));
				}
			}

			if (candidates.Count ==0) return null;

			// Sort candidates: higher score first, then closer in world distance, then smaller pixel distance
			candidates.Sort((a, b) => {
				int sc = b.score.CompareTo(a.score);
				if (sc !=0) return sc;
				int wc = a.distance.CompareTo(b.distance);
				if (wc !=0) return wc;
				return a.pixelDistance.CompareTo(b.pixelDistance);
			});

			var best = candidates[0];
			hitDistance = best.distance;
			return best.axis.Name;
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
