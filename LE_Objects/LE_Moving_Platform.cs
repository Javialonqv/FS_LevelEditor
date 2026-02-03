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
    public class LE_Moving_Platform : LE_Object
    {
        public MovingPlatformController script;

        void Awake()
        {
            if (EditorController.Instance)
            {
                // Even thought MP RigidBody doesn't affect us in editor (in gravity), it causes a visual bug where the Content obj is misplaced.
                // PD: Repeat after me, I HATE PHYSICS. - Jav.
                Destroy(gameObject.GetChild("Content").GetComponent<Rigidbody>());
            }
        }

        public static Dictionary<string, object> GetDefaultProperties()
        {
            return new Dictionary<string, object>()
            {
                { "ActivateOnStart", true },
                { "MovementMode", WaypointMode.TRAVEL_BACK },
                { "MoveSpeed", 5f },
                { "waypoints", new List<WaypointData>() }
            };
        }

        public override void OnInstantiated(LEScene scene)
        {
            if (scene == LEScene.Editor)
            {
                // Set the platform active or not.
                SetMeshOnEditor((bool)GetProperty("ActivateOnStart"));
            }

            base.OnInstantiated(scene);
        }
        public override void ObjectStart(LEScene scene)
        {
            if (scene == LEScene.Playmode)
            {
                if (GetProperty<bool>("ActivateOnStart")) script.Activate();

                // Deceleration set to -1 will disable it completely.
                // Do this on ObjectStart 'cause MPs seem to be reseting it on start or something, idk. - Jav.
                script.decelerationStartDistance = -1f;

                // User will use GLOBAL waypoints to move this MP, instead of the local ones.
                if (waypointSupport.targetWaypointsData.Count > 0 && customWaypointSupport.targetWaypointsData.Count == 0)
                {
                    // Remove the RigidBody because it causes problems.
                    Destroy(contentObject.GetComponent<Rigidbody>());
                }
            }

            base.ObjectStart(scene);
        }

        public override void InitComponent()
        {
            GameObject content = gameObject.GetChild("Content");

            script = content.AddComponent<MovingPlatformController>();
            script.accelerationDuration = 0f;
            script.accelerationMultiplier = 1;
            script.decelerationStartDistance = -1f;
            script.activated = false;
            script.activeDuringKine = true;
            script.additionalMeshFilters = new MeshFilter[0];
            script.allBlocSwitchesOn = false;
            script.alwaysUseLinearJumpMomentum = false;
            script.audios = content.GetComponents<AudioSource>().ToArray();
            script.autoKillZoneEnabling = true;
            script.BlocSwitchs = new GameObject[0];
            script.cachedTransform = content.transform;
            script.canCallOnReachEvent = false;
            script.controlScript = Controls.Instance;
            script.decelerationStartDistance = 1;
            script.hasOnMaterials = false;
            script.hitSound = t_movingPlatform.hitSound;
            script.isSmasher = false;
            script.m_objectsToMove = new Il2CppSystem.Collections.Generic.List<GameObject>();
            script.maxVerticalJumpPositiveBoost = -1;
            script.moveSound = t_movingPlatform.moveSound;
            script.moveSound2 = t_movingPlatform.moveSound2;
            script.moveSoundLoop = t_movingPlatform.moveSoundLoop;
            script.moveSoundStop = t_movingPlatform.moveSoundStop;
            script.movingPlatform = true;
            script.m_originalMovingSpeed = GetProperty<float>("MoveSpeed");
            script.movingSpeed = GetProperty<float>("MoveSpeed");
            script.offMesh = content.GetComponent<MeshRenderer>();
            script.onActivate = new UnityEngine.Events.UnityEvent();
            script.onDeactivate = new UnityEngine.Events.UnityEvent();
            script.onEveryStartMoving = new UnityEngine.Events.UnityEvent();
            script.onEveryStopMoving = new UnityEngine.Events.UnityEvent();
            script.onMesh = content.GetChild("OnMesh_MovingPlatform");
            script.platformCollider = content.GetComponent<BoxCollider>();
            script.playerOnThisPlatform = false;
            script.playMoveSound = true;
            script.pushPlayerSidesCollider = content.GetChild("PushPlayerTrigger").GetComponent<BoxCollider>();
            script.rawUnitsPerSecond = 3;
            script.rb = content.GetComponent<Rigidbody>();
            script.revertIfMoving = false;
            script.speedrunModeMultiplier = 1;
            script.timerBeforeNextWaypoint = 0;
            script.useMeshSwap = false;
            script.verticalBoostMultiplier = 1;

            script.platformCollider.material = t_movingPlatform.platformCollider.material;

            script.GetComponents<AudioSource>()[0].outputAudioMixerGroup = t_movingPlatform.GetComponents<AudioSource>()[0].outputAudioMixerGroup;
            script.GetComponents<AudioSource>()[1].outputAudioMixerGroup = t_movingPlatform.GetComponents<AudioSource>()[1].outputAudioMixerGroup;

            // --------- SETUP TAGS & LAYERS ---------

            content.tag = "MovingPlatform";
            content.GetChild("PlayerLiftTrigger").tag = "MovingPlatform";
            content.GetChild("PlayerLiftTrigger").layer = LayerMask.NameToLayer("Ignore Raycast");
            content.GetChild("ObjectsMoveTrigger").tag = "ObjectsMoveTrigger";
            content.GetChild("ObjectsMoveTrigger").layer = LayerMask.NameToLayer("Ignore Raycast");
            script.pushPlayerSidesCollider.gameObject.tag = "PushPlayer";
            script.pushPlayerSidesCollider.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

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
            else if (name == "MoveSpeed")
            {
                if (value is string)
                {
                    if (Utils.TryParseFloat((string)value, out float result))
                    {
                        properties["MoveSpeed"] = result;
                        return true;
                    }
                }
                else if (value is float)
                {
                    properties["MoveSpeed"] = (float)value;
                    return true;
                }
            }
            else if (name == "MovementMode")
            {
                if (value is int)
                {
                    properties["MovementMode"] = (WaypointMode)value;
                    return true;
                }
                else if (value is WaypointMode)
                {
                    properties["MovementMode"] = value;
                    return true;
                }
            }
            else if (name == "waypoints")
            {
                if (value is List<WaypointData>)
                {
                    properties["waypoints"] = value;
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
            else if (actionName == "InvertState")
            {
                // Check if the platform is currently active
                bool isActive = script.activated;
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

        void SetMeshOnEditor(bool isPlatformActive)
        {
            gameObject.GetChildAt("Content/OnMesh_MovingPlatform").SetActive(isPlatformActive);
        }
    }
}
