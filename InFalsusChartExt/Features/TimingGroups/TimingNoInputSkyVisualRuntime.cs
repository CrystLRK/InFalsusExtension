using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

namespace InFalsusChartExt
{
    /// <summary>
    /// Independent renderer for timinggroup(noinput) sky areas.
    ///
    /// These areas never enter the native sky spawn/judge pools. The runtime
    /// borrows only the native prefab, material, mesh builder and time geometry
    /// formulas. Normal sky areas remain entirely on the original code path.
    /// </summary>
    internal static class TimingNoInputSkyVisualRuntime
    {
        private const double BracketLeadMinMs = 5.0;
        private const double JoinTimeToleranceMs = 2.0;
        private const float JoinSpaceTolerance = 1e-9f;

        private sealed class Entry
        {
            public long Id;
            public long AuthoredGroup;
            public GameObject Band;
            public GameObject Shadow;
            public GameObject LeftBracket;
            public GameObject RightBracket;
            public Il2Cppifapp.Game.Scenes.Game.SkySafeArea BandArea;
            public Il2Cppifapp.Game.Scenes.Game.SkySafeArea ShadowArea;
            public Il2Cppifapp.Game.Scenes.Game.SafeAreaColorDetails BaseColorDetails;
            public bool IsHead;
            public bool HeadInBounds;
            public Vector2 PreviousRange = new Vector2(0f, 1f);
            public Vector2 NextRange = new Vector2(0f, 1f);
            public int SeenFrame = -1;
            public readonly List<Material> OwnedMaterials =
                new List<Material>(4);
        }

        private sealed class RangeInfo
        {
            public long Id;
            public long Group;
            public int TimingGroup;
            public VisualNoteLifetime Lifetime;
            public int StartMs;
            public int EndMs;
            public float StartLo;
            public float StartHi;
            public float EndLo;
            public float EndHi;
            public bool IsHead;
            public bool IsFirst = true;
            public bool IsLast = true;
            public Vector2 PreviousRange = new Vector2(0f, 1f);
            public Vector2 NextRange = new Vector2(0f, 1f);
            public readonly List<Vector2> PreviousRanges =
                new List<Vector2>(2);
            public readonly List<Vector2> NextRanges =
                new List<Vector2>(2);
            public VisualNoteDescriptor Descriptor;
        }

        private static readonly Dictionary<long, Entry> Entries =
            new Dictionary<long, Entry>();
        private static readonly Dictionary<long, RangeInfo> Ranges =
            new Dictionary<long, RangeInfo>();
        private static readonly List<RangeInfo> OrderedRanges =
            new List<RangeInfo>();
        private static readonly List<long> DeadIds = new List<long>();
        private static int _generation = -1;
        private static int _errors;

        internal static unsafe void Tick(
            Il2Cppifapp.Game.Scenes.Game.Track track)
        {
            if (!BuildProfile.Supports(NativeCapability.VisualNotes))
                return;
            try
            {
                if (track == null || track.Pointer == IntPtr.Zero) return;
                if (TimingGroupRuntime.IsResumeTransition()) return;
                if (!EnsureChart(track)) return;

                double now = TimingGroupRuntime.CurrentMs();
                if (double.IsNaN(now)) return;
                double maxLeadMs =
                    VisualSpawnTiming.MaxLeadMs(
                        track.Pointer);
                double noClipTailMs =
                    TimingGroupRuntime.NoClipTailMs();
                if (track.safeAreaPrefab == null ||
                    track.safeAreaPrefab.Pointer == IntPtr.Zero)
                    return;

                int frame = Time.frameCount;
                for (int i = 0; i < OrderedRanges.Count; i++)
                {
                    RangeInfo info = OrderedRanges[i];
                    long visualId = info.Id;
                    bool noClip =
                        info.Lifetime == VisualNoteLifetime.NoClip;
                    // Noclip owns its complete independent visual lifetime.
                    // It must not inherit hidegroup visibility/removal state.
                    if (!noClip &&
                        SceneControlRuntime.IsTimingGroupHidden(
                            info.TimingGroup))
                    {
                        Hide(visualId);
                        continue;
                    }

                    int timingGroup = info.TimingGroup;
                    if (!TryGetGroupMotion(
                            timingGroup, now, info.StartMs, info.EndMs,
                            out double leadMs,
                            out double durationMs,
                            out double progress))
                        continue;
                    if (noClip)
                    {
                        // Noclip disables judgement-line consumption entirely.
                        // Keeping progress at 1 would collapse the mesh to a
                        // nearly empty line and visually equal disappearance.
                        progress = 0.0;
                    }
                    bool pastVisibleTail = noClip &&
                        leadMs < -noClipTailMs;
                    if (leadMs > maxLeadMs ||
                        (!noClip && progress >= 1.0) ||
                        pastVisibleTail)
                    {
                        Hide(visualId);
                        continue;
                    }

                    Il2Cpp_b._fA note =
                        BuildRenderNote(info, leadMs);
                    if (!Entries.TryGetValue(visualId, out Entry entry))
                    {
                        entry = CreateEntry(track, visualId);
                        if (entry == null) continue;
                        entry.IsHead = info.IsFirst;
                        entry.HeadInBounds =
                            IsBracketHeadInBounds(
                                info.StartLo, info.StartHi);
                        entry.AuthoredGroup = info.Group;
                        entry.PreviousRange = info.PreviousRange;
                        entry.NextRange = info.NextRange;
                        Entries[visualId] = entry;
                    }
                    entry.SeenFrame = frame;
                    UpdateEntry(track, entry, ref note, now,
                        leadMs, durationMs, progress);
                    UpdateBrackets(
                        track, entry, ref note, leadMs,
                        maxLeadMs);
                }

                DeadIds.Clear();
                foreach (Entry entry in Entries.Values)
                    if (entry.SeenFrame != frame) DeadIds.Add(entry.Id);
                for (int i = 0; i < DeadIds.Count; i++)
                {
                    long id = DeadIds[i];
                    Hide(id);
                    DestroyEntry(Entries[id]);
                    Entries.Remove(id);
                }
            }
            catch (Exception e)
            {
                if (_errors++ < 20)
                    ChartExtMod.Log.Error(
                        "[ChartExt] noinput sky visual tick failed: " + e);
            }
        }

        internal static bool TryGetGroupMotion(
            int timingGroup,
            double nowMs,
            double startMs,
            double endMs,
            out double leadMs,
            out double durationMs,
            out double progress)
        {
            double visualNow = TimingGroupRuntime.VisualTimeMs(
                timingGroup, nowMs);
            double visualStart = TimingGroupRuntime.VisualTimeMs(
                timingGroup, startMs);
            double visualEnd = TimingGroupRuntime.VisualTimeMs(
                timingGroup, endMs);
            leadMs = 0.0;
            durationMs = 0.0;
            progress = 0.0;
            if (double.IsNaN(visualNow) ||
                double.IsNaN(visualStart) ||
                double.IsNaN(visualEnd) ||
                visualEnd <= visualStart)
                return false;

            // The scroll lead belongs entirely to this timing group.
            // UpdateEntry applies the existing display scale; global time
            // normalization here would create a one-frame position jump when
            // the main timeline changes speed.
            leadMs = visualStart - visualNow;
            durationMs = visualEnd - visualStart;
            progress = Math.Max(0.0, Math.Min(
                1.0,
                (visualNow - visualStart) /
                (visualEnd - visualStart)));
            return true;
        }

        private static bool EnsureChart(
            Il2Cppifapp.Game.Scenes.Game.Track track)
        {
            ChartRuntimeFrame frame = ChartRuntimeContext.Current;
            if (!ChartRuntimeContext.IsActive ||
                frame == null || frame.VisualChart == null)
                return false;
            int generation = frame.Generation;
            if (_generation == generation)
                return true;

            DestroyAll();
            Ranges.Clear();
            OrderedRanges.Clear();
            BuildRanges();
            _generation = generation;
            return Ranges.Count > 0;
        }

        internal static void ResetForChartSwitch()
        {
            DestroyAll();
            Ranges.Clear();
            OrderedRanges.Clear();
            DeadIds.Clear();
            _generation = -1;
        }

        private static void BuildRanges()
        {
            Ranges.Clear();
            OrderedRanges.Clear();
            ChartRuntimeFrame frame = ChartRuntimeContext.Current;
            if (frame?.VisualChart == null) return;

            for (int i = 0; i < frame.VisualChart.Notes.Count; i++)
            {
                VisualNoteDescriptor descriptor =
                    frame.VisualChart.Notes[i];
                if (descriptor == null ||
                    descriptor.Kind != VisualNoteKind.SkyArea)
                    continue;
                var info = new RangeInfo
                {
                    Id = descriptor.VisualId,
                    Group = descriptor.Group & 0xFFFFFFFFL,
                    TimingGroup = descriptor.TimingGroup,
                    Lifetime = descriptor.Lifetime,
                    StartMs = (int)descriptor.StartMs,
                    EndMs = (int)descriptor.EndMs,
                    StartLo = (float)(
                        descriptor.StartCenter -
                        descriptor.StartWidth * 0.5),
                    StartHi = (float)(
                        descriptor.StartCenter +
                        descriptor.StartWidth * 0.5),
                    EndLo = (float)(
                        descriptor.EndCenter -
                        descriptor.EndWidth * 0.5),
                    EndHi = (float)(
                        descriptor.EndCenter +
                        descriptor.EndWidth * 0.5),
                    Descriptor = descriptor,
                };
                OrderedRanges.Add(info);
                Ranges[info.Id] = info;
            }
            OrderedRanges.Sort((a, b) =>
            {
                int c = a.StartMs.CompareTo(b.StartMs);
                if (c != 0) return c;
                c = a.EndMs.CompareTo(b.EndMs);
                return c != 0 ? c : a.Id.CompareTo(b.Id);
            });

            var tailsByEndBucket = new Dictionary<long, List<int>>();
            var edges = new List<(int Earlier, int Later, float Lo,
                float Hi)>();
            for (int i = 0; i < OrderedRanges.Count; i++)
            {
                RangeInfo current = OrderedRanges[i];
                long firstBucket = (long)Math.Floor(
                    (current.StartMs - JoinTimeToleranceMs) /
                    JoinTimeToleranceMs);
                long lastBucket = (long)Math.Floor(
                    (current.StartMs + JoinTimeToleranceMs) /
                    JoinTimeToleranceMs);
                for (long bucket = firstBucket;
                     bucket <= lastBucket;
                     bucket++)
                {
                    if (!tailsByEndBucket.TryGetValue(
                            bucket, out List<int> candidates))
                        continue;
                    for (int ci = 0; ci < candidates.Count; ci++)
                    {
                        int previousIndex = candidates[ci];
                        RangeInfo previous =
                            OrderedRanges[previousIndex];
                        if (previous.Lifetime != current.Lifetime)
                            continue;
                        if (previous.StartMs >= current.StartMs)
                            continue;
                        if (Math.Abs(
                                (long)current.StartMs -
                                previous.EndMs) >
                            JoinTimeToleranceMs)
                            continue;
                        float lo = Math.Max(
                            previous.EndLo, current.StartLo);
                        float hi = Math.Min(
                            previous.EndHi, current.StartHi);
                        if (lo > hi + JoinSpaceTolerance)
                            continue;
                        if (lo > hi)
                        {
                            float contact = (lo + hi) * 0.5f;
                            lo = contact;
                            hi = contact;
                        }
                        edges.Add((previousIndex, i, lo, hi));
                    }
                }

                long endBucket = (long)Math.Floor(
                    current.EndMs / JoinTimeToleranceMs);
                if (!tailsByEndBucket.TryGetValue(
                        endBucket, out List<int> endList))
                    tailsByEndBucket[endBucket] =
                        endList = new List<int>(4);
                endList.Add(i);
            }

            var parent = new int[OrderedRanges.Count];
            for (int i = 0; i < parent.Length; i++)
                parent[i] = i;
            for (int i = 0; i < edges.Count; i++)
            {
                int a = Find(parent, edges[i].Earlier);
                int b = Find(parent, edges[i].Later);
                if (a != b) parent[b] = a;
            }

            for (int i = 0; i < edges.Count; i++)
            {
                if (Find(parent, edges[i].Earlier) !=
                    Find(parent, edges[i].Later))
                    continue;
                RangeInfo previous =
                    OrderedRanges[edges[i].Earlier];
                RangeInfo current =
                    OrderedRanges[edges[i].Later];
                previous.IsLast = false;
                current.IsFirst = false;
                AddRange(
                    previous.NextRanges,
                    new Vector2(edges[i].Lo, edges[i].Hi));
                AddRange(
                    current.PreviousRanges,
                    new Vector2(edges[i].Lo, edges[i].Hi));
            }

            // A same-authored-group continuation still suppresses the next
            // head even when its geometry is disconnected. It remains a
            // separate runtime block and therefore gets an independent head.
            var lastByAuthoredGroup =
                new Dictionary<(VisualNoteLifetime, long),
                    RangeInfo>();
            for (int i = 0; i < OrderedRanges.Count; i++)
            {
                RangeInfo current = OrderedRanges[i];
                var key = (current.Lifetime, current.Group);
                if (lastByAuthoredGroup.TryGetValue(
                        key, out RangeInfo previous) &&
                    previous.StartMs < current.StartMs &&
                    Math.Abs(
                        (long)current.StartMs -
                        previous.EndMs) <=
                    JoinTimeToleranceMs)
                    current.IsFirst = false;
                lastByAuthoredGroup[key] = current;
            }

            for (int i = 0; i < OrderedRanges.Count; i++)
            {
                RangeInfo info = OrderedRanges[i];
                info.PreviousRange = MergeRenderRanges(
                    info.PreviousRanges);
                info.NextRange = MergeRenderRanges(
                    info.NextRanges);
            }
        }

        private static Vector2 MergeRenderRanges(
            List<Vector2> ranges)
        {
            if (ranges.Count == 0)
                return new Vector2(0f, 1f);
            float lo = ranges[0].x;
            float hi = ranges[0].y;
            for (int i = 1; i < ranges.Count; i++)
            {
                if (ranges[i].x < lo) lo = ranges[i].x;
                if (ranges[i].y > hi) hi = ranges[i].y;
            }
            return new Vector2(lo, hi);
        }

        private static void AddRange(
            List<Vector2> ranges, Vector2 added)
        {
            int insert = 0;
            while (insert < ranges.Count &&
                   ranges[insert].x < added.x)
                insert++;
            ranges.Insert(insert, added);

            int write = 0;
            for (int read = 0; read < ranges.Count; read++)
            {
                if (write == 0)
                {
                    ranges[write++] = ranges[read];
                    continue;
                }
                Vector2 previous = ranges[write - 1];
                Vector2 current = ranges[read];
                if (current.x <=
                    previous.y + JoinSpaceTolerance)
                {
                    ranges[write - 1] = new Vector2(
                        previous.x,
                        Math.Max(previous.y, current.y));
                }
                else
                {
                    ranges[write++] = current;
                }
            }
            if (write < ranges.Count)
                ranges.RemoveRange(write, ranges.Count - write);
        }

        private static int Find(int[] parent, int index)
        {
            while (parent[index] != index)
            {
                parent[index] = parent[parent[index]];
                index = parent[index];
            }
            return index;
        }

        private static unsafe Il2Cpp_b._fA BuildRenderNote(
            RangeInfo info, double leadMs)
        {
            byte* buffer = stackalloc byte[
                NativeLayoutCatalog.NoteStride];
            new Span<byte>(
                buffer, NativeLayoutCatalog.NoteStride).Clear();
            IntPtr ptr = (IntPtr)buffer;
            VisualNoteDescriptor descriptor = info.Descriptor;

            Marshal.WriteInt64(
                ptr + NativeLayoutCatalog.Note.Id, info.Id);
            Marshal.WriteInt64(
                ptr + NativeLayoutCatalog.Note.Group, info.Group);
            Marshal.WriteInt32(
                ptr + NativeLayoutCatalog.Note.Side, 4);
            Marshal.WriteInt32(
                ptr + NativeLayoutCatalog.Note.Type, 5);
            Marshal.WriteInt32(
                ptr + NativeLayoutCatalog.Note.StartMs, info.StartMs);
            Marshal.WriteInt32(
                ptr + NativeLayoutCatalog.Note.EndMs, info.EndMs);
            WriteBand(
                ptr + NativeLayoutCatalog.Note.PackedStart,
                descriptor.StartCenter);
            WriteBand(
                ptr + NativeLayoutCatalog.Note.PackedEnd,
                descriptor.EndCenter);
            WriteBand(
                ptr + NativeLayoutCatalog.Note.PackedStartWidth,
                descriptor.StartWidth);
            WriteBand(
                ptr + NativeLayoutCatalog.Note.PackedEndWidth,
                descriptor.EndWidth);
            Marshal.WriteInt64(
                ptr + NativeLayoutCatalog.Note.RuntimeLead,
                BitConverter.DoubleToInt64Bits(leadMs));
            Marshal.WriteByte(
                ptr + NativeLayoutCatalog.Note.Consumed, 0);
            uint flags = descriptor.Flags & ~0x6000u;
            if (info.IsFirst) flags |= 0x2000u;
            if (info.IsLast) flags |= 0x4000u;
            Marshal.WriteInt32(
                ptr + NativeLayoutCatalog.Note.Flags,
                unchecked((int)flags));

            return Unsafe.ReadUnaligned<Il2Cpp_b._fA>(
                (void*)buffer);
        }

        private static void WriteBand(
            IntPtr address, double value)
        {
            const int denominator = 1000000;
            Marshal.WriteInt32(
                address,
                (int)Math.Round(value * denominator));
            Marshal.WriteInt32(address + 4, denominator);
            Marshal.WriteInt32(
                address + 8,
                BitConverter.SingleToInt32Bits((float)value));
        }

        private static Entry CreateEntry(
            Il2Cppifapp.Game.Scenes.Game.Track track, long id)
        {
            Entry entry = null;
            try
            {
                entry = new Entry { Id = id };
                entry.Band = CreateArea(
                    track, track.safeAreaPrefab,
                    $"IFCE_NoInputSky_{id}",
                    entry.OwnedMaterials);
                entry.Shadow = CreateArea(
                    track, track.safeAreaShadowPrefab,
                    $"IFCE_NoInputSkyShadow_{id}",
                    entry.OwnedMaterials);
                if (entry.Band == null || entry.Shadow == null)
                {
                    DestroyEntry(entry);
                    return null;
                }
                entry.BandArea =
                    entry.Band.GetComponent<
                        Il2Cppifapp.Game.Scenes.Game.SkySafeArea>();
                entry.ShadowArea =
                    entry.Shadow.GetComponent<
                        Il2Cppifapp.Game.Scenes.Game.SkySafeArea>();
                if (entry.BandArea != null)
                {
                    entry.BaseColorDetails = entry.BandArea.colorDetails;
                    if (entry.BaseColorDetails == null)
                        entry.BaseColorDetails = track.safeAreaColorDetails;
                    entry.BandArea.colorDetails = entry.BaseColorDetails;
                }
                return entry;
            }
            catch (Exception e)
            {
                DestroyEntry(entry);
                if (_errors++ < 20)
                    ChartExtMod.Log.Error(
                        "[ChartExt] noinput sky visual create failed: " + e);
                return null;
            }
        }

        private static GameObject CreateArea(
            Il2Cppifapp.Game.Scenes.Game.Track track,
            GameObject prefab, string name,
            List<Material> ownedMaterials)
        {
            if (prefab == null || prefab.Pointer == IntPtr.Zero)
                return null;
            GameObject go = null;
            try
            {
                go = UnityEngine.Object.Instantiate(prefab);
                go.name = name;
                Transform parent = track.notesContainer;
                if (parent != null && parent.Pointer != IntPtr.Zero)
                    go.transform.SetParent(parent, false);

                var area =
                    go.GetComponent<
                        Il2Cppifapp.Game.Scenes.Game.SkySafeArea>();
                var filter = go.GetComponent<MeshFilter>();
                var renderer = go.GetComponent<MeshRenderer>();
                if (area != null)
                {
                    if (filter != null) area.meshFilter = filter;
                    if (renderer != null) area.meshRenderer = renderer;
                }
                if (renderer != null)
                    ownedMaterials.AddRange(CloneMaterials(go));
                if (renderer != null)
                {
                    renderer.forceRenderingOff = false;
                    renderer.enabled = true;
                }
                go.SetActive(true);
                return go;
            }
            catch
            {
                TryDestroy(go);
                throw;
            }
        }

        private static void UpdateEntry(
            Il2Cppifapp.Game.Scenes.Game.Track track,
            Entry entry, ref Il2Cpp_b._fA note,
            double nowMs, double leadMs,
            double durationMs, double progress)
        {
            float bandScale = BitConverter.Int32BitsToSingle(
                Marshal.ReadInt32(
                    track.Pointer + NativeLayoutCatalog.Track.TrackWidth));
            float shadowScale = BitConverter.Int32BitsToSingle(
                Marshal.ReadInt32(
                    track.Pointer + NativeLayoutCatalog.Track.NoteWidthScale));
            double trackLength = BitConverter.Int64BitsToDouble(
                Marshal.ReadInt64(
                    track.Pointer + NativeLayoutCatalog.Track.BaseSpeed));
            double speed = BitConverter.Int64BitsToDouble(
                Marshal.ReadInt64(
                    track.Pointer + NativeLayoutCatalog.Track.DepthFactor));
            double trackScale = trackLength / 3.333333334;
            double visualScale = trackScale * speed;
            double zPerMs = visualScale / 1000.0;
            double durationSeconds = Math.Max(0.0, durationMs) / 1000.0;
            float bandZScale = (float)visualScale;
            float shadowZScale = (float)trackScale;
            double visualLength = durationSeconds * visualScale;
            double chartSeconds = nowMs / 1000.0;
            double noteStartSeconds =
                chartSeconds + leadMs / 1000.0;

            if (entry.BandArea != null)
            {
                if (SkyColorRuntime.TryGetActiveColor(
                        entry.AuthoredGroup, nowMs,
                        out uint rgba, out bool edgeGlow) &&
                    track.safeAreaColorDetails != null)
                {
                    var tinted = SkyColorRuntime.GetClone(
                        track.safeAreaColorDetails.Pointer,
                        rgba, edgeGlow);
                    if (tinted != null)
                        entry.BandArea.colorDetails = tinted;
                }
                else
                {
                    entry.BandArea.colorDetails =
                            entry.BaseColorDetails ?? track.safeAreaColorDetails;
                }
            }

            if (entry.BandArea != null)
            {
                entry.BandArea._KZ(
                    ref note,
                    bandScale,
                    (float)chartSeconds,
                    noteStartSeconds,
                    visualLength,
                    bandZScale,
                    0f,
                    progress,
                    entry.PreviousRange,
                    entry.NextRange);
                Vector3 zero = ReadVector3(
                    track.Pointer + NativeLayoutCatalog.Track.RestPosition);
                float x = -bandScale * 0.5f +
                          bandScale * note._Ce._LD;
                float z = (float)(zPerMs * leadMs);
                Transform tr = entry.Band.transform;
                tr.localPosition = new Vector3(
                    zero.x + x, zero.y,
                    zero.z + z);
                if (!entry.Band.activeSelf) entry.Band.SetActive(true);
            }

            if (entry.ShadowArea != null)
            {
                entry.ShadowArea._KZ(
                    ref note,
                    shadowScale,
                    (float)chartSeconds,
                    noteStartSeconds,
                    visualLength,
                    shadowZScale,
                    0f,
                    progress,
                    entry.PreviousRange,
                    entry.NextRange);
                Vector3 zero = ReadVector3(
                    track.Pointer + NativeLayoutCatalog.Track.HomePosition);
                float x = -shadowScale * 0.5f +
                          shadowScale * note._Ce._LD;
                float z = (float)(zPerMs * leadMs);
                Transform tr = entry.Shadow.transform;
                tr.localPosition = new Vector3(
                    zero.x + x, zero.y,
                    zero.z + z);
                if (!entry.Shadow.activeSelf) entry.Shadow.SetActive(true);
            }
        }

        private static void UpdateBrackets(
            Il2Cppifapp.Game.Scenes.Game.Track track,
            Entry entry, ref Il2Cpp_b._fA note,
            double leadMs, double maxLeadMs)
        {
            bool visible = entry.IsHead &&
                           entry.HeadInBounds &&
                           note._Ee > BracketLeadMinMs &&
                           leadMs <= maxLeadMs;
            if (!visible)
            {
                SetBracketsActive(entry, false);
                return;
            }
            if (entry.LeftBracket == null)
                CreateBrackets(track, entry);
            if (entry.LeftBracket == null ||
                entry.RightBracket == null)
                return;
            if (entry.Band == null ||
                entry.Band.Pointer == IntPtr.Zero)
                return;

            float bandScale = BitConverter.Int32BitsToSingle(
                Marshal.ReadInt32(
                    track.Pointer + NativeLayoutCatalog.Track.TrackWidth));
            Vector3 bandPosition =
                entry.Band.transform.localPosition;
            Quaternion rotation =
                ReadQuaternion(
                    track.Pointer + NativeLayoutCatalog.Track.RestRotation);
            float halfWidth =
                bandScale * Math.Abs(note._De._LD) * 0.5f;

            entry.LeftBracket.transform.localPosition =
                new Vector3(
                    bandPosition.x - halfWidth,
                    bandPosition.y,
                    bandPosition.z);
            entry.RightBracket.transform.localPosition =
                new Vector3(
                    bandPosition.x + halfWidth,
                    bandPosition.y,
                    bandPosition.z);
            entry.LeftBracket.transform.localRotation = rotation;
            entry.RightBracket.transform.localRotation =
                rotation * Quaternion.Euler(0f, 180f, 0f);
            SetBracketsActive(entry, true);
        }

        internal static bool IsBracketHeadInBounds(
            float startLo, float startHi)
        {
            return startLo >= 0f && startHi <= 1f;
        }

        private static void CreateBrackets(
            Il2Cppifapp.Game.Scenes.Game.Track track,
            Entry entry)
        {
            try
            {
                if (track.safeAreaEntryBracketPrefab == null ||
                    track.safeAreaEntryBracketPrefab.Pointer == IntPtr.Zero)
                    return;
                entry.LeftBracket = UnityEngine.Object.Instantiate(
                    track.safeAreaEntryBracketPrefab);
                entry.RightBracket = UnityEngine.Object.Instantiate(
                    track.safeAreaEntryBracketPrefab);
                entry.LeftBracket.name =
                    $"IFCE_NoInputSkyBracketL_{entry.Id}";
                entry.RightBracket.name =
                    $"IFCE_NoInputSkyBracketR_{entry.Id}";
                Transform parent = track.notesContainer;
                if (parent != null && parent.Pointer != IntPtr.Zero)
                {
                    entry.LeftBracket.transform.SetParent(parent, false);
                    entry.RightBracket.transform.SetParent(parent, false);
                }
                List<Material> bracketMaterials =
                    CloneMaterials(entry.LeftBracket);
                bracketMaterials.AddRange(
                    CloneMaterials(entry.RightBracket));
                entry.OwnedMaterials.AddRange(
                    bracketMaterials);
            }
            catch (Exception e)
            {
                TryDestroy(entry.LeftBracket);
                TryDestroy(entry.RightBracket);
                entry.LeftBracket = null;
                entry.RightBracket = null;
                if (_errors++ < 20)
                    ChartExtMod.Log.Error(
                        "[ChartExt] noinput sky bracket create failed: " + e);
            }
        }

        private static List<Material> CloneMaterials(GameObject root)
        {
            var owned = new List<Material>(4);
            if (root == null || root.Pointer == IntPtr.Zero)
                return owned;
            try
            {
                var renderers =
                    root.GetComponentsInChildren<MeshRenderer>(true);
                for (int i = 0;
                     renderers != null && i < renderers.Length;
                     i++)
                {
                    MeshRenderer renderer = renderers[i];
                    if (renderer == null ||
                        renderer.sharedMaterial == null)
                        continue;
                    Material clone = new Material(
                        renderer.sharedMaterial);
                    try
                    {
                        renderer.sharedMaterial = clone;
                        owned.Add(clone);
                    }
                    catch
                    {
                        UnityEngine.Object.Destroy(clone);
                        throw;
                    }
                }
                return owned;
            }
            catch
            {
                DestroyMaterials(owned);
                throw;
            }
        }

        private static void SetBracketsActive(
            Entry entry, bool active)
        {
            try
            {
                if (entry.LeftBracket != null &&
                    entry.LeftBracket.activeSelf != active)
                    entry.LeftBracket.SetActive(active);
            }
            catch { }
            try
            {
                if (entry.RightBracket != null &&
                    entry.RightBracket.activeSelf != active)
                    entry.RightBracket.SetActive(active);
            }
            catch { }
        }

        private static Vector3 ReadVector3(IntPtr address)
        {
            return new Vector3(
                BitConverter.Int32BitsToSingle(
                    Marshal.ReadInt32(address)),
                BitConverter.Int32BitsToSingle(
                    Marshal.ReadInt32(address + 4)),
                BitConverter.Int32BitsToSingle(
                    Marshal.ReadInt32(address + 8)));
        }

        private static Quaternion ReadQuaternion(IntPtr address)
        {
            return new Quaternion(
                BitConverter.Int32BitsToSingle(
                    Marshal.ReadInt32(address)),
                BitConverter.Int32BitsToSingle(
                    Marshal.ReadInt32(address + 4)),
                BitConverter.Int32BitsToSingle(
                    Marshal.ReadInt32(address + 8)),
                BitConverter.Int32BitsToSingle(
                    Marshal.ReadInt32(address + 12)));
        }

        private static void Hide(long id)
        {
            if (!Entries.TryGetValue(id, out Entry entry)) return;
            try { if (entry.Band != null) entry.Band.SetActive(false); } catch { }
            try { if (entry.Shadow != null) entry.Shadow.SetActive(false); } catch { }
            SetBracketsActive(entry, false);
        }

        private static void DestroyAll()
        {
            foreach (Entry entry in Entries.Values)
                DestroyEntry(entry);
            Entries.Clear();
        }

        private static void DestroyEntry(Entry entry)
        {
            if (entry == null) return;
            DestroyMaterials(entry.OwnedMaterials);
            entry.OwnedMaterials.Clear();
            try { if (entry.Band != null) UnityEngine.Object.Destroy(entry.Band); } catch { }
            try { if (entry.Shadow != null) UnityEngine.Object.Destroy(entry.Shadow); } catch { }
            try { if (entry.LeftBracket != null) UnityEngine.Object.Destroy(entry.LeftBracket); } catch { }
            try { if (entry.RightBracket != null) UnityEngine.Object.Destroy(entry.RightBracket); } catch { }
        }

        private static void DestroyMaterials(
            IReadOnlyList<Material> materials)
        {
            for (int i = 0; i < materials.Count; i++)
            {
                Material material = materials[i];
                try
                {
                    if (material != null && material.Pointer != IntPtr.Zero)
                        UnityEngine.Object.Destroy(material);
                }
                catch
                {
                }
            }
        }

        private static void TryDestroy(GameObject go)
        {
            try
            {
                if (go != null && go.Pointer != IntPtr.Zero)
                    UnityEngine.Object.Destroy(go);
            }
            catch
            {
            }
        }
    }
}
