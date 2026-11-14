// Global runtime lifecycle and owned-resource cleanup.
using System;
using InFalsusChartExt.CustomSong.Audio;

namespace InFalsusChartExt
{
    /// <summary>
    /// Central disposal boundary for objects and caches owned by this mod.
    /// Original game objects must never be destroyed here.
    /// </summary>
    internal static class ChartRuntimeLifecycle
    {
        private static bool _sceneCleanupPending;
        private static int _cleanupNotBeforeFrame;

        internal static void OnSceneLoaded(int buildIndex, string sceneName)
        {
        }

        internal static void MarkChartActive()
        {
            _sceneCleanupPending = false;
            AutoplayScoreGuard.BeginChartSession();
        }

        internal static void OnSceneUnloaded(int buildIndex, string sceneName)
        {
            AutoplayScoreGuard.OnSceneUnloaded();
            _sceneCleanupPending = true;
            _cleanupNotBeforeFrame = UnityEngine.Time.frameCount + 3;
        }

        internal static void Tick()
        {
            if (!_sceneCleanupPending) return;
            if (UnityEngine.Time.frameCount < _cleanupNotBeforeFrame) return;
            try
            {
                Il2Cppifapp.Game.Scenes.Game.Track track =
                    Il2Cppifapp.Game.Scenes.Game.Track._a;
                if (track != null && track.Pointer != IntPtr.Zero)
                    return;
            }
            catch { }
            if (BackgroundSkin.HasAttachedNativeMaterial())
            {
                // A delayed unload from the previous scene can arrive after the
                // new chart installed its cloned material. Keep ownership with
                // the active chart and retry only after a short backoff.
                _cleanupNotBeforeFrame = UnityEngine.Time.frameCount + 15;
                return;
            }
            Dispose();
            _sceneCleanupPending = false;
        }

        internal static void Dispose()
        {
            Dispose(disposeChartContext: true);
        }

        internal static void DisposeForChartTransform()
        {
            Dispose(disposeChartContext: false);
        }

        private static void Dispose(bool disposeChartContext)
        {
            _sceneCleanupPending = false;
            Run("sky-block-judge", SkyBlockJudgeRuntime.ResetForChartSwitch);
            Run("sky-block-visual", SkyBlockVisualRuntime.ResetForChartSwitch);
            Run("sky-block-line-glow", SkyBlockLineGlowRuntime.ResetForChartSwitch);
            Run("sky-block-slots", SkyBlockRuntimeSlots.Reset);
            if (disposeChartContext)
                Run(
                    "chart-context",
                    ChartRuntimeContext.ResetForChartSwitch);
            Run("showtext", ShowTextRuntime.DisposeResources);
            Run("bomb", BombSkinRuntime.DisposeForSceneUnload);
            Run("timing-group", TimingGroupRuntime.ResetForChartSwitch);
            Run("timing-group-visual", TimingGroupVisualRuntime.ResetForChartSwitch);
            Run("timing-noinput-sky", TimingNoInputSkyVisualRuntime.ResetForChartSwitch);
            Run("visual-notes", VisualNoteRuntime.ResetForChartSwitch);
            Run("normal-sky-edge", NormalSkyInternalEdgeRuntime.ResetForChartSwitch);
            Run("sky-color", SkyColorRuntime.ResetForChartSwitch);
            Run("scene-control", SceneControlRuntime.DisposeForSceneUnload);
            Run("track-move", TrackMoveRuntime.ResetForChartSwitch);
            Run("autoplay", AutoplayRuntime.ResetForChartSwitch);
            Run("audio-offset", AudioOffsetRuntime.ResetForChartSwitch);
            Run(
                "custom-song-audio",
                CustomSongAudioController.ReleaseOwnedForSceneUnload);
            Run("background-native", BackgroundNativeHook.ResetForChartSwitch);
            Run("background", BackgroundSkin.DisposeForSceneUnload);
            NativeNoteSlotTable.Reset();
        }

        private static void Run(string name, Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                try
                {
                    ChartExtMod.Log.Error(
                        $"[ChartExt] {name} cleanup failed: {e.Message}");
                }
                catch { }
            }
        }
    }
}
