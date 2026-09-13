using System.Collections;
using UnityEngine;

namespace FS_LevelEditor.Misc
{

    public class ScaleTweener : MonoBehaviour
    {
        Coroutine scaleRoutine;

        public bool isPlaying = false;

        public static ScaleTweener ScaleTo(GameObject obj, Vector3 targetScale, float duration)
        {
            // Stop previous scale if exists.
            ScaleTweener existing = obj.GetComponent<ScaleTweener>();
            if (existing)
            {
                if (existing.scaleRoutine != null)
                    NativeModLoader.Instance.StopCoroutine(existing.scaleRoutine);

                existing.scaleRoutine = (Coroutine)NativeModLoader.Instance.StartCoroutine(existing.DoScale(targetScale, duration));
                return existing;
            }

            // Create new tweener.
            ScaleTweener tweener = obj.AddComponent<ScaleTweener>();
            tweener.scaleRoutine = (Coroutine)NativeModLoader.Instance.StartCoroutine(tweener.DoScale(targetScale, duration));

            return tweener;
        }

        IEnumerator DoScale(Vector3 targetScale, float duration)
        {
            Vector3 startScale = transform.localScale;

            float elapsed = 0f;
            isPlaying = true;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                try // To avoid a bug where this coroutine is still executing even after the object is destroyed (OnDestroy not being called propertly?)
                {
                    transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                }
                catch
                {
                    yield break;
                }
                yield return null;
            }

            transform.localScale = targetScale;
            isPlaying = false;

            scaleRoutine = null;
        }
        void OnDestroy()
        {
            if (scaleRoutine != null)
            {
                NativeModLoader.Instance.StopCoroutine(scaleRoutine);
            }
        }

        public static void StopRotation(GameObject obj)
        {
            ScaleTweener tweener = obj.GetComponent<ScaleTweener>();
            if (tweener && tweener.scaleRoutine != null)
            {
                NativeModLoader.Instance.StopCoroutine(tweener.scaleRoutine);
            }
        }
    }
}
