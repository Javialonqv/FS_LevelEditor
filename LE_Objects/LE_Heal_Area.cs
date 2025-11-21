using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using HarmonyLib;
using MelonLoader;
using System.Collections;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Heal_Area : LE_Object
    {
        HealArea script;
        public Coroutine smallHealPatchCoroutine;

        void Awake()
        {
            properties = new Dictionary<string, object>
            {
                { "HealValue", 3 },
                { "HealInterval", .1f },
                { "MaxHealth", 60 }
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
            GameObject areaObj = gameObject.GetChildAt("Content/Area");
            areaObj.tag = "HealArea";
            areaObj.layer = LayerMask.NameToLayer("Ignore Raycast");

            script = areaObj.AddComponent<HealArea>();
            script.halfStatusObj = new GameObject("ShouldBeSaved");
            script.emptyStatusObj = new GameObject("ShouldBeSaved");
            script.vfx = new GameObject("ShouldBeSaved").AddComponent<ParticleSystem>();
            script.m_light = new GameObject("ShouldBeSaved").AddComponent<Light>();
            script.healValue = GetProperty<int>("HealValue");
            script.healInterval = GetProperty<float>("HealInterval");
            script.maxHealthToGive = GetProperty<int>("MaxHealth");

            initialized = true;
        }

        public override bool SetProperty(string name, object value)
        {
            if (name == "HealInterval")
            {
                if (value is string)
                {
                    if (Utils.TryParseFloat((string)value, out float result))
                    {
                        properties["HealInterval"] = result;
                        return true;
                    }
                }
                else if (value is float)
                {
                    properties["HealInterval"] = (float)value;
                    return true;
                }
            }
            else if (name == "HealValue")
            {
                if (value is string)
                {
                    if (int.TryParse((string)value, out int result))
                    {
                        properties["HealValue"] = result;
                        return true;
                    }
                }
                else if (value is int)
                {
                    properties["HealValue"] = (int)value;
                    return true;
                }
            }
            else if (name == "MaxHealth")
            {
                if (value is string)
                {
                    if (int.TryParse((string)value, out int result))
                    {
                        properties["MaxHealth"] = result;
                        return true;
                    }
                }
                else if (value is int)
                {
                    properties["MaxHealth"] = (int)value;
                    return true;
                }
            }
            return base.SetProperty(name, value);
        }

        // This is a coroutine that will be (hopefully) executed along with the HealRoutine in the original HealArea script.
        public IEnumerator SmallHealPatchRoutine()
        {
            // The Controls class usually uses the HealCharacter function in order to cure the player, and also calls OnHasNormalHealth,
            // which stops the heart beat.
            // However, since that function seems to heal the WHOLE player, HealArea uses another aproach.
            // In older mono versions of FS, it seems that HealArea calls a SmallHeal function inside of Controls, which does NOT call
            // OnHasNormalHealth, therefore, the game never stops the hearth beats, so we need to do the check and call it ourselves.

            while (true)
            {
                if (Controls.Instance.currentHP > Controls.Instance.m_lowHealthThreshold)
                {
                    Controls.Instance.StopHeartBeatSound();
                }
                yield return new WaitForSeconds(script.healInterval);
            }
        }

        public static new Color GetDefaultObjectColor(LEObjectContext context)
        {
            return new Color(0f, 1f, 0.65098039215686274509803921568627f);
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(HealArea), nameof(HealArea.StartHeal))]
    public static class SmallHealPatch
    {
        public static void Postfix(HealArea __instance)
        {
            // Check if it has the LE component of HealArea.
            if (__instance.transform.parent && __instance.transform.parent.parent &&
                __instance.transform.parent.parent.TryGetComponent(out LE_Heal_Area leScript))
            {
                if (leScript.smallHealPatchCoroutine == null)
                {
                    Logger.Log($"A heal area \"{leScript.objectFullNameWithID}\" was reached! Patching small heal bug...");
                    leScript.smallHealPatchCoroutine = (Coroutine)MelonCoroutines.Start(leScript.SmallHealPatchRoutine());
                }
            }
        }
    }
    [HarmonyLib.HarmonyPatch(typeof(HealArea), nameof(HealArea.StopHeal))]
    public static class SmallHealUnPatch
    {
        public static void Postfix(HealArea __instance)
        {
            // Check if it has the LE component of HealArea.
            if (__instance.transform.parent && __instance.transform.parent.parent &&
                __instance.transform.parent.parent.TryGetComponent(out LE_Heal_Area leScript))
            {
                if (leScript.smallHealPatchCoroutine != null)
                {
                    Logger.Log($"A heal area \"{leScript.objectFullNameWithID}\" stopped healing! Undoing small heal patch...");
                    MelonCoroutines.Stop(leScript.smallHealPatchCoroutine);
                    leScript.smallHealPatchCoroutine = null;
                }
            }
        }
    }
}
