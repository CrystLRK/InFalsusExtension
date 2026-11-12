using Il2Cppifapp.Game.Data;

namespace InFalsusChartExt.CustomSong.Discovery
{
    /// <summary>
    /// Determines whether the current custom song config actually provides a
    /// difficulty. The game save and native chart records remain untouched.
    /// </summary>
    internal static class CustomDifficultyUnlock
    {
        internal static bool ShouldUnlock(
            in SongInfo songInfo,
            ChartDifficultyFlag difficulty)
            => ShouldUnlock(songInfo.BaseName, difficulty);

        internal static bool ShouldUnlock(
            string baseName,
            ChartDifficultyFlag difficulty)
        {
            if (baseName == null ||
                !Lifecycle.CustomSongRuntime.TryGetSongByBaseName(
                    baseName,
                    out CustomSongPlan song))
                return false;
            for (int i = 0; i < song.Charts.Count; i++)
            {
                if (song.Charts[i].Difficulty == difficulty)
                    return true;
            }
            return false;
        }
    }
}
