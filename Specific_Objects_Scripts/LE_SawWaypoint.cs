using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_SawWaypoint : LE_Object
    {
        void Awake()
        {
            
        }

        void Start()
        {
            if (PlayModeController.Instance != null)
            {

            }
            else // If it's not in playmode, just create a collider so the user can click the object in LE.
            {
                GameObject collider = new GameObject("Collider");
                collider.transform.parent = transform;
                collider.transform.localScale = Vector3.one;
                collider.transform.localPosition = Vector3.zero;
                collider.AddComponent<BoxCollider>().size = new Vector3(0.1f, 1.3f, 1.3f);
            }
        }
    }
}
