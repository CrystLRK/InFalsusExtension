using System.Collections.Generic;
using InFalsusChartExt.CustomSong.Discovery;
using Il2Cppifapp.Game.Data;

namespace InFalsusChartExt.CustomSong.Scores
{
    /// <summary>
    /// Current-session lookup from runtime SongId to the installed custom
    /// plan and its generated SongInfo.
    /// </summary>
    internal sealed class CustomScorePlanIndex
    {
        private readonly Dictionary<ushort, CustomSongPlan> _songs =
            new Dictionary<ushort, CustomSongPlan>();
        private readonly Dictionary<ushort, SongInfo> _songInfos =
            new Dictionary<ushort, SongInfo>();

        internal CustomScorePlanIndex(
            CustomLibraryPlan plan,
            IReadOnlyList<SongInfo> installedSongs)
        {
            foreach (CustomPackPlan pack in plan.Packs)
            {
                foreach (CustomSongPlan song in pack.Songs)
                    _songs[song.RuntimeSongId] = song;
            }
            for (int i = 0; i < installedSongs.Count; i++)
            {
                SongInfo songInfo = installedSongs[i];
                if (songInfo is not null)
                    _songInfos[songInfo.Id.Value] = songInfo;
            }
        }

        internal bool TryGetSong(
            ushort runtimeSongId,
            out CustomSongPlan song) =>
            _songs.TryGetValue(runtimeSongId, out song);

        internal bool TryGetSongInfo(
            ushort runtimeSongId,
            out SongInfo songInfo) =>
            _songInfos.TryGetValue(runtimeSongId, out songInfo);

        internal bool TryGetChart(
            CustomSongPlan song,
            ChartDifficultyFlag difficulty,
            out CustomChartPlan chart)
        {
            for (int i = 0; i < song.Charts.Count; i++)
            {
                if (song.Charts[i].Difficulty == difficulty)
                {
                    chart = song.Charts[i];
                    return true;
                }
            }
            chart = null;
            return false;
        }

        internal static string Key(
            CustomPackPlan pack,
            CustomSongPlan song,
            CustomChartPlan chart) =>
            CustomScoreFile.Key(
                pack.Config.Id,
                song.Config.Id,
                DifficultyName(chart.Difficulty));

        internal static string DifficultyName(
            ChartDifficultyFlag difficulty) =>
            difficulty switch
            {
                ChartDifficultyFlag.Minimal => "MIN",
                ChartDifficultyFlag.Evolved => "EVO",
                ChartDifficultyFlag.Ultimate => "ULT",
                ChartDifficultyFlag.Forbidden => "FBD",
                _ => difficulty.ToString(),
            };
    }
}
