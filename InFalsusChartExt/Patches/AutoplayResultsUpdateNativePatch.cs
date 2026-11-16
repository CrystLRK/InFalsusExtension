using System;
using System.Runtime.InteropServices;
using Il2Cppifapp.Game.Data;
using MonoMod.RuntimeDetour;

namespace InFalsusChartExt
{
    /// <summary>
    /// Replaces only the profile-validated native GameResultsV4.TryUpdate
    /// entry. Harmony cannot safely marshal its three by-ref IL2CPP arguments.
    /// </summary>
    internal static class AutoplayResultsUpdateNativePatch
    {
        private static NativeDetour _detour;
        private static UpdateResultDelegate _original;
        private static UpdateResultDelegate _replacement;
        private static int _errors;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int UpdateResultDelegate(
            IntPtr instance,
            IntPtr songInfo,
            IntPtr result,
            IntPtr existingScore,
            IntPtr methodInfo);

        internal static bool Install()
        {
            if (!BuildProfile.Supports(
                    NativeCapability.AutoplayScoreGuard))
                return false;
            if (_detour is not null)
                return true;
            try
            {
                IntPtr target =
                    NativePatchTargetResolver.ResolveNative(
                        "gameResultsTryUpdate");
                if (target == IntPtr.Zero)
                    throw new MissingMethodException(
                        "gameResultsTryUpdate native entry was not resolved");
                _replacement = Detour;
                _detour = new NativeDetour(
                    target,
                    Marshal.GetFunctionPointerForDelegate(_replacement),
                    new NativeDetourConfig
                    {
                        ManualApply = true,
                    });
                _original =
                    _detour.GenerateTrampoline<
                        UpdateResultDelegate>();
                _detour.Apply();
                ChartExtMod.Log.Msg(
                    "[ChartExt] autoplay results native detour installed");
                return true;
            }
            catch (Exception exception)
            {
                LogError("install", exception);
                BuildProfile.DisableCapabilities(
                    NativeCapability.AutoplayScoreGuard,
                    "autoplay results native detour install failed: " +
                    exception.Message);
                Uninstall();
                return false;
            }
        }

        internal static void Uninstall()
        {
            try { _detour?.Undo(); }
            catch { }
            try { _detour?.Free(); }
            catch { }
            _detour = null;
            _original = null;
            _replacement = null;
        }

        private static int Detour(
            IntPtr instance,
            IntPtr songInfo,
            IntPtr result,
            IntPtr existingScore,
            IntPtr methodInfo)
        {
            try
            {
                if (AutoplayScoreGuard.ShouldSuppressResults)
                {
                    if (existingScore != IntPtr.Zero)
                        Marshal.WriteInt64(existingScore, 0);
                    return (int)GameResultsV4.UpdateResult.NoChange;
                }

                UpdateResultDelegate original = _original;
                if (original is null)
                {
                    LogError(
                        "detour",
                        new InvalidOperationException(
                            "original trampoline was unavailable"));
                    return (int)GameResultsV4.UpdateResult.NoChange;
                }
                return original(
                    instance,
                    songInfo,
                    result,
                    existingScore,
                    methodInfo);
            }
            catch (Exception exception)
            {
                LogError("detour", exception);
                return (int)GameResultsV4.UpdateResult.NoChange;
            }
        }

        private static void LogError(
            string stage,
            Exception exception)
        {
            if (_errors++ >= 5)
                return;
            try
            {
                ChartExtMod.Log.Error(
                    $"[ChartExt] autoplay results {stage} failed: " +
                    exception);
            }
            catch
            {
            }
        }
    }
}
