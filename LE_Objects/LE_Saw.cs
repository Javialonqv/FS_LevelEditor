using FS_LevelEditor.Editor;
using FS_LevelEditor.Playmode;
using Il2Cpp;
using Il2CppDiscord;
using UnityEngine;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Saw : LE_Object
    {
        ScieScript script;

        ScieScript sawScript;

        void Awake()
        {
            properties = new Dictionary<string, object>()
            {
                { "ActivateOnStart", true },
                { "TravelBack", true },
                { "Loop", false },
                { "waypoints", new List<WaypointData>() },
                { "Damage", 50 },
                { "WaitTime", 0f },
                { "Rotate", false },
                { "RotateSpeed", 1 }
            };
        }

        public override void OnInstantiated(LEScene scene)
        {
            if (scene == LEScene.Editor)
            {
                // Set the saw on or off.
                SetMeshOnEditor((bool)GetProperty("ActivateOnStart"));
            }

            base.OnInstantiated(scene);
        }
        public override void ObjectStart(LEScene scene)
        {
            if (scene == LEScene.Playmode)
            {
                // If it's false, that means the saw wasn't really spawned at the start of the level, activate it again to avoid bugs.
                if (!setActiveAtStart)
                {
                    // There's a good reason for this, I swear, the Activate and Deactivate functions are just inverting the enabled bool in the saw LOL, the both functions
                    // do the same thing, so first enable it, and then disable if needed, cause if we don't do anything, there's a bug with the saw animation.
                    script.Activate();
                    bool activateOnStart = (bool)GetProperty("ActivateOnStart");
                    if (!activateOnStart)
                    {
                        script.Deactivate();
                    }
                }
            }

            base.ObjectStart(scene);
        }

        public override void InitComponent()
        {
            GameObject content = gameObject.GetChild("Content");

            content.SetActive(false);
            content.tag = "Scie";

            content.GetComponent<AudioSource>().outputAudioMixerGroup = t_saw.GetComponent<AudioSource>().outputAudioMixerGroup;

            RotationScie rotationScie = content.GetChild("Scie_OFF").AddComponent<RotationScie>();
            rotationScie.vitesseRotation = 500;

            script = content.AddComponent<ScieScript>();
            script.doesDamage = true;
            script.damage = (int)GetProperty("Damage");
            script.rotationScript = rotationScie;
            script.m_damageCollider = content.GetComponent<BoxCollider>();
            script.m_audioSource = content.GetComponent<AudioSource>();
            script.movingSaw = false;
            script.movingSpeed = 10;
            script.forcedHeading = true;
            script.allowSideRotation = false;
            script.sideSpeedMultiplier = 5;
            if(GetProperty<bool>("Rotate"))
            {
                script.allowSideRotation = true;
                script.forcedHeading = true;
                script.sideSpeedMultiplier = GetProperty<int>("RotateSpeed");
            }
            script.scieSound = t_saw.scieSound;
            script.offMesh = content.GetChild("Scie_OFF").GetComponent<MeshRenderer>();
            script.onMesh = content.GetChildAt("Scie_OFF/Scie_ON").GetComponent<MeshRenderer>();
            script.m_collision = content.GetChild("Collision").GetComponent<BoxCollider>();
            script.physicsCollider = content.GetChild("Saw_PhysicsCollider").GetComponent<MeshCollider>();
            script.m_damageCollider = content.GetChild("Saw_DamageCollider").GetComponent<MeshCollider>();

            ForwardTriggerCollision damageTrigger = script.m_damageCollider.gameObject.AddComponent<ForwardTriggerCollision>();
            damageTrigger.target = script.gameObject;
            damageTrigger.triggerEnter = true;
            damageTrigger.triggerExit = true;

            script.particlesHolder = content.GetChild("SawParticlesHolder");
            script.particles1 = content.GetChildAt("SawParticlesHolder/SawSparks_1").GetComponent<ParticleSystem>();
            script.particles2 = content.GetChildAt("SawParticlesHolder/SawSparks_2").GetComponent<ParticleSystem>();
            script.particles3 = content.GetChildAt("SawParticlesHolder/SawSparks_3").GetComponent<ParticleSystem>();
            script.particles4 = content.GetChildAt("SawParticlesHolder/SawSparks_4").GetComponent<ParticleSystem>();
            script.particles1.GetComponent<ParticleSystemRenderer>().sharedMaterial = t_saw.particles1.GetComponent<ParticleSystemRenderer>().sharedMaterial;
            script.particles2.GetComponent<ParticleSystemRenderer>().sharedMaterial = t_saw.particles2.GetComponent<ParticleSystemRenderer>().sharedMaterial;
            script.particles3.GetComponent<ParticleSystemRenderer>().sharedMaterial = t_saw.particles3.GetComponent<ParticleSystemRenderer>().sharedMaterial;
            script.particles4.GetComponent<ParticleSystemRenderer>().sharedMaterial = t_saw.particles4.GetComponent<ParticleSystemRenderer>().sharedMaterial;


            if (setActiveAtStart) // Only do this if it's meant to be enabled at start, otherwise, the saw will be bugged.
            {
                // There's a good reason for this, I swear, the Activate and Deactivate functions are just inverting the enabled bool in the saw LOL, the both functions
                // do the same thing, so first enable it, and then disable if needed, cause if we don't do anything, there's a bug with the saw animation.
                script.Activate();
                bool activateOnStart = (bool)GetProperty("ActivateOnStart");
                if (!activateOnStart)
                {
                    script.Deactivate();
                }
            }

            content.SetActive(true);

            // --------- SETUP TAGS & LAYERS ---------

            script.m_collision.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            script.physicsCollider.gameObject.tag = "Saw_PhysicsCollider";
            script.physicsCollider.gameObject.layer = LayerMask.NameToLayer("AllExceptPlayer");
            script.m_damageCollider.gameObject.tag = "Scie";
            script.m_damageCollider.gameObject.layer = LayerMask.NameToLayer("PlayerCollisionOnly");

            script.particles1.gameObject.layer = LayerMask.NameToLayer("TransparentFX");
            script.particles2.gameObject.layer = LayerMask.NameToLayer("TransparentFX");
            script.particles3.gameObject.layer = LayerMask.NameToLayer("TransparentFX");
            script.particles4.gameObject.layer = LayerMask.NameToLayer("TransparentFX");

            initialized = true;
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
            }
            else if (name == "TravelBack")
            {
                if (value is bool)
                {
                    properties["TravelBack"] = (bool)value;
                    return true;
                }
            }
            else if (name == "Loop")
            {
                if (value is bool)
                {
                    properties["Loop"] = (bool)value;
                    return true;
                }
            }
            else if (name == "waypoints") // For now, this is only called when loading the level...
            {
                if (value is List<WaypointData>)
                {
                    properties["waypoints"] = value;
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
            else if (name == "WaitTime")
            {
                if (value is string)
                {
                    if (Utils.TryParseFloat((string)value, out float result))
                    {
                        properties["WaitTime"] = result;
                        return true;
                    }
                }
                else if (value is float)
                {
                    properties["WaitTime"] = (float)value;
                    return true;
                }
            }
            else if (name == "Rotate")
            {
                if (value is bool)
                {
                    properties["Rotate"] = (bool)value;
                    return true;
                }
            }
            else if (name == "RotateSpeed")
            {
                if (value is string)
                {
                    if (int.TryParse((string)value, out int result))
                    {
                        properties["RotateSpeed"] = result;
                        return true;
                    }
                }
                else if (value is int)
                {
                    properties["RotateSpeed"] = (int)value;
                    return true;
                }
            }
            return base.SetProperty(name, value);
        }
        public override bool TriggerAction(string actionName)
        {
            if (actionName == "AddWaypoint")
            {
                customWaypointSupport.AddWaypoint();
                return true;
            }
            else if (actionName == "Activate")
            {
                script.Activate();
                return true;
            }
            else if (actionName == "Deactivate")
            {
                script.Deactivate();
                return true;
            }
            else if (actionName == "ToggleActivated")
            {
                if (script.activated)
                {
                    script.Deactivate();
                }
                else
                {
                    script.Activate();
                }
                return true;
            }

            return base.TriggerAction(actionName);
        }

        void SetMeshOnEditor(bool isSawOn)
        {
            gameObject.GetChildAt("Content/Scie_OFF").GetComponent<MeshRenderer>().enabled = !isSawOn;
            gameObject.GetChildAt("Content/Scie_OFF/Scie_ON").GetComponent<MeshRenderer>().enabled = isSawOn;
        }
    }
}
