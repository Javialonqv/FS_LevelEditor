using FS_LevelEditor.Editor;
using Il2Cpp;
using UnityEngine;

namespace FS_LevelEditor
{
	[MelonLoader.RegisterTypeInIl2Cpp]
	public class LE_Vent_With_Smoke_Cyan : LE_Object
	{
		VentWithSmokeController script;

		GameObject particles;

		void Awake()
		{
			particles = gameObject.GetChildAt("Content/Particles");
		}

        public static Dictionary<string, object> GetDefaultProperties()
        {
            return new Dictionary<string, object>()
            {
                { "Particles", true }
            };
        }

        public override void OnInstantiated(LEScene scene)
        {
			if (scene == LEScene.Editor)
			{
				UpdateParticlesStateInEditor(GetProperty<bool>("Particles"));
			}

            base.OnInstantiated(scene);
        }

        public override void InitComponent()
        {
			script = contentObject.AddComponent<VentWithSmokeController>();
			script.m_particles = particles;
            script.UpdateParticlesAllowed(GetProperty<bool>("Particles"));

            base.InitComponent();
        }

        public override bool SetProperty(string name, object value)
        {
			if (name == "Particles")
			{
				if (value is bool boolValue)
				{
					properties["Particles"] = boolValue;
					if (EditorController.Instance) UpdateParticlesStateInEditor(boolValue);
				}
			}

            return base.SetProperty(name, value);
        }
		void UpdateParticlesStateInEditor(bool enabled)
		{
			particles.SetActive(enabled);
		}

        public override void SetCollidersStateForEdgeCase(bool newEnabledState)
        {
            contentObject.GetChild("Mesh").GetComponent<MeshCollider>().enabled = newEnabledState;
        }
    }
}