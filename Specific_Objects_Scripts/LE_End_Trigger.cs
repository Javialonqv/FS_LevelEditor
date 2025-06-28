using FS_LevelEditor;
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
    public class LE_End_Trigger : LE_Object
    {
        // Since there's only ONE end trigger per level, don't even put the id on it.
        public override string objectFullNameWithID
        {
            get { return objectOriginalName; }
        }

        static int currentInstances = 0;
        const int maxInstances = 1;

        void Awake()
        {
            currentInstances++;

            gameObject.GetChildAt("Content/End").tag = "Checkpoint";

            Logger.DebugError("END TRIGGER IS EXTREMELY EXPERIMENTAL, I JUST ADDED IT, DON'T COMPLAIN ABOUT IT SHRISS!!!");
        }

        public override void OnInstantiated(LEScene scene)
        {
            if (scene == LEScene.Playmode)
            {
                gameObject.GetChildAt("Content/Mesh").SetActive(false);
            }

            base.OnInstantiated(scene);
        }

        public override void InitComponent()
        {
            GameObject endTrigger = gameObject.GetChildAt("Content/End");

            CheckpointController checkpoint = endTrigger.AddComponent<CheckpointController>();

            initialized = true;
        }

        public static new Color GetObjectColor(LEObjectContext context)
        {
            return new Color(1f, 1f, 0.07843138f);
        }

        void OnDestroy()
        {
            currentInstances--;
        }
    }
}

[HarmonyLib.HarmonyPatch(typeof(Controls), nameof(Controls.OnCheckpointPassed))]
public static class EndCheckpointReachedPatch
{
    public static void Postfix(string _checkpointName, GameObject _objectCollided)
    {
        if (PlayModeController.Instance && _checkpointName == "End")
        {
            _objectCollided.SetActive(false);
            PlayModeController.Instance.endTriggerReached = true;
        }
    }
}