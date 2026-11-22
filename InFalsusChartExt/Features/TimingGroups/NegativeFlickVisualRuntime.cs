using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace InFalsusChartExt
{
    /// <summary>
    /// Negative-speed timinggroup flicks are owned by the Mod for rendering:
    /// the native spawn list entry is removed (visual only), the native judge
    /// list still performs judgement, and a VisualFlickNoteInstance draws the
    /// flick with the signed group lead. The native Track.Update negates the
    /// lead before rendering, so negative leads cannot use the native path.
    /// </summary>
    internal static class NegativeFlickVisualRuntime
    {
        private sealed class Slot
        {
            internal long Id;
            internal int Side;
            internal int Position;
            internal int Group;
            internal uint StartMs;
            internal uint EndMs;
            internal uint Flags;
            internal double Center;
            internal double Width;
        }

        private static readonly List<Slot> Slots =
            new List<Slot>(32);
        private static readonly HashSet<long> Ids =
            new HashSet<long>();
        private static readonly Dictionary<long, VisualFlickNoteInstance>
            Instances =
                new Dictionary<long, VisualFlickNoteInstance>();
        private static int _generation = -1;
        private static int _errors;

        internal static void EnsureChart(
            Il2Cppifapp.Game.Scenes.Game.Track track)
        {
            int generation = ChartExtState.ChartGeneration;
            if (_generation == generation) return;
            DisposeAll();
            _generation = generation;
            Slots.Clear();
            Ids.Clear();
            if (track == null || track.Pointer == IntPtr.Zero)
                return;
            var grouped = ChartExtState.GroupedSlots;
            for (int i = 0; i < grouped.Count; i++)
            {
                ChartExtState.GroupSlot slot = grouped[i];
                if (slot.Type != 4 || slot.Group <= 0)
                    continue;
                if (!TimingGroupRuntime.TryResolveGroupedSlot(
                        slot, out IntPtr note, out _, false))
                    continue;
                int start = Marshal.ReadInt32(
                    note + NativeLayoutCatalog.Note.StartMs);
                int end = Marshal.ReadInt32(
                    note + NativeLayoutCatalog.Note.EndMs);
                double atStart = TimingGroupRuntime.DebugWGroup(
                    slot.Group, start);
                double before = TimingGroupRuntime.DebugWGroup(
                    slot.Group, start - 1.0);
                if (!(atStart < before))
                    continue;
                var item = new Slot
                {
                    Id = slot.Id,
                    Side = slot.Side,
                    Position = slot.Position,
                    Group = slot.Group,
                    StartMs = (uint)start,
                    EndMs = (uint)end,
                    Flags = (uint)Marshal.ReadInt32(
                        note + NativeLayoutCatalog.Note.Flags),
                    Center = ReadFloat(
                        note, NativeLayoutCatalog.Note.StartCenter),
                    Width = ReadFloat(
                        note, NativeLayoutCatalog.Note.StartWidth),
                };
                Slots.Add(item);
                Ids.Add(item.Id);
            }
        }

        internal static bool IsNegativeFlick(long id)
            => Ids.Contains(id);

        internal static void StripSpawnList(
            Il2CppSystem.Collections.Generic.Dictionary<
                Il2Cpp_b._CA,
                Il2Cppifapp.Game.Common.UnorderedListVal<int>> lists)
        {
            if (Ids.Count == 0 || lists == null ||
                lists.Pointer == IntPtr.Zero)
                return;
            try
            {
                for (int side = 0;
                     side < NativeLayoutCatalog.SideCount;
                     side++)
                {
                    var key = (Il2Cpp_b._CA)side;
                    if (!lists.ContainsKey(key)) continue;
                    var list = lists[key];
                    if (list == null ||
                        list.Pointer == IntPtr.Zero)
                        continue;
                    IntPtr lp = list.Pointer;
                    IntPtr arr = Marshal.ReadIntPtr(
                        lp + NativeLayoutCatalog.Runtime.ListItems);
                    int count = Marshal.ReadInt32(
                        lp + NativeLayoutCatalog.Runtime.ListCount);
                    if (arr == IntPtr.Zero || count <= 0)
                        continue;
                    int capacity = Marshal.ReadInt32(
                        arr +
                        NativeLayoutCatalog.ArrayLengthOffset);
                    if (count > capacity) continue;
                    IntPtr data =
                        arr + InteropUtil.ArrayDataOffset;
                    IntPtr basePtr =
                        NativeNoteSlotTable.SlotBase(side);
                    if (basePtr == IntPtr.Zero) continue;
                    int write = 0;
                    for (int read = 0; read < count; read++)
                    {
                        int position = Marshal.ReadInt32(
                            data + read * 4);
                        if (position < 0) continue;
                        IntPtr note = basePtr +
                            position *
                            NativeLayoutCatalog.NoteStride;
                        long id = Marshal.ReadInt64(
                            note + NativeLayoutCatalog.Note.Id);
                        int type = Marshal.ReadInt32(
                            note + NativeLayoutCatalog.Note.Type);
                        if (type == 4 && Ids.Contains(id))
                            continue;
                        if (write != read)
                            Marshal.WriteInt32(
                                data + write * 4, position);
                        write++;
                    }
                    if (write != count)
                        Marshal.WriteInt32(
                            lp +
                            NativeLayoutCatalog.Runtime.ListCount,
                            write);
                }
            }
            catch (Exception e)
            {
                if (_errors++ < 20)
                    ChartExtMod.Log.Error(
                        "[ChartExt] negative flick strip failed: " +
                        e);
            }
        }

        internal static void Tick(
            Il2Cppifapp.Game.Scenes.Game.Track track)
        {
            if (Slots.Count == 0 ||
                track == null || track.Pointer == IntPtr.Zero)
                return;
            if (!VisualTrackFrame.TryCapture(
                    track, out VisualTrackFrame frame) ||
                !frame.IsValid)
                return;
            for (int i = 0; i < Slots.Count; i++)
            {
                Slot slot = Slots[i];
                double lead = frame.Lead(
                    slot.Group, slot.StartMs);
                bool hidden =
                    SceneControlRuntime.IsNoteHidden(slot.Id);
                bool active = !hidden &&
                    lead >= -frame.MaxLeadMs &&
                    lead <= 1500.0;
                if (active)
                {
                    if (!Instances.TryGetValue(
                            slot.Id, out VisualFlickNoteInstance instance) ||
                        instance == null)
                    {
                        instance = Create(slot, frame);
                        if (instance == null) continue;
                        Instances[slot.Id] = instance;
                    }
                    instance.Update(frame);
                }
                else if (Instances.TryGetValue(
                             slot.Id, out VisualFlickNoteInstance hiddenInstance) &&
                         hiddenInstance != null)
                {
                    hiddenInstance.SetHidden(true);
                    if (lead < -2000.0)
                    {
                        hiddenInstance.Dispose();
                        Instances.Remove(slot.Id);
                    }
                }
            }
        }

        private static VisualFlickNoteInstance Create(
            Slot slot, in VisualTrackFrame frame)
        {
            try
            {
                var descriptor = new VisualNoteDescriptor
                {
                    VisualId = slot.Id,
                    SourceId = slot.Id,
                    Kind = VisualNoteKind.Flick,
                    Lifetime = VisualNoteLifetime.NoInput,
                    TimingGroup = slot.Group,
                    Group = slot.Group,
                    Side = (uint)slot.Side,
                    StartMs = slot.StartMs,
                    EndMs = slot.EndMs,
                    Flags = slot.Flags,
                    StartCenter = slot.Center,
                    StartWidth = slot.Width,
                    EndCenter = slot.Center,
                    EndWidth = slot.Width,
                };
                return new VisualFlickNoteInstance(
                    descriptor, frame);
            }
            catch (Exception e)
            {
                if (_errors++ < 20)
                    ChartExtMod.Log.Error(
                        "[ChartExt] negative flick create failed: " +
                        e);
                return null;
            }
        }

        internal static void ResetForChartSwitch()
        {
            DisposeAll();
            Slots.Clear();
            Ids.Clear();
            _generation = -1;
        }

        private static void DisposeAll()
        {
            foreach (VisualFlickNoteInstance instance
                     in Instances.Values)
            {
                try { instance.Dispose(); } catch { }
            }
            Instances.Clear();
        }

        private static double ReadFloat(
            IntPtr note, int offset) =>
            BitConverter.Int32BitsToSingle(
                Marshal.ReadInt32(note + offset));
    }
}
