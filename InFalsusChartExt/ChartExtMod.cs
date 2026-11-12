// Code organization: sole source-root entry point. Runtime behavior is unchanged.
using MelonLoader;
using InFalsusChartExt.CustomSong.Audio;
using InFalsusChartExt.CustomSong.Lifecycle;

[assembly: MelonInfo(typeof(InFalsusChartExt.ChartExtMod), "InFalsus Chart Ext", "0.1.0-patch2", "IFCE")]
[assembly: MelonGame("lowiro", "infalsus")]

namespace InFalsusChartExt
{
    public sealed class ChartExtMod : MelonMod
    {
        internal static MelonLogger.Instance Log { get; private set; }

        public override void OnInitializeMelon()
        {
            Log = LoggerInstance;
            BuildProfile.Initialize();
            CustomDifficultyUnlockPatch.Install();

            // 谱面容量上限（4096 → 65536）：必须在任何谱面数据容器创建之前改写 il2cpp 立即数。
            NumericLimitPatch.Apply();

            // MelonLoader already runs PatchAll for every [HarmonyPatch] class in this assembly
            // during HarmonyInit; patching again here would register every prefix twice.

            Log.Msg("[ChartExt] loaded; patches installed.");
            OptionsOverlay.Init();

            try
            {
                if (BuildProfile.Current.SupportsBackgroundNativeHook)
                    BackgroundNativeHook.Install();
                else
                    Log.Warning("[BuildProfile] native background hook disabled for this build.");
            }
            catch (System.Exception e)
            {
                Log.Error("[ChartExt] native background hook install failed: " + e);
            }

        }

        public override void OnUpdate()
        {
            ChartRuntimeLifecycle.Tick();
            NativeNoteSlotTable.Refresh();
            AutoplayRuntime.Tick();
            RegressionDiagnostics.GameplayStateTick(
                Il2Cppifapp.Game.Scenes.Game.Track._a);
            RegressionDiagnostics.AudioTick();
            RegressionDiagnostics.TickInput(
                Il2Cppifapp.Game.Scenes.Game.Track._a);
            AudioOffsetRuntime.Tick();
            OptionsOverlay.Tick();
            DisplayModeRuntime.Tick();
            RunMergedSubsystem("installer", () =>
                CustomSongRuntime.Tick());
        }

        /// <summary>showtext 的 IMGUI 绘制（与谱面时间同步的出现/渐变/消失）。</summary>
        public override void OnGUI()
        {
            ShowTextRuntime.OnGUI();
            OptionsOverlay.OnGUI();
            AutoplayScoreGuard.OnGUI();
            DisplayModeRuntime.DrawWarning();
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            ChartRuntimeLifecycle.OnSceneLoaded(buildIndex, sceneName);
        }

        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            ChartRuntimeLifecycle.OnSceneUnloaded(buildIndex, sceneName);
        }

        public void OnApplicationFocus(bool focus)
        {
            DisplayModeRuntime.OnApplicationFocus(focus);
        }

        public override void OnApplicationQuit()
        {
            CustomSongAudioController.PrepareForShutdown();
        }

        public override void OnDeinitializeMelon()
        {
            CustomSongAudioController.PrepareForShutdown();
            ChartRuntimeLifecycle.Dispose();
            AutoplayResultsUpdateNativePatch.Uninstall();
            CustomDifficultyUnlockPatch.Uninstall();
            try { BackgroundNativeHook.Dispose(); }
            catch { }
            try { CustomSongRuntime.Dispose(); }
            catch { }
        }

        private int _mergedHookErrors;

        private void RunMergedSubsystem(
            string name,
            System.Action action)
        {
            try
            {
                action();
            }
            catch (System.Exception e)
            {
                if (_mergedHookErrors < 10)
                {
                    _mergedHookErrors++;
                    Log.Error(
                        $"[ChartExt] merged subsystem '{name}' " +
                        "tick failed: " + e);
                }
            }
        }
    }
}
