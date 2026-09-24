using FS_LevelEditor;
using FS_LevelEditor.Playmode;
using System.Collections;
using UnityEngine;
using static FS_LevelEditor.LE_Death_Trigger;

namespace FS_LevelEditor
{

    public class LE_End_Trigger : LE_Object
    {
        public bool titleCard = false;
        public string titleCardText = "Merry Christmas\nand Happy New Year!";
        void Awake()
        {
            gameObject.GetChildAt("Content/End").tag = "Checkpoint";
        }

        public static Dictionary<string, object> GetDefaultProperties()
        {
            return new Dictionary<string, object>()
            {
                { "TitleCard", false },
                { "TitleCardText", "Merry Christmas\nand Happy New Year!" }
            };
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
            endTrigger.layer = LayerMask.NameToLayer("Ignore Raycast");

            CheckpointController checkpoint = endTrigger.AddComponent<CheckpointController>();

            titleCard = GetProperty<bool>("TitleCard");
            titleCardText = GetProperty<string>("TitleCardText");

            initialized = true;
        }

        public static new Color GetDefaultObjectColor(LEObjectContext context)
        {
            return new Color(0f, 1f, 1f, 0.05f);
        }
        public override bool SetProperty(string name, object value)
        {
            if (name == "TitleCard")
            {
                if (value is bool boolValue)
                {
                    properties["TitleCard"] = boolValue;
                    return true;
                }
            }
            else if (name == "TitleCardText")
            {
                if (value is string stringValue)
                {
                    properties["TitleCardText"] = stringValue;
                    return true;
                }
            }
            return base.SetProperty(name, value);
        }
        void OnDestroy()
        {
            EndCheckpointReachedPatch.CancelEndSequence();
        }
    }
}

[HarmonyLib.HarmonyPatch(typeof(Controls), nameof(Controls.OnCheckpointPassed))]
public static class EndCheckpointReachedPatch
{
    public static Coroutine endLevelCoroutine;

    public static bool Prefix(Controls __instance, string _checkpointName, GameObject _objectCollided)
    {
        if (PlayModeController.Instance && _checkpointName == "End" && _objectCollided != null)
        {
            LE_End_Trigger endTrigger = _objectCollided.GetComponentInParent<LE_End_Trigger>();

            if (endTrigger != null)
            {
                bool showTitleCard = endTrigger.titleCard;
                string text = endTrigger.titleCardText;

                _objectCollided.SetActive(false);

                __instance.CancelAnyJetpack();
                __instance.CancelAnyDodge();
                __instance.CancelAnySprintRequests();
                __instance.CancelAnyZeroGMovements();

                if (endLevelCoroutine != null && NativeModLoader.Instance != null)
                {
                    NativeModLoader.Instance.StopCoroutine(endLevelCoroutine);
                }

                if (showTitleCard)
                {
                    UILocalize.Destroy(InGameUIManager.Instance.eventWinPage.GetChildAt("TitleText").GetComponent<UILocalize>());
                    InGameUIManager.Instance.eventWinPage.GetChildAt("TitleText").GetComponent<UILabel>().text = text;
                    InGameUIManager.Instance.eventWinPage.GetChildAt("TitleText").GetComponent<UILabel>().ProcessText();
                    InGameUIManager.Instance.WinEvent();

                    endLevelCoroutine = NativeModLoader.Instance.StartCoroutine(LeaveTheLevel(5));
                }
                else
                {
                    endLevelCoroutine = NativeModLoader.Instance.StartCoroutine(LeaveTheLevel(0));
                }

                return false;
            }
        }

        return true;
    }
    public static void CancelEndSequence()
    {
        if (endLevelCoroutine != null)
        {
            NativeModLoader.Instance.StopCoroutine(endLevelCoroutine);
        }
        endLevelCoroutine = null;

        if (InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.HideWinEvent();
        }
    }
    static IEnumerator LeaveTheLevel(int delay)
    {
        yield return new WaitForSeconds(delay);
        endLevelCoroutine = null;
        PlayModeController.Instance.endTriggerReached = true;
        LE_MenuUIManager.Instance.GoBackToLEWhileInPlayMode(
            PlayModeController.Instance.levelFileNameWithoutExtension,
            PlayModeController.Instance.levelName
        );
        InGameUIManager.Instance.HideWinEvent();
    }
}

[HarmonyLib.HarmonyPatch(typeof(Controls), nameof(Controls.KillCharacter), new Type[] {typeof(bool), typeof(bool)})]
public static class TitleCardKillFix
{
    public static void Postfix()
    {
        EndCheckpointReachedPatch.CancelEndSequence();
    }
}