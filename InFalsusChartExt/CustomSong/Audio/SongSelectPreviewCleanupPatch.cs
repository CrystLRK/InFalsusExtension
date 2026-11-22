using System.Reflection;
using HarmonyLib;
#if MELONLOADER
using Il2Cppifapp.Game.Scenes;
#elif BEPINEX
using ifapp.Game.Scenes;
#endif

namespace InFalsusChartExt.CustomSong.Audio
{
    /// <summary>
    /// The selection preview is Mod-owned: its Channel is never published into
    /// the short-lived native controller. Destroying the song-select scene must
    /// therefore release it explicitly, otherwise the preview loops forever.
    /// </summary>
    [HarmonyPatch]
    internal static class SongSelectPreviewCleanupPatch
    {
        [HarmonyPrepare]
        private static bool Prepare()
        {
            if (!BuildProfile.Supports(
                    NativeCapability.CustomSong))
                return false;
            return AccessTools.Method(
                typeof(SongSelectScene),
                "OnDestroy") is not null;
        }

        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() =>
            AccessTools.Method(
                typeof(SongSelectScene),
                "OnDestroy");

        [HarmonyPostfix]
        private static void Postfix() =>
            CustomSongAudioController.ReleaseSelectionOwned();
    }
}
