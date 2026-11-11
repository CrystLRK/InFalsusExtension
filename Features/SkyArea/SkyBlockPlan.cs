using System;
using System.Collections.Generic;

namespace InFalsusChartExt
{
    internal enum SkyBlockMode
    {
        Normal,
    }

    internal struct SkyBandRange
    {
        internal float Lo;
        internal float Hi;

        internal SkyBandRange(float lo, float hi)
        {
            Lo = lo;
            Hi = hi;
        }

        internal bool Contains(float value, float tolerance)
            => value >= Lo - tolerance && value <= Hi + tolerance;

        internal bool Overlaps(SkyBandRange other, float tolerance)
            => Math.Min(Hi, other.Hi) + tolerance >=
               Math.Max(Lo, other.Lo);

        internal bool OverlapsWithArea(SkyBandRange other)
            => Math.Min(Hi, other.Hi) -
               Math.Max(Lo, other.Lo) > 1e-6f;
    }

    internal sealed class SkyBlockMember
    {
        internal long Id;
        internal long AuthoredGroup;
        internal int TimingGroup;
        internal SkyBlockMode Mode;
        internal uint StartMs;
        internal uint EndMs;
        internal double StartCenter;
        internal double StartWidth;
        internal double EndCenter;
        internal double EndWidth;
        internal int LeftEase;
        internal int RightEase;

        internal int RuntimeGroup;
        internal int VisualIndex = -1;
        internal SkyBlock Block;
        internal SkyGroupState GroupState;
        internal bool IsFirst = true;
        internal bool IsLast = true;
        internal bool HasAdjacentSameAuthoredPrevious;
        internal SkyBlockMember AdjacentSameAuthoredPrevious;
        internal bool HasPreviousEdge;
        internal bool HasNextEdge;
        internal SkyBandRange PreviousRange = new SkyBandRange(0f, 1f);
        internal SkyBandRange NextRange = new SkyBandRange(0f, 1f);
        internal readonly List<SkyBandRange> PreviousInternalRanges =
            new List<SkyBandRange>(2);
        internal readonly List<SkyBandRange> NextInternalRanges =
            new List<SkyBandRange>(2);

        internal int Side = -1;
        internal int Position = -1;
        internal bool WasJudged;
        internal bool JudgementSettled;
        internal byte LastJudgement;
        internal long GlowSerial;

        internal SkyBandRange RangeAt(double timeMs)
        {
            if (EndMs <= StartMs)
                return new SkyBandRange(
                    (float)(EndCenter - EndWidth * 0.5),
                    (float)(EndCenter + EndWidth * 0.5));

            double u = (timeMs - StartMs) /
                       (double)(EndMs - StartMs);
            if (u <= 0.0) u = 0.0;
            else if (u >= 1.0) u = 1.0;

            double left = StartCenter - StartWidth * 0.5;
            double startRight = StartCenter + StartWidth * 0.5;
            double endLeft = EndCenter - EndWidth * 0.5;
            double right = EndCenter + EndWidth * 0.5;
            double easedLeft = Ease(LeftEase, u);
            double easedRight = Ease(RightEase, u);
            return new SkyBandRange(
                (float)(left + (endLeft - left) * easedLeft),
                (float)(startRight + (right - startRight) * easedRight));
        }

        private static double Ease(int code, double value)
        {
            switch (code)
            {
                case 1:
                    return Math.Sin(value * Math.PI * 0.5);
                case 2:
                    return 1.0 - Math.Cos(value * Math.PI * 0.5);
                default:
                    return value;
            }
        }
    }

    /// <summary>
    /// Native warning/penalty state for one authored group inside one connected
    /// SkyBlock. A mixed timeline layer may contain several of these states,
    /// but no two authored groups ever share one state instance.
    /// </summary>
    internal sealed class SkyGroupState
    {
        internal SkyBlock Block;
        internal bool Covered;
        internal bool Latch;
        internal bool RedLatched;
        internal bool HeadSoundPlayed;
        internal SkyBlockMember GlowOwner;
        internal double WarningMs;
        internal double PenaltyMs;
        internal double LastRecordMs;
        internal double InitialRecordMs;
        internal bool HasRecordClock;
        internal int EvaluatedFrame;
        internal SkyJudgmentLayer LastTimelineLayer;
        internal readonly List<SkyBlockMember> Members =
            new List<SkyBlockMember>(4);
    }

    internal sealed class SkyJudgmentLayer
    {
        internal SkyBlock Block;
        internal uint StartMs;
        internal uint EndMs;
        internal readonly List<SkyBlockMember> Members =
            new List<SkyBlockMember>(4);
        internal readonly List<SkyGroupState> GroupStates =
            new List<SkyGroupState>(2);
    }

    internal sealed class SkyBlock
    {
        internal int RuntimeGroup;
        internal SkyBlockMode Mode;
        internal uint StartMs;
        internal uint EndMs;
        internal bool RequiresIndependentJudge;
        internal bool Covered;
        internal bool Broken;
        internal bool HasEverCovered;
        internal float RedFactor;
        internal int EvaluatedFrame;
        internal readonly List<SkyBlockMember> Members =
            new List<SkyBlockMember>(8);
        internal readonly List<SkyGroupState> GroupStates =
            new List<SkyGroupState>(2);
        internal readonly List<SkyJudgmentLayer> TimelineLayers =
            new List<SkyJudgmentLayer>(8);
        internal readonly List<SkyJudgmentLayer> SharedLayers =
            new List<SkyJudgmentLayer>(4);
        internal readonly List<SkyBlock> OverlapPeers =
            new List<SkyBlock>(4);
    }

    /// <summary>
    /// Runtime-only SkyArea block description. Source ids, times, geometry,
    /// flags and authored groups are immutable. This plan never writes note
    /// group or payload fields.
    /// </summary>
    internal sealed class SkyBlockPlan
    {
        private const double JoinTimeToleranceMs = 2.0;
        private const float JoinSpaceTolerance = 1e-9f;

        internal readonly Dictionary<long, SkyBlockMember> MemberById =
            new Dictionary<long, SkyBlockMember>();
        internal readonly List<SkyBlockMember> Members =
            new List<SkyBlockMember>();
        internal readonly List<SkyBlockMember> VisualMembers =
            new List<SkyBlockMember>();
        internal readonly List<SkyBlockMember> JudgeMembers =
            new List<SkyBlockMember>();
        internal readonly HashSet<long> VisualMemberIds =
            new HashSet<long>();
        internal readonly HashSet<long> JudgeMemberIds =
            new HashSet<long>();
        internal readonly List<SkyBlock> Blocks =
            new List<SkyBlock>();
        internal readonly List<SkyJudgmentLayer> SharedLayers =
            new List<SkyJudgmentLayer>();
        internal readonly List<SkyJudgmentLayer> JudgeLayers =
            new List<SkyJudgmentLayer>();
        internal int Generation;

        internal bool IsVisualMember(long id)
            => VisualMemberIds.Contains(id);

        internal bool IsJudgeMember(long id)
            => JudgeMemberIds.Contains(id);

        /// <summary>
        /// Writes the monotonic runtime-group alias into the in-memory payload
        /// handed to the game. Authored groups stay in each SkyBlockMember for
        /// setcolor and diagnostics; neither the source chart nor converter is
        /// changed.
        /// </summary>
        internal int ApplyRuntimeGroups(
            IReadOnlyList<Icp1Codec.Note> notes)
        {
            if (notes == null || notes.Count == 0)
                return 0;
            var ordered = new List<Icp1Codec.Note>(notes.Count);
            for (int i = 0; i < notes.Count; i++)
            {
                Icp1Codec.Note note = notes[i];
                if (note != null && note.Type == 5)
                    ordered.Add(note);
            }
            ordered.Sort((a, b) =>
            {
                int c = a.StartMs.CompareTo(b.StartMs);
                if (c != 0) return c;
                c = a.EndMs.CompareTo(b.EndMs);
                return c != 0 ? c : a.Index.CompareTo(b.Index);
            });

            int nextRuntimeGroup = 0;
            int changed = 0;
            bool hasPrevious = false;
            (int TimingGroup, long Authored) previousKey = default;
            int previousRuntimeGroup = 0;
            uint previousEndMs = 0;
            for (int i = 0; i < ordered.Count; i++)
            {
                Icp1Codec.Note note = ordered[i];
                int timingGroup =
                    ChartExtState.TimingGroupOf(note.Index);
                long authored =
                    (long)((ulong)note.Group & 0xFFFFFFFFUL);
                var key = (timingGroup, authored);
                bool sameRun =
                    hasPrevious &&
                    key == previousKey &&
                    note.StartMs <= previousEndMs + 2u;
                int runtimeGroup;
                if (!sameRun)
                {
                    runtimeGroup = nextRuntimeGroup++;
                }
                else
                {
                    runtimeGroup = previousRuntimeGroup;
                }
                hasPrevious = true;
                previousKey = key;
                previousRuntimeGroup = runtimeGroup;
                previousEndMs = sameRun
                    ? Math.Max(previousEndMs, note.EndMs)
                    : note.EndMs;

                long alias = (uint)runtimeGroup;
                long updated =
                    (note.Group & ~0xFFFFFFFFL) | alias;
                if (note.Group == updated)
                    continue;
                note.Group = updated;
                changed++;
            }
            return changed;
        }

        internal void Clear()
        {
            MemberById.Clear();
            Members.Clear();
            VisualMembers.Clear();
            JudgeMembers.Clear();
            VisualMemberIds.Clear();
            JudgeMemberIds.Clear();
            Blocks.Clear();
            SharedLayers.Clear();
            JudgeLayers.Clear();
            Generation = 0;
        }

        internal static SkyBlockPlan TryBuild(
            IReadOnlyList<Icp1Codec.Note> notes,
            int generation)
        {
            if (notes == null || notes.Count == 0) return null;
            var plan = new SkyBlockPlan { Generation = generation };
            try
            {
                plan.Build(notes);
                return plan;
            }
            catch (Exception e)
            {
                try
                {
                    ChartExtMod.Log.Error(
                        "[ChartExt] sky block plan failed: " + e);
                }
                catch { }
                plan.Clear();
                return null;
            }
        }

        private void Build(IReadOnlyList<Icp1Codec.Note> notes)
        {
            for (int i = 0; i < notes.Count; i++)
            {
                Icp1Codec.Note note = notes[i];
                if (note == null || note.Type != 5) continue;
                if (!TryReadGeometry(
                        note,
                        out double startCenter,
                        out double startWidth,
                        out double endCenter,
                        out double endWidth))
                    continue;

                float startLo =
                    (float)(startCenter - startWidth * 0.5);
                float startHi =
                    (float)(startCenter + startWidth * 0.5);
                float endLo =
                    (float)(endCenter - endWidth * 0.5);
                float endHi =
                    (float)(endCenter + endWidth * 0.5);
                var member = new SkyBlockMember
                {
                    Id = note.Index,
                    AuthoredGroup = (long)(
                        (ulong)note.Group & 0xFFFFFFFFUL),
                    TimingGroup =
                        ChartExtState.TimingGroupOf(note.Index),
                    Mode = ModeOf(note),
                    StartMs = note.StartMs,
                    EndMs = note.EndMs,
                    StartCenter = startCenter,
                    StartWidth = startWidth,
                    EndCenter = endCenter,
                    EndWidth = endWidth,
                    LeftEase = DecodeEase(
                        note.Flags & 0x1Cu, 0x04, 0x08, 0x10),
                    RightEase = DecodeEase(
                        note.Flags & 0xE0u, 0x20, 0x40, 0x80),
                };
                Members.Add(member);
                MemberById[member.Id] = member;
            }

            if (Members.Count == 0) return;
            Members.Sort((a, b) =>
            {
                int c = a.StartMs.CompareTo(b.StartMs);
                if (c != 0) return c;
                c = a.EndMs.CompareTo(b.EndMs);
                return c != 0 ? c : a.Id.CompareTo(b.Id);
            });
            var lastByAuthoredGroup =
                new Dictionary<(SkyBlockMode, long),
                    SkyBlockMember>();
            for (int i = 0; i < Members.Count; i++)
            {
                SkyBlockMember current = Members[i];
                var key = (
                    current.Mode,
                    current.AuthoredGroup);
                if (lastByAuthoredGroup.TryGetValue(
                        key, out SkyBlockMember previous) &&
                    previous.StartMs < current.StartMs)
                {
                    current.HasAdjacentSameAuthoredPrevious =
                        Math.Abs(
                            (long)current.StartMs -
                            previous.EndMs) <=
                        JoinTimeToleranceMs;
                    if (current.HasAdjacentSameAuthoredPrevious)
                        current.AdjacentSameAuthoredPrevious =
                            previous;
                }
                lastByAuthoredGroup[key] = current;
            }

            var parent = new int[Members.Count];
            for (int i = 0; i < parent.Length; i++)
                parent[i] = i;
            var edges = new List<(int Earlier, int Later)>();
            var tails = new Dictionary<long, List<int>>();

            for (int i = 0; i < Members.Count; i++)
            {
                SkyBlockMember current = Members[i];
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
                    if (!tails.TryGetValue(
                            bucket, out List<int> candidates))
                        continue;
                    for (int ci = 0; ci < candidates.Count; ci++)
                    {
                        int previousIndex = candidates[ci];
                        SkyBlockMember previous =
                            Members[previousIndex];
                        if (!CanConnect(previous, current))
                            continue;
                        int a = Find(parent, previousIndex);
                        int b = Find(parent, i);
                        if (a != b) parent[b] = a;
                        edges.Add((previousIndex, i));
                    }
                }

                long endBucket = (long)Math.Floor(
                    current.EndMs / JoinTimeToleranceMs);
                if (!tails.TryGetValue(
                        endBucket, out List<int> endList))
                    tails[endBucket] = endList = new List<int>(4);
                endList.Add(i);
            }

            var byRoot = new Dictionary<int, SkyBlock>();
            for (int i = 0; i < Members.Count; i++)
            {
                int root = Find(parent, i);
                if (!byRoot.TryGetValue(root, out SkyBlock block))
                {
                    block = new SkyBlock
                    {
                        Mode = Members[i].Mode,
                        StartMs = Members[i].StartMs,
                        EndMs = Members[i].EndMs,
                    };
                    byRoot[root] = block;
                    Blocks.Add(block);
                }
                block.Members.Add(Members[i]);
                Members[i].Block = block;
                if (Members[i].StartMs < block.StartMs)
                    block.StartMs = Members[i].StartMs;
                if (Members[i].EndMs > block.EndMs)
                    block.EndMs = Members[i].EndMs;
            }

            Blocks.Sort((a, b) =>
            {
                int c = a.StartMs.CompareTo(b.StartMs);
                if (c != 0) return c;
                long aid = a.Members.Count > 0
                    ? a.Members[0].Id
                    : long.MaxValue;
                long bid = b.Members.Count > 0
                    ? b.Members[0].Id
                    : long.MaxValue;
                return aid.CompareTo(bid);
            });

            for (int i = 0; i < edges.Count; i++)
            {
                SkyBlockMember earlier =
                    Members[edges[i].Earlier];
                SkyBlockMember later =
                    Members[edges[i].Later];
                if (earlier.Block != later.Block) continue;

                float lo = Math.Max(
                    (float)(earlier.EndCenter -
                            earlier.EndWidth * 0.5),
                    (float)(later.StartCenter -
                            later.StartWidth * 0.5));
                float hi = Math.Min(
                    (float)(earlier.EndCenter +
                            earlier.EndWidth * 0.5),
                    (float)(later.StartCenter +
                            later.StartWidth * 0.5));
                if (lo > hi + JoinSpaceTolerance)
                    continue;
                if (lo > hi)
                {
                    float contact = (lo + hi) * 0.5f;
                    lo = contact;
                    hi = contact;
                }

                earlier.IsLast = false;
                later.IsFirst = false;
                AddOrMergeRange(
                    earlier.NextInternalRanges,
                    new SkyBandRange(lo, hi));
                AddOrMergeRange(
                    later.PreviousInternalRanges,
                    new SkyBandRange(lo, hi));
                earlier.HasNextEdge = true;
                later.HasPreviousEdge = true;
            }

            for (int bi = 0; bi < Blocks.Count; bi++)
            {
                SkyBlock block = Blocks[bi];
                block.RuntimeGroup = bi;
                block.Members.Sort((a, b) =>
                    a.Id.CompareTo(b.Id));
                for (int mi = 0; mi < block.Members.Count; mi++)
                {
                    SkyBlockMember member = block.Members[mi];
                    member.RuntimeGroup = block.RuntimeGroup;
                    member.PreviousRange =
                        MergeRenderRanges(
                            member.PreviousInternalRanges,
                            member.HasPreviousEdge);
                    member.NextRange =
                        MergeRenderRanges(
                            member.NextInternalRanges,
                            member.HasNextEdge);
                }
                BuildGroupStates(block);
                if (block.Mode == SkyBlockMode.Normal)
                    BuildTimeline(block);
            }

            BuildOverlapPeers();
            MarkIndependentJudgeBlocks();
            BuildVisualAndJudgeLists();
        }

        private static void BuildGroupStates(SkyBlock block)
        {
            block.GroupStates.Clear();
            if (block.Members.Count == 0)
                return;

            // Coverage, warning, penalty, red and glow ownership belong to the
            // connected runtime group. Authored groups remain on each member
            // for source color and record identity only.
            var state = new SkyGroupState
            {
                Block = block,
            };
            for (int i = 0; i < block.Members.Count; i++)
            {
                SkyBlockMember member = block.Members[i];
                member.GroupState = state;
                state.Members.Add(member);
            }
            block.GroupStates.Add(state);
            ValidateGroupStateOwnership(block);
        }

        private static void ValidateGroupStateOwnership(
            SkyBlock block)
        {
            if (block.GroupStates.Count != 1)
                throw new InvalidOperationException(
                    "SkyBlock runtime-group state count is invalid.");
            SkyGroupState state = block.GroupStates[0];
            if (state == null || state.Block != block)
                throw new InvalidOperationException(
                    "SkyBlock runtime-group state owner is invalid.");
            for (int i = 0; i < block.Members.Count; i++)
            {
                SkyBlockMember member = block.Members[i];
                if (member.GroupState != state)
                    throw new InvalidOperationException(
                        $"SkyBlock member {member.Id} has invalid runtime-group state.");
            }
            if (state.Members.Count != block.Members.Count)
                throw new InvalidOperationException(
                    "SkyBlock runtime-group member list is inconsistent.");
        }

        private void BuildOverlapPeers()
        {
            if (Blocks.Count < 2) return;

            var ordered = new int[Blocks.Count];
            for (int i = 0; i < ordered.Length; i++)
                ordered[i] = i;
            Array.Sort(ordered, (a, b) =>
            {
                SkyBlock blockA = Blocks[a];
                SkyBlock blockB = Blocks[b];
                int c = blockA.StartMs.CompareTo(blockB.StartMs);
                if (c != 0) return c;
                c = blockA.EndMs.CompareTo(blockB.EndMs);
                return c != 0
                    ? c
                    : blockA.RuntimeGroup.CompareTo(
                        blockB.RuntimeGroup);
            });

            var active = new SortedSet<int>();
            var endings = new PriorityQueue<int, uint>();
            for (int oi = 0; oi < ordered.Length; oi++)
            {
                int currentIndex = ordered[oi];
                SkyBlock current = Blocks[currentIndex];
                while (endings.TryPeek(
                           out int endedIndex,
                           out uint endedAt) &&
                       endedAt <= current.StartMs)
                {
                    endings.Dequeue();
                    active.Remove(endedIndex);
                }

                if (current.Mode == SkyBlockMode.Normal)
                {
                    foreach (int peerIndex in active)
                    {
                        SkyBlock peer = Blocks[peerIndex];
                        if (peer.Mode != SkyBlockMode.Normal)
                            continue;
                        current.OverlapPeers.Add(peer);
                        peer.OverlapPeers.Add(current);
                    }
                }

                active.Add(currentIndex);
                endings.Enqueue(currentIndex, current.EndMs);
            }
        }

        private void MarkIndependentJudgeBlocks()
        {
            if (!SkyBlockPolicy.EnableSharedBranchJudgement)
                return;
            for (int bi = 0; bi < Blocks.Count; bi++)
            {
                SkyBlock block = Blocks[bi];
                if (block.Mode != SkyBlockMode.Normal) continue;
                if (block.SharedLayers.Count > 0)
                    block.RequiresIndependentJudge = true;
                for (int mi = 0; mi < block.Members.Count; mi++)
                {
                    SkyBlockMember member = block.Members[mi];
                    SkyBlockMember previous =
                        member.AdjacentSameAuthoredPrevious;
                    if (previous == null ||
                        previous.Block == null ||
                        previous.Block == block)
                        continue;
                    block.RequiresIndependentJudge = true;
                    previous.Block.RequiresIndependentJudge = true;
                }
            }

            for (int a = 0; a < Blocks.Count; a++)
            {
                SkyBlock blockA = Blocks[a];
                if (blockA.Mode != SkyBlockMode.Normal) continue;
                for (int pi = 0; pi < blockA.OverlapPeers.Count; pi++)
                {
                    SkyBlock blockB = blockA.OverlapPeers[pi];
                    if (blockB.Mode != SkyBlockMode.Normal ||
                        blockA.RuntimeGroup >=
                        blockB.RuntimeGroup)
                        continue;
                    if (!BlocksOverlapAtTime(blockA, blockB))
                        continue;
                    blockA.RequiresIndependentJudge = true;
                    blockB.RequiresIndependentJudge = true;
                }
            }
        }

        private void BuildVisualAndJudgeLists()
        {
            for (int bi = 0; bi < Blocks.Count; bi++)
            {
                SkyBlock block = Blocks[bi];
                if (block.Mode != SkyBlockMode.Normal) continue;
                bool visual = block.RequiresIndependentJudge;
                long authoredGroup = block.Members.Count > 0
                    ? block.Members[0].AuthoredGroup
                    : 0;
                for (int mi = 0; mi < block.Members.Count; mi++)
                {
                    SkyBlockMember member = block.Members[mi];
                    if (member.AuthoredGroup != authoredGroup)
                        visual = true;
                    if (member.HasAdjacentSameAuthoredPrevious &&
                        member.AdjacentSameAuthoredPrevious.Block !=
                        block)
                        visual = true;
                }
                if (!visual) continue;
                block.RequiresIndependentJudge = true;

                for (int mi = 0; mi < block.Members.Count; mi++)
                {
                    SkyBlockMember member = block.Members[mi];
                    member.VisualIndex = VisualMembers.Count;
                    VisualMembers.Add(member);
                    VisualMemberIds.Add(member.Id);
                }

                for (int mi = 0; mi < block.Members.Count; mi++)
                {
                    SkyBlockMember member = block.Members[mi];
                    JudgeMembers.Add(member);
                    JudgeMemberIds.Add(member.Id);
                }
                for (int li = 0; li < block.TimelineLayers.Count; li++)
                    JudgeLayers.Add(block.TimelineLayers[li]);
            }

            JudgeMembers.Sort((a, b) =>
                a.Id.CompareTo(b.Id));
            VisualMembers.Sort((a, b) =>
            {
                int c = a.StartMs.CompareTo(b.StartMs);
                if (c != 0) return c;
                c = a.EndMs.CompareTo(b.EndMs);
                return c != 0 ? c : a.Id.CompareTo(b.Id);
            });
            for (int i = 0; i < VisualMembers.Count; i++)
                VisualMembers[i].VisualIndex = i;
            JudgeLayers.Sort((a, b) =>
            {
                int c = a.StartMs.CompareTo(b.StartMs);
                if (c != 0) return c;
                c = a.Block.RuntimeGroup.CompareTo(
                    b.Block.RuntimeGroup);
                return c != 0 ? c : a.EndMs.CompareTo(b.EndMs);
            });
        }

        private static bool BlocksOverlapAtTime(
            SkyBlock a, SkyBlock b)
        {
            for (int ai = 0; ai < a.Members.Count; ai++)
            {
                SkyBlockMember am = a.Members[ai];
                for (int bi = 0; bi < b.Members.Count; bi++)
                {
                    SkyBlockMember bm = b.Members[bi];
                    uint lo = Math.Max(am.StartMs, bm.StartMs);
                    uint hi = Math.Min(am.EndMs, bm.EndMs);
                    if (lo >= hi) continue;
                    if (MembersOverlapAcross(
                            am, bm, lo, hi))
                        return true;
                }
            }
            return false;
        }

        private static bool MembersOverlapAcross(
            SkyBlockMember a,
            SkyBlockMember b,
            double lo,
            double hi)
        {
            if (RangesOverlapAt(a, b, lo) ||
                RangesOverlapAt(a, b, hi))
                return true;
            return MembersOverlapInterval(a, b, lo, hi, 0);
        }

        private static bool MembersOverlapInterval(
            SkyBlockMember a,
            SkyBlockMember b,
            double lo,
            double hi,
            int depth)
        {
            double mid = (lo + hi) * 0.5;
            SkyBandRange ar = a.RangeAt(mid);
            SkyBandRange br = b.RangeAt(mid);
            if (ar.Overlaps(br, JoinSpaceTolerance))
                return true;

            double half = (hi - lo) * 0.5;
            double aLeft = BoundaryRate(a, true);
            double aRight = BoundaryRate(a, false);
            double bLeft = BoundaryRate(b, true);
            double bRight = BoundaryRate(b, false);
            double firstLower =
                (ar.Lo - br.Hi) -
                (aLeft + bRight) * half;
            double secondLower =
                (br.Lo - ar.Hi) -
                (bLeft + aRight) * half;
            if (firstLower > JoinSpaceTolerance ||
                secondLower > JoinSpaceTolerance)
                return false;

            if (hi - lo <= 0.01 || depth >= 40)
            {
                return ar.Overlaps(
                           br, JoinSpaceTolerance) ||
                       RangesOverlapAt(a, b, lo) ||
                       RangesOverlapAt(a, b, hi);
            }
            return MembersOverlapInterval(
                       a, b, lo, mid, depth + 1) ||
                   MembersOverlapInterval(
                       a, b, mid, hi, depth + 1);
        }

        private static bool RangesOverlapAt(
            SkyBlockMember a,
            SkyBlockMember b,
            double timeMs)
        {
            return a.RangeAt(timeMs).Overlaps(
                b.RangeAt(timeMs), JoinSpaceTolerance);
        }

        private static double BoundaryRate(
            SkyBlockMember member,
            bool left)
        {
            if (member.EndMs <= member.StartMs)
                return 0.0;
            double start = left
                ? member.StartCenter - member.StartWidth * 0.5
                : member.StartCenter + member.StartWidth * 0.5;
            double end = left
                ? member.EndCenter - member.EndWidth * 0.5
                : member.EndCenter + member.EndWidth * 0.5;
            int ease = left
                ? member.LeftEase
                : member.RightEase;
            double easeDerivative = 1.0;
            if (ease == 1 || ease == 2)
                easeDerivative = Math.PI * 0.5;
            return Math.Abs(end - start) /
                   (member.EndMs - member.StartMs) *
                   easeDerivative;
        }

        private void BuildTimeline(SkyBlock block)
        {
            var events =
                new List<(uint Time, int Start, SkyBlockMember Member)>(
                    block.Members.Count * 2);
            for (int i = 0; i < block.Members.Count; i++)
            {
                SkyBlockMember member = block.Members[i];
                if (member.EndMs <= member.StartMs) continue;
                events.Add((member.StartMs, 1, member));
                events.Add((member.EndMs, 0, member));
            }
            events.Sort((a, b) =>
            {
                int c = a.Time.CompareTo(b.Time);
                if (c != 0) return c;
                c = a.Start.CompareTo(b.Start);
                return c != 0 ? c : a.Member.Id.CompareTo(b.Member.Id);
            });

            var active = new List<SkyBlockMember>(4);
            var indices = new Dictionary<long, int>(4);
            int cursor = 0;
            while (cursor < events.Count)
            {
                uint time = events[cursor].Time;
                while (cursor < events.Count &&
                       events[cursor].Time == time)
                {
                    var ev = events[cursor++];
                    if (ev.Start != 0)
                    {
                        indices[ev.Member.Id] = active.Count;
                        active.Add(ev.Member);
                        continue;
                    }
                    if (!indices.TryGetValue(
                            ev.Member.Id, out int index))
                        continue;
                    int last = active.Count - 1;
                    SkyBlockMember moved = active[last];
                    active[index] = moved;
                    active.RemoveAt(last);
                    indices.Remove(ev.Member.Id);
                    if (index != last)
                        indices[moved.Id] = index;
                }
                if (cursor >= events.Count || active.Count == 0)
                    continue;
                uint next = events[cursor].Time;
                if (next <= time) continue;

                var layer = new SkyJudgmentLayer
                {
                    Block = block,
                    StartMs = time,
                    EndMs = next,
                };
                layer.Members.AddRange(active);
                layer.Members.Sort((a, b) =>
                    a.Id.CompareTo(b.Id));
                for (int i = 0; i < layer.Members.Count; i++)
                {
                    SkyGroupState state =
                        layer.Members[i].GroupState;
                    if (state != null &&
                        state.LastTimelineLayer != layer)
                    {
                        layer.GroupStates.Add(state);
                        state.LastTimelineLayer = layer;
                    }
                }
                block.TimelineLayers.Add(layer);
                if (layer.Members.Count > 1)
                {
                    block.SharedLayers.Add(layer);
                    SharedLayers.Add(layer);
                }
            }
        }

        private static void AddOrMergeRange(
            List<SkyBandRange> ranges, SkyBandRange added)
        {
            int insert = 0;
            while (insert < ranges.Count &&
                   ranges[insert].Lo < added.Lo)
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
                SkyBandRange previous = ranges[write - 1];
                SkyBandRange current = ranges[read];
                if (current.Lo <=
                    previous.Hi + JoinSpaceTolerance)
                {
                    ranges[write - 1] = new SkyBandRange(
                        previous.Lo,
                        Math.Max(previous.Hi, current.Hi));
                }
                else
                {
                    ranges[write++] = current;
                }
            }
            if (write < ranges.Count)
                ranges.RemoveRange(write, ranges.Count - write);
        }

        internal static SkyBandRange MergeRenderRanges(
            List<SkyBandRange> ranges,
            bool hasEdge)
        {
            if (!hasEdge || ranges.Count == 0)
                return new SkyBandRange(0f, 1f);
            float lo = ranges[0].Lo;
            float hi = ranges[0].Hi;
            for (int i = 1; i < ranges.Count; i++)
            {
                if (ranges[i].Lo < lo) lo = ranges[i].Lo;
                if (ranges[i].Hi > hi) hi = ranges[i].Hi;
            }
            return new SkyBandRange(lo, hi);
        }

        private static bool TryReadGeometry(
            Icp1Codec.Note note,
            out double startCenter,
            out double startWidth,
            out double endCenter,
            out double endWidth)
        {
            startCenter = 0.0;
            startWidth = 0.0;
            endCenter = 0.0;
            endWidth = 0.0;
            if (!note.TryGetScaledPair(
                    0, out int scn, out int scd) ||
                !note.TryGetScaledPair(
                    1, out int ecn, out int ecd) ||
                !note.TryGetScaledPair(
                    2, out int swn, out int swd) ||
                !note.TryGetScaledPair(
                    3, out int ewn, out int ewd))
                return false;

            startCenter = scn / (double)scd;
            endCenter = ecn / (double)ecd;
            startWidth = Math.Abs(swn / (double)swd);
            endWidth = Math.Abs(ewn / (double)ewd);
            return IsFinite(startCenter) &&
                   IsFinite(startWidth) &&
                   IsFinite(endCenter) &&
                   IsFinite(endWidth);
        }

        private static bool CanConnect(
            SkyBlockMember previous,
            SkyBlockMember current)
        {
            if (previous.StartMs >= current.StartMs) return false;
            if (Math.Abs(
                    (long)current.StartMs -
                    previous.EndMs) >
                JoinTimeToleranceMs)
                return false;
            if (previous.TimingGroup != current.TimingGroup)
                return false;
            if (!SkyBlockPolicy.MergeAcrossAuthoredGroups &&
                previous.AuthoredGroup != current.AuthoredGroup)
                return false;

            double previousLo =
                previous.EndCenter -
                previous.EndWidth * 0.5;
            double previousHi =
                previous.EndCenter +
                previous.EndWidth * 0.5;
            double currentLo =
                current.StartCenter -
                current.StartWidth * 0.5;
            double currentHi =
                current.StartCenter +
                current.StartWidth * 0.5;
            return Math.Min(previousHi, currentHi) +
                       JoinSpaceTolerance >=
                   Math.Max(previousLo, currentLo);
        }

        private static SkyBlockMode ModeOf(Icp1Codec.Note note)
        {
            // Visual-only notes are removed before SkyBlock planning.
            return SkyBlockMode.Normal;
        }

        private static int DecodeEase(
            uint bits, uint first, uint second, uint third)
        {
            if ((bits & third) == third) return 2;
            if ((bits & second) == second) return 1;
            return 0;
        }

        private static bool IsFinite(double value)
            => !double.IsNaN(value) &&
               !double.IsInfinity(value);

        private static int Find(int[] parent, int index)
        {
            while (parent[index] != index)
            {
                parent[index] = parent[parent[index]];
                index = parent[index];
            }
            return index;
        }
    }

    internal static class SkyBlockPolicy
    {
        internal static float OverlapHitPadding = 0.1f;
        internal static bool MergeAcrossAuthoredGroups = true;
        internal static bool EnableSharedBranchJudgement = true;
    }
}
