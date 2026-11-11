using System;
using System.Runtime.InteropServices;

namespace InFalsusChartExt
{
    /// <summary>
    /// Native-path rendering for timinggroup visual note types.
    ///
    /// `_hb` builds the active-note dictionary, and Track.Update enumerates that
    /// same dictionary while using `_IA[id]` as the render time. A grouped flick
    /// can therefore be rendered by the original Track/FlickNoteV3/HoldNote pools:
    /// keep the original type, append its already-compiled slot to the active list
    /// when its group visual lead enters the native window, and let the native
    /// renderer handle parent, scale, fade, length and judgement lifecycle.
    /// </summary>
    internal static class TimingGroupNativeFlickRuntime
    {
        private static int _errors;

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

                for (int i = 0; i < ChartExtState.GroupedSlots.Count; i++)
                {
                    ChartExtState.GroupSlot slot = ChartExtState.GroupedSlots[i];
                    if (slot.Type != 2 && slot.Type != 4 && slot.Type != 5)
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
                    AppendUnique(list.Pointer, slot.Position, visualLead);
                }
            }
            catch (Exception e)
            {
                if (_errors++ < 20)
                    ChartExtMod.Log.Error("[TGNative] flick list injection failed: " +
                                          e.Message);
            }
        }

        private static void AppendUnique(IntPtr list, int position, double lead)
        {
            IntPtr arr = Marshal.ReadIntPtr(
                list + NativeLayoutCatalog.Runtime.ListItems);
            int count = Marshal.ReadInt32(
                list + NativeLayoutCatalog.Runtime.ListCount);
            if (arr == IntPtr.Zero || count < 0) return;
            int capacity = Marshal.ReadInt32(
                arr + NativeLayoutCatalog.ArrayLengthOffset);
            if (count > capacity) return;
            IntPtr data = arr + NativeLayoutCatalog.ArrayDataOffset;
            for (int i = 0; i < count; i++)
                if (Marshal.ReadInt32(data + 4 * i) == position) return;
            if (count >= capacity) return;
            Marshal.WriteInt32(data + 4 * count, position);
            Marshal.WriteInt32(
                list + NativeLayoutCatalog.Runtime.ListCount, count + 1);
        }
    }
}
