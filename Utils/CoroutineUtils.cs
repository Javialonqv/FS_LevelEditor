using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor
{
    public static class CoroutineUtils
    {
        static Dictionary<string, List<object>> executingCoroutinesWithIDs = new();

        public static void Start(IEnumerator coroutine)
        {
            MelonCoroutines.Start(coroutine);
        }
        public static object Start(IEnumerator coroutine, string id)
        {
            if (!executingCoroutinesWithIDs.ContainsKey(id))
                executingCoroutinesWithIDs.Add(id, new());

            object coroutineToken = MelonCoroutines.Start(coroutine);
            executingCoroutinesWithIDs[id].Add(coroutineToken);

            return coroutineToken;
        }

        public static void Stop(object coroutineToken)
        {
            foreach (var keyPair in executingCoroutinesWithIDs)
            {
                if (keyPair.Value.Remove(coroutineToken))
                {
                    break;
                }
            }

            MelonCoroutines.Stop(coroutineToken);
        }
        public static void StopAllCoroutines(string coroutinesID)
        {
            if (!executingCoroutinesWithIDs.ContainsKey(coroutinesID))
                return;

            foreach (var coroutine in executingCoroutinesWithIDs[coroutinesID])
            {
                if (coroutine != null)
                    MelonCoroutines.Stop(coroutine);
            }

            executingCoroutinesWithIDs.Remove(coroutinesID);
        }
    }
}
