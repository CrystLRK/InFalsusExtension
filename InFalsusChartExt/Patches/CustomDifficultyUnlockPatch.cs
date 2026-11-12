using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Il2CppInterop.Runtime;
using Il2Cppifapp.Game.Data;
using InFalsusChartExt.CustomSong.Discovery;
using InFalsusChartExt.CustomSong.Lifecycle;
using MonoMod.RuntimeDetour;

namespace InFalsusChartExt
{
    /// <summary>
    /// Replaces only the profile-validated native _NH._uoA entry. Harmony
    /// cannot safely marshal its by-ref SongInfo argument on IL2CPP.
    /// </summary>
    internal static class CustomDifficultyUnlockPatch
    {
        private const int SongInfoBaseNameOffset = 8;
        private static readonly ConcurrentDictionary<ushort, byte>
            UnlockMasks =
                new ConcurrentDictionary<ushort, byte>();
        private static NativeDetour _detour;
        private static DifficultyStatusDelegate _original;
        private static DifficultyStatusDelegate _replacement;
        private static int _errors;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate byte DifficultyStatusDelegate(
            IntPtr instance,
            IntPtr dataAccess,
            IntPtr songInfo,
            byte difficulty,
            byte flags,
            IntPtr methodInfo);

        internal static void Install()
        {
            if (!BuildProfile.Supports(
                    NativeCapability.CustomDifficultyUnlock) ||
                _detour is not null)
                return;
            try
            {
                IntPtr target =
                    NativePatchTargetResolver.ResolveNative(
                        "difficultyStatus");
                if (target == IntPtr.Zero)
                    throw new MissingMethodException(
                        "difficultyStatus native entry was not resolved");
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
                        DifficultyStatusDelegate>();
                _detour.Apply();
                ChartExtMod.Log.Msg(
                    "[ChartExt] difficulty unlock native detour installed");
            }
            catch (Exception exception)
            {
                LogError("install", exception);
                Uninstall();
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
            UnlockMasks.Clear();
        }

        private static byte Detour(
            IntPtr instance,
            IntPtr dataAccess,
            IntPtr songInfo,
            byte difficulty,
            byte flags,
            IntPtr methodInfo)
        {
            byte original = _original(
                instance,
                dataAccess,
                songInfo,
                difficulty,
                flags,
                methodInfo);
            if (original == 0 || songInfo == IntPtr.Zero)
                return original;
            try
            {
                ushort songId =
                    (ushort)Marshal.ReadInt16(songInfo);
                if (!UnlockMasks.TryGetValue(
                        songId, out byte unlockMask))
                {
                    IntPtr baseNamePointer =
                        Marshal.ReadIntPtr(
                            songInfo + SongInfoBaseNameOffset);
                    string baseName =
                        baseNamePointer == IntPtr.Zero
                            ? string.Empty
                            : IL2CPP.Il2CppStringToManaged(
                                baseNamePointer);
                    unlockMask = BuildUnlockMask(baseName);
                    UnlockMasks.TryAdd(songId, unlockMask);
                }
                return (unlockMask & difficulty) != 0
                    ? (byte)Il2Cpp_K._Ch._vZA
                    : original;
            }
            catch (Exception exception)
            {
                LogError("detour", exception);
                return original;
            }
        }

        private static byte BuildUnlockMask(string baseName)
        {
            if (!CustomSongRuntime.TryGetSongByBaseName(
                    baseName,
                    out var song))
                return 0;
            byte mask = 0;
            for (int i = 0; i < song.Charts.Count; i++)
                mask |= (byte)song.Charts[i].Difficulty;
            return mask;
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
                    $"[ChartExt] difficulty unlock {stage} failed: " +
                    exception);
            }
            catch
            {
            }
        }
    }
}
