using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

namespace InFalsusChartExt
{
    /// <summary>
    /// Independent renderer for target normal SkyBlock members. The native
    /// SkyArea objects remain untouched and are removed from the target spawn
    /// list; this runtime owns visibility and lifetime for target members only.
    /// </summary>
    internal static class SkyBlockVisualRuntime
    {
        private const double EndRetainMs = 2000.0;
        private const double BracketLeadMinMs = 5.0;
        private const int MaxCreationsPerFrame = 8;
        private const uint HeadFlag = 0x2000u;
        private const uint TailFlag = 0x4000u;

        private sealed class Entry
        {
            internal SkyBlockMember Member;
            internal GameObject Band;
            internal GameObject Shadow;
            internal GameObject LeftBracket;
            internal GameObject RightBracket;
            internal Il2Cppifapp.Game.Scenes.Game.SkySafeArea
                BandArea;
            internal Il2Cppifapp.Game.Scenes.Game.SkySafeArea
                ShadowArea;
            internal Il2Cppifapp.Game.Scenes.Game.SafeAreaColorDetails
                BaseColorDetails;
            internal bool BracketsActive;
            internal bool BandActive;
            internal bool ShadowActive;
        }

        private static readonly List<SkyBlockMember>
            ActiveMembers =
                new List<SkyBlockMember>(64);
        private static readonly Stack<Entry> FreeEntries =
            new Stack<Entry>(64);
        private static Entry[] _entries = Array.Empty<Entry>();
        private static int _generation = -1;
        private static int _cursor;
        private static double _lastNow = double.NaN;
        private static double _spawnLeadMs =
            VisualSpawnTiming.NativeQuickLeadMs;
        private static int _creationsThisFrame;
        private static int _errors;

        internal static void OnChartCompiled()
        {
            if (!BuildProfile.Supports(NativeCapability.SkyBlock))
                return;
            SkyBlockPlan plan = CurrentPlan();
            int generation = plan?.Generation ?? -1;
            if (_generation == generation) return;
            DestroyAll();
            _generation = generation;
            _cursor = 0;
            _lastNow = double.NaN;
            ActiveMembers.Clear();
            _entries = plan == null ||
                       plan.VisualMembers.Count == 0
                ? Array.Empty<Entry>()
                : new Entry[plan.VisualMembers.Count];
        }

        internal static void Tick(
            Il2Cppifapp.Game.Scenes.Game.Track track)
        {
            if (!BuildProfile.Supports(NativeCapability.SkyBlock))
                return;
            try
            {
                if (track == null ||
                    track.Pointer == IntPtr.Zero)
                    return;
                SkyBlockPlan plan = CurrentPlan();
                if (plan == null ||
                    plan.VisualMembers.Count == 0 ||
                    !SkyBlockRuntimeSlots.Ensure(plan))
                    return;
                if (_generation != plan.Generation)
                    OnChartCompiled();
                if (_entries.Length != plan.VisualMembers.Count)
                    return;

                double now =
                    TimingGroupRuntime.CurrentMs();
                if (double.IsNaN(now)) return;
                _spawnLeadMs =
                    VisualSpawnTiming.MaxLeadMs(
                        track.Pointer);
                _creationsThisFrame = 0;
                RefreshActiveMembers(plan, now);

                for (int i = 0;
                     i < ActiveMembers.Count;
                     i++)
                {
                    SkyBlockMember member =
                        ActiveMembers[i];
                    if (SceneControlRuntime.IsNoteHidden(member.Id))
                    {
                        Entry hiddenEntry =
                            _entries[member.VisualIndex];
                        if (hiddenEntry != null)
                            Hide(hiddenEntry);
                        if (now <
                            member.EndMs + EndRetainMs)
                            continue;
                        ReleaseActiveAt(i);
                        i--;
                        continue;
                    }
                    if (!SkyBlockRuntimeSlots.TryNote(
                            member, out IntPtr notePtr))
                    {
                        if (!ShouldRetainUnavailableMember(
                                member, now))
                        {
                            ReleaseActiveAt(i);
                            i--;
                        }
                        continue;
                    }
                    if (now >= member.EndMs &&
                        !member.JudgementSettled)
                        SyncFinalRecordState(member);
                    double visualNow =
                        TimingGroupRuntime.VisualTimeMs(
                            member.TimingGroup, now);
                    double visualStart =
                        TimingGroupRuntime.VisualTimeMs(
                            member.TimingGroup, member.StartMs);
                    double visualEnd =
                        TimingGroupRuntime.VisualTimeMs(
                            member.TimingGroup, member.EndMs);
                    if (!IsFinite(visualNow) ||
                        !IsFinite(visualStart) ||
                        !IsFinite(visualEnd) ||
                        visualEnd <= visualStart)
                        continue;

                    double leadMs = PositionLeadMs(
                        member, now, visualStart, visualNow);
                    double durationMs =
                        visualEnd - visualStart;
                    double endDeltaMs =
                        member.TimingGroup == 0
                            ? member.EndMs - now
                            : visualEnd - visualNow;
                    if (leadMs > _spawnLeadMs)
                    {
                        ReleaseActiveAt(i);
                        i--;
                        continue;
                    }
                    double progress =
                        Math.Max(0.0, Math.Min(
                            1.0,
                            (visualNow - visualStart) /
                            (visualEnd - visualStart)));
                    if (ShouldReleaseAfterLine(
                            member, progress, endDeltaMs,
                            now))
                    {
                        ReleaseActiveAt(i);
                        i--;
                        continue;
                    }
                    bool retainWholeStrip =
                        ShouldRetainWholeStrip(
                            member, now);
                    bool forceFullRed =
                        progress >= 1.0 && retainWholeStrip;

                    Entry entry = _entries[member.VisualIndex];
                    if (entry == null)
                    {
                        if (_creationsThisFrame >=
                            MaxCreationsPerFrame)
                            continue;
                        _creationsThisFrame++;
                        entry = AcquireEntry(track, member);
                        if (entry == null) continue;
                        _entries[member.VisualIndex] = entry;
                    }
                    UpdateEntry(
                        track, entry, notePtr,
                        ref now, leadMs, durationMs,
                        progress, retainWholeStrip,
                        forceFullRed);
                }
            }
            catch (Exception e)
            {
                LogError("tick", e);
            }
        }

        internal static void ResetForChartSwitch()
        {
            DestroyAll();
            ActiveMembers.Clear();
            _entries = Array.Empty<Entry>();
            _generation = -1;
            _cursor = 0;
            _lastNow = double.NaN;
        }

        private static void RefreshActiveMembers(
            SkyBlockPlan plan, double now)
        {
            bool rewind =
                !double.IsNaN(_lastNow) &&
                now + 0.001 < _lastNow;
            if (double.IsNaN(_lastNow) || rewind)
            {
                ActiveMembers.Clear();
                _cursor = 0;
                if (rewind)
                {
                    for (int i = 0; i < _entries.Length; i++)
                    {
                        if (_entries[i] != null)
                            ReleaseMember(_entries[i].Member);
                    }
                }
                while (_cursor < plan.VisualMembers.Count)
                {
                    SkyBlockMember member =
                        plan.VisualMembers[_cursor];
                    double leadMs = SpawnLeadMs(
                        member, now);
                    if (leadMs > _spawnLeadMs)
                        break;
                    _cursor++;
                    if (ShouldTrackMember(member, now))
                        ActiveMembers.Add(member);
                }
            }
            else
            {
                while (_cursor < plan.VisualMembers.Count)
                {
                    SkyBlockMember member =
                        plan.VisualMembers[_cursor];
                    double leadMs = SpawnLeadMs(
                        member, now);
                    if (leadMs > _spawnLeadMs)
                        break;
                    _cursor++;
                    if (ShouldTrackMember(member, now))
                        ActiveMembers.Add(member);
                }
            }
            _lastNow = now;
        }

        private static double SpawnLeadMs(
            SkyBlockMember member,
            double now)
        {
            return PositionLeadMs(
                member,
                now,
                TimingGroupRuntime.VisualTimeMs(
                    member.TimingGroup,
                    member.StartMs),
                TimingGroupRuntime.VisualTimeMs(
                    member.TimingGroup,
                    now));
        }

        private static double PositionLeadMs(
            SkyBlockMember member,
            double now,
            double visualStart,
            double visualNow)
        {
            return member.TimingGroup == 0
                ? member.StartMs - now
                : visualStart - visualNow;
        }

        private static bool ShouldTrackMember(
            SkyBlockMember member, double now)
        {
            return now < member.EndMs + EndRetainMs;
        }

        internal static bool ShouldRetainUnavailableMember(
            SkyBlockMember member, double now)
        {
            return member != null &&
                   now < member.EndMs + EndRetainMs;
        }

        private static bool ShouldReleaseAfterLine(
            SkyBlockMember member,
            double progress,
            double endDeltaMs,
            double now)
        {
            if (progress < 1.0)
                return false;
            if (SkyBlockJudgeRuntime.IsMemberInNormalState(
                    member, now))
                return true;
            return endDeltaMs <= -EndRetainMs;
        }

        private static bool ShouldRetainWholeStrip(
            SkyBlockMember member, double now)
        {
            return !SkyBlockJudgeRuntime.IsMemberInNormalState(
                member, now);
        }


        private static void ReleaseActiveAt(int index)
        {
            SkyBlockMember member = ActiveMembers[index];
            ReleaseMember(member);
            int last = ActiveMembers.Count - 1;
            ActiveMembers[index] = ActiveMembers[last];
            ActiveMembers.RemoveAt(last);
        }

        private static unsafe void SyncFinalRecordState(
            SkyBlockMember member)
        {
            if (TryReadFinalRecordState(
                    member.Id,
                    out bool final,
                    out byte judgement) &&
                final)
            {
                member.JudgementSettled = true;
                member.LastJudgement = judgement;
                if (judgement != ChartExtState.JudgeNone &&
                    judgement != ChartExtState.JudgeMiss)
                    member.WasJudged = true;
            }
        }

        private static unsafe bool TryReadFinalRecordState(
            long id, out bool final, out byte judgement)
        {
            final = false;
            judgement = ChartExtState.JudgeNone;
            try
            {
                Il2Cppifapp.Game.LogicalNotePlayer player =
                    Il2Cppifapp.Game.LogicalNotePlayer._Qe;
                if (player == null ||
                    player.Pointer == IntPtr.Zero)
                    return false;
                Il2Cpp_b._T compiled = player._ve;
                if (compiled == null ||
                    compiled.Pointer == IntPtr.Zero)
                    return false;
                var records = compiled._XC;
                if (records == null ||
                    records.Pointer == IntPtr.Zero ||
                    !records.ContainsKey(id))
                    return false;
                Il2CppSystem.Memory<Il2Cpp_K._cH> memory =
                    records[id];
                if (memory._object == null ||
                    memory._object.Pointer == IntPtr.Zero)
                    return false;
                int count = memory._length;
                int sliceIndex = memory._index;
                IntPtr arrayObject = memory._object.Pointer;
                int arrayLength = Marshal.ReadInt32(
                    arrayObject +
                    NativeLayoutCatalog.ArrayLengthOffset);
                if (count <= 0 ||
                    sliceIndex < 0 ||
                    sliceIndex + count > arrayLength)
                    return false;
                byte* record = InteropUtil.RecordPointer(
                    arrayObject, sliceIndex, count - 1);
                final = *(record +
                    NativeLayoutCatalog.Record.Final) != 0;
                judgement = *(record +
                    NativeLayoutCatalog.Record.Judgment);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static Entry CreateEntry(
            Il2Cppifapp.Game.Scenes.Game.Track track,
            SkyBlockMember member)
        {
            Entry entry = null;
            try
            {
                entry = new Entry
                {
                    Member = member,
                    Band = CreateArea(
                        track, track.safeAreaPrefab,
                        $"IFCE_SkyBlock_{member.Id}"),
                    Shadow = CreateArea(
                        track, track.safeAreaShadowPrefab,
                        $"IFCE_SkyBlockShadow_{member.Id}"),
                };
                if (entry.Band == null ||
                    entry.Shadow == null)
                {
                    DestroyEntry(entry);
                    return null;
                }
                entry.BandArea = entry.Band.GetComponent<
                    Il2Cppifapp.Game.Scenes.Game.SkySafeArea>();
                entry.ShadowArea = entry.Shadow.GetComponent<
                    Il2Cppifapp.Game.Scenes.Game.SkySafeArea>();
                entry.BandActive = true;
                entry.ShadowActive = true;
                var details = track.safeAreaColorDetails;
                if (details == null && entry.BandArea != null)
                    details = entry.BandArea.colorDetails;
                entry.BaseColorDetails = details;
                if (entry.BandArea != null && details != null)
                    entry.BandArea.colorDetails = details;
                if (entry.ShadowArea != null && details != null)
                    entry.ShadowArea.colorDetails = details;
                if (entry.BandArea != null &&
                    entry.BaseColorDetails == null)
                {
                    entry.BaseColorDetails =
                        track.safeAreaColorDetails;
                    entry.BandArea.colorDetails =
                        entry.BaseColorDetails;
                }
                return entry;
            }
            catch (Exception e)
            {
                DestroyEntry(entry);
                LogError("create", e);
                return null;
            }
        }

        private static Entry AcquireEntry(
            Il2Cppifapp.Game.Scenes.Game.Track track,
            SkyBlockMember member)
        {
            Entry entry;
            if (FreeEntries.Count > 0)
            {
                entry = FreeEntries.Pop();
                entry.Member = member;
            }
            else
            {
                entry = CreateEntry(track, member);
            }
            // The native spawn list is filtered before Track.Update consumes
            // it. This target-only guard handles an object that was already
            // materialized before the filter ran.
            SpawnOrderFilter.HideNativeTarget(member.Id);
            return entry;
        }

        private static void ReleaseMember(
            SkyBlockMember member)
        {
            if (member == null ||
                member.VisualIndex < 0 ||
                member.VisualIndex >= _entries.Length)
                return;
            Entry entry = _entries[member.VisualIndex];
            if (entry == null) return;
            _entries[member.VisualIndex] = null;
            Hide(entry);
            FreeEntries.Push(entry);
        }

        private static GameObject CreateArea(
            Il2Cppifapp.Game.Scenes.Game.Track track,
            GameObject prefab, string name)
        {
            if (prefab == null ||
                prefab.Pointer == IntPtr.Zero)
                return null;
            GameObject go = null;
            try
            {
                go = UnityEngine.Object.Instantiate(prefab);
                go.name = name;
                Transform parent = track.notesContainer;
                if (parent != null &&
                    parent.Pointer != IntPtr.Zero)
                    go.transform.SetParent(parent, false);

                var area = go.GetComponent<
                    Il2Cppifapp.Game.Scenes.Game.SkySafeArea>();
                var filter = go.GetComponent<MeshFilter>();
                var renderer = go.GetComponent<MeshRenderer>();
                if (area != null)
                {
                    if (filter != null) area.meshFilter = filter;
                    if (renderer != null)
                        area.meshRenderer = renderer;
                }
                if (renderer != null &&
                    renderer.sharedMaterial != null)
                    renderer.material =
                        new Material(renderer.sharedMaterial);
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
                DestroyOwnedArea(go);
                throw;
            }
        }

        private static unsafe void UpdateEntry(
            Il2Cppifapp.Game.Scenes.Game.Track track,
            Entry entry, IntPtr notePtr,
            ref double nowMs, double leadMs,
            double durationMs, double progress,
            bool retainWholeStrip,
            bool forceFullRed)
        {
            SkyBlockMember member = entry.Member;
            Il2Cpp_b._fA note =
                Unsafe.ReadUnaligned<Il2Cpp_b._fA>(
                    (void*)notePtr);
            IntPtr local =
                (IntPtr)Unsafe.AsPointer(ref note);

            uint flags = (uint)Marshal.ReadInt32(
                local + NativeLayoutCatalog.Note.Flags);
            flags &= ~(HeadFlag | TailFlag);
            if (member.IsFirst &&
                !member.HasAdjacentSameAuthoredPrevious)
                flags |= HeadFlag;
            if (member.IsLast)
                flags |= TailFlag;
            Marshal.WriteInt32(
                local + NativeLayoutCatalog.Note.Flags,
                unchecked((int)flags));

            Vector2 previousRange =
                GetRenderRange(member, true);
            Vector2 nextRange =
                GetRenderRange(member, false);
            ApplyColor(
                track, entry, member.AuthoredGroup, nowMs);

            float bandScale = ReadFloat(
                track.Pointer +
                NativeLayoutCatalog.Track.TrackWidth);
            float shadowScale = ReadFloat(
                track.Pointer +
                NativeLayoutCatalog.Track.NoteWidthScale);
            double trackLength =
                BitConverter.Int64BitsToDouble(
                    Marshal.ReadInt64(
                        track.Pointer +
                        NativeLayoutCatalog.Track.BaseSpeed));
            double speed =
                BitConverter.Int64BitsToDouble(
                    Marshal.ReadInt64(
                        track.Pointer +
                        NativeLayoutCatalog.Track.DepthFactor));
            double trackScale =
                trackLength / 3.333333334;
            double visualScale = trackScale * speed;
            double zPerMs = visualScale / 1000.0;
            double durationSeconds =
                Math.Max(0.0, durationMs) / 1000.0;
            float bandZScale = (float)visualScale;
            float shadowZScale = (float)trackScale;
            double visualLength =
                durationSeconds * visualScale;
            double chartSeconds = nowMs / 1000.0;
            double displayLeadMs = leadMs;
            double noteStartSeconds =
                chartSeconds + displayLeadMs / 1000.0;
            float redFactor = forceFullRed
                ? 1f
                : SkyBlockJudgeRuntime.TryGetMemberRedFactor(
                        member, nowMs, out float currentRed)
                    ? currentRed
                    : 0f;
            // Native visual lifetime is decided by the last record, not by
            // whether any earlier record was successful. Keep the full strip
            // while the last record is pending or is Miss.
            double meshProgress = retainWholeStrip
                ? 0.0
                : progress;

            if (entry.BandArea != null)
            {
                entry.BandArea._KZ(
                    ref note,
                    bandScale,
                    (float)chartSeconds,
                    noteStartSeconds,
                    visualLength,
                    bandZScale,
                    redFactor,
                    meshProgress,
                    previousRange,
                    nextRange);
                Vector3 zero = ReadVector3(
                    track.Pointer +
                    NativeLayoutCatalog.Track.RestPosition);
                float x = -bandScale * 0.5f +
                          bandScale * note._Ce._LD;
                float z = (float)(zPerMs * displayLeadMs);
                entry.Band.transform.localPosition =
                    new Vector3(zero.x + x, zero.y, zero.z + z);
                if (!entry.BandActive)
                {
                    entry.Band.SetActive(true);
                    entry.BandActive = true;
                }
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
                    redFactor,
                    meshProgress,
                    previousRange,
                    nextRange);
                Vector3 zero = ReadVector3(
                    track.Pointer +
                    NativeLayoutCatalog.Track.HomePosition);
                float x = -shadowScale * 0.5f +
                          shadowScale * note._Ce._LD;
                float z = (float)(zPerMs * displayLeadMs);
                entry.Shadow.transform.localPosition =
                    new Vector3(zero.x + x, zero.y, zero.z + z);
                if (!entry.ShadowActive)
                {
                    entry.Shadow.SetActive(true);
                    entry.ShadowActive = true;
                }
            }

            UpdateBrackets(
                track, entry, ref note, displayLeadMs);
        }

        private static Vector2 GetRenderRange(
            SkyBlockMember member, bool previous)
        {
            SkyBandRange range = previous
                ? member.PreviousRange
                : member.NextRange;
            return new Vector2(range.Lo, range.Hi);
        }

        private static void ApplyColor(
            Il2Cppifapp.Game.Scenes.Game.Track track,
            Entry entry, long authoredGroup, double nowMs)
        {
            Il2Cppifapp.Game.Scenes.Game.SafeAreaColorDetails details =
                entry.BaseColorDetails ??
                track.safeAreaColorDetails;
            if (ChartExtState.SkyColorEvents.Count == 0)
            {
                if (entry.BandArea != null)
                    entry.BandArea.colorDetails = details;
                if (entry.ShadowArea != null)
                    entry.ShadowArea.colorDetails = details;
                return;
            }
            if (SkyColorRuntime.TryGetActiveColor(
                    authoredGroup,
                    nowMs,
                    out uint rgba,
                    out bool edgeGlow) &&
                track.safeAreaColorDetails != null)
            {
                var tinted = SkyColorRuntime.GetClone(
                    track.safeAreaColorDetails.Pointer,
                    rgba, edgeGlow);
                if (tinted != null) details = tinted;
            }
            if (entry.BandArea != null)
                entry.BandArea.colorDetails = details;
            if (entry.ShadowArea != null)
                entry.ShadowArea.colorDetails = details;
        }

        private static void UpdateBrackets(
            Il2Cppifapp.Game.Scenes.Game.Track track,
            Entry entry, ref Il2Cpp_b._fA note,
            double leadMs)
        {
            bool visible =
                entry.Member.IsFirst &&
                !entry.Member.HasAdjacentSameAuthoredPrevious &&
                note._Ee > BracketLeadMinMs &&
                leadMs <= _spawnLeadMs;
            if (!visible)
            {
                SetBracketsActive(entry, false);
                return;
            }
            if (entry.LeftBracket == null)
                CreateBrackets(track, entry);
            if (entry.LeftBracket == null ||
                entry.RightBracket == null ||
                entry.Band == null)
                return;

            float bandScale = ReadFloat(
                track.Pointer +
                NativeLayoutCatalog.Track.TrackWidth);
            Vector3 bandPosition =
                entry.Band.transform.localPosition;
            Quaternion rotation = ReadQuaternion(
                track.Pointer +
                NativeLayoutCatalog.Track.RestRotation);
            float halfWidth =
                bandScale *
                Math.Abs(note._De._LD) * 0.5f;
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
            entry.LeftBracket.transform.localRotation =
                rotation;
            entry.RightBracket.transform.localRotation =
                rotation * Quaternion.Euler(0f, 180f, 0f);
            SetBracketsActive(entry, true);
        }

        private static void CreateBrackets(
            Il2Cppifapp.Game.Scenes.Game.Track track,
            Entry entry)
        {
            try
            {
                if (track.safeAreaEntryBracketPrefab == null ||
                    track.safeAreaEntryBracketPrefab.Pointer ==
                    IntPtr.Zero)
                    return;
                entry.LeftBracket =
                    UnityEngine.Object.Instantiate(
                        track.safeAreaEntryBracketPrefab);
                entry.RightBracket =
                    UnityEngine.Object.Instantiate(
                        track.safeAreaEntryBracketPrefab);
                entry.LeftBracket.name =
                    $"IFCE_SkyBlockBracketL_{entry.Member.Id}";
                entry.RightBracket.name =
                    $"IFCE_SkyBlockBracketR_{entry.Member.Id}";
                Transform parent = track.notesContainer;
                if (parent != null &&
                    parent.Pointer != IntPtr.Zero)
                {
                    entry.LeftBracket.transform.SetParent(
                        parent, false);
                    entry.RightBracket.transform.SetParent(
                        parent, false);
                }
                CloneMaterials(entry.LeftBracket);
                CloneMaterials(entry.RightBracket);
                entry.LeftBracket.SetActive(false);
                entry.RightBracket.SetActive(false);
                entry.BracketsActive = false;
            }
            catch (Exception e)
            {
                DestroyOwnedArea(entry.LeftBracket);
                DestroyOwnedArea(entry.RightBracket);
                entry.LeftBracket = null;
                entry.RightBracket = null;
                LogError("brackets", e);
            }
        }

        private static void DestroyOwnedArea(GameObject go)
        {
            if (go == null || go.Pointer == IntPtr.Zero)
                return;
            try
            {
                var renderers =
                    go.GetComponentsInChildren<MeshRenderer>(true);
                for (int i = 0;
                     renderers != null && i < renderers.Length;
                     i++)
                {
                    Material material = renderers[i]?.sharedMaterial;
                    if (material != null &&
                        material.Pointer != IntPtr.Zero)
                        UnityEngine.Object.Destroy(material);
                }
            }
            catch { }
            try { UnityEngine.Object.Destroy(go); }
            catch { }
        }

        private static void CloneMaterials(GameObject root)
        {
            var renderers =
                root.GetComponentsInChildren<MeshRenderer>(true);
            if (renderers == null) return;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null &&
                    renderers[i].sharedMaterial != null)
                    renderers[i].material =
                        new Material(
                            renderers[i].sharedMaterial);
            }
        }

        private static void SetBracketsActive(
            Entry entry, bool active)
        {
            if (entry.BracketsActive == active) return;
            entry.BracketsActive = active;
            try
            {
                if (entry.LeftBracket != null)
                    entry.LeftBracket.SetActive(active);
                if (entry.RightBracket != null)
                    entry.RightBracket.SetActive(active);
            }
            catch { }
        }

        private static void Hide(Entry entry)
        {
            if (entry == null) return;
            try
            {
                if (entry.Band != null)
                    entry.Band.SetActive(false);
                if (entry.Shadow != null)
                    entry.Shadow.SetActive(false);
            }
            catch { }
            entry.BandActive = false;
            entry.ShadowActive = false;
            SetBracketsActive(entry, false);
        }

        private static void DestroyAll()
        {
            for (int i = 0; i < _entries.Length; i++)
            {
                DestroyEntry(_entries[i]);
                _entries[i] = null;
            }
            while (FreeEntries.Count > 0)
                DestroyEntry(FreeEntries.Pop());
        }

        private static void DestroyEntry(Entry entry)
        {
            if (entry == null) return;
            DestroyMaterials(entry.Band);
            DestroyMaterials(entry.Shadow);
            DestroyMaterials(entry.LeftBracket);
            DestroyMaterials(entry.RightBracket);
            TryDestroy(entry.Band);
            TryDestroy(entry.Shadow);
            TryDestroy(entry.LeftBracket);
            TryDestroy(entry.RightBracket);
        }

        private static void TryDestroy(GameObject go)
        {
            try
            {
                if (go != null &&
                    go.Pointer != IntPtr.Zero)
                    UnityEngine.Object.Destroy(go);
            }
            catch { }
        }

        private static void DestroyMaterials(GameObject root)
        {
            if (root == null || root.Pointer == IntPtr.Zero)
                return;
            try
            {
                var renderers =
                    root.GetComponentsInChildren<MeshRenderer>(
                        true);
                for (int i = 0;
                     renderers != null &&
                     i < renderers.Length;
                     i++)
                {
                    Material material =
                        renderers[i]?.sharedMaterial;
                    if (material != null &&
                        material.Pointer != IntPtr.Zero)
                        UnityEngine.Object.Destroy(material);
                }
            }
            catch { }
        }

        private static Vector3 ReadVector3(IntPtr address)
        {
            return new Vector3(
                ReadFloat(address),
                ReadFloat(address + 4),
                ReadFloat(address + 8));
        }

        private static Quaternion ReadQuaternion(
            IntPtr address)
        {
            return new Quaternion(
                ReadFloat(address),
                ReadFloat(address + 4),
                ReadFloat(address + 8),
                ReadFloat(address + 12));
        }

        private static float ReadFloat(IntPtr address)
            => BitConverter.Int32BitsToSingle(
                Marshal.ReadInt32(address));

        private static SkyBlockPlan CurrentPlan()
        {
            ChartRuntimeFrame frame =
                ChartRuntimeContext.Current;
            return ChartRuntimeContext.IsActive
                ? frame?.SkyBlocks
                : null;
        }

        private static bool IsFinite(double value)
            => !double.IsNaN(value) &&
               !double.IsInfinity(value);

        private static void LogError(
            string where, Exception error)
        {
            if (_errors++ >= 20) return;
            try
            {
                ChartExtMod.Log.Error(
                    $"[ChartExt] skyblock visual {where} failed: " +
                    error);
            }
            catch { }
        }
    }
}
