using System.Reflection;
using HarmonyLib;

namespace InFalsusChartExt
{
    /// <summary>
    /// Writes simulated input states after the native note update. The
    /// native _hb pass refreshes note timing before _Oz consumes it, so
    /// writing here matches the original autoplay input order.
    /// </summary>
    [HarmonyPatch]
    internal static class AutoplayInputTickPatch
    {
        [HarmonyPrepare]
        private static bool Prepare()
        {
            bool ready =
                BuildProfile.Supports(NativeCapability.Autoplay) &&
                NativePatchTargetResolver.IsAvailable(
                    "logicalNotePlayerHb");
            if (ready)
            {
                try
                {
                    ChartExtMod.Log.Msg(
                        "[AutoplayInput] hb postfix installed");
                }
                catch
                {
                }
            }
            return ready;
        }

        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() =>
            NativePatchTargetResolver.ResolveHarmony(
                "logicalNotePlayerHb");

        [HarmonyPostfix]
        private static void Postfix() =>
            AutoplayRuntime.ApplyInputs(false);
    }
}
