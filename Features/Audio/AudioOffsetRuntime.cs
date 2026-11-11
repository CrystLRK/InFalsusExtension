using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using HarmonyLib;
using Il2Cpp_J;
using MelonLoader;

namespace InFalsusChartExt
{
    /// <summary>
    /// audiooffset(timems): shifts the gameplay music only. Notes, judgement time and
    /// the chart clock are untouched. Negative values seek into the song; positive
    /// values hold the FMOD channel paused until the requested delay has elapsed.
    /// </summary>
    internal static class AudioOffsetRuntime
    {
        private static readonly int NativeChannelOffset =
            NativeLayoutCatalog.Audio.Channel;
        private const uint TimeUnitMs = 1;

        private sealed class ChannelState
        {
            public _Cg Controller;
            public bool NegativeSeekPrepared;
            public bool NegativeSeekAtZero;
            public bool PositiveReleased;
        }

        private static readonly List<ChannelState> Channels = new List<ChannelState>();
        private static int _errors;
        private static int _logs;
        private static IntPtr _fmodModule;
        private static SetPositionDelegate _setPosition;
        private static SetPausedDelegate _setPaused;

        internal static void Register(_Cg controller)
        {
            if (controller == null || controller.Pointer == IntPtr.Zero) return;
            foreach (ChannelState state in Channels)
            {
                if (state.Controller != null &&
                    state.Controller.Pointer == controller.Pointer)
                    return;
            }
            Channels.Add(new ChannelState { Controller = controller });
        }

        internal static void ResetForChartSwitch()
        {
            Channels.Clear();
        }

        internal static void Tick()
        {
            if (!BuildProfile.Supports(NativeCapability.AudioOffset))
                return;
            try
            {
                if (!ChartExtState.HasAudioOffset ||
                    ChartExtState.AudioOffsetMs == 0 ||
                    Channels.Count == 0)
                    return;
                double nowMs = TimingGroupRuntime.CurrentMs();
                if (double.IsNaN(nowMs) || double.IsInfinity(nowMs)) return;
                int offset = ChartExtState.AudioOffsetMs;
                for (int i = 0; i < Channels.Count; i++)
                {
                    ChannelState state = Channels[i];
                    if (state.Controller == null ||
                        state.Controller.Pointer == IntPtr.Zero)
                        continue;
                    IntPtr channel = Marshal.ReadIntPtr(
                        state.Controller.Pointer + NativeChannelOffset);
                    if (channel == IntPtr.Zero) continue;

                    if (offset < 0)
                    {
                        uint seekMs = (uint)Math.Min(int.MaxValue, -offset);
                        if (!state.NegativeSeekPrepared && nowMs >= -250.0)
                        {
                            SetPosition(channel, seekMs);
                            state.NegativeSeekPrepared = true;
                        }
                        if (!state.NegativeSeekAtZero && nowMs >= 0.0)
                        {
                            SetPosition(channel, seekMs);
                            state.NegativeSeekAtZero = true;
                            if (_logs++ < 6)
                                ChartExtMod.Log.Msg(
                                    $"[ChartExt] audiooffset seek +{seekMs}ms");
                        }
                    }
                    else
                    {
                        if (nowMs < offset)
                        {
                            SetPaused(channel, true);
                        }
                        else if (!state.PositiveReleased)
                        {
                            SetPaused(channel, false);
                            state.PositiveReleased = true;
                            if (_logs++ < 6)
                                ChartExtMod.Log.Msg(
                                    $"[ChartExt] audiooffset released after {offset}ms");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (_errors++ < 20)
                    ChartExtMod.Log.Error("[ChartExt] audiooffset tick failed: " + e);
            }
        }

        private static void SetPosition(IntPtr channel, uint positionMs)
        {
            SetPositionDelegate call = ResolveSetPosition();
            if (call == null) return;
            int result = call(channel, positionMs, TimeUnitMs);
            if (result != 0 && _errors++ < 20)
                ChartExtMod.Log.Warning(
                    $"[ChartExt] FMOD Channel_SetPosition returned {result}");
        }

        private static void SetPaused(IntPtr channel, bool paused)
        {
            SetPausedDelegate call = ResolveSetPaused();
            if (call == null) return;
            int result = call(channel, paused ? 1 : 0);
            if (result != 0 && _errors++ < 20)
                ChartExtMod.Log.Warning(
                    $"[ChartExt] FMOD Channel_SetPaused returned {result}");
        }

        private static SetPositionDelegate ResolveSetPosition()
        {
            if (_setPosition != null) return _setPosition;
            IntPtr fn = ResolveExport("FMOD5_Channel_SetPosition");
            if (fn == IntPtr.Zero) return null;
            _setPosition =
                Marshal.GetDelegateForFunctionPointer<SetPositionDelegate>(fn);
            return _setPosition;
        }

        private static SetPausedDelegate ResolveSetPaused()
        {
            if (_setPaused != null) return _setPaused;
            IntPtr fn = ResolveExport("FMOD5_Channel_SetPaused");
            if (fn == IntPtr.Zero) return null;
            _setPaused =
                Marshal.GetDelegateForFunctionPointer<SetPausedDelegate>(fn);
            return _setPaused;
        }

        private static IntPtr ResolveExport(string name)
        {
            if (_fmodModule == IntPtr.Zero)
            {
                _fmodModule = GetModuleHandle("fmodstudioL.dll");
                if (_fmodModule == IntPtr.Zero)
                    _fmodModule = GetModuleHandle("fmodstudio.dll");
            }
            return _fmodModule == IntPtr.Zero
                ? IntPtr.Zero
                : GetProcAddress(_fmodModule, name);
        }

        [DllImport("kernel32", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string moduleName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr module, string procedureName);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int SetPositionDelegate(IntPtr channel, uint position, uint timeUnit);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int SetPausedDelegate(IntPtr channel, int paused);
    }

}
