using System;
using System.Reflection;
using HarmonyLib;
using Il2Cpp;
using UnityEngine;

// This patch replaces the heavy vanilla UpdateAllParticleObjects logic while in LE Playmode.
// The original method iterates several SavedObjetsHolder lists every time particles are toggled.
// In custom Playmode we already track instantiated objects, so we can iterate that compact list once
// and touch only components that actually exist, avoiding null / missing holder issues.
namespace FS_LevelEditor.Playmode.Patches
{
	[HarmonyPatch]
	internal static class PerformancePatch
	{
		// Dynamically resolve the target method at runtime to avoid compile errors if the concrete
		// declaring type changes between game versions. We try a small list of known candidates.
		static MethodBase TargetMethod()
		{
			string[] candidates =
			{
                // Method is typically in OptionsController (most common target in current builds)
                "OptionsController:UpdateAllParticleObjects",
                // Fallback possibilities (older / potential refactors)
                "SavedObjetsHolder:UpdateAllParticleObjects",
				"ParticlesController:UpdateAllParticleObjects",
				"Controls:UpdateAllParticleObjects"
			};

			foreach (var sig in candidates)
			{
				var m = AccessTools.Method(sig);
				if (m != null) return m;
			}

			return null; // Harmony will log if not found; we silently fail.
		}

		static bool Prefix()
		{
			// Only replace logic while playing a custom level (PlayModeController active).
			if (PlayModeController.Instance == null) return true; // run original

			try
			{
				bool enabled = Controls.particlesEnabled;

				// Iterate once over instantiated LE objects instead of multiple SavedObjetsHolder lists.
				foreach (var leObj in PlayModeController.Instance.currentInstantiatedObjects)
				{
					if (leObj == null || leObj.isDeleted) continue;
					var go = leObj.gameObject;
					if (!go || !go.activeInHierarchy) continue;

					// Laser
					var laser = go.GetComponent<Laser_H_Controller>();
					if (laser != null)
					{
						SafeCall(() => laser.UpdateParticlesAllowed(enabled));
					}

					// Saw
					var saw = go.GetComponent<ScieScript>();
					if (saw != null)
					{
						SafeCall(() => saw.UpdateParticlesAllowed(enabled));
					}

					// Vent with smoke
					var vent = go.GetComponent<VentWithSmokeController>();
					if (vent != null)
					{
						SafeCall(() => vent.UpdateParticlesAllowed(enabled));
					}

					// Generic particle disabler blocks
					var disabler = go.GetComponent<ParticlesDisabler>();
					if (disabler != null)
					{
						SafeCall(() => disabler.RefreshState(enabled));
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Error($"PerformancePatch.UpdateAllParticleObjects replacement failed: {ex}");
			}

			// Skip original heavy implementation.
			return false;
		}

		static void SafeCall(Action a)
		{
			try { a(); } catch (Exception e) { Logger.DebugWarning("Particles update call error: " + e.Message); }
		}
	}
}
