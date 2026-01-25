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
        GameObject light;

        void Awake()
        {
            particles = gameObject.GetChildAt("Content/Particles");
            light = gameObject.GetChildAt("Content/CollectibleHealth_Baked_Spawn_Light");
        }

        public static Dictionary<string, object> GetDefaultProperties()
        {
            return new Dictionary<string, object>()
            {
                { "Particles", true },
                { "Light", true }
            };
        }

        public override void OnInstantiated(LEScene scene)
        {
            if (scene == LEScene.Editor)
            {
                UpdateParticlesStateInEditor(GetProperty<bool>("Particles"));
                SetLightState(GetProperty<bool>("Light"));
            }

            base.OnInstantiated(scene);
        }

        public override void InitComponent()
        {
            script = contentObject.AddComponent<VentWithSmokeController>();
            script.m_particles = particles;
            script.UpdateParticlesAllowed(GetProperty<bool>("Particles"));

            SetLightState(GetProperty<bool>("Light"));

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
            else if (name == "Light")
            {
                if (value is bool boolValue)
                {
                    properties["Light"] = boolValue;
                    if (EditorController.Instance) SetLightState(boolValue);
                }
            }

            return base.SetProperty(name, value);
        }
        void UpdateParticlesStateInEditor(bool enabled)
        {
            particles.SetActive(enabled);
        }

        void SetLightState(bool enabled)
        {
            light.SetActive(enabled);
        }

        public override void SetCollidersStateForEdgeCase(bool newEnabledState)
        {
            contentObject.GetChild("Mesh").GetComponent<MeshCollider>().enabled = newEnabledState;
        }
    }
}
