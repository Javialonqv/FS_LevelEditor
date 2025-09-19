using FS_LevelEditor.Editor;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using InitialState = FS_LevelEditor.LE_Door.InitialState;
using InitialStateAuto = FS_LevelEditor.LE_Door.InitialStateAuto;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    internal class LE_Door_V2 : LE_Object
    {
        GameObject cyanPillars, redPillars;
        GameObject topPartCyan, bottomPartCyan;
        GameObject topPartRed, bottomPartRed;

        PorteScript doorScript;

        void Awake()
        {
            properties = new Dictionary<string, object>()
            {
                { "IsAuto", false },
                { "InitialState", InitialState.CLOSED },
                { "InitialStateAuto", InitialStateAuto.LOCKED }
            };

            cyanPillars = gameObject.GetChildAt("Content/Mesh_V2/portev2/DoorPillars/Cyan");
            redPillars = gameObject.GetChildAt("Content/Mesh_V2/portev2/DoorPillars/Red");
            topPartCyan = gameObject.GetChildAt("Content/Mesh_V2/portev2/door_V2_parts/partsHolder/onParts/OnTopPart/onPart1Cyan");
            bottomPartCyan = gameObject.GetChildAt("Content/Mesh_V2/portev2/door_V2_parts/partsHolder/onParts/OnBottomPart/onPart2Cyan");
            topPartRed = gameObject.GetChildAt("Content/Mesh_V2/portev2/door_V2_parts/partsHolder/onParts/OnTopPart/onPart1Red");
            bottomPartRed = gameObject.GetChildAt("Content/Mesh_V2/portev2/door_V2_parts/partsHolder/onParts/OnBottomPart/onPart2Red");
        }

        public override void OnInstantiated(LEScene scene)
        {
            if (scene == LEScene.Editor)
            {
                UpdateMeshInEditorAutomatically();
            }

            base.OnInstantiated(scene);
        }
        public override void ObjectStart(LEScene scene)
        {
            if (scene == LEScene.Playmode)
            {
                // To avoid bugs, the trigger is disabled if it's NOT auto.
                gameObject.GetChildAt("Content/ActivateTrigger").SetActive(GetProperty<bool>("IsAuto"));

                if (GetProperty<bool>("IsAuto"))
                {
                    // If set as auto, the Door will be blue and allowed to open by default, force it so it doesn't.
                    if (GetProperty<InitialStateAuto>("InitialStateAuto") == InitialStateAuto.LOCKED)
                    {
                        doorScript.SetAllowOpen(false);
                        doorScript.Invoke("SetToRedColor", 0.1f);
                    }
                }
                else
                {
                    // Door is locked by default, only open it if the attribute says so.
                    if (GetProperty<InitialState>("InitialState") == InitialState.OPEN)
                    {
                        doorScript.Open();
                    }
                }
            }

            base.ObjectStart(scene);
        }

        public override void InitComponent()
        {
            GameObject content = gameObject.GetChild("Content");

            content.SetActive(false);

            doorScript = content.AddComponent<PorteScript>();
            doorScript.activationTrigger = content.GetChild("ActivateTrigger").transform;
            doorScript.activationTriggerCollider = content.GetChild("ActivateTrigger").GetComponent<BoxCollider>();
            doorScript.allCollidersExceptInstant = new Collider[0];
            doorScript.allowOpen = true;
            doorScript.animationSpeed = 1;
            doorScript.BlocSwitchs = new GameObject[0];
            doorScript.closeSound = t_doorV2.closeSound;
            doorScript.defaultIsRed = false;
            doorScript.defaultTriggerState = true;
            doorScript.doorEditorState = false;
            doorScript.doorEditorVisibleState = false;
            doorScript.doorMesh = content.GetChild("Mesh").transform;
            doorScript.doorMeshV2 = content.GetChild("Mesh_V2").transform;
            //doorScript.forceTeleportGO = content.GetChildAt("Mesh/porte1/ForceTeleport_Holder/ForceTeleport_Vent");
            doorScript.forceTeleportGO_MeshV2 = content.GetChildAt("Mesh_V2/portev2/ForceTeleport_Holder/ForceTeleport_Vent");
            doorScript.instantCollider = content.GetChild("InstantCollider").GetComponent<BoxCollider>();
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
            doorScript.portal = content.GetChild("DoorOcclusionPortal").GetComponent<OcclusionPortal>();

            foreach (var state in t_doorV2.doorMeshV2.GetComponent<Animation>())
            {
                var animState = state.Cast<AnimationState>();
                doorScript.doorMeshV2.GetComponent<Animation>().AddClip(animState.clip, animState.name);
            }
            doorScript.doorMeshV2.GetComponent<Animation>().clip = t_doorV2.doorMeshV2.GetComponent<Animation>().clip;

            ForceTeleport teleport = doorScript.forceTeleportGO_MeshV2.AddComponent<ForceTeleport>();
            teleport.considerPlayer = true;
            teleport.considerBooks = true;
            teleport.considerEncKeys = true;
            teleport.considerPowerCores = true;
            teleport.considerTablets = true;
            teleport.LocalXAxisOnly = true;
            teleport.takeClosest = true;
            teleport.teleportPoints = new Il2CppSystem.Collections.Generic.List<Transform>();
            teleport.teleportPoints.Add(doorScript.doorMesh.Find("porte1/TeleportPoint1_Inside"));
            teleport.teleportPoints.Add(doorScript.doorMesh.Find("porte1/TeleportPoint2_Outside"));
            teleport.forceTPPoint_Player = teleport.teleportPoints[0];
            teleport.teleportX = true;
            teleport.teleportY = true;
            teleport.teleportZ = true;
            teleport.useListOfPoints = true;

            // ---------- SETUP TAGS & LAYERS ----------

            content.tag = GetProperty<bool>("IsAuto") ? "PorteAuto" : "Porte";
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
            content.GetChildAt("Mesh_V2/portev2/DoorPillars/Red").transform.GetChild(0).gameObject.GetChild("SimplifiedCollider").layer = LayerMask.NameToLayer("PlayerCollisionOnly");
            content.GetChildAt("Mesh_V2/portev2/DoorPillars/Red").transform.GetChild(0).gameObject.GetChild("PerfectCollider").layer = LayerMask.NameToLayer("AllExceptPlayer");
            content.GetChildAt("Mesh_V2/portev2/DoorPillars/Red").transform.GetChild(0).gameObject.GetChild("SimplifiedPhysicsCollider").layer = LayerMask.NameToLayer("Planet");
            content.GetChildAt("Mesh_V2/portev2/DoorPillars/Red").transform.GetChild(1).tag = "Pillar";
            content.GetChildAt("Mesh_V2/portev2/DoorPillars/Red").transform.GetChild(1).gameObject.GetChild("SimplifiedCollider").layer = LayerMask.NameToLayer("PlayerCollisionOnly");
            content.GetChildAt("Mesh_V2/portev2/DoorPillars/Red").transform.GetChild(1).gameObject.GetChild("PerfectCollider").layer = LayerMask.NameToLayer("AllExceptPlayer");
            content.GetChildAt("Mesh_V2/portev2/DoorPillars/Red").transform.GetChild(1).gameObject.GetChild("SimplifiedPhysicsCollider").layer = LayerMask.NameToLayer("Planet");
            // --------------------------------------------------
            #endregion
            teleport.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            doorScript.activationTrigger.tag = "ActivateTrigger";
            doorScript.activationTrigger.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            doorScript.instantCollider.gameObject.layer = LayerMask.NameToLayer("PlayerCollisionOnly");

            content.SetActive(true);

            initialized = true;
        }

        public override bool SetProperty(string name, object value)
        {
            if (name == "IsAuto")
            {
                if (value is bool)
                {
                    properties["IsAuto"] = (bool)value;
                    UpdateMeshInEditorAutomatically();
                }
            }
            else if (name == "InitialState")
            {
                if (value is int)
                {
                    properties["InitialState"] = (InitialState)value;
                    UpdateMeshInEditorAutomatically();
                    return true;
                }
                else if (value is InitialState)
                {
                    properties["InitialState"] = value;
                    UpdateMeshInEditorAutomatically();
                    return true;
                }
            }
            else if (name == "InitialStateAuto")
            {
                if (value is int)
                {
                    properties["InitialStateAuto"] = (InitialStateAuto)value;
                    UpdateMeshInEditorAutomatically();
                    return true;
                }
                else if (value is InitialStateAuto)
                {
                    properties["InitialStateAuto"] = value;
                    UpdateMeshInEditorAutomatically();
                    return true;
                }
            }

            return base.SetProperty(name, value);
        }
        public override bool TriggerAction(string actionName)
        {
            if (actionName == "Activate")
            {
                if (GetProperty<bool>("IsAuto"))
                {
                    doorScript.SetAllowOpen(true);
                    doorScript.SetToGreenColor();
                }
                else
                {
                    doorScript.Open();
                }
                return true;
            }
            else if (actionName == "Deactivate")
            {
                if (GetProperty<bool>("IsAuto"))
                {
                    doorScript.SetAllowOpen(false);
                    doorScript.SetToRedColor();
                    if (doorScript.open) doorScript.Close(); // Force close in case is open.
                }
                else
                {
                    doorScript.Close();
                }
                return true;
            }
            else if (actionName == "InvertState")
            {
                // Depends if is auto or not, use one or another variable to check if should deactivate or activate the door.
                bool isActive = GetProperty<bool>("IsAuto") ? doorScript.allowOpen : doorScript.open;
                if (isActive)
                {
                    TriggerAction("Deactivate");
                }
                else
                {
                    TriggerAction("Activate");
                }
                return true;
            }
            else if (actionName == "CloseFast")
            {
                if (GetProperty<bool>("IsAuto"))
                {
                    doorScript.SetAllowOpen(false);
                    doorScript.SetToRedColor();
                    doorScript.CloseFast();
                }
                else
                {
                    doorScript.CloseFast();
                }
            }

            return base.TriggerAction(actionName);
        }

        void UpdateMeshInEditorAutomatically()
        {
            // This method is only to force it to update in the EDITOR, not in playmode.
            if (!EditorController.Instance) return;

            if (GetProperty<bool>("IsAuto"))
            {
                UpdateMeshInEditor(GetProperty<InitialStateAuto>("InitialStateAuto"));
            }
            else
            {
                UpdateMeshInEditor(GetProperty<InitialState>("InitialState"));
            }
        }
        void UpdateMeshInEditor(InitialState newState)
        {
            cyanPillars.SetActive(newState == InitialState.OPEN);
            redPillars.SetActive(newState == InitialState.CLOSED);

            topPartCyan.SetActive(newState == InitialState.OPEN);
            bottomPartCyan.SetActive(newState == InitialState.OPEN);

            topPartRed.SetActive(newState == InitialState.CLOSED);
            bottomPartRed.SetActive(newState == InitialState.CLOSED);
        }
        void UpdateMeshInEditor(InitialStateAuto newState)
        {
            cyanPillars.SetActive(newState == InitialStateAuto.UNLOCKED);
            redPillars.SetActive(newState == InitialStateAuto.LOCKED);

            topPartCyan.SetActive(newState == InitialStateAuto.UNLOCKED);
            bottomPartCyan.SetActive(newState == InitialStateAuto.UNLOCKED);

            topPartRed.SetActive(newState == InitialStateAuto.LOCKED);
            bottomPartRed.SetActive(newState == InitialStateAuto.LOCKED);
        }
    }
}
