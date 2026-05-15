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
using FS_LevelEditor.UI_Related;

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
		public Dictionary<LE_Object.ObjectType, GameObject> allCategoriesObjects = new();
		List<Dictionary<LE_Object.ObjectType, GameObject>> allCategoriesObjectsSorted = new();
		GameObject[] otherObjectsFromBundle;
		public GameObject levelObjectsParent;
		List<AudioClip> tracks = new();

		public Dictionary<string, object> globalProperties = LevelData.GetDefaultGlobalProperties();

		GameObject backToLEButton;

		public List<LE_Object> currentInstantiatedObjects = new List<LE_Object>();
		public int deathsInCurrentLevel = 0;
		public List<LE_Screen> screensOnTheLevel = new List<LE_Screen>();
		public List<LE_Small_Screen> smallScreensOnTheLevel = new List<LE_Small_Screen>();

		public bool endTriggerReached = false;

		// Objectives management
		public Dictionary<string, ObjectiveController> activeObjectives = new Dictionary<string, ObjectiveController>();
		private string lastObj = null;

		void Awake()
		{
			Instance = this;

			LE_Object.ResetStaticVariablesInObjects();

			// Reset AND logic state for new playmode session
			AndLogicManager.Reset();

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

			#region Setup OST
			string[] trackNames = new[]
			{
				"Level1",
				"Level2_old",
				"Level2",
				"Level3",
				"Level4",
				"Level5_Calm_Loop",
				"Fractaloween_Soundtrack",
				"Fractalentine_Soundtrack",
				"White Trees",
				"SR3d"
			};
			foreach (var trackName in trackNames)
			{
				AudioClip track = LEBundle.Load<AudioClip>(trackName);
				if (track != null)
				{
					track.hideFlags = HideFlags.DontUnloadUnusedAsset;
					tracks.Add(track);
				}
			}
			#endregion
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
			// Don't unload the bundle - FMOD needs access to FSB data for audio clips
			// The bundle will be unloaded in OnDestroy
		}

		void Start()
		{
			TeleportPlayer();
			ConfigureGlobalProperties();
			MelonCoroutines.Start(SetupEnvCam());

			Utils.Invoke(() => ParticlesPatch.GetObjectsWithParticlesReferences(), 0.1f); // Delay the invoke, so objects are initialized correctly first.
			SetSpeedrunTimerFont();
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
			if(!(bool)GetGlobalProperty("HasFlashlight"))
			{
				Controls.Instance.SetFlashlightNotAllowed();
			}
			bool hasJetpackGlobal = (bool)GetGlobalProperty("HasJetpack");
			Controls.Instance.hasJetPack = hasJetpackGlobal;
			Patches.DebudModePatch.DebugAllowed = (bool)GetGlobalProperty("DebugAllowed");

            SetupLevelSkybox((int)GetGlobalProperty("Skybox"));
			SetupLevelMusic((int)GetGlobalProperty("Music"));

			PlaymodeUpgrades.ApplyUpgrades((List<UpgradeSaveData>)GetGlobalProperty("Upgrades"));
		}
		public object GetGlobalProperty(string name)
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
		void SetupLevelMusic(int musicID)
		{
			if (musicID >= 0 && musicID < tracks.Count)
			{
				MusicManager.Instance.SetCurrentLevelNormalMusic(tracks[musicID]);
				MusicManager.Instance.PauseMenuMusic();
				MusicManager.Instance.m_context = MusicManager.MusicContext.NORMAL;
			}
		}

		// Other stuff...
		public void PatchPauseCurrentLevelNameInResumeButton()
		{
			MelonCoroutines.Start(Coroutine());
			IEnumerator Coroutine()
			{
				yield return new WaitForSecondsRealtime(0.025f);
				MenuController.GetInstance().levelToResumeLabel.font = NGUI_Utils.notoSansFont; // Support special chars.
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

			// Unload the asset bundle when playmode is destroyed
			if (LEBundle != null && !Core.isQuitting)
			{
				try
				{
					LEBundle.Unload(true);
				}
				catch (System.Runtime.InteropServices.SEHException)
				{
					// Asset bundle may have already been garbage collected by IL2CPP runtime
				}
				LEBundle = null;
			}

			Instance = null;

			editorObjectsRootFromBundle = null;
			categories.Clear();
			categories = null;
			allCategoriesObjects.Clear();
			allCategoriesObjects = null;
			allCategoriesObjectsSorted.Clear();
			allCategoriesObjectsSorted = null;
			otherObjectsFromBundle = null;
			levelObjectsParent = null;
			tracks.Clear();
			tracks = null;
			globalProperties.Clear();
			globalProperties = null;
			currentInstantiatedObjects.Clear();
			currentInstantiatedObjects = null;
			screensOnTheLevel.Clear();
			screensOnTheLevel = null;
			smallScreensOnTheLevel.Clear();
			smallScreensOnTheLevel = null;
			activeObjectives.Clear();
			activeObjectives = null;
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

		void SetSpeedrunTimerFont()
		{
			// Special characters support.
			GameObject.Find("(singleton) InGameUIManager/Camera/Panel/Timers/Holder/TimersHolder/Background1/SpeedrunTimers_Holder/CurrentLevelTimerTitle").GetComponent<UILabel>().font =
				NGUI_Utils.notoSansFont;
		}
    }
}