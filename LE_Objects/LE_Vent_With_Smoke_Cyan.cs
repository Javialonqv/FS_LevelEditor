using Il2Cpp;
using UnityEngine;

namespace FS_LevelEditor
{
	[MelonLoader.RegisterTypeInIl2Cpp]
	public class LE_Vent_With_Smoke_Cyan : LE_Object
	{
		void Awake()
		{
			gameObject.AddComponent<VentWithSmokeController>().m_particles = gameObject.GetChildAt("Content/Particles");
		}

        public override void SetCollidersStateForEdgeCase(bool newEnabledState)
        {
            contentObject.GetChild("Mesh").GetComponent<MeshCollider>().enabled = newEnabledState;
        }
    }
}