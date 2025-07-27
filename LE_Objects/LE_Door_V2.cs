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
    internal class LE_Door_V2 : LE_Object
    {
        PorteScript doorScript;

        public override void ObjectStart(LEScene scene)
        {
            if (scene == LEScene.Playmode)
            {
                doorScript.SetToGreenColor();
            }
        }

        public override void InitComponent()
        {
            GameObject content = gameObject.GetChildWithName("Content");

            content.SetActive(false);

            doorScript = content.AddComponent<PorteScript>();
            doorScript.activationTrigger = content.GetChildWithName("ActivateTrigger").transform;
            doorScript.activationTriggerCollider = content.GetChildWithName("ActivateTrigger").GetComponent<BoxCollider>();
            doorScript.allCollidersExceptInstant = new Collider[0];
            doorScript.allowOpen = true;
            doorScript.animationSpeed = 1;
            doorScript.BlocSwitchs = new GameObject[0];
            doorScript.closeSound = t_doorV2.closeSound;
            doorScript.defaultIsRed = false;
            doorScript.defaultTriggerState = true;
            doorScript.doorEditorState = false;
            doorScript.doorEditorVisibleState = false;
            doorScript.doorMesh = content.GetChildWithName("Mesh").transform;
            doorScript.doorMeshV2 = content.GetChildWithName("Mesh_V2").transform;
            //doorScript.forceTeleportGO = content.GetChildAt("Mesh/porte1/ForceTeleport_Holder/ForceTeleport_Vent");
            doorScript.forceTeleportGO_MeshV2 = content.GetChildAt("Mesh_V2/portev2/ForceTeleport_Holder/ForceTeleport_Vent");
            doorScript.instantCollider = content.GetChildWithName("InstantCollider").GetComponent<BoxCollider>();
            doorScript.isRed = true;
            doorScript.isSkinV2 = true;
            doorScript.lockingBarsMeshes = new Il2CppSystem.Collections.Generic.List<MeshFilter>();
            doorScript.lockingDeviceIsLocked = true;
            //doorScript.m_animation_V1 = doorScript.doorMesh.GetComponent<Animation>();
            doorScript.m_animation_v2 = doorScript.doorMeshV2.GetComponent<Animation>();
            doorScript.m_animationToUse = doorScript.doorMeshV2.GetComponent<Animation>();
            doorScript.m_audioSource = content.GetComponent<AudioSource>();
            doorScript.m_audioSource.outputAudioMixerGroup = t_doorV2.m_audioSource.outputAudioMixerGroup;
            doorScript.m_greenPillars = content.GetChildAt("Mesh_V2/portev2/DoorPillars/Cyan");
            doorScript.m_greenRenderers = new Il2CppSystem.Collections.Generic.List<GameObject>();
            doorScript.m_greenRenderers.Add(content.GetChildAt("Mesh_V2/portev2/door_V2_parts/partsHolder/onParts/OnTopPart/onPart1Cyan"));
            doorScript.m_greenRenderers.Add(content.GetChildAt("Mesh_V2/portev2/door_V2_parts/partsHolder/onParts/OnBottomPart/onPart2Cyan"));
            //doorScript.m_leftDoorRedRenderer = content.GetChildAt("Mesh/porte1/gauche/gaucheRed").GetComponent<MeshRenderer>();
            //doorScript.m_leftDoorRenderer = content.GetChildAt("Mesh/porte1/gauche").GetComponent<MeshRenderer>();
            doorScript.m_onClose = new UnityEngine.Events.UnityEvent();
            doorScript.m_onLock = new UnityEngine.Events.UnityEvent();
            doorScript.m_onOpen = new UnityEngine.Events.UnityEvent();
            doorScript.m_onUnlock = new UnityEngine.Events.UnityEvent();
            doorScript.m_redPillars = content.GetChildAt("Mesh_V2/portev2/DoorPillars/Red");
            doorScript.m_redRenderers = new Il2CppSystem.Collections.Generic.List<GameObject>();
            doorScript.m_redRenderers.Add(content.GetChildAt("Mesh_V2/portev2/door_V2_parts/partsHolder/onParts/OnTopPart/onPart1Red"));
            doorScript.m_redRenderers.Add(content.GetChildAt("Mesh_V2/portev2/door_V2_parts/partsHolder/onParts/OnBottomPart/onPart2Red"));
            //doorScript.m_rightDoorRedRenderer = content.GetChildAt("Mesh/porte1/droite/droiteRed").GetComponent<MeshRenderer>();
            //doorScript.m_rightDoorRenderer = content.GetChildAt("Mesh/porte1/droite").GetComponent<MeshRenderer>();
            doorScript.m_switchList = new InterrupteurController[0];
            doorScript.openSound = t_doorV2.openSound;
            doorScript.open = false;
            doorScript.openAtStart = false;
            doorScript.portal = content.GetChildWithName("DoorOcclusionPortal").GetComponent<OcclusionPortal>();

            foreach (var state in t_doorV2.doorMeshV2.GetComponent<Animation>())
            {
                var animState = state.Cast<AnimationState>();
                doorScript.doorMeshV2.GetComponent<Animation>().AddClip(animState.clip, animState.name);
            }
            doorScript.doorMeshV2.GetComponent<Animation>().clip = t_doorV2.doorMeshV2.GetComponent<Animation>().clip;

            ForceTeleport teleport = doorScript.forceTeleportGO_MeshV2.AddComponent<ForceTeleport>();
            teleport.considerBooks = true;
            teleport.considerEncKeys = true;
            teleport.considerPowerCores = true;
            teleport.considerTablets = true;
            teleport.LocalXAxisOnly = true;
            teleport.takeClosest = true;
            teleport.teleportPoints = new Il2CppSystem.Collections.Generic.List<Transform>();
            teleport.teleportPoints.Add(doorScript.doorMesh.Find("porte1/TeleportPoint1_Inside"));
            teleport.teleportPoints.Add(doorScript.doorMesh.Find("porte1/TeleportPoint2_Outside"));
            teleport.teleportX = true;
            teleport.teleportY = true;
            teleport.teleportZ = true;
            teleport.useListOfPoints = true;

            // ---------- SETUP TAGS & LAYERS ----------

            content.tag = "PorteAuto";
            content.GetChildAt("Mesh_V2/portev2/cadre_v2/CadreColliders/Bottom_IgnorePlayer").layer = LayerMask.NameToLayer("IgnorePlayerCollision");
            #region Door Pillars Tags & Layers
            // --------------------------------------------------
            content.GetChildAt("Mesh_V2/portev2/DoorPillars/Cyan/Pillar_Type1_Cyan_Left").tag = "Pillar";
            content.GetChildAt("Mesh_V2/portev2/DoorPillars/Cyan/Pillar_Type1_Cyan_Left/SimplifiedCollider").layer = LayerMask.NameToLayer("PlayerCollisionOnly");
            content.GetChildAt("Mesh_V2/portev2/DoorPillars/Cyan/Pillar_Type1_Cyan_Left/PerfectCollider").layer = LayerMask.NameToLayer("AllExceptPlayer");
            content.GetChildAt("Mesh_V2/portev2/DoorPillars/Cyan/Pillar_Type1_Cyan_Left/SimplifiedPhysicsCollider").layer = LayerMask.NameToLayer("Planet");
            content.GetChildAt("Mesh_V2/portev2/DoorPillars/Cyan/Pillar_Type1_Cyan_Right").tag = "Pillar";
            content.GetChildAt("Mesh_V2/portev2/DoorPillars/Cyan/Pillar_Type1_Cyan_Right/SimplifiedCollider").layer = LayerMask.NameToLayer("PlayerCollisionOnly");
            content.GetChildAt("Mesh_V2/portev2/DoorPillars/Cyan/Pillar_Type1_Cyan_Right/PerfectCollider").layer = LayerMask.NameToLayer("AllExceptPlayer");
            content.GetChildAt("Mesh_V2/portev2/DoorPillars/Cyan/Pillar_Type1_Cyan_Right/SimplifiedPhysicsCollider").layer = LayerMask.NameToLayer("Planet");
            // --------------------------------------------------
            content.GetChildAt("Mesh_V2/portev2/DoorPillars/Red").transform.GetChild(0).tag = "Pillar";
            content.GetChildAt("Mesh_V2/portev2/DoorPillars/Red").transform.GetChild(0).gameObject.GetChildWithName("SimplifiedCollider").layer = LayerMask.NameToLayer("PlayerCollisionOnly");
            content.GetChildAt("Mesh_V2/portev2/DoorPillars/Red").transform.GetChild(0).gameObject.GetChildWithName("PerfectCollider").layer = LayerMask.NameToLayer("AllExceptPlayer");
            content.GetChildAt("Mesh_V2/portev2/DoorPillars/Red").transform.GetChild(0).gameObject.GetChildWithName("SimplifiedPhysicsCollider").layer = LayerMask.NameToLayer("Planet");
            content.GetChildAt("Mesh_V2/portev2/DoorPillars/Red").transform.GetChild(1).tag = "Pillar";
            content.GetChildAt("Mesh_V2/portev2/DoorPillars/Red").transform.GetChild(1).gameObject.GetChildWithName("SimplifiedCollider").layer = LayerMask.NameToLayer("PlayerCollisionOnly");
            content.GetChildAt("Mesh_V2/portev2/DoorPillars/Red").transform.GetChild(1).gameObject.GetChildWithName("PerfectCollider").layer = LayerMask.NameToLayer("AllExceptPlayer");
            content.GetChildAt("Mesh_V2/portev2/DoorPillars/Red").transform.GetChild(1).gameObject.GetChildWithName("SimplifiedPhysicsCollider").layer = LayerMask.NameToLayer("Planet");
            // --------------------------------------------------
            #endregion
            teleport.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            doorScript.activationTrigger.tag = "ActivateTrigger";
            doorScript.activationTrigger.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            doorScript.instantCollider.gameObject.layer = LayerMask.NameToLayer("PlayerCollisionOnly");

            content.SetActive(true);

            initialized = true;
        }
    }
}
