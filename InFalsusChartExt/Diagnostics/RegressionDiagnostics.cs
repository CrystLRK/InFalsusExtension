using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using InFalsusChartExt.CustomSong.Audio;
using Il2Cpp_J;

namespace InFalsusChartExt
{
    /// <summary>
    /// Temporary diagnostics for non-deterministic audio and pause regressions.
    /// All call sites are removed from release builds by ConditionalAttribute.
    /// </summary>
    internal static class RegressionDiagnostics
    {
#if IFCE_DIAGNOSTICS
        private const int MaxFactoryEvents = 64;
        private const int MaxStateEvents = 240;
        private static readonly List<AudioFactoryTrace> Factories =
            new List<AudioFactoryTrace>();
        private static bool _hasGameplayState;
        private static bool _gameplay;
        private static int _factoryEvents;
        private static int _stateEvents;
        private static int _resumeSamples;
        private static int _pauseSamples;
        private static float _nextAudioSample;
        private static float _nextResumeSample;
        private static float _nextPauseSample;
        private const int MaxResumeSamples = 8;
        private const int VkF10 = 0x79;
        private static bool _f10Down;

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int virtualKey);

        private sealed class AudioFactoryTrace
        {
            internal string Name;
            internal bool Selection;
            internal IntPtr Controller;
            internal IntPtr Sound;
            internal IntPtr Channel;
            internal int Paused = -1;
            internal int Playing = -1;
            internal uint Position;
            internal int StalledSamples;
            internal float NextStalledLog;
        }

        private static bool CanLogState =>
            _stateEvents < MaxStateEvents;

        private static void LogState(string message)
        {
            if (!CanLogState)
                return;
            _stateEvents++;
            ChartExtMod.Log.Msg(
                "[DiagState] " + message);
        }

        internal static void AudioFactory(
            string name,
            bool selection,
            _Cg controller)
        {
            if (controller is null ||
                controller.Pointer == IntPtr.Zero)
                return;
            AudioFactoryTrace trace = null;
            for (int i = 0; i < Factories.Count; i++)
            {
                if (Factories[i].Controller !=
                    controller.Pointer)
                    continue;
                trace = Factories[i];
                break;
            }
            if (trace is null)
            {
                if (_factoryEvents >= MaxFactoryEvents)
                    return;
                trace = new AudioFactoryTrace
                {
                    Controller = controller.Pointer,
                };
                Factories.Add(trace);
                _factoryEvents++;
            }
            trace.Name = name ?? string.Empty;
            trace.Selection = selection;
            Refresh(trace, true);
        }

        internal static void AudioTick()
        {
            float now = UnityEngine.Time.realtimeSinceStartup;
            if (now < _nextAudioSample)
                return;
            _nextAudioSample = now + 0.5f;
            bool gameplay =
                (byte)Il2Cpp_E._VD._a == 2;
            if (!gameplay)
                return;
            for (int i = 0; i < Factories.Count; i++)
            {
                AudioFactoryTrace trace = Factories[i];
                int oldPaused = trace.Paused;
                int oldPlaying = trace.Playing;
                Refresh(trace, false);
                if (trace.Paused == oldPaused &&
                    trace.Playing == oldPlaying)
                {
                    if (!trace.Selection &&
                        trace.Paused == 1 &&
                        trace.Position == 0 &&
                        trace.StalledSamples < 8 &&
                        now >= trace.NextStalledLog)
                    {
                        trace.StalledSamples++;
                        trace.NextStalledLog = now + 1f;
                        WriteFactory(trace, "stalled");
                    }
                    continue;
                }
                WriteFactory(trace, "state-change");
            }
        }

        internal static void GameplayStateTick(
            Il2Cppifapp.Game.Scenes.Game.Track track)
        {
            byte state = (byte)Il2Cpp_E._VD._a;
            bool gameplay = state == 2;
            if (!_hasGameplayState)
            {
                _hasGameplayState = true;
                _gameplay = gameplay;
                if (gameplay)
                {
                    _resumeSamples = MaxResumeSamples;
                    _nextResumeSample =
                        UnityEngine.Time.realtimeSinceStartup;
                }
                PauseTick(track);
                return;
            }
            if (gameplay == _gameplay)
            {
                PauseTick(track);
                return;
            }
            _gameplay = gameplay;
            LogState(
                $"gameplay={gameplay} state={state} " +
                $"chart='{ChartExtState.LastChartName}' " +
                $"generation={ChartExtState.ChartGeneration}");
            _resumeSamples = gameplay ? MaxResumeSamples : 0;
            _nextResumeSample =
                UnityEngine.Time.realtimeSinceStartup;
            _pauseSamples = gameplay ? 0 : MaxResumeSamples;
            _nextPauseSample =
                UnityEngine.Time.realtimeSinceStartup;
            PauseTick(track);
        }

        internal static void TrackTick(
            Il2Cppifapp.Game.Scenes.Game.Track track)
        {
            if (_resumeSamples <= 0 || track is null ||
                track.Pointer == IntPtr.Zero)
                return;
            float realtime =
                UnityEngine.Time.realtimeSinceStartup;
            if (realtime < _nextResumeSample)
                return;
            _resumeSamples--;
            _nextResumeSample = realtime + 0.5f;
            string snapshot =
                SkyBlockVisualRuntime.DiagnosticSnapshot(
                    track,
                    TimingGroupRuntime.CurrentMs());
            LogState(
                $"resume-sample={MaxResumeSamples - _resumeSamples} " +
                snapshot);
        }

        private static void PauseTick(
            Il2Cppifapp.Game.Scenes.Game.Track track)
        {
            if (_gameplay || _pauseSamples <= 0 ||
                track is null || track.Pointer == IntPtr.Zero)
                return;
            float realtime =
                UnityEngine.Time.realtimeSinceStartup;
            if (realtime < _nextPauseSample)
                return;
            _pauseSamples--;
            _nextPauseSample = realtime + 0.5f;
            LogState(
                $"pause-sample={MaxResumeSamples - _pauseSamples} " +
                SkyBlockVisualRuntime.DiagnosticSnapshot(
                    track,
                    TimingGroupRuntime.CurrentMs()));
        }

        internal static void TickInput(
            Il2Cppifapp.Game.Scenes.Game.Track track)
        {
            bool down =
                (GetAsyncKeyState(VkF10) & 0x8000) != 0;
            if (!down || _f10Down)
            {
                _f10Down = down;
                return;
            }
            _f10Down = true;
            if (track is null || track.Pointer == IntPtr.Zero)
                return;
            LogState(
                "manual-snapshot " +
                SkyBlockVisualRuntime.DiagnosticSnapshot(
                    track,
                    TimingGroupRuntime.CurrentMs()));
        }

        internal static void AudioOwned(
            string stage,
            bool selection,
            IntPtr controller,
            IntPtr sound,
            IntPtr channel)
        {
            LogState(
                $"audio-owner stage={stage}" +
                $" selection={selection}" +
                $" controller=0x{controller.ToInt64():X}" +
                $" sound=0x{sound.ToInt64():X}" +
                $" channel=0x{channel.ToInt64():X}");
        }

        internal static void Reset()
        {
            Factories.Clear();
            _hasGameplayState = false;
            _gameplay = false;
            _factoryEvents = 0;
            _stateEvents = 0;
            _resumeSamples = 0;
            _pauseSamples = 0;
            _nextAudioSample = 0f;
            _nextResumeSample = 0f;
            _nextPauseSample = 0f;
            _f10Down = false;
        }

        private static void Refresh(
            AudioFactoryTrace trace,
            bool force)
        {
            try
            {
                IntPtr controller = trace.Controller;
                IntPtr sound = Marshal.ReadIntPtr(
                    controller +
                    NativeLayoutCatalog.CustomSong.AudioSound);
                IntPtr channel = Marshal.ReadIntPtr(
                    controller +
                    NativeLayoutCatalog.CustomSong.AudioChannel);
                int paused = -1;
                int playing = -1;
                uint position = 0;
                if (channel != IntPtr.Zero)
                {
                    if (FmodExports.GetPaused(
                            channel, out int value) == 0)
                        paused = value;
                    if (FmodExports.IsPlaying(
                            channel, out int active) == 0)
                        playing = active;
                    if (FmodExports.GetPosition(
                            channel,
                            out position,
                            (uint)Il2CppFMOD.TIMEUNIT.MS) != 0)
                        position = 0;
                }
                bool changed =
                    force ||
                    trace.Sound != sound ||
                    trace.Channel != channel ||
                    trace.Paused != paused ||
                    trace.Playing != playing ||
                    (Math.Abs(
                        (long)position -
                        (long)trace.Position) > 200);
                trace.Sound = sound;
                trace.Channel = channel;
                trace.Paused = paused;
                trace.Playing = playing;
                trace.Position = position;
                if (changed)
                    WriteFactory(trace, force ? "factory" : "sample");
            }
            catch (Exception exception)
            {
                if (force && CanLogState)
                {
                    _stateEvents++;
                    ChartExtMod.Log.Warning(
                        "[DiagState] audio probe failed: " +
                        exception.Message);
                }
            }
        }

        private static void WriteFactory(
            AudioFactoryTrace trace,
            string stage)
        {
            LogState(
                $"audio stage={stage}" +
                $" selection={trace.Selection}" +
                $" name='{trace.Name}'" +
                $" chart='{ChartExtState.LastChartName}'" +
                $" generation={ChartExtState.ChartGeneration}" +
                $" controller=0x{trace.Controller.ToInt64():X}" +
                $" sound=0x{trace.Sound.ToInt64():X}" +
                $" channel=0x{trace.Channel.ToInt64():X}" +
                $" paused={trace.Paused}" +
                $" playing={trace.Playing}" +
                $" position={trace.Position}");
        }
#endif

#if !IFCE_DIAGNOSTICS
        [Conditional("IFCE_DIAGNOSTICS")]
        internal static void AudioFactory(
            string name,
            bool selection,
            _Cg controller)
        {
        }

        [Conditional("IFCE_DIAGNOSTICS")]
        internal static void AudioTick()
        {
        }

        [Conditional("IFCE_DIAGNOSTICS")]
        internal static void GameplayStateTick(
            Il2Cppifapp.Game.Scenes.Game.Track track)
        {
        }

        [Conditional("IFCE_DIAGNOSTICS")]
        internal static void TrackTick(
            Il2Cppifapp.Game.Scenes.Game.Track track)
        {
        }

        [Conditional("IFCE_DIAGNOSTICS")]
        internal static void TickInput(
            Il2Cppifapp.Game.Scenes.Game.Track track)
        {
        }

        [Conditional("IFCE_DIAGNOSTICS")]
        internal static void AudioOwned(
            string stage,
            bool selection,
            IntPtr controller,
            IntPtr sound,
            IntPtr channel)
        {
        }

        [Conditional("IFCE_DIAGNOSTICS")]
        internal static void Reset()
        {
        }
#endif
    }
}
