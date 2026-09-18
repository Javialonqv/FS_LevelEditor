using UnityEngine;
using HarmonyLib;
using FractalSpace;

namespace FS_LevelEditor.Playmode.Patches
{
    [HarmonyPatch(typeof(BlocScript), nameof(BlocScript.OnCubeInHands), new Type[] { typeof(bool), typeof(bool), typeof(bool) })]
    public static class LastPosGlitchFixPart1
    {
        public static void Postfix(BlocScript __instance)
        {
            __instance.m_rigidbody.interpolation = RigidbodyInterpolation.None;
        }
    }
    [HarmonyPatch(typeof(BlocScript), nameof(BlocScript.DeactivateBloc), new Type[] {typeof(bool), typeof(bool)})]
    public static class LastPosGlitchFixPart2 { 
        public static void Postfix(BlocScript __instance)
        {
            __instance.m_rigidbody.interpolation = RigidbodyInterpolation.Extrapolate;
        }
    }

}
