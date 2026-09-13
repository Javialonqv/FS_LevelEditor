using System.Collections;
using UnityEngine;

namespace FS_LevelEditor
{
    public static class CoroutineUtils
    {
        static Dictionary<string, List<Coroutine>> executingCoroutinesWithIDs = new();

        public static void Start(IEnumerator coroutine)
        {
            NativeModLoader.Instance.StartCoroutine(coroutine);
        }
        public static object Start(IEnumerator coroutine, string id)
        {
            if (!executingCoroutinesWithIDs.ContainsKey(id))
                executingCoroutinesWithIDs.Add(id, new());

            Coroutine coroutineToken = NativeModLoader.Instance.StartCoroutine(coroutine);
            executingCoroutinesWithIDs[id].Add(coroutineToken);

            return coroutineToken;
        }

        public static void Stop(Coroutine coroutineToken)
        {
            foreach (var keyPair in executingCoroutinesWithIDs)
            {
                if (keyPair.Value.Remove(coroutineToken))
                {
                    break;
                }
            }

            NativeModLoader.Instance.StopCoroutine(coroutineToken);
        }
        public static void StopAllCoroutines(string coroutinesID)
        {
            if (!executingCoroutinesWithIDs.ContainsKey(coroutinesID))
                return;

            foreach (var coroutine in executingCoroutinesWithIDs[coroutinesID])
            {
                if (coroutine != null)
                    NativeModLoader.Instance.StopCoroutine(coroutine);
            }

            executingCoroutinesWithIDs.Remove(coroutinesID);
        }
    }
}
