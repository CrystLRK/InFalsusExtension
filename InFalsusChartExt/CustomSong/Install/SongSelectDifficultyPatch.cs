using System.Reflection;
using HarmonyLib;
using InFalsusChartExt.CustomSong.Discovery;
using InFalsusChartExt.CustomSong.Lifecycle;
#if MELONLOADER
using Il2Cppifapp.Game.Data;
using Il2Cppifapp.Game.Scenes;
#elif BEPINEX
using ifapp.Game.Data;
using ifapp.Game.Scenes;
#endif

namespace InFalsusChartExt.CustomSong.Install
{
    /// <summary>
    /// Tracks the song-select difficulty and rewrites the preview window just
    /// before the native preview recorder reads it. Both entry points are
    /// normal managed methods; no UI clone or native detour is involved.
    /// </summary>
    [HarmonyPatch]
    internal static class SongSelectDifficultyPatch
    {
        private static bool _warned;

        [HarmonyPrepare]
        private static bool Prepare()
        {
            if (!BuildProfile.Supports(
                    NativeCapability.CustomSong))
                return false;
            bool available =
                AccessTools.Method(
                    typeof(SongSelectScene),
                    "_MN") is not null;
            if (!available && !_warned)
            {
                _warned = true;
                ChartExtMod.Log.Warning(
                    "[CustomSong] song-select difficulty hooks were not " +
                    "found; per-difficulty preview/audio will use slot 0.");
            }
            return available;
        }

        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() =>
            AccessTools.Method(
                typeof(SongSelectScene),
                "_MN");

        [HarmonyPrefix]
        private static void Prefix(
            SongId __0,
            ChartDifficultyFlag __1)
        {
            if (!CustomSongRuntime.IsInstalled)
                return;
            SongSelectionState.Set(__0.Value, __1);
            SongLibraryInstaller.UpdatePreviewForSelection(
                __0.Value,
                SongSelectionState.DifficultySlot);
        }
    }
}
