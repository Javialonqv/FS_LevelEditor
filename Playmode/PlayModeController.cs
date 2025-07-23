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

        GameObject backToLEButton;

        public Dictionary<string, object> globalProperties = LevelData.GetDefaultGlobalProperties();
        public List<LE_Object> currentInstantiatedObjects = new List<LE_Object>();
        public int deathsInCurrentLevel = 0;
        public List<LE_Screen> screensOnTheLevel = new List<LE_Screen>();
        public List<LE_Small_Screen> smallScreensOnTheLevel = new List<LE_Small_Screen>();

        public bool endTriggerReached = false;

        void Awake()
        {
            Instance = this;

            LoadAssetBundle();

            deathsInCurrentLevel = Melon<Core>.Instance.totalDeathsInCurrentPlaymodeSession;

            Invoke("DisableTheCurrentScene", 0.2f);

            levelObjectsParent = new GameObject("LevelObjects");
            levelObjectsParent.transform.position = Vector3.zero;

            CreateBackToLEButton();

            PlaymodePauseMenuPatcher.Create();
        }

        void Start()
        {
            TeleportPlayer();
            ConfigureGlobalProperties();

            UnloadBundle();
        }

        // When the script obj is destroyed, that means the scene has changed, destroy the back to LE button, since it'll be created again when entering...
        // again...
        void OnDestroy()
        {
            Destroy(backToLEButton);

            LE_Object.ResetStaticVariablesInObjects();

            PlaymodePauseMenuPatcher.DestroyPatcher();
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

                obj.SetActive(false);
            }
        }

        void ConfigureGlobalProperties()
        {
            if (!(bool)GetGlobalProperty("HasTaser"))
            {
                Controls.Instance.DeactivateWeapon();
            }
            Controls.Instance.hasJetPack = (bool)GetGlobalProperty("HasJetpack");

            SetupLevelSkybox((int)GetGlobalProperty("Skybox"));

            ApplyUpgrades((List<UpgradeSaveData>)GetGlobalProperty("Upgrades"));
        }
        object GetGlobalProperty(string name)
        {
            if (globalProperties.ContainsKey(name))
            {
                return globalProperties[name];
            }

            return null;
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

        void ApplyUpgrades(List<UpgradeSaveData> upgrades)
        {
            foreach (var upgrade in upgrades)
            {
                switch (upgrade.type)
                {
                    case UpgradeType.DODGE:
                        Controls.m_hasDodgeSkill = upgrade.active;
                        Controls.m_currentDodgeLevel = upgrade.level;
                        break;
                }
            }
        }

        void CreateBackToLEButton()
        {
            GameObject template = GameObject.Find("MainMenu/Camera/Holder/Main/LargeButtons/2_Chapters");
            backToLEButton = Instantiate(template, template.transform.parent);
            backToLEButton.name = "4_BackToLE";
            Destroy(backToLEButton.GetComponent<ButtonController>());
            Destroy(backToLEButton.GetChildWithName("Label").GetComponent<UILocalize>());
            backToLEButton.GetChildWithName("Label").GetComponent<UILabel>().text = "Back to Level Editor";

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
            return otherObjectsFromBundle.FirstOrDefault(obj => obj.name == objectName);
        }

        void SetupLevelSkybox(int skyboxID)
        {
            string skyboxMatName = $"Skybox_CH{skyboxID + 1}";
            Material skyboxMat = LEBundle.Load<Material>(skyboxMatName);

            skyboxMat.shader = Shader.Find("Skybox/6 Sided 3 Axis Rotation");
            RenderSettings.skybox = skyboxMat;
        }

        public void UnloadBundle()
        {
            LEBundle.Unload(false);
        }

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
    }
}