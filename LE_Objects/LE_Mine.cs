using FS_LevelEditor;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using FS_LevelEditor.Editor;
using Il2CppAmazingAssets.TerrainToMesh;

namespace FS_LevelEditor
{
	[MelonLoader.RegisterTypeInIl2Cpp]
	public class LE_Mine : LE_Object
	{
		Laser_H_Controller mine;
		GameObject contactRangeSphere;
		GameObject proximityRangeSphere;
		GameObject remoteRangeSphere;
		GameObject sphereRange;

        public override string contentObjectName => "Mine";

		void Awake()
		{
			contactRangeSphere = gameObject.GetChildAt("Mine/SphereRange/Contact");
			proximityRangeSphere = gameObject.GetChildAt("Mine/SphereRange/Proximity");
			remoteRangeSphere = gameObject.GetChildAt("Mine/SphereRange/Remote");
			sphereRange = remoteRangeSphere.transform.parent.gameObject;
		}

        public static Dictionary<string, object> GetDefaultProperties()
        {
            return new Dictionary<string, object>()
            {
                { "ActivateOnStart", true },
                { "InstaKill", false },
                { "DamageThroughWalls", true },
                { "BreakWindows", false },
                { "ExplosionDamage", 34 },
                { "ContactRadius", 3f },
                { "RemoteRadius", 1f },
                { "ProximityRadius", 5f },
            };
        }

        public override void OnInstantiated(LEScene scene)
		{
			if (scene == LEScene.Editor)
			{
				SetMeshOnEditor((bool)GetProperty("ActivateOnStart"));
                SetContactRangeSphereScale(GetProperty<float>("ContactRadius"));
                SetProximityRangeSphereScale(GetProperty<float>("ProximityRadius"));
                SetRemoteRangeSphereScale(GetProperty<float>("RemoteRadius"));
            }

			if (scene == LEScene.Playmode)
			{
				if (contactRangeSphere != null) Destroy(contactRangeSphere.transform.parent.gameObject);
			}

			base.OnInstantiated(scene);
		}

        public override void ObjectStart(LEScene scene)
        {
			// Force the mine colliders to be disabled after 0.3s since mine's original script enabled them again depending of current SM state.
			if (scene == LEScene.Playmode && !collision)
			{
				Invoke(nameof(ForceDisableCollidersDelayed), 0.3f);
			}

            base.ObjectStart(scene);
        }
		void ForceDisableCollidersDelayed()
		{
            SetCollidersState(false);
        }

		public override void InitComponent()
		{
			Laser_H_Controller template = t_mine;

			gameObject.GetChild("Mine").SetActive(false);
			mine = gameObject.GetChild("Mine").AddComponent<Laser_H_Controller>();
			#region Rotate
			Rotate mine_rot = gameObject.GetChild("Mine").AddComponent<Rotate>();
			mine_rot.objectToRotate = gameObject.GetChildAt("Mine/MeshOn").transform;
			mine_rot.world = false;
			mine_rot.speed = new Vector3(0, .5f, 0);
			mine_rot.reactToTaser = false;
			mine_rot.timeOffAfterShot = 2;
			mine_rot.useQuaternion = true;
			#endregion
			#region Mine specific
			mine.isMine = true;
			mine.explosionDamage = GetProperty<int>("ExplosionDamage");
			mine.contactExplosionRadius = GetProperty<float>("ContactRadius");
			mine.remoteExplosionRadius = GetProperty<float>("RemoteRadius");
			mine.contactExplosionThroughWalls = true;
			mine.remoteExplosionThroughWalls = GetProperty<bool>("DamageThroughWalls");
			mine.explodeProximityMines = true;
			mine.proximityRadius = GetProperty<float>("ProximityRadius");
			mine.explodeByProximity = true;
			mine.breakWindowsOnExplode = true;
			mine.constant = true;
			#endregion
			#region Rendering
			mine.hasParticles = false;
			mine.useSSR = true;
			mine.forceDynLighting = false;
			mine.flareMultiplier = 1;
			mine.showIfTouchesNothing = false;
			mine.isUnderwater = false;
			#endregion
			#region Other
			mine.onTurnOn = new UnityEngine.Events.UnityEvent();
			mine.onTurnOff = new UnityEngine.Events.UnityEvent();
			mine.onExplode = new UnityEngine.Events.UnityEvent();
			mine.onActivate = new UnityEngine.Events.UnityEvent();
			mine.onDeactivate = new UnityEngine.Events.UnityEvent();
			mine.currentWaypointIndex = 0;
			mine.rb = null;
			mine.laserOriginPoint = gameObject.GetChildAt("Mine/LaserOriginPoint").transform;
			mine.rotateCom = mine_rot;
			mine.useBoxCast = false;
			mine.hasOnMaterials = false;
			mine.controlScript = Controls.Instance;
			mine.safetyCollider = gameObject.GetChildAt("Mine/SafetyCollider");
			mine.collisionOn = gameObject.GetChildAt("Mine/MeshOn").GetComponent<BoxCollider>();
			mine.collisionOff = gameObject.GetChildAt("Mine/MeshOff").GetComponent<BoxCollider>();
			mine.currentKine = null;
			mine.explodeWithInvalidPosObj = true;
			mine.cachedGO = mine.gameObject;
			mine.cachedTransform = mine.transform;
			mine.currentForward = Vector3.zero;
			mine.positionWithLaserStartPointOffset = Vector3.zero;
			mine.mineExplosion = t_mine.mineExplosion;
			mine.explosionHolder = gameObject.GetChildAt("Mine/ExplosionHolder").transform;
			mine.explosionSound = t_mine.explosionSound;
			mine.proximityLayer = t_mine.proximityLayer;
			mine.explosionCheckLayer = t_mine.explosionCheckLayer;
			mine.disableDistance = 300;
			mine.m_laserOn = t_mine.m_laserOn;
			mine.m_laserOff = t_mine.m_laserOff;
			mine.m_currentLaserImpact = gameObject.GetChildAt("Mine/LaserPointRed");
			mine.m_currentLaserImpactT = gameObject.GetChildAt("Mine/LaserPointRed").transform;
			mine.m_currentLaserImpactScript = gameObject.GetChildAt("Mine/LaserPointRed").GetComponent<LaserPoint>();
			mine.Line = mine.GetComponent<LineRenderer>();
			mine.transparentMat = t_mine.transparentMat;
			mine.cutoutMat = t_mine.cutoutMat;
			mine.layer = t_mine.layer;
			mine.hitColliderGO = null;
			mine.hitColliderGOPresent = false;
			mine.m_currentHitInfoCollider = null;
			mine.firstTempDelay = 0;
			mine.firstTempDelayIsOff = false;
			mine.loopAudioSource = mine.GetComponent<AudioSource>();
			mine.onOffAudioSource = gameObject.GetChildAt("Mine/Audio2").GetComponent<AudioSource>();
			mine.explosionAudioSource = gameObject.GetChildAt("Mine/ExplosionHolder").GetComponent<AudioSource>();
			mine.m_onMesh = gameObject.GetChildAt("Mine/MeshOn");
			mine.m_offMesh = gameObject.GetChildAt("Mine/MeshOff");
			mine.timer = 0;
			mine.tempOff = false;
			mine.timerBeforeNextWaypoint = 0;
			mine.currentWaypoint = null;
			mine.currentWaypointPos = Vector3.zero;
			mine.laserSound = t_mine.laserSound;
			mine.killZone = null;
			mine.unselectedColor = Color.black;
			mine.selectedColor = Color.black;
			mine.isGodray = false;
			mine.m_light = gameObject.GetChildAt("Mine/Light").GetComponent<Light>();
			mine.m_flare = gameObject.GetChildAt("Mine/Light").GetComponent<LensFlare>();
			mine.flareMultiplier = 1;
			mine.activeEditorState = true;
			mine.constantEditorState = true;
			mine.showIfTouchesNothing = true;
			mine.checkpoints = new GameObject[0];
			#endregion
			#region OSS
			ObjectStateSync sync = gameObject.GetChildAt("Mine").AddComponent<ObjectStateSync>();
			sync.assignNewParent = true;
			sync.objectGO = gameObject.GetChildAt("Mine/LaserRailHolder");
			sync.objectT = gameObject.GetChildAt("Mine/LaserRailHolder").transform;
			sync.stateInEditor = true;
			sync.firstOnEnable = true;
			#endregion
			#region Layers
			gameObject.GetChild("Mine").tag = "Laser";
			gameObject.GetChildAt("Mine/MeshOn").layer = LayerMask.NameToLayer("PlayerCollisionOnly");
			gameObject.GetChildAt("Mine/MeshOff").layer = LayerMask.NameToLayer("PlayerCollisionOnly");
			gameObject.GetChildAt("Mine/SafetyCollider").layer = LayerMask.NameToLayer("IgnorePlayerCollision");
			gameObject.GetChildAt("Mine/AutoAimCollider").tag = "AutoAim";
			gameObject.GetChildAt("Mine/AutoAimCollider").layer = LayerMask.NameToLayer("Water");
			gameObject.GetChildAt("Mine/AutoAimOverridePoint").tag = "AutoAim";
			gameObject.GetChildAt("Mine/AutoAimOverridePoint").layer = LayerMask.NameToLayer("Water");
			#endregion
			bool activateOnStart = (bool)GetProperty("ActivateOnStart");
			if (activateOnStart)
			{
				Invoke("ActivateMineDelayed", 0.2f);
			}

			gameObject.GetChild("Mine").SetActive(true);
			initialized = true;
		}

		// This method is meant to be invoked with Invoke().
		void ActivateMineDelayed()
		{
			mine.Activate();
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
			else if (name == "InstaKill")
			{
				if (value is bool)
				{
					properties["InstaKill"] = (bool)value;
					return true;
				}
			}
			else if (name == "ExplosionDamage")
			{
				if (value is string)
				{
					if (int.TryParse((string)value, out int result))
					{
						properties["ExplosionDamage"] = result;
						return true;
					}
				}
				else if (value is int)
				{
					properties["ExplosionDamage"] = (int)value;
					return true;
				}
			}
			else if (name == "ContactRadius")
			{
				if (value is string)
				{
					if (Utils.TryParseFloat((string)value, out float result))
					{
						properties["ContactRadius"] = result;
						SetContactRangeSphereScale(result);
						return true;
					}
				}
				else if (value is float)
				{
					properties["ContactRadius"] = (float)value;
					SetContactRangeSphereScale((float)value);
					return true;
				}
			}
			else if (name == "RemoteRadius")
			{
				if (value is string)
				{
					if (Utils.TryParseFloat((string)value, out float result))
					{
						properties["RemoteRadius"] = result;
						SetRemoteRangeSphereScale(result);
						return true;
					}
				}
				else if (value is float)
				{
					properties["RemoteRadius"] = (float)value;
					SetRemoteRangeSphereScale((float)value);
					return true;
				}
			}
			else if (name == "ProximityRadius")
			{
				if (value is string)
				{
					if (Utils.TryParseFloat((string)value, out float result))
					{
						properties["ProximityRadius"] = result;
						SetProximityRangeSphereScale(result);
						return true;
					}
				}
				else if (value is float)
				{
					properties["ProximityRadius"] = (float)value;
					SetProximityRangeSphereScale((float)value);
					return true;
				}
			}
            else if (name == "DamageThroughWalls")
            {
                if (value is bool)
                {
                    properties["DamageThroughWalls"] = (bool)value;
                    return true;
                }
            }
            else if (name == "BreakWindows")
            {
                if (value is bool)
                {
                    properties["BreakWindows"] = (bool)value;
                    return true;
                }
            }
            return base.SetProperty(name, value);
		}

		public override bool TriggerAction(string actionName)
		{
			if (actionName == "Activate")
			{
				mine.Activate();
				return true;
			}
			else if (actionName == "Deactivate")
			{
				mine.Deactivate();
				return true;
			}
			else if (actionName == "ToggleActivated")
			{
				if (mine.activated)
				{
					mine.Deactivate();
				}
				else
				{
					mine.Activate();
				}
				return true;
			}

			return base.TriggerAction(actionName);
		}

		void SetMeshOnEditor(bool isLaserOn)
		{
			gameObject.GetChildAt("Mine/MeshOff").GetComponent<MeshRenderer>().enabled = !isLaserOn;
			gameObject.GetChildAt("Mine/MeshOn").GetComponent<MeshRenderer>().enabled = isLaserOn;
		}

        void SetContactRangeSphereScale(float range)
        {
            if (contactRangeSphere != null)
            {
                // Divide by parent scale to compensate for the mine's default scale
                Vector3 parentScale = gameObject.transform.localScale;
                Vector3 rangeSphereScale = new Vector3(
                    (range * 2) / parentScale.x,
                    (range * 2) / parentScale.y,
                    (range * 2) / parentScale.z
                );
                contactRangeSphere.transform.localScale = rangeSphereScale;
            }
        }

        void SetProximityRangeSphereScale(float range)
        {
            if (proximityRangeSphere != null)
            {
                // Divide by parent scale to compensate for the mine's default scale
                Vector3 parentScale = gameObject.transform.localScale;
                Vector3 rangeSphereScale = new Vector3(
                    (range * 2) / parentScale.x,
                    (range * 2) / parentScale.y,
                    (range * 2) / parentScale.z
                );
                proximityRangeSphere.transform.localScale = rangeSphereScale;
            }
        }

        void SetRemoteRangeSphereScale(float range)
        {
            if (remoteRangeSphere != null)
            {
                // Divide by parent scale to compensate for the mine's default scale
                Vector3 parentScale = gameObject.transform.localScale;
                Vector3 rangeSphereScale = new Vector3(
                    (range * 2) / parentScale.x,
                    (range * 2) / parentScale.y,
                    (range * 2) / parentScale.z
                );
                remoteRangeSphere.transform.localScale = rangeSphereScale;
            }
        }

        public override void OnSelect()
		{
			sphereRange.SetActive(true);
			sphereRange.SetActiveRecursively(true);
            base.OnSelect();
		}

		public override void OnDeselect(GameObject nextSelectedObj)
		{
			sphereRange.SetActive(false);
			sphereRange.SetActiveRecursively(false);
            base.OnDeselect(nextSelectedObj);
		}

		// Skip the range spheres when setting object color (same pattern as LE_Ceiling_Light)
		public override void SetObjectColor(LEObjectContext context)
		{
			foreach (var renderer in gameObject.TryGetComponents<MeshRenderer>())
			{
				// Skip the range spheres
				if (sphereRange != null && renderer.transform.IsChildOf(sphereRange.transform))
					continue;

				// Skip waypoints
				if (canHaveWaypoints)
				{
					if (waypointSupport && renderer.transform.IsChildOf(waypointSupport.waypointsParent)) continue;
					if (customWaypointSupport && renderer.transform.IsChildOf(customWaypointSupport.waypointsParent)) continue;
				}

                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (!materials[i].HasProperty("_Color")) continue;

                    Color toSet = LE_Object.GetObjectColorForObject(objectType.Value, context);
                    toSet.a = materials[i].color.a;

                    materials[i] = MaterialUtils.GetMaterialWithColor(materials[i], toSet);
                }
                renderer.sharedMaterials = materials;
			}
		}

		void OnDestroy()
		{
			// Clean up mine component reference
			if (mine != null)
			{
				// Clear Unity Events to prevent memory leaks
				if (mine.onTurnOn != null)
				{
					mine.onTurnOn.RemoveAllListeners();
				}
				if (mine.onTurnOff != null)
				{
					mine.onTurnOff.RemoveAllListeners();
				}
				if (mine.onExplode != null)
				{
					mine.onExplode.RemoveAllListeners();
				}
				if (mine.onActivate != null)
				{
					mine.onActivate.RemoveAllListeners();
				}
				if (mine.onDeactivate != null)
				{
					mine.onDeactivate.RemoveAllListeners();
				}

				// Clear references
				mine.rotateCom = null;
				mine.laserOriginPoint = null;
				mine.safetyCollider = null;
				mine.collisionOn = null;
				mine.collisionOff = null;
				mine.explosionHolder = null;
				mine.m_currentLaserImpact = null;
				mine.m_currentLaserImpactT = null;
				mine.m_currentLaserImpactScript = null;
				mine.Line = null;
				mine.loopAudioSource = null;
				mine.onOffAudioSource = null;
				mine.explosionAudioSource = null;
				mine.m_onMesh = null;
				mine.m_offMesh = null;
				mine.m_light = null;
				mine.m_flare = null;

				mine = null;
			}

			// Cancel any pending invokes
			CancelInvoke("ActivateMineDelayed");

			// Clear range sphere references
			contactRangeSphere = null;
			proximityRangeSphere = null;
			remoteRangeSphere = null;
		}

        //public override void SetCollidersStateForEdgeCase(bool newEnabledState)
        //{
        //    contentObject.GetChildAt("MeshOn").GetComponent<BoxCollider>().enabled = newEnabledState;
        //    contentObject.GetChildAt("MeshOff").GetComponent<BoxCollider>().enabled = newEnabledState;
        //}
	}
}

[HarmonyLib.HarmonyPatch(typeof(Laser_H_Controller), nameof(Laser_H_Controller.OnTouchPlayer))]
public static class MineInstaKillPatch
{
	public static void Prefix(Laser_H_Controller __instance)
	{
		if (__instance.transform.parent != null && __instance.transform.parent.GetComponent<LE_Mine>())
		{
			if ((bool)__instance.transform.parent.GetComponent<LE_Mine>().GetProperty("InstaKill"))
			{
				Controls.Instance.KillCharacter(true);
			}
		}
	}
}