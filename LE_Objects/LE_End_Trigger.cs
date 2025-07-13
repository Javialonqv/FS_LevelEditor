using FS_LevelEditor.Playmode;
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
        void Awake()
        {
            gameObject.GetChildAt("Content/End").tag = "Checkpoint";
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

        public static new Color GetDefaultObjectColor(LEObjectContext context)
        {
            return new Color(1f, 0f, 0.07843138f);
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