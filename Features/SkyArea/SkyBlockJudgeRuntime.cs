using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using HarmonyLib;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace InFalsusChartExt
{
    /// <summary>
    /// Independent judgement for SkyBlock target members. The target positions
    /// are filtered from the native judge list before `_Oz`; this runtime then
    /// writes their records and uses the original `_PEA -> _Tz` presentation
    /// conversion instead of calling `_Mk` directly.
    /// </summary>
    internal static class SkyBlockJudgeRuntime
    {
        private const float CursorTolerance = 0.02f;
        private const double WarningMs = 300.0;
        private const double PenaltyMs = 500.0;

        private static readonly List<SkyJudgmentLayer> ActiveLayers =
            new List<SkyJudgmentLayer>(64);
        private static readonly List<PendingLayer> PendingLayers =
            new List<PendingLayer>(16);

        private struct PendingLayer
        {
            internal SkyJudgmentLayer Layer;
            internal double KeepUntilMs;
        }

        private struct DueRecord
        {
            internal SkyBlockMember Member;
            internal IntPtr Record;
            internal double RecordTime;
            internal double RuntimeLead;
            internal long GlobalIndex;
            internal int RecordIndex;
            internal int RecordCount;
        }

        private static IntPtr _presentationQueuePtr;
        private static IntPtr _presentationEnqueueMethod;
        private static IntPtr _presentationTupleBuffer;
        private static int _generation = -1;
        private static int _sweepIndex;
        private static double _lastNow = double.NaN;
        private static int _errors;
        private static bool _queueErrorLogged;
        private static long _nextGlowSerial;
        private static bool _hasPreviousCursor;
        private static float _previousCursorX;
        private static readonly List<DueRecord> DueRecords =
            new List<DueRecord>(64);
        private static readonly List<SkyGroupState> FrameGroupStates =
            new List<SkyGroupState>(64);
        private static readonly List<SkyBlock> FrameBlocks =
            new List<SkyBlock>(32);
        private static int _frameSerial;
        private static readonly List<SkyBandRange> CoverageScratch =
            new List<SkyBandRange>(64);

        internal static void OnChartCompiled()
        {
            if (!BuildProfile.Supports(NativeCapability.SkyBlock))
                return;
            ChartRuntimeFrame frame = ChartRuntimeContext.Current;
            SkyBlockPlan plan =
                ChartRuntimeContext.IsActive
                    ? frame?.SkyBlocks
                    : null;
            if (plan == null || plan.JudgeMembers.Count == 0)
            {
                ResetForChartSwitch();
                return;
            }
            if (_generation != plan.Generation)
            {
                _generation = plan.Generation;
                _nextGlowSerial = 0;
                _sweepIndex = 0;
                _lastNow = double.NaN;
                ActiveLayers.Clear();
                PendingLayers.Clear();
                DueRecords.Clear();
                ResetBlockStates(plan);
            }
            SkyBlockRuntimeSlots.Ensure(plan);
        }

        internal static bool IsManagedMember(long id)
        {
            ChartRuntimeFrame frame = ChartRuntimeContext.Current;
            SkyBlockPlan plan =
                ChartRuntimeContext.IsActive
                    ? frame?.SkyBlocks
                    : null;
            return plan != null && plan.IsJudgeMember(id);
        }

        internal static void AfterNativeJudge(
            Il2Cpp_E._VD judge)
        {
            if (!BuildProfile.Supports(NativeCapability.SkyBlock))
                return;
            SkyBlockPlan plan = CurrentPlan();
            if (plan == null || plan.JudgeMembers.Count == 0 ||
                !SkyBlockPolicy.EnableSharedBranchJudgement ||
                judge == null || judge.Pointer == IntPtr.Zero ||
                !SkyBlockRuntimeSlots.Ensure(plan))
                return;

            try
            {
                if ((byte)Il2Cpp_E._VD._a != 2)
                    return;
                double now = BitConverter.Int64BitsToDouble(
                    Marshal.ReadInt64(
                        judge.Pointer +
                        NativeLayoutCatalog.SkyJudge.ChartTime)) *
                    1000.0;
                float cursorX = BitConverter.Int32BitsToSingle(
                    Marshal.ReadInt32(
                        judge.Pointer +
                        NativeLayoutCatalog.SkyJudge.CursorX));
                if (!IsFinite(now) || !IsFinite(cursorX))
                    return;

                if (!EnsurePresentationQueue(judge))
                    return;
                if (!double.IsNaN(_lastNow) &&
                    now + 0.001 < _lastNow)
                    _hasPreviousCursor = false;
                float sweptLo = _hasPreviousCursor
                    ? Math.Min(_previousCursorX, cursorX)
                    : cursorX;
                float sweptHi = _hasPreviousCursor
                    ? Math.Max(_previousCursorX, cursorX)
                    : cursorX;
                bool cursorMoved =
                    _hasPreviousCursor &&
                    sweptHi - sweptLo > 0.001f;
                SkyBandRange sweptRange =
                    new SkyBandRange(sweptLo, sweptHi);
                RefreshActiveLayers(plan, now);
                bool auto =
                    AutoplayRuntime.Enabled &&
                    !AutoplayRuntime.GroundOnly;
                EvaluateGroupStates(
                    now, cursorX, sweptRange,
                    cursorMoved, auto);
                ConsumeDueRecords(
                    now, cursorX, sweptRange,
                    cursorMoved, auto);
                AdvanceGroupStates(now, cursorX);
                UpdateSkyHoldFeedback(judge);
                _previousCursorX = cursorX;
                _hasPreviousCursor = true;
            }
            catch (Exception e)
            {
                LogError("judge", e);
            }
        }

        internal static void ResetForChartSwitch()
        {
            ChartRuntimeFrame frame = ChartRuntimeContext.Current;
            SkyBlockPlan plan =
                ChartRuntimeContext.IsActive
                    ? frame?.SkyBlocks
                    : null;
            if (plan != null)
                ResetBlockStates(plan);
            ActiveLayers.Clear();
            PendingLayers.Clear();
            _presentationQueuePtr = IntPtr.Zero;
            _presentationEnqueueMethod = IntPtr.Zero;
            if (_presentationTupleBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_presentationTupleBuffer);
                _presentationTupleBuffer = IntPtr.Zero;
            }
            _nextGlowSerial = 0;
            _generation = -1;
            _sweepIndex = 0;
            _lastNow = double.NaN;
            _queueErrorLogged = false;
            _hasPreviousCursor = false;
            _previousCursorX = 0f;
            DueRecords.Clear();
            FrameGroupStates.Clear();
            FrameBlocks.Clear();
            _frameSerial = 0;
            SkyBlockFeedbackRuntime.ResetForChartSwitch();
        }

        private static void EvaluateGroupStates(
            double now,
            float cursorX,
            SkyBandRange sweptRange,
            bool cursorMoved,
            bool auto)
        {
            _frameSerial++;
            FrameGroupStates.Clear();
            FrameBlocks.Clear();
            for (int i = 0; i < ActiveLayers.Count; i++)
            {
                SkyJudgmentLayer layer = ActiveLayers[i];
                for (int gi = 0; gi < layer.GroupStates.Count; gi++)
                {
                    SkyGroupState state = layer.GroupStates[gi];
                    BeginGroupStateEvaluation(
                        state, layer.StartMs);
                    if (!state.Covered &&
                        GroupContains(
                            layer, state, now, cursorX,
                            sweptRange, cursorMoved))
                    {
                        state.Covered = true;
                    }
                }
            }
            for (int i = 0; i < PendingLayers.Count; i++)
            {
                SkyJudgmentLayer layer = PendingLayers[i].Layer;
                for (int gi = 0; gi < layer.GroupStates.Count; gi++)
                    BeginGroupStateEvaluation(
                        layer.GroupStates[gi],
                        layer.StartMs);
            }

            for (int i = 0; i < FrameGroupStates.Count; i++)
            {
                SkyGroupState state = FrameGroupStates[i];
                if (auto)
                    state.Covered = true;
                if (!state.HasRecordClock)
                {
                    state.HasRecordClock = true;
                    state.LastRecordMs =
                        state.InitialRecordMs;
                    state.WarningMs = WarningMs;
                    state.PenaltyMs = 0.0;
                    state.Latch = state.Covered;
                }
            }
        }

        private static void BeginGroupStateEvaluation(
            SkyGroupState state, uint startMs)
        {
            if (state == null ||
                state.EvaluatedFrame == _frameSerial)
                return;
            state.EvaluatedFrame = _frameSerial;
            state.Covered = false;
            if (!state.HasRecordClock)
                state.InitialRecordMs = startMs;
            SkyBlock block = state.Block;
            if (block != null &&
                block.EvaluatedFrame != _frameSerial)
            {
                block.EvaluatedFrame = _frameSerial;
                FrameBlocks.Add(block);
            }
            FrameGroupStates.Add(state);
        }

        private static bool GroupContains(
            SkyJudgmentLayer layer,
            SkyGroupState state,
            double time,
            float cursorX,
            SkyBandRange sweptRange,
            bool cursorMoved)
        {
            for (int i = 0; i < state.Members.Count; i++)
            {
                SkyBlockMember member = state.Members[i];
                if (!MemberActiveAt(member, time))
                    continue;
                SkyBandRange range = member.RangeAt(time);
                if (range.Contains(cursorX, CursorTolerance) ||
                    (cursorMoved &&
                     range.Overlaps(
                         sweptRange, CursorTolerance)))
                    return true;
            }
            return ExpandedOverlapContains(
                layer, state, time, cursorX,
                sweptRange, cursorMoved);
        }

        private static void ConsumeDueRecords(
            double now,
            float cursorX,
            SkyBandRange sweptRange,
            bool cursorMoved,
            bool auto)
        {
            Il2Cppifapp.Game.LogicalNotePlayer player =
                Il2Cppifapp.Game.LogicalNotePlayer._Qe;
            if (player == null || player.Pointer == IntPtr.Zero)
                return;
            Il2Cpp_b._T compiled = player._ve;
            if (compiled == null ||
                compiled.Pointer == IntPtr.Zero)
                return;
            var records = compiled._XC;
            if (records == null ||
                records.Pointer == IntPtr.Zero)
                return;
            Il2Cppifapp.Game.Scenes.Game.Track track =
                Il2Cppifapp.Game.Scenes.Game.Track._a;
            if (track == null || track.Pointer == IntPtr.Zero)
                return;

            for (int li = 0; li < ActiveLayers.Count; li++)
            {
                SkyJudgmentLayer layer = ActiveLayers[li];
                ConsumeLayerRecords(
                    layer, records, track,
                    cursorX, sweptRange,
                    cursorMoved, auto);
            }
            for (int li = PendingLayers.Count - 1;
                 li >= 0;
                 li--)
            {
                PendingLayer pending = PendingLayers[li];
                ConsumeLayerRecords(
                    pending.Layer, records, track,
                    cursorX, sweptRange,
                    cursorMoved, auto);
                if (now > pending.KeepUntilMs)
                    PendingLayers.RemoveAt(li);
            }
        }

        private static unsafe void ConsumeLayerRecords(
            SkyJudgmentLayer layer,
            Il2CppSystem.Collections.Generic.Dictionary<
                long, Il2CppSystem.Memory<Il2Cpp_K._cH>> records,
            Il2Cppifapp.Game.Scenes.Game.Track track,
            float cursorX,
            SkyBandRange sweptRange,
            bool cursorMoved,
            bool auto)
        {
            // Native _Oz evaluates each record at
            // note.StartMs + record.RelativeTime. A shared layer can contain
            // records from more than one member, so merge them by that native
            // timestamp before advancing the 300/500ms state machine.
            DueRecords.Clear();
            for (int mi = 0;
                 mi < layer.Members.Count;
                 mi++)
            {
                SkyBlockMember member =
                    layer.Members[mi];
                if (!SkyBlockRuntimeSlots.TryNote(
                        member, out IntPtr note))
                    continue;
                if (!records.ContainsKey(member.Id))
                    continue;
                Il2CppSystem.Memory<Il2Cpp_K._cH> memory =
                    records[member.Id];
                if (memory._object == null ||
                    memory._object.Pointer == IntPtr.Zero)
                    continue;
                int count = memory._length;
                if (count <= 0 || count > 100000)
                    continue;
                IntPtr arrayObject =
                    memory._object.Pointer;
                int sliceIndex = memory._index;
                int arrayLength = Marshal.ReadInt32(
                    arrayObject +
                    NativeLayoutCatalog.ArrayLengthOffset);
                if (sliceIndex < 0 ||
                    sliceIndex + count > arrayLength)
                    continue;

                double ee = BitConverter.Int64BitsToDouble(
                    Marshal.ReadInt64(
                        note +
                        NativeLayoutCatalog.Note.RuntimeLead));
                for (int r = 0; r < count; r++)
                {
                    byte* record =
                        InteropUtil.RecordPointer(
                            arrayObject,
                            sliceIndex,
                            r);
                    if (*(record +
                            NativeLayoutCatalog.Record.Final) != 0 ||
                        *(record +
                            NativeLayoutCatalog.Record.Judgment) != 0)
                        continue;
                    double relative =
                        BitConverter.Int64BitsToDouble(
                            *(long*)(record +
                                NativeLayoutCatalog.Record
                                    .RelativeTime));
                    if (!IsFinite(relative) ||
                        relative + ee > 0.0)
                        continue;

                    DueRecords.Add(new DueRecord
                    {
                        Member = member,
                        Record = (IntPtr)record,
                        RecordTime = member.StartMs + relative,
                        RuntimeLead = ee,
                        GlobalIndex = *(long*)record,
                        RecordIndex = r,
                        RecordCount = count,
                    });
                }
            }

            if (DueRecords.Count > 1)
            {
                DueRecords.Sort((a, b) =>
                {
                    int c = a.RecordTime.CompareTo(b.RecordTime);
                    if (c != 0) return c;
                    c = a.Member.Id.CompareTo(b.Member.Id);
                    return c != 0
                        ? c
                        : a.GlobalIndex.CompareTo(b.GlobalIndex);
                });
            }

            for (int i = 0; i < DueRecords.Count; i++)
            {
                DueRecord due = DueRecords[i];
                SkyGroupState state =
                    due.Member.GroupState;
                if (state == null)
                    continue;
                SkyBandRange range =
                    due.Member.RangeAt(due.RecordTime);
                bool recordCovered =
                    auto ||
                    GroupContains(
                        layer, state, due.RecordTime,
                        cursorX, sweptRange, cursorMoved);
                UpdateWarningState(
                    state, due.RecordTime, recordCovered);
                double noteLength =
                    (double)(due.Member.EndMs -
                             due.Member.StartMs) +
                    due.RuntimeLead;
                byte grade;
                if (state.PenaltyMs > 0.0)
                {
                    grade = ChartExtState.JudgeMiss;
                }
                else if (!state.Latch)
                {
                    if (noteLength >= 0.0)
                        continue;
                    grade = ChartExtState.JudgeMiss;
                }
                else
                {
                    grade = ChartExtState.JudgeShiny;
                }
                Vector3 position = track._dBA(
                    (range.Lo + range.Hi) * 0.5f);
                byte* record = (byte*)due.Record;
                byte oldJudgment =
                    *(record +
                        NativeLayoutCatalog.Record.Judgment);
                byte oldFinal =
                    *(record +
                        NativeLayoutCatalog.Record.Final);
                *(record +
                    NativeLayoutCatalog.Record.Judgment) = grade;
                *(record +
                    NativeLayoutCatalog.Record.Final) = 1;
                if (!EnqueuePresentation(
                        due.Member.Id,
                        due.GlobalIndex,
                        position))
                {
                    *(record +
                        NativeLayoutCatalog.Record.Judgment) =
                        oldJudgment;
                    *(record +
                        NativeLayoutCatalog.Record.Final) =
                        oldFinal;
                    continue;
                }
                if (grade == ChartExtState.JudgeShiny &&
                    !state.HeadSoundPlayed)
                {
                    state.HeadSoundPlayed = true;
                    SkyBlockFeedbackRuntime.PlayHit();
                }
                if (due.RecordIndex == due.RecordCount - 1)
                {
                    due.Member.JudgementSettled = true;
                    due.Member.LastJudgement = grade;
                }
                if (grade != ChartExtState.JudgeNone &&
                    grade != ChartExtState.JudgeMiss)
                {
                    due.Member.WasJudged = true;
                    AssignGlowOwner(
                        state,
                        due.RecordTime,
                        cursorX,
                        due.Member);
                }
            }

        }

        private static void AdvanceGroupStates(
            double now, float cursorX)
        {
            for (int i = 0; i < FrameGroupStates.Count; i++)
            {
                SkyGroupState state = FrameGroupStates[i];
                UpdateWarningState(
                    state, now, state.Covered);
                if (state.Covered &&
                    !state.RedLatched &&
                    state.PenaltyMs <= 0.0 &&
                    state.WarningMs > 0.0 &&
                    (state.GlowOwner == null ||
                     state.GlowOwner.GlowSerial <= 0L))
                {
                    AssignGlowOwner(
                        state, now, cursorX, null);
                }
            }
            for (int i = 0; i < FrameBlocks.Count; i++)
                UpdateBlockVisualState(FrameBlocks[i]);
        }

        private static bool ExpandedOverlapContains(
            SkyJudgmentLayer layer,
            SkyGroupState state,
            double time,
            float cursorX,
            SkyBandRange sweptRange,
            bool cursorMoved)
        {
            float padding = SkyBlockPolicy.OverlapHitPadding;
            if (padding <= 0f ||
                layer.Block == null ||
                layer.Block.Mode != SkyBlockMode.Normal ||
                state == null ||
                layer.Members.Count == 0)
                return false;

            CoverageScratch.Clear();
            for (int i = 0; i < state.Members.Count; i++)
            {
                SkyBlockMember member = state.Members[i];
                if (!MemberActiveAt(member, time))
                    continue;
                CoverageScratch.Add(
                    member.RangeAt(time));
            }
            if (CoverageScratch.Count == 0)
                return false;
            CoverageScratch.Sort(CompareRanges);

            int clusterStart = 0;
            while (clusterStart < CoverageScratch.Count)
            {
                float clusterLo =
                    CoverageScratch[clusterStart].Lo;
                float clusterHi =
                    CoverageScratch[clusterStart].Hi;
                int clusterEnd = clusterStart + 1;
                while (clusterEnd < CoverageScratch.Count)
                {
                    SkyBandRange next =
                        CoverageScratch[clusterEnd];
                    if (!next.OverlapsWithArea(
                            new SkyBandRange(
                                clusterLo, clusterHi)))
                        break;
                    if (next.Hi > clusterHi)
                        clusterHi = next.Hi;
                    clusterEnd++;
                }

                bool hasPadding = clusterEnd - clusterStart > 1;
                if (!hasPadding)
                {
                    hasPadding = HasExternalOverlap(
                        layer.Block,
                        new SkyBandRange(
                            clusterLo, clusterHi),
                        time);
                }
                if (hasPadding)
                {
                    SkyBandRange expanded =
                        new SkyBandRange(
                            clusterLo - padding,
                            clusterHi + padding);
                    if (expanded.Contains(
                            cursorX, CursorTolerance) ||
                        (cursorMoved &&
                         expanded.Overlaps(
                             sweptRange, CursorTolerance)))
                        return true;
                }
                clusterStart = clusterEnd;
            }
            return false;
        }

        private static bool HasExternalOverlap(
            SkyBlock block,
            SkyBandRange ownCluster,
            double time)
        {
            for (int pi = 0;
                 pi < block.OverlapPeers.Count;
                 pi++)
            {
                SkyBlock peer = block.OverlapPeers[pi];
                SkyJudgmentLayer peerLayer =
                    FindTimelineLayer(peer, time);
                if (peerLayer == null)
                    continue;
                for (int mi = 0;
                     mi < peerLayer.Members.Count;
                     mi++)
                {
                    SkyBlockMember member =
                        peerLayer.Members[mi];
                    if (!MemberActiveAt(member, time))
                        continue;
                    if (ownCluster.OverlapsWithArea(
                            member.RangeAt(time)))
                        return true;
                }
            }
            return false;
        }

        private static SkyJudgmentLayer FindTimelineLayer(
            SkyBlock block,
            double time)
        {
            int lo = 0;
            int hi = block.TimelineLayers.Count - 1;
            int candidate = -1;
            while (lo <= hi)
            {
                int mid = lo + ((hi - lo) >> 1);
                SkyJudgmentLayer layer =
                    block.TimelineLayers[mid];
                if (layer.StartMs <= time)
                {
                    candidate = mid;
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }
            if (candidate < 0) return null;
            SkyJudgmentLayer found =
                block.TimelineLayers[candidate];
            return time < found.EndMs ? found : null;
        }

        private static int CompareRanges(
            SkyBandRange a, SkyBandRange b)
        {
            int c = a.Lo.CompareTo(b.Lo);
            return c != 0 ? c : a.Hi.CompareTo(b.Hi);
        }

        private static bool MemberActiveAt(
            SkyBlockMember member, double time)
            => time >= member.StartMs &&
               time <= member.EndMs;

        private static void UpdateBlockVisualState(
            SkyBlock block)
        {
            if (block == null) return;
            float red = 0f;
            bool covered = false;
            bool everCovered = false;
            for (int i = 0; i < block.GroupStates.Count; i++)
            {
                SkyGroupState state = block.GroupStates[i];
                red = Math.Max(red, ComputeRedFactor(state));
                covered |= state.Covered;
                everCovered |= state.Latch;
            }
            block.RedFactor = red;
            block.Covered = covered;
            block.Broken = !covered;
            block.HasEverCovered = everCovered;
        }

        internal static bool TryGetMemberRedFactor(
            SkyBlockMember member,
            double now,
            out float red)
        {
            red = 0f;
            if (member == null) return false;
            SkyGroupState state = member.GroupState;
            if (state == null) return false;
            red = ComputeRedFactor(state);
            return true;
        }

        private static float ComputeRedFactor(
            SkyGroupState state)
        {
            if (state == null) return 0f;
            if (!state.HasRecordClock) return 0f;
            double progress =
                1.0 - state.WarningMs / WarningMs;
            return state.RedLatched
                ? 1f
                : progress > 0.25
                ? (float)Clamp(progress, 0.0, 1.0)
                : 0f;
        }

        internal static bool IsMemberInNormalState(
            SkyBlockMember member, double now)
        {
            return IsMemberInState(
                member, requireCoverage: false);
        }

        internal static bool IsMemberGlowActive(
            SkyBlockMember member, double now)
        {
            if (member == null) return false;
            SkyGroupState state = member.GroupState;
            return state != null &&
                   state.GlowOwner == member &&
                   member.GlowSerial > 0L &&
                   IsGroupGlowActive(state);
        }

        private static bool IsMemberInState(
            SkyBlockMember member,
            bool requireCoverage)
        {
            if (member == null) return false;
            SkyGroupState state = member.GroupState;
            return state != null &&
                   IsGroupNormal(state, requireCoverage);
        }

        private static bool IsGroupNormal(
            SkyGroupState state,
            bool requireCoverage)
        {
            return (!requireCoverage || state.Covered) &&
                   !state.RedLatched &&
                   state.WarningMs > 0.0 &&
                   state.PenaltyMs <= 0.0;
        }

        private static bool IsGroupGlowActive(
            SkyGroupState state)
        {
            return !state.RedLatched &&
                   state.WarningMs > 0.0 &&
                   state.PenaltyMs <= 0.0;
        }

        private static void AssignGlowOwner(
            SkyGroupState state,
            double recordTime,
            float cursorX,
            SkyBlockMember judgedMember)
        {
            SkyBlockMember unique = null;
            SkyBlockMember stable = null;
            SkyBlockMember first = null;
            int containing = 0;
            SkyBlock block = state.Block;
            if (block == null) return;
            for (int i = 0; i < state.Members.Count; i++)
            {
                SkyBlockMember member = state.Members[i];
                if (!MemberActiveAt(member, recordTime))
                    continue;
                if (first == null)
                    first = member;
                if (!member.RangeAt(recordTime).Contains(
                        cursorX, CursorTolerance))
                    continue;
                containing++;
                unique = member;
                if (stable == null ||
                    member.Id < stable.Id)
                    stable = member;
            }

            SkyBlockMember owner;
            if (containing == 1)
            {
                owner = unique;
            }
            else if (containing > 1)
            {
                owner = state.GlowOwner;
                if (owner == null ||
                    owner.GroupState != state)
                    owner = stable;
            }
            else
            {
                owner = state.GlowOwner;
                if (owner == null ||
                    owner.GroupState != state)
                    owner = judgedMember ?? first;
            }
            if (owner == null) return;

            if (state.GlowOwner != owner &&
                state.GlowOwner != null)
                state.GlowOwner.GlowSerial = 0;
            state.GlowOwner = owner;
            owner.GlowSerial = ++_nextGlowSerial;
        }

        private static void ClearGlowOwner(
            SkyGroupState state)
        {
            if (state?.GlowOwner != null)
                state.GlowOwner.GlowSerial = 0;
            if (state != null)
                state.GlowOwner = null;
        }

        private static void UpdateWarningState(
            SkyGroupState state,
            double recordTime,
            bool covered)
        {
            if (!state.HasRecordClock)
            {
                state.HasRecordClock = true;
                state.LastRecordMs = recordTime;
                state.WarningMs = WarningMs;
                state.PenaltyMs = 0.0;
                return;
            }

            double delta =
                recordTime - state.LastRecordMs;
            if (delta < 0.0)
                return;

            double oldPenalty = state.PenaltyMs;
            double remainingDelta = delta;
            state.PenaltyMs = Clamp(
                state.PenaltyMs - delta,
                0.0, PenaltyMs);
            if (state.PenaltyMs <= 0.0 &&
                oldPenalty > 0.0)
            {
                state.WarningMs = WarningMs;
                // _oz advances LastRecordTime by the consumed penalty before
                // applying the remaining delta to the warning value.
                remainingDelta =
                    Math.Max(0.0, delta - oldPenalty);
            }

            if (covered && state.PenaltyMs <= 0.0)
            {
                state.WarningMs = Clamp(
                    state.WarningMs + remainingDelta,
                    0.0, WarningMs);
            }
            else
            {
                state.WarningMs = Clamp(
                    state.WarningMs -
                        2.0 * remainingDelta,
                    0.0, WarningMs);
                if (state.WarningMs <= 0.0 &&
                    state.PenaltyMs <= 0.0)
                {
                    state.Latch = false;
                    state.PenaltyMs = PenaltyMs;
                    state.RedLatched = true;
                    ClearGlowOwner(state);
                }
            }
            if (covered &&
                state.PenaltyMs <= 0.0 &&
                state.WarningMs > 0.0)
            {
                state.Latch = true;
                state.RedLatched = false;
            }
            state.LastRecordMs = recordTime;
        }

        private static void UpdateSkyHoldFeedback(
            Il2Cpp_E._VD judge)
        {
            bool independentActive = false;
            for (int i = 0; i < FrameGroupStates.Count; i++)
            {
                SkyGroupState state = FrameGroupStates[i];
                if (!state.HeadSoundPlayed ||
                    !state.Covered ||
                    state.PenaltyMs > 0.0 ||
                    state.WarningMs <= 0.0)
                    continue;
                independentActive = true;
                break;
            }

            bool nativeActive = false;
            if (judge != null &&
                judge.Pointer != IntPtr.Zero)
            {
                double warning =
                    BitConverter.Int64BitsToDouble(
                        Marshal.ReadInt64(
                            judge.Pointer +
                            NativeLayoutCatalog.SkyJudge.WarningTime));
                double penalty =
                    BitConverter.Int64BitsToDouble(
                        Marshal.ReadInt64(
                            judge.Pointer +
                            NativeLayoutCatalog.SkyJudge.PenaltyTime));
                bool covered =
                    Marshal.ReadByte(
                        judge.Pointer +
                        NativeLayoutCatalog.SkyJudge.SpaceCovered) != 0;
                nativeActive =
                    IsFinite(warning) &&
                    IsFinite(penalty) &&
                    warning > 0.0 &&
                    penalty <= 0.0 &&
                    covered;
            }
            SkyBlockFeedbackRuntime.SetSkyHoldActive(
                independentActive,
                nativeActive);
        }

        private static unsafe bool EnqueuePresentation(
            long noteId, long recordIndex, Vector3 position)
        {
            if (_presentationEnqueueMethod == IntPtr.Zero ||
                _presentationTupleBuffer == IntPtr.Zero)
                return false;
            Marshal.WriteInt64(
                _presentationTupleBuffer +
                NativeLayoutCatalog.Presentation.NoteId,
                noteId);
            Marshal.WriteInt32(
                _presentationTupleBuffer +
                NativeLayoutCatalog.Presentation.RecordStart,
                -1);
            Marshal.WriteInt32(
                _presentationTupleBuffer +
                NativeLayoutCatalog.Presentation.RecordCount,
                -1);
            Marshal.WriteInt64(
                _presentationTupleBuffer +
                NativeLayoutCatalog.Presentation.RecordIndex,
                recordIndex);
            Marshal.WriteInt32(
                _presentationTupleBuffer +
                NativeLayoutCatalog.Presentation.PositionX,
                BitConverter.SingleToInt32Bits(position.x));
            Marshal.WriteInt32(
                _presentationTupleBuffer +
                NativeLayoutCatalog.Presentation.PositionY,
                BitConverter.SingleToInt32Bits(position.y));
            Marshal.WriteInt32(
                _presentationTupleBuffer +
                NativeLayoutCatalog.Presentation.PositionZ,
                BitConverter.SingleToInt32Bits(position.z));

            IntPtr* arguments = stackalloc IntPtr[1];
            arguments[0] = _presentationTupleBuffer;
            IntPtr exception = IntPtr.Zero;
            IL2CPP.il2cpp_runtime_invoke(
                _presentationEnqueueMethod,
                _presentationQueuePtr,
                (void**)arguments,
                ref exception);
            if (exception != IntPtr.Zero)
            {
                LogError(
                    "presentation",
                    new InvalidOperationException(
                        "native Queue.Enqueue raised an exception"));
                return false;
            }
            return true;
        }

        private static bool EnsurePresentationQueue(
            Il2Cpp_E._VD judge)
        {
            IntPtr queuePtr = Marshal.ReadIntPtr(
                judge.Pointer +
                NativeLayoutCatalog.SkyJudge.PendingPresentation);
            if (queuePtr == IntPtr.Zero) return false;
            if (_presentationEnqueueMethod != IntPtr.Zero &&
                _presentationQueuePtr == queuePtr)
                return true;
            _presentationQueuePtr = queuePtr;
            _presentationEnqueueMethod =
                FindPresentationEnqueue(queuePtr);
            if (_presentationEnqueueMethod == IntPtr.Zero)
            {
                if (!_queueErrorLogged)
                {
                    _queueErrorLogged = true;
                    ChartExtMod.Log.Error(
                        "[ChartExt] skyblock judge: " +
                        "_PEA Enqueue signature was not found");
                }
                return false;
            }
            if (_presentationTupleBuffer == IntPtr.Zero)
                _presentationTupleBuffer =
                    Marshal.AllocHGlobal(
                        NativeLayoutCatalog.Presentation.Stride);
            return _presentationTupleBuffer != IntPtr.Zero;
        }

        private static IntPtr FindPresentationEnqueue(
            IntPtr queuePtr)
        {
            IntPtr klass =
                IL2CPP.il2cpp_object_get_class(queuePtr);
            if (klass == IntPtr.Zero) return IntPtr.Zero;
            IL2CPP.il2cpp_runtime_class_init(klass);
            IntPtr iterator = IntPtr.Zero;
            while (true)
            {
                IntPtr method =
                    IL2CPP.il2cpp_class_get_methods(
                        klass, ref iterator);
                if (method == IntPtr.Zero) break;
                IntPtr namePtr =
                    IL2CPP.il2cpp_method_get_name(method);
                if (namePtr == IntPtr.Zero) continue;
                string name = Marshal.PtrToStringAnsi(namePtr);
                if (!string.Equals(
                        name,
                        NativeLayoutCatalog.Presentation
                            .EnqueueMethodName,
                        StringComparison.Ordinal))
                    continue;
                if (IL2CPP.il2cpp_method_get_param_count(
                        method) != 1)
                    continue;
                IntPtr paramType =
                    IL2CPP.il2cpp_method_get_param(method, 0);
                IntPtr paramClass =
                    paramType == IntPtr.Zero
                        ? IntPtr.Zero
                        : IL2CPP.il2cpp_class_from_type(paramType);
                if (paramClass == IntPtr.Zero)
                    continue;
                uint alignment = 0;
                int valueSize =
                    (int)IL2CPP.il2cpp_class_value_size(
                        paramClass, ref alignment);
                if (valueSize !=
                    NativeLayoutCatalog.Presentation
                        .EnqueueParameterSize)
                    continue;
                return method;
            }
            return IntPtr.Zero;
        }

        private static void RefreshActiveLayers(
            SkyBlockPlan plan, double now)
        {
            bool rewind =
                !double.IsNaN(_lastNow) &&
                now + 0.001 < _lastNow;
            if (double.IsNaN(_lastNow) || rewind)
            {
                ActiveLayers.Clear();
                PendingLayers.Clear();
                _sweepIndex = 0;
                if (rewind) ResetBlockStates(plan);
                for (int i = 0;
                     i < plan.JudgeLayers.Count;
                     i++)
                {
                    SkyJudgmentLayer layer =
                        plan.JudgeLayers[i];
                    if (now >= layer.StartMs &&
                        now < layer.EndMs)
                        ActiveLayers.Add(layer);
                    if (layer.StartMs <= now &&
                        i >= _sweepIndex)
                        _sweepIndex = i + 1;
                }
            }
            else
            {
                for (int i = ActiveLayers.Count - 1;
                     i >= 0;
                     i--)
                {
                    if (now < ActiveLayers[i].EndMs)
                        continue;
                    SkyJudgmentLayer ended =
                        ActiveLayers[i];
                    PendingLayers.Add(new PendingLayer
                    {
                        Layer = ended,
                        KeepUntilMs = ended.EndMs + 2000.0,
                    });
                    int last = ActiveLayers.Count - 1;
                    ActiveLayers[i] = ActiveLayers[last];
                    ActiveLayers.RemoveAt(last);
                }
                while (_sweepIndex < plan.JudgeLayers.Count)
                {
                    SkyJudgmentLayer layer =
                        plan.JudgeLayers[_sweepIndex];
                    if (layer.StartMs > now) break;
                    _sweepIndex++;
                    if (now < layer.EndMs)
                        ActiveLayers.Add(layer);
                    else
                        PendingLayers.Add(new PendingLayer
                        {
                            Layer = layer,
                            KeepUntilMs = now + 2000.0,
                        });
                }
            }
            _lastNow = now;
        }

        private static void ResetBlockStates(
            SkyBlockPlan plan)
        {
            if (plan == null) return;
            for (int i = 0; i < plan.Blocks.Count; i++)
            {
                SkyBlock block = plan.Blocks[i];
                block.Covered = false;
                block.Broken = false;
                block.HasEverCovered = false;
                block.RedFactor = 0f;
                block.EvaluatedFrame = 0;
                for (int mi = 0;
                     mi < block.Members.Count;
                     mi++)
                    block.Members[mi].GlowSerial = 0L;
                for (int gi = 0;
                     gi < block.GroupStates.Count;
                     gi++)
                {
                    SkyGroupState state =
                        block.GroupStates[gi];
                    state.Covered = false;
                    state.Latch = false;
                    state.RedLatched = false;
                    state.HeadSoundPlayed = false;
                    ClearGlowOwner(state);
                    state.WarningMs = 0.0;
                    state.PenaltyMs = 0.0;
                    state.LastRecordMs = 0.0;
                    state.InitialRecordMs = 0.0;
                    state.HasRecordClock = false;
                    state.EvaluatedFrame = 0;
                }
            }
        }

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

        private static double Clamp(
            double value, double min, double max)
            => value < min
                ? min
                : value > max ? max : value;

        private static void LogError(
            string where, Exception error)
        {
            if (_errors++ >= 20) return;
            try
            {
                ChartExtMod.Log.Error(
                    $"[ChartExt] skyblock judge {where} failed: " +
                    error);
            }
            catch { }
        }
    }
}
