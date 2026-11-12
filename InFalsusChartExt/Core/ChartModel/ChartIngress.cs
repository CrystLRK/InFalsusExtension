// Code organization: chart ingress model. Runtime behavior is unchanged.
using System;
using System.Collections.Generic;

namespace InFalsusChartExt
{
    internal sealed class ChartIngressResult
    {
        internal string ChartKey;
        internal string Source;
        internal int Generation;
        internal bool IsExtension;
        internal NativeChart NativeChart;
        internal VisualChart VisualChart;
        internal ChartRuntimeRules Rules;
        internal SkyBlockPlan SkyBlocks;
        internal BombPlan Bombs;

        internal void Dispose()
        {
            NativeChart?.Clear();
            VisualChart?.Clear();
            Rules?.Clear();
            SkyBlocks?.Clear();
            Bombs = null;
        }
    }

    internal sealed class ChartIngressPartition
    {
        internal readonly List<Icp1Codec.Note> NativeNotes;
        internal readonly List<VisualNoteDescriptor> VisualNotes;
        internal readonly List<long> OldIdByNew;

        internal ChartIngressPartition(int capacity)
        {
            NativeNotes = new List<Icp1Codec.Note>(capacity);
            VisualNotes = new List<VisualNoteDescriptor>();
            OldIdByNew = new List<long>(capacity);
        }

        internal int RemovedCount => VisualNotes.Count;
    }

    /// <summary>
    /// Transitional ingress boundary. It snapshots the decoded/native payload and
    /// classifies descriptors that will become VisualNotes in the next step.
    /// It does not mutate the chart or claim ownership of native notes.
    /// </summary>
    internal static class ChartIngress
    {
        internal static ChartIngressResult NativeOnly(
            string chartKey,
            string source,
            IReadOnlyList<Icp1Codec.Note> nativeNotes,
            IReadOnlyList<Icp1Codec.Event> nativeEvents,
            int nativeType2Events,
            bool applySkyBlocks = false)
        {
            var result = NewResult(
                chartKey, source, ChartExtState.ChartGeneration, false);
            SnapshotNative(
                result.NativeChart, nativeNotes, nativeEvents,
                nativeType2Events);
            if (applySkyBlocks)
            {
                result.SkyBlocks = SkyBlockPlan.TryBuild(
                    nativeNotes, result.Generation);
            }
            return result;
        }

        internal static ChartIngressResult FromCurrentState(
            string chartKey,
            string source,
            IReadOnlyList<Icp1Codec.Note> nativeNotes,
            IReadOnlyList<Icp1Codec.Event> nativeEvents,
            int nativeType2Events,
            BombPlan bombs = null)
        {
            var result = NewResult(
                chartKey, source, ChartExtState.ChartGeneration, true);
            SnapshotNative(
                result.NativeChart, nativeNotes, nativeEvents,
                nativeType2Events);
            result.NativeChart.BombCount = ChartExtState.BombIds.Count;
            SnapshotRules(result.Rules);

            // The plan is runtime-only. Source ids, groups, times, flags,
            // geometry and setcolor mappings stay untouched.
            result.SkyBlocks = SkyBlockPlan.TryBuild(
                nativeNotes, result.Generation);
            result.Bombs = bombs;
            return result;
        }

        /// <summary>
        /// Splits authored notes into the exact native payload and the visual-only
        /// descriptors consumed by VisualNoteRuntime.
        /// </summary>
        internal static ChartIngressPartition PartitionVisualNotes(
            IReadOnlyList<Icp1Codec.Note> notes,
            IReadOnlyCollection<long> legacyNoInputIds,
            IReadOnlyDictionary<long, int> timingGroups,
            IReadOnlyCollection<int> noInputTimingGroups,
            IReadOnlyCollection<int> noClipTimingGroups,
            IReadOnlyCollection<long> bombIds)
        {
            var legacy =
                new HashSet<long>(legacyNoInputIds ?? Array.Empty<long>());
            var bombs =
                new HashSet<long>(bombIds ?? Array.Empty<long>());
            var partition = new ChartIngressPartition(
                notes?.Count ?? 0);
            if (notes == null)
                return partition;

            for (int i = 0; i < notes.Count; i++)
            {
                Icp1Codec.Note note = notes[i];
                if (note == null)
                    continue;
                bool isLegacy = legacy.Contains(i);
                int timingGroup = 0;
                bool isTimingNoInput = false;
                bool isNoClip = false;
                if (timingGroups != null &&
                    timingGroups.TryGetValue(i, out timingGroup))
                {
                    isTimingNoInput =
                        Contains(noInputTimingGroups, timingGroup) ||
                        Contains(noClipTimingGroups, timingGroup);
                    isNoClip =
                        Contains(noClipTimingGroups, timingGroup);
                }
                if (!isLegacy && !isTimingNoInput)
                {
                    partition.OldIdByNew.Add(i);
                    partition.NativeNotes.Add(note);
                    continue;
                }

                VisualNoteKind kind;
                switch (note.Type)
                {
                    case 1:
                        kind = bombs.Contains(i)
                            ? VisualNoteKind.Bomb
                            : VisualNoteKind.Tap;
                        break;
                    case 2:
                        kind = VisualNoteKind.Hold;
                        break;
                    case 4:
                        kind = VisualNoteKind.Flick;
                        break;
                    case 5:
                        kind = VisualNoteKind.SkyArea;
                        break;
                    default:
                        partition.OldIdByNew.Add(i);
                        partition.NativeNotes.Add(note);
                        continue;
                }

                VisualNoteDescriptor descriptor =
                    VisualNoteDescriptor.FromNative(
                        note,
                        kind,
                        isNoClip
                            ? VisualNoteLifetime.NoClip
                            : VisualNoteLifetime.NoInput,
                        timingGroup);
                descriptor.VisualId =
                    (1L << 62) |
                    (descriptor.SourceId &
                     0x0FFFFFFFFFFFFFFFL);
                partition.VisualNotes.Add(descriptor);
            }
            return partition;
        }

        private static bool Contains(
            IReadOnlyCollection<int> values,
            int value)
        {
            if (values is HashSet<int> set)
                return set.Contains(value);
            foreach (int item in values)
                if (item == value)
                    return true;
            return false;
        }

        private static ChartIngressResult NewResult(
            string chartKey,
            string source,
            int generation,
            bool isExtension)
        {
            return new ChartIngressResult
            {
                ChartKey = chartKey,
                Source = source,
                Generation = generation,
                IsExtension = isExtension,
                NativeChart = new NativeChart(),
                VisualChart = new VisualChart(),
                Rules = new ChartRuntimeRules(),
            };
        }

        private static void SnapshotNative(
            NativeChart target,
            IReadOnlyList<Icp1Codec.Note> notes,
            IReadOnlyList<Icp1Codec.Event> events,
            int nativeType2Events)
        {
            for (int i = 0; i < notes.Count; i++)
                target.Add(notes[i]);
            target.EventCount = events?.Count ?? 0;
            target.NativeType2Events = nativeType2Events;
        }

        private static void SnapshotRules(ChartRuntimeRules target)
        {
            foreach (KeyValuePair<long, int> item
                     in ChartExtState.NoteTimingGroup)
                target.NoteTimingGroups[item.Key] = item.Value;
            foreach (int group in ChartExtState.NoInputTimingGroups)
                target.NoInputTimingGroups.Add(group);
            foreach (int group in ChartExtState.NoClipTimingGroups)
                target.NoClipTimingGroups.Add(group);
            target.TrackMoveCount = ChartExtState.TrackMoves.Count;
            target.ShowTextCount = ChartExtState.ShowTextEntries.Count;
            target.SceneControlCount = ChartExtState.SceneControls.Count;
            target.SkyColorCount = ChartExtState.SkyColorEvents.Count;
            target.AudioOffsetMs =
                ChartExtState.HasAudioOffset ? ChartExtState.AudioOffsetMs : 0;
        }

    }
}
