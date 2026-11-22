using System;
using System.Runtime.InteropServices;

namespace InFalsusChartExt
{
    /// <summary>
    /// Produces native _Sh events for independently judged SkyBlocks and sends
    /// them through GameScene._Mk. The original dispatcher owns throttling,
    /// sound settings, voice allocation and the shared SkyHold audio bus.
    /// </summary>
    internal static class SkyBlockFeedbackRuntime
    {
        private const string TargetKey = "gameSceneEventDispatch";
        private const byte HitEventType = 6;
        private const byte HoldEventType = 5;

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void EventDispatchCallback(
            IntPtr scene,
            IntPtr track,
            IntPtr eventBuffer);

        private static EventDispatchCallback _dispatch;
        private static IntPtr _eventBuffer;
        private static bool _resolutionAttempted;
        private static bool _resolutionFailedLogged;
        private static bool _managedActive;
        private static bool _autoplayActive;
        private static bool _busStartedByMod;
        private static int _lastStartFrame = -1;
        private static int _errors;

        internal static void PlayHit()
        {
            Dispatch(HitEventType, false);
        }

        internal static void SetSkyHoldActive(
            bool independentActive,
            bool nativeActive)
        {
            _managedActive = independentActive;
            UpdateHoldBus();
        }

        /// <summary>
        /// Autoplay record replacement bypasses the native sky hit path. This
        /// owner only controls the shared hold loop; hit events are dispatched
        /// separately when the first record is replaced.
        /// </summary>
        internal static void SetAutoplaySkyHoldActive(bool active)
        {
            _autoplayActive = active;
            UpdateHoldBus();
        }

        private static void UpdateHoldBus()
        {
            if (!TryGetHoldBusRunning(out bool running))
                return;
            bool desired = _managedActive || _autoplayActive;
            if (desired)
            {
                // Native may stop the shared bus after autoplay rewrites the
                // sky records. Re-arm it while an owner still wants the loop;
                // the end-of-owner path below is the only place that stops it.
                if (running)
                    return;
                int frame = UnityEngine.Time.frameCount;
                if (frame == _lastStartFrame)
                    return;
                _lastStartFrame = frame;
                if (Dispatch(HoldEventType, true))
                    _busStartedByMod = true;
                return;
            }
            // Only the owner that started the bus may stop it. Native-only
            // ownership is left untouched.
            if (_busStartedByMod && Dispatch(HoldEventType, false))
                _busStartedByMod = false;
        }

        private static bool TryGetHoldBusRunning(out bool running)
        {
            running = false;
            Il2Cppifapp.Game.Scenes.GameScene scene =
                Il2Cppifapp.Game.Scenes.GameScene._xn;
            if (scene is null || scene.Pointer == IntPtr.Zero)
                return false;
            IntPtr bus = Marshal.ReadIntPtr(
                scene.Pointer +
                NativeLayoutCatalog.GameSceneAudio.HoldBus);
            running =
                bus != IntPtr.Zero &&
                Marshal.ReadInt32(
                    bus +
                    NativeLayoutCatalog.GameSceneAudio.HoldBusRunning) != 0;
            return true;
        }

        internal static void ResetForChartSwitch()
        {
            if (_busStartedByMod)
                Dispatch(HoldEventType, false);
            _errors = 0;
            _managedActive = false;
            _autoplayActive = false;
            _busStartedByMod = false;
            _lastStartFrame = -1;
        }

        private static unsafe bool Dispatch(byte eventType, bool value)
        {
            if (!BuildProfile.Supports(
                    NativeCapability.SkyFeedback))
                return false;

            Il2Cppifapp.Game.Scenes.GameScene scene =
                Il2Cppifapp.Game.Scenes.GameScene._xn;
            if (scene is null || scene.Pointer == IntPtr.Zero)
                return false;
            if (!TryResolve())
                return false;

            int stride =
                NativeLayoutCatalog.GameSceneAudio.ShStride;
            if (stride <= 0 ||
                NativeLayoutCatalog.GameSceneAudio.ShType < 0 ||
                NativeLayoutCatalog.GameSceneAudio.ShType >= stride ||
                NativeLayoutCatalog.GameSceneAudio.ShBool < 0 ||
                NativeLayoutCatalog.GameSceneAudio.ShBool >= stride)
                return false;
            if (_eventBuffer == IntPtr.Zero)
                _eventBuffer = Marshal.AllocHGlobal(stride);

            try
            {
                byte* buffer = (byte*)_eventBuffer;
                for (int i = 0; i < stride; i++)
                    buffer[i] = 0;
                buffer[
                    NativeLayoutCatalog.GameSceneAudio.ShType] =
                    eventType;
                if (eventType == HoldEventType)
                {
                    buffer[
                        NativeLayoutCatalog.GameSceneAudio.ShBool] =
                        value ? (byte)1 : (byte)0;
                }
                _dispatch(
                    scene.Pointer,
                    IntPtr.Zero,
                    _eventBuffer);
                return true;
            }
            catch (Exception exception)
            {
                if (_errors++ < 10)
                {
                    ChartExtMod.Log.Error(
                        "[ChartExt] skyarea event dispatch failed: " +
                        exception);
                }
                return false;
            }
        }

        private static bool TryResolve()
        {
            if (_dispatch is not null)
                return true;
            if (_resolutionAttempted)
                return false;

            _resolutionAttempted = true;
            IntPtr entry =
                NativePatchTargetResolver.ResolveNative(TargetKey);
            if (entry == IntPtr.Zero)
            {
                if (!_resolutionFailedLogged)
                {
                    _resolutionFailedLogged = true;
                    ChartExtMod.Log.Error(
                        "[ChartExt] native GameScene event dispatcher " +
                        "is unavailable; independent SkyArea audio is disabled");
                }
                return false;
            }

            _dispatch =
                Marshal.GetDelegateForFunctionPointer<
                    EventDispatchCallback>(entry);
            return true;
        }
    }
}
