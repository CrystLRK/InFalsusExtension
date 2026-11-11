using System.Reflection;
using HarmonyLib;

namespace InFalsusChartExt
{
    [HarmonyPatch]
    internal static class SkyBlockJudgePatch
    {
        [HarmonyPrepare]
        private static bool Prepare() =>
            NativePatchTargetResolver.IsAvailable("judgeOz");

        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() =>
            NativePatchTargetResolver.ResolveHarmony("judgeOz");

        [HarmonyPriority(Priority.Last)]
        [HarmonyPostfix]
        private static void Postfix(Il2Cpp_E._VD __instance)
            => SkyBlockJudgeRuntime.AfterNativeJudge(__instance);
    }
}
