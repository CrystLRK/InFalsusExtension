using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace InFalsusChartExt
{
    /// <summary>
    /// Experimental engine-level input simulation. It writes the same
    /// per-record contact states and sky cursor that native input produces,
    /// then lets the original _pz finalize judgements. It never writes a
    /// judgement record and must not run together with the record rewrite.
    /// </summary>
    internal static class AutoplayEngineInput
    {
        private const int LaneCount = 6;
        private const int TapHoldMs = 50;
        private const int HoldSlackMs = 40;
        private const double CursorSpeed = 3.0;
        private const double FlickStepCap = 0.02;
        private const double FlickApproachMs = 600.0;
        private const double FlickGroupWindowMs = 16.0;
        private const double FlickEarlyWindowMs = 80.0;
        private const double FlickGestureStepCap = 0.25;
        private const double FlickMultiStepCap = 2.0;
        private const double FlickReturnStepCap = 0.04;
        private const double GroundEarlyWindowMs = 20.0;
        private const double SkyLockMinLeadMs = 200.0;
        private const double SkyLockMaxLeadMs = 300.0;
        private const int MaxFlickGroup = 16;
        private const int FlickGestureTargets = 5;

        private static readonly Dictionary<long, ActiveInput> Active =
            new Dictionary<long, ActiveInput>();
        private static readonly HashSet<long> SwipedFlicks =
            new HashSet<long>();
        private static readonly long[] FlickGroupIds =
            new long[MaxFlickGroup];
        private static readonly double[] FlickGroupCenters =
            new double[MaxFlickGroup];
        private static readonly double[] FlickGroupHalves =
            new double[MaxFlickGroup];
        private static readonly double[] FlickGroupSigns =
            new double[MaxFlickGroup];
        private static readonly double[] FlickGestureWaypoints =
            new double[FlickGestureTargets];
        private static int _flickGroupCount;
        private static int _flickGestureCount;
        private static int _flickGestureIndex;
        private static int _flickGestureHold;
        private static double _flickGestureStepCap =
            FlickGestureStepCap;
        private static bool _flickGestureActive;
        private static int _lastGroundLane = -1;
        private static readonly List<long> Scratch =
            new List<long>();
        private static readonly HashSet<long> TimingLogged =
            new HashSet<long>();
        private static int _driveCursor;
        private static bool _failed;
        private static int _flickLogs;
        private static bool _cursorMotionLogged;
        private static double _lastCursorMoveTime = -1.0;
        private static int _cursorInjectedFrame = -1;
        private const int MaxActiveSkies = 16;
        private static readonly double[] ActiveSkyCenters =
            new double[MaxActiveSkies];
        private static readonly double[] ActiveSkyHalves =
            new double[MaxActiveSkies];
        private static readonly double[] ActiveSkyLeads =
            new double[MaxActiveSkies];
        private static int _activeSkyCount;
        private static Il2Cppifapp.Game.Scenes.Game.Track
            _visualTrack;
        private static bool _visualSyncDisabled;
        private static bool _visualSyncErrorLogged;

        private static bool _enabled;

        internal static bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value)
                    return;
                _enabled = value;
                if (!_enabled)
                    ReleaseAll();
            }
        }

        internal static bool HasActed { get; private set; }

        internal static bool Failed => _failed;

        internal static void ResetForChartSwitch()
        {
            ReleaseAll();
            SwipedFlicks.Clear();
            TimingLogged.Clear();
            _failed = false;
            _flickLogs = 0;
            _cursorMotionLogged = false;
            HasActed = false;
            _driveCursor = 0;
            _lastCursorMoveTime = -1.0;
            _cursorInjectedFrame = -1;
            _visualTrack = null;
            _visualSyncDisabled = false;
            _visualSyncErrorLogged = false;
            _flickGroupCount = 0;
            _flickGestureCount = 0;
            _flickGestureIndex = 0;
            _flickGestureHold = 0;
            _flickGestureStepCap = FlickGestureStepCap;
            _flickGestureActive = false;
            _lastGroundLane = -1;
        }

        internal static void ReleaseAll()
        {
            foreach (ActiveInput input in Active.Values)
            {
                try
                {
                    ClearIfGround(input, false);
                }
                catch
                {
                }
            }
            Active.Clear();
            _cursorInjectedFrame = -1;
        }

        internal static void Tick(double nowMs, bool judgePass)
        {
            if (!Enabled || _failed)
                return;
            IntPtr judge = JudgePointer();
            if (judge == IntPtr.Zero)
                return;
            try
            {
                ReleaseExpired(nowMs);
                List<ChartExtState.AutoplayDue> dues =
                    ChartExtState.AutoplayDues;
                if (dues.Count == 0)
                    return;
                while (_driveCursor < dues.Count &&
                       dues[_driveCursor].StartMs <
                       nowMs - 5000.0)
                    _driveCursor++;
                if (!judgePass &&
                    _cursorInjectedFrame !=
                    UnityEngine.Time.frameCount)
                {
                    _cursorInjectedFrame =
                        UnityEngine.Time.frameCount;
                    UpdateCursor(judge, nowMs);
                }
                for (int i = _driveCursor; i < dues.Count; i++)
                {
                    ChartExtState.AutoplayDue due = dues[i];
                    if (due.StartMs > nowMs + 150.0)
                        break;
                    if (!ShouldDrive(due))
                        continue;
                    Drive(judge, due, nowMs);
                }
                foreach (ActiveInput input in Active.Values)
                {
                    if (input.Due.StartMs >=
                        nowMs - 5000.0)
                        continue;
                    Drive(judge, input.Due, nowMs);
                }
            }
            catch (Exception exception)
            {
                Fail("input tick", exception);
            }
        }

        internal static void TickCursorOnly(double nowMs)
        {
            if (_failed)
                return;
            IntPtr judge = JudgePointer();
            if (judge == IntPtr.Zero)
                return;
            int frame = UnityEngine.Time.frameCount;
            if (_cursorInjectedFrame == frame)
                return;
            _cursorInjectedFrame = frame;
            UpdateCursor(judge, nowMs);
        }

        private static bool ShouldDrive(
            ChartExtState.AutoplayDue due)
        {
            if (SkyBlockJudgeRuntime.IsManagedMember(due.Id))
                return false;
            if (!AutoplayRuntime.GroundOnly)
                return true;
            return due.Type == 1 || due.Type == 2;
        }

        private static void Drive(
            IntPtr judge,
            ChartExtState.AutoplayDue due,
            double nowMs)
        {
            if (due.Type < 1 || due.Type > 3)
                return;
            ActiveInput input = EnsureActive(due);
            IntPtr note = NotePointer(due);
            if (note == IntPtr.Zero)
                return;

            input.IsGround = true;
            bool press = !input.Pressed;
            if (press &&
                !TryPressGroundWhenDue(due, note))
                return;
            if (press &&
                !ChooseGroundLane(input, note))
                return;
            if (press)
            {
                input.PressFrame = UnityEngine.Time.frameCount;
                input.ReleaseAtMs = nowMs +
                    (due.Type == 2
                        ? (due.EndMs - due.StartMs) +
                          HoldSlackMs
                        : TapHoldMs);
            }
            bool keepEdge =
                input.PressFrame == UnityEngine.Time.frameCount;
            if (!WriteLanes(
                    judge, input.Lane, press, keepEdge, nowMs))
                return;
            input.Pressed = true;
            HasActed = true;
        }

        private static ActiveInput EnsureActive(
            ChartExtState.AutoplayDue due)
        {
            if (Active.TryGetValue(due.Id, out ActiveInput input))
                return input;
            input = new ActiveInput
            {
                Due = due,
                ReleaseAtMs =
                    due.Type == 1 || due.Type == 2
                        ? double.MaxValue
                        : due.Type == 4
                            ? due.EndMs + 120.0
                            : due.EndMs + HoldSlackMs,
            };
            Active[due.Id] = input;
            return input;
        }

        private static void UpdateCursor(
            IntPtr judge,
            double nowMs)
        {
            double now = UnityEngine.Time.time;
            double dt = _lastCursorMoveTime < 0.0
                ? 1.0 / 60.0
                : now - _lastCursorMoveTime;
            _lastCursorMoveTime = now;
            if (dt < 0.0)
                dt = 0.0;
            if (dt > 0.1)
                dt = 0.1;

            double current = ReadFloat(
                judge, NativeLayoutCatalog.SkyJudge.CursorX);
            if (!double.IsFinite(current))
                current = 0.5;

            if (_flickGestureActive)
            {
                UpdateFlickGesture(judge, current, dt);
                return;
            }

            List<ChartExtState.AutoplayDue> dues =
                ChartExtState.AutoplayDues;
            _activeSkyCount = 0;
            _flickGroupCount = 0;
            double flickGroupStartMs = double.NaN;
            bool hasNearSky = false;
            double nearSkyLeadMs = double.MaxValue;
            double nearSkyCenter = 0.0;

            for (int i = _driveCursor; i < dues.Count; i++)
            {
                ChartExtState.AutoplayDue due = dues[i];
                if (due.StartMs > nowMs + FlickApproachMs)
                    break;
                bool managedSky =
                    due.Type == 5 &&
                    SkyBlockJudgeRuntime.IsManagedMember(due.Id) &&
                    !AutoplayRuntime.GroundOnly;
                if (!ShouldDrive(due) && !managedSky)
                    continue;
                IntPtr note = NotePointer(due);
                if (note == IntPtr.Zero && !managedSky)
                    continue;
                if (due.Type == 4)
                {
                    if (SwipedFlicks.Contains(due.Id))
                        continue;
                    double runtimeLead = ReadRuntimeLead(note);
                    if (!double.IsFinite(runtimeLead) ||
                        runtimeLead < -200.0)
                    {
                        SwipedFlicks.Add(due.Id);
                        continue;
                    }
                    if (runtimeLead < -100.0)
                    {
                        SwipedFlicks.Add(due.Id);
                        continue;
                    }
                    if (runtimeLead > FlickEarlyWindowMs)
                        continue;
                    if (!TryGetFlickRange(
                            note,
                            out double flickCenter,
                            out double flickSign,
                            out _,
                            out double flickHalf))
                        continue;
                    if (_flickGroupCount == 0)
                        flickGroupStartMs = due.StartMs;
                    else if (Math.Abs(
                                 due.StartMs -
                                 flickGroupStartMs) >
                             FlickGroupWindowMs)
                        continue;
                    if (_flickGroupCount >= MaxFlickGroup)
                        continue;
                    FlickGroupIds[_flickGroupCount] = due.Id;
                    FlickGroupCenters[_flickGroupCount] =
                        flickCenter;
                    FlickGroupHalves[_flickGroupCount] =
                        flickHalf;
                    FlickGroupSigns[_flickGroupCount] =
                        flickSign;
                    _flickGroupCount++;
                }
                else if (due.Type == 5)
                {
                    if (managedSky)
                    {
                        if (_activeSkyCount <
                                MaxActiveSkies &&
                            TryGetManagedSkyRange(
                                due.Id,
                                nowMs,
                                out double skyX,
                                out double skyHalf,
                                out double skyLeadMs))
                        {
                            if (skyLeadMs <= 0.0)
                            {
                                ActiveSkyCenters[_activeSkyCount] =
                                    skyX;
                                ActiveSkyHalves[_activeSkyCount] =
                                    skyHalf;
                                ActiveSkyLeads[_activeSkyCount] =
                                    Math.Max(0.0, -skyLeadMs);
                                _activeSkyCount++;
                            }
                            else if (skyLeadMs <=
                                         SkyLockMaxLeadMs &&
                                     skyLeadMs < nearSkyLeadMs)
                            {
                                hasNearSky = true;
                                nearSkyLeadMs = skyLeadMs;
                                nearSkyCenter = skyX;
                            }
                        }
                    }
                    else if (nowMs >= due.StartMs &&
                             nowMs <= due.EndMs + 30.0 &&
                             _activeSkyCount < MaxActiveSkies &&
                             TryGetBarRange(
                                 note,
                                 nowMs,
                                 out double activeX,
                                 out double activeHalf))
                    {
                        ActiveSkyCenters[_activeSkyCount] = activeX;
                        ActiveSkyHalves[_activeSkyCount] =
                            activeHalf;
                        ActiveSkyLeads[_activeSkyCount] =
                            nowMs - due.StartMs;
                        _activeSkyCount++;
                    }
                    else if (due.StartMs > nowMs &&
                             due.StartMs - nowMs <=
                                 SkyLockMaxLeadMs &&
                             due.StartMs - nowMs <
                                 nearSkyLeadMs &&
                             TryGetBarRange(
                                 note,
                                 due.StartMs,
                                 out double nextX,
                                 out _))
                    {
                        hasNearSky = true;
                        nearSkyLeadMs = due.StartMs - nowMs;
                        nearSkyCenter = nextX;
                    }
                }
            }

            bool hasSky = _activeSkyCount > 0;
            double skyCenter = double.NaN;
            if (hasSky)
            {
                double left = double.NegativeInfinity;
                double right = double.PositiveInfinity;
                double fallbackCenter = 0.0;
                double fallbackAbs = double.MaxValue;
                for (int i = 0; i < _activeSkyCount; i++)
                {
                    double center = ActiveSkyCenters[i];
                    double half = ActiveSkyHalves[i];
                    left = Math.Max(left, center - half);
                    right = Math.Min(right, center + half);
                    double abs = Math.Abs(ActiveSkyLeads[i]);
                    if (abs < fallbackAbs)
                    {
                        fallbackAbs = abs;
                        fallbackCenter = center;
                    }
                }
                skyCenter = left <= right
                    ? (left + right) * 0.5
                    : fallbackCenter;
                if (!double.IsFinite(skyCenter))
                    hasSky = false;
            }

            if (_flickGroupCount > 0)
            {
                StartFlickGesture(
                    current,
                    hasSky,
                    skyCenter);
                UpdateFlickGesture(judge, current, dt);
                return;
            }

            double target = current;
            if (hasSky)
            {
                TeleportCursor(judge, skyCenter);
                return;
            }
            else if (hasNearSky)
            {
                double lead = Math.Min(
                    SkyLockMaxLeadMs,
                    Math.Max(
                        SkyLockMinLeadMs,
                        Math.Abs(nearSkyCenter - current) /
                            CursorSpeed * 1000.0));
                if (nearSkyLeadMs <= lead)
                {
                    TeleportCursor(judge, nearSkyCenter);
                    return;
                }
            }
            MoveCursor(judge, current, target, false, dt);
        }

        internal static void TeleportCursor(
            IntPtr judge,
            double target)
        {
            if (judge == IntPtr.Zero ||
                !double.IsFinite(target))
                return;
            float value = (float)Math.Max(
                0.0, Math.Min(1.0, target));
            int bits = BitConverter.SingleToInt32Bits(value);
            Marshal.WriteInt32(
                judge + NativeLayoutCatalog.SkyJudge.CursorRaw,
                bits);
            Marshal.WriteInt32(
                judge + NativeLayoutCatalog.SkyJudge.CursorX,
                bits);
            SyncVisualCursor(judge);
            HasActed = true;
            if (!_cursorMotionLogged)
            {
                _cursorMotionLogged = true;
                ChartExtMod.Log.Msg(
                    "[AutoplayInput] input event " +
                    "injection active");
            }
        }

        private static void StartFlickGesture(
            double current,
            bool hasSky,
            double skyCenter)
        {
            double left = double.NegativeInfinity;
            double right = double.PositiveInfinity;
            double minCenter = double.PositiveInfinity;
            double maxCenter = double.NegativeInfinity;
            bool hasLeft = false;
            bool hasRight = false;
            for (int i = 0; i < _flickGroupCount; i++)
            {
                double center = FlickGroupCenters[i];
                double half = FlickGroupHalves[i];
                double sign = FlickGroupSigns[i];
                left = Math.Max(left, center - half);
                right = Math.Min(right, center + half);
                minCenter = Math.Min(minCenter, center);
                maxCenter = Math.Max(maxCenter, center);
                if (sign < 0.0)
                    hasLeft = true;
                else if (sign > 0.0)
                    hasRight = true;
            }

            _flickGestureCount = 0;
            _flickGestureStepCap =
                _flickGroupCount > 1
                    ? FlickMultiStepCap
                    : FlickGestureStepCap;
            double skyLeft = 0.0;
            double skyRight = 1.0;
            bool hasSkyBounds =
                hasSky &&
                TryGetActiveSkyBounds(
                    out skyLeft,
                    out skyRight);
            void AddWaypoint(double value)
            {
                if (_flickGestureCount >= FlickGestureTargets)
                    return;
                value = hasSkyBounds
                    ? Math.Max(
                        skyLeft,
                        Math.Min(skyRight, value))
                    : Math.Max(
                        0.0,
                        Math.Min(1.0, value));
                FlickGestureWaypoints[_flickGestureCount++] =
                    value;
            }

            if (left <= right)
            {
                if (hasLeft && !hasRight)
                {
                    AddWaypoint(right);
                    AddWaypoint(left);
                }
                else if (hasRight && !hasLeft)
                {
                    AddWaypoint(left);
                    AddWaypoint(right);
                }
                else
                {
                    AddWaypoint(right);
                    AddWaypoint(left);
                    AddWaypoint(right);
                }
            }
            else if (hasLeft && !hasRight)
            {
                AddWaypoint(maxCenter);
                AddWaypoint(minCenter);
            }
            else if (hasRight && !hasLeft)
            {
                AddWaypoint(minCenter);
                AddWaypoint(maxCenter);
            }
            else
            {
                AddWaypoint(maxCenter);
                AddWaypoint(minCenter);
                AddWaypoint(maxCenter);
            }
            bool needReset =
                hasSky &&
                double.IsFinite(skyCenter) &&
                IsInsideActiveSky(current) &&
                _flickGestureCount > 0 &&
                !IsInsideActiveSky(
                    FlickGestureWaypoints[
                        _flickGestureCount - 1]);
            if (needReset)
                AddWaypoint(skyCenter);

            _flickGestureIndex = 0;
            _flickGestureHold = 0;
            _flickGestureActive = true;
            for (int i = 0; i < _flickGroupCount; i++)
                SwipedFlicks.Add(FlickGroupIds[i]);
            if (_flickLogs++ < 20)
            {
                ChartExtMod.Log.Msg(
                    "[AutoplayInput] flick group " +
                    $"n={_flickGroupCount} " +
                    $"targets={_flickGestureCount} " +
                    $"L={(hasLeft ? 1 : 0)} " +
                    $"R={(hasRight ? 1 : 0)} " +
                    $"reset={(needReset ? 1 : 0)}");
            }
        }

        private static void UpdateFlickGesture(
            IntPtr judge,
            double current,
            double dt)
        {
            for (int guard = 0; guard < 4; guard++)
            {
                if (!_flickGestureActive)
                    return;
                if (_flickGestureIndex >= _flickGestureCount)
                {
                    _flickGestureActive = false;
                    return;
                }
                double target =
                    FlickGestureWaypoints[_flickGestureIndex];
                if (Math.Abs(target - current) > 0.0015)
                {
                    MoveCursor(
                        judge,
                        current,
                        target,
                        true,
                        dt,
                        _flickGestureIndex ==
                            _flickGestureCount - 1
                            ? FlickReturnStepCap
                            : _flickGestureStepCap);
                    return;
                }
                if (_flickGestureHold > 0)
                {
                    _flickGestureHold--;
                    return;
                }
                if (_flickGestureIndex ==
                    _flickGestureCount - 1)
                {
                    _flickGestureActive = false;
                    return;
                }
                _flickGestureIndex++;
                _flickGestureHold =
                    _flickGroupCount > 1 ? 0 : 1;
            }
        }

        private static void MoveCursor(
            IntPtr judge,
            double current,
            double target,
            bool gesture,
            double dt,
            double gestureStepCap = FlickGestureStepCap)
        {
            if (!double.IsFinite(current) ||
                !double.IsFinite(target))
                return;
            double distance = Math.Abs(target - current);
            if (distance < 0.001)
                return;
            double catchUpSpeed = Math.Max(
                CursorSpeed,
                distance * 24.0);
            double maxStep = gesture
                ? gestureStepCap
                : Math.Min(catchUpSpeed * dt, 0.12);
            if (maxStep <= 0.0)
                return;
            double delta = target - current;
            if (delta > maxStep)
                delta = maxStep;
            if (delta < -maxStep)
                delta = -maxStep;
            if (Math.Abs(delta) < 0.0005)
                return;
            if (!InjectMouseDelta(delta))
            {
                Fail(
                    "cursor move",
                    new InvalidOperationException(
                        "mouse delta injection failed"));
                return;
            }
            HasActed = true;
            if (!_cursorMotionLogged)
            {
                _cursorMotionLogged = true;
                ChartExtMod.Log.Msg(
                    "[AutoplayInput] input event " +
                    "injection active");
            }
        }

        private static bool InjectMouseDelta(double deltaX)
        {
            Il2Cppifapp.Game.Scenes.GameScene scene =
                Il2Cppifapp.Game.Scenes.GameScene._xn;
            if (scene is null ||
                scene.Pointer == IntPtr.Zero)
                return false;
            Il2Cpp_E._VD judge =
                Il2Cppifapp.Game.Scenes.GameScene._DN;
            if (judge is null ||
                judge.Pointer == IntPtr.Zero)
                return false;
            try
            {
                double sensitivity = ReadDouble(
                    judge.Pointer, 184);
                if (!double.IsFinite(sensitivity) ||
                    Math.Abs(sensitivity) < 0.0001)
                    sensitivity = 1.0;
                var move =
                    new UnityEngine.Vector2(
                        (float)(deltaX / sensitivity),
                        0f);
                double clock = Il2Cpp_k._sg._A;
                float cursorX = ReadFloat(
                    judge.Pointer,
                    NativeLayoutCatalog.SkyJudge.CursorX);
                if (float.IsFinite(cursorX))
                {
                    Marshal.WriteInt32(
                        judge.Pointer +
                        NativeLayoutCatalog.SkyJudge.CursorRaw,
                        BitConverter.SingleToInt32Bits(
                            cursorX));
                }
                judge._Mz(
                    clock,
                    move);
                SyncVisualCursor(judge.Pointer);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void SyncVisualCursor(IntPtr judge)
        {
            if (judge == IntPtr.Zero ||
                _visualSyncDisabled)
                return;
            try
            {
                Il2Cppifapp.Game.Scenes.Game.Track track =
                    _visualTrack;
                if (track is null ||
                    track.Pointer == IntPtr.Zero)
                {
                    track =
                        Il2Cppifapp.Game.Scenes.Game.Track._a;
                    if (track is null ||
                        track.Pointer == IntPtr.Zero)
                        return;
                    _visualTrack = track;
                }

                float cursorX = ReadFloat(
                    judge,
                    NativeLayoutCatalog.SkyJudge.CursorX);
                float cursorRaw = ReadFloat(
                    judge,
                    NativeLayoutCatalog.SkyJudge.CursorRaw);
                if (float.IsFinite(cursorX))
                    track._bBA(cursorX);
                if (float.IsFinite(cursorRaw))
                    track._BBA(cursorRaw);
            }
            catch (Exception exception)
            {
                _visualSyncDisabled = true;
                _visualTrack = null;
                if (_visualSyncErrorLogged)
                    return;
                _visualSyncErrorLogged = true;
                ChartExtMod.Log.Warning(
                    "[AutoplayInput] visual cursor sync " +
                    "failed; autoplay continues: " +
                    exception.Message);
            }
        }

        private static bool WriteLanes(
            IntPtr judge,
            int lane,
            bool press,
            bool keepEdge,
            double nowMs)
        {
            IntPtr states = Marshal.ReadIntPtr(
                judge + NativeLayoutCatalog.SkyJudge.InputStates);
            if (states == IntPtr.Zero ||
                !LooksLikeArray(states))
                return false;
            int length = Marshal.ReadInt32(
                states +
                NativeLayoutCatalog.ArrayLengthOffset);
            if (lane < 0 || lane >= length)
                return false;

            IntPtr data = IntPtr.Add(
                states, NativeLayoutCatalog.ArrayDataOffset);
            int stride =
                NativeLayoutCatalog.SkyJudge.InputStateStride;
            double nowSeconds = nowMs / 1000.0;
            IntPtr entry = IntPtr.Add(
                data, lane * stride);
            Marshal.WriteByte(
                entry,
                NativeLayoutCatalog.SkyJudge.InputHeld,
                1);
            if (press || keepEdge)
            {
                Marshal.WriteByte(
                    entry,
                    NativeLayoutCatalog.SkyJudge.InputConsumed,
                    0);
                Marshal.WriteByte(
                    entry,
                    NativeLayoutCatalog.SkyJudge.InputPressed,
                    1);
                Marshal.WriteInt64(
                    entry,
                    NativeLayoutCatalog.SkyJudge.InputPressedAt,
                    BitConverter.DoubleToInt64Bits(nowSeconds));
                Marshal.WriteInt64(
                    entry,
                    NativeLayoutCatalog.SkyJudge.InputLastInput,
                    BitConverter.DoubleToInt64Bits(nowSeconds));
            }
            return true;
        }

        private static bool ChooseGroundLane(
            ActiveInput input,
            IntPtr note)
        {
            int laneFirst = Marshal.ReadInt32(
                note + NativeLayoutCatalog.Note.LaneFirst);
            int laneLast = Marshal.ReadInt32(
                note + NativeLayoutCatalog.Note.LaneLast);
            if (laneFirst < 0 || laneLast < laneFirst)
                return false;
            if (input.Lane >= laneFirst &&
                input.Lane <= laneLast)
                return true;
            int fallback = -1;
            for (int lane = laneFirst;
                 lane <= laneLast;
                 lane++)
            {
                if (lane == _lastGroundLane)
                {
                    if (fallback < 0)
                        fallback = lane;
                    continue;
                }
                if (LaneHeldByOther(input.Due.Id, lane))
                {
                    if (fallback < 0)
                        fallback = lane;
                    continue;
                }
                input.Lane = lane;
                _lastGroundLane = lane;
                return true;
            }
            if (fallback < 0)
                fallback = laneFirst;
            input.Lane = fallback;
            _lastGroundLane = fallback;
            return true;
        }

        internal static bool TryPressGroundWhenDue(
            ChartExtState.AutoplayDue due,
            IntPtr note)
        {
            Il2Cppifapp.Game.LogicalNotePlayer player =
                Il2Cppifapp.Game.LogicalNotePlayer._Qe;
            Il2Cpp_b._T compiled = player?._ve;
            if (compiled is null ||
                compiled.Pointer == IntPtr.Zero)
                return false;
            var records = compiled._XC;
            if (records is null ||
                !records.ContainsKey(due.Id))
                return false;
            Il2CppSystem.Memory<Il2Cpp_K._cH> memory =
                records[due.Id];
            if (memory._object is null ||
                memory._object.Pointer == IntPtr.Zero)
                return false;
            int count = memory._length;
            if (count <= 0 || count > 4096)
                return false;
            IntPtr arrayObject = memory._object.Pointer;
            int sliceIndex = memory._index;
            int arrayLength = Marshal.ReadInt32(
                arrayObject +
                NativeLayoutCatalog.ArrayLengthOffset);
            if (sliceIndex < 0 ||
                sliceIndex + count > arrayLength)
                return false;

            double delta = BitConverter.Int64BitsToDouble(
                Marshal.ReadInt64(
                    note + NativeLayoutCatalog.Note.RuntimeLead));
            if (!double.IsFinite(delta))
                return false;
            unsafe
            {
                for (int r = 0; r < count; r++)
                {
                    byte* record = InteropUtil.RecordPointer(
                        arrayObject, sliceIndex, r);
                    if (*(record + InteropUtil.FinalOffset) != 0)
                        continue;
                    double offset =
                        BitConverter.Int64BitsToDouble(
                            *(long*)(record +
                                NativeLayoutCatalog.Record
                                    .RelativeTime));
                    if (!double.IsFinite(offset) ||
                        offset + delta > GroundEarlyWindowMs)
                        return false;
                    double corrected = -offset - 1.0;
                    Marshal.WriteInt64(
                        note + NativeLayoutCatalog.Note.RuntimeLead,
                        BitConverter.DoubleToInt64Bits(
                            corrected));
                    if (TimingLogged.Count < 20 &&
                        TimingLogged.Add(due.Id))
                    {
                        ChartExtMod.Log.Msg(
                            "[AutoplayInput] exact " +
                            $"id={due.Id} type={due.Type} " +
                            $"rel={offset:F1} delta={delta:F1} " +
                            $"corrected={corrected:F1}");
                    }
                    return true;
                }
            }
            return false;
        }

        private static double ReadRuntimeLead(IntPtr note) =>
            BitConverter.Int64BitsToDouble(
                Marshal.ReadInt64(
                    note + NativeLayoutCatalog.Note.RuntimeLead));

        internal static bool TryGetFlickRange(
            IntPtr note,
            out double center,
            out double sign,
            out double move,
            out double halfWidth)
        {
            center = ReadFloat(
                note, NativeLayoutCatalog.Note.StartCenter);
            int flags = Marshal.ReadInt32(
                note + NativeLayoutCatalog.Note.Flags);
            int direction = (flags >> 8) & 0xFF;
            sign = direction == 16 ? -1.0 :
                direction == 4 ? 1.0 : 0.0;
            double width = ReadFloat(
                note, NativeLayoutCatalog.Note.StartWidth);
            move = 0.0;
            halfWidth = 0.0;
            if (!double.IsFinite(center) || sign == 0.0 ||
                !double.IsFinite(width) || width <= 0.0001)
                return false;
            halfWidth = width * 0.5;
            move = Math.Min(halfWidth, 0.08);
            if (move <= 0.0001)
                move = 0.0001;
            return true;
        }

        private static bool TryGetFlickSign(
            IntPtr note,
            out double sign)
        {
            int flags = Marshal.ReadInt32(
                note + NativeLayoutCatalog.Note.Flags);
            int direction = (flags >> 8) & 0xFF;
            sign = direction == 16 ? -1.0 :
                direction == 4 ? 1.0 : 0.0;
            return sign != 0.0;
        }

        private static void ReleaseExpired(double nowMs)
        {
            Scratch.Clear();
            foreach (KeyValuePair<long, ActiveInput> pair
                     in Active)
            {
                if (pair.Value.Due.Type == 4 &&
                    !pair.Value.Swiped &&
                    nowMs > pair.Value.Due.EndMs + 500.0)
                {
                    Scratch.Add(pair.Key);
                    continue;
                }
                bool neverPressed =
                    pair.Value.ReleaseAtMs ==
                    double.MaxValue;
                if (neverPressed)
                {
                    if (nowMs <=
                        pair.Value.Due.EndMs + 5000.0)
                        continue;
                }
                else if (nowMs < pair.Value.ReleaseAtMs)
                    continue;
                Scratch.Add(pair.Key);
            }
            for (int i = 0; i < Scratch.Count; i++)
            {
                long id = Scratch[i];
                if (!Active.TryGetValue(id, out ActiveInput input))
                    continue;
                ClearIfGround(input, true);
                Active.Remove(id);
            }
        }

        private static void ClearIfGround(
            ActiveInput input,
            bool preserveOtherHeld)
        {
            if (input.IsGround)
                ClearLanes(
                    input.Lane,
                    input.Due.Id,
                    preserveOtherHeld);
        }

        private static void ClearLanes(
            int lane,
            long excludedId,
            bool preserveOtherHeld)
        {
            IntPtr judge = JudgePointer();
            if (judge == IntPtr.Zero)
                return;
            IntPtr states = Marshal.ReadIntPtr(
                judge + NativeLayoutCatalog.SkyJudge.InputStates);
            if (states == IntPtr.Zero ||
                !LooksLikeArray(states))
                return;
            int length = Marshal.ReadInt32(
                states +
                NativeLayoutCatalog.ArrayLengthOffset);
            if (lane < 0 || lane >= length)
                return;
            if (preserveOtherHeld &&
                LaneHeldByOther(excludedId, lane))
                return;
            IntPtr data = IntPtr.Add(
                states, NativeLayoutCatalog.ArrayDataOffset);
            int stride =
                NativeLayoutCatalog.SkyJudge.InputStateStride;
            IntPtr entry = IntPtr.Add(
                data, lane * stride);
            Marshal.WriteByte(
                entry,
                NativeLayoutCatalog.SkyJudge.InputHeld,
                0);
            Marshal.WriteByte(
                entry,
                NativeLayoutCatalog.SkyJudge.InputPressed,
                0);
            Marshal.WriteByte(
                entry,
                NativeLayoutCatalog.SkyJudge.InputConsumed,
                0);
            Marshal.WriteInt64(
                entry,
                NativeLayoutCatalog.SkyJudge.InputPressedAt,
                0);
            Marshal.WriteInt64(
                entry,
                NativeLayoutCatalog.SkyJudge.InputLastInput,
                0);
        }

        internal static void ConsumeGroundEdges()
        {
            if (!Enabled || _failed)
                return;
            IntPtr judge = JudgePointer();
            if (judge == IntPtr.Zero)
                return;
            IntPtr states = Marshal.ReadIntPtr(
                judge + NativeLayoutCatalog.SkyJudge.InputStates);
            if (states == IntPtr.Zero ||
                !LooksLikeArray(states))
                return;
            int length = Marshal.ReadInt32(
                states +
                NativeLayoutCatalog.ArrayLengthOffset);
            IntPtr data = IntPtr.Add(
                states, NativeLayoutCatalog.ArrayDataOffset);
            int stride =
                NativeLayoutCatalog.SkyJudge.InputStateStride;
            int frame = UnityEngine.Time.frameCount;
            foreach (ActiveInput input in Active.Values)
            {
                if (!input.IsGround ||
                    input.PressFrame != frame)
                    continue;
                int lane = input.Lane;
                if (lane < 0 || lane >= length)
                    continue;
                IntPtr entry = IntPtr.Add(
                    data, lane * stride);
                if (Marshal.ReadByte(
                        entry,
                        NativeLayoutCatalog.SkyJudge
                            .InputConsumed) != 0)
                {
                    Marshal.WriteByte(
                        entry,
                        NativeLayoutCatalog.SkyJudge.InputPressed,
                        0);
                    Marshal.WriteByte(
                        entry,
                        NativeLayoutCatalog.SkyJudge.InputConsumed,
                        1);
                }
            }
        }

        private static bool LaneHeldByOther(
            long excludedId,
            int lane)
        {
            foreach (KeyValuePair<long, ActiveInput> pair in Active)
            {
                ActiveInput other = pair.Value;
                if (other.Due.Id == excludedId ||
                    !other.IsGround ||
                    !other.Pressed)
                    continue;
                if (other.Lane == lane)
                    return true;
            }
            return false;
        }

        internal static bool TryGetBarRange(
            IntPtr note,
            double nowMs,
            out double center,
            out double half)
        {
            center = double.NaN;
            half = 0.0;
            int startMs = Marshal.ReadInt32(
                note + NativeLayoutCatalog.Note.StartMs);
            int endMs = Marshal.ReadInt32(
                note + NativeLayoutCatalog.Note.EndMs);
            double duration = endMs - startMs;
            double startX = ReadFloat(
                note, NativeLayoutCatalog.Note.StartCenter);
            double endX = ReadFloat(
                note, NativeLayoutCatalog.Note.EndCenter);
            double startW = ReadFloat(
                note, NativeLayoutCatalog.Note.StartWidth);
            double endW = ReadFloat(
                note, NativeLayoutCatalog.Note.EndWidth);
            if (!double.IsFinite(startX) ||
                !double.IsFinite(endX) ||
                !double.IsFinite(startW) ||
                !double.IsFinite(endW))
                return false;
            if (duration <= 0.5)
            {
                center = startX;
                half = Math.Abs(startW) * 0.5;
                return true;
            }
            double progress = Math.Max(
                0.0,
                Math.Min(
                    1.0,
                    (nowMs - startMs) / duration));
            int flags = Marshal.ReadInt32(
                note + NativeLayoutCatalog.Note.Flags);
            double left = Ease(
                (flags & 0x08) != 0 ? 1 :
                ((flags & 0x10) != 0 ? 2 : 0),
                progress);
            double right = Ease(
                (flags & 0x40) != 0 ? 1 :
                ((flags & 0x80) != 0 ? 2 : 0),
                progress);
            double x0 = -startW * 0.5 + startX +
                ((endX - endW * 0.5) -
                 (startX - startW * 0.5)) * left;
            double x1 = startW * 0.5 + startX +
                ((endX + endW * 0.5) -
                 (startX + startW * 0.5)) * right;
            center = (x0 + x1) * 0.5;
            half = Math.Abs(x1 - x0) * 0.5;
            return true;
        }

        private static double Ease(int kind, double progress)
        {
            const double HalfPi = Math.PI / 2.0;
            switch (kind)
            {
                case 1: return Math.Sin(progress * HalfPi);
                case 2: return 1.0 - Math.Cos(progress * HalfPi);
                default: return progress;
            }
        }

        internal static bool TryGetManagedSkyRange(
            long id,
            double nowMs,
            out double center,
            out double half,
            out double leadMs)
        {
            center = double.NaN;
            half = 0.0;
            leadMs = double.NaN;
            if (!ChartRuntimeContext.IsActive)
                return false;
            SkyBlockPlan plan =
                ChartRuntimeContext.Current?.SkyBlocks;
            if (plan == null ||
                !plan.MemberById.TryGetValue(
                    id, out SkyBlockMember member))
                return false;
            if (!SkyBlockVisualRuntime.TryGetVisualTiming(
                    member,
                    nowMs,
                    out double visualNow,
                    out double visualStart,
                    out double visualEnd))
                return false;
            if (visualEnd <= visualStart ||
                visualNow > visualEnd)
                return false;
            double progress = visualNow < visualStart
                ? 0.0
                : (visualNow - visualStart) /
                  (visualEnd - visualStart);
            double authoredMs =
                member.StartMs +
                progress * (member.EndMs - member.StartMs);
            SkyBandRange range = member.RangeAt(authoredMs);
            center = (range.Lo + range.Hi) * 0.5;
            half = (range.Hi - range.Lo) * 0.5;
            leadMs = visualStart - visualNow;
            return double.IsFinite(center) &&
                   double.IsFinite(half);
        }

        private static bool IsInsideActiveSky(double value)
        {
            for (int i = 0; i < _activeSkyCount; i++)
            {
                if (value >=
                        ActiveSkyCenters[i] -
                        ActiveSkyHalves[i] - 0.0005 &&
                    value <=
                        ActiveSkyCenters[i] +
                        ActiveSkyHalves[i] + 0.0005)
                    return true;
            }
            return false;
        }

        private static bool TryGetActiveSkyBounds(
            out double left,
            out double right)
        {
            left = double.NegativeInfinity;
            right = double.PositiveInfinity;
            if (_activeSkyCount <= 0)
                return false;
            for (int i = 0; i < _activeSkyCount; i++)
            {
                left = Math.Max(
                    left,
                    ActiveSkyCenters[i] -
                        ActiveSkyHalves[i]);
                right = Math.Min(
                    right,
                    ActiveSkyCenters[i] +
                        ActiveSkyHalves[i]);
            }
            if (left <= right)
                return true;
            int nearest = 0;
            double nearestAbs = double.MaxValue;
            for (int i = 0; i < _activeSkyCount; i++)
            {
                double abs = Math.Abs(ActiveSkyLeads[i]);
                if (abs < nearestAbs)
                {
                    nearestAbs = abs;
                    nearest = i;
                }
            }
            left = ActiveSkyCenters[nearest] -
                ActiveSkyHalves[nearest];
            right = ActiveSkyCenters[nearest] +
                ActiveSkyHalves[nearest];
            return left <= right;
        }

        internal static float ReadFloat(
            IntPtr pointer,
            int offset) =>
            BitConverter.Int32BitsToSingle(
                Marshal.ReadInt32(pointer, offset));

        internal static double ReadDouble(
            IntPtr pointer,
            int offset) =>
            BitConverter.Int64BitsToDouble(
                Marshal.ReadInt64(pointer, offset));

        internal static IntPtr NotePointer(
            ChartExtState.AutoplayDue due)
        {
            IntPtr basePtr =
                NativeNoteSlotTable.SlotBase(due.Side);
            if (basePtr == IntPtr.Zero)
                return IntPtr.Zero;
            IntPtr note = IntPtr.Add(
                basePtr,
                due.Position *
                NativeLayoutCatalog.NoteStride);
            if (Marshal.ReadInt64(
                    note + NativeLayoutCatalog.Note.Id) !=
                due.Id)
                return IntPtr.Zero;
            return note;
        }

        internal static IntPtr JudgePointer()
        {
            Il2Cpp_E._VD judge =
                Il2Cppifapp.Game.Scenes.GameScene._DN;
            return judge is null
                ? IntPtr.Zero
                : judge.Pointer;
        }

        private static bool LooksLikeArray(IntPtr array)
        {
            if (array == IntPtr.Zero)
                return false;
            int length = Marshal.ReadInt32(
                array +
                NativeLayoutCatalog.ArrayLengthOffset);
            return length >= 0 && length <= 1_000_000;
        }

        private static void Fail(
            string stage,
            Exception exception)
        {
            _failed = true;
            try
            {
                ChartExtMod.Log.Error(
                    $"[AutoplayInput] {stage} failed; engine input " +
                    "mode and autoplay disabled; record fallback " +
                    "was not used: " + exception);
            }
            catch
            {
            }
            Enabled = false;
            AutoplayRuntime.Enabled = false;
        }

        private sealed class ActiveInput
        {
            internal ChartExtState.AutoplayDue Due;
            internal bool Pressed;
            internal bool IsGround;
            internal bool Swiped;
            internal int PressFrame;
            internal int Lane = -1;
            internal double ReleaseAtMs;
        }
    }
}
