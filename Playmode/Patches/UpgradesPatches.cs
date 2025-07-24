using FS_LevelEditor.SaveSystem;
using Il2Cpp;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using HarmonyLib;

namespace FS_LevelEditor.Playmode.Patches
{
    public static class UpgradePatches
    {
        public static MethodInfo getIntMethod
        {
            get
            {
                return typeof(FractalSave).GetMethod(nameof(FractalSave.GetInt));
            }
        }
        public static MethodInfo getIntMethodPrefix
        {
            get
            {
                return typeof(UpgradePatches).GetMethod(nameof(GetIntPatches), BindingFlags.NonPublic | BindingFlags.Static);
            }
        }

        public static MethodInfo getBoolMethod
        {
            get
            {
                return typeof(FractalSave).GetMethod(nameof(FractalSave.GetBool));
            }
        }
        public static MethodInfo getBoolMethodPrefix
        {
            get
            {
                return typeof(UpgradePatches).GetMethod(nameof(GetBoolPatches), BindingFlags.NonPublic | BindingFlags.Static);
            }
        }

        public static void Init()
        {
            HarmonyLib.Harmony harmony = Melon<Core>.Instance.HarmonyInstance;

            harmony.Patch(getIntMethod, new HarmonyMethod(getIntMethodPrefix), null, null);
            harmony.Patch(getBoolMethod, new HarmonyMethod(getBoolMethodPrefix), null, null);
        }
        public static void Unpatch()
        {
            HarmonyLib.Harmony harmony = Melon<Core>.Instance.HarmonyInstance;

            harmony.Unpatch(getIntMethod, HarmonyPatchType.All);
            harmony.Unpatch(getBoolMethod, HarmonyPatchType.All);
        }

        static bool GetIntPatches(ref int __result, string _key)
        {
            var upgrades = (List<UpgradeSaveData>)PlayModeController.Instance.globalProperties["Upgrades"];

            switch (_key)
            {
                case "Dodge_Upgrade_Level":
                    __result = upgrades.Find(x => x.type == UpgradeType.DODGE).level;
                    return false;
            }

            return true;
        }
        static bool GetBoolPatches(ref bool __result, string _key)
        {
            var upgrades = (List<UpgradeSaveData>)PlayModeController.Instance.globalProperties["Upgrades"];
            switch (_key)
            {
                case "Has_Dodge":
                    __result = upgrades.Find(x => x.type == UpgradeType.DODGE).active;
                    return false;
                case "Has_Sprint":
                    __result = upgrades.Find(x => x.type == UpgradeType.SPRINT).active;
                    return false;
                case "Has_HS":
                    __result = upgrades.Find(x => x.type == UpgradeType.HYPER_SPEED).active;
                    return false;
            }

            return true;
        }
    }
}
