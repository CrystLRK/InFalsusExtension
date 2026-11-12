using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace InFalsusChartExt
{
    /// <summary>
    /// timinggroup 运行期（视觉接管）。
    ///
    /// 判定完全不受影响：本模块只在**渲染相**（Track.Update 顶部 `_hb` 之后、渲染循环
    /// 之前）把带组音符的时间字段临时改写，使引擎用全局时间轴渲染出"该组独立
    /// W(t)=∫speed dt 时间线"的等价位置：
    ///
    ///   be' = W_global⁻¹( W_global(now) + ( W_group(be) − W_group(now) ) )
    ///
    /// 渲染结束（Track.Update 末尾）立即还原 be/end —— 判定相 `_hb` 与所有判定逻辑
    /// 读到的仍是真实时间；`_Ee` 同步改写为 be'−now，两条渲染取数路径都覆盖。
    /// 该机制对 tap/hold/flick/skyarea 一视同仁（长度由引擎自己按 be/end 推导），
    /// 无需接触任何渲染对象。
    /// </summary>
    internal static class TimingGroupRuntime
    {
        private static readonly List<WarpSession> Sessions =
            new List<WarpSession>();
        private static int _sessionCount;
        private static int _errors;
        private static int _lastFrame = -1;
        private static int _iaSkipLogs;
        private static int _restoreSkipLogs;
        private static bool _visualClockPaused;
        private static double _visualClockHoldMs;
        private static SpeedCurve _globalCurve;
        private static readonly Dictionary<int, SpeedCurve> GroupCurves =
            new Dictionary<int, SpeedCurve>();
        private static int _curveGeneration = -1;
        private static int _curveGlobalEvents = -1;
        private static int _curveGroupEvents = -1;

        private sealed class WarpSession
        {
            internal int Side;
            internal int Position;
            internal int Group;
            internal int Type;
            internal long Id;
            internal IntPtr CompiledOwner;
            internal int CompileGeneration;

            internal int OriginalStartMs;
            internal int OriginalEndMs;
            internal long OriginalRuntimeLeadBits;
            internal int WarpedStartMs;
            internal int WarpedEndMs;
            internal long WarpedRuntimeLeadBits;

            internal long OriginalIaStartBits;
            internal long OriginalIaEndBits;
            internal long WrittenIaStartBits;
            internal long WrittenIaEndBits;

            internal bool IaStartWritten;
            internal bool IaEndWritten;
            internal bool StartWritten;
            internal bool EndWritten;
            internal bool RuntimeLeadWritten;
            internal bool VisualFieldsApplied;
        }

        private sealed class SpeedCurve
        {
            private readonly List<double> _times = new List<double>();
            private readonly List<double> _windAt = new List<double>();
            private readonly List<double> _multAfter = new List<double>();

            internal bool AllPositive { get; private set; } = true;

            internal void Build(
                List<(double Time, double Mult, int Order)> events)
            {
                _times.Clear();
                _windAt.Clear();
                _multAfter.Clear();
                events.Sort((a, b) =>
                {
                    int time = a.Time.CompareTo(b.Time);
                    return time != 0
                        ? time
                        : a.Order.CompareTo(b.Order);
                });
                double previous = 0.0;
                double wind = 0.0;
                double multiplier = 1.0;
                for (int i = 0; i < events.Count; i++)
                {
                    double eventTime = events[i].Time;
                    double eventMult = events[i].Mult;
                    wind += multiplier * (eventTime - previous);
                    previous = eventTime;
                    multiplier = eventMult;
                    if (!(eventMult > 0.0))
                        AllPositive = false;
                    _times.Add(eventTime);
                    _windAt.Add(wind);
                    _multAfter.Add(eventMult);
                }
            }

            internal double Evaluate(double t)
            {
                int eventIndex = FirstEventAtOrAfter(t);
                if (eventIndex == 0) return t;
                int previous = eventIndex - 1;
                return _windAt[previous] +
                       _multAfter[previous] * (t - _times[previous]);
            }

            internal double Inverse(double target)
            {
                if (_times.Count == 0)
                    return target;
                if (target <= 0.0)
                    return target;

                int eventIndex = FirstWindAtOrAbove(target);
                if (eventIndex < 0)
                {
                    int last = _times.Count - 1;
                    double multiplier = _multAfter[last];
                    if (!(multiplier > 1e-12)) return _times[last];
                    return _times[last] +
                           (target - _windAt[last]) / multiplier;
                }
                if (eventIndex == 0)
                    return target;

                int previous = eventIndex - 1;
                double segmentMult = _multAfter[previous];
                if (!(segmentMult > 1e-12))
                    return _times[previous];
                return _times[previous] +
                       (target - _windAt[previous]) / segmentMult;
            }

            private int FirstEventAtOrAfter(double t)
            {
                int lo = 0;
                int hi = _times.Count;
                while (lo < hi)
                {
                    int mid = lo + ((hi - lo) >> 1);
                    if (_times[mid] < t) lo = mid + 1;
                    else hi = mid;
                }
                return lo;
            }

            private int FirstWindAtOrAbove(double target)
            {
                int lo = 0;
                int hi = _windAt.Count;
                while (lo < hi)
                {
                    int mid = lo + ((hi - lo) >> 1);
                    if (_windAt[mid] < target) lo = mid + 1;
                    else hi = mid;
                }
                return lo < _windAt.Count ? lo : -1;
            }
        }

        internal static bool Active => ChartExtState.GroupedSlots.Count > 0;

        internal static void ApplyWarp()
        {
            if (!BuildProfile.Supports(NativeCapability.TimingGroup))
                return;
            try
            {
                List<ChartExtState.GroupSlot> slots = ChartExtState.GroupedSlots;
                if (slots.Count == 0) return;
                // 同一帧只改写一次（`_hb` 可能被调用多次；已改写状态下重复计算会叠乘）。
                if (UnityEngine.Time.frameCount == _lastFrame &&
                    _sessionCount > 0)
                    return;
                _lastFrame = UnityEngine.Time.frameCount;
                // 上一帧若未来得及还原（异常路径），先把真实时间放回去，避免叠乘。
                if (_sessionCount > 0) Restore();
                double nowMs = NowMs();
                if (double.IsNaN(nowMs)) return;

                double wNowGlobal = WGlobal(nowMs);

                Il2Cppifapp.Game.LogicalNotePlayer player =
                    Il2Cppifapp.Game.LogicalNotePlayer._Qe;
                if (player == null || player.Pointer == IntPtr.Zero)
                    return;
                Il2Cpp_b._T compiled = player._ve;
                if (compiled == null || compiled.Pointer == IntPtr.Zero)
                    return;
                IntPtr compiledOwner = compiled.Pointer;
                int compileGeneration = ChartExtState.CompileGeneration;
                if (!TryResolveWarpTable(
                        compiled, out IntPtr iaBase, out int iaLength))
                    return;

                ClearSessions();

                for (int i = 0; i < slots.Count; i++)
                {
                    ChartExtState.GroupSlot slot = slots[i];
                    if (!TryResolveSlotNote(slot, out IntPtr note))
                        continue;
                    int be = Marshal.ReadInt32(
                        note + NativeLayoutCatalog.Note.StartMs);
                    double rel = be - nowMs;
                    double wgNow = WGroup(slot.Group, nowMs);
                    double wgBe = WGroup(slot.Group, be);
                    double visualRel = wgBe - wgNow;
                    // Native flick rendering can be ahead of the real spawn window
                    // because its group timeline may be much slower. Keep its `_IA`
                    // entry available while it is in the visual window; other types
                    // retain the original real-time gate.
                    if (slot.Type == 2 || slot.Type == 4 || slot.Type == 5)
                    {
                        if (visualRel > 3000.0 || visualRel < -3000.0) continue;
                    }
                    else if (rel > 3000.0 || rel < -3000.0)
                    {
                        continue;
                    }
                    int end = Marshal.ReadInt32(
                        note + NativeLayoutCatalog.Note.EndMs);

                    double groupLead = wgBe - wgNow;
                    double groupEndLead = WGroup(slot.Group, end) - wgNow;
                    double targetBe = wNowGlobal + groupLead;
                    double targetEnd = wNowGlobal + groupEndLead;
                    int beW = (int)Math.Round(
                        InvGlobal(targetBe, nowMs));
                    int endW = (int)Math.Round(
                        InvGlobal(targetEnd, nowMs));
                    if (double.IsNaN(targetBe) || double.IsInfinity(targetBe) ||
                        double.IsNaN(targetEnd) || double.IsInfinity(targetEnd))
                        continue;

                    // 注意：这里先不写 note+0x18/0x1C。
                    // `_hb` 用 note start 判断“距离不足 1000ms 立即生成”，
                    // 若在 `_hb` 前缀就写 warp 后的短时间，低流速音符会被
                    // 这个 1 秒快速路径提前加入生成列表，表现为远处突然出现。
                    // `_hb` 返回后再由 ApplyVisualFields() 写入渲染时间。
                    // `_IA` 表项 = 渲染真正消费的"音符时间"，**单位是秒**（探针实证：
                    // id=0/be=1000 → 1.0；id=104/be=14000 → 14.0），16 字节/项、按 id 索引、
                    // 基址 = object + 0x20 + index*16。写入 warp 空间的目标时间（秒）；
                    // 先做 sanity（nan/inf/与原时间偏离过大都跳过），防止再次写坏。
                    long id = Marshal.ReadInt64(
                        note + NativeLayoutCatalog.Note.Id);
                    if (id < 0 || id >= iaLength)
                        continue;

                    IntPtr entry = new IntPtr(
                        iaBase.ToInt64() + id *
                        NativeLayoutCatalog.Compiled.WarpEntryStride);
                    long origBits = Marshal.ReadInt64(entry);
                    long orig2Bits = Marshal.ReadInt64(new IntPtr(
                        entry.ToInt64() +
                        NativeLayoutCatalog.Compiled.WarpEntryEnd));
                    double orig = BitConverter.Int64BitsToDouble(origBits);
                    double orig2 = BitConverter.Int64BitsToDouble(orig2Bits);
                    bool origUninitialized =
                        !double.IsNaN(orig) && !double.IsInfinity(orig) &&
                        Math.Abs(orig) <= 1e-9 && be > 1000;
                    bool sane = !double.IsNaN(orig) &&
                                !double.IsInfinity(orig) &&
                                (origUninitialized ||
                                 Math.Abs(orig * 1000.0 - be) <= 120000.0);
                    bool orig2Uninitialized =
                        !double.IsNaN(orig2) &&
                        !double.IsInfinity(orig2) &&
                        Math.Abs(orig2) <= 1e-9 && end > 1000;
                    bool saneEnd =
                        !double.IsNaN(orig2) &&
                        !double.IsInfinity(orig2) &&
                        (orig2Uninitialized ||
                         Math.Abs(orig2 * 1000.0 - end) <= 120000.0);
                    if (!sane || !saneEnd)
                    {
                        if (_iaSkipLogs < 6)
                        {
                            _iaSkipLogs++;
                            ChartExtMod.Log.Warning(
                                "[ChartExt] timing warp: ia entry skipped " +
                                $"id={id} start={orig:F3}s/{be} " +
                                $"end={orig2:F3}s/{end}");
                        }
                        continue;
                    }

                    // A zero-speed fold can collapse several source timestamps
                    // onto the same global time. Preserve a positive visual
                    // duration for holds/skyareas so the native mesh path never
                    // receives an inverted interval (which aborts Track.Update
                    // and briefly removes the whole sky layer).
                    if (endW < beW) endW = beW;
                    if (end > be && endW <= beW) endW = beW + 1;

                    WarpSession session = AcquireSession();
                    session.Side = slot.Side;
                    session.Position = slot.Position;
                    session.Group = slot.Group;
                    session.Type = slot.Type;
                    session.Id = id;
                    session.CompiledOwner = compiledOwner;
                    session.CompileGeneration = compileGeneration;
                    session.OriginalStartMs = be;
                    session.OriginalEndMs = end;
                    session.WarpedStartMs = beW;
                    session.WarpedEndMs = endW;
                    session.OriginalIaStartBits = origBits;
                    session.OriginalIaEndBits = orig2Bits;
                    session.WrittenIaStartBits =
                        BitConverter.DoubleToInt64Bits(targetBe / 1000.0);
                    session.WrittenIaEndBits =
                        BitConverter.DoubleToInt64Bits(targetEnd / 1000.0);
                    session.IaStartWritten = true;
                    session.IaEndWritten = true;
                    session.StartWritten = false;
                    session.EndWritten = false;
                    session.RuntimeLeadWritten = false;
                    session.VisualFieldsApplied = false;
                    _sessionCount++;

                    Marshal.WriteInt64(
                        entry, session.WrittenIaStartBits);
                    Marshal.WriteInt64(
                        new IntPtr(
                            entry.ToInt64() +
                            NativeLayoutCatalog.Compiled.WarpEntryEnd),
                        session.WrittenIaEndBits);
                }
            }
            catch (Exception e)
            {
                if (_errors < 20) { _errors++; ChartExtMod.Log.Error("[ChartExt] timing warp failed: " + e); }
                Restore();
            }
        }

        /// <summary>渲染结束立即还原真实时间字段（判定/结算读到的永远是原值）。</summary>
        internal static void Restore()
        {
            int count = _sessionCount;
            if (count == 0) return;
            try
            {
                bool sameCompiledOwner = TryGetCompiledOwner(
                    out IntPtr currentOwner);
                bool tableReady = false;
                IntPtr iaBase = IntPtr.Zero;
                int iaLength = 0;
                if (sameCompiledOwner)
                {
                    Il2Cppifapp.Game.LogicalNotePlayer player =
                        Il2Cppifapp.Game.LogicalNotePlayer._Qe;
                    Il2Cpp_b._T compiled = player?._ve;
                    tableReady = compiled != null &&
                        TryResolveWarpTable(
                            compiled, out iaBase, out iaLength);
                }

                for (int i = count - 1; i >= 0; i--)
                {
                    WarpSession session = Sessions[i];
                    bool sameOwner = sameCompiledOwner &&
                        IsSameOwner(
                            session,
                            currentOwner,
                            ChartExtState.CompileGeneration);
                    if (!sameOwner)
                    {
                        LogRestoreSkip(
                            $"owner changed id={session.Id}");
                        continue;
                    }

                    if (TryResolveSessionNote(session, out IntPtr note))
                    {
                        RestoreInt32(
                            note + NativeLayoutCatalog.Note.StartMs,
                            session.WarpedStartMs,
                            session.OriginalStartMs,
                            session.StartWritten,
                            session.Id,
                            "start");
                        RestoreInt32(
                            note + NativeLayoutCatalog.Note.EndMs,
                            session.WarpedEndMs,
                            session.OriginalEndMs,
                            session.EndWritten,
                            session.Id,
                            "end");
                        RestoreInt64(
                            note + NativeLayoutCatalog.Note.RuntimeLead,
                            session.WarpedRuntimeLeadBits,
                            session.OriginalRuntimeLeadBits,
                            session.RuntimeLeadWritten,
                            session.Id,
                            "runtimeLead");
                    }
                    else
                    {
                        LogRestoreSkip(
                            $"slot changed id={session.Id}");
                    }

                    if (!tableReady)
                    {
                        LogRestoreSkip(
                            $"compiled table unavailable id={session.Id}");
                        continue;
                    }
                    if (session.Id < 0 || session.Id >= iaLength)
                    {
                        LogRestoreSkip(
                            $"ia range changed id={session.Id}");
                        continue;
                    }

                    IntPtr entry = new IntPtr(
                        iaBase.ToInt64() + session.Id *
                        NativeLayoutCatalog.Compiled.WarpEntryStride);
                    RestoreInt64(
                        entry,
                        session.WrittenIaStartBits,
                        session.OriginalIaStartBits,
                        session.IaStartWritten,
                        session.Id,
                        "iaStart");
                    RestoreInt64(
                        new IntPtr(
                            entry.ToInt64() +
                            NativeLayoutCatalog.Compiled.WarpEntryEnd),
                        session.WrittenIaEndBits,
                        session.OriginalIaEndBits,
                        session.IaEndWritten,
                        session.Id,
                        "iaEnd");
                }
            }
            catch (Exception e)
            {
                if (_errors < 20) { _errors++; ChartExtMod.Log.Error("[ChartExt] timing restore failed: " + e); }
            }
            finally
            {
                ClearSessions();
            }
        }

        /// <summary>
        /// `_Ib` 编译边界。旧 compiled 对象仍可通过 session owner 校验时先恢复，
        /// 然后推进独立 compile epoch；同一 `_ve` 指针被再次编译时也不会误认领旧事务。
        /// </summary>
        internal static void BeginCompile()
        {
            Restore();
            ChartExtState.CompileGeneration++;
            _lastFrame = -1;
        }

        /// <summary>
        /// 切谱边界：把上一张谱可能尚未还原的 timinggroup 渲染时间写回，
        /// 再丢弃所有临时状态。必须在下一张谱编译/渲染开始前调用，否则
        /// `_IA` 中残留的压缩时间会让新谱部分 note 视觉提前出现。
        /// </summary>
        internal static void ResetForChartSwitch()
        {
            try { Restore(); }
            catch { }
            _lastFrame = -1;
            _visualClockPaused = false;
            _visualClockHoldMs = 0.0;
        }

        /// <summary>
        /// `_hb` 已用真实 note start 完成本帧生成列表筛选后，在进入原版
        /// Track 渲染循环前写入变速后的起止时间与 `_Ee`。这样：
        /// 1. 低流速音符不会命中 `_hb` 的 start&lt;1000ms 快速生成路径；
        /// 2. 位置、hold/skyarea 长度仍按 timinggroup 时间线渲染；
        /// 3. 判定相看到的仍是原始时间，Track.Update 末尾统一还原。
        /// </summary>
        internal static void ApplyVisualFields()
        {
            try
            {
                double nowMs = NowMs();
                if (double.IsNaN(nowMs))
                    return;
                if (!TryGetCompiledOwner(out IntPtr currentOwner))
                    return;
                for (int i = 0; i < _sessionCount; i++)
                {
                    WarpSession session = Sessions[i];
                    if (session.VisualFieldsApplied)
                        continue;
                    if (!IsSameOwner(
                            session,
                            currentOwner,
                            ChartExtState.CompileGeneration))
                        continue;
                    if (!TryResolveSessionNote(session, out IntPtr note))
                        continue;

                    session.OriginalRuntimeLeadBits =
                        Marshal.ReadInt64(
                            note +
                            NativeLayoutCatalog.Note.RuntimeLead);
                    session.WarpedRuntimeLeadBits =
                        BitConverter.DoubleToInt64Bits(
                            session.WarpedStartMs - nowMs);
                    session.StartWritten = true;
                    Marshal.WriteInt32(
                        note + NativeLayoutCatalog.Note.StartMs,
                        session.WarpedStartMs);
                    session.EndWritten = true;
                    Marshal.WriteInt32(
                        note + NativeLayoutCatalog.Note.EndMs,
                        session.WarpedEndMs);
                    session.RuntimeLeadWritten = true;
                    Marshal.WriteInt64(
                        note + NativeLayoutCatalog.Note.RuntimeLead,
                        session.WarpedRuntimeLeadBits);
                    session.VisualFieldsApplied = true;
                }
            }
            catch (Exception e)
            {
                if (_errors < 20)
                {
                    _errors++;
                    ChartExtMod.Log.Error("[ChartExt] timing visual fields failed: " +
                                          e.Message);
                }
                Restore();
            }
        }

        private static WarpSession AcquireSession()
        {
            if (_sessionCount == Sessions.Count)
                Sessions.Add(new WarpSession());
            return Sessions[_sessionCount];
        }

        private static void ClearSessions()
        {
            _sessionCount = 0;
        }

        private static bool TryResolveSlotNote(
            ChartExtState.GroupSlot slot, out IntPtr note)
        {
            note = IntPtr.Zero;
            if (!HasCurrentCompiledIdentity(
                    slot.CompiledOwner,
                    slot.CompileGeneration))
                return false;
            IntPtr basePtr = NativeNoteSlotTable.SlotBase(slot.Side);
            if (basePtr == IntPtr.Zero)
                return false;
            IntPtr candidate = basePtr +
                slot.Position * NativeLayoutCatalog.NoteStride;
            long currentId = Marshal.ReadInt64(
                candidate + NativeLayoutCatalog.Note.Id);
            if (!IsSameSlotIdentity(slot.Id, currentId))
                return false;
            note = candidate;
            return true;
        }

        /// <summary>
        /// Resolves a grouped note only while the compiled owner and compile
        /// generation that produced the slot are still current. Callers that
        /// temporarily flip the visual type may also accept the already-flipped
        /// state so the operation remains idempotent.
        /// </summary>
        internal static bool TryResolveGroupedSlot(
            ChartExtState.GroupSlot slot,
            out IntPtr note,
            out int currentType,
            bool allowVisualType)
        {
            note = IntPtr.Zero;
            currentType = 0;
            if (slot == null ||
                !HasCurrentCompiledIdentity(
                    slot.CompiledOwner,
                    slot.CompileGeneration))
                return false;
            IntPtr basePtr = NativeNoteSlotTable.SlotBase(slot.Side);
            if (basePtr == IntPtr.Zero)
                return false;
            IntPtr candidate = basePtr +
                slot.Position * NativeLayoutCatalog.NoteStride;
            long currentId = Marshal.ReadInt64(
                candidate + NativeLayoutCatalog.Note.Id);
            if (!IsSameSlotIdentity(slot.Id, currentId))
                return false;
            currentType = Marshal.ReadInt32(
                candidate + NativeLayoutCatalog.Note.Type);
            if (currentType != slot.Type &&
                !(allowVisualType && currentType == 6))
                return false;
            note = candidate;
            return true;
        }

        internal static bool HasCurrentCompiledIdentity(
            IntPtr expectedOwner,
            int expectedCompileGeneration)
        {
            return expectedOwner != IntPtr.Zero &&
                   expectedCompileGeneration ==
                   ChartExtState.CompileGeneration &&
                   TryGetCompiledOwner(out IntPtr currentOwner) &&
                   currentOwner == expectedOwner;
        }

        private static bool TryResolveSessionNote(
            WarpSession session, out IntPtr note)
        {
            note = IntPtr.Zero;
            IntPtr basePtr = NativeNoteSlotTable.SlotBase(session.Side);
            if (basePtr == IntPtr.Zero)
                return false;
            IntPtr candidate = basePtr +
                session.Position * NativeLayoutCatalog.NoteStride;
            long currentId = Marshal.ReadInt64(
                candidate + NativeLayoutCatalog.Note.Id);
            if (!IsSameSlotIdentity(session.Id, currentId))
                return false;
            note = candidate;
            return true;
        }

        private static bool TryGetCompiledOwner(out IntPtr owner)
        {
            owner = IntPtr.Zero;
            try
            {
                Il2Cppifapp.Game.LogicalNotePlayer player =
                    Il2Cppifapp.Game.LogicalNotePlayer._Qe;
                if (player == null || player.Pointer == IntPtr.Zero)
                    return false;
                Il2Cpp_b._T compiled = player._ve;
                if (compiled == null || compiled.Pointer == IntPtr.Zero)
                    return false;
                owner = compiled.Pointer;
                return true;
            }
            catch
            {
                owner = IntPtr.Zero;
                return false;
            }
        }

        private static bool TryResolveWarpTable(
            Il2Cpp_b._T compiled,
            out IntPtr iaBase,
            out int iaLength)
        {
            iaBase = IntPtr.Zero;
            iaLength = 0;
            if (compiled == null || compiled.Pointer == IntPtr.Zero)
                return false;
            try
            {
                IntPtr tableObj = Marshal.ReadIntPtr(
                    compiled.Pointer +
                    NativeLayoutCatalog.Compiled.WarpMemoryObject);
                int tableIndex = Marshal.ReadInt32(
                    compiled.Pointer +
                    NativeLayoutCatalog.Compiled.WarpMemoryIndex);
                iaLength = Marshal.ReadInt32(
                    compiled.Pointer +
                    NativeLayoutCatalog.Compiled.WarpMemoryLength);
                if (tableObj == IntPtr.Zero ||
                    tableIndex < 0 ||
                    iaLength < 0)
                {
                    iaLength = 0;
                    return false;
                }
                iaBase = new IntPtr(
                    tableObj.ToInt64() +
                    NativeLayoutCatalog.ArrayDataOffset +
                    (long)tableIndex *
                    NativeLayoutCatalog.Compiled.WarpEntryStride);
                return true;
            }
            catch
            {
                iaBase = IntPtr.Zero;
                iaLength = 0;
                return false;
            }
        }

        private static void RestoreInt32(
            IntPtr address,
            int writtenValue,
            int originalValue,
            bool wasWritten,
            long id,
            string field)
        {
            if (!wasWritten) return;
            if (!ValueStillOwned(
                    Marshal.ReadInt32(address),
                    writtenValue,
                    wasWritten))
            {
                LogRestoreSkip(
                    $"{field} changed id={id}");
                return;
            }
            Marshal.WriteInt32(address, originalValue);
        }

        private static void RestoreInt64(
            IntPtr address,
            long writtenValue,
            long originalValue,
            bool wasWritten,
            long id,
            string field)
        {
            if (!wasWritten) return;
            if (!ValueStillOwned(
                    Marshal.ReadInt64(address),
                    writtenValue,
                    wasWritten))
            {
                LogRestoreSkip(
                    $"{field} changed id={id}");
                return;
            }
            Marshal.WriteInt64(address, originalValue);
        }

        private static bool IsSameSlotIdentity(
            long expectedId, long currentId)
        {
            return expectedId == currentId;
        }

        private static bool IsSameOwner(
            WarpSession session,
            IntPtr currentOwner,
            int currentCompileGeneration)
        {
            return session.CompiledOwner == currentOwner &&
                   session.CompileGeneration ==
                   currentCompileGeneration;
        }

        private static bool ValueStillOwned(
            long currentValue,
            long writtenValue,
            bool wasWritten)
        {
            return wasWritten &&
                   currentValue == writtenValue;
        }

        private static void LogRestoreSkip(string detail)
        {
            if (_restoreSkipLogs >= 12) return;
            _restoreSkipLogs++;
            ChartExtMod.Log.Warning(
                "[ChartExt] timing restore skipped: " + detail);
        }

        private static double NowMs()
        {
            Il2Cpp_E._VD judge = Il2Cppifapp.Game.Scenes.GameScene._DN;
            if (judge == null || judge.Pointer == IntPtr.Zero) return double.NaN;
            double raw = judge._c * 1000.0;
            if ((byte)Il2Cpp_E._VD._a != 2)
            {
                if (!_visualClockPaused)
                {
                    _visualClockPaused = true;
                    _visualClockHoldMs = raw;
                }
                return _visualClockHoldMs;
            }
            _visualClockPaused = false;
            return raw;
        }

        internal static double CurrentMs() => NowMs();

        internal static bool IsVisualClockPaused =>
            _visualClockPaused;

        /// <summary>
        /// The pause layer remains active during the resume transition. Mod
        /// visuals must hold their last pose until control returns to gameplay.
        /// </summary>
        internal static bool IsPauseLayerActive()
        {
            try
            {
                var scene =
                    Il2Cppifapp.Game.Scenes.GameScene._xn;
                if (scene == null ||
                    scene.Pointer == IntPtr.Zero)
                    return false;
                var pause = scene.pauseLayer;
                return pause != null &&
                       pause.Pointer != IntPtr.Zero &&
                       pause.gameObject != null &&
                       pause.gameObject.activeSelf;
            }
            catch
            {
                return false;
            }
        }

        internal static bool IsResumeTransition()
        {
            return (byte)Il2Cpp_E._VD._a == 2 &&
                   IsPauseLayerActive();
        }

        /// <summary>
        /// Conservative noClip tail length derived from the current track
        /// scale. Two visible track lengths are enough for the independent
        /// object to pass behind the camera without using a fixed 2s/3s cull.
        /// </summary>
        internal static double NoClipTailMs()
        {
            Il2Cppifapp.Game.Scenes.Game.Track track =
                Il2Cppifapp.Game.Scenes.Game.Track._a;
            if (track == null || track.Pointer == IntPtr.Zero)
                return 8000.0;
            double speed = BitConverter.Int64BitsToDouble(
                Marshal.ReadInt64(
                    track.Pointer + NativeLayoutCatalog.Track.DepthFactor));
            if (double.IsNaN(speed) || double.IsInfinity(speed))
                return 8000.0;
            double magnitude = Math.Abs(speed);
            if (magnitude < 1e-4)
                return 120000.0;
            return Math.Min(120000.0, 8000.0 / magnitude);
        }

        internal static double DebugWGroup(int group, double t) => WGroup(group, t);

        /// <summary>该计时组在原始谱面时间 t 的视觉时间 W_g(t)。
        /// group=0 表示全局时间线。</summary>
        internal static double VisualTimeMs(int group, double t)
        {
            return group > 0 ? WGroup(group, t) : WGlobal(t);
        }

        private static void EnsureCurves()
        {
            int generation = ChartExtState.ChartGeneration;
            int globalEvents = ChartExtState.SpeedEvents.Count;
            int groupEvents = ChartExtState.GroupSpeeds.Count;
            if (_curveGeneration == generation &&
                _curveGlobalEvents == globalEvents &&
                _curveGroupEvents == groupEvents)
                return;

            var global =
                new List<(double Time, double Mult, int Order)>(
                    globalEvents);
            for (int i = 0; i < globalEvents; i++)
            {
                ChartExtState.SpeedEvent item = ChartExtState.SpeedEvents[i];
                global.Add((
                    item.TimeMs,
                    item.Mult,
                    item.SourceOrder));
            }
            _globalCurve = new SpeedCurve();
            _globalCurve.Build(global);

            GroupCurves.Clear();
            for (int i = 0; i < groupEvents; i++)
            {
                ChartExtState.GroupSpeed item = ChartExtState.GroupSpeeds[i];
                if (!GroupCurves.TryGetValue(item.Group, out SpeedCurve curve))
                {
                    curve = new SpeedCurve();
                    var events =
                        new List<(double Time, double Mult, int Order)>();
                    for (int j = i; j < groupEvents; j++)
                    {
                        ChartExtState.GroupSpeed candidate =
                            ChartExtState.GroupSpeeds[j];
                        if (candidate.Group != item.Group) continue;
                        events.Add((
                            candidate.TimeMs,
                            candidate.Mult,
                            candidate.SourceOrder));
                    }
                    curve.Build(events);
                    GroupCurves[item.Group] = curve;
                }
            }
            _curveGeneration = generation;
            _curveGlobalEvents = globalEvents;
            _curveGroupEvents = groupEvents;
        }

        private static double WGlobal(double t)
        {
            EnsureCurves();
            return _globalCurve.Evaluate(t);
        }

        private static double WGroup(int group, double t)
        {
            // Chart speed events are authored on the chart timeline, whose origin is 0.
            // `judge._c` is negative during the pre-roll. Applying the default 1.0 there
            // makes a group whose first event is `track(0, x)` run at 1.0 until the
            // countdown reaches zero, then visibly snap to x. Keep the group timeline at
            // its initial value during pre-roll; this is visual-only and never changes
            // note/judgement timestamps.
            if (t < 0.0) t = 0.0;
            EnsureCurves();
            return GroupCurves.TryGetValue(group, out SpeedCurve curve)
                ? curve.Evaluate(t)
                : t;
        }

        /// <summary>
        /// Invert the global scroll timeline exactly on its piecewise-linear
        /// speed segments. The old fixed 481-point table sampled every 250 ms;
        /// Outliers changes track() every ~19 ms, so interpolation produced up
        /// to tens of milliseconds of visual error and left SkyArea tails after
        /// their native lifetime.
        /// </summary>
        private static double InvGlobal(double target, double nowMs)
        {
            EnsureCurves();
            if (_globalCurve.AllPositive)
            {
                double candidate = _globalCurve.Inverse(target);
                const double half = 60000.0;
                return Math.Max(nowMs - half,
                    Math.Min(candidate, nowMs + half));
            }
            return InvGlobalSlow(target, nowMs);
        }

        private static double InvGlobalSlow(double target, double nowMs)
        {
            const double half = 60000.0;
            const double tolerance = 1e-7;
            double begin = nowMs - half;
            double end = nowMs + half;
            List<ChartExtState.SpeedEvent> events =
                ChartExtState.SpeedEvents;

            double multiplier = 1.0;
            int eventIndex = 0;
            while (eventIndex < events.Count &&
                   events[eventIndex].TimeMs <= begin)
            {
                multiplier = events[eventIndex].Mult;
                eventIndex++;
            }

            double segmentStart = begin;
            double wStart = WGlobal(begin);
            double best = double.NaN;
            double bestDistance = double.PositiveInfinity;

            for (; eventIndex < events.Count; eventIndex++)
            {
                double segmentEnd = events[eventIndex].TimeMs;
                if (segmentEnd >= end) break;
                if (segmentEnd <= segmentStart)
                {
                    multiplier = events[eventIndex].Mult;
                    continue;
                }

                double wEnd = wStart +
                              multiplier * (segmentEnd - segmentStart);
                ConsiderInverseSegment(
                    segmentStart, segmentEnd, wStart, wEnd,
                    target, nowMs, tolerance,
                    ref best, ref bestDistance);

                segmentStart = segmentEnd;
                wStart = wEnd;
                multiplier = events[eventIndex].Mult;
            }

            if (segmentStart < end)
            {
                double wEnd = wStart +
                              multiplier * (end - segmentStart);
                ConsiderInverseSegment(
                    segmentStart, end, wStart, wEnd,
                    target, nowMs, tolerance,
                    ref best, ref bestDistance);
            }

            if (!double.IsNaN(best)) return best;

            double wBegin = WGlobal(begin);
            double wFinish = WGlobal(end);
            return Math.Abs(target - wBegin) <
                   Math.Abs(target - wFinish)
                ? begin
                : end;
        }

        private static void ConsiderInverseSegment(
            double start, double end, double wStart, double wEnd,
            double target, double nowMs, double tolerance,
            ref double best, ref double bestDistance)
        {
            double lo = Math.Min(wStart, wEnd) - tolerance;
            double hi = Math.Max(wStart, wEnd) + tolerance;
            if (target < lo || target > hi) return;

            double candidate;
            double span = wEnd - wStart;
            if (Math.Abs(span) < tolerance)
            {
                candidate = Math.Max(start, Math.Min(nowMs, end));
            }
            else
            {
                candidate = start +
                            (target - wStart) * (end - start) / span;
                candidate = Math.Max(start, Math.Min(candidate, end));
            }

            double distance = Math.Abs(candidate - nowMs);
            if (distance >= bestDistance) return;
            best = candidate;
            bestDistance = distance;
        }
    }
}
