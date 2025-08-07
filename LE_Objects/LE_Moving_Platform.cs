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
        MovingPlatformController script;

        void Awake()
        {
            properties = new Dictionary<string, object>()
            {
                { "waypoints", new List<WaypointData>() }
            };
        }

        public override void ObjectStart(LEScene scene)
        {
            if (scene == LEScene.Playmode)
            {
                script.Activate();
            }

            base.ObjectStart(scene);
        }

        public override void InitComponent()
        {
            GameObject content = gameObject.GetChild("Content");

            script = content.AddComponent<MovingPlatformController>();
            script.accelerationDuration = 0.3f;
            script.accelerationMultiplier = 0.0554f;
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
            script.m_originalMovingSpeed = 3;
            script.movingSpeed = 3;
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
            script.speedrunModeMultiplier = 4;
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
            if (name == "waypoints")
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

            return base.TriggerAction(actionName);
        }
    }
}
