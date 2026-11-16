using System.Reflection;
using HarmonyLib;
#if MELONLOADER
using Il2Cpp_J;
#elif BEPINEX
using _J;
#endif

namespace InFalsusChartExt
{
    [HarmonyPatch]
    internal static class AudioOffsetCapturePatch
    {
        [HarmonyPrepare]
        private static bool Prepare() =>
            NativePatchTargetResolver.IsAvailable("audioFactory");

        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() =>
            NativePatchTargetResolver.ResolveHarmony("audioFactory");

        [HarmonyPostfix]
        private static void Postfix(_Cg __result, bool __6)
        {
            if (!__6) AudioOffsetRuntime.Register(__result);
        }
    }
}
