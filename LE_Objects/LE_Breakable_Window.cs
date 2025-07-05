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
        static bool staticVariablesInitialized = false;
        static AudioMixerGroup sfxOutputMixerGroup;
        static Vector3[] windowPartsOriginalPositions;
        static Vector3[] windowPartsOriginalScales;
        static Mesh[] windowPartMeshes;
        static PhysicMaterial[] windowPartMaterials;
        static Mesh[] windowPartColliderMeshes;
        static AudioClip[][] windowPartImpactSounds;
        static AudioClip[][] windowPartCollisionSounds;

        public override void InitComponent()
        {
            GameObject content = gameObject.GetChildWithName("Content");

            BreakableWindowController window = content.AddComponent<BreakableWindowController>();
            window.isFirstWindow = true;
            window.partsHolder = content.GetChildWithName("BreakableWindow_Shattered").transform;
            window.m_meshRenderer = content.GetChildWithName("Window_OriginalMesh").GetComponent<MeshRenderer>();
            window.m_audioSource = content.GetComponent<AudioSource>();
            window.m_generalBreakSounds = t_window.m_generalBreakSounds;
            if (!staticVariablesInitialized)
            {
                windowPartsOriginalPositions = new Vector3[window.partsHolder.childCount];
                windowPartsOriginalScales = new Vector3[window.partsHolder.childCount];
                for (int i = 0; i < window.partsHolder.childCount; i++)
                {
                    Transform child = window.partsHolder.GetChild(i);
                    windowPartsOriginalPositions[i] = child.transform.localPosition;
                    windowPartsOriginalScales[i] = child.transform.localScale;
                }
            }
            window.originalPositions = windowPartsOriginalPositions;
            window.originalScales = windowPartsOriginalScales;
            window.broken = false;
            window.usePhysicsBreak = true;
            window.taserIgnorePartsWhenBroken = false;

            if (!staticVariablesInitialized) sfxOutputMixerGroup = t_window.m_audioSource.outputAudioMixerGroup;
            window.m_audioSource.outputAudioMixerGroup = sfxOutputMixerGroup;

            BreakableWindowPart[] parts = new BreakableWindowPart[window.partsHolder.childCount];
            BreakableWindowPart[] fakeParts = new BreakableWindowPart[window.partsHolder.childCount];
            for (int i = 0; i < window.partsHolder.childCount; i++)
            {
                var child = window.partsHolder.GetChild(i);
                var templateChild = t_window.partsHolder.GetChild(i);

                if (!staticVariablesInitialized)
                {
                    if (i == 0)
                    {
                        windowPartMeshes = new Mesh[window.partsHolder.childCount];
                        windowPartMaterials = new PhysicMaterial[window.partsHolder.childCount];
                        windowPartColliderMeshes = new Mesh[window.partsHolder.childCount];
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
                part.m_associatedWindow = window;
                part.m_rigidBody = child.GetComponent<Rigidbody>();
                part.m_meshRenderer = child.GetComponent<MeshRenderer>();
                part.m_audioSource = child.GetComponent<AudioSource>();
                if (!staticVariablesInitialized)
                {
                    if (i == 0)
                    {
                        windowPartImpactSounds = new AudioClip[window.partsHolder.childCount][];
                        windowPartCollisionSounds = new AudioClip[window.partsHolder.childCount][];
                    }

                    var templateComp = templateChild.GetComponent<BreakableWindowPart>();
                    windowPartImpactSounds[i] = templateComp.m_impactSounds;
                    windowPartCollisionSounds[i] = templateComp.m_collisionSounds;
                }
                part.m_impactSounds = windowPartImpactSounds[i];
                part.m_collisionSounds = windowPartCollisionSounds[i];
                part.m_meshCollider = child.GetComponent<MeshCollider>();
                part.delayBeforeKinematic = 6;
                part.lifeTime = 30;

                parts[i] = part;
                if (i != 0 && i != 44 && i != 45 && i != 46) fakeParts[i] = part;
            }

            window.allParts = parts;
            window.fakeBreakParts = fakeParts;

            // ---------- SETUP TAGS & LAYERS ----------

            window.m_meshRenderer.gameObject.layer = LayerMask.NameToLayer("Glass");
            window.m_meshRenderer.transform.GetChild(0).gameObject.layer = LayerMask.NameToLayer("PlayerCollisionOnly");
            window.partsHolder.gameObject.layer = LayerMask.NameToLayer("TransparentFX");
            foreach (var part in parts)
            {
                part.gameObject.tag = "Destructible";
                part.gameObject.layer = LayerMask.NameToLayer("Glass");
            }
            content.GetChildWithName("ConstantPlayerBlockingCollider").layer = LayerMask.NameToLayer("Player");

            initialized = true;
        }
    }
}
