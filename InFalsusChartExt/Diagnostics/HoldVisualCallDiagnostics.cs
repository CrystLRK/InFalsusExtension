#if IFCE_DIAGNOSTICS
using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2Cppifapp.Game.Scenes.Game;

namespace InFalsusChartExt
{
    /// <summary>
    /// Read-only argument probe for comparing the native Hold visual state with
    /// the independently instantiated noinput/noclip Hold visuals.
    /// </summary>
    [HarmonyPatch(typeof(HoldNoteV2), "_cZ")]
    internal static class HoldVisualCallDiagnostics
    {
        private static int _nativeLogs;
        private static int _visualLogs;
        private static readonly Dictionary<IntPtr, Snapshot> Snapshots =
            new Dictionary<IntPtr, Snapshot>();

        private struct Snapshot
        {
            internal float A0;
            internal float A1;
            internal float A2;
            internal bool A3;
            internal bool A5;
            internal double A6;
        }

        [HarmonyPrefix]
        private static void Prefix(
            HoldNoteV2 __instance,
            float __0,
            float __1,
            float __2,
            bool __3,
            int __4,
            bool __5,
            double __6)
        {
            string name = "";
            try
            {
                name = __instance?.gameObject?.name ?? "";
            }
            catch { }
            bool visual = name.StartsWith(
                "IFCE_VisualHold_",
                System.StringComparison.Ordinal);
            if (!IsGameplay())
                return;
            if (visual)
            {
                if (_visualLogs >= 120)
                    return;
            }
            else
            {
                if (_nativeLogs >= 24)
                    return;
            }
            IntPtr key = __instance?.Pointer ?? IntPtr.Zero;
            if (key == IntPtr.Zero)
                return;
            bool first = !Snapshots.TryGetValue(key, out Snapshot previous);
            bool changed = first ||
                Math.Abs(__0 - previous.A0) >= 0.1f ||
                Math.Abs(__1 - previous.A1) >= 0.1f ||
                Math.Abs(__2 - previous.A2) >=
                    (visual ? 0.02f : 0.2f) ||
                __3 != previous.A3 ||
                __5 != previous.A5 ||
                Math.Abs(__6 - previous.A6) >= 500.0;
            if (!changed)
                return;
            Snapshots[key] = new Snapshot
            {
                A0 = __0,
                A1 = __1,
                A2 = __2,
                A3 = __3,
                A5 = __5,
                A6 = __6,
            };
            if (visual)
                _visualLogs++;
            else
                _nativeLogs++;
            ChartExtMod.Log.Msg(
                "[r7-hold-call] " +
                $"name='{name}' ptr=0x{__instance?.Pointer.ToInt64():X} " +
                $"a0={__0:F3} a1={__1:F3} a2={__2:F3} " +
                $"a3={__3} a4={__4} a5={__5} a6={__6:F3}");
        }

        private static bool IsGameplay()
        {
            try
            {
                return (byte)Il2Cpp_E._VD._a == 2;
            }
            catch
            {
                return false;
            }
        }

        internal static void Reset()
        {
            _nativeLogs = 0;
            _visualLogs = 0;
            Snapshots.Clear();
        }
    }
}
#endif
