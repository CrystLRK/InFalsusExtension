// Code organization: chart runtime aggregate models. Runtime behavior is unchanged.
using System.Collections.Generic;

namespace InFalsusChartExt
{
    internal sealed class VisualChart
    {
        internal readonly List<VisualNoteDescriptor> Notes =
            new List<VisualNoteDescriptor>();
        internal readonly HashSet<long> Ids = new HashSet<long>();

        internal int Count => Notes.Count;

        internal void Add(VisualNoteDescriptor descriptor)
        {
            if (descriptor == null || !Ids.Add(descriptor.VisualId))
                return;
            Notes.Add(descriptor);
        }

        internal void Clear()
        {
            Notes.Clear();
            Ids.Clear();
        }
    }

    /// <summary>
    /// Transitional native snapshot. It records the exact native note set after
    /// noinput/noclip ingress has been removed from the payload.
    /// </summary>
    internal sealed class NativeChart
    {
        internal readonly List<long> NoteIds = new List<long>();
        internal readonly HashSet<long> NoteIdSet = new HashSet<long>();
        internal int TapCount;
        internal int HoldCount;
        internal int FlickCount;
        internal int BombCount;
        internal int SkyAreaCount;
        internal int EventCount;
        internal int NativeType2Events;

        internal int Count => NoteIds.Count;

        internal void Add(Icp1Codec.Note note)
        {
            if (note == null || !NoteIdSet.Add(note.Index)) return;
            NoteIds.Add(note.Index);
            switch (note.Type)
            {
                case 1: TapCount++; break;
                case 2: HoldCount++; break;
                case 4: FlickCount++; break;
                case 5: SkyAreaCount++; break;
            }
        }

        internal void Clear()
        {
            NoteIds.Clear();
            NoteIdSet.Clear();
            TapCount = HoldCount = FlickCount = BombCount =
                SkyAreaCount = EventCount = NativeType2Events = 0;
        }
    }

    internal sealed class ChartRuntimeRules
    {
        internal readonly Dictionary<long, int> NoteTimingGroups =
            new Dictionary<long, int>();
        internal readonly HashSet<int> NoInputTimingGroups =
            new HashSet<int>();
        internal readonly HashSet<int> NoClipTimingGroups =
            new HashSet<int>();
        internal int TrackMoveCount;
        internal int ShowTextCount;
        internal int SceneControlCount;
        internal int SkyColorCount;
        internal int AudioOffsetMs;

        internal void Clear()
        {
            NoteTimingGroups.Clear();
            NoInputTimingGroups.Clear();
            NoClipTimingGroups.Clear();
            TrackMoveCount = ShowTextCount = SceneControlCount =
                SkyColorCount = AudioOffsetMs = 0;
        }
    }
}
