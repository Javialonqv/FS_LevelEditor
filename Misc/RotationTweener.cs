using MelonLoader;
using System.Collections;
using UnityEngine;

public enum RotationPath
{
    Shortest,
    Longest
}

[RegisterTypeInIl2Cpp]
public class RotationTweener : MonoBehaviour
{
    Coroutine rotationCoroutine;

    public bool isPlaying = false;

    public static RotationTweener RotateTo(GameObject obj, Vector3 targetEuler, float duration, RotationPath path = RotationPath.Shortest)
    {
        // Stop previous rotation if exists.
        RotationTweener existing = obj.GetComponent<RotationTweener>();
        if (existing)
        {
            if (existing.rotationCoroutine != null) MelonCoroutines.Stop(existing.rotationCoroutine);
            existing.rotationCoroutine = (Coroutine)MelonCoroutines.Start(existing.DoRotation(targetEuler, duration, path));
            return existing;
        }

        // Create new tweener.
        RotationTweener tweener = obj.AddComponent<RotationTweener>();
        tweener.rotationCoroutine = (Coroutine)MelonCoroutines.Start(tweener.DoRotation(targetEuler, duration, path));

        return tweener;
    }

    private IEnumerator DoRotation(Vector3 targetEuler, float duration, RotationPath path)
    {
        Quaternion startRot = transform.rotation;
        Quaternion targetRot = Quaternion.Euler(targetEuler);

        // If we want the longest way, invert one of the quaternions.
        if (path == RotationPath.Longest)
        {
            if (Quaternion.Dot(startRot, targetRot) > 0f)
            {
                targetRot = new Quaternion(-targetRot.x, -targetRot.y, -targetRot.z, -targetRot.w);
            }
        }
        else
        {
            if (Quaternion.Dot(startRot, targetRot) < 0f)
            {
                targetRot = new Quaternion(-targetRot.x, -targetRot.y, -targetRot.z, -targetRot.w);
            }
        }

        float elapsed = 0f;
        isPlaying = true;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            //t = Mathf.SmoothStep(0f, 1f, t);

            try // To avoid a bug where this coroutine is still executing even after the object is destroyed (OnDestroy not being called propertly?)
            {
                transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            }
            catch
            {
                //OnDestroy();
                yield break;
            }
            yield return null;
        }

        transform.rotation = Quaternion.Euler(targetEuler);
        isPlaying = false;

        rotationCoroutine = null;
        //DestroyImmediate(this);
    }
    void OnDestroy()
    {
        if (rotationCoroutine != null)
        {
            MelonCoroutines.Stop(rotationCoroutine);
        }
    }

    public static void StopRotation(GameObject obj)
    {
        RotationTweener tweener = obj.GetComponent<RotationTweener>();
        if (tweener && tweener.rotationCoroutine != null)
        {
            MelonCoroutines.Stop(tweener.rotationCoroutine);
            //DestroyImmediate(tweener);
        }
    }
}