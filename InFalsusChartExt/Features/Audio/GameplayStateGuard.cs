using System;
using System.Runtime.InteropServices;
using InFalsusChartExt.CustomSong.Audio;
#if MELONLOADER
using Il2Cppifapp.Game;
using Il2Cppifapp.Game.Scenes;
#elif BEPINEX
using ifapp.Game;
using ifapp.Game.Scenes;
#endif

namespace InFalsusChartExt
{
    /// <summary>
    /// Keeps the native judge in the gameplay state through the chart tail.
    /// The game can leave gameplay when its internal audio worker ends, which
    /// may be before the final SkyArea. This guard restores `_VD._a = 2` only
    /// in the tail window, then releases it after the chart end + 1s or the
    /// audio end + 1s (whichever is later).
    /// </summary>
    internal static class GameplayStateGuard
    {
        private const int StaticClassRva = 0x31B1248;
        private const int StaticDataOffset = 184;
        private static int _generation = -1;
        private static double _lastEndMs;
        private static double _forceStartMs;
        private static double _restoreAtMs;
        private static double _audioEndMs;
        private static bool _forcing;
        private static bool _finishTimeApplied;
        private static IntPtr _staticData;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(string name);

        internal static void Reset()
        {
            _generation = -1;
            _lastEndMs = 0.0;
            _forceStartMs = 0.0;
            _restoreAtMs = 0.0;
            _audioEndMs = 0.0;
            _forcing = false;
            _finishTimeApplied = false;
            _staticData = IntPtr.Zero;
        }

        internal static void Tick()
        {
            try
            {
                if (ChartExtState.ChartGeneration != _generation)
                {
                    _generation = ChartExtState.ChartGeneration;
                    _forcing = false;
                    PrepareWindow();
                }
                if (_lastEndMs <= 0.0)
                    return;
                if (!_finishTimeApplied)
                    _finishTimeApplied =
                        ApplyNativeFinishTime(_audioEndMs);
                Il2Cpp_E._VD judge = GameScene._DN;
                if (judge is null ||
                    judge.Pointer == IntPtr.Zero)
                    return;
                double now = judge._c * 1000.0;
                if (now < _forceStartMs)
                    return;
                if (now > _restoreAtMs)
                {
                    if (_forcing)
                    {
                        WriteState(0);
                        _forcing = false;
                    }
                    return;
                }
                if (TimingGroupRuntime.IsPauseLayerActive())
                    return;
                if ((byte)Il2Cpp_E._VD._a != 2)
                {
                    WriteState(2);
                    _forcing = true;
                }
            }
            catch
            {
            }
        }

        private static void PrepareWindow()
        {
            _lastEndMs = 0.0;
            var dues = ChartExtState.AutoplayDues;
            for (int i = 0; i < dues.Count; i++)
            {
                if (dues[i].EndMs > _lastEndMs)
                    _lastEndMs = dues[i].EndMs;
            }
            if (_lastEndMs <= 0.0)
                return;
            double audioEndMs = _lastEndMs + 3000.0;
            if (CustomSongAudioController
                    .TryGetGameplayLengthMs(out uint lengthMs) &&
                lengthMs > 0)
                audioEndMs = lengthMs;
            _forceStartMs = _lastEndMs - 5000.0;
            _restoreAtMs = Math.Max(
                _lastEndMs + 1000.0,
                audioEndMs + 1000.0);
            _audioEndMs = audioEndMs;
        }

        private static bool ApplyNativeFinishTime(
            double audioEndMs)
        {
            try
            {
                LogicalNotePlayer player =
                    LogicalNotePlayer._Qe;
                if (player is null ||
                    player.Pointer == IntPtr.Zero)
                    return false;
                IntPtr pointer = player.Pointer;
                double desired = Math.Max(
                    _lastEndMs + 1000.0,
                    audioEndMs + 1000.0);
                double finish = BitConverter.Int64BitsToDouble(
                    Marshal.ReadInt64(
                        new IntPtr(pointer.ToInt64() + 0x68)));
                if (double.IsFinite(finish) &&
                    finish < desired)
                {
                    Marshal.WriteInt64(
                        new IntPtr(pointer.ToInt64() + 0x68),
                        BitConverter.DoubleToInt64Bits(desired));
                }
                double forceComplete =
                    BitConverter.Int64BitsToDouble(
                        Marshal.ReadInt64(
                            new IntPtr(
                                pointer.ToInt64() + 0x60)));
                double forceCompleteTarget =
                    _lastEndMs - 1000.0;
                if (double.IsFinite(forceComplete) &&
                    forceCompleteTarget > 0.0 &&
                    Math.Abs(forceComplete -
                        forceCompleteTarget) > 0.5)
                {
                    Marshal.WriteInt64(
                        new IntPtr(pointer.ToInt64() + 0x60),
                        BitConverter.DoubleToInt64Bits(
                            forceCompleteTarget));
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        private static void WriteState(byte value)
        {
            if (_staticData == IntPtr.Zero)
            {
                IntPtr module = GetModuleHandle(
                    "GameAssembly.dll");
                if (module == IntPtr.Zero)
                    return;
                IntPtr klass = Marshal.ReadIntPtr(
                    new IntPtr(
                        module.ToInt64() +
                        StaticClassRva));
                if (klass == IntPtr.Zero)
                    return;
                _staticData = Marshal.ReadIntPtr(
                    new IntPtr(
                        klass.ToInt64() +
                        StaticDataOffset));
            }
            if (_staticData == IntPtr.Zero)
                return;
            Marshal.WriteByte(_staticData, value);
            Marshal.WriteByte(
                new IntPtr(_staticData.ToInt64() + 1),
                value);
        }
    }
}
