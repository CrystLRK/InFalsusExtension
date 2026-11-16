using System;
using System.Runtime.InteropServices;
#if BEPINEX
using BepInEx.Unity.IL2CPP.Hook;
#endif

namespace InFalsusChartExt
{
    /// <summary>
    /// Loader-specific native detour facade. The call sites only need a
    /// target, a typed detour delegate and the original trampoline.
    /// </summary>
    internal sealed class NativeDetourHook<T>
        where T : Delegate
    {
        private IntPtr _target;
        private T _detourDelegate;
#if MELONLOADER
        private MelonLoader.NativeUtils.NativeHook<T> _hook;
#elif BEPINEX
        private INativeDetour _hook;
#endif

        internal IntPtr Target
        {
            set => _target = value;
        }

        internal T DetourDelegate
        {
            set => _detourDelegate = value;
        }

        internal T Trampoline { get; private set; }
        internal bool IsHooked { get; private set; }

        internal void Attach()
        {
            if (IsHooked) return;
            if (_target == IntPtr.Zero ||
                _detourDelegate is null)
                throw new InvalidOperationException(
                    "native detour target or delegate is missing");

#if MELONLOADER
            _hook =
                new MelonLoader.NativeUtils.NativeHook<T>();
            _hook.Target = _target;
            _hook.Detour =
                Marshal.GetFunctionPointerForDelegate(
                    _detourDelegate);
            _hook.Attach();
            Trampoline = _hook.Trampoline;
            IsHooked = _hook.IsHooked;
#elif BEPINEX
            _hook = INativeDetour.CreateAndApply(
                _target,
                _detourDelegate,
                out T trampoline);
            Trampoline = trampoline;
            IsHooked = _hook is not null;
#endif
        }

        internal void Detach()
        {
            if (!IsHooked) return;
            try
            {
#if MELONLOADER
                _hook?.Detach();
#elif BEPINEX
                if (_hook is IDisposable disposable)
                    disposable.Dispose();
#endif
            }
            catch
            {
            }
            _hook = null;
            Trampoline = null;
            IsHooked = false;
        }
    }
}
