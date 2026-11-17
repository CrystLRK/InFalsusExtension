# 显示模式扩展

## 目标

在不改变原版窗口模式和无边框全屏行为的前提下，为显示设置增加真正的
`ExclusiveFullScreen` 第三项。

## 入口

- 原版显示设置：在 `UiSettingDisplayPanel` 初始化后扩展全屏选择器；
- 原版全屏回调：只在第三项状态把 `FullScreenWindow` 请求改写为
  `ExclusiveFullScreen`；
- 分辨率/刷新率：沿用游戏设置页当前选择；
- Alt+Tab：重新获得焦点时重新应用独占模式；
- 配置：`MelonPreferences` 的 `DisplayMode`。

## 实现边界

- `DisplayModeRuntime`：三态、UI 同步、焦点恢复、失败回退；
- `DisplayModePatches`：只挂接显示面板、全屏选择器和
  `UnityEngine.Screen.SetResolution`；
- 不接入音符、SkyArea、判定、计分、谱面、对象池或程序化渲染。

## 失败处理

独占模式请求后延迟验证实际 `Screen.fullScreenMode`。首次失败允许直接
重试一次；仍失败则回退 `FullScreenWindow`、更新配置并显示短暂警告。

## 用户文档

- `docs/独占全屏.md`
