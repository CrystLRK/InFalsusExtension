// Code organization: native note slot view. Runtime behavior is unchanged.
using System;
using System.Runtime.InteropServices;
using MelonLoader;

namespace InFalsusChartExt
{
    /// <summary>
    /// Read-only view over the game's per-side compiled note slot arrays.
    /// This is runtime infrastructure, not a diagnostic probe.
    /// </summary>
    internal static class NativeNoteSlotTable
    {
        internal static readonly int SideCount =
            NativeLayoutCatalog.SideCount;
        private static readonly IntPtr[] SideSlotBase = new IntPtr[SideCount];
        private static float _nextRefresh;
        private static int _errors;

        /// <summary>
        /// Address of slot 0 as the runtime Memory view sees it (0 when unknown).
        /// </summary>
        internal static IntPtr SlotBase(int side)
            => side >= 0 && side < SideCount ? SideSlotBase[side] : IntPtr.Zero;

        internal static void Refresh()
        {
            if (!BuildProfile.SupportsNativeRead)
            {
                Clear();
                return;
            }
            try
            {
                if (UnityEngine.Time.realtimeSinceStartup < _nextRefresh) return;
                _nextRefresh = UnityEngine.Time.realtimeSinceStartup + 0.25f;

                var player = Il2Cppifapp.Game.LogicalNotePlayer._Qe;
                if (player == null || player.Pointer == IntPtr.Zero)
                {
                    Clear();
                    return;
                }
                var dictionary = player._Hb();
                if (dictionary == null || dictionary.Pointer == IntPtr.Zero)
                {
                    Clear();
                    return;
                }

                for (int side = 0; side < SideCount; side++)
                {
                    var key = (Il2Cpp_b._CA)side;
                    if (!dictionary.ContainsKey(key))
                    {
                        SideSlotBase[side] = IntPtr.Zero;
                        continue;
                    }

                    var list = dictionary[key];
                    if (list == null || list.Pointer == IntPtr.Zero)
                    {
                        SideSlotBase[side] = IntPtr.Zero;
                        continue;
                    }

                    IntPtr hap = list.Pointer;
                    IntPtr array = Marshal.ReadIntPtr(
                        hap + NativeLayoutCatalog.CompiledSide.PositionArray);
                    IntPtr memoryObject = Marshal.ReadIntPtr(
                        hap + NativeLayoutCatalog.CompiledSide.MemoryObject);
                    int memoryIndex = Marshal.ReadInt32(
                        hap + NativeLayoutCatalog.CompiledSide.MemoryIndex);
                    IntPtr viewObject = memoryObject != IntPtr.Zero ? memoryObject : array;
                    int viewIndex = memoryObject != IntPtr.Zero ? memoryIndex : 0;
                    SideSlotBase[side] = viewObject == IntPtr.Zero
                        ? IntPtr.Zero
                        : viewObject + NativeLayoutCatalog.ArrayDataOffset +
                          viewIndex * NativeLayoutCatalog.NoteStride;
                }
            }
            catch (Exception e)
            {
                Clear();
                if (_errors++ < 10)
                    ChartExtMod.Log.Error("[ChartExt] note slot refresh failed: " + e.Message);
            }
        }

        internal static void ForceRefresh()
        {
            _nextRefresh = 0f;
            Refresh();
        }

        internal static void Reset()
        {
            _nextRefresh = 0f;
            Clear();
        }

        private static void Clear()
        {
            Array.Clear(SideSlotBase, 0, SideSlotBase.Length);
        }
    }

}
