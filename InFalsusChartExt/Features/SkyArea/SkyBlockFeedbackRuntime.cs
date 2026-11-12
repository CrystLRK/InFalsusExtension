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
        private static bool _holdActive;
        private static int _errors;

        internal static void PlayHit()
        {
            Dispatch(HitEventType, false);
        }

        internal static void SetSkyHoldActive(
            bool independentActive,
            bool nativeActive)
        {
            Il2Cppifapp.Game.Scenes.GameScene scene =
                Il2Cppifapp.Game.Scenes.GameScene._xn;
            if (scene is null || scene.Pointer == IntPtr.Zero)
                return;
            IntPtr bus = Marshal.ReadIntPtr(
                scene.Pointer +
                NativeLayoutCatalog.GameSceneAudio.HoldBus);
            bool running =
                bus != IntPtr.Zero &&
                Marshal.ReadInt32(
                    bus +
                    NativeLayoutCatalog.GameSceneAudio.HoldBusRunning) != 0;
            if (independentActive)
            {
                _holdActive = true;
                if (!nativeActive &&
                    !running &&
                    Dispatch(HoldEventType, true))
                {
                    _holdActive = true;
                }
                return;
            }

            if (nativeActive)
            {
                // Native event 5 owns the bus in a native-only state.
                _holdActive = false;
                return;
            }

            if (_holdActive && Dispatch(HoldEventType, false))
                _holdActive = false;
        }

        internal static void ResetForChartSwitch()
        {
            _errors = 0;
            _holdActive = false;
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
