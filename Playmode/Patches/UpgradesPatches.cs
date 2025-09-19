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
		private const int MaxUpgradeLevel = 3;
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

			int Clamp(UpgradeType t, int v) => Math.Max(0, Math.Min(LevelData.GetUpgradeMaxLevel(t), v));
			switch (_key)
			{
				case "Dodge_Upgrade_Level":
					__result = Clamp(UpgradeType.DODGE, SafeFind(upgrades, UpgradeType.DODGE).level);
					return false;
				case "Jetpack_Upgrade_Level":
					__result = Clamp(UpgradeType.JETPACK, SafeFind(upgrades, UpgradeType.JETPACK).level);
					return false;
				case "Health_Upgrade_Level":
					__result = Clamp(UpgradeType.HEALTH, SafeFind(upgrades, UpgradeType.HEALTH).level);
					return false;
				case "Speed_Upgrade_Level":
					__result = Clamp(UpgradeType.SPEED, SafeFind(upgrades, UpgradeType.SPEED).level);
					return false;
				case "Taser_Capacity_Upgrade_Level":
					__result = Clamp(UpgradeType.TASER_CAPACITY, SafeFind(upgrades, UpgradeType.TASER_CAPACITY).level);
					return false;
				case "Health_Backpack_Upgrade_Level":
					__result = Clamp(UpgradeType.HEALTH_BACKPACK, SafeFind(upgrades, UpgradeType.HEALTH_BACKPACK).level);
					return false;
				case "Taser_Backpack_Upgrade_Level":
					__result = Clamp(UpgradeType.TASER_BACKPACK, SafeFind(upgrades, UpgradeType.TASER_BACKPACK).level);
					return false;
				case "Taser_Power_Upgrade_Level":
					__result = Clamp(UpgradeType.TASER_POWER, SafeFind(upgrades, UpgradeType.TASER_POWER).level);
					return false;
				case "Stealth_Upgrade_Level":
					__result = Clamp(UpgradeType.STEALTH, SafeFind(upgrades, UpgradeType.STEALTH).level);
					return false;
				case "Aim_Stabilizer_Upgrade_Level":
					__result = Clamp(UpgradeType.AIM_STABILIZER, SafeFind(upgrades, UpgradeType.AIM_STABILIZER).level);
					return false;
				case "Hover_Upgrade_Level":
					__result = Clamp(UpgradeType.HOVER, SafeFind(upgrades, UpgradeType.HOVER).level);
					return false;
				case "Scope_Upgrade_Level":
					__result = Clamp(UpgradeType.SCOPE, SafeFind(upgrades, UpgradeType.SCOPE).level);
					return false;
				case "Safe_Landing_Upgrade_Level":
					__result = Clamp(UpgradeType.SAFE_LANDING, SafeFind(upgrades, UpgradeType.SAFE_LANDING).level);
					return false;
				case "UV_Flashlight_Upgrade_Level":
					__result = Clamp(UpgradeType.UV_FLASHLIGHT, SafeFind(upgrades, UpgradeType.UV_FLASHLIGHT).level);
					return false;
				case "Scanner_Upgrade_Level":
					__result = Clamp(UpgradeType.SCANNER, SafeFind(upgrades, UpgradeType.SCANNER).level);
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
					__result = SafeFind(upgrades, UpgradeType.DODGE).active;
					return false;
				case "Has_Sprint":
					__result = SafeFind(upgrades, UpgradeType.SPRINT).active;
					return false;
				case "Has_HS":
					__result = SafeFind(upgrades, UpgradeType.HYPER_SPEED).active;
					return false;
				// Add more flags if the game checks other boolean keys
				case "Has_Jetpack":
					__result = SafeFind(upgrades, UpgradeType.JETPACK).active;
					return false;
				// Ensure taser availability follows level save (default true) instead of base game save
				case "Has_Taser":
				case "Has_Tazer":
				case "Has_Gun":
					if (PlayModeController.Instance != null && PlayModeController.Instance.globalProperties != null)
					{
						if (PlayModeController.Instance.globalProperties.TryGetValue("HasTaser", out var hasTaserObj) && hasTaserObj is bool hasTaser)
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
				__result = true;
				return false;
			}
			return true;
		}
	}
}
