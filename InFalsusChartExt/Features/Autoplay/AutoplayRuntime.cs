using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using HarmonyLib;
using MelonLoader;

namespace InFalsusChartExt
{
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
        private static bool _groundOnly;
        private static int _cursor;
        private static int _holdCursor;
        private static readonly List<ChartExtState.AutoplayDue>
            ActiveHolds =
                new List<ChartExtState.AutoplayDue>();
        private static double _lastNow = -1e9;
        private static int _errors;

        internal static bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value) return;
                _enabled = value;
                ResetCursors();
                try { ChartExtMod.Log.Msg($"[ChartExt] autoplay {(_enabled ? "ON" : "OFF")}"); } catch { }
                if (_enabled)
                    AutoplayScoreGuard.NoteAutoplayUsed();
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
                _groundOnly = value;
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
                AutoplayScoreGuard.NoteAutoplayUsed();
                if (ChartExtState.AutoplayDues.Count == 0) return;
                double now = NowMs();
                if (double.IsNaN(now)) return;
                if (now < _lastNow - 5000.0)
                    ResetCursors();
                _lastNow = now;
                while (_cursor < ChartExtState.AutoplayDues.Count &&
                       ChartExtState.AutoplayDues[_cursor].StartMs < now - 5000.0)
                    _cursor++;
                UpdateActiveHolds(now);
            }
            catch (Exception e)
            {
                if (_errors < 10) { _errors++; ChartExtMod.Log.Error("[ChartExt] autoplay tick failed: " + e); }
            }
        }

        internal static void ResetForChartSwitch()
        {
            ResetCursors();
        }

        private static void ResetCursors()
        {
            _cursor = 0;
            _holdCursor = 0;
            ActiveHolds.Clear();
            _lastNow = -1e9;
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
                List<ChartExtState.AutoplayDue> dues = ChartExtState.AutoplayDues;
                if (dues.Count == 0) return;
                Il2Cppifapp.Game.LogicalNotePlayer player = Il2Cppifapp.Game.LogicalNotePlayer._Qe;
                if (player == null || player.Pointer == IntPtr.Zero) return;
                Il2Cpp_b._T compiled = player._ve;
                if (compiled == null || compiled.Pointer == IntPtr.Zero) return;
                var records = compiled._XC;
                if (records == null || records.Pointer == IntPtr.Zero) return;
                double now = NowMs();
                if (double.IsNaN(now)) return;

                for (int i = _cursor; i < dues.Count; i++)
                {
                    ChartExtState.AutoplayDue due = dues[i];
                    if (due.StartMs > now + 300.0) break;
                    if (due.Type == 2) continue;
                    if (due.StartMs < now - 5000.0) continue;
                    RewriteDue(
                        due,
                        records);
                }
                for (int i = 0; i < ActiveHolds.Count; i++)
                {
                    ChartExtState.AutoplayDue due = ActiveHolds[i];
                    if (due.StartMs > now + 300.0)
                        continue;
                    RewriteDue(
                        due,
                        records);
                }
            }
            catch (Exception e)
            {
                if (_errors < 20) { _errors++; ChartExtMod.Log.Error("[ChartExt] autoplay outcome failed: " + e); }
            }
        }

        private static void RewriteDue(
            ChartExtState.AutoplayDue due,
            Il2CppSystem.Collections.Generic.Dictionary<
                long,
                Il2CppSystem.Memory<Il2Cpp_K._cH>> records)
        {
            if (SkyBlockJudgeRuntime.IsManagedMember(due.Id))
                return;
            if (_groundOnly &&
                due.Type != 1 && due.Type != 2)
                return;
            IntPtr basePtr = NativeNoteSlotTable.SlotBase(due.Side);
            if (basePtr == IntPtr.Zero) return;
            IntPtr note = basePtr +
                due.Position * NativeLayoutCatalog.NoteStride;
            if (Marshal.ReadInt64(
                    note + NativeLayoutCatalog.Note.Id) != due.Id)
                return;
            double ee = BitConverter.Int64BitsToDouble(
                Marshal.ReadInt64(
                    note + NativeLayoutCatalog.Note.RuntimeLead));
            if (!records.ContainsKey(due.Id)) return;
            Il2CppSystem.Memory<Il2Cpp_K._cH> memory =
                records[due.Id];
            if (memory._object == null ||
                memory._object.Pointer == IntPtr.Zero)
                return;
            int count = memory._length;
            if (count <= 0 || count > 4096) return;
            IntPtr arrayObject = memory._object.Pointer;
            int sliceIndex = memory._index;
            int arrayLength = Marshal.ReadInt32(
                arrayObject +
                NativeLayoutCatalog.ArrayLengthOffset);
            if (sliceIndex < 0 ||
                sliceIndex + count > arrayLength)
                return;
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
                        if (v25 <= -150.0 &&
                            judgment != ChartExtState.JudgeShiny)
                        {
                            *(record + InteropUtil.JudgmentOffset) =
                                ChartExtState.JudgeShiny;
                        }
                    }
                }
            }
        }

        private static double NowMs()
        {
            Il2Cpp_E._VD judge = Il2Cppifapp.Game.Scenes.GameScene._DN;
            if (judge == null || judge.Pointer == IntPtr.Zero) return double.NaN;
            return judge._c * 1000.0;
        }
    }

}
