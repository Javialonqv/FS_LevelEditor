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
            gameObject.SetActive(false);
            gameObject.tag = "Health";

            DisolveOnEnable disolve = gameObject.AddComponent<DisolveOnEnable>();

            disolve.onEnable = true;
            disolve.m_renderer = gameObject.GetChildWithName("Mesh").GetComponent<MeshRenderer>();
            // Extract the dissolve materials from another healthpack in the scene.
            disolve.dissolveMaterials = FindObjectOfType<Health>().gameObject.GetComponent<DisolveOnEnable>().dissolveMaterials;
            disolve.finalMaterials = new Material[] { disolve.m_renderer.sharedMaterial };
            disolve.appearSpeed = 8;
            disolve.startOffset = -3.4f;
            disolve.endOffset = 3;
            disolve.ignoreTimeScale = true;

            health = gameObject.AddComponent<Health>();

            health.preciseCollider = gameObject.GetChildAt("Mesh/PreciseCollider").GetComponent<MeshCollider>();
            health.respawnTime = 60;
            health.timerBeforeRespawn = -1;
            health.generalGrowSpeed = 3;
            health.m_animComp = GetComponent<Animation>();
            health.m_boxCollider = GetComponent<BoxCollider>();
            health.mesh = gameObject.GetChildWithName("Mesh").GetComponent<MeshRenderer>();
            health.m_light = gameObject.GetChildAt("Mesh/PC_Only").GetComponent<Light>();
            health.m_lightBreathAnimComp = gameObject.GetChildAt("Mesh/PC_Only").GetComponent<Animation>();
            health.m_flare = gameObject.GetChildAt("Mesh/HealthFlare").GetComponent<LensFlare>();
            health.xScaleSpeed = 2;
            health.yScaleSpeed = 1;
            health.zScaleSpeed = 1;
            health.m_dissolve = disolve;

            gameObject.SetActive(true);
        }
    }
}