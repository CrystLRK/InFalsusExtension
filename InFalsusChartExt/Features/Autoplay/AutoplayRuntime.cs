using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using HarmonyLib;

namespace InFalsusChartExt
{
    internal enum AutoplayMode
    {
        Off = 0,
        Auto = 1,
        Engine = 2,
    }

    /// <summary>
    /// autoplay：把每个可判定音符自动判为最高 EXACT（大 EXACT / Shiny，记录字节 6），
    /// 覆盖原版与扩展音符；**noinput 跳过**（其记录被 park，不参与任何判定与计分），
    /// bomb 交给独立 BombJudgeRuntime（不点=EXACT、点击=BREAK）。
    ///
    /// 机制与炸弹同源：`_Oz` 后置在 `_Tz` 消费事件之前改写记录判定字节 →
    /// 连击/实时分数/判定反馈/结算全部走原生链路。多段记录（hold/skyarea）逐段改写；
    /// 若某记录过了判定时刻仍未被游戏 finalize（极端情况），补写字节 6 保证结算不出现
    /// None（保底物量，不产生实时反馈）。
    /// </summary>
    internal static class AutoplayRuntime
    {
        private static bool _enabled;
        private static AutoplayMode _mode;
        private static bool _groundOnly;
        private static int _cursor;
        private static int _holdCursor;
        private static readonly List<ChartExtState.AutoplayDue>
            ActiveHolds =
                new List<ChartExtState.AutoplayDue>();
        private static readonly HashSet<long> AutoplaySkyHits =
            new HashSet<long>();
        private static readonly HashSet<long> AutoplaySkyPresented =
            new HashSet<long>();
        private static readonly List<(long GlobalIndex, double Relative)>
            ChangedSkyRecords =
                new List<(long GlobalIndex, double Relative)>(16);
        private static int _errors;
#if IFCE_DIAGNOSTICS
        private static int _r7LateWrites;
#endif

        internal static bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value) return;
                _enabled = value;
                if (!_enabled)
                {
                    _mode = AutoplayMode.Off;
                    _groundOnly = false;
                    AutoplayEngineInput.Enabled = false;
                    AutoplayEngineInput.ReleaseAll();
                    RdAutoplayRuntime.ReleaseAll();
                }
                else if (_mode == AutoplayMode.Off)
                {
                    _mode = AutoplayEngineInput.Enabled
                        ? AutoplayMode.Engine
                        : AutoplayMode.Auto;
                }
                ResetCursors();
                try { ChartExtMod.Log.Msg($"[ChartExt] autoplay {(_enabled ? "ON" : "OFF")}"); } catch { }
            }
        }

        /// <summary>
        /// Session-only Autoplay mode. Auto uses record replacement and the
        /// engine cursor only for visual feedback; Engine uses native input
        /// simulation and never rewrites judgement records.
        /// </summary>
        internal static AutoplayMode Mode
        {
            get => _mode;
            set
            {
                if (_mode == value) return;
                _mode = value;
                if (_mode == AutoplayMode.Off)
                {
                    _enabled = false;
                    _groundOnly = false;
                    AutoplayEngineInput.Enabled = false;
                    AutoplayEngineInput.ReleaseAll();
                    RdAutoplayRuntime.ReleaseAll();
                }
                else
                {
                    AutoplayEngineInput.Enabled =
                        _mode == AutoplayMode.Engine;
                    _enabled = true;
                }
                ResetCursors();
                try
                {
                    ChartExtMod.Log.Msg(
                        "[ChartExt] autoplay mode " + _mode);
                }
                catch { }
            }
        }

        /// <summary>
        /// When enabled together with Enabled, only ground notes (tap/hold,
        /// types 1/2) receive automatic judgments. Flick and SkyArea remain
        /// manual while still participating in native scoring normally.
        /// </summary>
        internal static bool GroundOnly
        {
            get => _groundOnly;
            set
            {
                if (_groundOnly == value) return;
                if (value && _mode == AutoplayMode.Off)
                    Mode = AutoplayMode.Auto;
                _groundOnly = value;
                AutoplayEngineInput.ReleaseAll();
                RdAutoplayRuntime.ReleaseAll();
                ResetCursors();
                try
                {
                    ChartExtMod.Log.Msg(
                        $"[ChartExt] autoplay mode {(_groundOnly ? "GROUND_ONLY" : "ALL")}");
                }
                catch { }
            }
        }

        /// <summary>每帧维护时间游标（跳过已远离的条目；时钟回退时归零自愈）。</summary>
        internal static void Tick()
        {
            if (!BuildProfile.Supports(NativeCapability.Autoplay))
                return;
            if (!_enabled) return;
            try
            {
                if (!AutoplayScoreGuard.EnsureRunGuard())
                {
                    Enabled = false;
                    ResetCursors();
                }
            }
            catch (Exception e)
            {
                if (_errors < 10) { _errors++; ChartExtMod.Log.Error("[ChartExt] autoplay tick failed: " + e); }
            }
        }

        internal static void ResetForChartSwitch()
        {
            ResetCursors();
            AutoplayEngineInput.ResetForChartSwitch();
            RdAutoplayRuntime.ResetForChartSwitch();
        }

        /// <summary>
        /// Runs immediately before the native judge consumes input states.
        /// Auto mode rewrites records; Engine mode injects native input.
        /// </summary>
        internal static void ApplyInputs(bool judgePass)
        {
            if (!BuildProfile.Supports(NativeCapability.Autoplay))
                return;
            if (!_enabled)
                return;
            try
            {
                if (!AutoplayScoreGuard.EnsureRunGuard())
                {
                    Enabled = false;
                    AutoplayEngineInput.ReleaseAll();
                    return;
                }
                double now = NowMs();
                if (double.IsNaN(now))
                    return;
                if (!AutoplayEngineInput.Enabled)
                {
                    RdAutoplayRuntime.Tick(now, judgePass);
                    return;
                }
                AutoplayEngineInput.Tick(now, judgePass);
                if (AutoplayEngineInput.HasActed)
                    AutoplayScoreGuard.MarkUsed();
            }
            catch (Exception e)
            {
                if (_errors < 10)
                {
                    _errors++;
                    ChartExtMod.Log.Error(
                        "[ChartExt] autoplay input failed: " + e);
                }
            }
        }

        private static void ResetCursors()
        {
            _cursor = 0;
            _holdCursor = 0;
            ActiveHolds.Clear();
            AutoplaySkyHits.Clear();
            AutoplaySkyPresented.Clear();
            ChangedSkyRecords.Clear();
            SkyBlockFeedbackRuntime.SetAutoplaySkyHoldActive(false);
        }

        private static void UpdateActiveHolds(double now)
        {
            List<ChartExtState.AutoplayDue> dues =
                ChartExtState.AutoplayDues;
            while (_holdCursor < dues.Count)
            {
                ChartExtState.AutoplayDue due = dues[_holdCursor];
                if (due.StartMs > now + 300.0)
                    break;
                _holdCursor++;
                if (due.Type != 2)
                    continue;
                if (now > due.EndMs + 150.0)
                    continue;
                ActiveHolds.Add(due);
            }
            for (int i = ActiveHolds.Count - 1; i >= 0; i--)
            {
                if (now > ActiveHolds[i].EndMs + 150.0)
                    ActiveHolds.RemoveAt(i);
            }
        }

        /// <summary>`_Oz` 后置：窗口内的音符记录判定改写为 6（保留实时反馈链路）。</summary>
        internal static void ApplyOutcomes()
        {
            if (!BuildProfile.Supports(NativeCapability.Autoplay))
                return;
            if (!_enabled) return;
            try
            {
                if (!AutoplayScoreGuard.EnsureRunGuard())
                {
                    Enabled = false;
                    ResetCursors();
                    return;
                }
                double now = NowMs();
                if (double.IsNaN(now)) return;
                List<ChartExtState.AutoplayDue> dues = ChartExtState.AutoplayDues;
                if (dues.Count == 0) return;
                Il2Cppifapp.Game.LogicalNotePlayer player = Il2Cppifapp.Game.LogicalNotePlayer._Qe;
                if (player == null || player.Pointer == IntPtr.Zero) return;
                Il2Cpp_b._T compiled = player._ve;
                if (compiled == null || compiled.Pointer == IntPtr.Zero) return;
                var records = compiled._XC;
                if (records == null || records.Pointer == IntPtr.Zero) return;
                if (!AutoplayEngineInput.Enabled)
                {
                    bool skyHoldActive = false;
                    for (int i = 0; i < dues.Count; i++)
                    {
                        ChartExtState.AutoplayDue due = dues[i];
                        if (due.StartMs > now + 300.0) break;
                        bool sky =
                            due.Type == 5 &&
                            !_groundOnly &&
                            !SkyBlockJudgeRuntime.IsManagedMember(due.Id);
                        if (sky &&
                            now >= due.StartMs &&
                            now < due.EndMs)
                            skyHoldActive = true;
                        if (due.StartMs < now - 5000.0) continue;
                        ChangedSkyRecords.Clear();
                        bool changed = RewriteDue(
                            due, records, ChangedSkyRecords);
                        if (changed)
                            AutoplayScoreGuard.MarkUsed();
                        if (sky && changed)
                        {
                            MarkNoteConsumed(due);
                            Il2Cpp_E._VD judge =
                                Il2Cppifapp.Game.Scenes.GameScene._DN;
                            if (SkyBlockJudgeRuntime
                                    .EnsureNativePresentationQueue(judge))
                            {
                                for (int ci = 0;
                                     ci < ChangedSkyRecords.Count;
                                     ci++)
                                {
                                    var item =
                                        ChangedSkyRecords[ci];
                                    long key =
                                        item.GlobalIndex;
                                    if (!AutoplaySkyPresented.Add(key))
                                        continue;
                                    SkyBlockJudgeRuntime
                                        .EnqueuePresentation(
                                            due.Id,
                                            item.GlobalIndex,
                                            SkyPresentationPosition(
                                                due,
                                                item.Relative));
                                }
                            }
                        }
                        if (sky &&
                            changed &&
                            now >= due.StartMs &&
                            now <= due.StartMs + 200.0 &&
                            AutoplaySkyHits.Add(due.Id))
                            SkyBlockFeedbackRuntime.PlayHit();
                    }
                    SkyBlockFeedbackRuntime.SetAutoplaySkyHoldActive(
                        skyHoldActive);
                    return;
                }
                if (AutoplayEngineInput.Enabled)
                {
                    AutoplayEngineInput.ConsumeGroundEdges();
                    return;
                }

                for (int i = _cursor; i < dues.Count; i++)
                {
                    ChartExtState.AutoplayDue due = dues[i];
                    if (due.StartMs > now + 300.0) break;
                    if (due.Type == 2) continue;
                    if (due.StartMs < now - 5000.0) continue;
                    if (RewriteDue(due, records))
                        AutoplayScoreGuard.MarkUsed();
                }
                for (int i = 0; i < ActiveHolds.Count; i++)
                {
                    ChartExtState.AutoplayDue due = ActiveHolds[i];
                    if (due.StartMs > now + 300.0)
                        continue;
                    if (RewriteDue(due, records))
                        AutoplayScoreGuard.MarkUsed();
                }
            }
            catch (Exception e)
            {
                if (_errors < 20) { _errors++; ChartExtMod.Log.Error("[ChartExt] autoplay outcome failed: " + e); }
            }
        }

        private static bool RewriteDue(
            ChartExtState.AutoplayDue due,
            Il2CppSystem.Collections.Generic.Dictionary<
                long,
                Il2CppSystem.Memory<Il2Cpp_K._cH>> records,
            List<(long GlobalIndex, double Relative)> changedRecords = null)
        {
            if (SkyBlockJudgeRuntime.IsManagedMember(due.Id))
                return false;
            if (_groundOnly &&
                due.Type != 1 && due.Type != 2)
                return false;
            IntPtr basePtr = NativeNoteSlotTable.SlotBase(due.Side);
            if (basePtr == IntPtr.Zero) return false;
            IntPtr note = basePtr +
                due.Position * NativeLayoutCatalog.NoteStride;
            if (Marshal.ReadInt64(
                    note + NativeLayoutCatalog.Note.Id) != due.Id)
                return false;
            double ee = BitConverter.Int64BitsToDouble(
                Marshal.ReadInt64(
                    note + NativeLayoutCatalog.Note.RuntimeLead));
            if (!records.ContainsKey(due.Id)) return false;
            Il2CppSystem.Memory<Il2Cpp_K._cH> memory =
                records[due.Id];
            if (memory._object == null ||
                memory._object.Pointer == IntPtr.Zero)
                return false;
            int count = memory._length;
            if (count <= 0 || count > 4096) return false;
            IntPtr arrayObject = memory._object.Pointer;
            int sliceIndex = memory._index;
            int arrayLength = Marshal.ReadInt32(
                arrayObject +
                NativeLayoutCatalog.ArrayLengthOffset);
            if (sliceIndex < 0 ||
                sliceIndex + count > arrayLength)
                return false;
            bool changed = false;
            unsafe
            {
                for (int r = 0; r < count; r++)
                {
                    byte* record = InteropUtil.RecordPointer(
                        arrayObject,
                        sliceIndex,
                        r);
                    byte final =
                        *(record + InteropUtil.FinalOffset);
                    byte judgment =
                        *(record + InteropUtil.JudgmentOffset);
                    if (final != 0)
                    {
                        if (judgment != ChartExtState.JudgeShiny)
                        {
                            *(record + InteropUtil.JudgmentOffset) =
                                ChartExtState.JudgeShiny;
                            if (changedRecords != null)
                                changedRecords.Add((
                                    *(long*)record,
                                    BitConverter.Int64BitsToDouble(
                                        *(long*)(record +
                                            NativeLayoutCatalog.Record
                                                .RelativeTime))));
                            changed = true;
                        }
                    }
                    else
                    {
                        double rel =
                            BitConverter.Int64BitsToDouble(
                                *(long*)(record +
                                    NativeLayoutCatalog.Record
                                        .RelativeTime));
                        double v25 = rel + ee;
                        // 兜底窗口 0ms：到点即改写（其余逻辑保持原样）。
                        if (v25 <= 0.0 &&
                            judgment != ChartExtState.JudgeShiny)
                        {
#if IFCE_DIAGNOSTICS
                            if (_r7LateWrites++ < 20)
                            {
                                ChartExtMod.Log.Msg(
                                    "[r7-autoplay-late] " +
                                    $"id={due.Id} type={due.Type} " +
                                    $"side={due.Side} " +
                                    $"position={due.Position} " +
                                    $"start={due.StartMs} " +
                                    $"record={r} rel={rel:F1}");
                            }
#endif
                            *(record + InteropUtil.JudgmentOffset) =
                                ChartExtState.JudgeShiny;
                            changed = true;
                        }
                    }
                }
            }
            return changed;
        }

        private static UnityEngine.Vector3 SkyPresentationPosition(
            ChartExtState.AutoplayDue due,
            double relativeMs)
        {
            IntPtr basePtr =
                NativeNoteSlotTable.SlotBase(due.Side);
            if (basePtr == IntPtr.Zero)
                return default;
            IntPtr note = new IntPtr(
                basePtr.ToInt64() +
                (long)due.Position *
                NativeLayoutCatalog.NoteStride);
            float startCenter = BitConverter.Int32BitsToSingle(
                Marshal.ReadInt32(
                    note + NativeLayoutCatalog.Note.StartCenter));
            float endCenter = BitConverter.Int32BitsToSingle(
                Marshal.ReadInt32(
                    note + NativeLayoutCatalog.Note.EndCenter));
            int startMs = Marshal.ReadInt32(
                note + NativeLayoutCatalog.Note.StartMs);
            int endMs = Marshal.ReadInt32(
                note + NativeLayoutCatalog.Note.EndMs);
            double t = endMs > startMs
                ? Math.Max(
                    0.0,
                    Math.Min(
                        1.0,
                        (startMs + relativeMs - startMs) /
                        (double)(endMs - startMs)))
                : 0.0;
            float center = (float)(
                startCenter +
                (endCenter - startCenter) * t);
            Il2Cppifapp.Game.Scenes.Game.Track track =
                Il2Cppifapp.Game.Scenes.Game.Track._a;
            return track is null ||
                   track.Pointer == IntPtr.Zero
                ? default
                : track._dBA(center);
        }

        private static void MarkNoteConsumed(
            ChartExtState.AutoplayDue due)
        {
            IntPtr basePtr =
                NativeNoteSlotTable.SlotBase(due.Side);
            if (basePtr == IntPtr.Zero)
                return;
            IntPtr note = new IntPtr(
                basePtr.ToInt64() +
                (long)due.Position *
                NativeLayoutCatalog.NoteStride);
            if (Marshal.ReadInt64(
                    note + NativeLayoutCatalog.Note.Id) != due.Id)
                return;
            Marshal.WriteByte(
                note + NativeLayoutCatalog.Note.Consumed,
                1);
        }

        private static double NowMs()
        {
            Il2Cpp_E._VD judge = Il2Cppifapp.Game.Scenes.GameScene._DN;
            if (judge == null || judge.Pointer == IntPtr.Zero) return double.NaN;
            return judge._c * 1000.0;
        }
    }

}
