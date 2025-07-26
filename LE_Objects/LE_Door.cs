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
    public class LE_Door : LE_Object
    {
        public override void InitComponent()
        {
            GameObject content = gameObject.GetChildWithName("Content");

            content.SetActive(false);

            PorteScript script = content.AddComponent<PorteScript>();
            script.activationTrigger = content.GetChildWithName("ActivateTrigger").transform;
            script.activationTriggerCollider = content.GetChildWithName("ActivateTrigger").GetComponent<BoxCollider>();
            script.allCollidersExceptInstant = new Collider[0];
            script.allowOpen = true;
            script.animationSpeed = 1;
            script.BlocSwitchs = new GameObject[0];
            script.closeSound = t_door.closeSound;
            script.defaultIsRed = false;
            script.defaultTriggerState = true;
            script.doorEditorState = false;
            script.doorEditorVisibleState = false;
            script.doorMesh = content.GetChildWithName("Mesh").transform;
            //script.doorMeshV2 = content.GetChildWithName("Mesh_V2").transform;
            script.forceTeleportGO = content.GetChildAt("Mesh/porte1/ForceTeleport_Holder/ForceTeleport_Vent");
            //script.forceTeleportGO_MeshV2 = content.GetChildAt("Mesh_V2/portev2/ForceTeleport_Holder/ForceTeleport_Vent");
            script.instantCollider = content.GetChildWithName("InstantCollider").GetComponent<BoxCollider>();
            script.isRed = true;
            script.isSkinV2 = false;
            script.lockingBarsMeshes = new Il2CppSystem.Collections.Generic.List<MeshFilter>();
            script.lockingDeviceIsLocked = true;
            script.m_animation_V1 = script.doorMesh.GetComponent<Animation>();
            //script.m_animation_v2 = t_door.m_animation_v2;
            script.m_animationToUse = script.doorMesh.GetComponent<Animation>();
            script.m_audioSource = content.GetComponent<AudioSource>();
            //script.m_greenPillars = content.GetChildAt("Mesh_V2/portev2/DoorPillars/Cyan");
            //script.m_greenRenderers = new Il2CppSystem.Collections.Generic.List<GameObject>();
            //script.m_greenRenderers.Add(content.GetChildAt("Mesh_V2/portev2/door_V2_parts/partsHolder/onParts/OnTopPart/onPart1Cyan"));
            //script.m_greenRenderers.Add(content.GetChildAt("Mesh_V2/portev2/door_V2_parts/partsHolder/onParts/OnBottomPart/onPart2Cyan"));
            script.m_leftDoorRedRenderer = content.GetChildAt("Mesh/porte1/gauche/gaucheRed").GetComponent<MeshRenderer>();
            script.m_leftDoorRenderer = content.GetChildAt("Mesh/porte1/gauche").GetComponent<MeshRenderer>();
            script.m_onClose = new UnityEngine.Events.UnityEvent();
            script.m_onLock = new UnityEngine.Events.UnityEvent();
            script.m_onOpen = new UnityEngine.Events.UnityEvent();
            script.m_onUnlock = new UnityEngine.Events.UnityEvent();
            //script.m_redPillars = content.GetChildAt("Mesh_V2/portev2/DoorPillars/Red");
            //script.m_redRenderers = new Il2CppSystem.Collections.Generic.List<GameObject>();
            //script.m_redRenderers.Add(content.GetChildAt("Mesh_V2/portev2/door_V2_parts/partsHolder/onParts/OnTopPart/onPart1Red"));
            //script.m_redRenderers.Add(content.GetChildAt("Mesh_V2/portev2/door_V2_parts/partsHolder/onParts/OnBottomPart/onPart2Red"));
            script.m_rightDoorRedRenderer = content.GetChildAt("Mesh/porte1/droite/droiteRed").GetComponent<MeshRenderer>();
            script.m_rightDoorRenderer = content.GetChildAt("Mesh/porte1/droite").GetComponent<MeshRenderer>();
            script.m_switchList = new InterrupteurController[0];
            script.openSound = t_door.openSound;
            script.open = false;
            script.openAtStart = false;
            script.portal = content.GetChildWithName("DoorOcclusionPortal").GetComponent<OcclusionPortal>();

            foreach (var state in t_door.doorMesh.GetComponent<Animation>())
            {
                var animState = state.Cast<AnimationState>();
                script.doorMesh.GetComponent<Animation>().AddClip(animState.clip, animState.name);
            }
            script.doorMesh.GetComponent<Animation>().clip = t_door.doorMesh.GetComponent<Animation>().clip;

            ForceTeleport teleport = script.forceTeleportGO.AddComponent<ForceTeleport>();
            teleport.considerBooks = true;
            teleport.considerEncKeys = true;
            teleport.considerPowerCores = true;
            teleport.considerTablets = true;
            teleport.LocalXAxisOnly = true;
            teleport.takeClosest = true;
            teleport.teleportPoints = new Il2CppSystem.Collections.Generic.List<Transform>();
            teleport.teleportPoints.Add(script.doorMesh.Find("porte1/TeleportPoint1_Inside"));
            teleport.teleportPoints.Add(script.doorMesh.Find("porte1/TeleportPoint2_Outside"));
            teleport.teleportX = true;
            teleport.teleportY = true;
            teleport.teleportZ = true;
            teleport.useListOfPoints = true;

            // ---------- SETUP TAGS & LAYERS ----------

            content.tag = "PorteAuto";
            teleport.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            script.activationTrigger.tag = "ActivateTrigger";
            script.activationTrigger.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            script.instantCollider.gameObject.layer = LayerMask.NameToLayer("PlayerCollisionOnly");

            content.SetActive(true);
            initialized = true;
        }
    }
}
