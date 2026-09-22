using System.Collections.Generic;
using UnityEngine;
using FS_LevelEditor.Editor;

namespace FS_LevelEditor
{
    public class LE_Health_Pack : LE_Object
    {
        Health health;
        Light healthLight;
        GameObject healthFlare;

        void Awake()
        {
            healthLight = gameObject.GetChildAt("Content/Mesh/PC_Only").GetComponent<Light>();
            healthFlare = gameObject.GetChildAt("Content/Mesh/HealthFlare");
        }

        public static Dictionary<string, object> GetDefaultProperties()
        {
            return new Dictionary<string, object>()
            {
                { "RespawnTime", 60f },
                { "Light", false }
            };
        }

        public override void ObjectStart(LEScene scene)
        {
            if (scene == LEScene.Editor)
            {
                SetLightState(GetProperty<bool>("Light"));
            }

            base.ObjectStart(scene);
        }

        public override void InitComponent()
        {
            gameObject.GetChild("Content").SetActive(false);
            gameObject.GetChild("Content").tag = "Health";

            DisolveOnEnable disolve = gameObject.GetChild("Content").AddComponent<DisolveOnEnable>();

            disolve.onEnable = true;
            disolve.m_renderer = gameObject.GetChildAt("Content/Mesh").GetComponent<MeshRenderer>();
            // Extract the dissolve materials from another healthpack in the scene.
            disolve.dissolveMaterials = t_healthPack.GetComponent<DisolveOnEnable>().dissolveMaterials;
            disolve.finalMaterials = new Material[] { disolve.m_renderer.sharedMaterial };
            disolve.appearSpeed = 8;
            disolve.startOffset = -3.4f;
            disolve.endOffset = 3;
            disolve.ignoreTimeScale = true;

            health = gameObject.GetChild("Content").AddComponent<Health>();

            health.preciseCollider = gameObject.GetChildAt("Content/Mesh/PreciseCollider").GetComponent<MeshCollider>();
            Invoke("SetRespawnTime", 0.1f);
            health.timerBeforeRespawn = -1;
            health.generalGrowSpeed = 3;
            health.m_animComp = gameObject.GetChild("Content").GetComponent<Animation>();
            health.m_boxCollider = gameObject.GetChild("Content").GetComponent<BoxCollider>();
            health.mesh = gameObject.GetChildAt("Content/Mesh").GetComponent<MeshRenderer>();
            health.m_light = gameObject.GetChildAt("Content/Mesh/PC_Only").GetComponent<Light>();
            health.m_lightBreathAnimComp = gameObject.GetChildAt("Content/Mesh/PC_Only").GetComponent<Animation>();
            health.m_flare = gameObject.GetChildAt("Content/Mesh/HealthFlare").GetComponent<LensFlare>();
            health.xScaleSpeed = 2;
            health.yScaleSpeed = 1;
            health.zScaleSpeed = 1;
            health.m_dissolve = disolve;

            SetLightState(GetProperty<bool>("Light"));

            gameObject.GetChild("Content").SetActive(true);

            initialized = true;
        }

        // Since respawn time is fixed and is changed to default (20) at Start() of Ammo class, change it after 0.1s
        void SetRespawnTime()
        {
            health.respawnTime = (float)GetProperty("RespawnTime");
        }

        public override bool SetProperty(string name, object value)
        {
            if (name == "RespawnTime")
            {
                if (value is string)
                {
                    if (Utils.TryParseFloat((string)value, out float result))
                    {
                        properties["RespawnTime"] = result;
                        return true;
                    }
                }
                else if (value is float)
                {
                    properties["RespawnTime"] = (float)value;
                    if (health) health.respawnTime = (float)value;
                    return true;
                }
            }
            else if (name == "Light")
            {
                if (value is bool boolValue)
                {
                    properties["Light"] = boolValue;
                    if (EditorController.Instance) SetLightState(boolValue);
                    return true;
                }
            }

            return base.SetProperty(name, value);
        }

        public override bool TriggerAction(string actionName)
        {
            if (actionName == "SpawnNow")
            {
                if (health) health.Activate();
            }

            return base.TriggerAction(actionName);
        }

        public override void SetCollidersStateForEdgeCase(bool newEnabledState)
        {
            contentObject.GetComponent<BoxCollider>().enabled = newEnabledState;
            contentObject.GetChildAt("Mesh/PreciseCollider").SetActive(newEnabledState);
        }

        void SetLightState(bool enabled)
        {
            if (healthLight) healthLight.enabled = enabled;
            if (healthFlare) healthFlare.SetActive(enabled);
        }
    }
}