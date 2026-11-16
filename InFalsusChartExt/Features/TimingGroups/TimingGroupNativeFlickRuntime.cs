using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace InFalsusChartExt
{
    /// <summary>
    /// Native-path rendering for timinggroup visual note types.
    ///
    /// `_hb` builds the active-note dictionary, and Track.Update enumerates that
    /// same dictionary while using `_IA[id]` as the render time. Grouped Hold,
    /// Flick and SkyArea can therefore be rendered by the original pools:
    /// keep the original type, append its already-compiled slot to the active list
    /// when its group visual lead enters the native window, and let the native
    /// renderer handle parent, scale, fade, length and judgement lifecycle.
    /// Tap remains under TimingGroupVisualRuntime ownership because it is
    /// flipped to a non-rendering sentinel type before this injection point.
    /// </summary>
    internal static class TimingGroupNativeFlickRuntime
    {
        private static int _errors;
#if IFCE_DIAGNOSTICS
        private static readonly HashSet<long> DiagnosticInjected =
            new HashSet<long>();
        private static int _diagnosticGeneration = -1;
        private static int _diagnosticLogs;
#endif

        internal static void InjectSpawnList(
            Il2CppSystem.Collections.Generic.Dictionary<
                Il2Cpp_b._CA,
                Il2Cppifapp.Game.Common.UnorderedListVal<int>> lists)
        {
            try
            {
                if (lists == null || lists.Pointer == IntPtr.Zero) return;
                var track = Il2Cppifapp.Game.Scenes.Game.Track._a;
                if (track == null || track.Pointer == IntPtr.Zero) return;
                IntPtr tp = track.Pointer;
                double a = BitConverter.Int64BitsToDouble(
                    Marshal.ReadInt64(
                        tp + NativeLayoutCatalog.Track.BaseSpeed));
                double b = BitConverter.Int64BitsToDouble(
                    Marshal.ReadInt64(
                        tp + NativeLayoutCatalog.Track.DepthFactor));
                if (Math.Abs(a) < 1e-9 || Math.Abs(b) < 1e-9) return;
                double maxLeadMs = Math.Min(2000.0, Math.Abs(a / b) * 1000.0);
                double nowMs = TimingGroupRuntime.CurrentMs();
                if (double.IsNaN(nowMs)) return;
#if IFCE_DIAGNOSTICS
                if (_diagnosticGeneration !=
                    ChartExtState.ChartGeneration)
                {
                    _diagnosticGeneration =
                        ChartExtState.ChartGeneration;
                    DiagnosticInjected.Clear();
                    _diagnosticLogs = 0;
                }
#endif

                for (int i = 0; i < ChartExtState.GroupedSlots.Count; i++)
                {
                    ChartExtState.GroupSlot slot = ChartExtState.GroupedSlots[i];
                    if (slot.Type != 2 && slot.Type != 4 &&
                        slot.Type != 5)
                        continue;
                    if (!TimingGroupRuntime.TryResolveGroupedSlot(
                            slot, out IntPtr note, out _, false))
                        continue;
                    if (Marshal.ReadByte(
                            note + NativeLayoutCatalog.Note.Consumed) != 0)
                        continue;
                    int be = Marshal.ReadInt32(
                        note + NativeLayoutCatalog.Note.StartMs);
                    double visualLead = TimingGroupRuntime.DebugWGroup(slot.Group, be) -
                                        TimingGroupRuntime.DebugWGroup(slot.Group, nowMs);
                    if (visualLead < 0.0 ||
                        visualLead > maxLeadMs)
                        continue;

                    var key = (Il2Cpp_b._CA)slot.Side;
                    if (!lists.ContainsKey(key)) continue;
                    var list = lists[key];
                    if (list == null || list.Pointer == IntPtr.Zero) continue;
                    bool appended =
                        AppendUnique(list.Pointer, slot.Position);
#if IFCE_DIAGNOSTICS
                    long diagnosticKey =
                        ((long)(uint)slot.Side << 32) |
                        (uint)slot.Position;
                    if (appended &&
                        _diagnosticLogs < 32 &&
                        DiagnosticInjected.Add(diagnosticKey))
                    {
                        _diagnosticLogs++;
                        ChartExtMod.Log.Msg(
                            "[timing-spawn] " +
                            $"group={slot.Group} type={slot.Type} " +
                            $"side={slot.Side} pos={slot.Position} " +
                            $"visualLead={visualLead:F1}");
                    }
#endif
                }
            }
            catch (Exception e)
            {
                if (_errors++ < 20)
                    ChartExtMod.Log.Error("[TGNative] flick list injection failed: " +
                                          e.Message);
            }
        }

        private static bool AppendUnique(IntPtr list, int position)
        {
            IntPtr arr = Marshal.ReadIntPtr(
                list + NativeLayoutCatalog.Runtime.ListItems);
            int count = Marshal.ReadInt32(
                list + NativeLayoutCatalog.Runtime.ListCount);
            if (arr == IntPtr.Zero || count < 0) return false;
            int capacity = Marshal.ReadInt32(
                arr + NativeLayoutCatalog.ArrayLengthOffset);
            if (count > capacity) return false;
            IntPtr data = arr + NativeLayoutCatalog.ArrayDataOffset;
            for (int i = 0; i < count; i++)
                if (Marshal.ReadInt32(data + 4 * i) == position)
                    return false;
            if (count >= capacity) return false;
            Marshal.WriteInt32(data + 4 * count, position);
            Marshal.WriteInt32(
                list + NativeLayoutCatalog.Runtime.ListCount, count + 1);
            return true;
        }
    }
}
