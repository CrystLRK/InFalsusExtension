using HarmonyLib;
using InFalsusChartExt.CustomSong.Scores;

namespace InFalsusChartExt
{
    /// <summary>
    /// Observes the final native result after ResultsScene.Start has updated
    /// the original high-score table. Only custom songs are persisted.
    /// </summary>
    [HarmonyPatch]
    internal static class ResultsScoreCapturePatch
    {
        [HarmonyPrepare]
        private static bool Prepare()
            => BuildProfile.Supports(NativeCapability.CustomScores);

        [HarmonyTargetMethod]
        private static System.Reflection.MethodBase TargetMethod()
            => NativePatchTargetResolver.ResolveHarmony("resultsStart");

        [HarmonyPostfix]
        private static void Postfix()
            => CustomScoreRuntime.CaptureFinalResult();
    }
}
