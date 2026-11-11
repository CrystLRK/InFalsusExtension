using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace InFalsusChartExt
{
    internal static class NativePatchTargetResolver
    {
        private static readonly object Gate = new object();
        private static readonly Dictionary<string, Resolution>
            ManagedCache =
            new Dictionary<string, Resolution>(
                StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Resolution>
            NativeCache =
            new Dictionary<string, Resolution>(
                StringComparer.OrdinalIgnoreCase);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(string name);

        internal static MethodBase ResolveHarmony(string key)
        {
            Resolution resolution = ResolveManaged(key);
            if (!resolution.Success)
                return null;
            return resolution.ManagedMethod;
        }

        internal static bool IsAvailable(string key) =>
            ResolveManaged(key).Success;

        internal static IntPtr ResolveNative(string key)
        {
            Resolution resolution = ResolveNativeTarget(key);
            if (!resolution.Success)
                return IntPtr.Zero;
            return resolution.NativeEntry;
        }

        internal static IntPtr ResolveNativeMethod(string key)
        {
            Resolution resolution = ResolveNativeTarget(key);
            if (!resolution.Success)
                return IntPtr.Zero;
            return resolution.NativeMethod;
        }

        internal static bool ValidateManagedTargets(
            out string error)
        {
            try
            {
                if (NativeAbiProfileStore.Selected is null)
                    _ = BuildProfile.Current;
                NativeAbiProfile profile =
                    NativeAbiProfileStore.Selected
                    ?? throw new InvalidOperationException(
                        "native profile is unavailable");
                foreach (KeyValuePair<string, NativePatchTargetProfile>
                         item in profile.PatchTargets)
                {
                    _ = ResolveManagedMethod(item.Value);
                }
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        private static Resolution ResolveManaged(string key)
        {
            lock (Gate)
            {
                if (ManagedCache.TryGetValue(
                        key, out Resolution cached))
                    return cached;
                Resolution resolution = ResolveManagedCore(key);
                ManagedCache[key] = resolution;
                if (!resolution.Success)
                {
                    NativePatchTargetProfile profile =
                        GetProfile(key);
                    if (profile is not null)
                    {
                        BuildProfile.DisableCapabilities(
                            profile.Capabilities,
                            $"{key}: {resolution.Error}");
                    }
                }
                return resolution;
            }
        }

        private static Resolution ResolveManagedCore(string key)
        {
            try
            {
                NativePatchTargetProfile target =
                    GetRequiredProfile(key);
                MethodBase managed = ResolveManagedMethod(target);
                Type managedType =
                    managed is MethodInfo methodInfo &&
                    methodInfo.IsGenericMethod
                        ? methodInfo
                            .GetGenericMethodDefinition()
                            .DeclaringType
                        : managed.DeclaringType;

                IntPtr classPointer =
                    Il2CppClassPointerStore.GetNativeClassPointer(
                        managedType);
                if (classPointer == IntPtr.Zero)
                {
                    return Failure(
                        "native class pointer was not found");
                }
                if (!ClassIdentityMatches(
                        classPointer, target))
                {
                    return Failure(
                        "native class identity does not match profile");
                }

                return new Resolution(
                    true,
                    managed,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    null);
            }
            catch (Exception exception)
            {
                return Failure(exception.Message);
            }
        }

        private static Resolution ResolveNativeTarget(string key)
        {
            lock (Gate)
            {
                if (NativeCache.TryGetValue(
                        key, out Resolution cached))
                    return cached;

                Resolution managed = ResolveManaged(key);
                if (!managed.Success)
                    return managed;

                Resolution resolution;
                try
                {
                    NativePatchTargetProfile target =
                        GetRequiredProfile(key);
                    IntPtr classPointer =
                        Il2CppClassPointerStore
                            .GetNativeClassPointer(
                                managed.ManagedMethod.DeclaringType);
                    if (!TryResolveNativeEntry(
                            classPointer,
                            target,
                            out IntPtr nativeEntry,
                            out IntPtr nativeMethod,
                            out string nativeError))
                    {
                        resolution = Failure(nativeError);
                    }
                    else
                    {
                        resolution = new Resolution(
                            true,
                            managed.ManagedMethod,
                            nativeEntry,
                            nativeMethod,
                            null);
                    }
                }
                catch (Exception exception)
                {
                    resolution = Failure(exception.Message);
                }

                NativeCache[key] = resolution;
                if (!resolution.Success)
                {
                    NativePatchTargetProfile profile =
                        GetProfile(key);
                    if (profile is not null)
                    {
                        BuildProfile.DisableCapabilities(
                            profile.Capabilities,
                            $"{key}: {resolution.Error}");
                    }
                }
                return resolution;
            }
        }

        private static MethodBase ResolveManagedMethod(
            NativePatchTargetProfile target)
        {
            Type managedType = ResolveManagedType(
                target.ManagedType);
            if (managedType is null)
            {
                throw new MissingMemberException(
                    "managed type not found: " + target.ManagedType);
            }

            const BindingFlags flags =
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.DeclaredOnly;
            if (target.ManagedMetadataToken != 0)
            {
                MethodBase tokenMethod =
                    managedType.Module.ResolveMethod(
                        target.ManagedMetadataToken)
                    ?? throw new MissingMethodException(
                        managedType.FullName,
                        "managed metadata token did not resolve");
                if (tokenMethod.DeclaringType != managedType ||
                    !MethodMatchesProfile(tokenMethod, target))
                {
                    throw new MissingMethodException(
                        managedType.FullName,
                        "managed metadata token/signature mismatch");
                }
                return CloseGenericMethod(
                    tokenMethod, target);
            }

            IEnumerable<MethodBase> methods =
                string.Equals(
                    target.Method,
                    ".ctor",
                    StringComparison.Ordinal)
                    ? managedType.GetConstructors(flags)
                    : managedType.GetMethods(flags);
            var matches = new List<MethodBase>();
            foreach (MethodBase method in methods)
            {
                if (!MethodMatchesProfile(method, target))
                    continue;
                matches.Add(method);
            }
            if (matches.Count == 0)
            {
                throw new MissingMethodException(
                    target.ManagedType + "." + target.Method,
                    "managed signature does not match profile");
            }

            MethodBase managed = matches
                .OrderBy(method => method.MetadataToken)
                .First();
            return CloseGenericMethod(managed, target);
        }

        private static bool MethodMatchesProfile(
            MethodBase method,
            NativePatchTargetProfile target)
        {
            if (!string.Equals(
                    method.Name,
                    target.Method,
                    StringComparison.Ordinal))
                return false;
            int genericArity =
                method is MethodInfo methodInfo
                    ? methodInfo.GetGenericArguments().Length
                    : 0;
            return genericArity == target.GenericArity &&
                   SignatureMatches(method, target);
        }

        private static MethodBase CloseGenericMethod(
            MethodBase managed,
            NativePatchTargetProfile target)
        {
            if (target.GenericArguments.Length == 0)
                return managed;

            Type[] arguments = target.GenericArguments
                .Select(ResolveManagedType)
                .ToArray();
            if (Array.Exists(arguments, type => type is null))
            {
                throw new TypeLoadException(
                    "generic argument type was not found");
            }
            if (managed is not MethodInfo genericMethod)
            {
                throw new TypeLoadException(
                    "non-method generic target is unsupported");
            }
            return genericMethod.MakeGenericMethod(arguments);
        }

        private static bool TryResolveNativeEntry(
            IntPtr classPointer,
            NativePatchTargetProfile target,
            out IntPtr entry,
            out IntPtr selectedMethod,
            out string error)
        {
            entry = IntPtr.Zero;
            selectedMethod = IntPtr.Zero;
            error = null;
            IL2CPP.il2cpp_runtime_class_init(classPointer);
            var candidates = new List<IntPtr>();
            IntPtr iterator = IntPtr.Zero;
            while (true)
            {
                IntPtr method =
                    IL2CPP.il2cpp_class_get_methods(
                        classPointer, ref iterator);
                if (method == IntPtr.Zero)
                    break;
                IntPtr namePointer =
                    IL2CPP.il2cpp_method_get_name(method);
                if (namePointer == IntPtr.Zero)
                    continue;
                string name =
                    Marshal.PtrToStringAnsi(namePointer);
                if (!string.Equals(
                        name,
                        target.Method,
                        StringComparison.Ordinal))
                {
                    continue;
                }
                if (IL2CPP.il2cpp_method_get_param_count(method) !=
                    target.ParameterTypes.Length)
                {
                    continue;
                }
                candidates.Add(method);
            }

            if (target.GenericArguments.Length > 0)
            {
                foreach (IntPtr candidate in candidates)
                {
                    IntPtr candidateEntry =
                        IL2CPP.il2cpp_method_is_inflated(candidate)
                            ? Marshal.ReadIntPtr(candidate)
                            : InflateNativeMethod(
                                candidate,
                                classPointer,
                                target.GenericArguments,
                                out _);
                    if (candidateEntry == IntPtr.Zero)
                        continue;
                    if (!EntryMatches(
                            candidateEntry, target))
                        continue;
                    entry = candidateEntry;
                    selectedMethod = candidate;
                    break;
                }
            }
            else
            {
                foreach (IntPtr candidate in candidates)
                {
                    IntPtr candidateEntry =
                        Marshal.ReadIntPtr(candidate);
                    if (candidateEntry == IntPtr.Zero)
                        continue;
                    if (!EntryMatches(
                            candidateEntry, target))
                        continue;
                    entry = candidateEntry;
                    selectedMethod = candidate;
                    break;
                }
            }

            if (entry == IntPtr.Zero)
            {
                error =
                    $"native method/RVA not matched for " +
                    $"{target.Namespace}.{target.Class}." +
                    $"{target.Method}";
                return false;
            }
            return true;
        }

        private static bool EntryMatches(
            IntPtr entry,
            NativePatchTargetProfile target)
        {
            long rva = ToRva(entry);
            bool matchedFallback = false;
            if (target.Rva != 0 ||
                target.FallbackRva != 0)
            {
                if (rva != target.Rva &&
                    rva != target.FallbackRva)
                {
                    return false;
                }
                matchedFallback =
                    target.FallbackRva != 0 &&
                    rva == target.FallbackRva;
            }
            byte[] expectedPrologue =
                matchedFallback &&
                target.FallbackPrologue.Length > 0
                    ? target.FallbackPrologue
                    : target.Prologue;
            for (int i = 0;
                 i < expectedPrologue.Length;
                 i++)
            {
                if (Marshal.ReadByte(entry, i) !=
                    expectedPrologue[i])
                    return false;
            }
            return true;
        }

        private static IntPtr InflateNativeMethod(
            IntPtr method,
            IntPtr ownerClass,
            string[] genericArgumentNames,
            out string error)
        {
            error = null;
            try
            {
                Type[] managedArguments = genericArgumentNames
                    .Select(ResolveManagedType)
                    .ToArray();
                if (Array.Exists(
                        managedArguments,
                        type => type is null))
                {
                    error = "generic native argument type not found";
                    return IntPtr.Zero;
                }
                var nativeArguments =
                    new Il2CppReferenceArray<Il2CppSystem.Type>(
                        managedArguments.Length);
                for (int i = 0;
                     i < managedArguments.Length;
                     i++)
                {
                    if (managedArguments[i] != typeof(Material))
                    {
                        error =
                            "unsupported native generic argument: " +
                            managedArguments[i].FullName;
                        return IntPtr.Zero;
                    }
                    nativeArguments[i] =
                        Il2CppType.Of<Material>();
                }

                IntPtr reflection =
                    IL2CPP.il2cpp_method_get_object(
                        method, ownerClass);
                if (reflection == IntPtr.Zero)
                {
                    error = "native method reflection was null";
                    return IntPtr.Zero;
                }
                var reflected =
                    new Il2CppSystem.Reflection.MethodInfo(
                        reflection);
                Il2CppSystem.Reflection.MethodInfo closed =
                    reflected.MakeGenericMethod(
                        nativeArguments);
                if (closed is null ||
                    closed.Pointer == IntPtr.Zero)
                {
                    error = "native generic method inflation failed";
                    return IntPtr.Zero;
                }
                IntPtr closedMethod =
                    IL2CPP.il2cpp_method_get_from_reflection(
                        closed.Pointer);
                IntPtr closedEntry = closedMethod == IntPtr.Zero
                    ? IntPtr.Zero
                    : Marshal.ReadIntPtr(closedMethod);
                if (closedEntry == IntPtr.Zero)
                    error = "inflated native method has no entry";
                return closedEntry;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return IntPtr.Zero;
            }
        }

        private static bool SignatureMatches(
            MethodBase method,
            NativePatchTargetProfile target)
        {
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length != target.ParameterTypes.Length)
                return false;
            Type returnType =
                method is MethodInfo methodInfo
                    ? methodInfo.ReturnType
                    : typeof(void);
            if (!string.Equals(
                    NativeTypeName.Normalize(returnType),
                    target.ReturnType,
                    StringComparison.Ordinal))
            {
                return false;
            }
            for (int i = 0; i < parameters.Length; i++)
            {
                if (!string.Equals(
                        NativeTypeName.Normalize(
                            parameters[i].ParameterType),
                        target.ParameterTypes[i],
                        StringComparison.Ordinal))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool ClassIdentityMatches(
            IntPtr classPointer,
            NativePatchTargetProfile target)
        {
            string name = Marshal.PtrToStringAnsi(
                IL2CPP.il2cpp_class_get_name(classPointer));
            string namespaceName = Marshal.PtrToStringAnsi(
                IL2CPP.il2cpp_class_get_namespace(classPointer));
            IntPtr image = IL2CPP.il2cpp_class_get_image(
                classPointer);
            string assemblyName = image == IntPtr.Zero
                ? string.Empty
                : Marshal.PtrToStringAnsi(
                    IL2CPP.il2cpp_image_get_name(image));
            return string.Equals(
                       name,
                       target.Class,
                       StringComparison.Ordinal) &&
                   string.Equals(
                       namespaceName ?? string.Empty,
                       target.Namespace,
                       StringComparison.Ordinal) &&
                   string.Equals(
                       NormalizeAssemblyName(assemblyName),
                       NormalizeAssemblyName(target.Assembly),
                       StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeAssemblyName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;
            return value.EndsWith(
                    ".dll",
                    StringComparison.OrdinalIgnoreCase)
                ? value.Substring(0, value.Length - 4)
                : value;
        }

        private static Type ResolveManagedType(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;
            foreach (Assembly assembly in
                     AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    Type type = assembly.GetType(
                        name, false);
                    if (type is not null)
                        return type;
                }
                catch
                {
                }
            }
            return null;
        }

        private static NativePatchTargetProfile GetRequiredProfile(
            string key)
        {
            NativePatchTargetProfile target = GetProfile(key);
            return target ?? throw new InvalidOperationException(
                "patch target is missing from profile: " + key);
        }

        private static NativePatchTargetProfile GetProfile(
            string key)
        {
            if (NativeAbiProfileStore.Selected is null)
            {
                // Harmony PatchAll can run before OnInitializeMelon. Resolve
                // the process profile first so target availability is still
                // fail-closed and hash-selected.
                _ = BuildProfile.Current;
            }
            NativeAbiProfile profile =
                NativeAbiProfileStore.Selected;
            if (profile is null)
                return null;
            profile.PatchTargets.TryGetValue(
                key, out NativePatchTargetProfile target);
            return target;
        }

        private static long ToRva(IntPtr entry)
        {
            IntPtr module = GetModuleHandle("GameAssembly.dll");
            if (module == IntPtr.Zero)
                throw new InvalidOperationException(
                    "GameAssembly module handle is unavailable");
            return entry.ToInt64() - module.ToInt64();
        }

        private static Resolution Failure(string error) =>
            new Resolution(
                false,
                null,
                IntPtr.Zero,
                IntPtr.Zero,
                error);

        private sealed class Resolution
        {
            internal Resolution(
                bool success,
                MethodBase managedMethod,
                IntPtr nativeEntry,
                IntPtr nativeMethod,
                string error)
            {
                Success = success;
                ManagedMethod = managedMethod;
                NativeEntry = nativeEntry;
                NativeMethod = nativeMethod;
                Error = error;
            }

            internal bool Success { get; }
            internal MethodBase ManagedMethod { get; }
            internal IntPtr NativeEntry { get; }
            internal IntPtr NativeMethod { get; }
            internal string Error { get; }
        }
    }

    internal static class NativeTypeName
    {
        internal static string Normalize(Type type)
        {
            if (type.IsByRef)
                return Normalize(type.GetElementType()) + "&";
            if (type.IsArray)
                return Normalize(type.GetElementType()) + "[]";
            if (type.IsGenericParameter)
                return "!" + type.GenericParameterPosition;
            if (type.IsGenericType)
            {
                Type definition = type.GetGenericTypeDefinition();
                string name =
                    definition.FullName ?? definition.Name;
                int suffix = name.IndexOf('`');
                if (suffix >= 0)
                    name = name.Substring(0, suffix);
                return name + "<" +
                       string.Join(
                           ",",
                           type.GetGenericArguments()
                               .Select(Normalize)) +
                       ">";
            }
            return type.FullName ?? type.Name;
        }
    }
}
