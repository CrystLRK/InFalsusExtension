# SkyBlockLineGlowRuntime.cs

> 源码：`InFalsusChartExt/Features/SkyArea/SkyBlockLineGlowRuntime.cs`
> SHA256：`1BEE62E84C1A75FF364CBB7A80604A7870D7FBCFEA3319AD1E6A59B814D964E3`
> 代码行数：285
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

判定线光带

## 生命周期

谱面载入建立计划，游戏帧内更新视觉/判定，切谱回收。

## 顶层及类型声明

- `SkyBlockLineGlowRuntime`（`class`，源码第 18 行）

## 方法/属性职责

- `Tick`（第 34 行）：执行帧级状态或视觉更新；必须保持无异常扩散和稳定分配。
  - 签名：`internal static void Tick(`
- `ResetForChartSwitch`（第 130 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal static void ResetForChartSwitch()`
- `TryResolveRange`（第 142 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`private static bool TryResolveRange(`
- `PulseOpacity`（第 210 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static float PulseOpacity(`
- `ApplyOutput`（第 222 行）：把当前状态应用到目标对象；不得越界修改共享原版状态。
  - 签名：`private static void ApplyOutput(`
- `ReleaseIfActive`（第 235 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void ReleaseIfActive(`
- `IsFinite`（第 253 行）：判断当前对象/状态条件。
  - 签名：`private static bool IsFinite(double value)`
- `Clamp`（第 257 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static double Clamp(`
- `CurrentPlan`（第 263 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static SkyBlockPlan CurrentPlan()`
- `LogError`（第 272 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void LogError(`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 第 160 行：`Marshal.ReadInt32(`
- 第 162 行：`NativeLayoutCatalog.SkyJudge.CursorX));`

## 所有权与释放点

- 第 47 行：`ReleaseIfActive(track);`
- 第 56 行：`ReleaseIfActive(track);`
- 第 63 行：`ReleaseIfActive(track);`
- 第 78 行：`ReleaseIfActive(track);`
- 第 85 行：`ReleaseIfActive(track);`
- 第 130 行：`internal static void ResetForChartSwitch()`
- 第 134 行：`ReleaseIfActive(`
- 第 235 行：`private static void ReleaseIfActive(`

## 性能边界

- 包含帧级入口：`Tick`。不得新增无界扫描、每帧对象创建或无界缓存。

## 对应测试

- R3/R4 contract harness
- SkyBlock alias scan
- SkyBlockOverlapHarness
- SkyBlockTestHarness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
