using UnityEngine;
using UnityEngine.Rendering;

namespace FS_LevelEditor.Playmode
{
    /// <summary>
    /// EXPERIMENTAL. Playmode only: merges the meshes of purely static level geometry (grounds and walls) into a few big meshes, so they're drawn with far fewer draw calls.
    /// Only the visuals are merged (the original renderers get disabled), every original object, collider and component stays untouched.
    /// An object is NEVER merged if anything could change it while playing: waypoints, groups, being the target of an event, starting disabled, invisible mesh...
    /// Meshes are merged per material, shadow settings, layer and a spatial cell (so frustum culling still works). Controlled by the "CombineStaticMeshes" global property.
    /// </summary>
    public static class StaticMeshCombiner
    {
        const float CELL_SIZE = 40f;
        const string GLOBAL_PROPERTY = "CombineStaticMeshes";

        static readonly HashSet<LE_Object.ObjectType> combinableTypes = new HashSet<LE_Object.ObjectType>
        {
            LE_Object.ObjectType.GROUND,
            LE_Object.ObjectType.CYAN_GROUND,
            LE_Object.ObjectType.RED_GROUND,
            LE_Object.ObjectType.ORANGE_GROUND,
            LE_Object.ObjectType.LARGE_GROUND,
            LE_Object.ObjectType.GROUND_2,
            LE_Object.ObjectType.WALL,
            LE_Object.ObjectType.WALL_NO_COLOR,
            LE_Object.ObjectType.X_WALL,
            LE_Object.ObjectType.WINDOW,
        };

        struct BucketKey : IEquatable<BucketKey>
        {
            public Material material;
            public ShadowCastingMode shadows;
            public bool receiveShadows;
            public int layer;
            public Vector3Int cell;

            public bool Equals(BucketKey other)
            {
                return material == other.material && shadows == other.shadows && receiveShadows == other.receiveShadows && layer == other.layer && cell == other.cell;
            }
            public override bool Equals(object obj) => obj is BucketKey other && Equals(other);
            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = material != null ? material.GetInstanceID() : 0;
                    hash = hash * 31 + (int)shadows;
                    hash = hash * 31 + (receiveShadows ? 1 : 0);
                    hash = hash * 31 + layer;
                    hash = hash * 31 + cell.GetHashCode();
                    return hash;
                }
            }
        }

        class Bucket
        {
            public List<CombineInstance> instances = new List<CombineInstance>();
            public List<MeshRenderer> sources = new List<MeshRenderer>();
            public MeshRenderer firstSource;
        }

        public static bool IsEnabled(Dictionary<string, object> globalProperties)
        {
            return globalProperties != null && globalProperties.TryGetValue(GLOBAL_PROPERTY, out var value) && value is bool enabled && enabled;
        }

        public static void Combine(PlayModeController controller)
        {
            if (controller == null || !IsEnabled(controller.globalProperties)) return;

            System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();

            List<LE_Object> objects = controller.currentInstantiatedObjects;
            HashSet<(LE_Object.ObjectType, int)> targeted = GetTargetedObjects(objects);

            var buckets = new Dictionary<BucketKey, Bucket>();
            int candidateObjects = 0, skippedObjects = 0, skippedRenderers = 0;

            foreach (LE_Object obj in objects)
            {
                if (obj == null || !obj.objectType.HasValue || !combinableTypes.Contains(obj.objectType.Value)) continue;

                candidateObjects++;
                if (!IsStatic(obj, targeted))
                {
                    skippedObjects++;
                    continue;
                }

                foreach (MeshRenderer renderer in obj.GetComponentsInChildren<MeshRenderer>(false))
                {
                    if (!TryAddRenderer(renderer, buckets)) skippedRenderers++;
                }
            }

            GameObject root = new GameObject("LE_CombinedMeshes");
            root.transform.position = Vector3.zero;

            int combinedMeshes = 0, combinedRenderers = 0;
            foreach (var pair in buckets)
            {
                Bucket bucket = pair.Value;
                if (bucket.instances.Count < 2) continue;

                Mesh mesh = new Mesh();
                mesh.name = "LE_Combined_" + pair.Key.material.name;
                mesh.indexFormat = IndexFormat.UInt32;
                mesh.CombineMeshes(bucket.instances.ToArray(), true, true);
                mesh.UploadMeshData(true);

                GameObject combinedObj = new GameObject(mesh.name + "_" + pair.Key.cell);
                combinedObj.layer = pair.Key.layer;
                combinedObj.transform.SetParent(root.transform, false);
                combinedObj.AddComponent<MeshFilter>().sharedMesh = mesh;

                MeshRenderer combinedRenderer = combinedObj.AddComponent<MeshRenderer>();
                combinedRenderer.sharedMaterial = pair.Key.material;
                combinedRenderer.shadowCastingMode = pair.Key.shadows;
                combinedRenderer.receiveShadows = pair.Key.receiveShadows;
                combinedRenderer.lightProbeUsage = bucket.firstSource.lightProbeUsage;
                combinedRenderer.reflectionProbeUsage = bucket.firstSource.reflectionProbeUsage;
                combinedRenderer.allowOcclusionWhenDynamic = bucket.firstSource.allowOcclusionWhenDynamic;

                foreach (MeshRenderer source in bucket.sources) source.enabled = false;

                combinedMeshes++;
                combinedRenderers += bucket.sources.Count;
            }

            watch.Stop();
            Logger.Log($"[MeshCombiner] Combined {combinedRenderers} renderers into {combinedMeshes} meshes in {watch.Elapsed.TotalMilliseconds:F1} ms " +
                $"({candidateObjects} candidate objects, {skippedObjects} skipped as non-static, {skippedRenderers} renderers skipped).");
        }

        static bool TryAddRenderer(MeshRenderer renderer, Dictionary<BucketKey, Bucket> buckets)
        {
            if (renderer == null || !renderer.enabled || renderer.HasPropertyBlock()) return false;
            if (renderer.isPartOfStaticBatch || renderer.lightmapIndex >= 0) return false;

            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            if (mesh == null || !mesh.isReadable) return false;

            Material[] materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0 || materials.Length != mesh.subMeshCount) return false;

            foreach (Material material in materials)
            {
                if (material == null || material.renderQueue >= (int)RenderQueue.GeometryLast + 1) return false;
            }

            Matrix4x4 matrix = renderer.localToWorldMatrix;
            if (matrix.determinant <= 0f) return false;

            Vector3Int cell = new Vector3Int(
                Mathf.FloorToInt(renderer.bounds.center.x / CELL_SIZE),
                Mathf.FloorToInt(renderer.bounds.center.y / CELL_SIZE),
                Mathf.FloorToInt(renderer.bounds.center.z / CELL_SIZE));

            for (int i = 0; i < materials.Length; i++)
            {
                BucketKey key = new BucketKey
                {
                    material = materials[i],
                    shadows = renderer.shadowCastingMode,
                    receiveShadows = renderer.receiveShadows,
                    layer = renderer.gameObject.layer,
                    cell = cell
                };

                if (!buckets.TryGetValue(key, out Bucket bucket))
                {
                    bucket = new Bucket { firstSource = renderer };
                    buckets.Add(key, bucket);
                }

                bucket.instances.Add(new CombineInstance { mesh = mesh, subMeshIndex = i, transform = matrix });
                if (!bucket.sources.Contains(renderer)) bucket.sources.Add(renderer);
            }

            return true;
        }

        static bool IsStatic(LE_Object obj, HashSet<(LE_Object.ObjectType, int)> targeted)
        {
            if (!obj.gameObject.activeInHierarchy || !obj.setActiveAtStart) return false;
            if (obj.groupID.HasValue) return false;
            if (obj.invisibleMesh || obj.startMovingAtStart) return false;
            if (obj.waypoints != null && obj.waypoints.Count > 0) return false;
            if (obj.waypointSupport != null && obj.waypointSupport.targetWaypointsData != null && obj.waypointSupport.targetWaypointsData.Count > 0) return false;
            if (obj.customWaypointSupport != null && obj.customWaypointSupport.targetWaypointsData != null && obj.customWaypointSupport.targetWaypointsData.Count > 0) return false;
            if (obj.GetAvailableEventsIDs().Length > 0) return false;
            if (targeted.Contains((obj.objectType.Value, obj.objectID))) return false;

            return true;
        }

        static HashSet<(LE_Object.ObjectType, int)> GetTargetedObjects(List<LE_Object> objects)
        {
            var targeted = new HashSet<(LE_Object.ObjectType, int)>();

            foreach (LE_Object obj in objects)
            {
                if (obj == null || obj.properties == null) continue;

                foreach (var property in obj.properties)
                {
                    if (!(property.Value is List<LE_Event> events)) continue;

                    foreach (LE_Event @event in events)
                    {
                        if (@event != null && @event.targetObjType.HasValue) targeted.Add((@event.targetObjType.Value, @event.targetObjID));
                    }
                }
            }

            return targeted;
        }
    }
}
