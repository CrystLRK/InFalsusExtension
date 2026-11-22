using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace InFalsusChartExt
{
    /// <summary>
    /// 负流速 Hold / SkyArea 的反向独立视觉。
    ///
    /// 正流速完全不进入本模块；只有“视觉跨度 W(end) &lt; W(start)”的音符
    /// 会被接管：
    /// - 判定前：从判定线近侧出现并向判定线移动；
    /// - 判定中：固定在判定线，长度按 |W(end) - W(now)| 收缩；
    /// - 结束后：立即隐藏，不越过判定线。
    /// 判定、record、计分和音效仍由原版读取 authored 时间完成。
    /// </summary>
    internal static class NegativeSpeedVisualRuntime
    {
        /// <summary>
        /// 原版 `Track._gBA` 对 SkyArea（type=5）在 authored end 之后
        /// 仍会继续渲染 2000ms；渲染改写必须覆盖同一生命周期。
        /// </summary>
        private const double SkyTailMs = 2000.0;

        /// <summary>
        /// 尾程收拢窗口的视觉长度（毫秒）。必须大于 `_KZ` 的最小网格
        /// 阈值，但远小于一帧，实际不可见。
        /// </summary>
        private const double TailLengthMs = 0.3;

        /// <summary>
        /// <c>SkySafeArea._AFA</c>（dump.cs 0x58）：`_KZ` 的网格缓存键。
        /// `_KZ` 只在缓存键或 note id 变化时重建网格/材质长度；我们每帧
        /// 改写 `_IA` 窗口长度，但不改变判定进度缓存键，若不失效该键，
        /// 条带会保持上一次缓存的长度，直到判定进度跳变——表现为
        /// “长度跳变”。`_lZ()` 用 -2.0 表示无缓存，这里只写缓存键，
        /// 不动 note id。
        /// </summary>
        private const int SkyCacheKeyOffset = 0x58;
        private const double SkyCacheInvalid = -2.0;

        internal static bool Enabled;

        private sealed class Entry
        {
            internal long Id;
            internal int Type;
            internal GameObject Go;
            internal GameObject Shadow;
            internal Transform Root;
            internal Transform ShadowRoot;
            internal Il2Cppifapp.Game.Scenes.Game.HoldNoteV2 Hold;
            internal Il2Cppifapp.Game.Scenes.Game.SkySafeArea BandArea;
            internal Il2Cppifapp.Game.Scenes.Game.SkySafeArea ShadowArea;
            internal Vector3 BaseScale = Vector3.one;
            internal readonly List<Material> OwnedMaterials =
                new List<Material>(8);
        }

        private static readonly Dictionary<long, Entry> Entries =
            new Dictionary<long, Entry>();
        private static readonly List<ChartExtState.NegativeSlot> Targets =
            new List<ChartExtState.NegativeSlot>();
        private static readonly HashSet<long> TargetKeys =
            new HashSet<long>();
        private static readonly HashSet<long> SkyTargetIds =
            new HashSet<long>();
        private static readonly List<ChartExtState.NegativeSlot> SkyTargets =
            new List<ChartExtState.NegativeSlot>();

        private sealed class SkyVisual
        {
            internal long NoteId;
            internal GameObject Go;
            internal Transform Transform;
            internal Il2Cppifapp.Game.Scenes.Game.SkySafeArea Area;
            internal Material OwnedMaterial;
            internal Material MirrorMaterial;
            internal Vector3 BaseScale = Vector3.one;
            internal bool Mirrored;
            internal bool MeshProbed;
            internal float BoundsCenterY;
            internal Vector3 BoundsSize;
        }

        private static readonly Dictionary<IntPtr, SkyVisual> SkyVisuals =
            new Dictionary<IntPtr, SkyVisual>();
        private static readonly List<IntPtr> SkyVisualPrune =
            new List<IntPtr>(8);
        private static readonly Dictionary<long, bool> SkyMirrorNow =
            new Dictionary<long, bool>();
        private static int _skyScanFrame;
        private static bool _skyScanDone;
        private static int _skyMirrorLogs;
        private static int _axisMirrorLogs;

        private sealed class IaEdit
        {
            internal IntPtr StartEntry;
            internal IntPtr EndEntry;
            internal long OriginalStartBits;
            internal long OriginalEndBits;
            internal long WrittenStartBits;
            internal long WrittenEndBits;
            internal IntPtr Note;
            internal int OriginalStartMs;
            internal int OriginalEndMs;
            internal long OriginalLeadBits;
            internal int WrittenStartMs;
            internal int WrittenEndMs;
            internal long WrittenLeadBits;
            internal bool FieldsWritten;
            internal int SlotStartMs;
            internal int SlotEndMs;
            internal int Group;
            internal bool Mirrored;
            internal int OriginalCenterNum;
            internal int OriginalCenterBits;
            internal bool CenterWritten;
        }

        private static readonly List<IaEdit> IaEdits =
            new List<IaEdit>();
        private static int _iaEditCount;
        private static bool _skyWarpLogged;
        private static int _generation = -1;
        private static IntPtr _track;
        private static int _errors;
        private static readonly HashSet<long> CreateLogged =
            new HashSet<long>();
        private static readonly Dictionary<long, bool> LastVisible =
            new Dictionary<long, bool>();
        private static int _hideLogs;
        private static int _holdDebugLogs;

        /// <summary>
        /// 渲染相：把负速 SkyArea 的编译 <c>_IA[id]</c> 改成
        /// “锚点 + 正长度”（锚点 = min(W(start), W(now))）。
        ///
        /// 原版 Track.Update 用 <c>_IA.start</c> 定位置、用
        /// <c>_IA.end - _IA.start</c> 定长度，<c>_KZ</c> 的网格沿 -Z
        /// 展开。负速时把锚点在判定前放在头部、过线后放在判定线，
        /// 长度取“锚点到尾部”的绝对值，再配合对象负 scale 做镜像，
        /// 就能复刻 Cocos 版整段精灵翻转（几何 + UV 同翻）。
        /// </summary>
        internal static void ApplySkyWarp()
        {
            if (!Enabled) return;
            EnsureChart();
            if (SkyTargets.Count == 0) return;
            try
            {
                Il2Cppifapp.Game.LogicalNotePlayer player =
                    Il2Cppifapp.Game.LogicalNotePlayer._Qe;
                if (player == null || player.Pointer == IntPtr.Zero)
                    return;
                Il2Cpp_b._T compiled = player._ve;
                if (!TimingGroupRuntime.TryResolveWarpTable(
                        compiled, out IntPtr iaBase, out int iaLen))
                    return;
                double now = TimingGroupRuntime.CurrentMs();
                if (double.IsNaN(now) || double.IsInfinity(now))
                    return;
                double wNowGlobal =
                    TimingGroupRuntime.VisualTimeMs(0, now);
                double warpLeadMs =
                    VisualSpawnTiming.NativeQuickLeadMs;
                try
                {
                    var activeTrack =
                        Il2Cppifapp.Game.Scenes.Game.Track._a;
                    if (activeTrack != null &&
                        activeTrack.Pointer != IntPtr.Zero)
                        warpLeadMs = VisualSpawnTiming.MaxLeadMs(
                            activeTrack.Pointer);
                }
                catch { }
                // 原版实际生成比 MaxLead 略早；留 1500ms 覆盖首帧。
                warpLeadMs += 1500.0;
                _iaEditCount = 0;
                int stride = NativeLayoutCatalog.Compiled.WarpEntryStride;
                int endOffset = NativeLayoutCatalog.Compiled.WarpEntryEnd;
                for (int i = 0; i < SkyTargets.Count; i++)
                {
                    ChartExtState.NegativeSlot slot = SkyTargets[i];
                    // 原版 `Track._gBA` 对 type=5 会在 end 之后继续渲染
                    // 最多 2000ms（Miss 尾程）。如果这里在 end 处停止
                    // 改写，原版会立刻用 authored `_IA` 和当前 DepthFactor
                    // 重新摆放条带端帽/光效，出现“结束后方括号残留”和
                    // 判定线闪光。尾程继续写一个收拢窗口，让原版始终按
                    // 已消耗状态渲染。
                    if (now < slot.StartMs - warpLeadMs ||
                        now >= slot.EndMs + SkyTailMs)
                        continue;
                    long id = slot.Id;
                    if (id < 0 || id >= iaLen) continue;
                    double wStart = TimingGroupRuntime.VisualTimeMs(
                        slot.Group, slot.StartMs);
                    double wEnd = TimingGroupRuntime.VisualTimeMs(
                        slot.Group, slot.EndMs);
                    double wNow = TimingGroupRuntime.VisualTimeMs(
                        slot.Group, now);
                    // 判定前锚在头部（整条 note），过线后锚在判定线
                    // （可见部分 = 判定线到尾部）。
                    // 判前位置只看 note 自身的流速：不用“note 出现前”的
                    // 全局积分决定它在判定线前还是后。
                    bool judging = now >= slot.StartMs;
                    bool tail = now >= slot.EndMs;
                    double anchor;
                    double length;
                    if (tail)
                    {
                        // 收拢到判定线：长度只保留一个远小于一帧的
                        // 窗口，端帽和网格都退化为不可见。
                        anchor = wNow - 0.5;
                        length = TailLengthMs;
                    }
                    else if (judging)
                    {
                        // `_KZ` 用 “progress + note id” 判断是否重建网格；
                        // 偏 0.5ms 让 progress 随剩余长度变化。
                        anchor = wNow - 0.5;
                        length = Math.Abs(wEnd - wNow);
                    }
                    else
                    {
                        // 判前锚点 = 当前组时间 + note 自身速度 × 剩余时间
                        // （C5B7 语义）。用 W(start) 会让 -0.5× 组在
                        // 判定前一直停在线前。
                        double own = TimingGroupRuntime.SpeedAt(
                            slot.Group, slot.StartMs);
                        double leadStart =
                            (slot.StartMs - now) * own;
                        anchor = wNow + leadStart;
                        length = Math.Abs(wEnd - wStart);
                    }
                    // timinggroup 的 ApplyWarp 已把组时间线平移到全局
                    // 视觉域：target = wNowGlobal + (V - wgNow)。
                    // 长条窗口也按同一平移写入，两个事务不再互相覆盖。
                    double targetStart =
                        wNowGlobal + (anchor - wNow);
                    if (!(length > 0.1)) continue;

                    IntPtr startEntry = new IntPtr(
                        iaBase.ToInt64() + id * stride);
                    IntPtr endEntry = new IntPtr(
                        startEntry.ToInt64() + endOffset);

                    // 网格的 Z 跨度由原生 note 的 start/end 决定（不是 _IA）；
                    // 组内 note 的这两个字段会被 TimingGroupRuntime 改写，
                    // 所以这里先算好窗口对应的原始时间，等 ApplyVisualFields
                    // 之后再覆盖，帧末一并还原。
                    IntPtr notePtr = IntPtr.Zero;
                    double startRaw = TimingGroupRuntime.GlobalTimeFor(
                        targetStart, now);
                    double endRaw = TimingGroupRuntime.GlobalTimeFor(
                        targetStart + length, now);
                    double iaStart = targetStart / 1000.0;
                    double iaEnd = (targetStart + length) / 1000.0;
                    if (!(iaEnd > iaStart + 1e-4)) continue;
                    IntPtr basePtr =
                        NativeNoteSlotTable.SlotBase(slot.Side);
                    if (basePtr != IntPtr.Zero)
                    {
                        IntPtr candidate = basePtr +
                            slot.Position * NativeLayoutCatalog.NoteStride;
                        long noteId = Marshal.ReadInt64(
                            candidate + NativeLayoutCatalog.Note.Id);
                        if (noteId == id) notePtr = candidate;
                    }

                    if (_iaEditCount == IaEdits.Count)
                        IaEdits.Add(new IaEdit());
                    IaEdit edit = IaEdits[_iaEditCount++];
                    edit.StartEntry = startEntry;
                    edit.EndEntry = endEntry;
                    edit.OriginalStartBits =
                        Marshal.ReadInt64(startEntry);
                    edit.OriginalEndBits =
                        Marshal.ReadInt64(endEntry);
                    edit.WrittenStartBits =
                        BitConverter.DoubleToInt64Bits(iaStart);
                    edit.WrittenEndBits =
                        BitConverter.DoubleToInt64Bits(iaEnd);
                    edit.Note = notePtr;
                    edit.SlotStartMs = slot.StartMs;
                    edit.SlotEndMs = slot.EndMs;
                    edit.Group = slot.Group;
                    edit.Mirrored = wEnd < anchor;
                    edit.WrittenStartMs = (int)Math.Round(startRaw);
                    edit.WrittenEndMs = (int)Math.Round(endRaw);
                    // 尾程收拢后 start/end 会被四舍五入为同一毫秒，
                    // 原版多处按 (end - start) 做除法；至少保留 1ms，
                    // 避免除以 0。
                    if (edit.WrittenEndMs <= edit.WrittenStartMs)
                        edit.WrittenEndMs = edit.WrittenStartMs + 1;
                    double writtenLead;
                    if (tail)
                        writtenLead = -(TailLengthMs + 1.0);
                    else if (!judging)
                        writtenLead =
                            (edit.WrittenEndMs -
                             edit.WrittenStartMs) + 1.0;
                    else
                        writtenLead = startRaw - now;
                    edit.WrittenLeadBits =
                        BitConverter.DoubleToInt64Bits(
                            writtenLead);
                    edit.FieldsWritten = false;
                    Marshal.WriteInt64(
                        startEntry, edit.WrittenStartBits);
                    Marshal.WriteInt64(
                        endEntry, edit.WrittenEndBits);
                    if (!_skyWarpLogged)
                    {
                        _skyWarpLogged = true;
                        ChartExtMod.Log.Msg(
                            "[neg-speed] sky warp id=" + id +
                            " now=" + now.ToString("F0") +
                            " ia=[" + iaStart.ToString("F3") + "," +
                            iaEnd.ToString("F3") + "]");
                    }
                }
            }
            catch (Exception e)
            {
                if (_errors++ < 20)
                    ChartExtMod.Log.Error(
                        "[ChartExt] negative-speed sky warp failed: " +
                        e.Message);
                RestoreSkyWarp();
            }
        }

        /// <summary>渲染结束立即还原 `_IA`，判定/结算读到的永远是原值。</summary>
        internal static void RestoreSkyWarp()
        {
            int count = _iaEditCount;
            if (count == 0) return;
            _iaEditCount = 0;
            for (int i = count - 1; i >= 0; i--)
            {
                IaEdit edit = IaEdits[i];
                try
                {
                    if (edit.FieldsWritten &&
                        edit.Note != IntPtr.Zero)
                    {
                        Marshal.WriteInt32(
                            edit.Note +
                            NativeLayoutCatalog.Note.StartMs,
                            edit.OriginalStartMs);
                        Marshal.WriteInt32(
                            edit.Note +
                            NativeLayoutCatalog.Note.EndMs,
                            edit.OriginalEndMs);
                        Marshal.WriteInt64(
                            edit.Note +
                            NativeLayoutCatalog.Note.RuntimeLead,
                            edit.OriginalLeadBits);
                        edit.FieldsWritten = false;
                    }
                    if (edit.CenterWritten &&
                        edit.Note != IntPtr.Zero)
                    {
                        Marshal.WriteInt32(
                            edit.Note +
                            NativeLayoutCatalog.Note.PackedStart,
                            edit.OriginalCenterNum);
                        Marshal.WriteInt32(
                            edit.Note +
                            NativeLayoutCatalog.Note.StartCenter,
                            edit.OriginalCenterBits);
                        edit.CenterWritten = false;
                    }
                    if (Marshal.ReadInt64(edit.StartEntry) ==
                        edit.WrittenStartBits)
                        Marshal.WriteInt64(
                            edit.StartEntry, edit.OriginalStartBits);
                    if (Marshal.ReadInt64(edit.EndEntry) ==
                        edit.WrittenEndBits)
                        Marshal.WriteInt64(
                            edit.EndEntry, edit.OriginalEndBits);
                }
                catch { }
            }
        }

        /// <summary>
        /// 原生 SkyArea 网格的 Z 跨度取自 note 的 start/end；组内 note 的
        /// 这两个字段会被 TimingGroupRuntime 先写成 warp 值。这里在其后
        /// 把目标窗口的原始时间覆盖上去，帧末由 RestoreSkyWarp 还原。
        /// </summary>
        internal static void ApplyVisualFields()
        {
            if (!Enabled || _iaEditCount == 0) return;
            for (int i = 0; i < _iaEditCount; i++)
            {
                IaEdit edit = IaEdits[i];
                if (edit.Note == IntPtr.Zero || edit.FieldsWritten)
                    continue;
                try
                {
                    edit.OriginalStartMs = Marshal.ReadInt32(
                        edit.Note +
                        NativeLayoutCatalog.Note.StartMs);
                    edit.OriginalEndMs = Marshal.ReadInt32(
                        edit.Note +
                        NativeLayoutCatalog.Note.EndMs);
                    edit.OriginalLeadBits = Marshal.ReadInt64(
                        edit.Note +
                        NativeLayoutCatalog.Note.RuntimeLead);
                    edit.FieldsWritten = true;
                    Marshal.WriteInt32(
                        edit.Note +
                        NativeLayoutCatalog.Note.StartMs,
                        edit.WrittenStartMs);
                    Marshal.WriteInt32(
                        edit.Note +
                        NativeLayoutCatalog.Note.EndMs,
                        edit.WrittenEndMs);
                    Marshal.WriteInt64(
                        edit.Note +
                        NativeLayoutCatalog.Note.RuntimeLead,
                        edit.WrittenLeadBits);

                    // 网格的起点 X 跟随判定线处的弧线位置：只保留
                    // “当前 → 尾部”的剩余弧，避免把整条 X 路径压进短长度。
                    double span = edit.SlotEndMs - edit.SlotStartMs;
                    double nowMs = TimingGroupRuntime.CurrentMs();
                    if (span > 1.0 &&
                        !double.IsNaN(nowMs) &&
                        !double.IsInfinity(nowMs))
                    {
                        double progress =
                            (nowMs - edit.SlotStartMs) / span;
                        if (progress > 1e-6)
                        {
                            progress = Math.Min(1.0, progress);
                            float x1 = ReadFloat(
                                edit.Note +
                                NativeLayoutCatalog.Note.StartCenter);
                            float x2 = ReadFloat(
                                edit.Note +
                                NativeLayoutCatalog.Note.EndCenter);
                            float x = (float)(x1 +
                                (x2 - x1) * progress);
                            int den = Marshal.ReadInt32(
                                edit.Note +
                                NativeLayoutCatalog.Note.PackedStart + 4);
                            if (den <= 0) den = 1000;
                            edit.OriginalCenterNum = Marshal.ReadInt32(
                                edit.Note +
                                NativeLayoutCatalog.Note.PackedStart);
                            edit.OriginalCenterBits = Marshal.ReadInt32(
                                edit.Note +
                                NativeLayoutCatalog.Note.StartCenter);
                            edit.CenterWritten = true;
                            Marshal.WriteInt32(
                                edit.Note +
                                NativeLayoutCatalog.Note.PackedStart,
                                (int)Math.Round(x * den));
                            Marshal.WriteInt32(
                                edit.Note +
                                NativeLayoutCatalog.Note.StartCenter,
                                BitConverter.SingleToInt32Bits(x));
                        }
                    }
                }
                catch { }
            }
        }

        internal static void Tick(
            Il2Cppifapp.Game.Scenes.Game.Track track)
        {
            if (track == null || track.Pointer == IntPtr.Zero)
                return;
            if (!Enabled)
            {
                if (Entries.Count > 0) DestroyAll();
                RestoreAllSkyMirrors();
                return;
            }
            try
            {
                EnsureChart();
                TickSkyMirror(track);
                if (Targets.Count == 0) return;
                if (_track != track.Pointer)
                {
                    _track = track.Pointer;
                    DestroyAll();
                }
                if (!VisualTrackFrame.TryCapture(
                        track, out VisualTrackFrame frame) ||
                    !frame.IsValid)
                    return;

                double maxLead = frame.MaxLeadMs;
                for (int i = 0; i < Targets.Count; i++)
                {
                    ChartExtState.NegativeSlot slot = Targets[i];
                    IntPtr basePtr =
                        NativeNoteSlotTable.SlotBase(slot.Side);
                    if (basePtr == IntPtr.Zero) continue;
                    IntPtr note = basePtr +
                        slot.Position * NativeLayoutCatalog.NoteStride;
                    long id = Marshal.ReadInt64(
                        note + NativeLayoutCatalog.Note.Id);
                    if (id != slot.Id) continue;

                    // 使用编译期锁存的 authored 起止；避免运行时结构体字段
                    // 被其它模块改写后影响可见性与收缩进度。
                    int startMs = slot.StartMs;
                    int endMs = slot.EndMs;
                    double now = frame.NowMs;
                    double leadStart =
                        TimingGroupRuntime.VisualTimeMs(slot.Group, startMs) -
                        TimingGroupRuntime.VisualTimeMs(slot.Group, now);
                    double leadEnd =
                        TimingGroupRuntime.VisualTimeMs(slot.Group, endMs) -
                        TimingGroupRuntime.VisualTimeMs(slot.Group, now);
                    bool consumed = Marshal.ReadByte(
                        note + NativeLayoutCatalog.Note.Consumed) != 0;
                    bool judging = now >= startMs - 0.5;
                    // 判定前的头部位置必须用组内 W(t) 积分（真实累计距离），
                    // 不能用「note 自身流速 × 剩余时间」：note 起点处若恰好
                    // 有 1ms 极值 timing，会把整段接近过程按该极值渲染。
                    bool approaching =
                        now < startMs - 0.5 &&
                        (startMs - now) <= maxLead + 1.0 &&
                        Math.Abs(leadStart) <= maxLead;
                    bool visible = !consumed &&
                        now < endMs - 0.5 &&
                        (judging || approaching);
                    if (!visible)
                    {
                        if (Entries.TryGetValue(id, out Entry hidden))
                            SetActive(hidden, false);
                        if (LastVisible.TryGetValue(id, out bool wasVisible) &&
                            wasVisible && _hideLogs < 20)
                        {
                            _hideLogs++;
                            ChartExtMod.Log.Msg(
                                "[neg-speed] hide type=" + slot.Type +
                                " id=" + id + " now=" + now.ToString("F0") +
                                " start=" + startMs + " end=" + endMs);
                        }
                        LastVisible[id] = false;
                        continue;
                    }
                    LastVisible[id] = true;

                    if (!Entries.TryGetValue(id, out Entry entry))
                    {
                        entry = CreateEntry(track, slot, note);
                        if (entry == null) continue;
                        Entries[id] = entry;
                        if (CreateLogged.Add(id))
                            ChartExtMod.Log.Msg(
                                "[neg-speed] create type=" + slot.Type +
                                " id=" + id + " start=" + startMs +
                                " end=" + endMs);
                    }
                    UpdateEntry(
                        track, entry, note, slot.Group, in frame,
                        now, startMs, endMs, leadStart, leadEnd);
                    SetActive(entry, true);
                }
            }
            catch (Exception e)
            {
                if (_errors++ < 20)
                    ChartExtMod.Log.Error(
                        "[ChartExt] negative-speed visual tick failed: " +
                        e.Message);
            }
        }

        /// <summary>
        /// 复刻 Cocos 版“整段精灵负 scale”的镜像：网格仍按正长度构建，
        /// 渲染前对目标 SkyArea 对象做 Z 轴负 scale，使几何与 UV（含尾部
        /// 光纹）一起翻到玩家侧。对象在池中复用时立即还原，不影响其它
        /// SkyArea。
        /// </summary>
        private static void TickSkyMirror(
            Il2Cppifapp.Game.Scenes.Game.Track track)
        {
            bool invalidateGroupSkies =
                Enabled && TimingGroupRuntime.Active;
            if (!Enabled ||
                (SkyTargetIds.Count == 0 && !invalidateGroupSkies))
            {
                RestoreAllSkyMirrors();
                return;
            }
            double now = TimingGroupRuntime.CurrentMs();
            if (double.IsNaN(now) || double.IsInfinity(now)) return;
            double mirrorLeadMs =
                VisualSpawnTiming.NativeQuickLeadMs;
            try
            {
                if (track != null && track.Pointer != IntPtr.Zero)
                    mirrorLeadMs = VisualSpawnTiming.MaxLeadMs(
                        track.Pointer);
            }
            catch { }
            mirrorLeadMs += 1500.0;
            SkyMirrorNow.Clear();
            for (int i = 0; i < SkyTargets.Count; i++)
            {
                var slot = SkyTargets[i];
                if (now >= slot.StartMs - mirrorLeadMs &&
                    now < slot.EndMs + SkyTailMs)
                {
                    double wEnd = TimingGroupRuntime.VisualTimeMs(
                        slot.Group, slot.EndMs);
                    double anchor = now < slot.StartMs
                        ? TimingGroupRuntime.VisualTimeMs(
                            slot.Group, slot.StartMs)
                        : TimingGroupRuntime.VisualTimeMs(
                            slot.Group, now);
                    SkyMirrorNow[slot.Id] = wEnd < anchor;
                }
            }
            // timinggroup 谱面里，普通（非 complex）SkyArea 的 `_IA`
            // 也逐帧变化；同样需要让 `_KZ` 重建，否则会先按 authored
            // 长度画到判定开始才跳变成组速长度。
            if (SkyMirrorNow.Count == 0 && !invalidateGroupSkies)
            {
                RestoreAllSkyMirrors();
                return;
            }
            try
            {
                Transform root = track.notesContainer;
                if (root == null || root.Pointer == IntPtr.Zero) return;
                _skyScanFrame++;
                // 池对象在谱面加载时就已存在，但 note id 会随复用变化。
                // 首次进入负速窗口时立即扫描一次，之后只做低频兜底；
                // 这样目标第一次出现的那一帧就能拿到镜像，不会先以
                // 未镜像形态在远侧画几帧再瞬移/翻面。
                if (!_skyScanDone ||
                    _skyScanFrame % 90 == 0)
                {
                    ScanSkyVisuals(root);
                    _skyScanDone = true;
                }

                SkyVisualPrune.Clear();
                foreach (var pair in SkyVisuals)
                {
                    SkyVisual visual = pair.Value;
                    if (visual.Go == null ||
                        visual.Go.Pointer == IntPtr.Zero ||
                        visual.Area == null ||
                        visual.Area.Pointer == IntPtr.Zero)
                    {
                        SkyVisualPrune.Add(pair.Key);
                        continue;
                    }
                    long id = Marshal.ReadInt64(
                        visual.Area.Pointer +
                        NativeLayoutCatalog.SkySafeArea.NativeNoteId);
                    if (id != visual.NoteId)
                    {
                        // 池对象已分配给别的 note：先还原，再更新归属。
                        // 本帧不镜像，给 `_KZ` 一帧时间重建网格；否则
                        // 池里的上一份顶点会在谱面开头闪一下。
                        RestoreMirror(visual);
                        visual.NoteId = id;
                        Marshal.WriteInt64(
                            visual.Area.Pointer +
                            SkyCacheKeyOffset,
                            BitConverter.DoubleToInt64Bits(
                                SkyCacheInvalid));
                        continue;
                    }
                    if (SkyMirrorNow.TryGetValue(id, out bool mirror))
                    {
                        if (mirror) ApplyMirror(visual);
                        else RestoreMirror(visual);
                        // 强制下一帧 `_KZ` 重建：长度是按帧变化的窗口，
                        // 不能复用上一帧的网格/材质长度缓存。
                        Marshal.WriteInt64(
                            visual.Area.Pointer +
                            SkyCacheKeyOffset,
                            BitConverter.DoubleToInt64Bits(
                                SkyCacheInvalid));
                    }
                    else
                        RestoreMirror(visual);
                    if (invalidateGroupSkies && id >= 0)
                    {
                        bool active = false;
                        try { active = visual.Go.activeSelf; }
                        catch { }
                        if (active)
                            Marshal.WriteInt64(
                                visual.Area.Pointer +
                                SkyCacheKeyOffset,
                                BitConverter.DoubleToInt64Bits(
                                    SkyCacheInvalid));
                    }
                }
                for (int i = 0; i < SkyVisualPrune.Count; i++)
                    SkyVisuals.Remove(SkyVisualPrune[i]);
                // 入场括号的距离裁剪会误伤近处括号，已移除；
                // 远处括号属于原版生成窗口行为。
            }
            catch (Exception e)
            {
                if (_errors++ < 20)
                    ChartExtMod.Log.Error(
                        "[ChartExt] negative-speed sky mirror failed: " +
                        e.Message);
            }
        }

        private static void ScanSkyVisuals(Transform root)
        {
            var areas = root.GetComponentsInChildren<
                Il2Cppifapp.Game.Scenes.Game.SkySafeArea>(true);
            for (int i = 0; areas != null && i < areas.Length; i++)
            {
                var area = areas[i];
                if (area == null || area.Pointer == IntPtr.Zero)
                    continue;
                GameObject go = area.gameObject;
                if (go == null || go.Pointer == IntPtr.Zero)
                    continue;
                string name = null;
                try { name = go.name; } catch { }
                if (!string.IsNullOrEmpty(name) &&
                    name.StartsWith("IFCE_", StringComparison.Ordinal))
                    continue;
                long id = Marshal.ReadInt64(
                    area.Pointer +
                    NativeLayoutCatalog.SkySafeArea.NativeNoteId);
                // 注册全部 SkySafeArea（含池中未激活项）：id 随池复用变化，
                // 等到 id 命中目标再查询缓存会晚一帧。
                if (SkyVisuals.ContainsKey(area.Pointer))
                    continue;
                var renderer = area.meshRenderer;
                var visual = new SkyVisual
                {
                    // 首帧先当作“id 变化”，跳过镜像，给 `_KZ` 一帧时间
                    // 用当前 note 重建网格；否则池里上一份网格会在谱面
                    // 开头闪一下。
                    NoteId = long.MinValue,
                    Go = go,
                    Transform = go.transform,
                    Area = area,
                };
                try
                {
                    if (visual.Transform != null)
                        visual.BaseScale = visual.Transform.localScale;
                    Material shared = renderer == null ||
                        renderer.Pointer == IntPtr.Zero
                        ? null
                        : renderer.sharedMaterial;
                    if (shared != null &&
                        shared.Pointer != IntPtr.Zero &&
                        shared.HasProperty("_Cull"))
                    {
                        visual.OwnedMaterial = renderer!.material;
                        visual.OwnedMaterial.SetInt("_Cull", 0);
                    }
                }
                catch { }
                ProbeMesh(visual);
                SkyVisuals[area.Pointer] = visual;
                if (_skyMirrorLogs++ < 12)
                    ChartExtMod.Log.Msg(
                        "[neg-speed] sky mirror target id=" + id +
                        " centerY=" + visual.BoundsCenterY.ToString("F3") +
                        " size=(" + visual.BoundsSize.x.ToString("F2") +
                        "," + visual.BoundsSize.y.ToString("F2") +
                        "," + visual.BoundsSize.z.ToString("F2") + ")");
            }
        }

        private static void ApplyMirror(SkyVisual visual)
        {
            Transform tr = visual.Transform;
            if (tr == null || tr.Pointer == IntPtr.Zero) return;
            // 轴对称：transform 不翻（保持 X 宽度/中心与运动方向），
            // 只把 `_KZ` 生成的网格沿长度轴（局部 Y）取负，并反转
            // 三角形索引维持绕序；同时把 shader 的物体空间裁剪窗口
            // 映射到 p -> -p。
            tr.localScale = visual.BaseScale;
            MirrorMesh(visual);
            MirrorMaterial(visual);
            visual.Mirrored = true;
        }

        private static void RestoreMirror(SkyVisual visual)
        {
            if (!visual.Mirrored) return;
            visual.Mirrored = false;
            Transform tr = visual.Transform;
            if (tr == null || tr.Pointer == IntPtr.Zero) return;
            tr.localScale = visual.BaseScale;
            // `_KZ` 每帧重写材质参数、并在缓存失效时重建网格；
            // 目标窗口内我们每帧都失效缓存，因此不需要手工反向。
            // 离开窗口的池对象在下次复用（note id 变化）时也会重建。
        }

        /// <summary>
        /// 沿局部 Y（长度轴）镜像 `_KZ` 生成的网格，并交换每个三角形
        /// 的后两个索引以保持正面绕序。`reverse=false` 时执行镜像，
        /// `reverse=true` 时抵消镜像。
        /// </summary>
        private static void MirrorMesh(
            SkyVisual visual, bool reverse = false)
        {
            try
            {
                MeshFilter filter = visual.Area?.meshFilter;
                if (filter == null && visual.Go != null &&
                    visual.Go.Pointer != IntPtr.Zero)
                {
                    try { filter = visual.Go.GetComponent<MeshFilter>(); }
                    catch { }
                }
                Mesh mesh = filter == null ? null : filter.sharedMesh;
                if (mesh == null || mesh.Pointer == IntPtr.Zero) return;
                Vector3[] vertices = mesh.vertices;
                if (vertices == null || vertices.Length == 0) return;
                bool mirror = !reverse;
                for (int i = 0; i < vertices.Length; i++)
                {
                    Vector3 v = vertices[i];
                    if (mirror) v.y = -v.y;
                    vertices[i] = v;
                }
                mesh.vertices = vertices;
                int[] triangles = mesh.triangles;
                if (triangles != null)
                {
                    for (int i = 0; i + 2 < triangles.Length; i += 3)
                    {
                        int swap = triangles[i + 1];
                        triangles[i + 1] = triangles[i + 2];
                        triangles[i + 2] = swap;
                    }
                    mesh.triangles = triangles;
                }
                mesh.RecalculateBounds();
                if (!reverse && _axisMirrorLogs < 6)
                {
                    _axisMirrorLogs++;
                    Bounds bounds = mesh.bounds;
                    ChartExtMod.Log.Msg(
                        "[neg-speed] axis mirror id=" + visual.NoteId +
                        " verts=" + vertices.Length +
                        " bounds=(" +
                        bounds.center.x.ToString("F2") + "," +
                        bounds.center.y.ToString("F2") + "," +
                        bounds.center.z.ToString("F2") + ") size=(" +
                        bounds.size.x.ToString("F2") + "," +
                        bounds.size.y.ToString("F2") + "," +
                        bounds.size.z.ToString("F2") + ")");
                }
            }
            catch { }
        }

        private static void MirrorMaterial(
            SkyVisual visual, bool reverse = false)
        {
            try
            {
                if (visual.MirrorMaterial == null ||
                    visual.MirrorMaterial.Pointer == IntPtr.Zero)
                {
                    var renderer = visual.Area?.meshRenderer;
                    if (renderer == null && visual.Go != null &&
                        visual.Go.Pointer != IntPtr.Zero)
                    {
                        try
                        {
                            renderer = visual.Go.GetComponent<MeshRenderer>();
                        }
                        catch { }
                    }
                    if (renderer == null ||
                        renderer.Pointer == IntPtr.Zero)
                        return;
                    visual.MirrorMaterial = renderer.material;
                }
                Material material = visual.MirrorMaterial;
                if (material == null ||
                    material.Pointer == IntPtr.Zero)
                    return;
                if (!material.HasProperty("_VisualStart") ||
                    !material.HasProperty("_VisualLength"))
                    return;
                float start = material.GetFloat("_VisualStart");
                float length = material.GetFloat("_VisualLength");
                if (float.IsNaN(start) ||
                    float.IsNaN(length) ||
                    float.IsInfinity(start) ||
                    float.IsInfinity(length))
                    return;
                // 顶点 y -> -y 后，物体空间位置 p 变为 -p；可见区间
                // [start, start+length] 映射为 [-(start+length), -start]。
                float target = reverse ? start : -(start + length);
                material.SetFloat("_VisualStart", target);
                if (!reverse && _axisMirrorLogs < 6)
                {
                    ChartExtMod.Log.Msg(
                        "[neg-speed] axis material id=" + visual.NoteId +
                        " start=" + start.ToString("F3") +
                        " len=" + length.ToString("F3") +
                        " trackLen=" +
                        material.GetFloat("_TrackVisualLength")
                            .ToString("F3") +
                        " progress=" +
                        material.GetFloat("_VisualChartProgress")
                            .ToString("F3"));
                }
            }
            catch { }
        }

        private static void RestoreAllSkyMirrors()
        {
            foreach (var pair in SkyVisuals)
                RestoreMirror(pair.Value);
        }

        /// <summary>
        /// 镜像会反转三角形绕序。目标 shader 没有 <c>_Cull</c> 属性时，
        /// 若网格在局部 Y 轴上是“贴平且居中”的，就同时翻转 Y 轴：
        /// 等价于绕 X 轴 180°（行列式 +1），几何不变但绕序保持，
        /// 避免背面剔除把整条隐藏。
        /// </summary>
        private static void ProbeMesh(SkyVisual visual)
        {
            try
            {
                MeshFilter filter = visual.Area?.meshFilter;
                if (filter == null && visual.Go != null &&
                    visual.Go.Pointer != IntPtr.Zero)
                {
                    try { filter = visual.Go.GetComponent<MeshFilter>(); }
                    catch { }
                }
                Mesh mesh = filter == null
                    ? null
                    : filter.sharedMesh;
                if (mesh == null || mesh.Pointer == IntPtr.Zero)
                    return;
                Bounds bounds = mesh.bounds;
                if (bounds.size.sqrMagnitude < 1e-6f)
                    return;
                visual.MeshProbed = true;
                visual.BoundsCenterY = bounds.center.y;
                visual.BoundsSize = bounds.size;
            }
            catch { }
        }

        private static void UpdateEntry(
            Il2Cppifapp.Game.Scenes.Game.Track track,
            Entry entry, IntPtr notePtr, int group,
            in VisualTrackFrame frame,
            double now, int startMs, int endMs,
            double leadStart, double leadEnd)
        {
            int lane = Marshal.ReadInt32(
                notePtr + NativeLayoutCatalog.Note.Lane);
            int width = Marshal.ReadInt32(
                notePtr + NativeLayoutCatalog.Note.PackedStartWidth);
            double zEnd = frame.Distance(leadEnd);
            // W(end)-W(now)：判定前是整条，判定后是“判定线 → 尾部”的
            // 剩余量。取较低的一端作根，`_cZ` 沿 +Z 建条，天然得到
            // 正/负两个方向的镜像（负方向时贴图从尾部开始，等价翻转）。
            bool judging = now >= startMs - 0.5;
            double lowLead;
            double highLead;
            if (judging)
            {
                lowLead = Math.Min(0.0, leadEnd);
                highLead = Math.Max(0.0, leadEnd);
            }
            else
            {
                // 判定前：头部位置 = W(start) - W(now)，长度 =
                // W(end) - W(start)。零速、负速与多段变速都走同一
                // 公式：接近过程始终按该组真实流速累计，不会被
                // note 起点处 1ms 的极值 timing 带偏。
                lowLead = Math.Min(leadStart, leadEnd);
                highLead = Math.Max(leadStart, leadEnd);
            }
            double zLow = frame.Distance(lowLead);
            double zHigh = frame.Distance(highLead);
            float length = (float)Math.Max(0.0, zHigh - zLow);

            // 临时只读诊断：hold 在 timing group 内的接近/判定几何。
            if (entry.Type == 2 && group > 0 &&
                _holdDebugLogs < 240)
            {
                _holdDebugLogs++;
                if (_holdDebugLogs == 1)
                {
                    var dump = new System.Text.StringBuilder(
                        "[hold-debug] gspeeds:");
                    for (int gi = 0;
                         gi < ChartExtState.GroupSpeeds.Count && gi < 40;
                         gi++)
                    {
                        ChartExtState.GroupSpeed it =
                            ChartExtState.GroupSpeeds[gi];
                        if (it.Group != group) continue;
                        dump.Append(" (").Append(it.TimeMs)
                            .Append(',').Append(it.Mult.ToString("F5"))
                            .Append(')');
                    }
                    ChartExtMod.Log.Msg(dump.ToString());
                    ChartExtMod.Log.Msg(
                        "[hold-debug] W(0)=" +
                        TimingGroupRuntime.VisualTimeMs(group, 0.0)
                            .ToString("F3") +
                        " W(start)=" +
                        TimingGroupRuntime.VisualTimeMs(group, startMs)
                            .ToString("F3") +
                        " W(end)=" +
                        TimingGroupRuntime.VisualTimeMs(group, endMs)
                            .ToString("F3"));
                }
                ChartExtMod.Log.Msg(
                    "[hold-debug] g=" + group +
                    " id=" + entry.Id +
                    " now=" + now.ToString("F1") +
                    " start=" + startMs +
                    " end=" + endMs +
                    " leadS=" + leadStart.ToString("F3") +
                    " leadE=" + leadEnd.ToString("F3") +
                    " own=" +
                    TimingGroupRuntime.SpeedAt(group, startMs)
                        .ToString("F5") +
                    " low=" + lowLead.ToString("F3") +
                    " high=" + highLead.ToString("F3") +
                    " len=" + length.ToString("F4") +
                    " zLow=" + zLow.ToString("F3"));
            }

            if (entry.Type == 5)
            {
                UpdateSky(
                    track, entry, notePtr,
                    in frame, zEnd, leadEnd,
                    length, now);
            }
            else
            {
                bool active = IsHoldActive(
                    entry.Id, startMs, now);
                UpdateHold(
                    entry, frame, lane, width, length,
                    zLow, now, active);
            }
        }

        /// <summary>
        /// 读取该 Hold 最近一条已到期记录的判定：命中（>= Near）返回 true，
        /// 未判定 / Miss 返回 false。用于让克隆的材质状态与原版判定一致。
        /// </summary>
        private static unsafe bool IsHoldActive(
            long id, int startMs, double now)
        {
            try
            {
                Il2Cppifapp.Game.LogicalNotePlayer player =
                    Il2Cppifapp.Game.LogicalNotePlayer._Qe;
                Il2Cpp_b._T compiled = player?._ve;
                if (compiled == null ||
                    compiled.Pointer == IntPtr.Zero)
                    return false;
                var records = compiled._XC;
                if (records == null ||
                    records.Pointer == IntPtr.Zero ||
                    !records.ContainsKey(id))
                    return false;
                Il2CppSystem.Memory<Il2Cpp_K._cH> memory =
                    records[id];
                if (memory._object == null ||
                    memory._object.Pointer == IntPtr.Zero)
                    return false;
                int count = memory._length;
                if (count <= 0 || count > 100000)
                    return false;
                IntPtr arrayObject = memory._object.Pointer;
                int slice = memory._index;
                int arrayLength = Marshal.ReadInt32(
                    arrayObject +
                    NativeLayoutCatalog.ArrayLengthOffset);
                if (slice < 0 || slice + count > arrayLength)
                    return false;
                double best = double.NegativeInfinity;
                byte latest = ChartExtState.JudgeNone;
                for (int r = 0; r < count; r++)
                {
                    byte* record = InteropUtil.RecordPointer(
                        arrayObject, slice, r);
                    double relative =
                        BitConverter.Int64BitsToDouble(
                            *(long*)(record +
                                NativeLayoutCatalog.Record
                                    .RelativeTime));
                    if (double.IsNaN(relative) ||
                        double.IsInfinity(relative))
                        continue;
                    if (startMs + relative > now ||
                        relative <= best)
                        continue;
                    best = relative;
                    latest = *(record +
                        NativeLayoutCatalog.Record.Judgment);
                }
                return latest >= ChartExtState.JudgeNearEarly;
            }
            catch
            {
                return false;
            }
        }

        private static void UpdateHold(
            Entry entry, in VisualTrackFrame frame,
            int lane, int width, float length,
            double zBase, double now, bool active)
        {
            Transform tr = entry.Root;
            if (tr == null || entry.Hold == null) return;
            if (lane >= 1 && lane <= 4)
            {
                tr.localPosition = new Vector3(
                    frame.LaneX(lane, width), 0f, (float)zBase);
                tr.localRotation = frame.HoldRotation;
                tr.localScale = entry.BaseScale;
            }
            else if (lane == 0)
            {
                tr.localPosition = new Vector3(
                    frame.SideLeftPosition.x,
                    frame.SideLeftPosition.y,
                    frame.SideLeftPosition.z + (float)zBase);
                tr.localRotation = frame.SideLeftRotation;
                tr.localScale = entry.BaseScale;
            }
            else
            {
                tr.localPosition = new Vector3(
                    frame.SideRightPosition.x,
                    frame.SideRightPosition.y,
                    frame.SideRightPosition.z + (float)zBase);
                tr.localRotation = frame.SideRightRotation;
                tr.localScale = entry.BaseScale;
            }
            entry.Hold._cZ(
                length,
                (float)frame.DepthFactor,
                0f,
                active,
                width,
                false,
                now);
        }

        private static unsafe void UpdateSky(
            Il2Cppifapp.Game.Scenes.Game.Track track,
            Entry entry, IntPtr notePtr,
            in VisualTrackFrame frame,
            double zEnd, double leadEnd,
            float length, double now)
        {
            if (entry.BandArea == null || entry.Root == null) return;
            Il2Cpp_b._fA note =
                System.Runtime.CompilerServices.Unsafe
                    .ReadUnaligned<Il2Cpp_b._fA>((void*)notePtr);
            float bandScale = frame.TrackWidth;
            float shadowScale = BitConverter.Int32BitsToSingle(
                Marshal.ReadInt32(
                    track.Pointer +
                    NativeLayoutCatalog.Track.NoteWidthScale));
            double trackLength = BitConverter.Int64BitsToDouble(
                Marshal.ReadInt64(
                    track.Pointer +
                    NativeLayoutCatalog.Track.BaseSpeed));
            double speed = BitConverter.Int64BitsToDouble(
                Marshal.ReadInt64(
                    track.Pointer +
                    NativeLayoutCatalog.Track.DepthFactor));
            double trackScale = trackLength / 3.333333334;
            float bandZScale = (float)Math.Abs(trackScale * speed);
            double visualLength = length;
            float chartSeconds = (float)(now / 1000.0);
            double noteStartSeconds =
                chartSeconds + leadEnd / 1000.0;
            Vector2 zero = Vector2.zero;
            entry.BandArea._KZ(
                ref note,
                bandScale,
                chartSeconds,
                noteStartSeconds,
                visualLength,
                bandZScale,
                0f,
                0.0,
                zero,
                zero);
            Vector3 rest = frame.RestPosition;
            entry.Root.localPosition = new Vector3(
                rest.x + (-bandScale * 0.5f +
                          bandScale * note._Ce._LD),
                rest.y,
                rest.z + (float)zEnd);
            if (entry.ShadowArea != null && entry.ShadowRoot != null)
            {
                entry.ShadowArea._KZ(
                    ref note,
                    shadowScale,
                    chartSeconds,
                    noteStartSeconds,
                    visualLength,
                    (float)trackScale,
                    0f,
                    0.0,
                    zero,
                    zero);
                Vector3 home = ReadVector3(
                    track.Pointer +
                    NativeLayoutCatalog.Track.HomePosition);
                entry.ShadowRoot.localPosition = new Vector3(
                    home.x + (-shadowScale * 0.5f +
                              shadowScale * note._Ce._LD),
                    home.y,
                    home.z + (float)zEnd);
            }
        }

        private static Entry CreateEntry(
            Il2Cppifapp.Game.Scenes.Game.Track track,
            ChartExtState.NegativeSlot slot, IntPtr notePtr)
        {
            Entry entry = null;
            try
            {
                int lane = Marshal.ReadInt32(
                    notePtr + NativeLayoutCatalog.Note.Lane);
                entry = new Entry { Id = slot.Id, Type = slot.Type };
                if (slot.Type == 5)
                {
                    entry.Go = CreateArea(
                        track, track.safeAreaPrefab,
                        "IFCE_NegSky_" + slot.Id,
                        entry.OwnedMaterials);
                    entry.Shadow = CreateArea(
                        track, track.safeAreaShadowPrefab,
                        "IFCE_NegSkyShadow_" + slot.Id,
                        entry.OwnedMaterials);
                    if (entry.Go == null)
                    {
                        if (CreateLogged.Add(slot.Id))
                            ChartExtMod.Log.Warning(
                                "[neg-speed] sky create skipped id=" +
                                slot.Id + " (safeAreaPrefab unavailable)");
                        DestroyEntry(entry);
                        return null;
                    }
                    entry.Root = entry.Go.transform;
                    entry.ShadowRoot = entry.Shadow?.transform;
                    entry.BandArea = entry.Go.GetComponent<
                        Il2Cppifapp.Game.Scenes.Game.SkySafeArea>();
                    entry.ShadowArea = entry.Shadow?.GetComponent<
                        Il2Cppifapp.Game.Scenes.Game.SkySafeArea>();
                    if (entry.BandArea != null)
                        entry.BandArea.colorDetails =
                            track.safeAreaColorDetails;
                    SetActive(entry, false);
                    return entry;
                }

                GameObject prefab = lane == 0
                    ? track.sideHoldNotePrefab
                    : lane == 5
                        ? track.sideHoldNoteAltPrefab
                        : track.holdNotePrefab;
                if (prefab == null || prefab.Pointer == IntPtr.Zero)
                { DestroyEntry(entry); return null; }
                entry.Go = UnityEngine.Object.Instantiate(prefab);
                entry.Go.name = "IFCE_NegHold_" + slot.Id;
                entry.Root = entry.Go.transform;
                Transform parent = track.notesContainer;
                if (parent != null && parent.Pointer != IntPtr.Zero)
                    entry.Root.SetParent(parent, false);
                entry.BaseScale = entry.Root.localScale;
                VisualNoteObjectUtil.CloneMaterials(
                    entry.Go, entry.OwnedMaterials);
                entry.Hold = entry.Go.GetComponent<
                    Il2Cppifapp.Game.Scenes.Game.HoldNoteV2>();
                if (entry.Hold == null) { DestroyEntry(entry); return null; }
                ApplyHoldGuide(entry.Go, entry.Root, entry.Hold);
                SetActive(entry, false);
                return entry;
            }
            catch (Exception e)
            {
                DestroyEntry(entry);
                if (_errors++ < 20)
                    ChartExtMod.Log.Error(
                        "[ChartExt] negative-speed visual create failed: " +
                        e.Message);
                return null;
            }
        }

        private static Il2Cppifapp.Game.Scenes.Game.Track.__c
            _holdGuideCallback;

        private static void ApplyHoldGuide(
            GameObject go, Transform root,
            Il2Cppifapp.Game.Scenes.Game.HoldNoteV2 note)
        {
            try
            {
                _holdGuideCallback ??= new
                    Il2Cppifapp.Game.Scenes.Game.Track.__c();
                var tuple = new Il2CppSystem.ValueTuple<
                    GameObject, Transform,
                    Il2Cppifapp.Game.Scenes.Game.HoldNoteV2>(
                        go, root, note);
                _holdGuideCallback._MBA(tuple);
            }
            catch { }
        }

        private static GameObject CreateArea(
            Il2Cppifapp.Game.Scenes.Game.Track track,
            GameObject prefab, string name,
            List<Material> owned)
        {
            if (prefab == null || prefab.Pointer == IntPtr.Zero)
                return null;
            GameObject go = null;
            try
            {
                go = UnityEngine.Object.Instantiate(prefab);
                go.name = name;
                Transform parent = track.notesContainer;
                if (parent != null && parent.Pointer != IntPtr.Zero)
                    go.transform.SetParent(parent, false);
                var area = go.GetComponent<
                    Il2Cppifapp.Game.Scenes.Game.SkySafeArea>();
                var filter = go.GetComponent<MeshFilter>();
                var renderer = go.GetComponent<MeshRenderer>();
                if (area != null)
                {
                    if (filter != null) area.meshFilter = filter;
                    if (renderer != null) area.meshRenderer = renderer;
                }
                if (renderer != null)
                {
                    VisualNoteObjectUtil.CloneMaterials(go, owned);
                    renderer.forceRenderingOff = false;
                    renderer.enabled = true;
                }
                go.SetActive(false);
                return go;
            }
            catch
            {
                TryDestroy(go);
                throw;
            }
        }

        private static void SetActive(Entry entry, bool active)
        {
            try
            {
                if (entry.Go != null &&
                    entry.Go.activeSelf != active)
                    entry.Go.SetActive(active);
                if (entry.Shadow != null &&
                    entry.Shadow.activeSelf != active)
                    entry.Shadow.SetActive(active);
            }
            catch { }
        }

        private static void EnsureChart()
        {
            int generation = ChartExtState.ChartGeneration;
            if (generation == _generation) return;
            _generation = generation;
            ClearSkyVisuals();
            DestroyAll();
            Targets.Clear();
            TargetKeys.Clear();
            SkyTargets.Clear();
            SkyTargetIds.Clear();
            var slots = ChartExtState.NegativeSpeedSlots;
            for (int i = 0; i < slots.Count; i++)
            {
                ChartExtState.NegativeSlot slot = slots[i];
                double start = TimingGroupRuntime.VisualTimeMs(
                    slot.Group, slot.StartMs);
                double end = TimingGroupRuntime.VisualTimeMs(
                    slot.Group, slot.EndMs);
                // 复杂长条：净长度为负，或区间内存在负流速段
                // （中途反向/锯齿）。两类都需要按 W(end)-W(now) 渲染。
                bool complex = end < start - 1.0 ||
                    TimingGroupRuntime.HasNegativeSegment(
                        slot.Group, slot.StartMs, slot.EndMs);
                if (!complex) continue;
                if (slot.Type == 5)
                {
                    // SkyArea 不做独立视觉：保留原版对象，只在其渲染相
                    // 临时改写编译 _IA 表项（见 ApplySkyWarp）。
                    SkyTargets.Add(slot);
                    SkyTargetIds.Add(slot.Id);
                    continue;
                }
                Targets.Add(slot);
                TargetKeys.Add(
                    ((long)(uint)slot.Side << 32) |
                    (uint)slot.Position);
            }
            int holds = 0;
            int skies = 0;
            for (int i = 0; i < Targets.Count; i++)
            {
                if (Targets[i].Type == 5) skies++;
                else holds++;
            }
            ChartExtMod.Log.Msg(
                "[neg-speed] targets: total=" + Targets.Count +
                " hold=" + holds + " sky=" + skies +
                " skyWarp=" + SkyTargets.Count +
                " candidates=" + slots.Count);
        }

        internal static void StripSpawnList(
            Il2CppSystem.Collections.Generic.Dictionary<
                Il2Cpp_b._CA,
                Il2Cppifapp.Game.Common.UnorderedListVal<int>> lists)
        {
            if (!Enabled) return;
            if (lists == null || lists.Pointer == IntPtr.Zero) return;
            try
            {
                EnsureChart();
                if (TargetKeys.Count == 0) return;
                for (int side = 0;
                     side < NativeLayoutCatalog.SideCount;
                     side++)
                {
                    var key = (Il2Cpp_b._CA)side;
                    if (!lists.ContainsKey(key)) continue;
                    StripList(lists[key], side);
                }
            }
            catch (Exception e)
            {
                if (_errors++ < 20)
                    ChartExtMod.Log.Error(
                        "[ChartExt] negative-speed spawn strip failed: " +
                        e.Message);
            }
        }

        private static void StripList(
            Il2Cppifapp.Game.Common.UnorderedListVal<int> list,
            int side)
        {
            if (list == null || list.Pointer == IntPtr.Zero) return;
            IntPtr lp = list.Pointer;
            IntPtr arr = Marshal.ReadIntPtr(
                lp + NativeLayoutCatalog.Runtime.ListItems);
            int count = Marshal.ReadInt32(
                lp + NativeLayoutCatalog.Runtime.ListCount);
            if (arr == IntPtr.Zero || count <= 0) return;
            int capacity = Marshal.ReadInt32(
                arr + NativeLayoutCatalog.ArrayLengthOffset);
            if (count > capacity) return;
            IntPtr data = arr + NativeLayoutCatalog.ArrayDataOffset;
            int write = 0;
            for (int read = 0; read < count; read++)
            {
                int position = Marshal.ReadInt32(data + 4 * read);
                long key = ((long)(uint)side << 32) |
                           (uint)position;
                if (TargetKeys.Contains(key)) continue;
                if (write != read)
                    Marshal.WriteInt32(data + 4 * write, position);
                write++;
            }
            if (write != count)
                Marshal.WriteInt32(
                    lp + NativeLayoutCatalog.Runtime.ListCount, write);
        }

        internal static void ResetForChartSwitch()
        {
            RestoreSkyWarp();
            _generation = -1;
            _track = IntPtr.Zero;
            Targets.Clear();
            TargetKeys.Clear();
            SkyTargets.Clear();
            SkyTargetIds.Clear();
            ClearSkyVisuals();
            _skyScanFrame = 0;
            _skyScanDone = false;
            _skyMirrorLogs = 0;
            _skyWarpLogged = false;
            DestroyAll();
        }

        private static void ClearSkyVisuals()
        {
            foreach (var pair in SkyVisuals)
            {
                RestoreMirror(pair.Value);
                Material material = pair.Value.OwnedMaterial;
                if (material == null || material.Pointer == IntPtr.Zero)
                    continue;
                try { UnityEngine.Object.Destroy(material); }
                catch { }
            }
            SkyVisuals.Clear();
            SkyMirrorNow.Clear();
            SkyVisualPrune.Clear();
        }

        private static void DestroyAll()
        {
            foreach (Entry entry in Entries.Values)
                DestroyEntry(entry);
            Entries.Clear();
        }

        private static void DestroyEntry(Entry entry)
        {
            if (entry == null) return;
            for (int i = 0; i < entry.OwnedMaterials.Count; i++)
            {
                Material material = entry.OwnedMaterials[i];
                try
                {
                    if (material != null &&
                        material.Pointer != IntPtr.Zero)
                        UnityEngine.Object.Destroy(material);
                }
                catch { }
            }
            entry.OwnedMaterials.Clear();
            TryDestroy(entry.Go);
            TryDestroy(entry.Shadow);
            entry.Go = null;
            entry.Shadow = null;
            entry.Root = null;
            entry.ShadowRoot = null;
            entry.Hold = null;
            entry.BandArea = null;
            entry.ShadowArea = null;
        }

        private static void TryDestroy(GameObject go)
        {
            try
            {
                if (go != null && go.Pointer != IntPtr.Zero)
                    UnityEngine.Object.Destroy(go);
            }
            catch { }
        }

        private static Vector3 ReadVector3(IntPtr address)
        {
            return new Vector3(
                ReadFloat(address),
                ReadFloat(address + 4),
                ReadFloat(address + 8));
        }

        private static float ReadFloat(IntPtr address)
        {
            float value = BitConverter.Int32BitsToSingle(
                Marshal.ReadInt32(address));
            return float.IsNaN(value) || float.IsInfinity(value)
                ? 0f
                : value;
        }
    }
}
