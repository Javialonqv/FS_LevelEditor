using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Il2Cpp.Interop;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Laser : LE_Object
    {
        Laser_H_Controller laser;

        void Awake()
        {
            properties = new Dictionary<string, object>()
            {
                { "ActivateOnStart", true },
                { "Damage", 34 }
            };
        }

        public override void OnInstantiated(LEScene scene)
        {
            if (scene == LEScene.Editor)
            {
                SetMeshOnEditor((bool)GetProperty("ActivateOnStart"));
            }

            base.OnInstantiated(scene);
        }

        public override void InitComponent()
        {
            Laser_H_Controller template = t_laser;

            laser = gameObject.GetChildWithName("Content").AddComponent<Laser_H_Controller>();
            laser.laserOriginPoint = gameObject.GetChildAt("Content/LaserOriginPoint").transform;
            laser.laserHitDamage = (int)GetProperty("Damage");
            laser.onTurnOn = new UnityEngine.Events.UnityEvent();
            laser.onTurnOff = new UnityEngine.Events.UnityEvent();
            laser.onExplode = new UnityEngine.Events.UnityEvent();
            laser.onActivate = new UnityEngine.Events.UnityEvent();
            laser.onDeactivate = new UnityEngine.Events.UnityEvent();
            laser.safetyCollider = gameObject.GetChildAt("Content/SafetyCollider");
            laser.speedrunCollisionsWhenOn = true;
            laser.hasPlayerCollisionsWhenOff = true;
            laser.speedrunCollisionsWhenOff = true;
            laser.collisionOn = gameObject.GetChildAt("Content/MeshOn").GetComponent<BoxCollider>();
            laser.collisionOff = gameObject.GetChildAt("Content/MeshOff").GetComponent<BoxCollider>();
            laser.hasParticles = true;
            laser.forceDynLighting = true;
            laser.breakWindowsOnExplode = true;
            laser.explodeWithInvalidPosObj = true;
            laser.cachedTransform = laser.transform;
            laser.cachedGO = laser.gameObject;
            laser.explosionDamage = 150;
            laser.contactExplosionThroughWalls = true;
            laser.contactExplosionRadius = 10;
            laser.remoteExplosionRadius = 3;
            laser.explodeProximityMines = true;
            laser.proximityRadius = 3;
            laser.explodeByProximity = true;
            laser.disableDistance = 300;
            laser.m_laserOn = template.m_laserOn;
            laser.m_laserOff = template.m_laserOff;
            laser.m_currentLaserImpact = gameObject.GetChildAt("Content/LaserPointRed");
            laser.m_currentLaserImpactT = gameObject.GetChildAt("Content/LaserPointRed").transform;
            laser.Line = laser.GetComponent<LineRenderer>();
            laser.transparentMat = template.transparentMat;
            laser.cutoutMat = template.cutoutMat;
            laser.layer = template.layer;
            laser.constant = true;
            laser.loopAudioSource = laser.GetComponent<AudioSource>();
            laser.onOffAudioSource = gameObject.GetChildAt("Content/Audio2").GetComponent<AudioSource>();
            laser.m_onMesh = gameObject.GetChildAt("Content/MeshOn");
            laser.m_offMesh = gameObject.GetChildAt("Content/MeshOff");
            laser.firstEnableEver = true;
            laser.laserSound = template.laserSound;
            laser.unselectedColor = Color.black;
            laser.selectedColor = Color.black;
            laser.m_light = gameObject.GetChildAt("Content/Light").GetComponent<Light>();
            laser.m_flare = gameObject.GetChildAt("Content/Light").GetComponent<LensFlare>();
            laser.flareMultiplier = 1;
            laser.activeEditorState = true;
            laser.constantEditorState = true;

            laser.Line.material = template.Line.material;

            laser.loopAudioSource.outputAudioMixerGroup = template.loopAudioSource.outputAudioMixerGroup;

            ObjectStateSync sync = gameObject.GetChildAt("Content").AddComponent<ObjectStateSync>();
            sync.assignNewParent = true;
            sync.objectGO = gameObject.GetChildAt("Content/LaserRailHolder");
            sync.objectT = gameObject.GetChildAt("Content/LaserRailHolder").transform;
            sync.stateInEditor = true;
            sync.firstOnEnable = true;

            laser.m_flare.flare = template.m_flare.flare;

            laser.onOffAudioSource.outputAudioMixerGroup = template.onOffAudioSource.outputAudioMixerGroup;

            laser.safetyCollider.GetComponent<MeshCollider>().sharedMesh = template.safetyCollider.GetComponent<MeshCollider>().sharedMesh;

            OnlyForPC pcOnly = laser.m_currentLaserImpact.AddComponent<OnlyForPC>();
            pcOnly.PC_ExclusiveChild = pcOnly.gameObject.GetChildWithName("PC_FX");

            LaserPoint point = laser.m_currentLaserImpact.AddComponent<LaserPoint>();
            point.particles = point.gameObject.GetChildAt("PC_FX/Laser_Impact_PC_VFX/Sparks").GetComponent<ParticleSystem>();
            point.particlesGO = point.particles.gameObject;
            point.hitTexture = point.gameObject.GetChildWithName("LaserPointTexture");
            point.pcVFXHolder = point.gameObject.GetChildWithName("PC_FX");
            point.VFXParent = point.gameObject.GetChildAt("PC_FX/Laser_Impact_PC_VFX");
            point.pointLight = point.gameObject.GetChildAt("PC_FX/Laser_Impact_PC_VFX/LaserImpactRedLight");
            point.flare = point.gameObject.GetChildWithName("LensFlare");
            point.flareComponent = point.gameObject.GetChildWithName("LensFlare").GetComponent<LensFlare>();
            point.flareGO = point.gameObject.GetChildWithName("LensFlare");
            point.m_audioSource = point.GetComponent<AudioSource>();
            point.hasParticleComp = true;

            point.m_audioSource.clip = template.m_currentLaserImpactScript.m_audioSource.clip;
            point.m_audioSource.outputAudioMixerGroup = template.m_currentLaserImpactScript.m_audioSource.outputAudioMixerGroup;

            point.hitTexture.GetComponent<MeshRenderer>().material = template.m_currentLaserImpactScript.hitTexture.GetComponent<MeshRenderer>().material;
            point.hitTexture.GetComponent<MeshFilter>().mesh = template.m_currentLaserImpactScript.hitTexture.GetComponent<MeshFilter>().mesh;

            point.particles.GetComponent<ParticleSystemRenderer>().mesh = template.m_currentLaserImpactScript.particles.GetComponent<ParticleSystemRenderer>().mesh;
            point.particles.GetComponent<ParticleSystemRenderer>().material = template.m_currentLaserImpactScript.particles.GetComponent<ParticleSystemRenderer>().material;

            point.flare.GetComponent<LensFlare>().flare = template.m_currentLaserImpactScript.flareComponent.flare;

            sync.objectGO.GetChildWithName("LaserRail").GetComponent<MeshRenderer>().material = template.GetComponent<ObjectStateSync>().objectGO.GetChildWithName("LaserRail").GetComponent<MeshRenderer>().material;
            sync.objectGO.GetChildWithName("LaserRail").GetComponent<MeshFilter>().mesh = template.GetComponent<ObjectStateSync>().objectGO.GetChildWithName("LaserRail").GetComponent<MeshFilter>().mesh;

            laser.m_currentLaserImpactScript = point;

            bool activateOnStart = (bool)GetProperty("ActivateOnStart");
            if (activateOnStart)
            {
                Invoke("ActivateLaserDelayed", 0.2f);
            }

            initialized = true;
        }

        // This method is meant to be invoked with Invoke().
        void ActivateLaserDelayed()
        {
            laser.Activate();
        }

        public override bool SetProperty(string name, object value)
        {
            if (name == "ActivateOnStart")
            {
                if (value is bool)
                {
                    if (EditorController.Instance != null) SetMeshOnEditor((bool)value);
                    properties["ActivateOnStart"] = (bool)value;
                    return true;
                }
                else
                {
                    Logger.Error($"Tried to set \"ActivateOnStart\" property with value of type \"{value.GetType().Name}\".");
                    return false;
                }
            }
            else if (name == "Damage")
            {
                if (value is string)
                {
                    if (int.TryParse((string)value, out int result))
                    {
                        properties["Damage"] = result;
                        return true;
                    }
                }
                else if (value is int)
                {
                    properties["Damage"] = (int)value;
                    return true;
                }
            }

            return false;
        }

        public override bool TriggerAction(string actionName)
        {
            if (actionName == "Activate")
            {
                laser.Activate();
                return true;
            }
            else if (actionName == "Deactivate")
            {
                laser.Deactivate();
                return true;
            }
            else if (actionName == "ToggleActivated")
            {
                if (laser.activated)
                {
                    laser.Deactivate();
                }
                else
                {
                    laser.Activate();
                }
                return true;
            }

            return base.TriggerAction(actionName);
        }

        void SetMeshOnEditor(bool isLaserOn)
        {
            gameObject.GetChildAt("Content/MeshOff").GetComponent<MeshRenderer>().enabled = !isLaserOn;
            gameObject.GetChildAt("Content/MeshOn").GetComponent<MeshRenderer>().enabled = isLaserOn;
        }
    }
}
