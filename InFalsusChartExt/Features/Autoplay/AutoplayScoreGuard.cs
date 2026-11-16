using System;
using UnityEngine;

namespace InFalsusChartExt
{
    /// <summary>
    /// Latches per-run Autoplay score suppression and owns the result-page
    /// marker. Save interception is installed separately in Harmony patches.
    /// </summary>
    internal static class AutoplayScoreGuard
    {
        private static bool _usedThisChart;
        private static bool _freezeActive;
        private static bool _showResultMarker;
        private static bool _guardUnavailable;
        private static float _guardWarningUntil;
        private static GUIStyle _markerStyle;
        private static GUIStyle _warningStyle;
        private static int _errors;

        internal static bool SuppressScores { get; set; } = true;

        internal static bool FreezeActive =>
            _freezeActive;

        internal static bool ShouldSuppressResults =>
            _usedThisChart &&
            SuppressScores;

        /// <summary>
        /// Clears an unfinished previous run when a new gameplay chart starts.
        /// ResultsScene owns the normal end-of-run transition; using the chart
        /// boundary avoids mistaking a transient scene transition for a result
        /// screen while still preventing state leakage into the next chart.
        /// </summary>
        internal static void BeginChartSession()
        {
            if (_usedThisChart || _freezeActive)
            {
                try
                {
                    ChartExtMod.Log.Warning(
                        "[ChartExt] discarded unfinished autoplay score " +
                        "guard state at the next chart boundary");
                }
                catch
                {
                }
            }
            _usedThisChart = false;
            _freezeActive = false;
            _showResultMarker = false;
        }

        /// <summary>
        /// Returns false when score suppression was requested but cannot be
        /// established. Before a GameScene exists the caller should keep
        /// waiting; once it exists, failure must disable Autoplay.
        /// </summary>
        internal static bool EnsureRunGuard()
        {
            if (!SuppressScores)
                return true;
            if (!BuildProfile.Supports(
                    NativeCapability.AutoplayScoreGuard))
            {
                MarkGuardUnavailable(
                    "score guard capability is disabled");
                return false;
            }
            if (_usedThisChart)
                return true;
            try
            {
                Il2Cpp_E._VD judge =
                    Il2Cppifapp.Game.Scenes.GameScene._DN;
                if (judge == null ||
                    judge.Pointer == IntPtr.Zero)
                    return true;
                if (!AutoplayResultsUpdateNativePatch.Install())
                {
                    MarkGuardUnavailable(
                        "native results detour could not be installed");
                    return false;
                }
                _usedThisChart = true;
                _freezeActive = true;
                _guardUnavailable = false;
                ChartExtMod.Log.Msg(
                    "[ChartExt] autoplay score suppression latched");
                return true;
            }
            catch (Exception exception)
            {
                LogError(exception);
                MarkGuardUnavailable(exception.Message);
                return false;
            }
        }

        internal static void CompleteResultsStart()
        {
            if (_usedThisChart && SuppressScores)
            {
                _showResultMarker = true;
                ChartExtMod.Log.Msg(
                    "[ChartExt] autoplay result marker enabled");
            }
            _freezeActive = false;
            _usedThisChart = false;
        }

        internal static void OnSceneUnloaded()
        {
            _showResultMarker = false;
        }

        internal static void OnGUI()
        {
            try
            {
                if (_showResultMarker)
                {
                    if (_markerStyle == null)
                    {
                        _markerStyle = new GUIStyle
                        {
                            alignment = TextAnchor.UpperRight,
                            fontSize = 30,
                            fontStyle = FontStyle.Bold,
                        };
                        _markerStyle.normal.textColor =
                            new Color(1f, 0.86f, 0.25f, 1f);
                    }
                    GUI.Label(
                        new Rect(
                            Math.Max(0f, Screen.width - 320f),
                            18f,
                            288f,
                            50f),
                        "AUTOPLAY",
                        _markerStyle);
                }
                if (_guardUnavailable &&
                    Time.realtimeSinceStartup <
                    _guardWarningUntil)
                {
                    if (_warningStyle == null)
                    {
                        _warningStyle = new GUIStyle
                        {
                            alignment = TextAnchor.UpperCenter,
                            fontSize = 22,
                            fontStyle = FontStyle.Bold,
                        };
                        _warningStyle.normal.textColor =
                            new Color(1f, 0.42f, 0.42f, 1f);
                    }
                    GUI.Label(
                        new Rect(
                            Math.Max(20f, Screen.width * 0.1f),
                            70f,
                            Screen.width * 0.8f,
                            52f),
                        "AUTOPLAY DISABLED: SCORE GUARD UNAVAILABLE",
                        _warningStyle);
                }
            }
            catch (Exception exception)
            {
                LogError(exception);
            }
        }

        private static void MarkGuardUnavailable(string reason)
        {
            _usedThisChart = true;
            _freezeActive = true;
            if (!_guardUnavailable)
            {
                ChartExtMod.Log.Error(
                    "[ChartExt] autoplay disabled because score " +
                    "suppression is unavailable: " + reason);
            }
            _guardUnavailable = true;
            _guardWarningUntil =
                Time.realtimeSinceStartup + 8f;
        }

        private static void LogError(Exception exception)
        {
            if (_errors++ >= 5)
                return;
            try
            {
                ChartExtMod.Log.Error(
                    "[ChartExt] autoplay score guard failed: " +
                    exception);
            }
            catch
            {
            }
        }
    }
}
