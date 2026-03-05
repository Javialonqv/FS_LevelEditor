using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using UnityEngine;

namespace FS_LevelEditor.Misc
{
    [RegisterTypeInIl2Cpp]
    public class GlobalScaleChanger : MonoBehaviour
    {
        public Transform relativeTo;
        public Vector3 globalScale = Vector3.one;

        public static GlobalScaleChanger AddTo(GameObject obj, Transform relativeTo, Vector3 globalScale, bool updateScaleNow = false)
        {
            var instance = obj.AddComponent<GlobalScaleChanger>();
            instance.relativeTo = relativeTo;
            instance.globalScale = globalScale;

            if (updateScaleNow) instance.LateUpdate();

            return instance;
        }

        void LateUpdate()
        {
            if (!relativeTo) return;

            Vector3 parentScale = relativeTo.localScale;
            if (parentScale.x == 0) parentScale.x = 1;
            if (parentScale.y == 0) parentScale.y = 1;
            if (parentScale.z == 0) parentScale.z = 1;
            transform.localScale = new Vector3(globalScale.x / parentScale.x, globalScale.y / parentScale.y, globalScale.z / parentScale.z);
        }
    }
}
