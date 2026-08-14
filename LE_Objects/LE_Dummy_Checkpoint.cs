using FS_LevelEditor.Playmode;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Dummy_Checkpoint : LE_Object
    {
        public override string[] EventsIDs =>
        [
            "OnSave",
            "OnRespawn"
        ];

        public static bool AtLeastOneCheckpointReached = false;
        public static LE_Dummy_Checkpoint LastReachedCheckpoint = null;
        static int CheckpointsCount = 0;

        static bool HasGun;
        static bool HasJetpack;

        public override void InitComponent()
        {
            GameObject checkpointObj = contentObject.GetChild("Checkpoint");

            GameObject spawnObj = new GameObject("Spawn");
            spawnObj.transform.parent = checkpointObj.transform;
            spawnObj.transform.localPosition = Vector3.zero;
            spawnObj.transform.localScale = Vector3.one;

            CheckpointController checkpoint = checkpointObj.AddComponent<CheckpointController>();
            checkpoint.useSpawnPoint = true;
            checkpoint.onSave = new UnityEngine.Events.UnityEvent();
            checkpoint.onSave.AddListener((UnityAction)ExecuteOnSaveEvents);
            // onSpawn events do not work when respawning, I guess it's only for when the player continues playing the chapter from the menu?
            // My own respawn event implementation is in the RespawnEventOnQuickLoad patch.
            checkpoint.objectsToEnable = new GameObject[0];
            checkpoint.allowRespawnAnim = GetProperty<bool>("RespawnAnim");
            checkpoint.allowRespawnDialogs = false;

            checkpointObj.tag = "Checkpoint";
            // FS uses the trigger name as an identifier, which then is saved into the "Last_Checkpoint" key in the save file.
            checkpointObj.name = "Checkpoint" + CheckpointsCount++;

            base.InitComponent();
        }

        public override void OnInstantiated(LEScene scene)
        {
            if (scene == LEScene.Playmode)
            {
                gameObject.GetChildAt("Content/Mesh").SetActive(false);
            }

            base.OnInstantiated(scene);
        }

        public static Dictionary<string, object> GetDefaultProperties()
        {
            return new Dictionary<string, object>
            {
                { "RespawnAnim", false },
                { "OnSave", new List<LE_Event>() },
                { "OnRespawn", new List<LE_Event>() }
            };
        }

        public override bool SetProperty(string name, object value)
        {
            if (name == "RespawnAnim")
            {
                if (value is bool bValue)
                {
                    properties["RespawnAnim"] = bValue;
                    return true;
                }
            }
            else if (GetAvailableEventsIDs().Contains(name))
            {
                if (value is List<LE_Event>)
                {
                    properties[name] = (List<LE_Event>)value;
                    return true;
                }
            }

            return base.SetProperty(name, value);
        }

        void ExecuteOnSaveEvents()
        {
            AtLeastOneCheckpointReached = true;
            LastReachedCheckpoint = this;

            UpdateHasGunAndJetpackValues();

            eventExecuter.ExecuteEventsWithAndLogic((List<LE_Event>)properties["OnSave"], "OnSave", true);
        }
        public void ExecuteOnRespawnEvents()
        {
            // The game performs safety-checks to ensure that the player has taser/jetpack in Ch4, force it to have our saved values.
            if (HasGun)
                Controls.Instance.ActivateWeapon(false);
            else
                Controls.Instance.DeactivateWeapon();

            // We could use Controls.Instance.ActivateJetpack/DeactivateJetpack instead, but whatever.
            Controls.Instance.hasJetPack = HasJetpack;
            Controls.Instance.jetPackObject.SetActive(HasJetpack);

            eventExecuter.ExecuteEventsWithAndLogic((List<LE_Event>)properties["OnRespawn"], "OnRespawn", true);
        }

        public static void UpdateHasGunAndJetpackValues()
        {
            // NOTE: They're static, it's intended that the player keeps the taser/jetpack even if he didn't have it when he reached the checkpoint (as long as he acquired them lol).
            HasGun = Controls.Instance.HasTaser();
            HasJetpack = Controls.Instance.hasJetPack;
        }

        // FS uses SavedObjeTsHolder.AllCheckpoints to find the current checkpoint (because it's not cached for some reason), and then read the variables from it.
        public static void UpdateSavedObjetsHolderCheckpointsWithLevelOnes()
        {
            if (SavedObjetsHolder.GetInstance().AllCheckpoints == null)
                SavedObjetsHolder.GetInstance().AllCheckpoints = new();
            else
                SavedObjetsHolder.GetInstance().AllCheckpoints.Clear();

            var checkpoints = PlayModeController.Instance.levelObjectsParent.GetComponentsInChildren<CheckpointController>(true);
            foreach (var checkpoint in checkpoints)
            {
                SavedObjetsHolder.GetInstance().AllCheckpoints.Add(checkpoint);
            }
        }

        public static new Color GetDefaultObjectColor(LEObjectContext context)
        {
            return new Color(1f, 0f, 1f, 0.07843138f);
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(FractalSave), nameof(FractalSave.HasLastCheckpointQuickSaveForCurrentLevel))]
    public static class FractalSavePatch1
    {
        public static bool Prefix(out bool __result)
        {
            if (PlayModeController.Instance)
            {
                __result = LE_Dummy_Checkpoint.AtLeastOneCheckpointReached;
                return false;
            }

            __result = false; // Doesn't matter what I put here, the og method will get executed anyways and overwrite this value, just so the C# compiler doesn't complain.
            return true;
        }
    }
    [HarmonyLib.HarmonyPatch(typeof(FractalSave), nameof(FractalSave.AutoSaveFileHasLevelInformation))]
    public static class FractalSavePatch2
    {
        public static bool Prefix(out bool __result)
        {
            if (PlayModeController.Instance)
            {
                // Always return true when in playmode, it's just so the "Last Checkpoint" button works, the final decision is in the FractalSavePatch1.
                __result = true;
                return false;
            }

            __result = false; // Doesn't matter what I put here, the og method will get executed anyways and overwrite this value, just so the C# compiler doesn't complain.
            return true;
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(Controls), nameof(Controls.QuickLoadNow))]
    public static class RespawnEventOnQuickLoad
    {
        public static void Postfix(Controls __instance)
        {
            if (PlayModeController.Instance && LE_Dummy_Checkpoint.LastReachedCheckpoint)
            {
                LE_Dummy_Checkpoint.LastReachedCheckpoint.ExecuteOnRespawnEvents();
            }
        }
    }
}
