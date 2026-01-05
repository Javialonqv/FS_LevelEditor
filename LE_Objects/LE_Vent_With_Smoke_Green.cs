using FS_LevelEditor.Editor;
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
        VentWithSmokeController script;

        GameObject particles;

        void Awake()
        {
            properties = new Dictionary<string, object>()
            {
                { "Particles", true }
            };

            particles = gameObject.GetChildAt("Content/Particles");
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
