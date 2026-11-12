using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using HarmonyLib;
using InFalsusChartExt.CustomSong.Discovery;
using InFalsusChartExt.CustomSong.Lifecycle;
using Il2Cpp_J;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppFMOD;

namespace InFalsusChartExt.CustomSong.Audio
{
    internal static class CustomSongAudioController
    {
        private static readonly List<Pending> Waiting =
            new List<Pending>();
        private static readonly List<OwnedAudio> Owned =
            new List<OwnedAudio>();
        private static IntPtr _systemPointer;
        private static int _ownedErrors;
        private static int _pauseErrors;
        private static bool _shutdownRequested;
        [ThreadStatic]
        private static Stack<Route> _routes;
#if IFCE_DIAGNOSTICS
        [ThreadStatic]
        private static Stack<FactoryDiagnostic> _diagnostics;
#endif
        internal static void Reset()
        {
            Waiting.Clear();
            _routes?.Clear();
            _ownedErrors = 0;
            _pauseErrors = 0;
            ReleaseAllOwned();
            _shutdownRequested = false;
            RegressionDiagnostics.Reset();
        }

        internal static void PrepareForShutdown()
        {
            _shutdownRequested = true;
        }

        internal static void Tick()
        {
            if (_shutdownRequested)
                return;
            ReleaseDisposedControllerAudio();
            SynchronizeGameplayAudioPause();
            RegressionDiagnostics.AudioTick();
            for (int i = Waiting.Count - 1; i >= 0; i--)
            {
                Pending pending = Waiting[i];
                if (pending.Controller is null ||
                    pending.Controller.Pointer == IntPtr.Zero ||
                    pending.Deadline < DateTime.UtcNow)
                {
                    Waiting.RemoveAt(i);
                    continue;
                }
                if (!TryGetNativeSound(pending.Controller, out _) ||
                    !TryGetNativeChannel(
                        pending.Controller, out _) ||
                    !IsNativeAudioConfigured(pending.Controller))
                    continue;
                Waiting.RemoveAt(i);
                TryReplace(pending);
            }
        }

        internal static void ReleaseOwnedForSceneUnload()
        {
            Waiting.Clear();
            _ownedErrors = 0;
            _pauseErrors = 0;
            ReleaseAllOwned();
            RegressionDiagnostics.Reset();
        }

        [HarmonyPatch]
        private static class AudioFactoryPatch
        {
            [HarmonyPrepare]
            private static bool Prepare() =>
                NativePatchTargetResolver.IsAvailable(
                    "audioFactory");

            [HarmonyTargetMethod]
            private static System.Reflection.MethodBase
                TargetMethod() =>
                    NativePatchTargetResolver.ResolveHarmony(
                        "audioFactory");

            [HarmonyPrefix]
            private static void Prefix(
                ref string __0,
                bool __6)
            {
#if IFCE_DIAGNOSTICS
                _diagnostics ??= new Stack<FactoryDiagnostic>();
                _diagnostics.Push(
                    new FactoryDiagnostic(__0, __6));
#endif
                _routes ??= new Stack<Route>();
                if (!BuildProfile.Supports(
                        NativeCapability.CustomSong) ||
                    !CustomSongRuntime.TryGetSongForAudioName(
                        __0, out CustomSongPlan song))
                {
                    _routes.Push(null);
                    return;
                }
                if (!string.IsNullOrWhiteSpace(
                        song.TemplateAudioName))
                {
                    __0 = song.TemplateAudioName;
                }
                _routes.Push(new Route(song, __6));
            }

            [HarmonyPostfix]
            private static void Postfix(_Cg __result)
            {
#if IFCE_DIAGNOSTICS
                FactoryDiagnostic diagnostic =
                    _diagnostics is not null &&
                    _diagnostics.Count > 0
                        ? _diagnostics.Pop()
                        : null;
                RegressionDiagnostics.AudioFactory(
                    diagnostic?.Name,
                    diagnostic?.Selection ?? false,
                    __result);
#endif
                Route route =
                    _routes is not null && _routes.Count > 0
                        ? _routes.Pop()
                        : null;
                if (route?.Song is null || __result is null)
                    return;
#if IFCE_DISABLE_CUSTOM_AUDIO
                // Keep the Prefix template-name mapping, but skip the
                // independent audio replacement in this diagnostic build.
                return;
#else
                var pending = new Pending(
                    __result,
                    route.Song,
                    route.Selection);
                if (TryGetNativeSound(__result, out _) &&
                    TryGetNativeChannel(__result, out _) &&
                    IsNativeAudioConfigured(__result))
                    TryReplace(pending);
                else
                    Waiting.Add(pending);
#endif
            }
        }

        private static void TryReplace(Pending pending)
        {
            try
            {
                Replace(pending);
            }
            catch (Exception exception)
            {
                ChartExtMod.Log.Error(
                    $"[CustomSong] audio replacement failed for " +
                    $"'{pending.Song.Config.Id}': {exception}");
            }
        }

        private static void Replace(Pending pending)
        {
            _Cg controller = pending.Controller;
            if (!TryGetNativeSound(
                    controller, out IntPtr oldSoundHandle) ||
                oldSoundHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    "controller has no native Sound");
            }

            IntPtr channelPointer = IntPtr.Add(
                controller.Pointer,
                NativeLayoutCatalog.CustomSong.AudioChannel);
            IntPtr soundPointer = IntPtr.Add(
                controller.Pointer,
                NativeLayoutCatalog.CustomSong.AudioSound);
            IntPtr oldChannelHandle =
                Marshal.ReadIntPtr(channelPointer);
            int pausedValue = 0;
            if (oldChannelHandle != IntPtr.Zero)
                FmodExports.GetPaused(
                    oldChannelHandle, out pausedValue);
            bool wasPaused = pausedValue != 0;

            IntPtr systemPointer = ResolveSystem(oldSoundHandle);
            if (systemPointer == IntPtr.Zero)
                throw new InvalidOperationException(
                    "FMOD system could not be resolved");
            var system = new Il2CppFMOD.System
            {
                handle = systemPointer,
            };

            string path = pending.Selection
                ? pending.Song.PreviewPath
                : pending.Song.AudioPath;
            if (string.IsNullOrWhiteSpace(path) ||
                !File.Exists(path))
            {
                throw new FileNotFoundException(
                    "custom audio was not found", path);
            }

            MODE mode = MODE._2D |
                        MODE.CREATESTREAM |
                        MODE.IGNORETAGS |
                        MODE.LOWMEM;
            if (IsMpegAudioFile(path))
                mode |= MODE.MPEGSEARCH;
            if (pending.Selection)
                mode |= MODE.ACCURATETIME;
            var exInfo = new CREATESOUNDEXINFO
            {
                cbsize = Marshal.SizeOf<CREATESOUNDEXINFO>(),
                decodebuffersize = 4096,
                ignoresetfilesystem = 1,
            };
            RESULT result = system.createSound(
                path, mode, ref exInfo, out Sound replacement);
            if ((int)result != 0 &&
                (mode & MODE.MPEGSEARCH) == 0)
            {
                result = system.createSound(
                    path,
                    mode | MODE.MPEGSEARCH,
                    ref exInfo,
                    out replacement);
            }
            if ((int)result != 0 ||
                replacement.handle == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    $"FMOD createSound failed for '{path}': {result}");
            }

            Il2CppFMOD.Channel replacementChannel = default;
            bool channelStarted = false;
            try
            {
                ChannelGroup group;
                if (oldChannelHandle != IntPtr.Zero)
                {
                    int groupResult = FmodExports.GetChannelGroup(
                        oldChannelHandle,
                        out IntPtr groupHandle);
                    if (groupResult != 0 ||
                        groupHandle == IntPtr.Zero)
                    {
                        throw new InvalidOperationException(
                            $"FMOD Channel_GetChannelGroup failed: " +
                            groupResult);
                    }
                    group = new ChannelGroup
                    {
                        handle = groupHandle,
                    };
                }
                else
                {
                    result = system.getMasterChannelGroup(out group);
                    if ((int)result != 0)
                    {
                        throw new InvalidOperationException(
                            "FMOD master channel group failed: " +
                            result);
                    }
                }

                int playResult = FmodExports.Play(
                    systemPointer,
                    replacement.handle,
                    group.handle,
                    wasPaused,
                    out IntPtr replacementChannelHandle);
                if (playResult != 0 ||
                    replacementChannelHandle == IntPtr.Zero)
                {
                    throw new InvalidOperationException(
                        $"FMOD playSound failed: {playResult}");
                }
                replacementChannel = new Channel
                {
                    handle = replacementChannelHandle,
                };
                channelStarted = true;

                ConfigureChannel(
                    replacementChannel,
                    pending,
                    wasPaused);

                if (oldChannelHandle != IntPtr.Zero &&
                    !ShouldDeferNativeRelease())
                    FmodExports.Stop(oldChannelHandle);
                Marshal.WriteIntPtr(
                    soundPointer, replacement.handle);
                Marshal.WriteIntPtr(
                    channelPointer, replacementChannel.handle);
                TrackOwned(
                    controller,
                    replacement.handle,
                    replacementChannel.handle,
                    pending.Selection);
                RegressionDiagnostics.AudioOwned(
                    "replace",
                    pending.Selection,
                    controller.Pointer,
                    replacement.handle,
                    replacementChannel.handle);

                ChartExtMod.Log.Msg(
                    $"[CustomSong] audio " +
                    $"{(pending.Selection ? "preview" : "song")} " +
                    $"active '{pending.Song.Config.Id}' -> " +
                    Path.GetFileName(path));
            }
            catch
            {
                if (channelStarted)
                    FmodExports.Stop(replacementChannel.handle);
                FmodExports.Release(replacement.handle);
                throw;
            }
        }

        private static void ConfigureChannel(
            Il2CppFMOD.Channel channel,
            Pending pending,
            bool paused)
        {
            if (pending.Selection)
            {
                uint start = ToMilliseconds(
                    pending.Song.PreviewStart);
                uint end = ToMilliseconds(
                    pending.Song.PreviewEnd);
                Require(
                    FmodExports.SetMode(
                        channel.handle,
                        (uint)(MODE._2D | MODE.LOOP_NORMAL)),
                    "Channel_SetMode");
                Require(
                    FmodExports.SetLoopCount(
                        channel.handle, -1),
                    "Channel_SetLoopCount");
                if (end > start)
                {
                    Require(
                        FmodExports.SetLoopPoints(
                            channel.handle,
                            start, (uint)TIMEUNIT.MS,
                            end, (uint)TIMEUNIT.MS),
                        "Channel_SetLoopPoints");
                }
                Require(
                    FmodExports.SetPosition(
                        channel.handle,
                        start, (uint)TIMEUNIT.MS),
                    "Channel_SetPosition");
            }
            else
            {
                Require(
                    FmodExports.SetMode(
                        channel.handle,
                        (uint)(MODE._2D | MODE.LOOP_OFF)),
                    "Channel_SetMode");
                Require(
                    FmodExports.SetPosition(
                        channel.handle, 0, (uint)TIMEUNIT.MS),
                    "Channel_SetPosition");
            }
            Require(
                FmodExports.SetPaused(
                    channel.handle, paused),
                "Channel_SetPaused");
        }

        private static void Require(
            int result,
            string operation)
        {
            if (result != 0)
            {
                throw new InvalidOperationException(
                    $"FMOD {operation} failed: {result}");
            }
        }

        private static bool TryGetNativeSound(
            _Cg controller,
            out IntPtr sound)
        {
            sound = IntPtr.Zero;
            if (controller is null ||
                controller.Pointer == IntPtr.Zero)
            {
                return false;
            }
            sound = Marshal.ReadIntPtr(
                IntPtr.Add(
                    controller.Pointer,
                    NativeLayoutCatalog.CustomSong.AudioSound));
            return sound != IntPtr.Zero;
        }

        private static bool TryGetNativeChannel(
            _Cg controller,
            out IntPtr channel)
        {
            channel = IntPtr.Zero;
            if (controller is null ||
                controller.Pointer == IntPtr.Zero)
            {
                return false;
            }
            channel = Marshal.ReadIntPtr(
                IntPtr.Add(
                    controller.Pointer,
                    NativeLayoutCatalog.CustomSong.AudioChannel));
            return channel != IntPtr.Zero;
        }

        private static bool IsNativeAudioConfigured(_Cg controller)
        {
            if (controller is null ||
                controller.Pointer == IntPtr.Zero)
            {
                return false;
            }
            return Marshal.ReadByte(
                IntPtr.Add(
                    controller.Pointer,
                    NativeLayoutCatalog.CustomSong.AudioConfigured)) != 0;
        }

        private static bool IsMpegAudioFile(string path)
        {
            try
            {
                using FileStream stream = File.OpenRead(path);
                Span<byte> header = stackalloc byte[3];
                int read = stream.Read(header);
                return read >= 3 &&
                       ((header[0] == (byte)'I' &&
                         header[1] == (byte)'D' &&
                         header[2] == (byte)'3') ||
                        (header[0] == 0xFF &&
                         (header[1] & 0xE0) == 0xE0));
            }
            catch
            {
                return false;
            }
        }

        private static IntPtr ResolveSystem(IntPtr sound)
        {
            if (_systemPointer != IntPtr.Zero)
                return _systemPointer;
            if (TryReadGameSystem(out _systemPointer))
                return _systemPointer;
            if (FmodExports.GetSystem(
                    sound, out IntPtr system) != 0)
                return IntPtr.Zero;
            _systemPointer = system;
            return _systemPointer;
        }

        private static bool TryReadGameSystem(out IntPtr system)
        {
            system = IntPtr.Zero;
            try
            {
                const BindingFlags flags =
                    BindingFlags.Static |
                    BindingFlags.Public |
                    BindingFlags.NonPublic;
                Type type = typeof(Il2Cpp_J._IF);
                object value =
                    type.GetProperty("_ZSA", flags)?.GetValue(null)
                    ?? type.GetField("_ZSA", flags)?.GetValue(null);
                if (value is Il2CppFMOD.System gameSystem)
                    system = gameSystem.handle;
            }
            catch
            {
                system = IntPtr.Zero;
            }
            return system != IntPtr.Zero;
        }

        private static void TrackOwned(
            _Cg controller,
            IntPtr sound,
            IntPtr channel,
            bool selection)
        {
            IntPtr controllerPointer = controller.Pointer;
            OwnedAudio replaced = null;
            for (int i = Owned.Count - 1; i >= 0; i--)
            {
                OwnedAudio previous = Owned[i];
                if (previous.ControllerPointer !=
                    controllerPointer)
                    continue;
                Owned.RemoveAt(i);
                if (replaced is not null)
                    ReleaseOwned(previous);
                replaced = previous;
            }
            Owned.Add(new OwnedAudio
            {
                Controller = new WeakReference<_Cg>(controller),
                ControllerPointer = controllerPointer,
                Sound = sound,
                Channel = channel,
                Selection = selection,
            });
            if (replaced is not null)
                ReleaseOwned(replaced);
        }

        private static void ReleaseDisposedControllerAudio()
        {
            for (int i = Owned.Count - 1; i >= 0; i--)
            {
                try
                {
                    OwnedAudio owned = Owned[i];
                    if (owned.Controller is null ||
                        !owned.Controller.TryGetTarget(
                            out _Cg controller) ||
                        controller is null ||
                        controller.Pointer == IntPtr.Zero ||
                        controller.Pointer !=
                        owned.ControllerPointer)
                    {
                        Owned.RemoveAt(i);
                        ReleaseOwned(owned);
                        continue;
                    }

                    IntPtr controllerPointer =
                        owned.ControllerPointer;
                    IntPtr currentSound = Marshal.ReadIntPtr(
                        controllerPointer +
                        NativeLayoutCatalog.CustomSong.AudioSound);
                    IntPtr currentChannel = Marshal.ReadIntPtr(
                        controllerPointer +
                        NativeLayoutCatalog.CustomSong.AudioChannel);
                    if (currentSound == owned.Sound)
                    {
                        // The native factory may return before its caller
                        // creates the gameplay Channel. Sound identity still
                        // proves that the controller uses our replacement;
                        // adopt the late Channel instead of releasing the
                        // active audio.
                        if (currentChannel != IntPtr.Zero &&
                            currentChannel != owned.Channel)
                        {
                            RegressionDiagnostics.AudioOwned(
                                "adopt-channel",
                                owned.Selection,
                                controllerPointer,
                                owned.Sound,
                                currentChannel);
                            owned.Channel = currentChannel;
                        }
                        continue;
                    }
                    Owned.RemoveAt(i);
                    ReleaseOwned(owned);
                }
                catch (Exception exception)
                {
                    OwnedAudio owned = Owned[i];
                    Owned.RemoveAt(i);
                    ReleaseOwned(owned);
                    if (_ownedErrors++ < 10)
                    {
                        ChartExtMod.Log.Warning(
                            "[CustomSong] audio cleanup fell back to " +
                            "owned handles: " + exception.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Mirrors the native gameplay pause lifecycle for Mod-owned song
        /// channels. Leaving gameplay pauses the channel instead of stopping
        /// it, so resuming can continue from the same FMOD position.
        /// </summary>
        private static void SynchronizeGameplayAudioPause()
        {
            if (Owned.Count == 0)
                return;
            bool gameplay =
                (byte)Il2Cpp_E._VD._a == 2;
            for (int i = 0; i < Owned.Count; i++)
            {
                OwnedAudio owned = Owned[i];
                if (owned.Selection || owned.Channel == IntPtr.Zero)
                    continue;
                if (gameplay)
                {
                    owned.GameplayStarted = true;
                    if (owned.PausedByExtension)
                        SetOwnedPaused(owned, false);
                    continue;
                }
                if (!owned.GameplayStarted || owned.PausedByExtension)
                    continue;
                SetOwnedPaused(owned, true);
            }
        }

        private static void SetOwnedPaused(
            OwnedAudio owned,
            bool paused)
        {
            // Latch the requested state before the native call. A transient
            // failure must not turn this into a per-frame retry loop.
            owned.PausedByExtension = paused;
            try
            {
                int result = FmodExports.SetPaused(
                    owned.Channel, paused);
                if (result != 0 && _pauseErrors++ < 10)
                {
                    ChartExtMod.Log.Warning(
                        "[CustomSong] gameplay audio pause=" +
                        $"{paused} failed: {result}");
                }
            }
            catch (Exception exception)
            {
                if (_pauseErrors++ < 10)
                {
                    ChartExtMod.Log.Warning(
                        "[CustomSong] gameplay audio pause=" +
                        $"{paused} failed: {exception.Message}");
                }
            }
        }

        private static void ReleaseAllOwned()
        {
            for (int i = 0; i < Owned.Count; i++)
                ReleaseOwned(Owned[i]);
            Owned.Clear();
        }

        private static void ReleaseOwned(OwnedAudio owned)
        {
            if (ShouldDeferNativeRelease())
                return;
            RegressionDiagnostics.AudioOwned(
                "release",
                owned.Selection,
                owned.ControllerPointer,
                owned.Sound,
                owned.Channel);
            try
            {
                if (owned.Channel != IntPtr.Zero)
                    FmodExports.Stop(owned.Channel);
            }
            catch
            {
            }
            try
            {
                if (owned.Sound != IntPtr.Zero)
                    FmodExports.Release(owned.Sound);
            }
            catch
            {
            }
        }

        private static bool ShouldDeferNativeRelease()
        {
            return _shutdownRequested;
        }

        private static uint ToMilliseconds(double seconds) =>
            seconds <= 0
                ? 0
                : checked((uint)Math.Min(
                    uint.MaxValue,
                    Math.Round(seconds * 1000d)));

        private sealed class Route
        {
            internal Route(
                CustomSongPlan song,
                bool selection)
            {
                Song = song;
                Selection = selection;
            }

            internal CustomSongPlan Song { get; }
            internal bool Selection { get; }
        }

#if IFCE_DIAGNOSTICS
        private sealed class FactoryDiagnostic
        {
            internal FactoryDiagnostic(
                string name,
                bool selection)
            {
                Name = name ?? string.Empty;
                Selection = selection;
            }

            internal string Name { get; }
            internal bool Selection { get; }
        }
#endif

        private sealed class Pending
        {
            internal Pending(
                _Cg controller,
                CustomSongPlan song,
                bool selection)
            {
                Controller = controller;
                Song = song;
                Selection = selection;
                Deadline = DateTime.UtcNow.AddSeconds(10);
            }

            internal _Cg Controller { get; }
            internal CustomSongPlan Song { get; }
            internal bool Selection { get; }
            internal DateTime Deadline { get; }
        }

        private sealed class OwnedAudio
        {
            internal WeakReference<_Cg> Controller;
            internal IntPtr ControllerPointer;
            internal IntPtr Sound;
            internal IntPtr Channel;
            internal bool Selection;
            internal bool GameplayStarted;
            internal bool PausedByExtension;
        }
    }
}
