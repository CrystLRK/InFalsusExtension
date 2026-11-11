using HarmonyLib;
using Il2Cppifapp.Game.Data;
using InFalsusChartExt.CustomSong.Discovery;

namespace InFalsusChartExt
{
    [HarmonyPatch]
    internal static class CustomDifficultyUnlockPatch
    {
        [HarmonyPrepare]
        private static bool Prepare()
            => BuildProfile.Supports(
                NativeCapability.CustomDifficultyUnlock);

        [HarmonyTargetMethod]
        private static System.Reflection.MethodBase TargetMethod()
            => NativePatchTargetResolver.ResolveHarmony(
                "difficultyStatus");

        [HarmonyPostfix]
        private static void Postfix(
            SongInfo __1,
            ChartDifficultyFlag __2,
            ref Il2Cpp_K._Ch __result)
        {
            if (CustomDifficultyUnlock.ShouldUnlock(
                    in __1,
                    __2))
                __result = Il2Cpp_K._Ch._vZA;
        }
    }
}
