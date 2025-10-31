using Il2Cpp;
using MelonLoader;
using System.Collections;
using UnityEngine;

namespace FS_LevelEditor.WaypointSupports
{
    /// <summary>
    /// Applies waypoint rotation to saws/platforms AFTER they reach a waypoint, not during transit.
    /// This ensures objects only rotate to face movement direction while traveling, then smoothly
    /// rotate to the waypoint's rotation once arrived.
    /// </summary>
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class WaypointRotationApplier : MonoBehaviour
    {
        public Transform targetTransform; // The saw/platform's Content transform
        public WaypointSupport waypointSupport;

        private int lastWaypointIndex = -1;
        private Coroutine rotationCoroutine;
        private float rotationSpeed = 720f; // degrees per second - very fast but smooth

        void Update()
        {
            if (targetTransform == null || waypointSupport == null) return;
            if (waypointSupport.spawnedWaypoints == null || waypointSupport.spawnedWaypoints.Count == 0) return;

            // Find which waypoint we're currently at or moving towards
            int currentWaypointIndex = FindCurrentWaypointIndex();

            if (currentWaypointIndex != lastWaypointIndex && currentWaypointIndex >= 0)
            {
                // We've reached a new waypoint, apply its rotation
                ApplyWaypointRotation(currentWaypointIndex);
                lastWaypointIndex = currentWaypointIndex;
            }
        }

        int FindCurrentWaypointIndex()
        {
            float closestDistance = float.MaxValue;
            int closestIndex = -1;

            for (int i = 0; i < waypointSupport.spawnedWaypoints.Count; i++)
            {
                var waypoint = waypointSupport.spawnedWaypoints[i];
                if (waypoint == null) continue;

                float distance = Vector3.Distance(targetTransform.position, waypoint.transform.position);

                // Consider "reached" if within 0.1 units
                if (distance < 0.1f && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestIndex = i;
                }
            }

            return closestIndex;
        }

        void ApplyWaypointRotation(int waypointIndex)
        {
            if (waypointIndex < 0 || waypointIndex >= waypointSupport.spawnedWaypoints.Count) return;

            var waypoint = waypointSupport.spawnedWaypoints[waypointIndex];
            if (waypoint == null) return;

            // Get the target rotation from the waypoint
            Quaternion targetRotation = waypoint.transform.rotation;

            // Stop any existing rotation
            if (rotationCoroutine != null)
            {
                MelonCoroutines.Stop(rotationCoroutine);
            }

            // Start smooth rotation to target
            rotationCoroutine = (Coroutine)MelonCoroutines.Start(SmoothRotateToTarget(targetRotation));
        }

        IEnumerator SmoothRotateToTarget(Quaternion targetRotation)
        {
            Quaternion startRotation = targetTransform.rotation;
            float angle = Quaternion.Angle(startRotation, targetRotation);

            if (angle < 0.1f)
            {
                // Already at target rotation
                yield break;
            }

            float duration = angle / rotationSpeed;
            // Clamp minimum duration to ensure it's smooth but very fast
            duration = Mathf.Max(duration, 0.05f); // At least 50ms for smoothness
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Smooth step interpolation for silky smooth rotation
                t = t * t * (3f - 2f * t);

                targetTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
                yield return null;
            }

            // Ensure we end exactly at target
            targetTransform.rotation = targetRotation;
            rotationCoroutine = null;
        }

        void OnDestroy()
        {
            if (rotationCoroutine != null)
            {
                MelonCoroutines.Stop(rotationCoroutine);
            }
        }
    }
}
