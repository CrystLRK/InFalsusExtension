// Code organization: runtime options overlay. Runtime behavior is unchanged.
using System;
using System.Runtime.InteropServices;
using InFalsusChartExt.CustomSong.Scores;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InFalsusChartExt
{
    /// <summary>
    /// 游戏内选项面板（IMGUI，F8 开关）：自定义背景贴图等辅助选项。
    /// Autoplay 已迁移到游戏原生“音符设定”页。
    /// 状态由当前加载器的配置适配层持久化。
    ///
    /// 独占全屏入口由 DisplayModeRuntime 扩展原版显示设置选择器；
    /// 本面板继续承载 Mod 自有的调试/辅助开关。
    /// </summary>
    internal static class OptionsOverlay
    {
        [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int vk);
        private const int VK_F8 = 0x77;
        private static bool _f8Down;

        private static bool _visible;
        private static bool _initialized;
        private static PluginConfigCategory _category;
        private static PluginConfigEntry<bool> _autoplaySaveScoresEntry;
        private static PluginConfigEntry<bool> _backgroundEntry;
        private static PluginConfigEntry<bool> _hideGroupColorDebugEntry;
        private static PluginConfigEntry<bool> _skyMergeAuthoredEntry;
        private static PluginConfigEntry<bool> _skySharedBranchJudgementEntry;
        private static PluginConfigEntry<float> _skyOverlapHitPaddingEntry;
        private static PluginConfigEntry<bool> _negativeSpeedEntry;
        private static PluginConfigEntry<bool> _hideNoInputBracketsEntry;
        private static int _errors;

        internal static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            try
            {
                _category = ChartExtMod.Config.Category(
                    "InFalsusChartExt", "InFalsus Chart Ext");
                _autoplaySaveScoresEntry = _category.Bind(
                    "AutoplaySaveScores", false,
                    "Autoplay 保存成绩",
                    "默认关闭：AUTOPLAY 对局不写原生/自制谱最佳成绩。"
                    + "开启后按普通对局写入成绩。修改后需重启游戏。");
                _backgroundEntry = _category.Bind("CustomBackground", true,
                    "自定义谱面背景", "使用谱面同级 background.png / <谱名>.background.png");
                _hideGroupColorDebugEntry = _category.Bind(
                    "HideGroupSkyColorDebug", false,
                    "Hidegroup SkyArea 蓝色调试",
                    "默认关闭：hidegroup 正常隐藏 SkyArea。开启并重启游戏后保留 SkyArea 并染蓝；仅用于调试。");
                _skyMergeAuthoredEntry = _category.Bind(
                    "SkyBlockMergeAcrossAuthoredGroups", true,
                    "SkyArea 跨 authored group 几何合并",
                    "关闭并重启后普通 SkyArea 恢复原版 authored group 逻辑。");
                _skySharedBranchJudgementEntry = _category.Bind(
                    "SkyBlockSharedBranchJudgement", true,
                    "普通 SkyArea 分叉共享判定",
                    "关闭并重启后分叉成员恢复原版逐成员判定。");
                _skyOverlapHitPaddingEntry = _category.Bind(
                    "SkyAreaOverlapHitPadding", 0.1f,
                    "重叠 SkyArea 判定扩展",
                    "同一判定层内空间重叠时，每侧额外扩大的归一化范围；"
                    + "仅影响判定，不影响渲染。修改后需重启游戏。");
                _negativeSpeedEntry = _category.Bind(
                    "EnableNegativeSpeedModify", true,
                    "负流速 Hold/SkyArea 反向视觉",
                    "默认开启：负流速下 Hold/SkyArea 从判定线近侧出现，"
                    + "判定中固定在判定线并收缩。关闭并重启后恢复原版"
                    + "（原版负速 Hold 会抛异常）。");
                _hideNoInputBracketsEntry = _category.Bind(
                    "HideNoInputBrackets", true,
                    "隐藏 noinput/noclip SkyArea 方括号",
                    "默认开启：noinput / noclip 的 SkyArea 不显示入口"
                    + "方括号；条带本体、端帽、影子与判定不变。");
                DisplayModeRuntime.Initialize(_category);
                AutoplayScoreGuard.SuppressScores =
                    !_autoplaySaveScoresEntry.Value;
                // AUTO / GROUND ONLY are session-only. A previous session
                // must never auto-enable them on the next launch.
                AutoplayRuntime.Mode = AutoplayMode.Off;
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
                NegativeSpeedVisualRuntime.Enabled =
                    _negativeSpeedEntry.Value;
                TimingNoInputSkyVisualRuntime.HideBrackets =
                    _hideNoInputBracketsEntry.Value;
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
                bool f8 = false;
                try
                {
                    Keyboard keyboard = Keyboard.current;
                    if (keyboard != null)
                    {
                        f8 = keyboard.f8Key.wasPressedThisFrame;
                    }
                }
                catch { }
                // Win32 边沿兜底：游戏对输入后端敏感，双通道保证快捷键可用。
                if (WinEdge(VK_F8, ref _f8Down)) f8 = true;
                if (f8) _visible = !_visible;
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
                Rect panel = new Rect(Screen.width - 380f, 60f, 360f, 220f);
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
            bool background = GUILayout.Toggle(BackgroundSkin.Enabled, " 自定义谱面背景贴图");
            if (background != BackgroundSkin.Enabled)
            {
                BackgroundSkin.SetEnabled(background);
                if (_backgroundEntry != null)
                {
                    _backgroundEntry.Value = background;
                    _category.Save();
                }
            }
            if (GUILayout.Button(" 重新载入背景贴图"))
                BackgroundSkin.Reload();
            GUILayout.Label("谱面: " + (ChartExtState.LastChartName ?? "-"));
            GUILayout.Label("背景文件: " + (BackgroundSkin.CurrentFile ?? "(未找到)"));
            GUILayout.Label("自制谱成绩: " + CustomScoreRuntime.Status);
            GUILayout.Label(
                "提示：Autoplay 设置位于游戏“音符设定”页且仅本局有效；"
                + "SkyArea 分组配置仅从配置文件读取。");
        }
    }
}
