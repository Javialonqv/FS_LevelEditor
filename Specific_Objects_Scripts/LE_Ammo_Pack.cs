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
            gameObject.tag = "AmmoPack";

            DisolveOnEnable disolve = gameObject.GetChildAt("Mesh/PC_Only").AddComponent<DisolveOnEnable>();

            disolve.m_renderer = gameObject.GetChildWithName("Mesh").GetComponent<MeshRenderer>();
            disolve.dissolveMaterials = FindObjectOfType<Ammo>().gameObject.GetChildAt("Mesh/PC_Only").GetComponent<DisolveOnEnable>().dissolveMaterials;
            disolve.finalMaterials = new Material[] { disolve.m_renderer.sharedMaterial };
            disolve.appearSpeed = 3;
            disolve.startOffset = -0.6f;
            disolve.endOffset = 0.8f;
            disolve.ignoreTimeScale = true;

            Ammo ammo = gameObject.AddComponent<Ammo>();

            ammo.preciseCollider = gameObject.GetChildAt("Mesh/PreciseCollider").GetComponent<MeshCollider>();
            ammo.preciseCollider2 = gameObject.GetChildAt("Mesh/PreciseCollider").GetComponent<CapsuleCollider>();
            ammo.m_animComp = GetComponent<Animation>();
            ammo.m_boxCollider = GetComponent<BoxCollider>();
            ammo.mesh = gameObject.GetChildWithName("Mesh").GetComponent<MeshRenderer>();
            ammo.timerBeforeRespawn = -1;
            ammo.generalGrowSpeed = 3;
            ammo.xScaleSpeed = 2;
            ammo.yScaleSpeed = 1;
            ammo.zScaleSpeed = 1;
            ammo.m_light = gameObject.GetChildAt("Mesh/PC_Only").GetComponent<Light>();
            ammo.m_flare = gameObject.GetChildAt("Mesh/AmmoFlare").GetComponent<LensFlare>();
            ammo.m_dissolve = disolve;

            gameObject.SetActive(true);
        }
    }
}