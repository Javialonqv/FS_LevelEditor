using FS_LevelEditor;
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
    public class LE_Health_Pack : LE_Object
    {
        Health health;

        void Awake()
        {
            properties = new Dictionary<string, object>()
            {
                { "RespawnTime", 60f }
            };
        }

        public override void Start()
        {
            if (EditorController.Instance)
            {
                SetCollidersState(false);
                SetEditorCollider(true);
            }

            if (PlayModeController.Instance != null)
            {
                SetEditorCollider(false);

                if (!initialized) InitComponent();
            }
        }

        void InitComponent()
        {
            gameObject.GetChildWithName("Content").SetActive(false);
            gameObject.GetChildWithName("Content").tag = "Health";

            DisolveOnEnable disolve = gameObject.GetChildWithName("Content").AddComponent<DisolveOnEnable>();

            disolve.onEnable = true;
            disolve.m_renderer = gameObject.GetChildAt("Content/Mesh").GetComponent<MeshRenderer>();
            // Extract the dissolve materials from another healthpack in the scene.
            disolve.dissolveMaterials = t_healthPack.GetComponent<DisolveOnEnable>().dissolveMaterials;
            disolve.finalMaterials = new Material[] { disolve.m_renderer.sharedMaterial };
            disolve.appearSpeed = 8;
            disolve.startOffset = -3.4f;
            disolve.endOffset = 3;
            disolve.ignoreTimeScale = true;

            health = gameObject.GetChildWithName("Content").AddComponent<Health>();

            health.preciseCollider = gameObject.GetChildAt("Content/Mesh/PreciseCollider").GetComponent<MeshCollider>();
            Invoke("SetRespawnTime", 0.1f);
            health.timerBeforeRespawn = -1;
            health.generalGrowSpeed = 3;
            health.m_animComp = gameObject.GetChildWithName("Content").GetComponent<Animation>();
            health.m_boxCollider = gameObject.GetChildWithName("Content").GetComponent<BoxCollider>();
            health.mesh = gameObject.GetChildAt("Content/Mesh").GetComponent<MeshRenderer>();
            health.m_light = gameObject.GetChildAt("Content/Mesh/PC_Only").GetComponent<Light>();
            health.m_lightBreathAnimComp = gameObject.GetChildAt("Content/Mesh/PC_Only").GetComponent<Animation>();
            health.m_flare = gameObject.GetChildAt("Content/Mesh/HealthFlare").GetComponent<LensFlare>();
            health.xScaleSpeed = 2;
            health.yScaleSpeed = 1;
            health.zScaleSpeed = 1;
            health.m_dissolve = disolve;

            gameObject.GetChildWithName("Content").SetActive(true);

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
                    if (Utilities.TryParseFloat((string)value, out float result))
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

            return false;
        }

        public override bool TriggerAction(string actionName)
        {
            if (actionName == "SpawnNow")
            {
                if (health) health.Activate();
            }

            return base.TriggerAction(actionName);
        }
    }
}