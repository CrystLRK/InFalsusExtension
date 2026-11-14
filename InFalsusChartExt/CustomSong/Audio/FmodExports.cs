using System;
using System.Runtime.InteropServices;

namespace InFalsusChartExt.CustomSong.Audio
{
    internal static class FmodExports
    {
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate int SoundGetSystem(
            IntPtr sound,
            out IntPtr system);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate int ChannelGetGroup(
            IntPtr channel,
            out IntPtr channelGroup);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate int PlaySound(
            IntPtr system,
            IntPtr sound,
            IntPtr channelGroup,
            int paused,
            out IntPtr channel);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate int ChannelSetMode(
            IntPtr channel,
            uint mode);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate int ChannelSetLoopCount(
            IntPtr channel,
            int loopCount);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate int ChannelSetLoopPoints(
            IntPtr channel,
            uint start,
            uint startType,
            uint end,
            uint endType);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate int ChannelSetPosition(
            IntPtr channel,
            uint position,
            uint positionType);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate int ChannelSetPaused(
            IntPtr channel,
            int paused);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate int ChannelGetPaused(
            IntPtr channel,
            out int paused);

#if IFCE_DIAGNOSTICS
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate int ChannelIsPlaying(
            IntPtr channel,
            out int playing);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate int ChannelGetPosition(
            IntPtr channel,
            out uint position,
            uint positionType);
#endif

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate int ChannelStop(IntPtr channel);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate int SoundRelease(IntPtr sound);

        private static IntPtr _module;
        private static SoundGetSystem _getSystem;
        private static ChannelGetGroup _getGroup;
        private static PlaySound _playSound;
        private static ChannelSetMode _setMode;
        private static ChannelSetLoopCount _setLoopCount;
        private static ChannelSetLoopPoints _setLoopPoints;
        private static ChannelSetPosition _setPosition;
        private static ChannelSetPaused _setPaused;
        private static ChannelGetPaused _getPaused;
#if IFCE_DIAGNOSTICS
        private static ChannelIsPlaying _isPlaying;
        private static ChannelGetPosition _getPosition;
#endif
        private static ChannelStop _stop;
        private static SoundRelease _release;

        /// <summary>
        /// Resolves every export used after a replacement Sound is created.
        /// The replacement transaction calls this before createSound so a
        /// missing release/control export cannot leave a half-owned Sound.
        /// </summary>
        internal static void PrepareSoundLifecycle()
        {
            _getSystem ??= Resolve<SoundGetSystem>(
                "FMOD5_Sound_GetSystemObject",
                "FMOD_Sound_GetSystemObject");
            _getGroup ??= Resolve<ChannelGetGroup>(
                "FMOD5_Channel_GetChannelGroup",
                "FMOD_Channel_GetChannelGroup");
            _playSound ??= Resolve<PlaySound>(
                "FMOD5_System_PlaySound",
                "FMOD_System_PlaySound");
            _setMode ??= Resolve<ChannelSetMode>(
                "FMOD5_Channel_SetMode",
                "FMOD_Channel_SetMode");
            _setLoopCount ??= Resolve<ChannelSetLoopCount>(
                "FMOD5_Channel_SetLoopCount",
                "FMOD_Channel_SetLoopCount");
            _setLoopPoints ??= Resolve<ChannelSetLoopPoints>(
                "FMOD5_Channel_SetLoopPoints",
                "FMOD_Channel_SetLoopPoints");
            _setPosition ??= Resolve<ChannelSetPosition>(
                "FMOD5_Channel_SetPosition",
                "FMOD_Channel_SetPosition");
            _setPaused ??= Resolve<ChannelSetPaused>(
                "FMOD5_Channel_SetPaused",
                "FMOD_Channel_SetPaused");
            _getPaused ??= Resolve<ChannelGetPaused>(
                "FMOD5_Channel_GetPaused",
                "FMOD_Channel_GetPaused");
            _stop ??= Resolve<ChannelStop>(
                "FMOD5_Channel_Stop",
                "FMOD_Channel_Stop");
            _release ??= Resolve<SoundRelease>(
                "FMOD5_Sound_Release",
                "FMOD_Sound_Release");
        }

        internal static int GetSystem(IntPtr sound, out IntPtr system) =>
            (_getSystem ??= Resolve<SoundGetSystem>(
                "FMOD5_Sound_GetSystemObject",
                "FMOD_Sound_GetSystemObject"))(sound, out system);

        internal static int GetChannelGroup(
            IntPtr channel,
            out IntPtr group) =>
            (_getGroup ??= Resolve<ChannelGetGroup>(
                "FMOD5_Channel_GetChannelGroup",
                "FMOD_Channel_GetChannelGroup"))(channel, out group);

        internal static int Play(
            IntPtr system,
            IntPtr sound,
            IntPtr group,
            bool paused,
            out IntPtr channel) =>
            (_playSound ??= Resolve<PlaySound>(
                "FMOD5_System_PlaySound",
                "FMOD_System_PlaySound"))(
                    system, sound, group, paused ? 1 : 0, out channel);

        internal static int SetMode(
            IntPtr channel,
            uint mode) =>
            (_setMode ??= Resolve<ChannelSetMode>(
                "FMOD5_Channel_SetMode",
                "FMOD_Channel_SetMode"))(channel, mode);

        internal static int SetLoopCount(
            IntPtr channel,
            int count) =>
            (_setLoopCount ??= Resolve<ChannelSetLoopCount>(
                "FMOD5_Channel_SetLoopCount",
                "FMOD_Channel_SetLoopCount"))(channel, count);

        internal static int SetLoopPoints(
            IntPtr channel,
            uint start,
            uint startType,
            uint end,
            uint endType) =>
            (_setLoopPoints ??= Resolve<ChannelSetLoopPoints>(
                "FMOD5_Channel_SetLoopPoints",
                "FMOD_Channel_SetLoopPoints"))(
                    channel, start, startType, end, endType);

        internal static int SetPosition(
            IntPtr channel,
            uint position,
            uint type) =>
            (_setPosition ??= Resolve<ChannelSetPosition>(
                "FMOD5_Channel_SetPosition",
                "FMOD_Channel_SetPosition"))(
                    channel, position, type);

        internal static int SetPaused(
            IntPtr channel,
            bool paused) =>
            (_setPaused ??= Resolve<ChannelSetPaused>(
                "FMOD5_Channel_SetPaused",
                "FMOD_Channel_SetPaused"))(
                    channel, paused ? 1 : 0);

        internal static int GetPaused(
            IntPtr channel,
            out int paused) =>
            (_getPaused ??= Resolve<ChannelGetPaused>(
                "FMOD5_Channel_GetPaused",
                "FMOD_Channel_GetPaused"))(
                    channel, out paused);

#if IFCE_DIAGNOSTICS
        internal static int IsPlaying(
            IntPtr channel,
            out int playing) =>
            (_isPlaying ??= Resolve<ChannelIsPlaying>(
                "FMOD5_Channel_IsPlaying",
                "FMOD_Channel_IsPlaying"))(
                    channel, out playing);

        internal static int GetPosition(
            IntPtr channel,
            out uint position,
            uint type) =>
            (_getPosition ??= Resolve<ChannelGetPosition>(
                "FMOD5_Channel_GetPosition",
                "FMOD_Channel_GetPosition"))(
                    channel, out position, type);
#endif

        internal static int Stop(IntPtr channel) =>
            (_stop ??= Resolve<ChannelStop>(
                "FMOD5_Channel_Stop",
                "FMOD_Channel_Stop"))(channel);

        internal static int Release(IntPtr sound) =>
            (_release ??= Resolve<SoundRelease>(
                "FMOD5_Sound_Release",
                "FMOD_Sound_Release"))(sound);

        private static T Resolve<T>(params string[] names)
            where T : Delegate
        {
            EnsureModule();
            foreach (string name in names)
            {
                if (!NativeLibrary.TryGetExport(
                        _module, name, out IntPtr address))
                {
                    continue;
                }
                return Marshal.GetDelegateForFunctionPointer<T>(
                    address);
            }
            throw new MissingMethodException(
                "FMOD export was not found: " +
                string.Join(", ", names));
        }

        private static void EnsureModule()
        {
            if (_module != IntPtr.Zero)
                return;
            try
            {
                _module = NativeLibrary.Load("fmodstudio.dll");
            }
            catch
            {
                _module = NativeLibrary.Load("fmod.dll");
            }
        }
    }
}
