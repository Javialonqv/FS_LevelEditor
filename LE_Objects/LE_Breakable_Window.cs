using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Breakable_Window : LE_Object
    {
        // These values are the same for all of the windows.
        internal static bool staticVariablesInitialized = false;
        static AudioMixerGroup sfxOutputMixerGroup;
        static Vector3[] windowPartsOriginalPositions;
        static Vector3[] windowPartsOriginalScales;
        static Mesh[] windowPartMeshes;
        static PhysicMaterial[] windowPartMaterials;
        static Mesh[] windowPartColliderMeshes;
        static AudioClip[] windowPartImpactSounds;
        static AudioClip[] windowPartCollisionSounds;

        BreakableWindowController script;

        public override void InitComponent()
        {
            GameObject content = gameObject.GetChild("Content");

            script = content.AddComponent<BreakableWindowController>();
            script.isFirstWindow = true;
            script.partsHolder = content.GetChild("BreakableWindow_Shattered").transform;
            script.m_meshRenderer = content.GetChild("Window_OriginalMesh").GetComponent<MeshRenderer>();
            script.m_audioSource = content.GetComponent<AudioSource>();
            script.m_generalBreakSounds = t_window.m_generalBreakSounds;
            if (!staticVariablesInitialized)
            {
                windowPartsOriginalPositions = new Vector3[script.partsHolder.childCount];
                windowPartsOriginalScales = new Vector3[script.partsHolder.childCount];
                for (int i = 0; i < script.partsHolder.childCount; i++)
                {
                    Transform child = script.partsHolder.GetChild(i);
                    windowPartsOriginalPositions[i] = child.transform.localPosition;
                    windowPartsOriginalScales[i] = child.transform.localScale;
                }
            }
            script.originalPositions = windowPartsOriginalPositions;
            script.originalScales = windowPartsOriginalScales;
            script.broken = false;
            script.usePhysicsBreak = true;
            script.taserIgnorePartsWhenBroken = false;

            if (!staticVariablesInitialized) sfxOutputMixerGroup = t_window.m_audioSource.outputAudioMixerGroup;
            script.m_audioSource.outputAudioMixerGroup = sfxOutputMixerGroup;

            BreakableWindowPart[] parts = new BreakableWindowPart[script.partsHolder.childCount];
            BreakableWindowPart[] fakeParts = new BreakableWindowPart[script.partsHolder.childCount];
            for (int i = 0; i < script.partsHolder.childCount; i++)
            {
                var child = script.partsHolder.GetChild(i);
                var templateChild = t_window.partsHolder.GetChild(i);

                if (!staticVariablesInitialized)
                {
                    if (i == 0)
                    {
                        windowPartMeshes = new Mesh[script.partsHolder.childCount];
                        windowPartMaterials = new PhysicMaterial[script.partsHolder.childCount];
                        windowPartColliderMeshes = new Mesh[script.partsHolder.childCount];
                    }

                    windowPartMeshes[i] = templateChild.GetComponent<MeshFilter>().mesh;
                    windowPartMaterials[i] = templateChild.GetComponent<MeshCollider>().material;
                    windowPartColliderMeshes[i] = templateChild.GetComponent<MeshCollider>().sharedMesh;
                }

                child.GetComponent<MeshFilter>().mesh = windowPartMeshes[i];
                child.GetComponent<MeshCollider>().material = windowPartMaterials[i];
                child.GetComponent<MeshCollider>().sharedMesh = windowPartColliderMeshes[i];

                var proxy = child.gameObject.AddComponent<MovingPlatformProxy>();

                child.GetComponent<AudioSource>().outputAudioMixerGroup = sfxOutputMixerGroup;

                var part = child.gameObject.AddComponent<BreakableWindowPart>();
                part.movingPlatformProxy = proxy;
                part.m_associatedWindow = script;
                part.m_rigidBody = child.GetComponent<Rigidbody>();
                part.m_meshRenderer = child.GetComponent<MeshRenderer>();
                part.m_audioSource = child.GetComponent<AudioSource>();
                if (!staticVariablesInitialized && (windowPartImpactSounds == null || windowPartCollisionSounds == null))
                {
                    windowPartImpactSounds = new AudioClip[script.partsHolder.childCount];
                    windowPartCollisionSounds = new AudioClip[script.partsHolder.childCount];

                    var templateComp = templateChild.GetComponent<BreakableWindowPart>();
                    windowPartImpactSounds = templateComp.m_impactSounds;
                    windowPartCollisionSounds = templateComp.m_collisionSounds;
                }
                part.m_impactSounds = windowPartImpactSounds;
                part.m_collisionSounds = windowPartCollisionSounds;
                part.m_meshCollider = child.GetComponent<MeshCollider>();
                part.delayBeforeKinematic = 6;
                part.lifeTime = 30;

                parts[i] = part;
                if (i != 0 && i != 44 && i != 45 && i != 46) fakeParts[i] = part;
            }

            script.allParts = parts;
            script.fakeBreakParts = fakeParts;

            // ---------- SETUP TAGS & LAYERS ----------

            script.m_meshRenderer.gameObject.layer = LayerMask.NameToLayer("Glass");
            script.m_meshRenderer.transform.GetChild(0).gameObject.layer = LayerMask.NameToLayer("PlayerCollisionOnly");
            script.partsHolder.gameObject.layer = LayerMask.NameToLayer("TransparentFX");
            foreach (var part in parts)
            {
                part.gameObject.tag = "Destructible";
                part.gameObject.layer = LayerMask.NameToLayer("Glass");
            }
            content.GetChild("ConstantPlayerBlockingCollider").layer = LayerMask.NameToLayer("Player");

            staticVariablesInitialized = true;
            initialized = true;
        }

        public override bool TriggerAction(string actionName)
        {
            if (actionName == "BreakNow")
            {
                script.SetAsBroken(true);
                // For some reason, SetAsBroken disables the mesh renderer, force it to be enabled.
                //if (!invisibleMesh) script.m_meshRenderer.enabled = true;
            }

            return base.TriggerAction(actionName);
        }
    }
}
