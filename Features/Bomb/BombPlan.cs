using System;
using System.Collections.Generic;

namespace InFalsusChartExt
{
    internal enum BombMode
    {
        Normal,
        NoInput,
        NoClip,
    }

    /// <summary>
    /// Immutable chart-time description of one bomb. NativeId is assigned only
    /// after noinput/noclip ingress, while SourceId remains the stable authored
    /// position used during the transform.
    /// </summary>
    internal sealed class BombDescriptor
    {
        internal long SourceId;
        internal long NativeId = -1;
        internal long VisualId;
        internal int StartMs;
        internal int EndMs;
        internal int Lane;
        internal int Width;
        internal uint Side;
        internal int NativeSide = -1;
        internal int NativePosition = -1;
        internal int TimingGroup;
        internal BombMode Mode;
        internal long AuthoredGroup;
        internal double StartCenter;
        internal double StartWidth;
        internal double EndCenter;
        internal double EndWidth;
        internal int VisualIndex = -1;

        internal bool IsNative => NativeId >= 0;
        internal bool IsVisualOnly => Mode != BombMode.Normal;
    }

    /// <summary>
    /// Per-chart bomb catalog. The plan is built once during chart ingress and
    /// is never rebuilt from live native slots during rendering or judgement.
    /// </summary>
    internal sealed class BombPlan
    {
        internal readonly List<BombDescriptor> Descriptors =
            new List<BombDescriptor>();
        internal readonly List<BombDescriptor> NativeMembers =
            new List<BombDescriptor>();
        internal readonly List<BombDescriptor> VisualMembers =
            new List<BombDescriptor>();
        internal readonly List<BombDescriptor> Due =
            new List<BombDescriptor>();

        private readonly Dictionary<long, BombDescriptor> _bySourceId =
            new Dictionary<long, BombDescriptor>();
        private readonly Dictionary<long, BombDescriptor> _byNativeId =
            new Dictionary<long, BombDescriptor>();
        private readonly Dictionary<long, BombDescriptor> _byVisualId =
            new Dictionary<long, BombDescriptor>();

        internal int Count => Descriptors.Count;

        internal bool IsBomb(long nativeId)
            => _byNativeId.ContainsKey(nativeId);

        internal bool TryGetByNativeId(long nativeId, out BombDescriptor descriptor)
            => _byNativeId.TryGetValue(nativeId, out descriptor);

        internal static BombPlan TryBuild(
            IReadOnlyList<Icp1Codec.Note> notes,
            IReadOnlyDictionary<long, int> timingGroups,
            IReadOnlyCollection<int> noInputTimingGroups,
            IReadOnlyCollection<int> noClipTimingGroups,
            IReadOnlyCollection<long> bombSourceIds,
            IReadOnlyCollection<long> legacyNoInputIds)
        {
            if (notes == null || bombSourceIds == null ||
                bombSourceIds.Count == 0)
                return null;

            var plan = new BombPlan();
            var bombIds = new HashSet<long>(bombSourceIds);
            var legacyNoInput = legacyNoInputIds == null
                ? new HashSet<long>()
                : new HashSet<long>(legacyNoInputIds);

            foreach (long sourceId in bombIds)
            {
                if (sourceId < 0 || sourceId >= notes.Count)
                    return null;
                Icp1Codec.Note note = notes[(int)sourceId];
                if (note == null || note.Type != 1)
                    return null;

                int timingGroup = 0;
                timingGroups?.TryGetValue(sourceId, out timingGroup);
                BombMode mode = BombMode.Normal;
                if (Contains(noClipTimingGroups, timingGroup))
                    mode = BombMode.NoClip;
                else if (Contains(noInputTimingGroups, timingGroup) ||
                         legacyNoInput.Contains(sourceId))
                    mode = BombMode.NoInput;

                if (!TryReadGeometry(
                        note,
                        out double startCenter,
                        out double startWidth,
                        out double endCenter,
                        out double endWidth))
                    return null;

                var descriptor = new BombDescriptor
                {
                    SourceId = sourceId,
                    StartMs = checked((int)note.StartMs),
                    EndMs = checked((int)note.EndMs),
                    Lane = RoundNormalized(startCenter),
                    Width = Math.Max(1, RoundNormalized(startWidth)),
                    Side = note.Side,
                    TimingGroup = timingGroup,
                    Mode = mode,
                    AuthoredGroup = note.Group,
                    StartCenter = startCenter,
                    StartWidth = startWidth,
                    EndCenter = endCenter,
                    EndWidth = endWidth,
                };
                plan.Descriptors.Add(descriptor);
                plan._bySourceId.Add(sourceId, descriptor);
            }

            plan.Descriptors.Sort((a, b) =>
            {
                int c = a.StartMs.CompareTo(b.StartMs);
                return c != 0
                    ? c
                    : a.SourceId.CompareTo(b.SourceId);
            });
            return plan;
        }

        /// <summary>
        /// Rebinds descriptors to the final native ids and visual ids after the
        /// ingress partition has removed noinput/noclip notes.
        /// </summary>
        internal bool BindAfterPartition(
            IReadOnlyList<long> oldIdByNew,
            IReadOnlyList<VisualNoteDescriptor> visualNotes)
        {
            if (oldIdByNew == null)
                return false;

            NativeMembers.Clear();
            VisualMembers.Clear();
            Due.Clear();
            _byNativeId.Clear();
            _byVisualId.Clear();

            for (int i = 0; i < oldIdByNew.Count; i++)
            {
                if (!_bySourceId.TryGetValue(oldIdByNew[i], out BombDescriptor descriptor))
                    continue;
                if (descriptor.IsVisualOnly || descriptor.NativeId >= 0)
                    return false;
                descriptor.NativeId = i;
                if (!_byNativeId.TryAdd(i, descriptor))
                    return false;
                NativeMembers.Add(descriptor);
            }

            if (visualNotes != null)
            {
                for (int i = 0; i < visualNotes.Count; i++)
                {
                    VisualNoteDescriptor visual = visualNotes[i];
                    if (visual == null ||
                        visual.Kind != VisualNoteKind.Bomb ||
                        !_bySourceId.TryGetValue(
                            visual.SourceId, out BombDescriptor descriptor))
                        continue;
                    if (descriptor.Mode == BombMode.Normal ||
                        descriptor.VisualId != 0 ||
                        descriptor.VisualIndex >= 0)
                        return false;
                    descriptor.VisualId = visual.VisualId;
                    descriptor.VisualIndex = i;
                    if (!_byVisualId.TryAdd(visual.VisualId, descriptor))
                        return false;
                    VisualMembers.Add(descriptor);
                }
            }

            for (int i = 0; i < Descriptors.Count; i++)
            {
                BombDescriptor descriptor = Descriptors[i];
                if (descriptor.IsVisualOnly)
                {
                    if (descriptor.VisualId == 0 ||
                        descriptor.NativeId >= 0)
                        return false;
                }
                else
                {
                    if (descriptor.NativeId < 0)
                        return false;
                    Due.Add(descriptor);
                }
            }
            Due.Sort((a, b) =>
            {
                int c = a.StartMs.CompareTo(b.StartMs);
                return c != 0
                    ? c
                    : a.NativeId.CompareTo(b.NativeId);
            });
            return Validate(out _);
        }

        internal bool Validate(out string error)
        {
            var sourceIds = new HashSet<long>();
            var nativeIds = new HashSet<long>();
            var visualIds = new HashSet<long>();
            for (int i = 0; i < Descriptors.Count; i++)
            {
                BombDescriptor descriptor = Descriptors[i];
                if (descriptor == null ||
                    descriptor.Lane < 0 ||
                    descriptor.Lane > 5 ||
                    descriptor.Width < 1 ||
                    descriptor.StartMs < 0 ||
                    descriptor.EndMs < descriptor.StartMs ||
                    !IsFinite(descriptor.StartCenter) ||
                    !IsFinite(descriptor.StartWidth) ||
                    !IsFinite(descriptor.EndCenter) ||
                    !IsFinite(descriptor.EndWidth))
                {
                    error = $"invalid bomb descriptor at index {i}";
                    return false;
                }
                if (!sourceIds.Add(descriptor.SourceId))
                {
                    error = $"duplicate bomb source id {descriptor.SourceId}";
                    return false;
                }
                if (descriptor.NativeId >= 0 &&
                    !nativeIds.Add(descriptor.NativeId))
                {
                    error = $"duplicate bomb native id {descriptor.NativeId}";
                    return false;
                }
                if (descriptor.VisualId != 0 &&
                    !visualIds.Add(descriptor.VisualId))
                {
                    error = $"duplicate bomb visual id {descriptor.VisualId}";
                    return false;
                }
            }
            error = null;
            return true;
        }

        private static bool TryReadGeometry(
            Icp1Codec.Note note,
            out double startCenter,
            out double startWidth,
            out double endCenter,
            out double endWidth)
        {
            startCenter = startWidth = endCenter = endWidth = 0.0;
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

        private static int RoundNormalized(double value)
            => (int)Math.Round(
                value * 4.0,
                MidpointRounding.AwayFromZero);

        private static bool Contains(
            IReadOnlyCollection<int> values,
            int value)
        {
            if (values == null || value <= 0)
                return false;
            if (values is HashSet<int> set)
                return set.Contains(value);
            foreach (int candidate in values)
                if (candidate == value)
                    return true;
            return false;
        }

        private static bool IsFinite(double value)
            => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
