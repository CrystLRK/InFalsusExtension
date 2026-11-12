using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace InFalsusChartExt
{
    /// <summary>
    /// Removes native time-boundary caps inside connected normal skyarea runs
    /// when the connected end faces only partially overlap.
    ///
    /// The native renderer computes a partial missing range from the visible
    /// intersection. This layer extends that rule to normal runs by
    /// hiding the envelope of all connected intersections at each endpoint.
    /// Exposed portions remain visible and no synthetic boundary is created.
    /// Outer group boundaries, geometry, timing, judgment and score are
    /// untouched.
    /// </summary>
    internal static class NormalSkyInternalEdgeRuntime
    {
        private const int JoinTimeToleranceMs = 2;
        private const float JoinSpaceTolerance = 1e-4f;
        private const float RangeTolerance = 1e-4f;

        private sealed class Entry
        {
            public int Side;
            public int Position;
            public long Id;
            public bool HasPrevious;
            public bool HasNext;
            public Vector2 PreviousRange;
            public Vector2 NextRange;
        }

        private sealed class NoteInfo
        {
            public int Side;
            public int Position;
            public long Id;
            public int TimingGroup;
            public long Group;
            public int StartMs;
            public int EndMs;
            public float StartLo;
            public float StartHi;
            public float EndLo;
            public float EndHi;
        }

        private static readonly List<Entry> Entries = new List<Entry>();
        private static readonly int MissingRangeId =
            Shader.PropertyToID("_MissingRange");
        private static readonly MaterialPropertyBlock Block =
            new MaterialPropertyBlock();
        private static readonly MaterialPropertyBlock ClearBlock =
            new MaterialPropertyBlock();
        private static readonly HashSet<IntPtr> OwnedRenderers =
            new HashSet<IntPtr>();
        private static int _generation = -1;
        private static int _pendingGeneration = -1;
        private static ulong _pendingSignature;
        private static int _pendingStableFrames;
        private static int _errors;

        internal static void Tick(Il2Cppifapp.Game.Scenes.Game.Track track)
        {
            if (!BuildProfile.Supports(NativeCapability.NormalSkyEdge))
                return;
            try
            {
                if (track == null || track.Pointer == IntPtr.Zero)
                    return;
                bool ready = EnsureChart(track);
                ClearOwnedRenderers();
                if (!ready || Entries.Count == 0) return;

                for (int i = 0; i < Entries.Count; i++)
                {
                    Entry entry = Entries[i];
                    IntPtr basePtr = NativeNoteSlotTable.SlotBase(entry.Side);
                    if (basePtr == IntPtr.Zero) continue;
                    IntPtr notePtr = basePtr +
                        entry.Position * NativeLayoutCatalog.NoteStride;
                    if (Marshal.ReadInt64(notePtr) != entry.Id) continue;

                    Vector4 ranges = new Vector4(0f, 1f, 0f, 1f);
                    if (entry.HasPrevious)
                    {
                        ranges.x = entry.PreviousRange.x;
                        ranges.y = entry.PreviousRange.y;
                    }
                    if (entry.HasNext)
                    {
                        ranges.z = entry.NextRange.x;
                        ranges.w = entry.NextRange.y;
                    }
                    ApplyPool(track._UMA, entry.Id, ranges);
                    ApplyPool(track._vMA, entry.Id, ranges);
                }
            }
            catch (Exception e)
            {
                if (_errors++ < 20)
                    ChartExtMod.Log.Error(
                        "[ChartExt] normal sky internal edge failed: " + e);
            }
        }

        private static bool EnsureChart(
            Il2Cppifapp.Game.Scenes.Game.Track track)
        {
            int generation = ChartExtState.ChartGeneration;
            if (_generation == generation)
                return true;
            if (_pendingGeneration != generation)
            {
                _pendingGeneration = generation;
                _pendingSignature = 0;
                _pendingStableFrames = 0;
                Entries.Clear();
            }
            // SkyAreaSlots are filled by LogicalNotePlayer._Ib after the chart
            // generation has already advanced. Wait for the complete compile
            // snapshot instead of caching an empty chart.
            if (ChartExtState.SkyAreaSlots.Count == 0)
                return false;
            if (ChartExtState.ExpectedSkyAreaCount > 0 &&
                ChartExtState.SkyAreaSlots.Count !=
                    ChartExtState.ExpectedSkyAreaCount)
                return false;
            for (int i = 0; i < ChartExtState.SkyAreaSlots.Count; i++)
            {
                ChartExtState.SideSlot slot = ChartExtState.SkyAreaSlots[i];
                IntPtr basePtr = NativeNoteSlotTable.SlotBase(slot.Side);
                if (basePtr == IntPtr.Zero) return false;
                IntPtr notePtr = basePtr +
                    slot.Position * NativeLayoutCatalog.NoteStride;
                if (Marshal.ReadInt64(notePtr) != slot.Id ||
                    Marshal.ReadInt32(
                        notePtr + NativeLayoutCatalog.Note.Type) != 5)
                    return false;
            }
            ulong signature = SlotSignature();
            if (signature != _pendingSignature)
            {
                _pendingSignature = signature;
                _pendingStableFrames = 0;
                return false;
            }
            if (++_pendingStableFrames < 2)
                return false;
            Entries.Clear();

            var groups = new Dictionary<(int, long), List<NoteInfo>>();
            SkyBlockPlan blockPlan =
                ChartRuntimeContext.IsActive
                    ? ChartRuntimeContext.Current?.SkyBlocks
                    : null;
            for (int i = 0; i < ChartExtState.SkyAreaSlots.Count; i++)
            {
                ChartExtState.SideSlot slot =
                    ChartExtState.SkyAreaSlots[i];
                IntPtr basePtr = NativeNoteSlotTable.SlotBase(slot.Side);
                if (basePtr == IntPtr.Zero) continue;
                IntPtr notePtr = basePtr +
                    slot.Position * NativeLayoutCatalog.NoteStride;
                long id = Marshal.ReadInt64(notePtr);
                if (blockPlan != null &&
                    blockPlan.IsVisualMember(id))
                    continue;
                if (Marshal.ReadInt32(
                        notePtr + NativeLayoutCatalog.Note.Type) != 5)
                    continue;

                float startCenter = ReadFloat(
                    notePtr + NativeLayoutCatalog.Note.StartCenter);
                float endCenter = ReadFloat(
                    notePtr + NativeLayoutCatalog.Note.EndCenter);
                float startWidth = Math.Abs(ReadFloat(
                    notePtr + NativeLayoutCatalog.Note.StartWidth));
                float endWidth = Math.Abs(ReadFloat(
                    notePtr + NativeLayoutCatalog.Note.EndWidth));
                var info = new NoteInfo
                {
                    Side = slot.Side,
                    Position = slot.Position,
                    Id = id,
                    TimingGroup = ChartExtState.TimingGroupOf(id),
                    Group = Marshal.ReadInt64(
                        notePtr + NativeLayoutCatalog.Note.Group),
                    StartMs = Marshal.ReadInt32(
                        notePtr + NativeLayoutCatalog.Note.StartMs),
                    EndMs = Marshal.ReadInt32(
                        notePtr + NativeLayoutCatalog.Note.EndMs),
                    StartLo = startCenter - startWidth * 0.5f,
                    StartHi = startCenter + startWidth * 0.5f,
                    EndLo = endCenter - endWidth * 0.5f,
                    EndHi = endCenter + endWidth * 0.5f,
                };
                var key = (info.TimingGroup, info.Group);
                if (!groups.TryGetValue(key, out var list))
                {
                    list = new List<NoteInfo>();
                    groups[key] = list;
                }
                list.Add(info);
            }

            var previous = new HashSet<long>();
            var next = new HashSet<long>();
            var previousRange = new Dictionary<long, Vector2>();
            var nextRange = new Dictionary<long, Vector2>();
            foreach (var list in groups.Values)
            {
                list.Sort((a, b) =>
                {
                    int c = a.StartMs.CompareTo(b.StartMs);
                    return c != 0 ? c : a.EndMs.CompareTo(b.EndMs);
                });
                var tailsByEnd =
                    new Dictionary<int, List<NoteInfo>>();
                for (int i = 0; i < list.Count; i++)
                {
                    NoteInfo current = list[i];
                    for (int delta = -JoinTimeToleranceMs;
                         delta <= JoinTimeToleranceMs;
                         delta++)
                    {
                        if (!tailsByEnd.TryGetValue(
                                current.StartMs + delta,
                                out List<NoteInfo> candidates))
                            continue;
                        for (int ci = 0; ci < candidates.Count; ci++)
                        {
                            NoteInfo earlier = candidates[ci];
                            if (earlier.StartMs >= current.StartMs)
                                continue;
                            float lo = Math.Max(
                                earlier.EndLo, current.StartLo);
                            float hi = Math.Min(
                                earlier.EndHi, current.StartHi);
                            if (lo > hi + JoinSpaceTolerance)
                                continue;
                            if (lo > hi)
                            {
                                float contact = (lo + hi) * 0.5f;
                                lo = contact;
                                hi = contact;
                            }
                            previous.Add(current.Id);
                            next.Add(earlier.Id);
                            AddEnvelope(
                                previousRange, current.Id, lo, hi);
                            AddEnvelope(
                                nextRange, earlier.Id, lo, hi);
                        }
                    }
                    if (!tailsByEnd.TryGetValue(
                            current.EndMs, out List<NoteInfo> tails))
                    {
                        tails = new List<NoteInfo>(2);
                        tailsByEnd[current.EndMs] = tails;
                    }
                    tails.Add(current);
                }
            }

            foreach (var list in groups.Values)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    NoteInfo info = list[i];
                    bool hasPrevious = previous.Contains(info.Id);
                    bool hasNext = next.Contains(info.Id);
                    if (!hasPrevious && !hasNext) continue;
                    Vector2 previousEndpoint = hasPrevious
                        ? previousRange[info.Id]
                        : new Vector2(0f, 1f);
                    Vector2 nextEndpoint = hasNext
                        ? nextRange[info.Id]
                        : new Vector2(0f, 1f);
                    bool previousPartial =
                        hasPrevious &&
                        !CoversEndpoint(
                            previousEndpoint,
                            info.StartLo,
                            info.StartHi);
                    bool nextPartial =
                        hasNext &&
                        !CoversEndpoint(
                            nextEndpoint,
                            info.EndLo,
                            info.EndHi);
                    if (!previousPartial && !nextPartial)
                        continue;
                    Entries.Add(new Entry
                    {
                        Side = info.Side,
                        Position = info.Position,
                        Id = info.Id,
                        HasPrevious = hasPrevious,
                        HasNext = hasNext,
                        PreviousRange = previousEndpoint,
                        NextRange = nextEndpoint,
                    });
                }
            }
            _generation = generation;
            _pendingSignature = 0;
            _pendingStableFrames = 0;
            return true;
        }

        private static void AddEnvelope(
            Dictionary<long, Vector2> ranges,
            long id,
            float lo,
            float hi)
        {
            if (!ranges.TryGetValue(id, out Vector2 current))
            {
                ranges[id] = new Vector2(lo, hi);
                return;
            }
            ranges[id] = new Vector2(
                Math.Min(current.x, lo),
                Math.Max(current.y, hi));
        }

        private static bool CoversEndpoint(
            Vector2 range,
            float lo,
            float hi)
        {
            return range.x <= lo + RangeTolerance &&
                   range.y >= hi - RangeTolerance;
        }

        internal static void ResetForChartSwitch()
        {
            ClearOwnedRenderers();
            Entries.Clear();
            _generation = -1;
            _pendingGeneration = -1;
            _pendingSignature = 0;
            _pendingStableFrames = 0;
        }

        private static ulong SlotSignature()
        {
            ulong hash = 1469598103934665603UL;
            hash = MixHash(hash, ChartExtState.SkyAreaSlots.Count);
            for (int i = 0; i < ChartExtState.SkyAreaSlots.Count; i++)
            {
                ChartExtState.SideSlot slot = ChartExtState.SkyAreaSlots[i];
                IntPtr basePtr = NativeNoteSlotTable.SlotBase(slot.Side);
                hash = MixHash(hash, slot.Side);
                hash = MixHash(hash, slot.Position);
                if (basePtr == IntPtr.Zero) continue;
                IntPtr notePtr = basePtr +
                    slot.Position * NativeLayoutCatalog.NoteStride;
                hash = MixHash(hash, Marshal.ReadInt64(notePtr));
                hash = MixHash(hash, Marshal.ReadInt32(
                    notePtr + NativeLayoutCatalog.Note.Type));
            }
            return hash;
        }

        private static ulong MixHash(ulong hash, long value)
        {
            hash ^= unchecked((ulong)value);
            return hash * 1099511628211UL;
        }

        private static float ReadFloat(IntPtr address)
        {
            return BitConverter.Int32BitsToSingle(
                Marshal.ReadInt32(address));
        }

        private static void ApplyPool(
            Il2Cpp_k._JG<long,
                Il2Cppifapp.Game.Scenes.Game.SkySafeArea> pool,
            long id, Vector4 ranges)
        {
            if (pool == null || pool.Pointer == IntPtr.Zero) return;
            ApplyEntries(pool._QXA, id, ranges);
            ApplyEntries(pool._rXA, id, ranges);
        }

        private static void ApplyEntries(
            Il2CppSystem.Collections.Generic.Dictionary<
                long,
                Il2CppSystem.ValueTuple<
                    Il2Cpp_k._Sg,
                    UnityEngine.GameObject,
                    UnityEngine.Transform,
                    Il2Cppifapp.Game.Scenes.Game.SkySafeArea>> entries,
            long id, Vector4 ranges)
        {
            try
            {
                if (entries == null ||
                    entries.Pointer == IntPtr.Zero ||
                    !entries.ContainsKey(id))
                    return;
                Il2Cppifapp.Game.Scenes.Game.SkySafeArea area =
                    entries[id].Item4;
                if (area == null || area.Pointer == IntPtr.Zero)
                    return;
                if (Marshal.ReadInt64(
                        area.Pointer +
                        NativeLayoutCatalog.SkySafeArea.NativeNoteId) != id)
                    return;
                MeshRenderer renderer = area.meshRenderer;
                if (renderer == null ||
                    renderer.Pointer == IntPtr.Zero)
                    return;
                Block.Clear();
                Block.SetVector(MissingRangeId, ranges);
                renderer.SetPropertyBlock(Block);
                OwnedRenderers.Add(renderer.Pointer);
            }
            catch { }
        }

        private static void ClearOwnedRenderers()
        {
            foreach (IntPtr pointer in OwnedRenderers)
            {
                try
                {
                    var renderer = new MeshRenderer(pointer);
                    renderer.SetPropertyBlock(ClearBlock);
                }
                catch { }
            }
            OwnedRenderers.Clear();
        }
    }
}
