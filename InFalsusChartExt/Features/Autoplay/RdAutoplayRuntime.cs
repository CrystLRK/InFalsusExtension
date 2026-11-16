using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace InFalsusChartExt
{
    /// <summary>
    /// RD-style autoplay used by the non-engine mode. Ground input writes the
    /// first available lane with exact note-time correction; sky input writes
    /// the cursor directly; the native _nz detour supplies flick timing.
    /// </summary>
    internal static class RdAutoplayRuntime
    {
        private const int LaneCount = 6;
        private const double GroundEarlyMs = 20.0;
        private const double TapHoldMs = 50.0;
        private const double HoldSlackMs = 40.0;
        private static readonly HashSet<long> Pressed =
            new HashSet<long>();
        private static readonly double[] ReleaseAtMs =
            new double[LaneCount];
        private static int _lastLane = -1;
        private static int _cursor;
        private static int _errors;

        static RdAutoplayRuntime()
        {
            for (int i = 0; i < LaneCount; i++)
                ReleaseAtMs[i] = -1.0;
        }

        internal static bool Active =>
            AutoplayRuntime.Enabled &&
            !AutoplayEngineInput.Enabled;

        internal static void ResetForChartSwitch()
        {
            Pressed.Clear();
            for (int i = 0; i < LaneCount; i++)
                ReleaseAtMs[i] = -1.0;
            _lastLane = -1;
            _cursor = 0;
        }

        internal static void Tick(double nowMs, bool judgePass)
        {
            if (!Active)
                return;
            IntPtr judge = AutoplayEngineInput.JudgePointer();
            if (judge == IntPtr.Zero)
                return;
            try
            {
                if (!AutoplayRuntime.GroundOnly &&
                    judgePass)
                    AutoplayEngineInput.TickCursorOnly(nowMs);
                PinSkyBar(judge);
                ReleaseLanes(judge, nowMs);
                if (!judgePass)
                    PressGround(judge, nowMs);
            }
            catch (Exception exception)
            {
                LogError("tick", exception);
            }
        }

        internal static void ReleaseAll()
        {
            IntPtr judge = AutoplayEngineInput.JudgePointer();
            if (judge != IntPtr.Zero)
            {
                IntPtr data = LaneData(judge, out int length);
                if (data != IntPtr.Zero)
                {
                    for (int lane = 0; lane < length; lane++)
                        ClearLane(data, lane);
                }
            }
            ResetForChartSwitch();
        }

        private static void PinSkyBar(IntPtr judge)
        {
            if (AutoplayRuntime.GroundOnly)
                return;
            Marshal.WriteInt64(
                judge + NativeLayoutCatalog.SkyJudge.WarningTime,
                BitConverter.DoubleToInt64Bits(300.0));
            Marshal.WriteInt64(
                judge + NativeLayoutCatalog.SkyJudge.PenaltyTime,
                BitConverter.DoubleToInt64Bits(0.0));
            Marshal.WriteByte(
                judge + NativeLayoutCatalog.SkyJudge.StartOk,
                1);
        }

        private static void PressGround(IntPtr judge, double nowMs)
        {
            List<ChartExtState.AutoplayDue> dues =
                ChartExtState.AutoplayDues;
            while (_cursor < dues.Count &&
                   dues[_cursor].StartMs < nowMs - 5000.0)
                _cursor++;
            for (int i = _cursor; i < dues.Count; i++)
            {
                ChartExtState.AutoplayDue due = dues[i];
                if (due.StartMs > nowMs + 150.0)
                    break;
                if (due.Type < 1 || due.Type > 3)
                    continue;
                if (Pressed.Contains(due.Id))
                    continue;
                IntPtr note = AutoplayEngineInput.NotePointer(due);
                if (note == IntPtr.Zero)
                    continue;
                if (!AutoplayEngineInput.TryPressGroundWhenDue(
                        due, note))
                    continue;
                int lane = ChooseLane(judge, due, note);
                if (lane < 0)
                    continue;
                IntPtr data = LaneData(judge, out int length);
                if (data == IntPtr.Zero || lane >= length)
                    continue;
                IntPtr entry = Entry(data, lane);
                Marshal.WriteByte(
                    entry,
                    NativeLayoutCatalog.SkyJudge.InputHeld,
                    1);
                Marshal.WriteByte(
                    entry,
                    NativeLayoutCatalog.SkyJudge.InputConsumed,
                    0);
                Marshal.WriteByte(
                    entry,
                    NativeLayoutCatalog.SkyJudge.InputPressed,
                    1);
                Marshal.WriteInt64(
                    entry,
                    NativeLayoutCatalog.SkyJudge.InputLastInput,
                    BitConverter.DoubleToInt64Bits(
                        nowMs / 1000.0));
                double end = due.Type == 2
                    ? due.EndMs + HoldSlackMs
                    : due.StartMs + TapHoldMs;
                ReleaseAtMs[lane] = Math.Max(
                    ReleaseAtMs[lane], end);
                Pressed.Add(due.Id);
                AutoplayScoreGuard.MarkUsed();
            }
        }

        private static void ReleaseLanes(
            IntPtr judge,
            double nowMs)
        {
            IntPtr data = LaneData(judge, out int length);
            if (data == IntPtr.Zero)
                return;
            for (int lane = 0; lane < LaneCount &&
                 lane < length; lane++)
            {
                if (ReleaseAtMs[lane] < 0.0 ||
                    nowMs < ReleaseAtMs[lane])
                    continue;
                ClearLane(data, lane);
                ReleaseAtMs[lane] = -1.0;
            }
        }

        private static int ChooseLane(
            IntPtr judge,
            ChartExtState.AutoplayDue due,
            IntPtr note)
        {
            int first = Marshal.ReadInt32(
                note + NativeLayoutCatalog.Note.LaneFirst);
            int last = Marshal.ReadInt32(
                note + NativeLayoutCatalog.Note.LaneLast);
            if (first < 0 || last < first)
                return -1;
            int fallback = -1;
            for (int lane = first; lane <= last; lane++)
            {
                if (lane == _lastLane)
                {
                    if (fallback < 0) fallback = lane;
                    continue;
                }
                if (ReleaseAtMs[lane] >= 0.0)
                {
                    if (fallback < 0) fallback = lane;
                    continue;
                }
                _lastLane = lane;
                return lane;
            }
            if (fallback < 0)
                fallback = first;
            _lastLane = fallback;
            return fallback;
        }

        private static IntPtr LaneData(
            IntPtr judge,
            out int length)
        {
            length = 0;
            IntPtr states = Marshal.ReadIntPtr(
                judge + NativeLayoutCatalog.SkyJudge.InputStates);
            if (states == IntPtr.Zero)
                return IntPtr.Zero;
            length = Marshal.ReadInt32(
                states +
                NativeLayoutCatalog.ArrayLengthOffset);
            if (length < 0 || length > 64)
                return IntPtr.Zero;
            return IntPtr.Add(
                states, NativeLayoutCatalog.ArrayDataOffset);
        }

        private static IntPtr Entry(IntPtr data, int lane) =>
            IntPtr.Add(
                data,
                lane *
                NativeLayoutCatalog.SkyJudge.InputStateStride);

        private static void ClearLane(IntPtr data, int lane)
        {
            IntPtr entry = Entry(data, lane);
            Marshal.WriteByte(
                entry,
                NativeLayoutCatalog.SkyJudge.InputHeld,
                0);
            Marshal.WriteByte(
                entry,
                NativeLayoutCatalog.SkyJudge.InputPressed,
                0);
            Marshal.WriteByte(
                entry,
                NativeLayoutCatalog.SkyJudge.InputConsumed,
                0);
            Marshal.WriteInt64(
                entry,
                NativeLayoutCatalog.SkyJudge.InputLastInput,
                0);
        }

        private static void LogError(
            string stage,
            Exception exception)
        {
            if (_errors++ >= 5)
                return;
            try
            {
                ChartExtMod.Log.Error(
                    $"[RdAutoplay] {stage} failed: " +
                    exception);
            }
            catch
            {
            }
        }
    }
}
