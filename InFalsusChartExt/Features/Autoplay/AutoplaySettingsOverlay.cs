using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace InFalsusChartExt
{
    /// <summary>
    /// Session-only Autoplay controls shown while the native gameplay
    /// settings page is open. The overlay is independent of native C2D rows
    /// and therefore does not alter or clone any original settings object.
    /// </summary>
    internal static class AutoplaySettingsOverlay
    {
        private static IntPtr _panelPointer;
        private static Il2Cppifapp.Game.UI.Settings
            .UiSettingGameplayPanel _panel;
        private static GameObject _root;
        private static RuntimeCanvasUi.CanvasButton _none;
        private static RuntimeCanvasUi.CanvasButton _auto;
        private static RuntimeCanvasUi.CanvasButton _engine;
        private static RuntimeCanvasUi.CanvasButton _ground;
        private static bool _failed;
        private static bool _visible;
        private static int _clickFrame = -1;
        private static int _logs;
        private static int _errors;
        private static AutoplayMode _lastMode =
            (AutoplayMode)(-1);
        private static bool _lastGround;
        private static Il2CppStr.Strings.Localization _lastLanguage =
            (Il2CppStr.Strings.Localization)(-1);

        internal static void Attach(
            Il2Cppifapp.Game.UI.Settings
                .UiSettingGameplayPanel panel)
        {
            if (panel is null || panel.Pointer == IntPtr.Zero)
                return;
            _panelPointer = panel.Pointer;
            _panel = panel;
            Sync(panel);
        }

        internal static void Sync(
            Il2Cppifapp.Game.UI.Settings
                .UiSettingGameplayPanel panel)
        {
            if (_failed)
                return;
            if (panel is null ||
                panel.Pointer == IntPtr.Zero ||
                panel.Pointer != _panelPointer)
                return;
            _panel = panel;
            bool visible = false;
            try
            {
                visible = panel.gameObject is not null &&
                    panel.gameObject.activeInHierarchy;
            }
            catch
            {
            }
            SetVisible(visible);
            if (visible)
                Refresh();
        }

        internal static void Tick()
        {
            if (_failed)
                return;
            if (_visible && !PageAlive())
            {
                SetVisible(false);
                return;
            }
            if (!_visible)
                return;
            HandleClicks();
            Refresh();
        }

        internal static void OnGUI()
        {
            if (!_visible || _root is null || !_root.activeSelf)
                return;
            Event current = Event.current;
            if (current is null ||
                current.type != EventType.MouseDown ||
                current.button != 0)
                return;
            var point = new Vector2(
                current.mousePosition.x,
                Screen.height - current.mousePosition.y);
            if (TryClick(point))
                current.Use();
        }

        internal static void Hide()
        {
            SetVisible(false);
        }

        private static void SetVisible(bool visible)
        {
            if (visible)
                Ensure();
            if (_root is null)
                return;
            if (_root.activeSelf != visible)
                _root.SetActive(visible);
            _visible = visible;
        }

        private static void Ensure()
        {
            if (_root is not null || _failed)
                return;
            try
            {
                Canvas canvas = RuntimeCanvasUi.CreateCanvas(
                    "IFCE_AutoplaySettings",
                    short.MaxValue - 1);
                _root = canvas.gameObject;
                RectTransform panel = RuntimeCanvasUi.CreatePanel(
                    canvas.transform,
                    new Vector2(370f, 270f),
                    new Vector2(-24f, 0f),
                    new Color(0f, 0f, 0f, 0.84f),
                    new Vector2(1f, 0.5f),
                    new Vector2(1f, 0.5f));
                RuntimeCanvasUi.CreateText(
                    panel,
                    "AUTOPLAY",
                    18,
                    new Vector2(330f, 30f),
                    new Vector2(18f, -12f));
                _none = RuntimeCanvasUi.CreateButton(
                    panel,
                    string.Empty,
                    new Vector2(334f, 38f),
                    new Vector2(18f, -54f),
                    () => Choose(AutoplayMode.Off));
                _auto = RuntimeCanvasUi.CreateButton(
                    panel,
                    string.Empty,
                    new Vector2(334f, 38f),
                    new Vector2(18f, -98f),
                    () => Choose(AutoplayMode.Auto));
                _engine = RuntimeCanvasUi.CreateButton(
                    panel,
                    string.Empty,
                    new Vector2(334f, 38f),
                    new Vector2(18f, -142f),
                    () => Choose(AutoplayMode.Engine));
                RuntimeCanvasUi.CreateText(
                    panel,
                    "GROUND ONLY",
                    18,
                    new Vector2(330f, 30f),
                    new Vector2(18f, -188f));
                _ground = RuntimeCanvasUi.CreateButton(
                    panel,
                    string.Empty,
                    new Vector2(334f, 38f),
                    new Vector2(18f, -216f),
                    ToggleGround);
                Refresh();
                if (_logs++ < 2)
                {
                    ChartExtMod.Log.Msg(
                        "[ChartExt] autoplay settings overlay created");
                }
            }
            catch (Exception exception)
            {
                _failed = true;
                TryLogError(
                    "[ChartExt] autoplay settings overlay failed: " +
                    exception);
            }
        }

        private static void Choose(AutoplayMode mode)
        {
            AutoplayRuntime.Mode = mode;
            Refresh();
        }

        private static void ToggleGround()
        {
            AutoplayRuntime.GroundOnly =
                !AutoplayRuntime.GroundOnly;
            Refresh();
        }

        private static void Refresh()
        {
            if (_root is null ||
                _none is null ||
                _auto is null ||
                _engine is null ||
                _ground is null)
                return;
            Il2CppStr.Strings.Localization language =
                Il2CppStr.Strings.GetCurrentLocalization();
            if (_lastMode == AutoplayRuntime.Mode &&
                _lastGround == AutoplayRuntime.GroundOnly &&
                _lastLanguage == language)
                return;
            _lastMode = AutoplayRuntime.Mode;
            _lastGround = AutoplayRuntime.GroundOnly;
            _lastLanguage = language;
            string[] mode = ModeLabels(language);
            _none.Label.text =
                Mark(0, (int)AutoplayRuntime.Mode, mode[0]);
            _auto.Label.text =
                Mark(1, (int)AutoplayRuntime.Mode, mode[1]);
            _engine.Label.text =
                Mark(2, (int)AutoplayRuntime.Mode, mode[2]);
            _ground.Label.text = GroundLabel(
                language,
                AutoplayRuntime.GroundOnly);
        }

        private static string Mark(
            int index,
            int selected,
            string text)
        {
            return (index == selected ? "> " : "   ") + text;
        }

        private static void HandleClicks()
        {
            bool clicked = false;
            Vector2 position = default;
            try
            {
                Mouse mouse = Mouse.current;
                if (mouse is not null)
                {
                    clicked =
                        mouse.leftButton.wasPressedThisFrame;
                    position = mouse.position.ReadValue();
                }
            }
            catch
            {
            }
            if (!clicked)
            {
                try
                {
                    clicked =
                        UnityEngine.Input.GetMouseButtonDown(0);
                    position = UnityEngine.Input.mousePosition;
                }
                catch
                {
                }
            }
            if (!clicked)
                return;
            TryClick(position);
        }

        private static bool TryClick(Vector2 position)
        {
            if (_clickFrame == Time.frameCount)
                return false;
            if (Contains(_none, position))
            {
                _clickFrame = Time.frameCount;
                Choose(AutoplayMode.Off);
                return true;
            }
            if (Contains(_auto, position))
            {
                _clickFrame = Time.frameCount;
                Choose(AutoplayMode.Auto);
                return true;
            }
            if (Contains(_engine, position))
            {
                _clickFrame = Time.frameCount;
                Choose(AutoplayMode.Engine);
                return true;
            }
            if (Contains(_ground, position))
            {
                _clickFrame = Time.frameCount;
                ToggleGround();
                return true;
            }
            return false;
        }

        private static bool PageAlive()
        {
            if (_panel is null)
                return false;
            try
            {
                return _panel.Pointer != IntPtr.Zero &&
                    _panel.gameObject is not null &&
                    _panel.gameObject.activeInHierarchy;
            }
            catch
            {
                return false;
            }
        }

        private static bool Contains(
            RuntimeCanvasUi.CanvasButton button,
            Vector2 screenPoint)
        {
            if (button?.Rect is null)
                return false;
            try
            {
                return RectTransformUtility
                    .RectangleContainsScreenPoint(
                        button.Rect,
                        screenPoint,
                        null);
            }
            catch
            {
                return false;
            }
        }

        private static string[] ModeLabels(
            Il2CppStr.Strings.Localization localization)
        {
            switch (localization)
            {
                case Il2CppStr.Strings.Localization.ChineseSC:
                    return new[] { "无", "自动游玩", "轮椅模式" };
                case Il2CppStr.Strings.Localization.ChineseTC:
                    return new[] { "無", "自動遊玩", "輪椅模式" };
                case Il2CppStr.Strings.Localization.Japanese:
                    return new[] { "なし", "自動", "エンジン" };
                default:
                    return new[] { "NONE", "AUTO", "ENGINE" };
            }
        }

        private static string GroundLabel(
            Il2CppStr.Strings.Localization localization,
            bool enabled)
        {
            switch (localization)
            {
                case Il2CppStr.Strings.Localization.ChineseSC:
                    return enabled ? "仅地面：开启" : "仅地面：关闭";
                case Il2CppStr.Strings.Localization.ChineseTC:
                    return enabled ? "僅地面：開啟" : "僅地面：關閉";
                case Il2CppStr.Strings.Localization.Japanese:
                    return enabled ? "地上のみ：オン" : "地上のみ：オフ";
                default:
                    return enabled
                        ? "GROUND ONLY: ON"
                        : "GROUND ONLY: OFF";
            }
        }

        private static void TryLogError(string message)
        {
            if (_errors++ >= 3)
                return;
            try
            {
                ChartExtMod.Log.Error(message);
            }
            catch
            {
            }
        }
    }
}
