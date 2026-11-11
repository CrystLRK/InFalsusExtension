using System;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2Cppifapp.Game.UI.Settings;
using Il2CppStr;
using MelonLoader;
using UnityEngine;

namespace InFalsusChartExt
{
    internal enum ConfiguredDisplayMode
    {
        FollowNative = -1,
        Windowed = 0,
        FullScreenWindow = 1,
        ExclusiveFullScreen = 2,
    }

    /// <summary>
    /// Adds a third exclusive-fullscreen entry to the native display selector.
    /// Original two-state settings remain the source of truth for windowed and
    /// borderless modes; the Mod only owns the third mode.
    /// </summary>
    internal static class DisplayModeRuntime
    {
        private const string EntryName = "DisplayMode";
        private const double VerifyDelaySeconds = 1.0;
        private static MelonPreferences_Category _category;
        private static MelonPreferences_Entry<string> _modeEntry;
        private static ConfiguredDisplayMode _mode =
            ConfiguredDisplayMode.FollowNative;
        private static IntPtr _panelPointer;
        private static IntPtr _fullscreenPointer;
        private static int _syncDepth;
        private static double _verifyAt;
        private static bool _wasFocused = true;
        private static double _warningUntil;
        private static string _warning;
        private static int _logs;
        private static int _resolutionForceLogs;
        private static int _exclusiveRetries;

        internal static bool Initialized =>
            _modeEntry is not null;

        internal static ConfiguredDisplayMode Mode => _mode;

        internal static void Initialize(
            MelonPreferences_Category category)
        {
            if (Initialized || category is null) return;
            _category = category;
            try
            {
                _modeEntry = category.CreateEntry(
                    EntryName,
                    nameof(ConfiguredDisplayMode.FollowNative),
                    "窗口模式扩展",
                    "Windowed / FullScreenWindow / ExclusiveFullScreen；" +
                    "由游戏显示设置第三项同步写入。");
                _mode = Parse(_modeEntry.Value);
                Log(
                    $"[ChartExt] display mode configured=" +
                    $"{_mode}");
            }
            catch (Exception e)
            {
                _modeEntry = null;
                LogError(
                    "[ChartExt] display mode init failed: " + e);
            }
        }

        internal static void Attach(UiSettingDisplayPanel panel)
        {
            if (panel is null || panel.Pointer == IntPtr.Zero)
                return;
            try
            {
                UiSettingMultiselectorArrow fullscreen =
                    panel.fullscreen;
                if (fullscreen is null ||
                    fullscreen.Pointer == IntPtr.Zero)
                    return;
                _panelPointer = panel.Pointer;
                _fullscreenPointer = fullscreen.Pointer;
                EnsureThirdOption(fullscreen);
                SyncSelection(panel);
                Log(
                    "[ChartExt] exclusive fullscreen option attached; " +
                    $"mode={_mode}, actual={Screen.fullScreenMode}");
            }
            catch (Exception e)
            {
                LogError(
                    "[ChartExt] display mode attach failed: " + e);
            }
        }

        internal static void Tick()
        {
            if (!Initialized) return;
            try
            {
                bool focused = Application.isFocused;
                if (focused && !_wasFocused)
                    BeforeFocusGained();
                _wasFocused = focused;

                if (_verifyAt > 0.0 &&
                    Time.realtimeSinceStartup >= _verifyAt)
                    VerifyExclusiveMode();
            }
            catch (Exception e)
            {
                LogError(
                    "[ChartExt] display mode tick failed: " + e);
            }
        }

        internal static void OnApplicationFocus(bool focused)
        {
            if (!Initialized) return;
            if (focused && !_wasFocused)
                BeforeFocusGained();
            _wasFocused = focused;
        }

        internal static void Sync(UiSettingDisplayPanel panel)
        {
            SyncSelection(panel);
        }

        internal static void BeginPanelRefresh()
        {
            _syncDepth++;
        }

        internal static void EndPanelRefresh(
            UiSettingDisplayPanel panel)
        {
            try
            {
                SyncSelection(panel);
            }
            finally
            {
                if (_syncDepth > 0) _syncDepth--;
            }
        }

        internal static void EndPanelRefreshWithException()
        {
            if (_syncDepth > 0) _syncDepth--;
        }

        internal static bool BeforeFullscreenCallback()
            => _syncDepth == 0;

        internal static void BeforeIndexChange(
            UiSettingMultiselectorArrow control,
            int index)
        {
            if (_syncDepth > 0 ||
                control is null ||
                control.Pointer != _fullscreenPointer)
                return;
            SetMode(IndexToMode(index), persist: true);
        }

        internal static void BeforeResolutionRequest(
            ref FullScreenMode mode)
        {
            if (_mode !=
                    ConfiguredDisplayMode.ExclusiveFullScreen ||
                mode != FullScreenMode.FullScreenWindow)
                return;
            mode = FullScreenMode.ExclusiveFullScreen;
            if (_resolutionForceLogs++ < 4)
            {
                Log(
                    "[ChartExt] display resolution request forced " +
                    $"to ExclusiveFullScreen " +
                    $"({Screen.width}x{Screen.height})");
            }
            ScheduleVerification();
        }

        internal static void DrawWarning()
        {
            if (_warning is null ||
                Time.realtimeSinceStartup >= _warningUntil)
                return;
            try
            {
                float width = Math.Min(900f, Screen.width * 0.8f);
                var rect = new Rect(
                    (Screen.width - width) * 0.5f,
                    72f, width, 54f);
                try
                {
                    GUI.color =
                        new Color(1f, 0.84f, 0.84f, 0.96f);
                    GUI.Box(rect, _warning);
                }
                finally
                {
                    GUI.color = Color.white;
                }
            }
            catch
            {
            }
        }

        private static void SyncSelection(
            UiSettingDisplayPanel panel)
        {
            if (!Initialized ||
                panel is null ||
                panel.Pointer != _panelPointer)
                return;
            try
            {
                UiSettingMultiselectorArrow fullscreen =
                    panel.fullscreen;
                if (fullscreen is null ||
                    fullscreen.Pointer == IntPtr.Zero)
                    return;
                _fullscreenPointer = fullscreen.Pointer;
                EnsureThirdOption(fullscreen);
                int desired = DesiredIndex();
                if (fullscreen._nI == desired)
                    return;

                bool ownedGuard = _syncDepth == 0;
                if (ownedGuard) _syncDepth++;
                try
                {
                    fullscreen._Tg(desired);
                }
                finally
                {
                    if (ownedGuard && _syncDepth > 0)
                        _syncDepth--;
                }
            }
            catch (Exception e)
            {
                LogError(
                    "[ChartExt] display mode sync failed: " + e);
            }
        }

        private static void EnsureThirdOption(
            UiSettingMultiselectorArrow control)
        {
            Il2CppReferenceArray<TextParameters> options =
                control._MI;
            if (options is not null && options.Length >= 3)
                return;
            int count = options?.Length ?? 0;
            var expanded =
                new Il2CppReferenceArray<TextParameters>(
                    Math.Max(3, count));
            for (int i = 0; i < count; i++)
                expanded[i] = options[i];
            for (int i = count; i < 2; i++)
            {
                expanded[i] = TextParameters.Create(
                    i == 0
                        ? Strings.Key.Settings_Display_Windowed
                        : Strings.Key
                            .Settings_Display_FullScreenWindowed);
            }
            expanded[2] = TextParameters.Create(
                Strings.Key.Settings_Display_FullScreen);
            control._MI = expanded;
        }

        private static int DesiredIndex()
        {
            if (_mode ==
                ConfiguredDisplayMode.ExclusiveFullScreen)
                return 2;
            if (_mode == ConfiguredDisplayMode.FullScreenWindow)
                return 1;
            if (_mode == ConfiguredDisplayMode.Windowed)
                return 0;
            switch (Screen.fullScreenMode)
            {
                case FullScreenMode.ExclusiveFullScreen:
                    return 2;
                case FullScreenMode.FullScreenWindow:
                case FullScreenMode.MaximizedWindow:
                    return 1;
                default:
                    return 0;
            }
        }

        private static ConfiguredDisplayMode IndexToMode(int index)
        {
            return index switch
            {
                0 => ConfiguredDisplayMode.Windowed,
                1 => ConfiguredDisplayMode.FullScreenWindow,
                2 => ConfiguredDisplayMode.ExclusiveFullScreen,
                _ => _mode,
            };
        }

        private static void SetMode(
            ConfiguredDisplayMode mode,
            bool persist)
        {
            if (_mode == mode) return;
            _mode = mode;
            if (mode ==
                ConfiguredDisplayMode.ExclusiveFullScreen)
            {
                _exclusiveRetries = 0;
                ScheduleVerification();
            }
            if (persist)
                Save();
            Log(
                $"[ChartExt] display mode selected={mode} " +
                $"actual={Screen.fullScreenMode}");
        }

        private static void RequestConfiguredMode()
        {
            int width = Screen.width;
            int height = Screen.height;
            if (width <= 0 || height <= 0)
                return;
            RefreshRate refresh =
                Screen.currentResolution.refreshRateRatio;
            Screen.SetResolution(
                width,
                height,
                FullScreenMode.ExclusiveFullScreen,
                refresh);
            ScheduleVerification();
        }

        private static void BeforeFocusGained()
        {
            if (_mode !=
                    ConfiguredDisplayMode.ExclusiveFullScreen ||
                Screen.fullScreenMode ==
                    FullScreenMode.ExclusiveFullScreen)
                return;
            RequestConfiguredMode();
        }

        private static void ScheduleVerification()
        {
            _verifyAt =
                Time.realtimeSinceStartup + VerifyDelaySeconds;
        }

        private static void VerifyExclusiveMode()
        {
            _verifyAt = 0.0;
            if (Screen.fullScreenMode ==
                FullScreenMode.ExclusiveFullScreen)
                return;
            if (_exclusiveRetries++ == 0)
            {
                RequestConfiguredMode();
                return;
            }

            SetMode(
                ConfiguredDisplayMode.FullScreenWindow,
                persist: true);
            int width = Screen.width;
            int height = Screen.height;
            if (width > 0 && height > 0)
            {
                Screen.SetResolution(
                    width,
                    height,
                    FullScreenMode.FullScreenWindow,
                    Screen.currentResolution.refreshRateRatio);
            }
            ShowWarning(
                "Exclusive Fullscreen is unavailable; " +
                "falling back to Borderless Fullscreen.");
            LogError(
                "[ChartExt] exclusive fullscreen was not applied; " +
                "fell back to FullScreenWindow");
        }

        private static ConfiguredDisplayMode Parse(string value)
        {
            if (Enum.TryParse(
                    value,
                    true,
                    out ConfiguredDisplayMode parsed) &&
                Enum.IsDefined(
                    typeof(ConfiguredDisplayMode),
                    parsed))
                return parsed;
            return ConfiguredDisplayMode.FollowNative;
        }

        private static void Save()
        {
            if (_modeEntry is null) return;
            _modeEntry.Value = _mode.ToString();
            try { _category?.SaveToFile(); }
            catch { }
        }

        private static void ShowWarning(string message)
        {
            _warning = message;
            _warningUntil =
                Time.realtimeSinceStartup + 7.0;
        }

        private static void Log(string message)
        {
            if (_logs++ >= 12) return;
            try { ChartExtMod.Log.Msg(message); }
            catch { }
        }

        private static void LogError(string message)
        {
            try { ChartExtMod.Log.Error(message); }
            catch { }
        }
    }
}
