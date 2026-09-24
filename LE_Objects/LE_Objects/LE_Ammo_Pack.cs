using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using FS_LevelEditor.Editor;

namespace FS_LevelEditor
{
    public class LE_Ammo_Pack : LE_Object
    {
        Ammo ammo;
        Light ammoLight;
        GameObject ammoFlare;

        void Awake()
        {
            ammoLight = gameObject.GetChildAt("Content/Mesh/PC_Only").GetComponent<Light>();
            ammoFlare = gameObject.GetChildAt("Content/Mesh/AmmoFlare");
        }

        public static Dictionary<string, object> GetDefaultProperties()
        {
            return new Dictionary<string, object>()
            {
                { "RespawnTime", 20f },
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
            gameObject.GetChild("Content").tag = "AmmoPack";

            DisolveOnEnable disolve = gameObject.GetChildAt("Content/Mesh/PC_Only").AddComponent<DisolveOnEnable>();

            disolve.m_renderer = gameObject.GetChildAt("Content/Mesh").GetComponent<MeshRenderer>();
            disolve.dissolveMaterials = t_ammoPack.gameObject.GetChildAt("Mesh/PC_Only").GetComponent<DisolveOnEnable>().dissolveMaterials;
            disolve.finalMaterials = new Material[] { disolve.m_renderer.sharedMaterial };
            disolve.appearSpeed = 3;
            disolve.startOffset = -0.6f;
            disolve.endOffset = 0.8f;
            disolve.ignoreTimeScale = true;

            ammo = gameObject.GetChild("Content").AddComponent<Ammo>();

            ammo.preciseCollider = gameObject.GetChildAt("Content/Mesh/PreciseCollider").GetComponent<MeshCollider>();
            ammo.preciseCollider2 = gameObject.GetChildAt("Content/Mesh/PreciseCollider").GetComponent<CapsuleCollider>();
            ammo.m_animComp = gameObject.GetChild("Content").GetComponent<Animation>();
            ammo.m_boxCollider = gameObject.GetChild("Content").GetComponent<BoxCollider>();
            ammo.mesh = gameObject.GetChildAt("Content/Mesh").GetComponent<MeshRenderer>();
            ammo.timerBeforeRespawn = -1;
            Invoke("SetRespawnTime", 0.1f);
            ammo.generalGrowSpeed = 3;
            ammo.xScaleSpeed = 2;
            ammo.yScaleSpeed = 1;
            ammo.zScaleSpeed = 1;
            ammo.m_light = gameObject.GetChildAt("Content/Mesh/PC_Only").GetComponent<Light>();
            ammo.m_flare = gameObject.GetChildAt("Content/Mesh/AmmoFlare").GetComponent<LensFlare>();
            ammo.m_dissolve = disolve;

            SetLightState(GetProperty<bool>("Light"));

            gameObject.GetChild("Content").SetActive(true);

            initialized = true;
        }

        // Since respawn time is fixed and is changed to default (20) at Start() of Ammo class, change it after 0.1s
        void SetRespawnTime()
        {
            AccessTools.Field(typeof(Ammo), "respawnTime").SetValue(ammo, (float)GetProperty("RespawnTime"));
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
                    if (ammo)
                        AccessTools.Field(typeof(Ammo), "respawnTime").SetValue(ammo, (float)value);
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
                if (ammo) ammo.Activate();
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
            if (ammoLight) ammoLight.enabled = enabled;
            if (ammoFlare) ammoFlare.SetActive(enabled);
        }
    }
}