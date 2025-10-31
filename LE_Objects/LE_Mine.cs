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

		void Awake()
		{
			properties = new Dictionary<string, object>()
			{
				{ "ActivateOnStart", true },
				{ "InstaKill", false },
				{ "ExplosionDamage", 34 },
				{ "ContactRadius", 1f },
				{ "RemoteRadius", 1f },
				{ "ProximityRadius", 1f },
			};
		}

		public override void OnInstantiated(LEScene scene)
		{
			if (scene == LEScene.Editor)
			{
				SetMeshOnEditor((bool)GetProperty("ActivateOnStart"));
			}

			base.OnInstantiated(scene);
		}

		public override void InitComponent()
		{
			Laser_H_Controller template = t_mine;

			gameObject.GetChild("Content").SetActive(false);
			mine = gameObject.GetChild("Content").AddComponent<Laser_H_Controller>();
			#region Rotate
			Rotate mine_rot = gameObject.GetChild("Content").AddComponent<Rotate>();
			mine_rot.objectToRotate = gameObject.GetChildAt("Content/MeshOn").transform;
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
			mine.remoteExplosionThroughWalls = true;
			mine.explodeProximityMines = true;
			mine.proximityRadius = GetProperty<float>("ProximityRadius");
			mine.explodeByProximity = true;
			mine.breakWindowsOnExplode = false;
			mine.constant = true;
			#endregion
			#region Rendering
			mine.hasParticles = true;
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
			mine.laserOriginPoint = gameObject.GetChildAt("Content/LaserOriginPoint").transform;
			mine.rotateCom = mine_rot;
			mine.useBoxCast = false;
			mine.hasOnMaterials = false;
			mine.controlScript = Controls.Instance;
			mine.safetyCollider = gameObject.GetChildAt("Content/SafetyCollider");
			mine.collisionOn = gameObject.GetChildAt("Content/MeshOn").GetComponent<BoxCollider>();
			mine.collisionOff = gameObject.GetChildAt("Content/MeshOff").GetComponent<BoxCollider>();
			mine.currentKine = null;
			mine.explodeWithInvalidPosObj = true;
			mine.cachedGO = mine.gameObject;
			mine.cachedTransform = mine.transform;
			mine.currentForward = Vector3.zero;
			mine.positionWithLaserStartPointOffset = Vector3.zero;
			mine.mineExplosion = t_mine.mineExplosion;
			mine.explosionHolder = gameObject.GetChildAt("Content/ExplosionHolder").transform;
			mine.explosionSound = t_mine.explosionSound;
			mine.proximityLayer = t_mine.proximityLayer;
			mine.explosionCheckLayer = t_mine.explosionCheckLayer;
			mine.disableDistance = 300;
			mine.m_laserOn = t_mine.m_laserOn;
			mine.m_laserOff = t_mine.m_laserOff;
			mine.m_currentLaserImpact = gameObject.GetChildAt("Content/LaserPointRed");
			mine.m_currentLaserImpactT = gameObject.GetChildAt("Content/LaserPointRed").transform;
			mine.m_currentLaserImpactScript = gameObject.GetChildAt("Content/LaserPointRed").GetComponent<LaserPoint>();
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
			mine.onOffAudioSource = gameObject.GetChildAt("Content/Audio2").GetComponent<AudioSource>();
			mine.explosionAudioSource = gameObject.GetChildAt("Content/ExplosionHolder").GetComponent<AudioSource>();
			mine.m_onMesh = gameObject.GetChildAt("Content/MeshOn");
			mine.m_offMesh = gameObject.GetChildAt("Content/MeshOff");
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
			mine.m_light = gameObject.GetChildAt("Content/Light").GetComponent<Light>();
			mine.m_flare = gameObject.GetChildAt("Content/Light").GetComponent<LensFlare>();
			mine.flareMultiplier = 1;
			mine.activeEditorState = true;
			mine.constantEditorState = true;
			mine.showIfTouchesNothing = true;
			mine.checkpoints = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<GameObject>(0);
			#endregion
			#region OSS
			ObjectStateSync sync = gameObject.GetChildAt("Content").AddComponent<ObjectStateSync>();
			sync.assignNewParent = true;
			sync.objectGO = gameObject.GetChildAt("Content/LaserRailHolder");
			sync.objectT = gameObject.GetChildAt("Content/LaserRailHolder").transform;
			sync.stateInEditor = true;
			sync.firstOnEnable = true;
			#endregion
			#region Layers
			gameObject.GetChild("Content").tag = "Laser";
			gameObject.GetChildAt("Content/MeshOn").layer = LayerMask.NameToLayer("PlayerCollisionOnly");
			gameObject.GetChildAt("Content/MeshOff").layer = LayerMask.NameToLayer("PlayerCollisionOnly");
			gameObject.GetChildAt("Content/SafetyCollider").layer = LayerMask.NameToLayer("IgnorePlayerCollision");
			gameObject.GetChildAt("Content/AutoAimCollider").tag = "AutoAim";
			gameObject.GetChildAt("Content/AutoAimCollider").layer = LayerMask.NameToLayer("Water");
			gameObject.GetChildAt("Content/AutoAimOverridePoint").tag = "AutoAim";
			gameObject.GetChildAt("Content/AutoAimOverridePoint").layer = LayerMask.NameToLayer("Water");
			#endregion
			bool activateOnStart = (bool)GetProperty("ActivateOnStart");
			if (activateOnStart)
			{
				Invoke("ActivateMineDelayed", 0.2f);
			}

			gameObject.GetChild("Content").SetActive(true);
			gameObject.GetChild("Content").name = "Mine";
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
						return true;
					}
				}
				else if (value is float)
				{
					properties["ContactRadius"] = (float)value;
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
						return true;
					}
				}
				else if (value is float)
				{
					properties["RemoteRadius"] = (float)value;
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
						return true;
					}
				}
				else if (value is float)
				{
					properties["ProximityRadius"] = (float)value;
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
			gameObject.GetChildAt("Content/MeshOff").GetComponent<MeshRenderer>().enabled = !isLaserOn;
			gameObject.GetChildAt("Content/MeshOn").GetComponent<MeshRenderer>().enabled = isLaserOn;
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
		}
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