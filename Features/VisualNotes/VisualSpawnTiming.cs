using System;
using System.Runtime.InteropServices;

namespace InFalsusChartExt
{
    /// <summary>
    /// Shared visual-only spawn window. A positive lead is the time until a
    /// note reaches the judgement line in its visual timeline. The native
    /// 2000ms quick-window is a lower bound; low-speed tracks need a larger
    /// lead to remain visible while the chart is in pre-roll.
    /// </summary>
    internal static class VisualSpawnTiming
    {
        internal const double NativeQuickLeadMs = 2000.0;

        internal static bool IsFiniteTime(double value)
            => !double.IsNaN(value) &&
               !double.IsInfinity(value);

        internal static double MaxLeadMs(
            double baseSpeed,
            double depthFactor)
        {
            if (!IsFiniteTime(baseSpeed) ||
                !IsFiniteTime(depthFactor) ||
                Math.Abs(depthFactor) < 1e-9)
                return NativeQuickLeadMs;
            double dynamic = Math.Abs(
                baseSpeed / depthFactor) * 1000.0;
            if (!IsFiniteTime(dynamic) || dynamic < 0.0)
                return NativeQuickLeadMs;
            return Math.Max(
                NativeQuickLeadMs,
                dynamic);
        }

        internal static double MaxLeadMs(IntPtr track)
        {
            if (track == IntPtr.Zero)
                return NativeQuickLeadMs;
            double baseSpeed = BitConverter.Int64BitsToDouble(
                Marshal.ReadInt64(
                    track + NativeLayoutCatalog.Track.BaseSpeed));
            double depthFactor = BitConverter.Int64BitsToDouble(
                Marshal.ReadInt64(
                    track + NativeLayoutCatalog.Track.DepthFactor));
            return MaxLeadMs(baseSpeed, depthFactor);
        }
    }
}
