using System.Reflection;
using HarmonyLib;

namespace InFalsusChartExt
{
    [HarmonyPatch]
    internal static class TimingGroupLiveTapPatch
    {
        [HarmonyPrepare]
        private static bool Prepare() =>
            NativePatchTargetResolver.IsAvailable(
                "tapNoteVisual");

        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() =>
            NativePatchTargetResolver.ResolveHarmony(
                "tapNoteVisual");

        [HarmonyPostfix]
        private static void Postfix(
            Il2Cppifapp.Game.Scenes.Game.TapNote __instance)
        {
            TimingGroupVisualRuntime.RegisterTap(__instance);
        }
    }
}
