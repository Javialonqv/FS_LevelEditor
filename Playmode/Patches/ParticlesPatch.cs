using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FS_LevelEditor.Playmode.Patches
{
    [HarmonyLib.HarmonyPatch(typeof(Controls), nameof(Controls.UpdateAllParticleObjects))]
    public static class ParticlesPatch
    {
        static Laser_H_Controller[] allLasers;
        static ScieScript[] allSaws;
        static VentWithSmokeController[] allVentsWithSmoke;

        public static void GetObjectsWithParticlesReferences()
        {
            //allLasers = Utils.FindObjectsOfTypeIncludingDisabled<Laser_H_Controller>();
            //allSaws = Utils.FindObjectsOfTypeIncludingDisabled<ScieScript>();
            //allVentsWithSmoke = Utils.FindObjectsOfTypeIncludingDisabled<VentWithSmokeController>();
            allLasers = PlayModeController.Instance.levelObjectsParent.GetComponentsInChildren<Laser_H_Controller>(true);
            allSaws = PlayModeController.Instance.levelObjectsParent.GetComponentsInChildren<ScieScript>(true);
            allVentsWithSmoke = PlayModeController.Instance.levelObjectsParent.GetComponentsInChildren<VentWithSmokeController>(true);
        }

        public static void Postfix()
        {
            if (allLasers != null && allLasers.Length > 0)
            {
                foreach (var laser in allLasers)
                {
                    if (!laser || !laser.hasParticles) continue;

                    laser.UpdateParticlesAllowed(Controls.particlesEnabled);
                }
            }
            if (allSaws != null && allSaws.Length > 0)
            {
                foreach (var saw in allSaws)
                {
                    if (!saw) continue;

                    saw.UpdateParticlesAllowed(Controls.particlesEnabled);
                }
            }
            if (allVentsWithSmoke != null && allVentsWithSmoke.Length > 0)
            {
                foreach (var vent in allVentsWithSmoke)
                {
                    if (!vent) continue;

                    // This list only contains LE objects, they MUST have an LE_Object component in their parent.
                    vent.UpdateParticlesAllowed(Controls.particlesEnabled && vent.GetComponentInParent<LE_Object>(true).GetProperty<bool>("Particles"));
                }
            }
        }
    }
}
