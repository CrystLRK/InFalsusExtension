using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using HarmonyLib;
using UnityEngine;

namespace InFalsusChartExt
{
    /// <summary>
    /// trackmove: the playfield (the track) is displaced and rotated as chart time passes,
    /// with the requested easing; ease 0 resets it to its initial transform. Only the
    /// track's own transforms are written, so other UI is untouched.
    /// </summary>
    internal static class TrackMoveRuntime
    {
        private struct V3
        {
            public float x, y, z;
            public static V3 Add(V3 a, V3 b) => new V3 { x = a.x + b.x, y = a.y + b.y, z = a.z + b.z };
            public static V3 Sub(V3 a, V3 b) => new V3 { x = a.x - b.x, y = a.y - b.y, z = a.z - b.z };
            public static V3 Lerp(V3 a, V3 b, float t) => new V3 { x = a.x + (b.x - a.x) * t, y = a.y + (b.y - a.y) * t, z = a.z + (b.z - a.z) * t };
        }

        private struct Qt
        {
            public float x, y, z, w;
            public static readonly Qt Identity = new Qt { x = 0, y = 0, z = 0, w = 1 };

            public static Qt Mul(Qt a, Qt b) => new Qt
            {
                x = a.w * b.x + a.x * b.w + a.y * b.z - a.z * b.y,
                y = a.w * b.y - a.x * b.z + a.y * b.w + a.z * b.x,
                z = a.w * b.z + a.x * b.y - a.y * b.x + a.z * b.w,
                w = a.w * b.w - a.x * b.x - a.y * b.y - a.z * b.z,
            };

            public static Qt Axis(float ax, float ay, float az, float degrees)
            {
                float half = degrees * 0.5f * 0.017453292f;
                float s = (float)Math.Sin(half);
                return new Qt { x = ax * s, y = ay * s, z = az * s, w = (float)Math.Cos(half) };
            }

            /// <summary>Unity's Quaternion.Euler order: y * x * z.</summary>
            public static Qt Euler(float xDeg, float yDeg, float zDeg) =>
                Mul(Mul(Axis(0, 1, 0, yDeg), Axis(1, 0, 0, xDeg)), Axis(0, 0, 1, zDeg));

            public static Qt Nlerp(Qt a, Qt b, float t)
            {
                float dot = a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;
                float sign = dot < 0 ? -1f : 1f;
                var r = new Qt
                {
                    x = a.x + (b.x * sign - a.x) * t,
                    y = a.y + (b.y * sign - a.y) * t,
                    z = a.z + (b.z * sign - a.z) * t,
                    w = a.w + (b.w * sign - a.w) * t,
                };
                float len = (float)Math.Sqrt(r.x * r.x + r.y * r.y + r.z * r.z + r.w * r.w);
                if (len > 1e-6f) { r.x /= len; r.y /= len; r.z /= len; r.w /= len; }
                return r;
            }
        }

        // sign conventions for the three plane angles (flip here if the in-game test disagrees)
        private const float YawSign = -1f;   // xoz plane, about +Y
        private const float PitchSign = 1f;  // yoz plane, about +X
        private const float RollSign = 1f;   // xoy plane, about +Z

        private static bool _captured;
        private static IntPtr _chartPointer;
        private static V3 _baseMasterPos, _baseNotesPos, _pivot;
        private static Qt _baseMasterRot = Qt.Identity, _baseNotesRot = Qt.Identity;
        private static V3 _offsetPos;
        private static Qt _offsetRot = Qt.Identity;
        private static bool _applied;
        private static int _cursor;
        private static bool _animActive;
        private static double _animStart, _animDuration;
        private static int _animEase;
        private static V3 _animFromPos, _animToPos;
        private static Qt _animFromRot = Qt.Identity, _animToRot = Qt.Identity;
        private static int _errorLogs;

        internal static void Tick(Il2Cppifapp.Game.Scenes.Game.Track track)
        {
            if (!BuildProfile.Supports(NativeCapability.TrackMove))
                return;
            try
            {
                if (track == null || track.Pointer == IntPtr.Zero) return;

                List<ChartExtState.TrackMoveEntry> moves = ChartExtState.TrackMoves;
                if (moves.Count == 0)
                {
                    Il2Cppifapp.Game.LogicalNotePlayer currentPlayer =
                        Il2Cppifapp.Game.LogicalNotePlayer._Qe;
                    Il2Cpp_b._T currentChart = currentPlayer?._ve;
                    if (_captured && currentChart != null &&
                        currentChart.Pointer == _chartPointer)
                        Restore(track);
                    else
                        DiscardCapture();
                    return;
                }

                Il2Cppifapp.Game.LogicalNotePlayer player = Il2Cppifapp.Game.LogicalNotePlayer._Qe;
                if (player == null || player.Pointer == IntPtr.Zero) return;
                Il2Cpp_b._T chart = player._ve;
                if (chart == null || chart.Pointer == IntPtr.Zero)
                {
                    DiscardCapture();
                    return;
                }

                Il2Cpp_E._VD judge = Il2Cppifapp.Game.Scenes.GameScene._DN;
                if (judge == null || judge.Pointer == IntPtr.Zero) return;
                double nowMs = judge._c * 1000.0;

                if (!_captured || _chartPointer != chart.Pointer)
                {
                    Capture(track);
                    _chartPointer = chart.Pointer;
                    _offsetPos = new V3();
                    _offsetRot = Qt.Identity;
                    _applied = false;
                    _cursor = 0;
                    _animActive = false;
                }

                while (_cursor < moves.Count && moves[_cursor].TimeMs <= nowMs)
                {
                    ChartExtState.TrackMoveEntry move = moves[_cursor++];
                    // "Return to initial" is expressed by the six movement/angle parameters
                    // being all zero; the ease and duration parameters still apply, so the
                    // camera animates back smoothly instead of snapping.
                    bool reset = move.X == 0f && move.Y == 0f && move.Z == 0f &&
                                 move.AngXoz == 0f && move.AngYoz == 0f && move.AngXoy == 0f;
                    V3 targetPos;
                    Qt targetRot;
                    if (reset)
                    {
                        targetPos = new V3();
                        targetRot = Qt.Identity;
                    }
                    else
                    {
                        targetPos = V3.Add(_offsetPos, new V3 { x = move.X, y = move.Y, z = move.Z });
                        targetRot = Qt.Mul(_offsetRot,
                            Qt.Euler(move.AngYoz * PitchSign, move.AngXoz * YawSign, move.AngXoy * RollSign));
                    }
                    _animFromPos = _offsetPos;
                    _animToPos = targetPos;
                    _animFromRot = _offsetRot;
                    _animToRot = targetRot;
                    _animStart = move.TimeMs;
                    _animDuration = move.DurationMs;
                    _animEase = move.Ease;
                    _animActive = _animDuration > 0.0;
                    if (!_animActive)
                    {
                        _offsetPos = targetPos;
                        _offsetRot = targetRot;
                    }
                }

                if (_animActive)
                {
                    float t = (float)Math.Clamp((nowMs - _animStart) / _animDuration, 0.0, 1.0);
                    float k = _animEase == 1 ? t * t * t : _animEase == 2 ? 1f - (1f - t) * (1f - t) * (1f - t) : t;
                    _offsetPos = V3.Lerp(_animFromPos, _animToPos, k);
                    _offsetRot = Qt.Nlerp(_animFromRot, _animToRot, k);
                    if (t >= 1f) _animActive = false;
                }

                Apply(track);
            }
            catch (Exception e)
            {
                if (_errorLogs < 20) { _errorLogs++; ChartExtMod.Log.Error("[ChartExt] trackmove tick failed: " + e); }
            }
        }

        internal static void ResetForChartSwitch()
        {
            try
            {
                Il2Cppifapp.Game.Scenes.Game.Track track =
                    Il2Cppifapp.Game.Scenes.Game.Track._a;
                Il2Cppifapp.Game.LogicalNotePlayer player =
                    Il2Cppifapp.Game.LogicalNotePlayer._Qe;
                Il2Cpp_b._T chart = player?._ve;
                if (_captured && track != null &&
                    track.Pointer != IntPtr.Zero &&
                    chart != null && chart.Pointer == _chartPointer)
                    Restore(track);
                else
                    DiscardCapture();
            }
            catch
            {
                DiscardCapture();
            }
        }

        private static void DiscardCapture()
        {
            _captured = false;
            _applied = false;
            _animActive = false;
            _chartPointer = IntPtr.Zero;
            _cursor = 0;
            _offsetPos = new V3();
            _offsetRot = Qt.Identity;
        }

        private static void Capture(Il2Cppifapp.Game.Scenes.Game.Track track)
        {
            Transform master = track.masterScene;
            Transform notes = track.notesContainer;
            _baseMasterPos = Read(master.position);
            _baseMasterRot = Read(master.rotation);
            _baseNotesPos = Read(notes.position);
            _baseNotesRot = Read(notes.rotation);
            _pivot = _baseMasterPos;
            try
            {
                Transform line = master.Find("JUDGEMENT_LINE");
                if (line != null && line.Pointer != IntPtr.Zero) _pivot = Read(line.position);
            }
            catch
            {
                // keep the fallback pivot
            }
            _captured = true;
            _applied = false;
        }

        private static void Restore(Il2Cppifapp.Game.Scenes.Game.Track track)
        {
            try
            {
                if (_applied)
                {
                    Write(track.masterScene, _baseMasterPos, _baseMasterRot);
                    Write(track.notesContainer, _baseNotesPos, _baseNotesRot);
                }
            }
            catch
            {
                // the scene is going away
            }
            _captured = false;
            _applied = false;
            _animActive = false;
            _offsetPos = new V3();
            _offsetRot = Qt.Identity;
        }

        private static void Apply(Il2Cppifapp.Game.Scenes.Game.Track track)
        {
            bool identity = Math.Abs(_offsetPos.x) < 1e-6f && Math.Abs(_offsetPos.y) < 1e-6f && Math.Abs(_offsetPos.z) < 1e-6f
                            && Math.Abs(_offsetRot.x) < 1e-6f && Math.Abs(_offsetRot.y) < 1e-6f
                            && Math.Abs(_offsetRot.z) < 1e-6f && Math.Abs(_offsetRot.w - 1f) < 1e-6f;
            if (identity)
            {
                if (_applied)
                {
                    Write(track.masterScene, _baseMasterPos, _baseMasterRot);
                    Write(track.notesContainer, _baseNotesPos, _baseNotesRot);
                    _applied = false;
                }
                return;
            }

            V3 relMaster = V3.Sub(_baseMasterPos, _pivot);
            V3 relNotes = V3.Sub(_baseNotesPos, _pivot);
            V3 masterPos = V3.Add(V3.Add(_pivot, Rotate(relMaster, _offsetRot)), _offsetPos);
            V3 notesPos = V3.Add(V3.Add(_pivot, Rotate(relNotes, _offsetRot)), _offsetPos);
            Write(track.masterScene, masterPos, Qt.Mul(_offsetRot, _baseMasterRot));
            Write(track.notesContainer, notesPos, Qt.Mul(_offsetRot, _baseNotesRot));
            _applied = true;
        }

        private static V3 Rotate(V3 v, Qt q)
        {
            // q * v * q^-1, expanded
            float x = q.x, y = q.y, z = q.z, w = q.w;
            float ix = w * v.x + y * v.z - z * v.y;
            float iy = w * v.y + z * v.x - x * v.z;
            float iz = w * v.z + x * v.y - y * v.x;
            float iw = -x * v.x - y * v.y - z * v.z;
            return new V3
            {
                x = ix * w + iw * -x + iy * -z - iz * -y,
                y = iy * w + iw * -y + iz * -x - ix * -z,
                z = iz * w + iw * -z + ix * -y - iy * -x,
            };
        }

        private static V3 Read(UnityEngine.Vector3 v) => new V3 { x = v.x, y = v.y, z = v.z };

        private static Qt Read(UnityEngine.Quaternion q) => new Qt { x = q.x, y = q.y, z = q.z, w = q.w };

        private static void Write(Transform t, V3 pos, Qt rot)
        {
            var p = new UnityEngine.Vector3();
            p.x = pos.x; p.y = pos.y; p.z = pos.z;
            var q = new UnityEngine.Quaternion();
            q.x = rot.x; q.y = rot.y; q.z = rot.z; q.w = rot.w;
            t.SetPositionAndRotation(p, q);
        }
    }

    [HarmonyPatch]
    internal static class TrackTickPatch
    {
        [HarmonyPrepare]
        private static bool Prepare() =>
            NativePatchTargetResolver.IsAvailable("trackUpdate");

        [HarmonyTargetMethod]
        private static System.Reflection.MethodBase TargetMethod()
        {
            return NativePatchTargetResolver.ResolveHarmony(
                "trackUpdate");
        }

        [HarmonyPostfix]
        private static void Postfix(Il2Cppifapp.Game.Scenes.Game.Track __instance)
        {
            RegressionDiagnostics.TrackTick(__instance);
            TrackMoveRuntime.Tick(__instance);
            SceneControlRuntime.Tick(__instance);
            SkyColorRuntime.Tick(__instance);
            BackgroundSkin.Tick();
            TimingNoInputSkyVisualRuntime.Tick(__instance);
            VisualNoteRuntime.Tick(__instance);
            TimingGroupRuntime.Restore();
            TimingGroupVisualRuntime.Tick(__instance);
            ShowTextRuntime.EnsureLoaded();
            // JudgeListFilter disabled for now: the game rebuilds the judge list every
            // frame after this postfix, so the strip does not reliably precede the
            // judge tick. The filter will be re-attached at the list's source instead.
            BombSkinRuntime.Tick(__instance);
            AudioOffsetRuntime.Tick();
            HideGroupSkyColorRuntime.Tick(__instance);
            NormalSkyInternalEdgeRuntime.Tick(__instance);
            SkyBlockVisualRuntime.Tick(__instance);
            SkyBlockLineGlowRuntime.Tick(__instance);
        }
    }

    /// <summary>
    /// Removes noinput positions (visual-only bands) from an active-note list
    /// (raw UnorderedListVal
    /// access: +0x10 = int[] array object, +0x18 = count).
    /// </summary>
    internal static class JudgeListFilter
    {
        private static int _errorLogs;

        internal static void Strip(
            Il2CppSystem.Collections.Generic.Dictionary<Il2Cpp_b._CA, Il2Cppifapp.Game.Common.UnorderedListVal<int>> lists,
            SkyBlockPlan blockPlan,
            BombPlan bombPlan)
        {
            try
            {
                bool hasBlocks = blockPlan != null &&
                    blockPlan.JudgeMembers.Count > 0;
                bool hasBombs = bombPlan != null &&
                    bombPlan.NativeMembers.Count > 0;
                if (!hasBlocks && !hasBombs) return;
                if (lists == null || lists.Pointer == IntPtr.Zero) return;
                for (int side = 0; side < NativeLayoutCatalog.SideCount; side++)
                {
                    var key = (Il2Cpp_b._CA)side;
                    if (!lists.ContainsKey(key)) continue;
                    StripList(
                        lists[key], side,
                        blockPlan, bombPlan);
                }
            }
            catch (Exception e)
            {
                if (_errorLogs < 20) { _errorLogs++; ChartExtMod.Log.Error("[ChartExt] list strip failed: " + e); }
            }
        }

        private static void StripList(
            Il2Cppifapp.Game.Common.UnorderedListVal<int> list,
            int side, SkyBlockPlan blockPlan,
            BombPlan bombPlan)
        {
            try
            {
                if (list == null || list.Pointer == IntPtr.Zero) return;
                IntPtr lp = list.Pointer;
                IntPtr arr = Marshal.ReadIntPtr(
                    lp + NativeLayoutCatalog.Runtime.ListItems);
                int count = Marshal.ReadInt32(
                    lp + NativeLayoutCatalog.Runtime.ListCount);
                if (arr == IntPtr.Zero || count <= 0) return;
                int capacity = Marshal.ReadInt32(
                    arr + NativeLayoutCatalog.ArrayLengthOffset);
                if (count > capacity) return;
                IntPtr data = arr + NativeLayoutCatalog.ArrayDataOffset;
                IntPtr basePtr =
                    NativeNoteSlotTable.SlotBase(side);
                int write = 0;
                for (int read = 0; read < count; read++)
                {
                    int position = Marshal.ReadInt32(data + 4 * read);
                    if (basePtr != IntPtr.Zero)
                    {
                        IntPtr note = basePtr +
                            position *
                            NativeLayoutCatalog.NoteStride;
                        long id = Marshal.ReadInt64(
                            note + NativeLayoutCatalog.Note.Id);
                        if ((blockPlan != null &&
                             blockPlan.IsJudgeMember(id)) ||
                            (bombPlan != null &&
                             bombPlan.IsBomb(id)))
                            continue;
                    }
                    if (write != read)
                        Marshal.WriteInt32(data + 4 * write, position);
                    write++;
                }
                if (write != count)
                    Marshal.WriteInt32(
                        lp + NativeLayoutCatalog.Runtime.ListCount, write);
            }
            catch (Exception e)
            {
                if (_errorLogs < 20) { _errorLogs++; ChartExtMod.Log.Error("[ChartExt] list strip failed: " + e); }
            }
        }
    }

    /// <summary>
    /// `_hb` computes the set of currently active notes; the game calls it once with
    /// Track._QnA (spawner list) and once from inside the judge tick with `_VD._REA`
    /// (judge list) between the list rebuild and the judgement loop. Stripping the
    /// noinput positions here - only when the target is not the spawner list - keeps
    /// the area visible but invisible to the judge, so it never produces judgement
    /// effects, the red "left the area" feedback, or encounter events.
    /// </summary>
    [HarmonyPatch]
    internal static class HbJudgeListPatch
    {
        private static int _errorLogs;

        [HarmonyPrepare]
        private static bool Prepare() =>
            NativePatchTargetResolver.IsAvailable(
                "logicalNotePlayerHb");

        [HarmonyTargetMethod]
        private static System.Reflection.MethodBase TargetMethod() =>
            NativePatchTargetResolver.ResolveHarmony(
                "logicalNotePlayerHb");

        /// <summary>`_hb` 的渲染相调用发生在 Track.Update 顶部、渲染循环之前；本帧渲染数据
        /// （含各音符的 `_Ee`/位置）由 `_hb` 内部就地计算并可能被缓存，所以 timinggroup 的
        /// 时间改写必须放在**前缀**（先改写，再让 `_hb` 计算），而不是后置。
        /// 判定相前缀则兜底还原真实时间（防任何异常路径残留）。</summary>
        [HarmonyPrefix]
        private static void Prefix(
            Il2CppSystem.Collections.Generic.Dictionary<Il2Cpp_b._CA, Il2Cppifapp.Game.Common.UnorderedListVal<int>> __3)
        {
            try
            {
                SceneControlRuntime.PrepareForFrame(
                    TimingGroupRuntime.CurrentMs());
                if (!TimingGroupRuntime.Active) return;
                if (__3 == null || __3.Pointer == IntPtr.Zero) return;
                Il2Cppifapp.Game.Scenes.Game.Track track = Il2Cppifapp.Game.Scenes.Game.Track._a;
                if (track == null || track.Pointer == IntPtr.Zero) return;
                var spawn = track._QnA;
                if (spawn != null && spawn.Pointer == __3.Pointer)
                {
                    TimingGroupRuntime.ApplyWarp();
                    TimingGroupVisualRuntime.ApplyFlip(true);
                }
                else
                {
                    TimingGroupRuntime.Restore();
                    TimingGroupVisualRuntime.ApplyFlip(false);
                }
            }
            catch
            {
                // 时间改写失败按原时间渲染
            }
        }

        [HarmonyPostfix]
        private static void Postfix(
            Il2CppSystem.Collections.Generic.Dictionary<Il2Cpp_b._CA, Il2Cppifapp.Game.Common.UnorderedListVal<int>> __3)
        {
            try
            {
                if (__3 == null || __3.Pointer == IntPtr.Zero) return;
                BombPlan bombPlan =
                    ChartRuntimeContext.IsActive
                        ? ChartRuntimeContext.Current?.Bombs
                        : null;
                bool bombCapability =
                    BuildProfile.Supports(NativeCapability.BombVisual) ||
                    BuildProfile.Supports(NativeCapability.BombJudge);
                bool wantBomb = bombCapability && bombPlan != null &&
                    bombPlan.NativeMembers.Count > 0;
                bool wantTiming =
                    BuildProfile.Supports(NativeCapability.TimingGroup) &&
                    TimingGroupRuntime.Active;
                bool wantSceneControl =
                    BuildProfile.Supports(NativeCapability.SceneControl) &&
                    ChartExtState.SceneControls.Count > 0;
                SkyBlockPlan blockPlan =
                    ChartRuntimeContext.IsActive
                        ? ChartRuntimeContext.Current?.SkyBlocks
                        : null;
                bool wantSkyBlockVisual =
                    BuildProfile.Supports(NativeCapability.SkyBlock) &&
                    blockPlan != null &&
                    blockPlan.VisualMembers.Count > 0;
                bool wantSkyBlockJudge =
                    BuildProfile.Supports(NativeCapability.SkyBlock) &&
                    blockPlan != null &&
                    blockPlan.JudgeMembers.Count > 0;
                if (!wantBomb && !wantTiming &&
                    !wantSceneControl && !wantSkyBlockVisual &&
                    !wantSkyBlockJudge) return;
                Il2Cppifapp.Game.Scenes.Game.Track track = Il2Cppifapp.Game.Scenes.Game.Track._a;
                if (track == null || track.Pointer == IntPtr.Zero) return;
                var spawn = track._QnA;
                bool spawnPhase = spawn != null && spawn.Pointer == __3.Pointer;
                // `_hb` 已用真实 note start 生成完本帧列表。现在再写 timinggroup
                // 的渲染时间，既保留原版生成窗口，又让后面的 Track 渲染循环
                // 使用变速后的位置/长度。
                if (spawnPhase && wantTiming)
                {
                    TimingGroupNativeFlickRuntime.InjectSpawnList(__3);
                    TimingGroupRuntime.ApplyVisualFields();
                }
                if (spawnPhase)
                {
                    SceneControlRuntime.FilterSpawnList(__3);
                }
                if (wantBomb && spawnPhase)
                {
                    BombSpawnWatch.Update(__3);
                    BombRenderListFilter.Strip(__3, bombPlan);
                }
                // timinggroup 的两阶段处理：
                // Prefix 只写 `_IA`，让 `_hb` 继续用真实 note start 判断生成窗口；
                // 上面的 ApplyVisualFields() 再写本帧渲染用的 be/end/_Ee。

                if (!wantSkyBlockVisual &&
                    !wantSkyBlockJudge &&
                    !wantBomb)
                    return;
                if (spawnPhase)
                {
                    if (wantSkyBlockVisual)
                        SpawnOrderFilter.StripBlockVisuals(__3);
                    return;
                }
                if (wantSkyBlockJudge || wantBomb)
                    JudgeListFilter.Strip(
                        __3, blockPlan, bombPlan);
            }
            catch (Exception e)
            {
                if (_errorLogs < 20) { _errorLogs++; ChartExtMod.Log.Error("[ChartExt] hb strip failed: " + e); }
            }
        }
    }

    /// <summary>
    /// 每个 spawn 相把生成列表内容（side+pos）记录到 ChartExtState.SpawnKeysNow：
    /// 自绘 bomb 只画"这帧游戏真正会渲染"的音符，避免开局窗口尚未展开时把还没上榜的
    /// 音符按陈旧 `_Ee` 画到判定线附近（用户看到的"开头冒出几个"瞬态）。
    /// </summary>
    internal static class BombSpawnWatch
    {
        private static int _errorLogs;

        internal static void Update(
            Il2CppSystem.Collections.Generic.Dictionary<Il2Cpp_b._CA, Il2Cppifapp.Game.Common.UnorderedListVal<int>> lists)
        {
            ChartExtState.SpawnKeysNow.Clear();
            try
            {
                if (lists == null || lists.Pointer == IntPtr.Zero) return;
                for (int side = 0; side < NativeLayoutCatalog.SideCount; side++)
                {
                    var key = (Il2Cpp_b._CA)side;
                    if (!lists.ContainsKey(key)) continue;
                    var list = lists[key];
                    if (list == null || list.Pointer == IntPtr.Zero) continue;
                    IntPtr lp = list.Pointer;
                    IntPtr arr = Marshal.ReadIntPtr(
                        lp + NativeLayoutCatalog.Runtime.ListItems);
                    int count = Marshal.ReadInt32(
                        lp + NativeLayoutCatalog.Runtime.ListCount);
                    if (arr == IntPtr.Zero || count <= 0 || count > NumericLimitPatch.SideNoteCapacity) continue;
                    IntPtr data = arr + NativeLayoutCatalog.ArrayDataOffset;
                    for (int i = 0; i < count; i++)
                    {
                        int pos = Marshal.ReadInt32(data + 4 * i);
                        ChartExtState.SpawnKeysNow.Add(((long)side << 32) | (uint)pos);
                    }
                }
            }
            catch (Exception e)
            {
                if (_errorLogs < 20) { _errorLogs++; ChartExtMod.Log.Error("[ChartExt] spawn watch failed: " + e); }
            }
        }
    }

    /// <summary>
    /// Removes ordinary Bomb ids from the native render list. The independent
    /// Bomb visual runtime owns those objects.
    /// </summary>
    internal static class BombRenderListFilter
    {
        private static int _errorLogs;

        internal static void Strip(
            Il2CppSystem.Collections.Generic.Dictionary<
                Il2Cpp_b._CA,
                Il2Cppifapp.Game.Common.UnorderedListVal<int>> lists,
            BombPlan plan)
        {
            try
            {
                if (plan == null ||
                    plan.NativeMembers.Count == 0 ||
                    lists == null ||
                    lists.Pointer == IntPtr.Zero)
                    return;
                for (int side = 0;
                     side < NativeLayoutCatalog.SideCount;
                     side++)
                {
                    var key = (Il2Cpp_b._CA)side;
                    if (!lists.ContainsKey(key))
                        continue;
                    StripList(lists[key], side, plan);
                }
            }
            catch (Exception e)
            {
                if (_errorLogs++ < 10)
                    ChartExtMod.Log.Error(
                        "[ChartExt] bomb render filter failed: " +
                        e);
            }
        }

        private static void StripList(
            Il2Cppifapp.Game.Common.UnorderedListVal<int> list,
            int side,
            BombPlan plan)
        {
            if (list == null || list.Pointer == IntPtr.Zero)
                return;
            IntPtr lp = list.Pointer;
            IntPtr arr = Marshal.ReadIntPtr(
                lp + NativeLayoutCatalog.Runtime.ListItems);
            int count = Marshal.ReadInt32(
                lp + NativeLayoutCatalog.Runtime.ListCount);
            if (arr == IntPtr.Zero || count <= 0)
                return;
            int capacity = Marshal.ReadInt32(
                arr + NativeLayoutCatalog.ArrayLengthOffset);
            if (count > capacity)
                return;
            IntPtr data =
                arr + NativeLayoutCatalog.ArrayDataOffset;
            IntPtr basePtr = NativeNoteSlotTable.SlotBase(side);
            if (basePtr == IntPtr.Zero)
                return;
            int write = 0;
            for (int read = 0; read < count; read++)
            {
                int position =
                    Marshal.ReadInt32(data + 4 * read);
                IntPtr note = basePtr +
                    position * NativeLayoutCatalog.NoteStride;
                long id = Marshal.ReadInt64(
                    note + NativeLayoutCatalog.Note.Id);
                if (plan.IsBomb(id))
                    continue;
                if (write != read)
                    Marshal.WriteInt32(
                        data + 4 * write, position);
                write++;
            }
            if (write != count)
                Marshal.WriteInt32(
                    lp + NativeLayoutCatalog.Runtime.ListCount,
                    write);
        }
    }

    internal static class SpawnOrderFilter
    {
        private const int SkySide = 4;
        private static int _errorLogs;

        internal static void StripBlockVisuals(
            Il2CppSystem.Collections.Generic.Dictionary<Il2Cpp_b._CA, Il2Cppifapp.Game.Common.UnorderedListVal<int>> lists)
        {
            try
            {
                if (lists == null || lists.Pointer == IntPtr.Zero)
                    return;
                var key = (Il2Cpp_b._CA)SkySide;
                if (!lists.ContainsKey(key)) return;
                var list = lists[key];
                if (list == null || list.Pointer == IntPtr.Zero)
                    return;
                IntPtr lp = list.Pointer;
                IntPtr arr = Marshal.ReadIntPtr(
                    lp + NativeLayoutCatalog.Runtime.ListItems);
                int count = Marshal.ReadInt32(
                    lp + NativeLayoutCatalog.Runtime.ListCount);
                if (arr == IntPtr.Zero || count <= 0) return;
                int arrayLength = Marshal.ReadInt32(
                    arr + NativeLayoutCatalog.ArrayLengthOffset);
                if (count > arrayLength) return;
                IntPtr data =
                    arr + NativeLayoutCatalog.ArrayDataOffset;

                SkyBlockPlan blockPlan =
                    ChartRuntimeContext.IsActive
                        ? ChartRuntimeContext.Current?.SkyBlocks
                        : null;
                if (blockPlan == null) return;
                IntPtr basePtr =
                    NativeNoteSlotTable.SlotBase(SkySide);
                if (basePtr == IntPtr.Zero) return;
                int write = 0;
                for (int read = 0; read < count; read++)
                {
                    int position = Marshal.ReadInt32(
                        data + 4 * read);
                    IntPtr note = basePtr +
                        position * NativeLayoutCatalog.NoteStride;
                    long id = Marshal.ReadInt64(
                        note + NativeLayoutCatalog.Note.Id);
                    if (blockPlan.IsVisualMember(id))
                    {
                        HideNativeTarget(id);
                        continue;
                    }
                    if (write != read)
                        Marshal.WriteInt32(
                            data + 4 * write, position);
                    write++;
                }
                if (write != count)
                    Marshal.WriteInt32(
                        lp + NativeLayoutCatalog.Runtime.ListCount,
                        write);
            }
            catch (Exception e)
            {
                if (_errorLogs < 20)
                {
                    _errorLogs++;
                    ChartExtMod.Log.Error(
                        "[ChartExt] skyblock spawn strip failed: " +
                        e);
                }
            }
        }

        internal static void HideNativeTarget(long id)
        {
            try
            {
                Il2Cppifapp.Game.Scenes.Game.Track track =
                    Il2Cppifapp.Game.Scenes.Game.Track._a;
                if (track == null || track.Pointer == IntPtr.Zero)
                    return;
                HideNativeTarget(track._UMA, id);
                HideNativeTarget(track._vMA, id);
            }
            catch { }
        }

        private static void HideNativeTarget(
            Il2Cpp_k._JG<long,
                Il2Cppifapp.Game.Scenes.Game.SkySafeArea> pool,
            long id)
        {
            try
            {
                if (pool == null || pool.Pointer == IntPtr.Zero)
                    return;
                HideNativeTarget(pool._QXA, id);
                HideNativeTarget(pool._rXA, id);
            }
            catch { }
        }

        private static void HideNativeTarget(
            Il2CppSystem.Collections.Generic.Dictionary<
                long,
                Il2CppSystem.ValueTuple<
                    Il2Cpp_k._Sg,
                    UnityEngine.GameObject,
                    UnityEngine.Transform,
                    Il2Cppifapp.Game.Scenes.Game.SkySafeArea>>
                entries,
            long id)
        {
            try
            {
                if (entries == null ||
                    entries.Pointer == IntPtr.Zero ||
                    !entries.ContainsKey(id))
                    return;
                UnityEngine.GameObject gameObject =
                    entries[id].Item2;
                if (gameObject != null &&
                    gameObject.Pointer != IntPtr.Zero &&
                    gameObject.activeSelf)
                    gameObject.SetActive(false);
            }
            catch { }
        }

    }

    /// <summary>
    /// Track._BBA is the sky-ghost-cursor update called directly from the mouse
    /// input handler (GameScene._FwA -> _VD._Qz). During a scene transition the
    /// static Track instance can still be alive while its skyGhostCursor has
    /// already been destroyed. Calling Component.get_transform on that stale
    /// object crashed inside IL2CPP Class::Init (GameAssembly +0x3690F2).
    /// Skip the update only while the native Track or its cursor is invalid.
    /// </summary>
    [HarmonyPatch]
    internal static class TrackSkyGhostGuardPatch
    {
        [HarmonyPrepare]
        private static bool Prepare() =>
            NativePatchTargetResolver.IsAvailable(
                "trackSkyGhost");

        [HarmonyTargetMethod]
        private static System.Reflection.MethodBase TargetMethod() =>
            NativePatchTargetResolver.ResolveHarmony(
                "trackSkyGhost");

        [HarmonyPrefix]
        private static bool Prefix(Il2Cppifapp.Game.Scenes.Game.Track __instance)
        {
            if (!BuildProfile.SupportsNativeRead)
                return true;
            if (__instance == null || __instance.Pointer == IntPtr.Zero)
                return false;
            try
            {
                // UnityEngine.Object.m_CachedPtr is at +0x10. A destroyed object
                // keeps its managed wrapper but clears this native pointer.
                if (Marshal.ReadIntPtr(
                        __instance.Pointer +
                        NativeLayoutCatalog.Runtime.CachedPointer) == IntPtr.Zero)
                    return false;
                IntPtr cursor = Marshal.ReadIntPtr(
                    __instance.Pointer +
                    NativeLayoutCatalog.Track.SkyGhostCursor);
                if (cursor == IntPtr.Zero)
                    return false;
                if (Marshal.ReadIntPtr(
                        cursor + NativeLayoutCatalog.Runtime.CachedPointer) ==
                    IntPtr.Zero)
                    return false;
            }
            catch
            {
                return false;
            }
            return true;
        }
    }
}
