using System;
using System.IO;
using System.Runtime.InteropServices;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;

namespace InFalsusChartExt
{
    /// <summary>
    /// Re-encodes a chart's ICP1 payload in place of the game's parse call: extension trackmove
    /// notes (type 3 + magic flag) are extracted, noinput sky areas are recognised by their own
    /// flags bit 0x8000 (new encoding) or by legacy noinput events (type 2, time=0 &amp; value2=0),
    /// and native BPM transition events (type 2) are preserved as-is.
    /// </summary>
    [HarmonyPatch]
    [HarmonyPriority(Priority.Low)]
    internal static class ChartParsePatch
    {
        // Keep rewritten span backing arrays alive only for the current _Gab call.
        private static readonly System.Collections.Generic.List<object> PinnedArrays = new System.Collections.Generic.List<object>();
        private static int _logged;
        private static string _lastKey;
        private static int _gabCallDepth;

        [HarmonyPrepare]
        private static bool Prepare() =>
            NativePatchTargetResolver.IsAvailable("chartParse");

        [HarmonyTargetMethod]
        private static System.Reflection.MethodBase TargetMethod() =>
            NativePatchTargetResolver.ResolveHarmony("chartParse");

        [HarmonyPrefix]
        private static void Prefix(ref Il2CppSystem.ReadOnlySpan<byte> __0, string __1)
        {
            _gabCallDepth++;
            TransformSpan(ref __0, __1 ?? string.Empty, "GabPrefix");
        }

        [HarmonyPostfix]
        private static void Postfix()
            => EndGabCall();

        [HarmonyFinalizer]
        private static Exception Finalizer(Exception __exception)
        {
            EndGabCall();
            return __exception;
        }

        /// <summary>Postfix used on the custom-song hook's Prefix, so the transform runs on the
        /// substituted bytes no matter how Harmony ordered the two _Gab prefixes.</summary>
        internal static void HookPostfix(ref Il2CppSystem.ReadOnlySpan<byte> __0, ref string __1)
            => TransformSpan(ref __0, __1 ?? string.Empty, "HookPostfix");

        internal static void TransformSpan(ref Il2CppSystem.ReadOnlySpan<byte> span, string key, string source)
        {
            if (!BuildProfile.Supports(NativeCapability.ChartIngress))
                return;
            ChartExtState.TransformSnapshot transform = null;
            ChartRuntimeFrame preparedRuntime = null;
            Il2CppSystem.ReadOnlySpan<byte> originalSpan = span;
            bool spanReplaced = false;
            try
            {
                int length = span._length;
                if (length < Icp1Codec.HeaderSize || length > 64 * 1024 * 1024)
                {
                    LogOnce(source, key, length, "-", "guard-skip", null);
                    return;
                }

                // The Il2CppInterop ReadOnlySpan wrapper hides the data pointer inside a
                // boxed ByReference value; depending on the wrapper flavour the usable
                // address is the ByReference field itself or the pointer stored inside it.
                // Try every candidate and keep the first one that really starts with the
                // ICP1 magic.
                byte[] data = null;
                string resolvedVia = null;

                IntPtr p = IntPtr.Zero;
                try { p = span._pointer._value; } catch { }
                data = TryCopy(p, length, ref resolvedVia, "byref._value");

                if (data == null)
                {
                    try
                    {
                        Il2CppSystem.ByReference<byte> byref = span._pointer;
                        if (byref != null)
                        {
                            ref byte first = ref byref.Value;
                            unsafe { p = (IntPtr)System.Runtime.CompilerServices.Unsafe.AsPointer(ref first); }
                        }
                    }
                    catch { p = IntPtr.Zero; }
                    data = TryCopy(p, length, ref resolvedVia, "byref.Value");
                }

                if (data == null)
                {
                    try
                    {
                        IntPtr box = span._pointer.Pointer;
                        p = box == IntPtr.Zero ? IntPtr.Zero : Marshal.ReadIntPtr(Il2CppInterop.Runtime.IL2CPP.il2cpp_object_unbox(box));
                    }
                    catch { p = IntPtr.Zero; }
                    data = TryCopy(p, length, ref resolvedVia, "unbox");
                }

                if (data == null)
                {
                    try { p = span._pointer.Pointer; } catch { p = IntPtr.Zero; }
                    data = TryCopy(p, length, ref resolvedVia, "raw");
                }

                if (data == null)
                {
                    LogOnce(source, key, length, "-", "not-icp1", null);
                    return;
                }

                if (!Icp1Codec.TryDecode(data, key, out Icp1Codec.Chart chart))
                {
                    LogOnce(source, key, length, Head(data), "decode-fail", null);
                    return;
                }

                int markerNotes = 0;
                foreach (Icp1Codec.Note n in chart.Notes)
                {
                    uint containerMagic = n.Type == 3 ? ((n.Flags >> 8) & 0xFFFFFF) : 0;
                    if (n.Type == 3 &&
                        (containerMagic == ChartExtState.TrackMoveMagic ||
                         containerMagic == ChartExtState.ShowTextMagic ||
                         containerMagic == ChartExtState.AudioOffsetMagic ||
                         containerMagic == ChartExtState.SceneControlMagic ||
                         containerMagic == ChartExtState.SetColorMagic))
                        markerNotes++;
                    // bomb / noinput 标记位（同一 bit）：原版 303 张谱里 type1 的 flags 恒为 0、
                    // type5 从无 0x8000，因此该位可作为"本 Mod 扩展谱面"的安全入口判据。
                    else if ((n.Type == 1 || n.Type == 5) && (n.Flags & ChartExtState.NoInputFlag) != 0)
                        markerNotes++;
                    // timinggroup：组号存在 group 的高 32 位（官谱从未使用；trackmove 的 group
                    // 是坐标数据、showtext 的 group 是文本 id，均跳过）。
                    else if (n.Type != 3 && ((ulong)n.Group >> 32) != 0)
                        markerNotes++;
                }
                // type2 有双重语义：
                //  - Mod 扩展 noinput：打包器固定 time=0、value2=0（value1=天域组号）；
                //  - 原生 BPM 过渡（demo 的 BpmChangeV3：bpm(t,bpm,拍数,mode≠0)）：time/拍数正常携带。
                // 只能用 time=0 && value2=0 判"这是扩展事件"，否则官方 shouldhavebeen0-3 会被误判为扩展谱。
                int markerEvents = 0;
                int nativeT2 = 0;
                foreach (Icp1Codec.Event ev in chart.Events)
                {
                    if (ev.Type == 5) { markerEvents++; continue; }   // timinggroup 组内变速（扩展事件）
                    if (ev.Type != 2) continue;
                    if (ev.TimeMs == 0 && ev.Value2 == 0) markerEvents++;
                    else nativeT2++;
                }

                if (markerNotes == 0 && markerEvents == 0)
                {
                    // No extension statements in this payload. If it is the second visit of the
                    // same chart (hook postfix transformed it first), keep the extracted state.
                    bool sameKey = string.Equals(_lastKey, key, StringComparison.OrdinalIgnoreCase);
                    bool sameRuntimePlan =
                        ChartRuntimeContext.IsActive &&
                        string.Equals(
                            ChartRuntimeContext.Current.ChartKey,
                            key,
                            StringComparison.OrdinalIgnoreCase) &&
                        ChartRuntimeContext.Current.SkyBlocks != null;
                    // 自定义谱转码后的第二次解析来自 GabPrefix；只有这一种情况
                    // 可以沿用上一轮扩展状态。其余新解析周期必须清空残留。
                    bool secondVisitOfTransformedChart =
                        sameKey && source == "GabPrefix" &&
                        (ChartExtState.LastChartIsExtension ||
                         sameRuntimePlan);
                    if (!sameKey || !secondVisitOfTransformedChart)
                    {
                        ChartExtState.ResetChartState();
                        ChartExtState.LastChartIsExtension = false;
                        ChartExtState.LastChartName = key;
                        _lastKey = key;
                    }
                    if (!ChartRuntimeContext.IsActive)
                    {
                        bool applySkyBlocks = IsCustomChartKey(key);
                        if (applySkyBlocks)
                            CaptureGlobalSpeedEvents(chart.Events);
                        ChartIngressResult ingress = ChartIngress.NativeOnly(
                            key, source, chart.Notes, chart.Events, nativeT2,
                            applySkyBlocks);
                        int cleanRuntimeGroupAliases =
                            ingress.SkyBlocks?.ApplyRuntimeGroups(
                                chart.Notes) ?? 0;
                        bool changed = cleanRuntimeGroupAliases > 0;
                        ChartRuntimeContext.Begin(ingress);
                        if (changed)
                        {
                            ReplaceEncodedSpan(ref span, key, chart);
                            LogOnce(
                                source, key, length, Head(data),
                                "clean+skyblocks" +
                                $" rg={cleanRuntimeGroupAliases}", null);
                        }
                        else
                        {
                            LogOnce(
                                source, key, length, Head(data),
                                $"clean(notes={chart.Notes.Count},events={chart.Events.Count},nativeT2={nativeT2})",
                                null);
                        }
                    }
                    else
                    {
                        LogOnce(source, key, length, Head(data), "clean-reuse-plan", null);
                    }
                    return;
                }

                // A chart carrying extension statements: rebuild the runtime state for it.
                transform = ChartExtState.BeginTransform();
                ChartExtState.LastChartIsExtension = true;
                ChartExtState.LastChartName = key;
                _lastKey = key;

                // 兼容两种 noinput 表示：
                //  - 新：type5 音符自身的 flags 0x8000（文件里没有事件，下面循环直接读 note.Flags）；
                //  - 旧：type2 事件（time=0 且 value2=0）携带组号 —— 这里收集后补打 flag。
                var noInputGroups = new System.Collections.Generic.HashSet<long>();
                foreach (Icp1Codec.Event ev in chart.Events)
                    if (ev.Type == 2 && ev.TimeMs == 0 && ev.Value2 == 0)
                        noInputGroups.Add((long)BitConverter.Int64BitsToDouble((long)ev.Value1));

                var keptNotes = new System.Collections.Generic.List<Icp1Codec.Note>(chart.Notes.Count);
                int noteSourceOrder = 0;
                foreach (Icp1Codec.Note note in chart.Notes)
                {
                    int currentNoteOrder = noteSourceOrder++;
                    uint containerMagic = note.Type == 3 ? ((note.Flags >> 8) & 0xFFFFFF) : 0;
                    if (note.Type == 3 && containerMagic == ChartExtState.AudioOffsetMagic)
                    {
                        if (!ChartExtState.HasAudioOffset)
                        {
                            ChartExtState.HasAudioOffset = true;
                            ChartExtState.AudioOffsetMs = (int)note.Group;
                        }
                        continue;
                    }

                    if (note.Type == 3 && containerMagic == ChartExtState.SceneControlMagic)
                    {
                        int mode = (int)(note.Flags & 0xFF);
                        ChartExtState.SceneControls.Add(new ChartExtState.SceneControlEntry
                        {
                            TimeMs = (int)note.StartMs,
                            Mode = mode,
                            GroupId = (int)(uint)note.Group,
                            DurationMs = mode == ChartExtState.SceneTrackDisplay ? (int)note.EndMs : 0,
                            Value = mode == ChartExtState.SceneTrackDisplay ? note.Pairs[0] : (int)note.EndMs,
                            SourceOrder = currentNoteOrder,
                        });
                        continue;
                    }

                    if (note.Type == 3 && containerMagic == ChartExtState.SetColorMagic)
                    {
                        ChartExtState.SkyColorEvents.Add(new ChartExtState.SkyColorEntry
                        {
                            TimeMs = (int)note.StartMs,
                            SkyGroupId = (long)((ulong)note.Group & 0xFFFFFFFF),
                            Rgba = note.EndMs,
                            EdgeGlow = note.Pairs.Length > 0 && note.Pairs[0] != 0,
                            SourceOrder = currentNoteOrder,
                        });
                        continue;
                    }

                    if (note.Type == 3 && containerMagic == ChartExtState.TrackMoveMagic)
                    {
                        float x = note.Pairs[0] / 1000000f;
                        float y = note.Pairs[2] / 1000000f;
                        float z = note.Pairs[4] / 1000000f;
                        float xoz = note.Pairs[6] / 1000000f;
                        int yoz = (int)(uint)note.Group;
                        int xoy = (int)(uint)((ulong)note.Group >> 32);
                        ChartExtState.TrackMoves.Add(new ChartExtState.TrackMoveEntry
                        {
                            TimeMs = note.StartMs,
                            X = x,
                            Y = y,
                            Z = z,
                            AngXoz = xoz,
                            AngYoz = yoz / 1000f,
                            AngXoy = xoy / 1000f,
                            Ease = (int)(note.Flags & 0xFF),
                            DurationMs = note.EndMs - note.StartMs,
                            SourceOrder = currentNoteOrder,
                        });
                        continue;
                    }

                    // showtext 容器：锁存 (时间, 文本 id) 后从载荷中移除（游戏完全看不到该音符）。
                    if (note.Type == 3 && containerMagic == ChartExtState.ShowTextMagic)
                    {
                        ChartExtState.ShowTextEntries.Add(new ChartExtState.ShowTextEntry
                        {
                            TimeMs = note.StartMs,
                            Id = note.Group,
                            Size = note.Pairs[1] != 0 ? note.Pairs[0] / note.Pairs[1] : 0,
                            SourceOrder = currentNoteOrder,
                        });
                        continue;
                    }

                    // 旧表示在此补打 flag；新表示文件里已带（保持不变，幂等）
                    if (note.Type == 5 && noInputGroups.Contains(note.Group))
                        note.Flags |= ChartExtState.NoInputFlag;
                    keptNotes.Add(note);
                }

                var keptEvents = new System.Collections.Generic.List<Icp1Codec.Event>(chart.Events.Count);
                int eventSourceOrder = 0;
                foreach (Icp1Codec.Event ev in chart.Events)
                {
                    int currentEventOrder = eventSourceOrder++;
                    // timinggroup 组内变速（type5，value1=double 倍率，value2=float 组号）：
                    // 剥离进状态，运行期用它合成该组独立的 W(t)=∫speed dt 时间线。
                    if (ev.Type == 5)
                    {
                        ChartExtState.GroupSpeeds.Add(new ChartExtState.GroupSpeed
                        {
                            TimeMs = ev.TimeMs,
                            Group = (int)BitConverter.Int32BitsToSingle((int)(uint)ev.Value2),
                            Mult = BitConverter.Int64BitsToDouble((long)ev.Value1),
                            SourceOrder = currentEventOrder,
                        });
                        continue;
                    }
                    // 只剔除本 Mod 的 noinput 事件；原生 BPM 过渡保留（随后按序重编号）
                    if (!(ev.Type == 2 && ev.TimeMs == 0 && ev.Value2 == 0)) keptEvents.Add(ev);
                }
                ChartExtState.GroupSpeeds.Sort(CompareTimeAndOrder);
                ChartExtState.SceneControls.Sort(CompareTimeAndOrder);
                ChartExtState.TrackMoves.Sort(CompareTimeAndOrder);
                ChartExtState.HideGroupControlledTimingGroups.Clear();
                foreach (ChartExtState.SceneControlEntry entry
                         in ChartExtState.SceneControls)
                {
                    if (entry.Mode == ChartExtState.SceneHideGroup)
                        ChartExtState.HideGroupControlledTimingGroups.Add(
                            entry.GroupId);
                }
                ChartExtState.SkyColorEvents.Sort(CompareTimeAndOrder);

                // Dropping the noinput (type 2) events leaves gaps in the event ids
                // (e.g. 0,1,4,5,..). The runtime consumes the compiled event list by
                // index, and a gap made the whole chart scroll with a wrong speed
                // reading - renumber the survivors consecutively like a freshly
                // packed chart would have them.
                for (int i = 0; i < keptEvents.Count; i++)
                    keptEvents[i].Id = (ulong)i;

                // Speed events (track(...), v1 type 0): multiplier is a double in Value1.
                // The game maps the scroll as (time-left x current multiplier) - including
                // the position jump at each event; the bomb visual must read the same
                // timeline (see WarpedMs / ChartExtState.SpeedEvents in BombSkin.cs).
                for (int speedOrder = 0;
                     speedOrder < keptEvents.Count;
                     speedOrder++)
                {
                    Icp1Codec.Event ev = keptEvents[speedOrder];
                    if (ev.Type != 0) continue;
                    ChartExtState.SpeedEvents.Add(new ChartExtState.SpeedEvent
                    {
                        TimeMs = ev.TimeMs,
                        Mult = BitConverter.Int64BitsToDouble((long)ev.Value1),
                        SourceOrder = speedOrder,
                    });
                }
                ChartExtState.SpeedEvents.Sort(CompareTimeAndOrder);

                // BD Studio files may appear grouped by timinggroup instead of
                // chronological order. The runtime consumes the exported note
                // array in chronological order, so use the same stable ordering
                // as the Python converter. Visual-only record compaction is done
                // later on the compiled record slices and must not reorder notes.
                keptNotes.Sort((a, b) =>
                {
                    int c = a.StartMs.CompareTo(b.StartMs);
                    if (c != 0) return c;
                    c = a.EndMs.CompareTo(b.EndMs);
                    return c != 0 ? c : a.Index.CompareTo(b.Index);
                });
                // Do not rewrite authored SkyArea duration or group ids here.
                // Imported chart data must retain its original geometry and
                // grouping; compatibility belongs in an independent runtime.

                // Latch the note ids of the extended statements; the encoder writes
                // id = position, so the runtime patches can match on these ids alone.
                // The marker bits are then stripped from the exported payload: the
                // game must receive fully vanilla note records, the mod remembers
                // which ids carry the extended behaviour by itself.
                var legacyNoInputIds =
                    new System.Collections.Generic.HashSet<long>();
                for (int i = 0; i < keptNotes.Count; i++)
                {
                    Icp1Codec.Note note = keptNotes[i];
                    if (note.Type == 1 && (note.Flags & ChartExtState.BombFlag) != 0)
                    {
                        ChartExtState.BombIds.Add(i);
                        note.Flags &= ~(uint)ChartExtState.BombFlag;
                    }
                    else if (note.Type == 5 && (note.Flags & ChartExtState.NoInputFlag) != 0)
                    {
                        legacyNoInputIds.Add(i);
                        note.Flags &= ~(uint)ChartExtState.NoInputFlag;
                    }
                }

                // timinggroup：把每音符的组号从 group 高 32 位剥离进运行期状态，
                // 导出载荷里的 group 恢复 vanilla 语义（trackmove 的 group 是坐标数据，跳过）。
                for (int i = 0; i < keptNotes.Count; i++)
                {
                    Icp1Codec.Note note = keptNotes[i];
                    if (note.Type == 3) continue;
                    uint packedGroup = (uint)((ulong)note.Group >> 32);
                    int g = (int)(packedGroup & ChartExtState.TimingGroupIdMask);
                    if (g != 0)
                    {
                        ChartExtState.NoteTimingGroup[i] = g;
                        if ((packedGroup & (ChartExtState.TimingGroupNoInputBit |
                                            ChartExtState.TimingGroupNoClipBit)) != 0)
                            ChartExtState.NoInputTimingGroups.Add(g);
                        if ((packedGroup & ChartExtState.TimingGroupNoClipBit) != 0)
                            ChartExtState.NoClipTimingGroups.Add(g);
                        note.Group = (long)((ulong)note.Group & 0xFFFFFFFF);
                    }
                }

                // 重新编号：剔除 trackmove/showtext 容器后原 id 会留下空档，而游戏约定
                // note id = 数组位置（我们的运行期锁存也按位置匹配）。
                // 地键组号仍按位置写回；skyarea 的连续链号由转谱阶段单独维护。
                for (int i = 0; i < keptNotes.Count; i++)
                {
                    Icp1Codec.Note note = keptNotes[i];
                    note.Index = i;
                    if (note.Type == 1 || note.Type == 2 || note.Type == 4)
                        note.Group = i;
                }

                // noinput/noclip are visual-only by definition. Remove every
                // target type before encoding so the native compiler never sees
                // their notes, records, scoring entries or judgement effects.
                var noteTimingGroupBeforeMigration =
                    new System.Collections.Generic.Dictionary<long, int>(
                        ChartExtState.NoteTimingGroup);
                var bombIdsBeforeMigration =
                    new System.Collections.Generic.HashSet<long>(
                        ChartExtState.BombIds);
                BombPlan bombPlan = BombPlan.TryBuild(
                    keptNotes,
                    noteTimingGroupBeforeMigration,
                    ChartExtState.NoInputTimingGroups,
                    ChartExtState.NoClipTimingGroups,
                    bombIdsBeforeMigration,
                    legacyNoInputIds);
                if (bombIdsBeforeMigration.Count > 0 &&
                    bombPlan == null)
                    throw new InvalidOperationException(
                        "bomb plan could not describe the authored bombs");
                ChartIngressPartition partition =
                    ChartIngress.PartitionVisualNotes(
                        keptNotes,
                        legacyNoInputIds,
                        noteTimingGroupBeforeMigration,
                        ChartExtState.NoInputTimingGroups,
                        ChartExtState.NoClipTimingGroups,
                        bombIdsBeforeMigration);
                keptNotes = partition.NativeNotes;
                var oldIdByNew = partition.OldIdByNew;
                var visualNotes = partition.VisualNotes;
                int removedVisualNotes = partition.RemovedCount;
                if (bombPlan != null &&
                    !bombPlan.BindAfterPartition(
                        oldIdByNew, visualNotes))
                    throw new InvalidOperationException(
                        "bomb plan could not bind post-ingress ids");
                int nativeSkyAreaCount = 0;
                for (int i = 0; i < keptNotes.Count; i++)
                {
                    Icp1Codec.Note note = keptNotes[i];
                    note.Index = i;
                    if (note.Type == 1 || note.Type == 2 || note.Type == 4)
                        note.Group = i;
                    if (note.Type == 5)
                        nativeSkyAreaCount++;
                }
                ChartExtState.ExpectedSkyAreaCount =
                    nativeSkyAreaCount;

                ChartExtState.NoteTimingGroup.Clear();
                ChartExtState.BombIds.Clear();
                for (int i = 0; i < keptNotes.Count; i++)
                {
                    Icp1Codec.Note note = keptNotes[i];
                    long oldId = oldIdByNew[i];

                    if (noteTimingGroupBeforeMigration.TryGetValue(
                            oldId, out int timingGroup))
                        ChartExtState.NoteTimingGroup[i] = timingGroup;
                    if (bombIdsBeforeMigration.Contains(oldId))
                        ChartExtState.BombIds.Add(i);
                }

                if (visualNotes.Count > 0)
                {
                    ChartExtMod.Log.Msg(
                        $"[ChartExt] precompile visual notes: " +
                        $"{removedVisualNotes} removed from native payload " +
                        $"(remaining native notes={keptNotes.Count})");
                }
                // Capture authored group identity before the runtime block
                // planner replaces low 32 bits with a monotonic runtime alias.
                // setcolor and the independent visual layer keep using this map.
                ChartExtState.SkyAreaGroupById.Clear();
                for (int i = 0; i < keptNotes.Count; i++)
                {
                    Icp1Codec.Note note = keptNotes[i];
                    if (note.Type == 5)
                        ChartExtState.SkyAreaGroupById[i] =
                            (long)((ulong)note.Group & 0xFFFFFFFF);
                }

                ChartIngressResult transformedIngress =
                    ChartIngress.FromCurrentState(
                        key, source, keptNotes, keptEvents, nativeT2,
                        bombPlan);
                int extensionRuntimeGroupAliases =
                    transformedIngress.SkyBlocks
                        ?.ApplyRuntimeGroups(keptNotes) ?? 0;
                if (extensionRuntimeGroupAliases > 0)
                {
                    ChartExtMod.Log.Msg(
                        $"[ChartExt] skyarea runtime groups: " +
                        $"{extensionRuntimeGroupAliases} note(s) aliased " +
                        $"monotonically");
                }
                for (int i = 0; i < visualNotes.Count; i++)
                    transformedIngress.VisualChart.Add(
                        visualNotes[i]);
                chart.Notes.Clear();
                chart.Notes.AddRange(keptNotes);
                chart.Events.Clear();
                chart.Events.AddRange(keptEvents);

                // 防御性兜底：处理完成后若没有任何真实扩展内容（trackmove / noinput / bomb 全空），
                // 说明只是入口判据误触发——保持原始字节不变，绝不重编码非本 Mod 的谱面。
                if (ChartExtState.TrackMoves.Count == 0 && noInputGroups.Count == 0 &&
                    ChartExtState.BombIds.Count == 0 &&
                    removedVisualNotes == 0 &&
                    ChartExtState.ShowTextEntries.Count == 0 && ChartExtState.NoteTimingGroup.Count == 0 &&
                    ChartExtState.GroupSpeeds.Count == 0 &&
                    !ChartExtState.HasAudioOffset && ChartExtState.SceneControls.Count == 0 &&
                    ChartExtState.SkyColorEvents.Count == 0 &&
                    ChartExtState.NoInputTimingGroups.Count == 0)
                {
                    ChartExtState.RollbackTransform(transform);
                    transform = null;
                    ChartExtState.ResetChartState();
                    ChartExtState.LastChartIsExtension = false;
                    ChartExtState.LastChartName = key;
                    ChartRuntimeContext.Begin(ChartIngress.NativeOnly(
                        key, source, chart.Notes, chart.Events, nativeT2));
                    LogOnce(source, key, length, Head(data), "passthrough(no extension content)", null);
                    return;
                }

                byte[] encoded = Icp1Codec.Encode(chart, key);
                var managed = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<byte>(encoded);
                PinSpanArray(managed);
                preparedRuntime =
                    ChartRuntimeContext.Prepare(transformedIngress);
                if (preparedRuntime == null)
                    throw new InvalidOperationException(
                        "chart runtime frame could not be prepared");
                var replacementSpan =
                    new Il2CppSystem.ReadOnlySpan<byte>(
                    (Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppArrayBase<byte>)(object)managed);
                ChartRuntimeLifecycle.DisposeForChartTransform();
                span = replacementSpan;
                spanReplaced = true;
                ChartRuntimeContext.InstallPrepared(preparedRuntime);
                preparedRuntime = null;
                ChartExtState.CommitTransform(transform);
                transform = null;

                ChartExtMod.Log.Msg($"[ChartExt] transformed via {source}: key='{key}' {length}->{encoded.Length} bytes, " +
                                    $"(ptr {resolvedVia}) notes {chart.Notes.Count}, events {chart.Events.Count}, " +
                                    $"trackmoves {ChartExtState.TrackMoves.Count}, visual-notes {removedVisualNotes} (legacy groups {noInputGroups.Count}), " +
                                    $"speed events {ChartExtState.SpeedEvents.Count}, nativeT2 {nativeT2}");
            }
            catch (Exception e)
            {
                ChartRuntimeContext.DiscardPrepared(preparedRuntime);
                ChartExtState.RollbackTransform(transform);
                if (spanReplaced)
                    span = originalSpan;
                if (_logged < 80) { _logged++; ChartExtMod.Log.Error($"[ChartExt] transform failed ({source}): {e}"); }
            }
        }

        private static bool IsCustomChartKey(string key)
        {
            try
            {
                return InFalsusChartExt.CustomSong.Lifecycle
                    .CustomSongRuntime.TryGetChartByAnyKey(
                        key, out _);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Clean custom charts keep their payload byte-for-byte unchanged but
        /// still use Mod-owned SkyBlock visuals. Copy the global track()
        /// timeline into Mod state so W_global(t) is not a constant-speed line.
        /// </summary>
        private static void CaptureGlobalSpeedEvents(
            System.Collections.Generic.IReadOnlyList<Icp1Codec.Event> events)
        {
            ChartExtState.SpeedEvents.Clear();
            if (events == null)
                return;
            for (int i = 0; i < events.Count; i++)
            {
                Icp1Codec.Event ev = events[i];
                if (ev.Type != 0) continue;
                ChartExtState.SpeedEvents.Add(
                    new ChartExtState.SpeedEvent
                    {
                        TimeMs = ev.TimeMs,
                        Mult = BitConverter.Int64BitsToDouble(
                            (long)ev.Value1),
                        SourceOrder = i,
                    });
            }
            ChartExtState.SpeedEvents.Sort(CompareTimeAndOrder);
        }

        private static int CompareTimeAndOrder(
            ChartExtState.TrackMoveEntry a,
            ChartExtState.TrackMoveEntry b)
            => CompareTimeAndOrder(
                a.TimeMs, a.SourceOrder,
                b.TimeMs, b.SourceOrder);

        private static int CompareTimeAndOrder(
            ChartExtState.GroupSpeed a,
            ChartExtState.GroupSpeed b)
            => CompareTimeAndOrder(
                a.TimeMs, a.SourceOrder,
                b.TimeMs, b.SourceOrder);

        private static int CompareTimeAndOrder(
            ChartExtState.SceneControlEntry a,
            ChartExtState.SceneControlEntry b)
            => CompareTimeAndOrder(
                a.TimeMs, a.SourceOrder,
                b.TimeMs, b.SourceOrder);

        private static int CompareTimeAndOrder(
            ChartExtState.SkyColorEntry a,
            ChartExtState.SkyColorEntry b)
            => CompareTimeAndOrder(
                a.TimeMs, a.SourceOrder,
                b.TimeMs, b.SourceOrder);

        private static int CompareTimeAndOrder(
            ChartExtState.SpeedEvent a,
            ChartExtState.SpeedEvent b)
            => CompareTimeAndOrder(
                a.TimeMs, a.SourceOrder,
                b.TimeMs, b.SourceOrder);

        private static int CompareTimeAndOrder(
            double leftTime,
            int leftOrder,
            double rightTime,
            int rightOrder)
        {
            int time = leftTime.CompareTo(rightTime);
            return time != 0
                ? time
                : leftOrder.CompareTo(rightOrder);
        }

        private static void ReplaceEncodedSpan(
            ref Il2CppSystem.ReadOnlySpan<byte> span,
            string key,
            Icp1Codec.Chart chart)
        {
            byte[] encoded = Icp1Codec.Encode(chart, key);
            var managed =
                new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<byte>(
                    encoded);
            PinSpanArray(managed);
            span = new Il2CppSystem.ReadOnlySpan<byte>(
                (Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppArrayBase<byte>)
                (object)managed);
        }

        private static void PinSpanArray(object managed)
        {
            PinnedArrays.Add(managed);
        }

        private static void ReleasePinnedArrays()
        {
            PinnedArrays.Clear();
        }

        private static void EndGabCall()
        {
            if (_gabCallDepth > 0)
                _gabCallDepth--;
            if (_gabCallDepth == 0)
                ReleasePinnedArrays();
        }

        /// <summary>Copies the span bytes from a candidate pointer, but only when the
        /// target really begins with the ICP1 magic so a wrong wrapper guess is skipped
        /// instead of corrupting the transform.</summary>
        private static byte[] TryCopy(IntPtr pointer, int length, ref string via, string candidate)
        {
            if (pointer == IntPtr.Zero) return null;
            try
            {
                if (Marshal.ReadInt32(pointer) != 0x31504349) return null; // 'I','C','P','1'
                byte[] data = new byte[length];
                Marshal.Copy(pointer, data, 0, length);
                via = candidate;
                return data;
            }
            catch
            {
                return null;
            }
        }

        private static string Head(byte[] data)
        {
            int n = Math.Min(8, data.Length);
            var sb = new System.Text.StringBuilder(n * 2);
            for (int i = 0; i < n; i++) sb.Append(data[i].ToString("X2"));
            return sb.ToString();
        }

        private static void LogOnce(string source, string key, int length, string head, string status, string extra)
        {
            if (_logged >= 60) return;
            _logged++;
            ChartExtMod.Log.Msg($"[ChartExt] _Gab[{source}] key='{key}' len={length} head={head} -> {status}{(extra == null ? "" : " " + extra)}");
        }
    }

    /// <summary>
    /// Materialize the compiled IL2CPP Memory&lt;_cH&gt; slices before the
    /// native note-timeline builder consumes them. Without this pass the
    /// interop wrapper can expose an incomplete record dictionary to _kb and
    /// the chart aborts after its first judged note.
    /// </summary>
    [HarmonyPatch]
    internal static class CompiledRecordWarmup
    {
        private static int _errors;

        [HarmonyPrepare]
        private static bool Prepare() =>
            NativePatchTargetResolver.IsAvailable(
                "logicalNotePlayerKb");

        [HarmonyTargetMethod]
        private static System.Reflection.MethodBase TargetMethod() =>
            NativePatchTargetResolver.ResolveHarmony(
                "logicalNotePlayerKb");

        [HarmonyPrefix]
        private static void Prefix(Il2Cppifapp.Game.LogicalNotePlayer __instance)
        {
            if (!BuildProfile.Supports(NativeCapability.ChartIngress) ||
                !BuildProfile.SupportsNativeRead)
                return;
            try
            {
                if (__instance == null || __instance.Pointer == IntPtr.Zero ||
                    !ChartExtState.LastChartIsExtension)
                    return;
                Il2Cpp_b._T compiled = __instance._ve;
                if (compiled == null || compiled.Pointer == IntPtr.Zero) return;
                var records = compiled._XC;
                if (records == null || records.Pointer == IntPtr.Zero) return;
                foreach (var pair in records)
                {
                    Il2CppSystem.Memory<Il2Cpp_K._cH> memory = pair.Value;
                    if (memory._object == null || memory._object.Pointer == IntPtr.Zero)
                        continue;
                    _ = memory._index;
                    _ = memory._length;
                }
            }
            catch (Exception error)
            {
                if (_errors++ < 4)
                    ChartExtMod.Log.Error("[ChartExt] compiled record warmup failed: " + error.Message);
            }
        }
    }

    /// <summary>
    /// Builds runtime slot tables for the native notes that remain after
    /// noinput/noclip have been removed by ChartIngress.
    /// </summary>
    [HarmonyPatch]
    internal static class ChartCompilePatch
    {
        private static int _logged;

        [HarmonyPrepare]
        private static bool Prepare() =>
            NativePatchTargetResolver.IsAvailable(
                "logicalNotePlayerIb");

        [HarmonyTargetMethod]
        private static System.Reflection.MethodBase TargetMethod() =>
            NativePatchTargetResolver.ResolveHarmony(
                "logicalNotePlayerIb");

        [HarmonyPrefix]
        private static void Prefix()
        {
            if (!BuildProfile.Supports(NativeCapability.ChartIngress) ||
                !BuildProfile.SupportsNativeRead)
                return;
            AutoplayRuntime.ResetForChartSwitch();
            TimingGroupRuntime.BeginCompile();
            // 每次编译重新锁存槽位，避免重复编译时累积。
            ChartExtState.SkyAreaSlots.Clear();
            ChartExtState.GroupedSlots.Clear();
        }

        [HarmonyPostfix]
        private static void Postfix(Il2Cppifapp.Game.LogicalNotePlayer __instance)
        {
            if (!BuildProfile.Supports(NativeCapability.ChartIngress) ||
                !BuildProfile.SupportsNativeRead)
                return;
            try
            {
                if (__instance == null || __instance.Pointer == IntPtr.Zero)
                {
                    return;
                }
                Il2Cpp_b._T compiled = __instance._ve;
                if (compiled == null || compiled.Pointer == IntPtr.Zero)
                {
                    return;
                }

                var sides = compiled._vC;
                if (sides == null)
                {
                    return;
                }
                if (!InteropUtil.ResolveRecordLayout())
                {
                    return;
                }

                ChartExtState.AutoplayDues.Clear();
                bool allowTimingGroup =
                    BuildProfile.Supports(NativeCapability.TimingGroup);
                bool allowAutoplay =
                    BuildProfile.Supports(NativeCapability.Autoplay);
                for (int side = 0; side < NativeLayoutCatalog.SideCount; side++)
                {
                    if (!sides.ContainsKey((Il2Cpp_b._CA)side)) continue;
                    Il2Cpp_b._hA list = sides[(Il2Cpp_b._CA)side];
                    if (list == null || list.Pointer == IntPtr.Zero) continue;
                    Il2CppStructArray<Il2Cpp_b._fA> notes = list._ye;
                    int count = list._ze;
                    if (notes == null || count <= 0) continue;
                    IntPtr notesData = notes.Pointer + InteropUtil.ArrayDataOffset;
                    for (int i = 0; i < count && i < notes.Length; i++)
                    {
                        long id = notes[i]._ZD;
                        if ((int)notes[i]._be == 5)
                            ChartExtState.SkyAreaSlots.Add(
                                new ChartExtState.SideSlot
                                {
                                    Side = side,
                                    Position = i,
                                    Id = id,
                                });
                        if (allowTimingGroup &&
                            ChartExtState.NoteTimingGroup.TryGetValue(
                                id, out int timingGroup))
                            ChartExtState.GroupedSlots.Add(new ChartExtState.GroupSlot
                            {
                                Side = side,
                                Position = i,
                                Id = id,
                                Group = timingGroup,
                                Type = (int)notes[i]._be,
                                CompiledOwner = compiled.Pointer,
                                CompileGeneration =
                                    ChartExtState.CompileGeneration,
                            });
                        // autoplay：锁存所有可判定音符（Bomb 除外）的
                        // (时间, id, 槽位)。
                        if (allowAutoplay &&
                            !ChartExtState.BombIds.Contains(id))
                        {
                            ChartExtState.AutoplayDues.Add(new ChartExtState.AutoplayDue
                            {
                                StartMs = Marshal.ReadInt32(
                                    notesData + i * NativeLayoutCatalog.NoteStride +
                                    NativeLayoutCatalog.Note.StartMs),
                                EndMs = Marshal.ReadInt32(
                                    notesData + i * NativeLayoutCatalog.NoteStride +
                                    NativeLayoutCatalog.Note.EndMs),
                                Type = (int)notes[i]._be,
                                Id = id,
                                Side = side,
                                Position = i,
                                SourceOrder =
                                    ChartExtState.AutoplayDues.Count,
                            });
                        }
                    }
                }
                ChartExtState.AutoplayDues.Sort((a, b) =>
                {
                    int time = a.StartMs.CompareTo(b.StartMs);
                    return time != 0
                        ? time
                        : a.SourceOrder.CompareTo(
                            b.SourceOrder);
                });
                NativeNoteSlotTable.ForceRefresh();
                SkyBlockJudgeRuntime.OnChartCompiled();
                SkyBlockVisualRuntime.OnChartCompiled();
                BombJudgeRuntime.OnChartCompiled(__instance);
            }
            catch (Exception e)
            {
                if (_logged < 40) { _logged++; ChartExtMod.Log.Error("[ChartExt] compile postfix failed: " + e); }
            }
        }

    }

}
