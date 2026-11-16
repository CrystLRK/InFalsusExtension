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

        [HarmonyPrefix]
        private static void Prefix() =>
            AutoplayRuntime.ApplyInputs(true);

        [HarmonyPostfix]
        private static void Postfix() => AutoplayRuntime.ApplyOutcomes();
    }
}
