// Code organization: runtime options overlay. Runtime behavior is unchanged.
using System;
using System.Runtime.InteropServices;
using InFalsusChartExt.CustomSong.Scores;
using MelonLoader;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InFalsusChartExt
{
    /// <summary>
    /// 游戏内选项面板（IMGUI，F8 开关）：autoplay 与自定义背景贴图的开关。
    /// F7 切换 ground-only，F9 切换 autoplay 总开关。
    /// 状态持久化在 MelonPreferences（UserData\MelonPreferences.cfg）。
    ///
    /// 独占全屏入口由 DisplayModeRuntime 扩展原版显示设置选择器；
    /// 本面板继续承载 Mod 自有的调试/辅助开关。
    /// </summary>
    internal static class OptionsOverlay
    {
        [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int vk);
        private const int VK_F7 = 0x76;
        private const int VK_F8 = 0x77;
        private const int VK_F9 = 0x78;
        private static bool _f7Down, _f8Down, _f9Down;

        private static bool _visible;
        private static bool _initialized;
        private static MelonPreferences_Category _category;
        private static MelonPreferences_Entry<bool> _autoplayEntry;
        private static MelonPreferences_Entry<bool> _autoplayGroundOnlyEntry;
        private static MelonPreferences_Entry<bool> _autoplaySuppressScoresEntry;
        private static MelonPreferences_Entry<bool> _backgroundEntry;
        private static MelonPreferences_Entry<bool> _hideGroupColorDebugEntry;
        private static MelonPreferences_Entry<bool> _skyMergeAuthoredEntry;
        private static MelonPreferences_Entry<bool> _skySharedBranchJudgementEntry;
        private static MelonPreferences_Entry<float> _skyOverlapHitPaddingEntry;
        private static int _errors;

        internal static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            try
            {
                _category = MelonPreferences.CreateCategory("InFalsusChartExt", "InFalsus Chart Ext");
                _autoplayEntry = _category.CreateEntry("Autoplay", false,
                    "自动演奏 autoplay (F9)", "所有可判定音符自动大EXACT；noinput 不判定");
                _autoplayGroundOnlyEntry = _category.CreateEntry(
                    "AutoplayGroundOnly", false,
                    "仅自动判定地键 (F7)",
                    "自动判定 tap/hold，flick 与 skyarea 保留手动判定；"
                    + "开启时自动启用 Autoplay。");
                _autoplaySuppressScoresEntry = _category.CreateEntry(
                    "AutoplaySuppressScores", true,
                    "Autoplay 局不计成绩",
                    "本局只要在 gameplay 中开启过 Autoplay，"
                    + "就跳过原版成绩和自制谱 JSON 写入，并在结果页显示 "
                    + "AUTOPLAY。仅从配置文件读取，修改后需重启游戏。");
                _backgroundEntry = _category.CreateEntry("CustomBackground", true,
                    "自定义谱面背景", "使用谱面同级 background.png / <谱名>.background.png");
                _hideGroupColorDebugEntry = _category.CreateEntry(
                    "HideGroupSkyColorDebug", false,
                    "Hidegroup SkyArea 蓝色调试",
                    "默认关闭：hidegroup 正常隐藏 SkyArea。开启并重启游戏后保留 SkyArea 并染蓝；仅用于调试。");
                _skyMergeAuthoredEntry = _category.CreateEntry(
                    "SkyBlockMergeAcrossAuthoredGroups", true,
                    "SkyArea 跨 authored group 几何合并",
                    "关闭并重启后普通 SkyArea 恢复原版 authored group 逻辑。");
                _skySharedBranchJudgementEntry = _category.CreateEntry(
                    "SkyBlockSharedBranchJudgement", true,
                    "普通 SkyArea 分叉共享判定",
                    "关闭并重启后分叉成员恢复原版逐成员判定。");
                _skyOverlapHitPaddingEntry = _category.CreateEntry(
                    "SkyAreaOverlapHitPadding", 0.1f,
                    "重叠 SkyArea 判定扩展",
                    "同一判定层内空间重叠时，每侧额外扩大的归一化范围；"
                    + "仅影响判定，不影响渲染。修改后需重启游戏。");
                DisplayModeRuntime.Initialize(_category);
                AutoplayRuntime.GroundOnly =
                    _autoplayGroundOnlyEntry.Value;
                AutoplayScoreGuard.SuppressScores =
                    _autoplaySuppressScoresEntry.Value;
                // Autoplay is session-only. A previous session must never
                // auto-enable it on the next launch.
                if (_autoplayEntry.Value)
                {
                    _autoplayEntry.Value = false;
                    _category.SaveToFile();
                }
                AutoplayRuntime.Enabled = false;
                BackgroundSkin.Enabled = _backgroundEntry.Value;
                HideGroupSkyColorRuntime.DebugColorEnabled =
                    _hideGroupColorDebugEntry.Value;
                SkyBlockPolicy.MergeAcrossAuthoredGroups =
                    _skyMergeAuthoredEntry.Value;
                SkyBlockPolicy.EnableSharedBranchJudgement =
                    _skySharedBranchJudgementEntry.Value;
                SkyBlockPolicy.OverlapHitPadding = Math.Max(
                    0f,
                    Math.Min(0.5f, _skyOverlapHitPaddingEntry.Value));
            }
            catch (Exception e)
            {
                if (_errors < 5) { _errors++; ChartExtMod.Log.Error("[ChartExt] options init failed: " + e); }
            }
        }

        internal static void Tick()
        {
            Init();
            try
            {
                bool f7 = false, f8 = false, f9 = false;
                try
                {
                    Keyboard keyboard = Keyboard.current;
                    if (keyboard != null)
                    {
                        f7 = keyboard.f7Key.wasPressedThisFrame;
                        f8 = keyboard.f8Key.wasPressedThisFrame;
                        f9 = keyboard.f9Key.wasPressedThisFrame;
                    }
                }
                catch { }
                // Win32 边沿兜底：游戏对输入后端敏感，双通道保证快捷键可用。
                if (WinEdge(VK_F7, ref _f7Down)) f7 = true;
                if (WinEdge(VK_F8, ref _f8Down)) f8 = true;
                if (WinEdge(VK_F9, ref _f9Down)) f9 = true;
                if (f8) _visible = !_visible;
                if (f7)
                {
                    AutoplayRuntime.GroundOnly =
                        !AutoplayRuntime.GroundOnly;
                    if (AutoplayRuntime.GroundOnly)
                        AutoplayRuntime.Enabled = true;
                    if (_autoplayGroundOnlyEntry != null)
                        _autoplayGroundOnlyEntry.Value =
                            AutoplayRuntime.GroundOnly;
                    if (_autoplayEntry != null)
                        _autoplayEntry.Value = AutoplayRuntime.Enabled;
                    if (_category != null)
                        _category.SaveToFile();
                }
                if (f9)
                {
                    AutoplayRuntime.Enabled = !AutoplayRuntime.Enabled;
                    if (_autoplayEntry != null)
                    {
                        _autoplayEntry.Value = AutoplayRuntime.Enabled;
                        _category.SaveToFile();
                    }
                }
            }
            catch
            {
                // 输入轮询失败不影响游戏
            }
        }

        private static bool WinEdge(int vk, ref bool downState)
        {
            bool down = (GetAsyncKeyState(vk) & 0x8000) != 0;
            bool edge = down && !downState;
            downState = down;
            return edge;
        }

        internal static void OnGUI()
        {
            if (!_visible) return;
            try
            {
                int oldDepth = GUI.depth;
                GUI.depth = -900;
                Rect panel = new Rect(Screen.width - 380f, 60f, 360f, 245f);
                GUI.Box(panel, "InFalsusChartExt 选项（F8 开关）");
                GUILayout.BeginArea(new Rect(panel.x + 12f, panel.y + 28f, panel.width - 24f, panel.height - 40f));
                DrawContents();
                GUILayout.EndArea();
                GUI.depth = oldDepth;
            }
            catch
            {
                // IMGUI 绘制失败不影响游戏
            }
        }

        private static void DrawContents()
        {
            bool autoplay = GUILayout.Toggle(AutoplayRuntime.Enabled,
                " 自动演奏 autoplay：所有音符大EXACT（noinput 不判定）");
            if (autoplay != AutoplayRuntime.Enabled)
            {
                AutoplayRuntime.Enabled = autoplay;
                if (_autoplayEntry != null)
                {
                    _autoplayEntry.Value = autoplay;
                    _category.SaveToFile();
                }
            }
            bool groundOnly = GUILayout.Toggle(
                AutoplayRuntime.GroundOnly,
                " 仅自动判定地键（tap/hold；flick/skyarea 手动）");
            if (groundOnly != AutoplayRuntime.GroundOnly)
            {
                AutoplayRuntime.GroundOnly = groundOnly;
                if (groundOnly && !AutoplayRuntime.Enabled)
                {
                    AutoplayRuntime.Enabled = true;
                    if (_autoplayEntry != null)
                        _autoplayEntry.Value = true;
                }
                if (_autoplayGroundOnlyEntry != null)
                    _autoplayGroundOnlyEntry.Value = groundOnly;
                _category.SaveToFile();
            }
            bool background = GUILayout.Toggle(BackgroundSkin.Enabled, " 自定义谱面背景贴图");
            if (background != BackgroundSkin.Enabled)
            {
                BackgroundSkin.SetEnabled(background);
                if (_backgroundEntry != null)
                {
                    _backgroundEntry.Value = background;
                    _category.SaveToFile();
                }
            }
            if (GUILayout.Button(" 重新载入背景贴图"))
                BackgroundSkin.Reload();
            GUILayout.Label("谱面: " + (ChartExtState.LastChartName ?? "-"));
            GUILayout.Label("背景文件: " + (BackgroundSkin.CurrentFile ?? "(未找到)"));
            GUILayout.Label("自制谱成绩: " + CustomScoreRuntime.Status);
            GUILayout.Label(
                "提示：F7 切换仅地键，F9 切换 autoplay 总开关；"
                + "SkyArea 分组配置仅从配置文件读取。");
        }
    }
}
