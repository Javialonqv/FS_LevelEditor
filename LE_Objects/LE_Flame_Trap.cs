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
    public class LE_Flame_Trap : LE_Object
    {
        FlameTrapController trap;

        void Awake()
        {
            properties = new Dictionary<string, object>
            {
                { "ActivateOnStart", true },
                { "Constant", false }
            };
        }

        public override void InitComponent()
        {
            GameObject content = gameObject.GetChildWithName("Content");

            content.SetActive(false);

            trap = content.AddComponent<FlameTrapController>();
            trap.flameLight = content.GetChildAt("VFX/FlameLight").GetComponent<Light>();
            trap.loopAudioSource = content.GetChildWithName("LoopAudioSource").GetComponent<AudioSource>();
            trap.startClip = t_flameTrap.startClip;
            trap.loopClip = t_flameTrap.loopClip;
            trap.stopClip = t_flameTrap.stopClip;
            trap.doesDamage = true;
            trap.dotActvationDelay = -1;
            trap.useDOTScaling = false;
            trap.dotScalingSpeed = 0.8f;
            trap.constant = (bool)GetProperty("Constant");
            trap.onDuration = 4;
            trap.offDuration = 2;
            trap.firstOnAddition = 0;
            trap.offMaterials = t_flameTrap.offMaterials;
            trap.onMaterials = t_flameTrap.onMaterials;
            trap.particles = content.GetChildWithName("VFX").GetComponent<ParticleSystem>();
            trap.m_meshRenderer = content.GetChildAt("VFX/Mesh").GetComponent<MeshRenderer>();
            trap.noDeactivation = false;
            trap.activationTrigger = content.GetChildWithName("ActivateTrigger").GetComponent<BoxCollider>();
            trap.reducedColliderSizeMultipliers = Vector3.one;
            trap.reducedFlameLengthMultiplier = 0.5f;

            DOT dot = content.GetChildWithName("DOT_Trigger").AddComponent<DOT>();
            dot.pushCubes = true;
            dot.pushForce = 8;
            dot.DPS = 80;
            dot.useDmgCollisionDetection = false;
            dot.collisionCheckPoint = content.GetChildWithName("DamageObstructionPoint").transform;
            dot.damageColLayer = t_flameTrap.gameObject.GetChildWithName("DOT_Trigger").GetComponent<DOT>().damageColLayer;

            trap.dot = dot;

            trap.loopAudioSource.outputAudioMixerGroup = t_flameTrap.loopAudioSource.outputAudioMixerGroup;

            content.SetActive(true);

            if (GetProperty<bool>("ActivateOnStart")) trap.Activate();

            initialized = true;
        }

        public override bool SetProperty(string name, object value)
        {
            if (name == "ActivateOnStart")
            {
                if (value is bool)
                {
                    properties["ActivateOnStart"] = (bool)value;
                    return true;
                }
            }
            else if (name == "Constant")
            {
                if (value is bool)
                {
                    properties["Constant"] = (bool)value;
                    return true;
                }
            }

            return base.SetProperty(name, value);
        }

        public override bool TriggerAction(string actionName)
        {
            if (actionName == "Activate")
            {
                trap.TurnOn();
                return true;
            }
            else if (actionName == "Deactivate")
            {
                trap.TurnOff();
                return true;
            }
            else if (actionName == "ToggleActivated")
            {
                if (trap.IsOn())
                {
                    trap.TurnOff();
                }
                else
                {
                    trap.TurnOn();
                }
                return true;
            }

            return base.TriggerAction(actionName);
        }
    }
}
