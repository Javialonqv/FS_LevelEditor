using FS_LevelEditor;
using Il2Cpp;
using MelonLoader.ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Ammo_Pack : LE_Object
    {
        Ammo ammo;

        void Awake()
        {
            properties = new Dictionary<string, object>()
            {
                { "RespawnTime", 20f }
            };
        }

        void Start()
        {
            if (PlayModeController.Instance != null)
            {
                InitComponent();
            }
            else // If it's not in playmode, just create a collider so the user can click the object in LE.
            {
                GameObject collider = new GameObject("Collider");
                collider.transform.parent = transform;
                collider.transform.localScale = Vector3.one;
                collider.transform.localPosition = new Vector3(0f, 0.35f, 0f);
                collider.AddComponent<BoxCollider>().size = Vector3.one * 0.7f;
            }
        }

        void InitComponent()
        {
            gameObject.GetChildWithName("Content").SetActive(false);
            gameObject.GetChildWithName("Content").tag = "AmmoPack";

            DisolveOnEnable disolve = gameObject.GetChildAt("Content/Mesh/PC_Only").AddComponent<DisolveOnEnable>();

            disolve.m_renderer = gameObject.GetChildAt("Content/Mesh").GetComponent<MeshRenderer>();
            disolve.dissolveMaterials = t_ammoPack.gameObject.GetChildAt("Mesh/PC_Only").GetComponent<DisolveOnEnable>().dissolveMaterials;
            disolve.finalMaterials = new Material[] { disolve.m_renderer.sharedMaterial };
            disolve.appearSpeed = 3;
            disolve.startOffset = -0.6f;
            disolve.endOffset = 0.8f;
            disolve.ignoreTimeScale = true;

            ammo = gameObject.GetChildWithName("Content").AddComponent<Ammo>();

            ammo.preciseCollider = gameObject.GetChildAt("Content/Mesh/PreciseCollider").GetComponent<MeshCollider>();
            ammo.preciseCollider2 = gameObject.GetChildAt("Content/Mesh/PreciseCollider").GetComponent<CapsuleCollider>();
            ammo.m_animComp = gameObject.GetChildWithName("Content").GetComponent<Animation>();
            ammo.m_boxCollider = gameObject.GetChildWithName("Content").GetComponent<BoxCollider>();
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

            gameObject.GetChildWithName("Content").SetActive(true);
        }

        // Since respawn time is fixed and is changed to default (20) at Start() of Ammo class, change it after 0.1s
        void SetRespawnTime()
        {
            ammo.respawnTime = (float)GetProperty("RespawnTime");
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
                    if (ammo) ammo.respawnTime = (float)value;
                    return true;
                }
            }

            return false;
        }

        public override bool TriggerAction(string actionName)
        {
            if (actionName == "SpawnNow")
            {
                if (ammo) ammo.Activate();
            }

            return base.TriggerAction(actionName);
        }
    }
}