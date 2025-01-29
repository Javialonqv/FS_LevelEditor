using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Vent_With_Smoke_Green : LE_Object
    {
        void Awake()
        {
            gameObject.AddComponent<VentWithSmokeController>().m_particles = gameObject.GetChildWithName("Particles");
        }
    }
}
