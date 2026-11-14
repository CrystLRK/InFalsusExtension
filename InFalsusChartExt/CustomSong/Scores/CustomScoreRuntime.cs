using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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
#if IFCE_DIAGNOSTICS
            LogFinalResultDiagnostic();
#endif
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

#if IFCE_DIAGNOSTICS
        private static void LogFinalResultDiagnostic()
        {
            try
            {
                GameResultV4 final =
                    Il2Cppifapp.Game.Scenes.ResultsScene._gq;
                CustomScoreRecord record =
                    CustomScoreNativeBridge.ToRecord(final);
                int nativeNotes = -1;
                int visualNotes = -1;
                int noInput = 0;
                int noClip = 0;
                ChartRuntimeFrame context =
                    InFalsusChartExt.ChartRuntimeContext.Current;
                if (context is not null)
                {
                    nativeNotes = context.NativeChart?.Count ?? 0;
                    if (context.VisualChart is not null)
                    {
                        visualNotes = context.VisualChart.Count;
                        for (int i = 0;
                             i < context.VisualChart.Notes.Count;
                             i++)
                        {
                            InFalsusChartExt.VisualNoteDescriptor note =
                                context.VisualChart.Notes[i];
                            if (note is null) continue;
                            if (note.Lifetime ==
                                InFalsusChartExt.VisualNoteLifetime.NoClip)
                                noClip++;
                            else
                                noInput++;
                        }
                    }
                }
                ChartExtMod.Log.Msg(
                    "[r7-score] " +
                    $"score={record.Score} combo={record.MaxCombo} " +
                    $"tap=[{FormatCounts(record.Tap)}] " +
                    $"hold=[{FormatCounts(record.Hold)}] " +
                    $"sky=[{FormatCounts(record.SkyArea)}] " +
                    $"flick=[{FormatCounts(record.Flick)}] " +
                    $"native={nativeNotes} visual={visualNotes} " +
                    $"noinput={noInput} noclip={noClip}");
                LogRecordStateDiagnostic();
            }
            catch (Exception exception)
            {
                ChartExtMod.Log.Warning(
                    "[r7-score] diagnostic failed: " +
                    exception.Message);
            }
        }

        private static string FormatCounts(
            JudgementCounts counts) =>
            $"none={counts.None},miss={counts.Miss}," +
            $"nearEarly={counts.NearEarly}," +
            $"nearLate={counts.NearLate}," +
            $"perfectEarly={counts.PerfectEarly}," +
            $"perfectLate={counts.PerfectLate}," +
            $"shiny={counts.Shiny}";

        private static void LogRecordStateDiagnostic()
        {
            Il2Cppifapp.Game.LogicalNotePlayer player =
                Il2Cppifapp.Game.LogicalNotePlayer._Qe;
            Il2Cpp_b._T compiled = player?._ve;
            var records = compiled?._XC;
            if (records is null || records.Pointer == IntPtr.Zero)
            {
                ChartExtMod.Log.Msg(
                    "[r7-records] unavailable");
                return;
            }

            int notes = 0;
            int segments = 0;
            int none = 0;
            int shiny = 0;
            int anomalies = 0;
            foreach (var pair in records)
            {
                notes++;
                Il2CppSystem.Memory<Il2Cpp_K._cH> memory =
                    pair.Value;
                if (memory._object == null ||
                    memory._object.Pointer == IntPtr.Zero)
                    continue;
                int count = memory._length;
                if (count <= 0 || count > 4096)
                    continue;
                IntPtr arrayObject = memory._object.Pointer;
                int sliceIndex = memory._index;
                int arrayLength = Marshal.ReadInt32(
                    arrayObject +
                    NativeLayoutCatalog.ArrayLengthOffset);
                if (sliceIndex < 0 ||
                    sliceIndex + count > arrayLength)
                    continue;
                unsafe
                {
                    for (int r = 0; r < count; r++)
                    {
                        segments++;
                        byte* record = InteropUtil.RecordPointer(
                            arrayObject, sliceIndex, r);
                        byte judgment =
                            *(record +
                              InteropUtil.JudgmentOffset);
                        byte final =
                            *(record +
                              InteropUtil.FinalOffset);
                        double relative =
                            BitConverter.Int64BitsToDouble(
                                *(long*)(record +
                                    NativeLayoutCatalog.Record
                                        .RelativeTime));
                        double absolute =
                            BitConverter.Int64BitsToDouble(
                                *(long*)(record +
                                    InteropUtil.AbsoluteTimeOffset));
                        int recordType =
                            *(int*)(record +
                                InteropUtil.RecordTypeOffset);
                        int recordState =
                            *(int*)(record +
                                InteropUtil.RecordStateOffset);
                        ChartExtMod.Log.Msg(
                            "[r7-record-map] " +
                            $"id={pair.Key} index={r}/{count} " +
                            $"start={absolute - relative:F1} " +
                            $"abs={absolute:F1} rel={relative:F1} " +
                            $"type={recordType} state={recordState} " +
                            $"judgment={judgment} final={final}");
                        if (judgment == ChartExtState.JudgeNone)
                            none++;
                        else if (judgment ==
                                 ChartExtState.JudgeShiny)
                            shiny++;
                        else if (anomalies++ < 24)
                        {
                            ChartExtMod.Log.Msg(
                                "[r7-record] " +
                                $"id={pair.Key} index={r} " +
                                $"relative={relative:F1} " +
                                $"judgment={judgment}");
                        }
                    }
                }
            }
            ChartExtMod.Log.Msg(
                "[r7-records] " +
                $"notes={notes} segments={segments} " +
                $"none={none} shiny={shiny} " +
                $"anomalies={anomalies}");
        }
#endif

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
