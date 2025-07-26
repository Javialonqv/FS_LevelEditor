using FS_LevelEditor.Editor;
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
        public enum InitialState { CLOSED, OPEN };
        public enum InitialStateAuto { LOCKED, UNLOCKED };

        public MeshRenderer leftPartRed, leftPartBlue;
        public MeshRenderer rightPartRed, rightPartBlue;

        PorteScript doorScript;

        void Awake()
        {
            properties = new Dictionary<string, object>()
            {
                { "IsAuto", false },
                { "InitialState", InitialState.CLOSED },
                { "InitialStateAuto", InitialStateAuto.LOCKED }
            };

            leftPartRed = gameObject.GetChildAt("Content/Mesh/porte1/gauche/gaucheRed").GetComponent<MeshRenderer>();
            leftPartBlue = gameObject.GetChildAt("Content/Mesh/porte1/gauche").GetComponent<MeshRenderer>();
            rightPartRed = gameObject.GetChildAt("Content/Mesh/porte1/droite/droiteRed").GetComponent<MeshRenderer>();
            rightPartBlue = gameObject.GetChildAt("Content/Mesh/porte1/droite").GetComponent<MeshRenderer>();
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
                    // If set as auto, the Door will be blue blue and allowed to open by default, force it so it doesn't.
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
            doorScript.closeSound = t_door.closeSound;
            doorScript.defaultIsRed = false;
            doorScript.defaultTriggerState = true;
            doorScript.doorEditorState = false;
            doorScript.doorEditorVisibleState = false;
            doorScript.doorMesh = content.GetChildWithName("Mesh").transform;
            //script.doorMeshV2 = content.GetChildWithName("Mesh_V2").transform;
            doorScript.forceTeleportGO = content.GetChildAt("Mesh/porte1/ForceTeleport_Holder/ForceTeleport_Vent");
            //script.forceTeleportGO_MeshV2 = content.GetChildAt("Mesh_V2/portev2/ForceTeleport_Holder/ForceTeleport_Vent");
            doorScript.instantCollider = content.GetChildWithName("InstantCollider").GetComponent<BoxCollider>();
            doorScript.isRed = true;
            doorScript.isSkinV2 = false;
            doorScript.lockingBarsMeshes = new Il2CppSystem.Collections.Generic.List<MeshFilter>();
            doorScript.lockingDeviceIsLocked = true;
            doorScript.m_animation_V1 = doorScript.doorMesh.GetComponent<Animation>();
            //script.m_animation_v2 = script.doorMeshV2.GetComponent<Animation>();
            doorScript.m_animationToUse = doorScript.doorMesh.GetComponent<Animation>();
            doorScript.m_audioSource = content.GetComponent<AudioSource>();
            //script.m_greenPillars = content.GetChildAt("Mesh_V2/portev2/DoorPillars/Cyan");
            //script.m_greenRenderers = new Il2CppSystem.Collections.Generic.List<GameObject>();
            //script.m_greenRenderers.Add(content.GetChildAt("Mesh_V2/portev2/door_V2_parts/partsHolder/onParts/OnTopPart/onPart1Cyan"));
            //script.m_greenRenderers.Add(content.GetChildAt("Mesh_V2/portev2/door_V2_parts/partsHolder/onParts/OnBottomPart/onPart2Cyan"));
            doorScript.m_leftDoorRedRenderer = content.GetChildAt("Mesh/porte1/gauche/gaucheRed").GetComponent<MeshRenderer>();
            doorScript.m_leftDoorRenderer = content.GetChildAt("Mesh/porte1/gauche").GetComponent<MeshRenderer>();
            doorScript.m_onClose = new UnityEngine.Events.UnityEvent();
            doorScript.m_onLock = new UnityEngine.Events.UnityEvent();
            doorScript.m_onOpen = new UnityEngine.Events.UnityEvent();
            doorScript.m_onUnlock = new UnityEngine.Events.UnityEvent();
            //script.m_redPillars = content.GetChildAt("Mesh_V2/portev2/DoorPillars/Red");
            //script.m_redRenderers = new Il2CppSystem.Collections.Generic.List<GameObject>();
            //script.m_redRenderers.Add(content.GetChildAt("Mesh_V2/portev2/door_V2_parts/partsHolder/onParts/OnTopPart/onPart1Red"));
            //script.m_redRenderers.Add(content.GetChildAt("Mesh_V2/portev2/door_V2_parts/partsHolder/onParts/OnBottomPart/onPart2Red"));
            doorScript.m_rightDoorRedRenderer = content.GetChildAt("Mesh/porte1/droite/droiteRed").GetComponent<MeshRenderer>();
            doorScript.m_rightDoorRenderer = content.GetChildAt("Mesh/porte1/droite").GetComponent<MeshRenderer>();
            doorScript.m_switchList = new InterrupteurController[0];
            doorScript.openSound = t_door.openSound;
            doorScript.open = false;
            doorScript.openAtStart = false;
            doorScript.portal = content.GetChildWithName("DoorOcclusionPortal").GetComponent<OcclusionPortal>();

            foreach (var state in t_door.doorMesh.GetComponent<Animation>())
            {
                var animState = state.Cast<AnimationState>();
                doorScript.doorMesh.GetComponent<Animation>().AddClip(animState.clip, animState.name);
            }
            doorScript.doorMesh.GetComponent<Animation>().clip = t_door.doorMesh.GetComponent<Animation>().clip;

            ForceTeleport teleport = doorScript.forceTeleportGO.AddComponent<ForceTeleport>();
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

            content.tag = GetProperty<bool>("IsAuto") ? "PorteAuto" : "Porte";
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
            leftPartRed.enabled = newState == InitialState.CLOSED;
            leftPartBlue.enabled = newState == InitialState.OPEN;

            rightPartRed.enabled = newState == InitialState.CLOSED;
            rightPartBlue.enabled = newState == InitialState.OPEN;
        }
        void UpdateMeshInEditor(InitialStateAuto newState)
        {
            leftPartRed.enabled = newState == InitialStateAuto.LOCKED;
            leftPartBlue.enabled = newState == InitialStateAuto.UNLOCKED;

            rightPartRed.enabled = newState == InitialStateAuto.LOCKED;
            rightPartBlue.enabled = newState == InitialStateAuto.UNLOCKED;
        }
    }
}
