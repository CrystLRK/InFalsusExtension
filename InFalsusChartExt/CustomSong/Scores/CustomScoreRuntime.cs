using System;
using System.Collections.Generic;
using InFalsusChartExt.CustomSong.Discovery;
using Il2Cppifapp.Game.Data;

namespace InFalsusChartExt.CustomSong.Scores
{
    internal static class CustomScoreRuntime
    {
        private static readonly Dictionary<string, CustomScoreEntry>
            EntriesByKey =
                new Dictionary<string, CustomScoreEntry>(
                    StringComparer.OrdinalIgnoreCase);
        private static CustomScoreDocument _document;
        private static CustomScorePlanIndex _index;
        private static CustomLibraryPlan _pendingPlan;
        private static float _nextAttemptTime;
        private static bool _waitingLogged;
        private static bool _ready;
        private static bool _writeBlocked;

        internal static string Status { get; private set; } =
            "尚未加载";

        internal static void Initialize(
            CustomLibraryPlan plan,
            IReadOnlyList<SongInfo> installedSongs)
        {
            Reset();
            if (plan is null || installedSongs is null ||
                installedSongs.Count == 0)
            {
                Status = "没有自制谱";
                return;
            }

            _index = new CustomScorePlanIndex(
                plan, installedSongs);

            _document = CustomScoreFile.Load(out string loadError);
            if (_document is null)
            {
                _writeBlocked = true;
                Status = "成绩文件损坏，写入已停用";
                ChartExtMod.Log.Error(
                    "[CustomScore] refusing to overwrite invalid score " +
                    "storage: " + loadError);
                return;
            }
            if (!string.IsNullOrWhiteSpace(loadError))
            {
                ChartExtMod.Log.Warning(
                    "[CustomScore] " + loadError);
            }

            RebuildEntryIndex();
            _pendingPlan = plan;
            TryApplyPending();
        }

        internal static void Tick()
        {
            if (_ready || _writeBlocked ||
                _pendingPlan is null ||
                UnityEngine.Time.realtimeSinceStartup <
                    _nextAttemptTime)
                return;
            TryApplyPending();
        }

        private static void TryApplyPending()
        {
            _nextAttemptTime =
                UnityEngine.Time.realtimeSinceStartup + 2f;
            GameResultsV4 results =
                CustomScoreNativeBridge.FindGameResults();
            if (results is null)
            {
                Status = "等待原版成绩表";
                if (!_waitingLogged)
                {
                    _waitingLogged = true;
                    ChartExtMod.Log.Warning(
                        "[CustomScore] GameResultsV4 is not ready; " +
                        "will retry.");
                }
                return;
            }

            CustomLibraryPlan plan = _pendingPlan;
            bool changed = false;
            foreach (CustomPackPlan pack in plan.Packs)
            {
                foreach (CustomSongPlan song in pack.Songs)
                {
                    if (!_index.TryGetSongInfo(
                            song.RuntimeSongId,
                            out SongInfo songInfo))
                        continue;
                    foreach (CustomChartPlan chart in song.Charts)
                    {
                        string key = CustomScorePlanIndex.Key(
                            pack, song, chart);
                        if (EntriesByKey.TryGetValue(
                                key, out CustomScoreEntry stored) &&
                            stored.Best is not null)
                        {
                            GameResultV4 candidate =
                                CustomScoreNativeBridge.ToNative(
                                    stored.Best, songInfo, chart);
                            results.TryUpdate(
                                ref songInfo, ref candidate, out _);
                        }

                        if (results.TryGetResult(
                                ref songInfo,
                                chart.Difficulty,
                                out GameResultV4 best))
                        {
                            changed |= MergeNative(
                                pack, song, chart, best);
                        }
                    }
                }
            }

            if (changed &&
                !CustomScoreFile.Save(
                    _document, out string saveError))
            {
                _writeBlocked = true;
                Status = "成绩文件写入失败";
                ChartExtMod.Log.Error(
                    "[CustomScore] initial save failed: " + saveError);
                return;
            }

            _pendingPlan = null;
            _ready = true;
            Status =
                $"已加载 {EntriesByKey.Count} 条最佳成绩";
            ChartExtMod.Log.Msg(
                $"[CustomScore] storage ready: " +
                $"entries={EntriesByKey.Count}, " +
                $"file={CustomScoreFile.Path}");
        }

        internal static void CaptureFinalResult()
        {
            if (AutoplayScoreGuard.ShouldSuppressResults)
            {
                ChartExtMod.Log.Msg(
                    "[CustomScore] skipped for autoplay run");
                return;
            }
            if (!_ready || _writeBlocked)
                return;
            try
            {
                GameResultV4 final =
                    Il2Cppifapp.Game.Scenes.ResultsScene._gq;
                ushort songId = final.SongId.Value;
                if (!_index.TryGetSong(
                        songId, out CustomSongPlan song))
                    return;

                GameResultsV4 results =
                    CustomScoreNativeBridge.FindGameResults();
                if (results is null)
                {
                    MarkWriteFailure(
                        "原版成绩表不可用，无法确认最终最佳成绩");
                    return;
                }
                if (!_index.TryGetSongInfo(
                        songId, out SongInfo songInfo))
                {
                    MarkWriteFailure(
                        "当前自制谱 SongInfo 不可用");
                    return;
                }
                if (!results.TryGetResult(
                        ref songInfo,
                        final.Difficulty,
                        out GameResultV4 best))
                {
                    best = final;
                }

                if (!_index.TryGetChart(
                        song,
                        final.Difficulty,
                        out CustomChartPlan chart))
                {
                    MarkWriteFailure(
                        "结果难度没有对应的自制谱");
                    return;
                }

                bool changed = MergeNative(
                    song.Pack, song, chart, best);
                if (changed &&
                    !CustomScoreFile.Save(
                        _document, out string saveError))
                {
                    MarkWriteFailure(saveError);
                    return;
                }
                if (changed)
                {
                    Status =
                        $"已保存 {EntriesByKey.Count} 条最佳成绩";
                    ChartExtMod.Log.Msg(
                        $"[CustomScore] saved {song.Config.Id} " +
                        $"{CustomScorePlanIndex.DifficultyName(chart.Difficulty)} " +
                        $"score={best.PlayerScore}");
                }
            }
            catch (Exception exception)
            {
                MarkWriteFailure(exception.Message);
            }
        }

        internal static void Reset()
        {
            EntriesByKey.Clear();
            _document = null;
            _index = null;
            _pendingPlan = null;
            _nextAttemptTime = 0f;
            _waitingLogged = false;
            _ready = false;
            _writeBlocked = false;
            Status = "尚未加载";
        }

        private static bool MergeNative(
            CustomPackPlan pack,
            CustomSongPlan song,
            CustomChartPlan chart,
            GameResultV4 result)
        {
            string key = CustomScorePlanIndex.Key(
                pack, song, chart);
            CustomScoreRecord record =
                CustomScoreNativeBridge.ToRecord(result);
            if (EntriesByKey.TryGetValue(
                    key, out CustomScoreEntry entry) &&
                entry.Best is not null &&
                CustomScoreRecordComparer.Equals(
                    entry.Best, record))
            {
                return false;
            }
            if (entry is null)
            {
                entry = new CustomScoreEntry
                {
                    PackId = pack.Config.Id,
                    SongId = song.Config.Id,
                    BaseName = song.BaseName,
                    Difficulty =
                        CustomScorePlanIndex.DifficultyName(
                            chart.Difficulty),
                };
                _document.Entries.Add(entry);
                EntriesByKey.Add(key, entry);
            }
            entry.BaseName = song.BaseName;
            entry.Best = record;
            return true;
        }

        private static void RebuildEntryIndex()
        {
            EntriesByKey.Clear();
            for (int i = 0; i < _document.Entries.Count; i++)
            {
                CustomScoreEntry entry = _document.Entries[i];
                string key = CustomScoreFile.Key(
                    entry.PackId,
                    entry.SongId,
                    entry.Difficulty);
                EntriesByKey[key] = entry;
            }
        }

        private static void MarkWriteFailure(string error)
        {
            _writeBlocked = true;
            Status = "成绩写入失败";
            ChartExtMod.Log.Error(
                "[CustomScore] persistence disabled: " + error);
        }
    }
}
