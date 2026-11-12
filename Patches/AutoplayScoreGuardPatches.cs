using HarmonyLib;

namespace InFalsusChartExt
{
    [HarmonyPatch]
    internal static class AutoplayResultsCompletePatch
    {
        [HarmonyPrepare]
        private static bool Prepare()
            => BuildProfile.Supports(
                NativeCapability.AutoplayScoreGuard);

        [HarmonyTargetMethod]
        private static System.Reflection.MethodBase TargetMethod()
            => NativePatchTargetResolver.ResolveHarmony(
                "resultsStart");

        [HarmonyPriority(Priority.Last)]
        [HarmonyPostfix]
        private static void Postfix()
            => AutoplayScoreGuard.CompleteResultsStart();
    }

    [HarmonyPatch]
    internal static class AutoplaySaveFreezePatch
    {
        [HarmonyPrepare]
        private static bool Prepare()
            => BuildProfile.Supports(
                NativeCapability.AutoplayScoreGuard);

        [HarmonyTargetMethod]
        private static System.Reflection.MethodBase TargetMethod()
            => NativePatchTargetResolver.ResolveHarmony(
                "saveRequest");

        [HarmonyPrefix]
        private static bool Prefix(
            Il2CppSystem.Action<Il2Cpp_K._rH> __0)
        {
            if (!AutoplayScoreGuard.FreezeActive)
                return true;
            try
            {
                __0?.Invoke(Il2Cpp_K._rH._REb);
            }
            catch (System.Exception exception)
            {
                ChartExtMod.Log.Error(
                    "[ChartExt] autoplay save callback failed: " +
                    exception);
            }
            return false;
        }
    }
}
