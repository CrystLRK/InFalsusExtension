// Loader-neutral runtime plus thin MelonLoader/BepInEx host entries.
using System;
using System.Collections.Generic;
using System.IO;
using InFalsusChartExt.CustomSong.Audio;
using InFalsusChartExt.CustomSong.Lifecycle;
#if MELONLOADER
using MelonLoader;
#elif BEPINEX
using System.Reflection;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
#endif

#if MELONLOADER
[assembly: MelonInfo(
    typeof(InFalsusChartExt.MelonChartExtMod),
    "InFalsus Chart Ext",
    "0.3.0",
    "IFCE")]
[assembly: MelonGame("lowiro", "infalsus")]
#endif

namespace InFalsusChartExt
{
    internal static class ChartExtMod
    {
        internal static PluginLog Log { get; private set; }
        internal static PluginConfig Config { get; private set; }

        internal static void Initialize(
            PluginLog log,
            PluginConfig config)
        {
            Log = log;
            Config = config;
        }

        internal static void InitializeRuntime()
        {
            MigrateLegacyData();
            BuildProfile.Initialize();
            CustomDifficultyUnlockPatch.Install();
            SpecialLevelRuntime.InstallLateApply();

            // The capacity immediate patch must run before chart containers
            // are constructed.
            NumericLimitPatch.Apply();

            Log.Msg("[ChartExt] loaded; patches installed.");
            OptionsOverlay.Init();
            StartupLoadingScreen.Begin();

            try
            {
                if (BuildProfile.Current.SupportsBackgroundNativeHook)
                    BackgroundNativeHook.Install();
                else
                    Log.Warning(
                        "[BuildProfile] native background hook disabled " +
                        "for this build.");
            }
            catch (Exception e)
            {
                Log.Error(
                    "[ChartExt] native background hook install failed: " +
                    e);
            }

        }

        internal static void MigrateLegacyData()
        {
            try
            {
                string target = PluginEnvironment.PluginDataDirectory;
                Directory.CreateDirectory(target);
                var candidates = new List<string>
                {
                    Path.Combine(
                        PluginEnvironment.GameRoot,
                        "Mods",
                        "InFalsusChartExt"),
                    Path.Combine(
                        PluginEnvironment.PluginDirectory,
                        "InFalsusChartExt"),
                };
                var seen = new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);
                int copied = 0;
                foreach (string candidate in candidates)
                {
                    if (string.IsNullOrWhiteSpace(candidate) ||
                        !seen.Add(candidate) ||
                        !Directory.Exists(candidate))
                        continue;
                    copied += CopyMissing(candidate, target);
                }
                if (copied > 0)
                {
                    Log.Msg(
                        $"[ChartExt] migrated {copied} legacy data file(s) " +
                        $"to {target}");
                }
            }
            catch (Exception e)
            {
                Log.Warning(
                    "[ChartExt] legacy custom-results migration " +
                    "failed: " + e.Message);
            }
        }

        private static int CopyMissing(string source, string target)
        {
            int copied = 0;
            foreach (string file in Directory.GetFiles(source))
            {
                string destination = Path.Combine(
                    target, Path.GetFileName(file));
                if (File.Exists(destination)) continue;
                File.Copy(file, destination, overwrite: false);
                copied++;
            }
            foreach (string directory in Directory.GetDirectories(source))
            {
                string destination = Path.Combine(
                    target, Path.GetFileName(directory));
                Directory.CreateDirectory(destination);
                copied += CopyMissing(directory, destination);
            }
            return copied;
        }

        internal static void Tick()
        {
            long profileStart = FrameProfiler.Begin(out long profileAlloc);
            StartupLoadingScreen.Tick();
            ChartRuntimeLifecycle.Tick();
            NativeNoteSlotTable.Refresh();
            GameplayStateGuard.Tick();
            AutoplayRuntime.Tick();
            AutoplaySettingsOverlay.Tick();
            RegressionDiagnostics.GameplayStateTick(
                Il2Cppifapp.Game.Scenes.Game.Track._a);
            RegressionDiagnostics.AudioTick();
            AudioOffsetRuntime.Tick();
            OptionsOverlay.Tick();
            DisplayModeRuntime.Tick();
            RunMergedSubsystem(
                "installer", () => CustomSongRuntime.Tick());
            FrameProfiler.End(
                FrameProfiler.Tick, profileStart, profileAlloc);
        }

        internal static void OnGUI()
        {
            ShowTextRuntime.OnGUI();
            SpecialLevelRuntime.OnGUI();
            OptionsOverlay.OnGUI();
            AutoplayScoreGuard.OnGUI();
            DisplayModeRuntime.DrawWarning();
            StartupLoadingScreen.OnGUI();
            AutoplaySettingsOverlay.OnGUI();
        }

        internal static void OnSceneLoaded(
            int buildIndex,
            string sceneName)
        {
            StartupLoadingScreen.OnSceneLoaded(sceneName);
            if (!string.IsNullOrWhiteSpace(sceneName) &&
                sceneName.IndexOf(
                    "Results",
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                AutoplayScoreGuard.BeginResultsStart();
            }
            ChartRuntimeLifecycle.OnSceneLoaded(
                buildIndex, sceneName);
        }

        internal static void OnSceneUnloaded(
            int buildIndex,
            string sceneName)
        {
            ChartRuntimeLifecycle.OnSceneUnloaded(
                buildIndex, sceneName);
        }

        internal static void OnApplicationFocus(bool focus)
        {
            DisplayModeRuntime.OnApplicationFocus(focus);
        }

        internal static void OnApplicationQuit()
        {
            CustomSongAudioController.PrepareForShutdown();
        }

        internal static void Dispose()
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

        private static int _mergedHookErrors;

        private static void RunMergedSubsystem(
            string name,
            Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
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

#if MELONLOADER
    public sealed class MelonChartExtMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            string pluginDirectory = Path.GetDirectoryName(
                typeof(MelonChartExtMod).Assembly.Location)
                ?? Environment.CurrentDirectory;
            string gameRoot = Path.GetFullPath(
                Path.Combine(pluginDirectory, ".."));
            PluginEnvironment.Initialize(
                PluginLoaderKind.MelonLoader,
                gameRoot,
                pluginDirectory);
            ChartExtMod.Initialize(
                new PluginLog(LoggerInstance),
                new PluginConfig());
            ChartExtMod.InitializeRuntime();
        }

        public override void OnUpdate()
        {
            ChartExtMod.Tick();
        }

        public override void OnGUI()
        {
            ChartExtMod.OnGUI();
        }

        public override void OnSceneWasLoaded(
            int buildIndex,
            string sceneName)
        {
            ChartExtMod.OnSceneLoaded(buildIndex, sceneName);
        }

        public override void OnSceneWasUnloaded(
            int buildIndex,
            string sceneName)
        {
            ChartExtMod.OnSceneUnloaded(buildIndex, sceneName);
        }

        public void OnApplicationFocus(bool focus)
        {
            ChartExtMod.OnApplicationFocus(focus);
        }

        public override void OnApplicationQuit()
        {
            ChartExtMod.OnApplicationQuit();
        }

        public override void OnDeinitializeMelon()
        {
            ChartExtMod.Dispose();
        }
    }
#elif BEPINEX
    [BepInPlugin(
        "com.ifce.chartext",
        "InFalsus Chart Ext",
        "0.3.0")]
    [BepInProcess("infalsus.exe")]
    public sealed class BepInExChartExtPlugin : BasePlugin
    {
        private static readonly UnityAction<Scene, LoadSceneMode>
            SceneLoadedHandler =
                DelegateSupport.ConvertDelegate<
                    UnityAction<Scene, LoadSceneMode>>(
                    new Action<Scene, LoadSceneMode>(
                        OnSceneLoaded));
        private static readonly UnityAction<Scene>
            SceneUnloadedHandler =
                DelegateSupport.ConvertDelegate<
                    UnityAction<Scene>>(
                    new Action<Scene>(OnSceneUnloaded));

        private Harmony _harmony;
        private BepInExRuntimeBehaviour _behaviour;

        public override void Load()
        {
            string pluginDirectory = Path.GetDirectoryName(
                typeof(BepInExChartExtPlugin).Assembly.Location)
                ?? Environment.CurrentDirectory;
            PluginEnvironment.Initialize(
                PluginLoaderKind.BepInEx,
                BepInEx.Paths.GameRootPath,
                pluginDirectory);
            ChartExtMod.Initialize(
                new PluginLog(Log),
                new PluginConfig(
                    Config,
                    Path.Combine(
                        PluginEnvironment.GameRoot,
                        "UserData",
                        "MelonPreferences.cfg")));
            ChartExtMod.InitializeRuntime();

            // MelonLoader patches all [HarmonyPatch] types automatically.
            // BepInEx does not, so install them explicitly after the build
            // profile and native capacity preparation.
            _harmony = Harmony.CreateAndPatchAll(
                Assembly.GetExecutingAssembly(),
                "com.ifce.chartext");

            SceneManager.add_sceneLoaded(SceneLoadedHandler);
            SceneManager.add_sceneUnloaded(SceneUnloadedHandler);
            _behaviour = AddComponent<BepInExRuntimeBehaviour>();
            Log.LogInfo(
                "[ChartExt] BepInEx plugin loaded; patches installed.");
        }

        public override bool Unload()
        {
            try
            {
                SceneManager.remove_sceneLoaded(
                    SceneLoadedHandler);
                SceneManager.remove_sceneUnloaded(
                    SceneUnloadedHandler);
            }
            catch
            {
            }
            try { _harmony?.UnpatchSelf(); }
            catch { }
            ChartExtMod.Dispose();
            _behaviour = null;
            _harmony = null;
            return true;
        }

        private static void OnSceneLoaded(
            Scene scene,
            LoadSceneMode mode)
        {
            ChartExtMod.OnSceneLoaded(
                scene.buildIndex, scene.name);
        }

        private static void OnSceneUnloaded(Scene scene)
        {
            ChartExtMod.OnSceneUnloaded(
                scene.buildIndex, scene.name);
        }
    }

    public sealed class BepInExRuntimeBehaviour : MonoBehaviour
    {
        public void Update()
        {
            ChartExtMod.Tick();
        }

        public void OnGUI()
        {
            ChartExtMod.OnGUI();
        }

        public void OnApplicationFocus(bool focus)
        {
            ChartExtMod.OnApplicationFocus(focus);
        }

        public void OnApplicationQuit()
        {
            ChartExtMod.OnApplicationQuit();
        }
    }
#endif
}
