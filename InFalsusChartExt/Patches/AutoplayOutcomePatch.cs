using System.Reflection;
using HarmonyLib;

namespace InFalsusChartExt
{
    [HarmonyPatch]
    internal static class AutoplayOutcomePatch
    {
        [HarmonyPrepare]
        private static bool Prepare() =>
            NativePatchTargetResolver.IsAvailable("judgeOz");

        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() =>
            NativePatchTargetResolver.ResolveHarmony("judgeOz");

        [HarmonyPostfix]
        private static void Postfix() => AutoplayRuntime.ApplyOutcomes();
    }
}
