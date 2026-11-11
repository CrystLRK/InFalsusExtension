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
        private static GUIStyle _markerStyle;
        private static int _errors;

        internal static bool SuppressScores { get; set; } = true;

        internal static bool FreezeActive =>
            _freezeActive &&
            BuildProfile.Supports(
                NativeCapability.AutoplayScoreGuard);

        internal static bool ShouldSuppressResults =>
            _usedThisChart &&
            SuppressScores &&
            BuildProfile.Supports(
                NativeCapability.AutoplayScoreGuard);

        internal static void NoteAutoplayUsed()
        {
            if (_usedThisChart ||
                !SuppressScores ||
                !BuildProfile.Supports(
                    NativeCapability.AutoplayScoreGuard))
                return;
            try
            {
                Il2Cpp_E._VD judge =
                    Il2Cppifapp.Game.Scenes.GameScene._DN;
                if (judge == null ||
                    judge.Pointer == IntPtr.Zero)
                    return;
                _usedThisChart = true;
                _freezeActive = true;
                ChartExtMod.Log.Msg(
                    "[ChartExt] autoplay score suppression latched");
            }
            catch (Exception exception)
            {
                LogError(exception);
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
            if (!_showResultMarker)
                return;
            try
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
            catch (Exception exception)
            {
                LogError(exception);
            }
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
