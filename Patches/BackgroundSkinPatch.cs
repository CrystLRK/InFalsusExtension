using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace InFalsusChartExt
{
    [HarmonyPatch]
    internal static class BackgroundSkinPatch
    {
        [HarmonyPrepare]
        private static bool Prepare() =>
            NativePatchTargetResolver.IsAvailable("backgroundApply");

        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() =>
            NativePatchTargetResolver.ResolveHarmony(
                "backgroundApply");

        [HarmonyPostfix]
        private static void Postfix(Material __0) => BackgroundSkin.Apply(__0);
    }
}
