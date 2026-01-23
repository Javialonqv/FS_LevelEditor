using FS_LevelEditor.Playmode.Patches;
using FS_LevelEditor.SaveSystem;
using Harmony;
using Il2Cpp;
using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;

namespace FS_LevelEditor.Playmode
{
	[RegisterTypeInIl2Cpp]
	public class PlayModeController : MonoBehaviour
	{
		public static PlayModeController Instance;
		Il2CppAssetBundle LEBundle;

		public string levelFileNameWithoutExtension;
		public string levelName;

		GameObject editorObjectsRootFromBundle;
		List<string> categories = new List<string>();
		Dictionary<LE_Object.ObjectType, GameObject> allCategoriesObjects = new();
		List<Dictionary<LE_Object.ObjectType, GameObject>> allCategoriesObjectsSorted = new();
		GameObject[] otherObjectsFromBundle;
		public GameObject levelObjectsParent;

		public Dictionary<string, object> globalProperties = LevelData.GetDefaultGlobalProperties();

		GameObject backToLEButton;

		public List<LE_Object> currentInstantiatedObjects = new List<LE_Object>();
		public int deathsInCurrentLevel = 0;
		public List<LE_Screen> screensOnTheLevel = new List<LE_Screen>();
		public List<LE_Small_Screen> smallScreensOnTheLevel = new List<LE_Small_Screen>();

		public bool endTriggerReached = false;
		int totalUpgradeCount = 0;

		// Objectives management
		public Dictionary<string, ObjectiveController> activeObjectives = new Dictionary<string, ObjectiveController>();
		private string lastObj = null;

		void Awake()
		{
			Instance = this;

			LoadAssetBundle();
			levelObjectsParent = new GameObject("LevelObjects");
			levelObjectsParent.transform.position = Vector3.zero;
			CreateBackToLEButton();
			PlaymodePauseMenuPatcher.Create();

			deathsInCurrentLevel = Melon<Core>.Instance.totalDeathsInCurrentPlaymodeSession;

			Invoke("DisableTheCurrentScene", 0.2f);
        }

		void LoadAssetBundle()
		{
			Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FS_LevelEditor.level_editor");
			byte[] bytes = new byte[stream.Length];
			stream.Read(bytes);

			LEBundle = Il2CppAssetBundleManager.LoadFromMemory(bytes);

			editorObjectsRootFromBundle = LEBundle.Load<GameObject>("LevelObjectsRoot");
			editorObjectsRootFromBundle.hideFlags = HideFlags.DontUnloadUnusedAsset;

			foreach (var child in editorObjectsRootFromBundle.GetChilds())
			{
				categories.Add(child.name);
			}


			foreach (var categoryObj in editorObjectsRootFromBundle.GetChilds())
			{
				Dictionary<LE_Object.ObjectType, GameObject> categoryObjects = new();

				foreach (var obj in categoryObj.GetChilds())
				{
					if (obj.name == "None") continue;

					var objectType = LE_Object.ConvertNameToObjectType(obj.name);
					if (objectType == null) continue; // JUST IN CASE.

					categoryObjects.Add(objectType.Value, obj);
					allCategoriesObjects.Add(objectType.Value, obj);
				}

				allCategoriesObjectsSorted.Add(categoryObjects);
			}

			otherObjectsFromBundle = LEBundle.Load<GameObject>("OtherObjects").GetChilds();
		}
		public GameObject LoadOtherObjectInBundle(string objectName)
		{
			if (otherObjectsFromBundle == null) return null;

			GameObject toReturn = otherObjectsFromBundle.FirstOrDefault(obj => obj && obj.name == objectName);

			if (objectName == "EditorLine")
			{
				toReturn.GetComponent<LineRenderer>().material.shader = Shader.Find("Sprites/Default");
			}

			return toReturn;
		}
		public void UnloadBundle()
		{
			LEBundle.Unload(false);
		}

		void Start()
		{
			TeleportPlayer();
			ConfigureGlobalProperties();
			MelonCoroutines.Start(SetupEnvCam());
			UnloadBundle();
		}

		void CreateBackToLEButton()
		{
			GameObject template = GameObject.Find("MainMenu/Camera/Holder/Main/LargeButtons/2_Chapters");
			backToLEButton = Instantiate(template, template.transform.parent);
			backToLEButton.name = "4_BackToLE";
			Destroy(backToLEButton.GetComponent<ButtonController>());
			Destroy(backToLEButton.GetChild("Label").GetComponent<UILocalize>());
			backToLEButton.GetChild("Label").GetComponent<UILabel>().text = "Back to Level Editor";

			backToLEButton.GetComponent<UIButton>().onClick.Add(new EventDelegate(this, nameof(GoBackToLEWhileInPlayMode)));

			backToLEButton.SetActive(true);
		}
		void GoBackToLEWhileInPlayMode()
		{
			Invoke("DestroyBackToLEButton", 0.2f);
			LE_MenuUIManager.Instance.GoBackToLEWhileInPlayMode(levelFileNameWithoutExtension, levelName);
		}
		void DestroyBackToLEButton()
		{
			Destroy(backToLEButton);
		}

		void DisableTheCurrentScene()
		{
			GameObject[] sceneObjects = SceneManager.GetActiveScene().GetRootGameObjects();

			foreach (GameObject obj in sceneObjects)
			{
				if (obj.name == gameObject.name) continue;
				if (obj.name == "Character") continue;
				if (obj.name == "FootStepController") continue;
				if (obj.name == "Checkpoints") continue;
				if (obj.name == "LevelObjects") continue;
				if (obj.name == "Player") continue;
				if (obj.name == "GUI") continue;
				if (obj.name == "2DGUI") continue;

				obj.SetActive(false);
			}
		}
        void TeleportPlayer()
		{
			LE_Player_Spawn spawn = FindObjectOfType<LE_Player_Spawn>();

			if (!spawn)
			{
				Logger.Error("Couldn't find player spawn object in the level!");
				LE_CustomErrorPopups.NoPlayerSpawnObjectDetected();
				return;
			}

			Controls.Instance.transform.position = spawn.transform.position + Vector3.up;
			Controls.Instance.gameCamera.transform.localPosition = new Vector3(0f, 0.907f, 0f);
			Controls.Instance.gameCamera.transform.eulerAngles = spawn.transform.eulerAngles;
			Controls.Instance.Angle = new Vector2(spawn.transform.eulerAngles.y, spawn.transform.eulerAngles.x);
			Controls.Instance.transform.localScale = spawn.transform.localScale;
		}

		public GameObject PlaceObject(LE_Object.ObjectType? objectType, Vector3 position, Vector3 eulerAngles, Vector3 scale, bool setAsSelected = true)
		{
			if (objectType == null)
			{
				Logger.Error("objectType is null. Skipping object placement...");
				return null;
			}

			GameObject template = allCategoriesObjects[objectType.Value];
			GameObject obj = Instantiate(template, levelObjectsParent.transform);

			obj.transform.localPosition = position;
			obj.transform.localEulerAngles = eulerAngles;
			obj.transform.localScale = scale;

			LE_Object addedComp = LE_Object.AddComponentToObject(obj, objectType.Value);

			if (objectType == LE_Object.ObjectType.SCREEN)
			{
				screensOnTheLevel.Add((LE_Screen)addedComp);
			}
			else if (objectType == LE_Object.ObjectType.SMALL_SCREEN)
			{
				smallScreensOnTheLevel.Add((LE_Small_Screen)addedComp);
			}

			if (addedComp == null)
			{
				Destroy(obj);
				return null;
			}

			obj.SetActive(true);

			return obj;
		}

		void ConfigureGlobalProperties()
		{
			if (!(bool)GetGlobalProperty("HasTaser"))
			{
				Controls.Instance.DeactivateWeapon();
			}
			bool hasJetpackGlobal = (bool)GetGlobalProperty("HasJetpack");
			Controls.Instance.hasJetPack = hasJetpackGlobal;

			SetupLevelSkybox((int)GetGlobalProperty("Skybox"));

			ApplyUpgrades((List<UpgradeSaveData>)GetGlobalProperty("Upgrades"), hasJetpackGlobal);
		}
		object GetGlobalProperty(string name)
		{
			if (globalProperties.ContainsKey(name))
			{
				return globalProperties[name];
			}

			return null;
		}
		// --------------------------------------------------
		void SetupLevelSkybox(int skyboxID)
		{
			string skyboxMatName = $"Skybox_CH{skyboxID + 1}";
			Material skyboxMat = LEBundle.Load<Material>(skyboxMatName);

			// Apply the same shader logic as the editor
			if (Regex.Match(skyboxMatName, @"(?:9|10|11|12|13)$").Success)
			{
				skyboxMat.shader = Shader.Find("Skybox/6 Sided");
			}
			else
			{
				skyboxMat.shader = Shader.Find("Skybox/6 Sided 3 Axis Rotation");
			}
			
			RenderSettings.skybox = skyboxMat;
		}

		void ResetAllUpgradeEffects(bool allowJetpack)
		{
			// Force all upgrade-driven effects OFF/0 before applying data
			Controls.m_hasDodgeSkill = false;
			Controls.m_currentDodgeLevel = 0;

			Controls.m_hasSprintSkill = false;

			if (TimeManipulator.Instance)
				TimeManipulator.Instance.SetInPlayerPosession(false);

			if (!allowJetpack || allowJetpack)
				Controls.m_currentJetpackUpgradeLevel = 0; // level 0 by default regardless; allowJetpack only gates enabling later

			Controls.m_currentHealthUpgradeLevel = 0;
			Controls.m_currentSpeedUpgradeLevel = 0;
			Controls.m_currentTaserCapacityUpgradeLevel = 0;

			Controls.m_currentHealthBackpackLevel = 0;
			Controls.m_currentTaserBackpackLevel = 0;
			Controls.m_currentTaserPowerUpgradeLevel = 0;
			Controls.m_currentStealthUpgradeLevel = 0;
			Controls.m_currentAimStabilizerLevel = 0;
			Controls.m_currentHoverUpgradeLevel = 0;
			Controls.m_currentScopeLevel = 0;
			Controls.m_currentSafeLandingLevel = 0;
			Controls.m_currentUVFlashlightLevel = 0;
			Controls.m_currentScannerLevel = 0;
			Controls.DisableInfraredFlashlight();
		}

		void ApplyUpgrades(List<UpgradeSaveData> upgrades, bool allowJetpack = true)
		{
			// Always reset all effects first. Missing entries remain disabled.
			ResetAllUpgradeEffects(allowJetpack);

			// If no upgrades provided by level data, leave everything disabled
			if (upgrades == null)
			{
				UpgradePatches.Init();
				// Set StatsManager.totalUpgradesCount to 0 when no upgrades
				StatsManager.totalUpgradesCount = 0;
				return;
			}

			foreach (var up in upgrades)
			{
				int max = LevelData.GetUpgradeMaxLevel(up.type);
				if (up.level > max) up.level = max;

				if (!allowJetpack && up.type == UpgradeType.JETPACK)
				{
					up.active = false;
					up.level = 0;
				}

				if (up.type != UpgradeType.JETPACK)
				{
					// For non-jetpack, only considered enabled if level > 0 and active flag is true
					up.active = up.active && up.level > 0;
				}
				else
				{
					// For jetpack, treat as enabled only if allowed and level > 0 and active flag
					up.active = allowJetpack && up.active && up.level > 0;
				}
			}

			// Apply only the upgrades present/enabled in the list
			foreach (var upgrade in upgrades)
			{
				if (!upgrade.IsEnabled) continue;
				switch (upgrade.type)
				{
					case UpgradeType.DODGE:
						Controls.m_hasDodgeSkill = upgrade.active;
						Controls.m_currentDodgeLevel = upgrade.level;
						break;
					case UpgradeType.SPRINT:
						Controls.m_hasSprintSkill = upgrade.active;
						break;
					case UpgradeType.HYPER_SPEED:
						TimeManipulator.Instance.SetInPlayerPosession(upgrade.active);
						break;
					case UpgradeType.JETPACK:
						if (allowJetpack)
							Controls.m_currentJetpackUpgradeLevel = upgrade.level;
						break;
					case UpgradeType.HEALTH:
						Controls.m_currentHealthUpgradeLevel = upgrade.level;
						Controls.Instance.currentHP = Controls.Instance.currentMaxHP; // Heal to full on health upgrade application
						break;
					case UpgradeType.SPEED:
						Controls.m_currentSpeedUpgradeLevel = upgrade.level;
						break;
					case UpgradeType.TASER_CAPACITY:
						Controls.m_currentTaserCapacityUpgradeLevel = upgrade.level;
						break;
					case UpgradeType.HEALTH_BACKPACK:
						Controls.m_currentHealthBackpackLevel = upgrade.level;
						break;
					case UpgradeType.TASER_BACKPACK:
						Controls.m_currentTaserBackpackLevel = upgrade.level;
						break;
					case UpgradeType.TASER_POWER:
						Controls.m_currentTaserPowerUpgradeLevel = upgrade.level;
						break;
					case UpgradeType.STEALTH:
						Controls.m_currentStealthUpgradeLevel = upgrade.level;
						break;
					case UpgradeType.AIM_STABILIZER:
						Controls.m_currentAimStabilizerLevel = upgrade.level;
						break;
					case UpgradeType.HOVER:
						Controls.m_currentHoverUpgradeLevel = upgrade.level;
						break;
					case UpgradeType.SCOPE:
						Controls.m_currentScopeLevel = upgrade.level;
						break;
					case UpgradeType.SAFE_LANDING:
						Controls.m_currentSafeLandingLevel = upgrade.level;
						break;
					case UpgradeType.UV_FLASHLIGHT:
						Controls.m_currentUVFlashlightLevel = upgrade.level;
						if (upgrade.level > 0 && upgrade.active)
							Controls.EnableInfraredFlashlight();
						break;
					case UpgradeType.SCANNER:
						Controls.m_currentScannerLevel = upgrade.level;
						break;
				}
				if (upgrade.IsEnabled)
				{
					totalUpgradeCount += upgrade.level;
					Debug.Log(totalUpgradeCount);	
				}
			}
			StatsManager.totalUpgradesCount = totalUpgradeCount;
			if (totalUpgradeCount <= 0)
			{
                StatsManager.totalUpgradesCount = 0; // Ensure it's exactly 0 if no upgrades
            }


            //For now, let's ignore that bitch
            UpgradePatches.Init();
		}

		// Other stuff...
		public void PatchPauseCurrentLevelNameInResumeButton()
		{
			MelonCoroutines.Start(Coroutine());
			IEnumerator Coroutine()
			{
				yield return new WaitForSecondsRealtime(0.025f);
				MenuController.GetInstance().levelToResumeLabel.text = "Custom Level : " + levelName;
			}
		}
		public void InvertPlayerGravity()
		{
			Controls.Instance.InverseGravity();

			foreach (var screen in screensOnTheLevel)
			{
				if (!screen.GetProperty<bool>("InvertWithGravity")) continue;

				screen.TriggerAction("InvertText");
			}
			foreach (var screen in smallScreensOnTheLevel)
			{
				if (!screen.GetProperty<bool>("InvertWithGravity")) continue;

				screen.TriggerAction("InvertText");
			}
		}
		IEnumerator SetupEnvCam()
		{
			Transform envCam = null;
			while (envCam == null)
			{
				envCam = GameObject.Find("EnvCam").transform;
				yield return null;
			}

			// Now EnvCam exists, configure it
			var camera = envCam.GetComponent<Camera>();
			camera.useOcclusionCulling = false;
			camera.farClipPlane = 200f;
			// Do not overwrite upgrade values here; they are applied from the editor data in ApplyUpgrades.
			// Refresh taser modules only if present to reflect applied upgrades.
			if (Controls.Instance.HasTaser())
				Controls.Instance.gunController.RefreshTaserModules();
		}

		void OnDestroy()
		{
			// When the script obj is destroyed, that means the scene has changed, destroy the back to LE button, since it'll be created again when entering...
			// again...
			Destroy(backToLEButton);

            if (levelObjectsParent != null)
            {
                Destroy(levelObjectsParent);
            }

            LE_Object.ResetStaticVariablesInObjects();

			PlaymodePauseMenuPatcher.DestroyPatcher();
			UpgradePatches.Unpatch();
			CleanupAllObjectives();

            Destroy(editorObjectsRootFromBundle);
        }

		// Objectives management methods
		public void CleanupAllObjectives()
		{
			// First destroy all tracked objective GameObjects
			foreach (var kvp in activeObjectives)
			{
				if (kvp.Value != null && kvp.Value.gameObject != null)
				{
					Destroy(kvp.Value.gameObject);
				}
			}
			activeObjectives.Clear();
			
			// Then cleanup any remaining UI elements (should already be cleaned by ObjectiveController.Cancel/Accomplish)
			if (InGameUIManager.Instance != null)
			{
				InGameUIManager.Instance.DestroyAllObjectives();
				InGameUIManager.Instance.DestroyAllObjectiveMarkers();
			}
			lastObj = null;
		}

		public void CreateObjective(string objectiveName)
		{

			if (activeObjectives.TryGetValue(objectiveName, out var existingController))
			{
				return;
			}

			// Create a new GameObject with ObjectiveController
			GameObject objectiveObj = new GameObject("Obj_" + objectiveName);
			objectiveObj.tag = "Objective";
			objectiveObj.layer = LayerMask.NameToLayer("Ignore Raycast");
            ObjectiveController objectiveController = objectiveObj.AddComponent<ObjectiveController>();

            objectiveController.hasMarker = false;
			objectiveController.markerDelay = 0;
			objectiveController.markerObj = null;
			objectiveController.onActivated = new UnityEngine.Events.UnityEvent();
			objectiveController.onAccomplished = new UnityEngine.Events.UnityEvent();
			objectiveController.BlocSwitchs = new GameObject[0];
			objectiveController.dialogToActivate = false;
			objectiveController.dialogTimeStart = 0;
			objectiveController.objectiveDelay = 0;
			objectiveController.currentKine = null;
			objectiveController.onMarkerDisplayed = new UnityEngine.Events.UnityEvent();
			objectiveController.useActivationConditions = false;
			objectiveController.doorsToBeOpen = new Il2CppSystem.Collections.Generic.List<PorteScript>(0);
			objectiveController.killPlanesToBeDisabled = new Il2CppSystem.Collections.Generic.List<KillPlaneController>(0);
            objectiveController.objective = objectiveName;
			objectiveController.Activate();
			objectiveController.currentlyActive = true;

			// Track this objective
			activeObjectives[objectiveName] = objectiveController;
			
        }

		public bool AccomplishObjective(string objectiveName)
		{
			if (activeObjectives.TryGetValue(objectiveName, out var controller))
			{
				controller.Accomplish();
				return true;
			}
			
			return false;
		}

		public bool FailObjective(string objectiveName)
		{
			if (activeObjectives.TryGetValue(objectiveName, out var controller))
			{
				controller.Cancel();
				return true;
			}
			
			return false;
		}
        public bool DoesObjectiveExist(string objectiveName)
        {
            // Check if the objective exists in your objectives list/dictionary
            // Return true if it exists, false otherwise
            // This depends on how you're tracking objectives in your PlayModeController
            return activeObjectives.ContainsKey(objectiveName); // Adjust this based on your actual implementation
        }
    }
}