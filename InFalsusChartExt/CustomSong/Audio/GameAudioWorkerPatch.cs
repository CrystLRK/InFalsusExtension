using System.Reflection;
using HarmonyLib;
#if MELONLOADER
using Il2Cppifapp.Game.Scenes;
using Il2Cpp_J;
#elif BEPINEX
using ifapp.Game.Scenes;
using _J;
#endif

namespace InFalsusChartExt.CustomSong.Audio
{
    /// <summary>
    /// `GameScene._bl(_Cg)` disposes the old audio worker and builds a new one
    /// from the controller's Sound. Install the custom Sound before that
    /// happens so the worker caches the custom track length instead of the
    /// template length.
    /// </summary>
    [HarmonyPatch]
    internal static class GameAudioWorkerPatch
    {
        [HarmonyPrepare]
        private static bool Prepare() =>
            AccessTools.Method(
                typeof(GameScene), "_bl") is not null;

        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() =>
            AccessTools.Method(
                typeof(GameScene), "_bl");

        [HarmonyPrefix]
        private static void Prefix(_Cg __0) =>
            CustomSongAudioController.PrepareBeforeAudioWorker(__0);
    }
}
