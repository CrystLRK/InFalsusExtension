using System;
using System.Runtime.InteropServices;

namespace InFalsusChartExt
{
    /// <summary>
    /// hidegroup visual mode requested by the chart author: while a target
    /// timing group is hidden, keep the native SkyArea visible and switch only
    /// its color details to blue. Native judgement, glow, mesh updates and
    /// lifetime stay untouched. Noclip never enters this runtime.
    /// </summary>
    internal static class HideGroupSkyColorRuntime
    {
        private const uint HideBlueRgba = 0x3D7BFFFF;
        private static readonly int ColorDetailsOffset =
            NativeLayoutCatalog.SkySafeArea.ColorDetails;
        private static readonly int NativeNoteIdOffset =
            NativeLayoutCatalog.SkySafeArea.NativeNoteId;

        /// <summary>
        /// Diagnostic-only interface retained for future hidegroup inspection.
        /// Default gameplay keeps the native hide behavior. Enable through the
        /// HideGroupSkyColorDebug MelonPreferences entry and restart the game.
        /// </summary>
        internal static bool DebugColorEnabled { get; set; }

        private static int _errors;

        internal static void Tick(
            Il2Cppifapp.Game.Scenes.Game.Track track)
        {
            if (!BuildProfile.Supports(NativeCapability.HideGroup))
                return;
            if (!DebugColorEnabled ||
                ChartExtState.HideGroupControlledTimingGroups.Count == 0 ||
                track == null || track.Pointer == IntPtr.Zero ||
                track.safeAreaColorDetails == null ||
                track.safeAreaColorDetails.Pointer == IntPtr.Zero)
                return;
            try
            {
                double now = TimingGroupRuntime.CurrentMs();
                if (double.IsNaN(now)) return;
                IntPtr source = track.safeAreaColorDetails.Pointer;

                for (int i = 0; i < ChartExtState.SkyAreaSlots.Count; i++)
                {
                    ChartExtState.SideSlot slot =
                        ChartExtState.SkyAreaSlots[i];
                    IntPtr basePtr = NativeNoteSlotTable.SlotBase(slot.Side);
                    if (basePtr == IntPtr.Zero) continue;
                    IntPtr note = basePtr +
                        slot.Position * NativeLayoutCatalog.NoteStride;
                    if (Marshal.ReadInt64(
                            note + NativeLayoutCatalog.Note.Id) !=
                        slot.Id)
                        continue;
                    if (Marshal.ReadInt32(
                            note + NativeLayoutCatalog.Note.Type) != 5)
                        continue;
                    long id = Marshal.ReadInt64(
                        note + NativeLayoutCatalog.Note.Id);
                    int timingGroup =
                        ChartExtState.TimingGroupOf(id);
                    bool controlled =
                        ChartExtState.HideGroupControlledTimingGroups.Contains(
                            timingGroup);
                    bool hidden =
                        controlled &&
                        SceneControlRuntime.IsNoteHidden(id);
                    uint rgba = 0;
                    bool edgeGlow = true;
                    if (!hidden &&
                        ChartExtState.SkyAreaGroupById.TryGetValue(
                            id, out long skyGroup))
                    {
                        SkyColorRuntime.TryGetActiveColor(
                            skyGroup, now,
                            out rgba, out edgeGlow);
                    }
                    IntPtr desired = source;
                    if (hidden)
                    {
                        var clone = SkyColorRuntime.GetClone(
                            source, HideBlueRgba, true);
                        if (clone != null && clone.Pointer != IntPtr.Zero)
                            desired = clone.Pointer;
                    }
                    else if (rgba != 0)
                    {
                        var clone = SkyColorRuntime.GetClone(
                            source, rgba, edgeGlow);
                        if (clone != null && clone.Pointer != IntPtr.Zero)
                            desired = clone.Pointer;
                    }

                    ApplyPoolColor(track._UMA, id, desired);
                    ApplyPoolColor(track._vMA, id, desired);
                }
            }
            catch (Exception e)
            {
                if (_errors++ < 20)
                    ChartExtMod.Log.Error(
                        "[ChartExt] hidegroup sky color failed: " + e);
            }
        }

        private static void ApplyPoolColor(
            Il2Cpp_k._JG<long,
                Il2Cppifapp.Game.Scenes.Game.SkySafeArea> pool,
            long id, IntPtr details)
        {
            if (pool == null || pool.Pointer == IntPtr.Zero ||
                details == IntPtr.Zero)
                return;
            ApplyEntryColor(pool._QXA, id, details);
            ApplyEntryColor(pool._rXA, id, details);
        }

        private static void ApplyEntryColor(
            Il2CppSystem.Collections.Generic.Dictionary<
                long,
                Il2CppSystem.ValueTuple<
                    Il2Cpp_k._Sg,
                    UnityEngine.GameObject,
                    UnityEngine.Transform,
                    Il2Cppifapp.Game.Scenes.Game.SkySafeArea>> entries,
            long id, IntPtr details)
        {
            if (entries == null || entries.Pointer == IntPtr.Zero ||
                !entries.ContainsKey(id))
                return;
            var entry = entries[id];
            UnityEngine.GameObject go = entry.Item2;
            Il2Cppifapp.Game.Scenes.Game.SkySafeArea area = entry.Item4;
            if (go == null || go.Pointer == IntPtr.Zero ||
                !go.activeSelf ||
                area == null || area.Pointer == IntPtr.Zero)
                return;
            if (Marshal.ReadInt64(
                    area.Pointer + NativeNoteIdOffset) != id)
                return;
            if (Marshal.ReadIntPtr(area.Pointer + ColorDetailsOffset) ==
                details)
                return;
            Marshal.WriteIntPtr(
                area.Pointer + ColorDetailsOffset, details);
        }
    }
}
