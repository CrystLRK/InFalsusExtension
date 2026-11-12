using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace InFalsusChartExt
{
    /// <summary>
    /// Background substitution follows InFalsusChartLoader's background-only
    /// route: arm the reference returned by GameplayBackgrounds._UmA, let the
    /// game load its own Material through AddressableHandleAutoReleaser._LIA,
    /// then return a clone of that Material with the custom main texture.
    /// </summary>
    internal static unsafe class BackgroundNativeHook
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr UmaFn(IntPtr backgrounds, int background, IntPtr methodInfo);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr LiaFn(IntPtr owner, IntPtr reference, IntPtr methodInfo);

        private static NativeHook<UmaFn> _umaHook;
        private static NativeHook<LiaFn> _liaHook;
        private static UmaFn _umaTrampoline;
        private static LiaFn _liaTrampoline;
        private static readonly UmaFn _umaDetourDelegate = UmaDetour;
        private static readonly LiaFn _liaDetourDelegate = LiaDetour;
        private static IntPtr _armedReference;
        private static Texture _armedTexture;
        private static int _installErrors;
        private static int _loadLogs;

        internal static bool Installed { get; private set; }

        internal static void Install()
        {
            if (Installed) return;
            try
            {
                IntPtr umaTarget =
                    NativePatchTargetResolver.ResolveNative(
                        "backgroundUma");
                IntPtr liaTarget =
                    NativePatchTargetResolver.ResolveNative(
                        "backgroundLia");
                if (umaTarget == IntPtr.Zero || liaTarget == IntPtr.Zero)
                    throw new MissingMethodException(
                        $"native background methods missing (Uma=0x{umaTarget.ToInt64():X}, " +
                        $"Lia=0x{liaTarget.ToInt64():X})");

                _umaHook = new NativeHook<UmaFn>();
                _umaHook.Target = umaTarget;
                _umaHook.Detour = Marshal.GetFunctionPointerForDelegate(_umaDetourDelegate);
                _umaHook.Attach();
                if (!_umaHook.IsHooked)
                    throw new InvalidOperationException("GameplayBackgrounds._UmA native hook failed");
                _umaTrampoline = _umaHook.Trampoline;

                _liaHook = new NativeHook<LiaFn>();
                _liaHook.Target = liaTarget;
                _liaHook.Detour = Marshal.GetFunctionPointerForDelegate(_liaDetourDelegate);
                _liaHook.Attach();
                if (!_liaHook.IsHooked)
                    throw new InvalidOperationException(
                        "AddressableHandleAutoReleaser._LIA native hook failed");
                _liaTrampoline = _liaHook.Trampoline;

                Installed = true;
                ChartExtMod.Log.Msg(
                    $"[ChartExt] native background hooks installed: _UmA=0x{umaTarget.ToInt64():X}, " +
                    $"_LIA=0x{liaTarget.ToInt64():X}");
            }
            catch (Exception e)
            {
                try { _umaHook?.Detach(); } catch { }
                try { _liaHook?.Detach(); } catch { }
                _umaHook = null;
                _liaHook = null;
                _umaTrampoline = null;
                _liaTrampoline = null;
                Installed = false;
                if (_installErrors++ < 3)
                    ChartExtMod.Log.Warning(
                        "[ChartExt] native background hooks unavailable; legacy lookup remains: " +
                        e.Message);
            }
        }

        internal static void ResetForChartSwitch()
        {
            _armedReference = IntPtr.Zero;
            _armedTexture = null;
        }

        internal static void Dispose()
        {
            try { _liaHook?.Detach(); }
            catch (Exception e)
            {
                ChartExtMod.Log.Warning(
                    "[ChartExt] background _LIA detach failed: " +
                    e.Message);
            }
            try { _umaHook?.Detach(); }
            catch (Exception e)
            {
                ChartExtMod.Log.Warning(
                    "[ChartExt] background _UmA detach failed: " +
                    e.Message);
            }
            _liaHook = null;
            _umaHook = null;
            _liaTrampoline = null;
            _umaTrampoline = null;
            _armedReference = IntPtr.Zero;
            _armedTexture = null;
            Installed = false;
        }

        private static IntPtr UmaDetour(IntPtr backgrounds, int background, IntPtr methodInfo)
        {
            IntPtr reference = _umaTrampoline(backgrounds, background, methodInfo);
            try
            {
                _armedReference = IntPtr.Zero;
                _armedTexture = null;
                if (!BackgroundSkin.Enabled || reference == IntPtr.Zero) return reference;
                Texture texture = BackgroundSkin.NativeTextureForCurrentChart();
                if (texture == null || texture.Pointer == IntPtr.Zero) return reference;
                _armedReference = reference;
                _armedTexture = texture;
                ChartExtMod.Log.Msg(
                    $"[ChartExt] background armed: ref=0x{reference.ToInt64():X}, " +
                    $"key='{ChartExtState.LastChartName}', background={background}");
            }
            catch (Exception e)
            {
                if (_loadLogs++ < 5)
                    ChartExtMod.Log.Warning("[ChartExt] background _UmA arm failed: " + e.Message);
            }
            return reference;
        }

        private static IntPtr LiaDetour(IntPtr owner, IntPtr reference, IntPtr methodInfo)
        {
            bool claimed =
                BackgroundSkin.Enabled &&
                reference != IntPtr.Zero &&
                reference == _armedReference &&
                _armedTexture != null;
            Texture texture = claimed ? _armedTexture : null;
            if (claimed)
            {
                _armedReference = IntPtr.Zero;
                _armedTexture = null;
            }

            // Claim before entering the loader so a nested load cannot steal
            // this one-shot handshake.
            IntPtr gameMaterial = _liaTrampoline(owner, reference, methodInfo);
            try
            {
                if (_loadLogs++ < 3)
                    ChartExtMod.Log.Msg(
                        $"[ChartExt] background _LIA: ref=0x{reference.ToInt64():X}, " +
                        $"claimed={claimed}, " +
                        $"material=0x{gameMaterial.ToInt64():X}");
                if (!claimed || gameMaterial == IntPtr.Zero)
                    return gameMaterial;

                if (texture == null || texture.Pointer == IntPtr.Zero) return gameMaterial;
                IntPtr clone = BackgroundSkin.CloneNativeBackground(gameMaterial, texture);
                return clone == IntPtr.Zero ? gameMaterial : clone;
            }
            catch (Exception e)
            {
                if (_loadLogs++ < 5)
                    ChartExtMod.Log.Warning("[ChartExt] background _LIA clone failed: " + e.Message);
                return gameMaterial;
            }
        }

        private sealed class NativeHook<T> where T : Delegate
        {
            private readonly MelonLoader.NativeUtils.NativeHook<T> _hook = new();

            public IntPtr Target
            {
                set => _hook.Target = value;
            }

            public IntPtr Detour
            {
                set => _hook.Detour = value;
            }

            public T Trampoline => _hook.Trampoline;

            public bool IsHooked => _hook.IsHooked;

            public void Attach() => _hook.Attach();

            public void Detach()
            {
                if (_hook.IsHooked) _hook.Detach();
            }
        }
    }
}
