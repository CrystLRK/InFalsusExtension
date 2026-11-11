// Code organization: SkyBlock native slot binding. Runtime behavior is unchanged.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace InFalsusChartExt
{
    internal static class SkyBlockRuntimeSlots
    {
        private static readonly Dictionary<long, ChartExtState.SideSlot>
            SlotsById =
                new Dictionary<long, ChartExtState.SideSlot>(256);
        private static int _generation = -1;
        private static int _boundMembers = -1;

        internal static bool Ensure(SkyBlockPlan plan)
        {
            if (plan == null) return false;
            if (_generation == plan.Generation &&
                _boundMembers == plan.Members.Count)
                return true;
            if (ChartExtState.SkyAreaSlots.Count == 0)
                return false;

            int expected = plan.Members.Count;
            if (expected == 0) return true;
            if (_generation != plan.Generation)
            {
                SlotsById.Clear();
                _generation = plan.Generation;
                _boundMembers = -1;
            }

            if (SlotsById.Count == 0)
            {
                for (int i = 0;
                     i < ChartExtState.SkyAreaSlots.Count;
                     i++)
                {
                    ChartExtState.SideSlot slot =
                        ChartExtState.SkyAreaSlots[i];
                    if (slot != null)
                        SlotsById[slot.Id] = slot;
                }
            }

            for (int i = 0; i < plan.Members.Count; i++)
            {
                SkyBlockMember member = plan.Members[i];
                if (member.Side >= 0 && member.Position >= 0)
                    continue;
                if (!SlotsById.TryGetValue(
                        member.Id,
                        out ChartExtState.SideSlot slot))
                    return false;
                IntPtr basePtr =
                    NativeNoteSlotTable.SlotBase(slot.Side);
                if (basePtr == IntPtr.Zero) return false;
                IntPtr note = basePtr +
                    slot.Position *
                    NativeLayoutCatalog.NoteStride;
                if (Marshal.ReadInt64(
                        note + NativeLayoutCatalog.Note.Id) !=
                        member.Id ||
                    Marshal.ReadInt32(
                        note + NativeLayoutCatalog.Note.Type) != 5)
                    return false;
                member.Side = slot.Side;
                member.Position = slot.Position;
            }
            _boundMembers = plan.Members.Count;
            return true;
        }

        internal static bool TryNote(
            SkyBlockMember member, out IntPtr note)
        {
            note = IntPtr.Zero;
            if (member == null ||
                member.Side < 0 || member.Position < 0)
                return false;
            IntPtr basePtr =
                NativeNoteSlotTable.SlotBase(member.Side);
            if (basePtr == IntPtr.Zero) return false;
            IntPtr candidate = basePtr +
                member.Position *
                NativeLayoutCatalog.NoteStride;
            if (Marshal.ReadInt64(
                    candidate + NativeLayoutCatalog.Note.Id) !=
                    member.Id ||
                Marshal.ReadInt32(
                    candidate + NativeLayoutCatalog.Note.Type) != 5)
                return false;
            note = candidate;
            return true;
        }

        internal static void Reset()
        {
            SlotsById.Clear();
            _generation = -1;
            _boundMembers = -1;
        }
    }
}
