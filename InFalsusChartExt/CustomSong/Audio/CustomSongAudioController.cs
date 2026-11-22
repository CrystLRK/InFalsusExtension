using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using HarmonyLib;
using InFalsusChartExt.CustomSong.Discovery;
using InFalsusChartExt.CustomSong.Lifecycle;
#if MELONLOADER
using Il2Cpp_J;
#elif BEPINEX
using _J;
#endif
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
#if MELONLOADER
using Il2CppFMOD;
#elif BEPINEX
using FMOD;
#endif

namespace InFalsusChartExt.CustomSong.Audio
{
    internal static class CustomSongAudioController
    {
        private static readonly List<Pending> Waiting =
            new List<Pending>();
        private static readonly List<DeferredRoute> Deferred =
            new List<DeferredRoute>();
        private static readonly List<OwnedAudio> Owned =
            new List<OwnedAudio>();
        private static readonly List<OwnedAudio> Detached =
            new List<OwnedAudio>();
        private static IntPtr _systemPointer;
        private static int _ownedErrors;
        private static int _pauseErrors;
        private static bool _shutdownRequested;
        private static bool _sceneUnloadPending;
        private static bool _isPlayingUnavailable;
        [ThreadStatic]
        private static Stack<Route> _routes;
#if IFCE_DIAGNOSTICS
        [ThreadStatic]
        private static Stack<FactoryDiagnostic> _diagnostics;
#endif
        internal static void Reset()
        {
            for (int i = 0; i < Waiting.Count; i++)
            {
                if (Waiting[i].Selection)
                    SilenceTemplateChannel(
                        Waiting[i].Controller);
            }
            for (int i = 0; i < Deferred.Count; i++)
                SilenceTemplateChannel(
                    Deferred[i].Controller);
            Waiting.Clear();
            Deferred.Clear();
            _routes?.Clear();
            _ownedErrors = 0;
            _pauseErrors = 0;
            ReleaseAllOwned();
            ReleaseDetached();
            _sceneUnloadPending = false;
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
            PollDetached();
            ReleaseDisposedControllerAudio();
            ReleaseSelectionOwnedIfGameplayActive();
            SynchronizeGameplayAudioPause();
            RegressionDiagnostics.AudioTick();
            ResolveDeferred();
            for (int i = Waiting.Count - 1; i >= 0; i--)
            {
                Pending pending = Waiting[i];
                if (pending.Controller is null ||
                    pending.Controller.Pointer == IntPtr.Zero ||
                    pending.Deadline < DateTime.UtcNow)
                {
                    Waiting.RemoveAt(i);
                    if (pending.Selection)
                        SilenceTemplateChannel(
                            pending.Controller);
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

        /// <summary>
        /// Marks a scene unload so the delayed chart cleanup can detach
        /// gameplay audio instead of stopping it.
        /// </summary>
        internal static void OnSceneUnloaded()
        {
            _sceneUnloadPending = true;
        }

        /// <summary>
        /// A new chart owns the gameplay bus again. Any channel left playing
        /// from the previous chart must be stopped before the new song starts.
        /// </summary>
        internal static void OnChartActivated()
        {
            _sceneUnloadPending = false;
            ReleaseDetached();
        }

        internal static void ReleaseOwnedForSceneUnload()
        {
            for (int i = 0; i < Waiting.Count; i++)
            {
                if (Waiting[i].Selection)
                    SilenceTemplateChannel(
                        Waiting[i].Controller);
            }
            for (int i = 0; i < Deferred.Count; i++)
                SilenceTemplateChannel(
                    Deferred[i].Controller);
            Waiting.Clear();
            Deferred.Clear();
            _ownedErrors = 0;
            _pauseErrors = 0;
            // A chart/scene cleanup can run while the song-select preview
            // controller is still the active UI owner. Selection preview
            // remains Mod-owned until the next selection, gameplay handoff,
            // or the song-select scene is destroyed.
            //
            // Native `_Cg.Dispose` only clears its Sound/Channel fields; it
            // deliberately does not stop playback. On a real scene unload the
            // gameplay channel must therefore be detached and allowed to run
            // to its natural end. Paused-away audio is the exception: leaving
            // gameplay from the pause menu must not resume the song.
            for (int i = Owned.Count - 1; i >= 0; i--)
            {
                OwnedAudio owned = Owned[i];
                if (owned.Selection)
                    continue;
                Owned.RemoveAt(i);
                if (_sceneUnloadPending &&
                    !owned.PausedByExtension)
                {
                    Detached.Add(owned);
                }
                else
                {
                    ReleaseOwned(owned);
                }
            }
            _sceneUnloadPending = false;
            RegressionDiagnostics.Reset();
        }

        private static void PollDetached()
        {
            if (Detached.Count == 0 || _isPlayingUnavailable)
                return;
            for (int i = Detached.Count - 1; i >= 0; i--)
            {
                OwnedAudio owned = Detached[i];
                bool finished = owned.Channel == IntPtr.Zero;
                if (!finished)
                {
                    try
                    {
                        int result = FmodExports.IsPlaying(
                            owned.Channel,
                            out int playing);
                        // A failed query must not be interpreted as "ended";
                        // OnChartActivated/Reset still release the handle.
                        finished = result == 0 && playing == 0;
                    }
                    catch
                    {
                        _isPlayingUnavailable = true;
                        return;
                    }
                }
                if (!finished)
                    continue;
                Detached.RemoveAt(i);
                ReleaseOwned(owned);
            }
        }

        private static void ReleaseDetached()
        {
            for (int i = 0; i < Detached.Count; i++)
                ReleaseOwned(Detached[i]);
            Detached.Clear();
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
                // Deferred resolution must look up the custom song by the
                // name the native caller asked for, not by the template alias
                // substituted below.
                string requestedName = __0;
#if IFCE_DIAGNOSTICS
                _diagnostics ??= new Stack<FactoryDiagnostic>();
                _diagnostics.Push(
                    new FactoryDiagnostic(__0, __6));
#endif
                _routes ??= new Stack<Route>();
                if (!BuildProfile.Supports(
                        NativeCapability.CustomSong))
                {
                    _routes.Push(new Route(
                        null,
                        __6,
                        requestedName,
                        false));
                    return;
                }
                bool installed =
                    CustomSongRuntime.IsInstalled;
                if (!CustomSongRuntime.TryGetSongForAudioName(
                        requestedName, out CustomSongPlan song))
                {
                    // A non-custom factory request means the user moved to an
                    // official preview or official gameplay. The selection
                    // preview is Mod-owned and must not outlive that handoff.
                    ReleaseSelectionOwned();
                    _routes.Push(new Route(
                        null,
                        __6,
                        requestedName,
                        __6 && !installed));
                    return;
                }
                if (!string.IsNullOrWhiteSpace(
                        song.TemplateAudioName))
                {
                    __0 = song.TemplateAudioName;
                }
                _routes.Push(new Route(
                    song, __6, requestedName, false));
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
                if (route is null || __result is null)
                    return;
                if (route.Song is null)
                {
                    if (route.Selection &&
                        route.PendingCustom)
                    {
                        Deferred.Add(new DeferredRoute(
                            __result,
                            route.RequestedName));
#if IFCE_DIAGNOSTICS
                        ChartExtMod.Log.Msg(
                            "[CustomSong] preview deferred " +
                            $"name='{route.RequestedName}'");
#endif
                    }
                    return;
                }
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

        private static void ResolveDeferred()
        {
            for (int i = Deferred.Count - 1; i >= 0; i--)
            {
                DeferredRoute deferred = Deferred[i];
                if (deferred.Controller is null ||
                    deferred.Controller.Pointer == IntPtr.Zero ||
                    deferred.Deadline < DateTime.UtcNow)
                {
                    Deferred.RemoveAt(i);
                    SilenceTemplateChannel(
                        deferred.Controller);
#if IFCE_DIAGNOSTICS
                    ChartExtMod.Log.Msg(
                        "[CustomSong] preview deferred expired " +
                        $"name='{deferred.RequestedName}'");
#endif
                    continue;
                }
                if (!CustomSongRuntime.TryGetSongForAudioName(
                        deferred.RequestedName,
                        out CustomSongPlan song))
                    continue;
                Deferred.RemoveAt(i);
#if IFCE_DIAGNOSTICS
                ChartExtMod.Log.Msg(
                    "[CustomSong] preview deferred resolved " +
                    $"name='{deferred.RequestedName}' " +
                    $"song='{song.Config.Id}'");
#endif
                var pending = new Pending(
                    deferred.Controller,
                    song,
                    true);
                if (TryGetNativeSound(
                        deferred.Controller,
                        out _) &&
                    TryGetNativeChannel(
                        deferred.Controller,
                        out _) &&
                    IsNativeAudioConfigured(deferred.Controller))
                    TryReplace(pending);
                else
                    Waiting.Add(pending);
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
                // A failed preview must not fall back to the template song
                // (for example the stock "a la mode").
                if (pending.Selection)
                    SilenceTemplateChannel(
                        pending.Controller);
            }
        }

        /// <summary>
        /// Called from the native audio-worker callback before it reads the
        /// controller's Sound. Installing the custom Sound here makes the
        /// worker cache the custom length instead of the template length.
        /// </summary>
        internal static void PrepareBeforeAudioWorker(_Cg controller)
        {
            if (controller is null ||
                controller.Pointer == IntPtr.Zero)
                return;
            if (TryGetNativeSound(controller, out IntPtr currentSound) &&
                IsOwnedSound(currentSound))
                return;
            for (int i = Waiting.Count - 1; i >= 0; i--)
            {
                Pending pending = Waiting[i];
                if (pending.Selection ||
                    pending.Controller is null ||
                    pending.Controller.Pointer != controller.Pointer)
                    continue;
                if (!TryGetNativeSound(controller, out _))
                    return;
                Waiting.RemoveAt(i);
                TryReplace(pending);
                return;
            }
            // `_bl` may run before the factory Postfix has registered the
            // pending route. The active chart still identifies the custom
            // song, so install it here as well.
            if (!CustomSongRuntime.SelectionCurrent)
                return;
            CustomChartPlan activeChart =
                CustomSongRuntime.ActiveChart;
            CustomSongPlan active = activeChart?.Song;
            if (active is null)
                return;
            if (!TryGetNativeSound(controller, out _))
                return;
            TryReplace(new Pending(
                controller, active, false));
        }

        internal static bool TryGetGameplayLengthMs(out uint lengthMs)
        {
            lengthMs = 0;
            for (int i = 0; i < Owned.Count; i++)
            {
                OwnedAudio owned = Owned[i];
                if (owned.Selection ||
                    owned.Sound == IntPtr.Zero)
                    continue;
                try
                {
                    if (FmodExports.GetLength(
                            owned.Sound,
                            out uint length,
                            (uint)TIMEUNIT.MS) == 0 &&
                        length > 0)
                    {
                        lengthMs = length;
                        return true;
                    }
                }
                catch
                {
                }
            }
            return false;
        }

        private static void Replace(Pending pending)
        {
            _Cg controller = pending.Controller;
            FmodExports.PrepareSoundLifecycle();
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
            if (pending.Selection)
                wasPaused = false;
            uint resumePositionMs = 0;
            bool inheritedPosition = false;
            if (oldChannelHandle != IntPtr.Zero &&
                !pending.Selection)
            {
                // Only a controller that already holds one of our own
                // replacements may donate its position (pause/resume
                // recreation). The initial template Sound is unrelated and
                // may have been playing for seconds while the controller was
                // being configured; inheriting it would start the custom
                // song mid-track and end the chart early.
                inheritedPosition = IsOwnedSound(oldSoundHandle);
                if (inheritedPosition &&
                    FmodExports.GetPosition(
                        oldChannelHandle,
                        out uint position,
                        (uint)TIMEUNIT.MS) == 0)
                    resumePositionMs = position;
            }
            IntPtr systemPointer = ResolveSystem(oldSoundHandle);
            if (systemPointer == IntPtr.Zero)
                throw new InvalidOperationException(
                    "FMOD system could not be resolved");
            var system = new Il2CppFMOD.System
            {
                handle = systemPointer,
            };

            string path = pending.Selection
                ? pending.Song.PreviewFor(
                    DifficultySlot(pending.Song))
                : pending.Song.AudioFor(
                    DifficultySlot(pending.Song));
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
            {
                // MP3 (especially VBR) needs both MPEG search and accurate
                // time for pause/resume seeking; without ACCURATETIME the
                // resumed position drifts.
                mode |= MODE.MPEGSEARCH | MODE.ACCURATETIME;
            }
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
                    wasPaused,
                    resumePositionMs);
#if IFCE_DIAGNOSTICS
                if (pending.Selection)
                {
                    FmodExports.GetPaused(
                        replacementChannel.handle,
                        out int diagnosticPaused);
                    FmodExports.GetPosition(
                        replacementChannel.handle,
                        out uint diagnosticPosition,
                        (uint)TIMEUNIT.MS);
                    ChartExtMod.Log.Msg(
                        "[preview-audio] " +
                        $"pausedIn={pausedValue} " +
                        $"pausedNow={diagnosticPaused} " +
                        $"positionMs={diagnosticPosition}");
                }
#endif

                if (oldChannelHandle != IntPtr.Zero &&
                    !ShouldDeferNativeRelease())
                    FmodExports.Stop(oldChannelHandle);
                if (pending.Selection)
                {
                    // The first song-select controller may be disposed almost
                    // immediately after the factory callback. Keep selection
                    // audio in Mod-owned lifecycle instead of publishing its
                    // handles into that short-lived controller.
                    TrackSelectionOwned(
                        replacement.handle,
                        replacementChannel.handle,
                        controller.Pointer);
                }
                else
                {
                    ReleaseSelectionOwned();
                    Marshal.WriteIntPtr(
                        soundPointer, replacement.handle);
                    Marshal.WriteIntPtr(
                        channelPointer, replacementChannel.handle);
                    TrackOwned(
                        controller,
                        replacement.handle,
                        replacementChannel.handle,
                        false);
                }
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
            bool paused,
            uint resumePositionMs)
        {
            if (pending.Selection)
            {
                int slot = DifficultySlot(pending.Song);
                uint start = ToMilliseconds(
                    pending.Song.PreviewStartFor(slot));
                uint end = ToMilliseconds(
                    pending.Song.PreviewEndFor(slot));
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
                int positionResult = FmodExports.SetPosition(
                    channel.handle,
                    resumePositionMs,
                    (uint)TIMEUNIT.MS);
                if (positionResult != 0 && resumePositionMs != 0)
                {
                    // A shorter replacement sound may reject the old
                    // position; fall back to the start instead of failing
                    // the whole replacement.
                    positionResult = FmodExports.SetPosition(
                        channel.handle,
                        0,
                        (uint)TIMEUNIT.MS);
                }
                Require(positionResult, "Channel_SetPosition");
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

        private static bool IsOwnedSound(IntPtr sound)
        {
            if (sound == IntPtr.Zero)
                return false;
            for (int i = 0; i < Owned.Count; i++)
            {
                if (Owned[i].Sound == sound)
                    return true;
            }
            return false;
        }


        private static void TrackSelectionOwned(
            IntPtr sound,
            IntPtr channel,
            IntPtr controllerPointer)
        {
            ReleaseSelectionOwned();
            Owned.Add(new OwnedAudio
            {
                Controller = new WeakReference<_Cg>(null),
                ControllerPointer = controllerPointer,
                Sound = sound,
                Channel = channel,
                Selection = true,
            });
        }

        internal static void ReleaseSelectionOwned()
        {
            for (int i = Owned.Count - 1; i >= 0; i--)
            {
                OwnedAudio owned = Owned[i];
                if (!owned.Selection)
                    continue;
                Owned.RemoveAt(i);
                ReleaseOwned(owned);
            }
        }

        /// <summary>
        /// Stops the template Sound the native factory created for a custom
        /// preview. Used when the custom replacement cannot be installed, so
        /// the user hears silence instead of the wrong song.
        /// </summary>
        private static void SilenceTemplateChannel(_Cg controller)
        {
            try
            {
                if (controller is null ||
                    controller.Pointer == IntPtr.Zero)
                    return;
                IntPtr channel = Marshal.ReadIntPtr(
                    controller.Pointer +
                    NativeLayoutCatalog.CustomSong.AudioChannel);
                if (channel != IntPtr.Zero)
                    FmodExports.Stop(channel);
            }
            catch
            {
            }
        }

        private static void ReleaseSelectionOwnedIfGameplayActive()
        {
            bool hasSelection = false;
            for (int i = 0; i < Owned.Count; i++)
            {
                if (!Owned[i].Selection)
                    continue;
                hasSelection = true;
                break;
            }
            if (!hasSelection)
                return;

            bool gameplay = false;
            try
            {
                Il2Cppifapp.Game.Scenes.GameScene scene =
                    Il2Cppifapp.Game.Scenes.GameScene._xn;
                gameplay =
                    scene != null &&
                    scene.Pointer != IntPtr.Zero &&
                    scene.gameObject != null &&
                    scene.gameObject.activeInHierarchy;
                if (!gameplay)
                {
                    Il2Cppifapp.Game.Scenes.Game.Track track =
                        Il2Cppifapp.Game.Scenes.Game.Track._a;
                    gameplay =
                        track != null &&
                        track.Pointer != IntPtr.Zero &&
                        track.gameObject != null &&
                        track.gameObject.activeInHierarchy;
                }
            }
            catch
            {
                return;
            }
            if (gameplay)
                ReleaseSelectionOwned();
        }

        private static void ReleaseDisposedControllerAudio()
        {
            for (int i = Owned.Count - 1; i >= 0; i--)
            {
                try
                {
                    OwnedAudio owned = Owned[i];
                    if (owned.Selection)
                        continue;
                    // A gameplay scene may already have destroyed its
                    // controller while the chart cleanup still waits for the
                    // track to disappear. Do not stop the song during that
                    // window; ReleaseOwnedForSceneUnload decides between
                    // detaching and stopping.
                    if (_sceneUnloadPending)
                        continue;
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
        /// channels. Only the pause layer freezes the channel; leaving
        /// gameplay keeps the native "play to the end" behaviour.
        /// </summary>
        private static void SynchronizeGameplayAudioPause()
        {
            if (Owned.Count == 0)
                return;
            bool gameplay =
                (byte)Il2Cpp_E._VD._a == 2;
            // The native chart clock freezes while the pause layer is still
            // visible (including the resume fade). Resuming the Mod-owned
            // channel earlier makes the game seek it back when the clock
            // resumes, which users hear as the music jumping backwards.
            bool pauseLayerActive =
                TimingGroupRuntime.IsPauseLayerActive();
            bool running =
                gameplay && !pauseLayerActive;
            for (int i = 0; i < Owned.Count; i++)
            {
                OwnedAudio owned = Owned[i];
                if (owned.Selection || owned.Channel == IntPtr.Zero)
                    continue;
                if (gameplay)
                    owned.GameplayStarted = true;
                if (running)
                {
                    if (owned.PausedByExtension &&
                        !owned.PauseUnavailable)
                        SetOwnedPaused(owned, false);
                    continue;
                }
                // Only the pause menu may pause Mod-owned gameplay audio.
                // Chart-end/results transitions must leave the song playing,
                // matching native `_Cg` (whose Dispose never stops it).
                if (!pauseLayerActive)
                    continue;
                if (!owned.GameplayStarted)
                    continue;
                if (owned.PauseUnavailable)
                    continue;
                // Re-assert every frame: the native side may resume the
                // channel as soon as the chart clock starts, while the pause
                // layer is still fading out.
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
                // FMOD_ERR_INVALID_HANDLE: the native controller already
                // stopped/released this channel (for example during the
                // results transition). Do not retry every frame.
                if (result == 30)
                {
                    owned.PauseUnavailable = true;
                    return;
                }
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

        private static int DifficultySlot(CustomSongPlan song)
        {
            if (SongSelectionState.HasSelection &&
                SongSelectionState.SongId ==
                song.RuntimeSongId)
                return SongSelectionState.DifficultySlot;
            CustomChartPlan chart = CustomSongRuntime.ActiveChart;
            if (chart is not null &&
                ReferenceEquals(chart.Song, song))
                return chart.Slot;
            return 0;
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
                bool selection,
                string requestedName,
                bool pendingCustom)
            {
                Song = song;
                Selection = selection;
                RequestedName = requestedName ?? string.Empty;
                PendingCustom = pendingCustom;
            }

            internal CustomSongPlan Song { get; }
            internal bool Selection { get; }
            internal string RequestedName { get; }
            internal bool PendingCustom { get; }
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

        private sealed class DeferredRoute
        {
            internal DeferredRoute(
                _Cg controller,
                string requestedName)
            {
                Controller = controller;
                RequestedName = requestedName ?? string.Empty;
                Deadline = DateTime.UtcNow.AddSeconds(10);
            }

            internal _Cg Controller { get; }
            internal string RequestedName { get; }
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
            internal bool PauseUnavailable;
        }
    }
}
