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
		private static UpgradeSaveData SafeFind(List<UpgradeSaveData> list, UpgradeType t)
		{
			var u = list.FirstOrDefault(x => x.type == t);
			if (u == null)
				return new UpgradeSaveData { type = t, active = false, level = 0 };
			return u;
		}
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

		static readonly Dictionary<string, UpgradeType> upgradeIntKeys = new Dictionary<string, UpgradeType>()
		{
            ["Dodge_Upgrade_Level"] =			UpgradeType.DODGE,
            ["Jetpack_Upgrade_Level"] =			UpgradeType.JETPACK,
            ["Health_Upgrade_Level"] =			UpgradeType.HEALTH,
            ["Speed_Upgrade_Level"] =			UpgradeType.SPEED,
            ["Taser_Capacity_Upgrade_Level"] =	UpgradeType.TASER_CAPACITY,
            ["Health_Backpack_Upgrade_Level"] = UpgradeType.HEALTH_BACKPACK,
            ["Taser_Backpack_Upgrade_Level"] =	UpgradeType.TASER_BACKPACK,
            ["Taser_Power_Upgrade_Level"] =		UpgradeType.TASER_POWER,
            ["Stealth_Upgrade_Level"] =			UpgradeType.STEALTH,
            ["Aim_Stabilizer_Upgrade_Level"] =	UpgradeType.AIM_STABILIZER,
            ["Hover_Upgrade_Level"] =			UpgradeType.HOVER,
            ["Scope_Upgrade_Level"] =			UpgradeType.SCOPE,
            ["Safe_Landing_Upgrade_Level"] =	UpgradeType.SAFE_LANDING,
            ["UV_Flashlight_Upgrade_Level"] =	UpgradeType.UV_FLASHLIGHT,
            ["Scanner_Upgrade_Level"] =			UpgradeType.SCANNER
        };
        static readonly Dictionary<string, UpgradeType> upgradeBoolKeys = new()
        {
            ["Has_Dodge"] =		UpgradeType.DODGE,
            ["Has_Sprint"] =	UpgradeType.SPRINT,
            ["Has_HS"] =		UpgradeType.HYPER_SPEED,
            ["Has_Jetpack"] =	UpgradeType.JETPACK
			// This doesn't include the taser keys since that's an special case.
        };

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
			if (!upgradeIntKeys.TryGetValue(_key, out UpgradeType type))
				return true;

			var upgrades = PlaymodeUpgrades.targetUpgradesData;
			int maxLevel = LevelData.GetUpgradeMaxLevel(type);
			int level = SafeFind(upgrades, type).level;

			__result = Math.Clamp(level, 0, maxLevel);

			return false;
		}
		static bool GetBoolPatches(ref bool __result, string _key)
		{
			var upgrades = PlaymodeUpgrades.targetUpgradesData;

			if (upgradeBoolKeys.TryGetValue(_key, out UpgradeType type))
			{
				__result = SafeFind(upgrades, type).active;
				return false;
			}

			switch (_key)
			{
				// Ensure taser availability follows level save (default true) instead of base game save
				case "Has_Taser":
				case "Has_Tazer":
				case "Has_Gun":
					if (PlayModeController.Instance != null && PlayModeController.Instance.globalProperties != null)
					{
						if (PlayModeController.Instance.GetGlobalProperty("HasTaser") is bool hasTaser)
						{
                            __result = hasTaser;
                            return false;
                        }
					}
					// Fallback to true if something goes wrong, since default is true
					__result = true;
					return false;
			}

			return true;
		}
	}

	[HarmonyPatch(typeof(Controls), nameof(Controls.HasAtLeastOneUpgrade))]
	public static class HasOneUpgrade
	{
		public static bool Prefix( bool __result)
		{
			if(PlayModeController.Instance)
			{
				__result = PlaymodeUpgrades.totalUpgradeCount > 0;
				return false;
			}
			return true;
		}
	}
}
