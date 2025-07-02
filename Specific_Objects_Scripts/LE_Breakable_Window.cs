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
    public class LE_Breakable_Window : LE_Object
    {
        public override void InitComponent()
        {
            GameObject content = gameObject.GetChildWithName("Content");

            BreakableWindowController window = content.AddComponent<BreakableWindowController>();
            window.isFirstWindow = true;
            window.partsHolder = content.GetChildWithName("BreakableWindow_Shattered").transform;
            window.m_meshRenderer = content.GetChildWithName("Window_OriginalMesh").GetComponent<MeshRenderer>();
            window.m_audioSource = content.GetComponent<AudioSource>();
            window.m_generalBreakSounds = t_window.m_generalBreakSounds;
            List<Vector3> originalPositions = new List<Vector3>();
            List<Vector3> originalScales = new List<Vector3>();
            foreach (var child in window.partsHolder.gameObject.GetChilds())
            {
                originalPositions.Add(child.transform.localPosition);
                originalScales.Add(child.transform.localScale);
            }
            window.originalPositions = originalPositions.ToArray();
            window.originalScales = originalScales.ToArray();
            window.broken = false;
            window.usePhysicsBreak = true;
            window.taserIgnorePartsWhenBroken = false;

            window.m_audioSource.outputAudioMixerGroup = t_window.m_audioSource.outputAudioMixerGroup;

            List<BreakableWindowPart> parts = new List<BreakableWindowPart>();
            List<BreakableWindowPart> fakeParts = new List<BreakableWindowPart>();
            for (int i = 0; i < window.partsHolder.childCount; i++)
            {
                var child = window.partsHolder.GetChild(i);
                var templateChild = t_window.partsHolder.GetChild(i);

                child.GetComponent<MeshFilter>().mesh = templateChild.GetComponent<MeshFilter>().mesh;
                child.GetComponent<MeshCollider>().material = templateChild.GetComponent<MeshCollider>().material;
                child.GetComponent<MeshCollider>().sharedMesh = templateChild.GetComponent<MeshCollider>().sharedMesh;

                var proxy = child.gameObject.AddComponent<MovingPlatformProxy>();

                child.GetComponent<AudioSource>().outputAudioMixerGroup = templateChild.GetComponent<AudioSource>().outputAudioMixerGroup;

                var part = child.gameObject.AddComponent<BreakableWindowPart>();
                part.movingPlatformProxy = proxy;
                part.m_associatedWindow = window;
                part.m_rigidBody = child.GetComponent<Rigidbody>();
                part.m_meshRenderer = child.GetComponent<MeshRenderer>();
                part.m_audioSource = child.GetComponent<AudioSource>();
                part.m_impactSounds = templateChild.GetComponent<BreakableWindowPart>().m_impactSounds;
                part.m_collisionSounds = templateChild.GetComponent<BreakableWindowPart>().m_collisionSounds;
                part.m_meshCollider = child.GetComponent<MeshCollider>();
                part.delayBeforeKinematic = 6;
                part.lifeTime = 30;

                parts.Add(part);
                if (i != 0 && i != 44 && i != 45 && i != 46) fakeParts.Add(part);
            }

            window.allParts = parts.ToArray();
            window.fakeBreakParts = fakeParts.ToArray();

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
