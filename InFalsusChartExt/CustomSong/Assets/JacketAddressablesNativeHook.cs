using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace InFalsusChartExt.CustomSong.Assets
{
    /// <summary>
    /// Resolves mod-owned jacket markers without changing the shared generic
    /// Addressables method context used by unrelated resource loads.
    /// </summary>
    internal static class JacketAddressablesNativeHook
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct NativeAsyncOperationHandle
        {
            internal IntPtr InternalOperation;
            internal int Version;
            internal int Padding;
            internal IntPtr LocationName;
        }

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate IntPtr LoadAssetAsyncFn(
            IntPtr result,
            IntPtr instance,
            IntPtr key,
            IntPtr methodInfo);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate IntPtr AcquireFn(
            IntPtr result,
            ref NativeAsyncOperationHandle source,
            IntPtr methodInfo);

        private static readonly object Gate = new object();
        private static readonly Dictionary<IntPtr, Replacement>
            Replacements =
            new Dictionary<IntPtr, Replacement>();
        private static readonly LoadAssetAsyncFn DetourDelegate =
            LoadAssetDetour;

        private static MelonLoader.NativeUtils.NativeHook<
            LoadAssetAsyncFn> _hook;
        private static LoadAssetAsyncFn _trampoline;
        private static IntPtr _acquireMethodInfo;
        private static AcquireFn _acquire;
        private static bool _installed;

        internal static bool Installed => _installed;

        internal static bool Install()
        {
            lock (Gate)
            {
                if (_installed)
                    return true;
                try
                {
                    IntPtr loadTarget =
                        NativePatchTargetResolver.ResolveNative(
                            "addressablesMaterialByKey");
                    IntPtr acquireTarget =
                        NativePatchTargetResolver.ResolveNative(
                            "addressablesHandleAcquire");
                    IntPtr acquireMethod =
                        NativePatchTargetResolver.ResolveNativeMethod(
                            "addressablesHandleAcquire");
                    if (loadTarget == IntPtr.Zero ||
                        acquireTarget == IntPtr.Zero ||
                        acquireMethod == IntPtr.Zero)
                    {
                        throw new MissingMethodException(
                            "Addressables marker or handle-acquire " +
                            "native target is unavailable.");
                    }

                    _acquireMethodInfo = acquireMethod;
                    _acquire = Marshal.GetDelegateForFunctionPointer<
                        AcquireFn>(acquireTarget);
                    _hook = new MelonLoader.NativeUtils.NativeHook<
                        LoadAssetAsyncFn>();
                    _hook.Target = loadTarget;
                    _hook.Detour =
                        Marshal.GetFunctionPointerForDelegate(
                            DetourDelegate);
                    _hook.Attach();
                    if (!_hook.IsHooked)
                        throw new InvalidOperationException(
                            "Jacket Addressables native hook failed.");
                    _trampoline = _hook.Trampoline;
                    _installed = true;
                    ChartExtMod.Log.Msg(
                        "[CustomSong] jacket native marker hook " +
                        $"installed: load=0x{loadTarget.ToInt64():X}, " +
                        $"acquire=0x{acquireTarget.ToInt64():X}");
                    return true;
                }
                catch (Exception exception)
                {
                    try { _hook?.Detach(); }
                    catch { }
                    _hook = null;
                    _trampoline = null;
                    _acquire = null;
                    _acquireMethodInfo = IntPtr.Zero;
                    _installed = false;
                    ChartExtMod.Log.Error(
                        "[CustomSong] jacket native marker hook " +
                        "unavailable: " + exception.Message);
                    return false;
                }
            }
        }

        internal static object Register(
            AssetReferenceT<Material> marker,
            Material material,
            out NativeAsyncOperationHandle handle)
        {
            if (marker is null || marker.Pointer == IntPtr.Zero)
                throw new ArgumentNullException(nameof(marker));
            object root = CreateCompletedHandle(
                material, out handle);
            lock (Gate)
            {
                Replacements[marker.Pointer] =
                    new Replacement(handle, root);
            }
            return root;
        }

        internal static void Unregister(
            AssetReferenceT<Material> marker)
        {
            if (marker is null ||
                marker.Pointer == IntPtr.Zero)
            {
                return;
            }
            lock (Gate)
                Replacements.Remove(marker.Pointer);
        }

        internal static void Dispose()
        {
            lock (Gate)
            {
                Replacements.Clear();
                try { _hook?.Detach(); }
                catch { }
                _hook = null;
                _trampoline = null;
                _acquire = null;
                _acquireMethodInfo = IntPtr.Zero;
                _installed = false;
            }
        }

        private static IntPtr LoadAssetDetour(
            IntPtr result,
            IntPtr instance,
            IntPtr key,
            IntPtr methodInfo)
        {
            try
            {
                Replacement replacement;
                lock (Gate)
                {
                    if (key == IntPtr.Zero ||
                        !Replacements.TryGetValue(
                            key, out replacement))
                    {
                        replacement = default;
                    }
                }
                if (replacement.Handle.InternalOperation !=
                    IntPtr.Zero)
                {
                    NativeAsyncOperationHandle source =
                        replacement.Handle;
                    return _acquire(
                        result, ref source, _acquireMethodInfo);
                }
            }
            catch (Exception exception)
            {
                try
                {
                    ChartExtMod.Log.Error(
                        "[CustomSong] jacket marker acquire " +
                        "failed: " + exception.Message);
                }
                catch { }
            }

            return _trampoline(
                result, instance, key, methodInfo);
        }

        private static object CreateCompletedHandle(
            Material material,
            out NativeAsyncOperationHandle handle)
        {
            if (material is null ||
                material.Pointer == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(material));
            }
            object manager = Addressables.ResourceManager ??
                throw new InvalidOperationException(
                    "Addressables.ResourceManager is unavailable.");
            MethodInfo factory = manager.GetType()
                .GetMethods(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic)
                .FirstOrDefault(method =>
                    method.Name == "CreateCompletedOperation" &&
                    method.IsGenericMethodDefinition &&
                    method.GetParameters().Length == 2);
            if (factory is null)
            {
                throw new MissingMethodException(
                    manager.GetType().FullName,
                    "CreateCompletedOperation<T>(T,string)");
            }

            object boxed = factory
                .MakeGenericMethod(typeof(Material))
                .Invoke(
                    manager,
                    new object[] { material, null });
            if (boxed is not Il2CppObjectBase native ||
                native.Pointer == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    "CreateCompletedOperation<Material> did not " +
                    "return a native boxed handle.");
            }
            IntPtr value = IL2CPP.il2cpp_object_unbox(
                native.Pointer);
            if (value == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    "The completed Material handle could not be " +
                    "unboxed.");
            }

            handle = new NativeAsyncOperationHandle
            {
                InternalOperation = Marshal.ReadIntPtr(
                    value + NativeLayoutCatalog.Runtime
                        .OperationInternalHandle),
                Version = Marshal.ReadInt32(
                    value + NativeLayoutCatalog.Runtime
                        .OperationVersion),
                Padding = Marshal.ReadInt32(
                    value + NativeLayoutCatalog.Runtime
                        .OperationPadding),
                LocationName = Marshal.ReadIntPtr(
                    value + NativeLayoutCatalog.Runtime
                        .OperationLocationName),
            };
            if (handle.InternalOperation == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    "The completed Material handle has no internal " +
                    "operation.");
            }
            return boxed;
        }

        private readonly struct Replacement
        {
            internal Replacement(
                NativeAsyncOperationHandle handle,
                object root)
            {
                Handle = handle;
                Root = root;
            }

            internal NativeAsyncOperationHandle Handle { get; }
            internal object Root { get; }
        }
    }
}
