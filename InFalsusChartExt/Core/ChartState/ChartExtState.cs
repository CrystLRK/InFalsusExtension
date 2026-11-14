using System;
using System.Collections.Generic;

namespace InFalsusChartExt
{
    /// <summary>Shared, last-parsed chart state used by the runtime patches.</summary>
    internal static class ChartExtState
    {
        // note flag bits (the game never uses these on tap/sky notes)
        internal const int BombFlag = 0x8000;     // tap note: the bomb
        internal const int NoInputFlag = 0x8000;  // sky area note: no judgement / no score
        internal const int TrackMoveMagic = 0x54524B; // stored in flags bits 8..31 of a type-3 note
        internal const int ShowTextMagic = 0x545854; // "TXT": showtext container note magic (bits 8..31)
        internal const int AudioOffsetMagic = 0x414F46;   // "AOF": chart audio-offset metadata
        internal const int SceneControlMagic = 0x53434E;  // "SCN": scenecontrol metadata
        internal const int SetColorMagic = 0x434F4C;      // "COL": sky-area color metadata
        internal const uint TimingGroupNoInputBit = 0x80000000;
        internal const uint TimingGroupNoClipBit = 0x40000000;
        internal const uint TimingGroupIdMask = 0x1FFFFFFF;
        internal const int SceneTrackDisplay = 1;
        internal const int SceneHideGroup = 2;

        // judgement enum values (Il2Cpp_K._FH)
        internal const byte JudgeNone = 0;
        internal const byte JudgeMiss = 1;
        internal const byte JudgeNearEarly = 2;
        internal const byte JudgeNearLate = 3;
        internal const byte JudgePerfectEarly = 4;
        internal const byte JudgePerfectLate = 5;
        internal const byte JudgeShiny = 6;

        internal sealed class TrackMoveEntry
        {
            public double TimeMs;
            public float X, Y, Z;
            public float AngXoz, AngYoz, AngXoy;
            public int Ease;
            public float DurationMs;
            public int SourceOrder;
        }

        /// <summary>Per-chart state, replaced on every parse of a chart that carries markers.</summary>
        internal static readonly List<TrackMoveEntry> TrackMoves = new List<TrackMoveEntry>();

        /// <summary>Note ids carrying the extended statements, latched at transform time so
        /// the runtime patches never have to trust struct-copy reads.</summary>
        internal static readonly HashSet<long> BombIds = new HashSet<long>();

        internal sealed class SideSlot
        {
            public int Side;
            public int Position;
            public long Id;
            public int StartMs;
            public int EndMs;
        }

        /// <summary>全部 skyarea 槽位（生命周期探针用：消费位/生成列表/池对象激活状态）。</summary>
        internal static readonly List<SideSlot> SkyAreaSlots = new List<SideSlot>();

        /// <summary>timinggroup：需要按组时间线接管视觉的音符槽位。</summary>
        internal sealed class GroupSlot
        {
            public int Side;
            public int Position;
            public long Id;
            public int Group;
            public int Type;
            public IntPtr CompiledOwner;
            public int CompileGeneration;
        }
        internal static readonly List<GroupSlot> GroupedSlots = new List<GroupSlot>();

        /// <summary>showtext：语句锁存的 (时间, 文本 id)；文本内容/样式在谱面同级
        /// <c>&lt;谱名&gt;.text.json</c>，运行期按共享时钟显示。</summary>
        internal sealed class ShowTextEntry
        {
            public double TimeMs;
            public long Id;
            public int Size;
            public int SourceOrder;
        }
        internal static readonly List<ShowTextEntry> ShowTextEntries = new List<ShowTextEntry>();

        /// <summary>audiooffset(timems): the first statement in a chart is accepted; later
        /// duplicates are ignored. The value moves the gameplay music channel only.</summary>
        internal static bool HasAudioOffset;
        internal static int AudioOffsetMs;

        /// <summary>scenecontrol(timems,mode,*param), sorted by time after extraction.</summary>
        internal sealed class SceneControlEntry
        {
            public int TimeMs;
            public int Mode;
            public int GroupId;
            public int DurationMs;
            public int Value;
            public int SourceOrder;
        }
        internal static readonly List<SceneControlEntry> SceneControls =
            new List<SceneControlEntry>();
        internal static readonly HashSet<int> HideGroupControlledTimingGroups =
            new HashSet<int>();

        /// <summary>setcolor(timems,skyGroupId,#RRGGBBAA). Rgba is packed as RRGGBBAA.</summary>
        internal sealed class SkyColorEntry
        {
            public int TimeMs;
            public long SkyGroupId;
            public uint Rgba;
            public bool EdgeGlow;
            public int SourceOrder;
        }
        internal static readonly List<SkyColorEntry> SkyColorEvents =
            new List<SkyColorEntry>();

        /// <summary>Compiled note id -> engine sky-area group, used by setcolor to find
        /// every currently spawned renderer belonging to the requested group.</summary>
        internal static readonly Dictionary<long, long> SkyAreaGroupById =
            new Dictionary<long, long>();

        /// <summary>Timing groups whose notes are visual-only.</summary>
        internal static readonly HashSet<int> NoInputTimingGroups = new HashSet<int>();
        internal static readonly HashSet<int> NoClipTimingGroups = new HashSet<int>();

        /// <summary>timinggroup：每个音符的计时组（note.group_id 高 32 位，转谱期剥离）。</summary>
        internal static readonly Dictionary<long, int> NoteTimingGroup = new Dictionary<long, int>();

        internal static int TimingGroupOf(long id)
        {
            return NoteTimingGroup.TryGetValue(id, out int group) ? group : 0;
        }

        internal sealed class GroupSpeed
        {
            public double TimeMs;
            public int Group;
            public double Mult;
            public int SourceOrder;
        }
        internal static readonly List<GroupSpeed> GroupSpeeds = new List<GroupSpeed>();

        /// <summary>autoplay：所有可判定音符（noinput/bomb 除外）的 (起始时间, id, 槽位)。
        /// 编译期锁存、按时间排序；运行期用它做窗口内的记录判定改写。</summary>
        internal sealed class AutoplayDue
        {
            public int StartMs;
            public int EndMs;
            public int Type;
            public long Id;
            public int Side;
            public int Position;
            public int SourceOrder;
        }
        internal static readonly List<AutoplayDue> AutoplayDues = new List<AutoplayDue>();

        /// <summary>谱面里的速度事件（v1 type 0 事件，`track(time, mult)`）：
        /// 倍率以 double 存在 Value1；运行期用它把炸弹位置做与游戏一致的"流速时间积分"。</summary>
        internal sealed class SpeedEvent
        {
            public double TimeMs;
            public double Mult;
            public int SourceOrder;
        }

        internal static readonly List<SpeedEvent> SpeedEvents = new List<SpeedEvent>();

        /// <summary>本帧 spawn 列表里出现的 (side&lt;&lt;32 | pos)：游戏本帧真正会渲染的音符集合。
        /// 每个 spawn 相 `_hb` 后由 BombSpawnWatch 重建；bomb 自绘只画这里面的音符。</summary>
        internal static readonly HashSet<long> SpawnKeysNow = new HashSet<long>();

        internal static string LastChartName;
        /// <summary>当前解析状态是否属于带扩展语句的谱面。官谱编译前据此
        /// 清除上一张扩展谱的标记，避免首进官谱时误隐藏或剔除音符。</summary>
        internal static bool LastChartIsExtension;
        internal static int ChartGeneration;
        internal static int CompileGeneration;
        internal static int ExpectedSkyAreaCount;
        private static readonly Stack<TransformSnapshot>
            TransformTransactions = new Stack<TransformSnapshot>();

        internal sealed class TransformSnapshot
        {
            internal readonly List<TrackMoveEntry> TrackMoves =
                new List<TrackMoveEntry>();
            internal readonly HashSet<long> BombIds = new HashSet<long>();
            internal readonly List<SideSlot> SkyAreaSlots =
                new List<SideSlot>();
            internal readonly List<GroupSlot> GroupedSlots =
                new List<GroupSlot>();
            internal readonly List<ShowTextEntry> ShowTextEntries =
                new List<ShowTextEntry>();
            internal bool HasAudioOffset;
            internal int AudioOffsetMs;
            internal readonly List<SceneControlEntry> SceneControls =
                new List<SceneControlEntry>();
            internal readonly HashSet<int> HideGroupControlledTimingGroups =
                new HashSet<int>();
            internal readonly List<SkyColorEntry> SkyColorEvents =
                new List<SkyColorEntry>();
            internal readonly Dictionary<long, long> SkyAreaGroupById =
                new Dictionary<long, long>();
            internal readonly HashSet<int> NoInputTimingGroups =
                new HashSet<int>();
            internal readonly HashSet<int> NoClipTimingGroups =
                new HashSet<int>();
            internal readonly Dictionary<long, int> NoteTimingGroup =
                new Dictionary<long, int>();
            internal readonly List<GroupSpeed> GroupSpeeds =
                new List<GroupSpeed>();
            internal readonly List<AutoplayDue> AutoplayDues =
                new List<AutoplayDue>();
            internal readonly List<SpeedEvent> SpeedEvents =
                new List<SpeedEvent>();
            internal readonly HashSet<long> SpawnKeysNow =
                new HashSet<long>();
            internal string LastChartName;
            internal bool LastChartIsExtension;
            internal int ChartGeneration;
            internal int CompileGeneration;
            internal int ExpectedSkyAreaCount;
            internal bool Committed;
        }

        internal static TransformSnapshot BeginTransform()
        {
            TransformSnapshot snapshot = CaptureTransformSnapshot();
            TransformTransactions.Push(snapshot);
            ChartGeneration++;
            ChartRuntimeLifecycle.MarkChartActive();
            ClearChartState();
            return snapshot;
        }

        internal static void CommitTransform(TransformSnapshot snapshot)
        {
            if (snapshot == null || snapshot.Committed)
                return;
            snapshot.Committed = true;
            PopTransform(snapshot);
        }

        internal static void RollbackTransform(TransformSnapshot snapshot)
        {
            if (snapshot == null || snapshot.Committed)
                return;
            RestoreTransformSnapshot(snapshot);
            snapshot.Committed = true;
            PopTransform(snapshot);
        }

        private static void PopTransform(TransformSnapshot snapshot)
        {
            if (TransformTransactions.Count > 0 &&
                ReferenceEquals(TransformTransactions.Peek(), snapshot))
            {
                TransformTransactions.Pop();
                return;
            }
            if (TransformTransactions.Contains(snapshot))
            {
                var keep = new Stack<TransformSnapshot>();
                while (TransformTransactions.Count > 0)
                {
                    TransformSnapshot current =
                        TransformTransactions.Pop();
                    if (ReferenceEquals(current, snapshot))
                        break;
                    keep.Push(current);
                }
                while (keep.Count > 0)
                    TransformTransactions.Push(keep.Pop());
            }
        }

        private static TransformSnapshot CaptureTransformSnapshot()
        {
            var snapshot = new TransformSnapshot
            {
                HasAudioOffset = HasAudioOffset,
                AudioOffsetMs = AudioOffsetMs,
                LastChartName = LastChartName,
                LastChartIsExtension = LastChartIsExtension,
                ChartGeneration = ChartGeneration,
                CompileGeneration = CompileGeneration,
                ExpectedSkyAreaCount = ExpectedSkyAreaCount,
            };
            CopyTrackMoves(TrackMoves, snapshot.TrackMoves);
            snapshot.BombIds.UnionWith(BombIds);
            CopySideSlots(SkyAreaSlots, snapshot.SkyAreaSlots);
            CopyGroupSlots(GroupedSlots, snapshot.GroupedSlots);
            CopyShowTextEntries(ShowTextEntries, snapshot.ShowTextEntries);
            CopySceneControls(SceneControls, snapshot.SceneControls);
            snapshot.HideGroupControlledTimingGroups.UnionWith(
                HideGroupControlledTimingGroups);
            CopySkyColorEntries(
                SkyColorEvents, snapshot.SkyColorEvents);
            foreach (var pair in SkyAreaGroupById)
                snapshot.SkyAreaGroupById[pair.Key] = pair.Value;
            snapshot.NoInputTimingGroups.UnionWith(
                NoInputTimingGroups);
            snapshot.NoClipTimingGroups.UnionWith(
                NoClipTimingGroups);
            foreach (var pair in NoteTimingGroup)
                snapshot.NoteTimingGroup[pair.Key] = pair.Value;
            CopyGroupSpeeds(GroupSpeeds, snapshot.GroupSpeeds);
            CopyAutoplayDues(AutoplayDues, snapshot.AutoplayDues);
            CopySpeedEvents(SpeedEvents, snapshot.SpeedEvents);
            snapshot.SpawnKeysNow.UnionWith(SpawnKeysNow);
            return snapshot;
        }

        private static void RestoreTransformSnapshot(
            TransformSnapshot snapshot)
        {
            TrackMoves.Clear();
            CopyTrackMoves(snapshot.TrackMoves, TrackMoves);
            BombIds.Clear();
            BombIds.UnionWith(snapshot.BombIds);
            SkyAreaSlots.Clear();
            CopySideSlots(snapshot.SkyAreaSlots, SkyAreaSlots);
            GroupedSlots.Clear();
            CopyGroupSlots(snapshot.GroupedSlots, GroupedSlots);
            ShowTextEntries.Clear();
            CopyShowTextEntries(
                snapshot.ShowTextEntries, ShowTextEntries);
            HasAudioOffset = snapshot.HasAudioOffset;
            AudioOffsetMs = snapshot.AudioOffsetMs;
            SceneControls.Clear();
            CopySceneControls(
                snapshot.SceneControls, SceneControls);
            HideGroupControlledTimingGroups.Clear();
            HideGroupControlledTimingGroups.UnionWith(
                snapshot.HideGroupControlledTimingGroups);
            SkyColorEvents.Clear();
            CopySkyColorEntries(
                snapshot.SkyColorEvents, SkyColorEvents);
            SkyAreaGroupById.Clear();
            foreach (var pair in snapshot.SkyAreaGroupById)
                SkyAreaGroupById[pair.Key] = pair.Value;
            NoInputTimingGroups.Clear();
            NoInputTimingGroups.UnionWith(
                snapshot.NoInputTimingGroups);
            NoClipTimingGroups.Clear();
            NoClipTimingGroups.UnionWith(
                snapshot.NoClipTimingGroups);
            NoteTimingGroup.Clear();
            foreach (var pair in snapshot.NoteTimingGroup)
                NoteTimingGroup[pair.Key] = pair.Value;
            GroupSpeeds.Clear();
            CopyGroupSpeeds(snapshot.GroupSpeeds, GroupSpeeds);
            AutoplayDues.Clear();
            CopyAutoplayDues(
                snapshot.AutoplayDues, AutoplayDues);
            SpeedEvents.Clear();
            CopySpeedEvents(snapshot.SpeedEvents, SpeedEvents);
            SpawnKeysNow.Clear();
            SpawnKeysNow.UnionWith(snapshot.SpawnKeysNow);
            LastChartName = snapshot.LastChartName;
            LastChartIsExtension =
                snapshot.LastChartIsExtension;
            ChartGeneration = snapshot.ChartGeneration;
            CompileGeneration = snapshot.CompileGeneration;
            ExpectedSkyAreaCount =
                snapshot.ExpectedSkyAreaCount;
        }

        private static void CopyTrackMoves(
            List<TrackMoveEntry> source,
            List<TrackMoveEntry> target)
        {
            for (int i = 0; i < source.Count; i++)
            {
                TrackMoveEntry item = source[i];
                target.Add(new TrackMoveEntry
                {
                    TimeMs = item.TimeMs,
                    X = item.X, Y = item.Y, Z = item.Z,
                    AngXoz = item.AngXoz,
                    AngYoz = item.AngYoz,
                    AngXoy = item.AngXoy,
                    Ease = item.Ease,
                    DurationMs = item.DurationMs,
                    SourceOrder = item.SourceOrder,
                });
            }
        }

        private static void CopySideSlots(
            List<SideSlot> source, List<SideSlot> target)
        {
            for (int i = 0; i < source.Count; i++)
            {
                SideSlot item = source[i];
                target.Add(new SideSlot
                {
                    Side = item.Side,
                    Position = item.Position,
                    Id = item.Id,
                    StartMs = item.StartMs,
                    EndMs = item.EndMs,
                });
            }
        }

        private static void CopyGroupSlots(
            List<GroupSlot> source, List<GroupSlot> target)
        {
            for (int i = 0; i < source.Count; i++)
            {
                GroupSlot item = source[i];
                target.Add(new GroupSlot
                {
                    Side = item.Side,
                    Position = item.Position,
                    Id = item.Id,
                    Group = item.Group,
                    Type = item.Type,
                    CompiledOwner = item.CompiledOwner,
                    CompileGeneration = item.CompileGeneration,
                });
            }
        }

        private static void CopyShowTextEntries(
            List<ShowTextEntry> source, List<ShowTextEntry> target)
        {
            for (int i = 0; i < source.Count; i++)
            {
                ShowTextEntry item = source[i];
                target.Add(new ShowTextEntry
                {
                    TimeMs = item.TimeMs,
                    Id = item.Id,
                    Size = item.Size,
                    SourceOrder = item.SourceOrder,
                });
            }
        }

        private static void CopySceneControls(
            List<SceneControlEntry> source,
            List<SceneControlEntry> target)
        {
            for (int i = 0; i < source.Count; i++)
            {
                SceneControlEntry item = source[i];
                target.Add(new SceneControlEntry
                {
                    TimeMs = item.TimeMs,
                    Mode = item.Mode,
                    GroupId = item.GroupId,
                    DurationMs = item.DurationMs,
                    Value = item.Value,
                    SourceOrder = item.SourceOrder,
                });
            }
        }

        private static void CopySkyColorEntries(
            List<SkyColorEntry> source,
            List<SkyColorEntry> target)
        {
            for (int i = 0; i < source.Count; i++)
            {
                SkyColorEntry item = source[i];
                target.Add(new SkyColorEntry
                {
                    TimeMs = item.TimeMs,
                    SkyGroupId = item.SkyGroupId,
                    Rgba = item.Rgba,
                    EdgeGlow = item.EdgeGlow,
                    SourceOrder = item.SourceOrder,
                });
            }
        }

        private static void CopyGroupSpeeds(
            List<GroupSpeed> source, List<GroupSpeed> target)
        {
            for (int i = 0; i < source.Count; i++)
            {
                GroupSpeed item = source[i];
                target.Add(new GroupSpeed
                {
                    TimeMs = item.TimeMs,
                    Group = item.Group,
                    Mult = item.Mult,
                    SourceOrder = item.SourceOrder,
                });
            }
        }

        private static void CopyAutoplayDues(
            List<AutoplayDue> source, List<AutoplayDue> target)
        {
            for (int i = 0; i < source.Count; i++)
            {
                AutoplayDue item = source[i];
                target.Add(new AutoplayDue
                {
                    StartMs = item.StartMs,
                    EndMs = item.EndMs,
                    Type = item.Type,
                    Id = item.Id,
                    Side = item.Side,
                    Position = item.Position,
                    SourceOrder = item.SourceOrder,
                });
            }
        }

        private static void CopySpeedEvents(
            List<SpeedEvent> source, List<SpeedEvent> target)
        {
            for (int i = 0; i < source.Count; i++)
            {
                SpeedEvent item = source[i];
                target.Add(new SpeedEvent
                {
                    TimeMs = item.TimeMs,
                    Mult = item.Mult,
                    SourceOrder = item.SourceOrder,
                });
            }
        }

        internal static void ResetChartState()
        {
            ChartGeneration++;
            ChartRuntimeLifecycle.MarkChartActive();
            ChartRuntimeLifecycle.Dispose();
            ClearChartState();
        }

        private static void ClearChartState()
        {
            TrackMoves.Clear();
            BombIds.Clear();
            SpeedEvents.Clear();
            SpawnKeysNow.Clear();
            SkyAreaSlots.Clear();
            GroupedSlots.Clear();
            ShowTextEntries.Clear();
            HasAudioOffset = false;
            AudioOffsetMs = 0;
            SceneControls.Clear();
            HideGroupControlledTimingGroups.Clear();
            SkyColorEvents.Clear();
            SkyAreaGroupById.Clear();
            NoInputTimingGroups.Clear();
            NoClipTimingGroups.Clear();
            NoteTimingGroup.Clear();
            GroupSpeeds.Clear();
            AutoplayDues.Clear();
            ExpectedSkyAreaCount = 0;
        }
    }
}
