# AutoplayScoreGuard.cs

> 源码：`InFalsusChartExt/Features/Autoplay/AutoplayScoreGuard.cs`
> SHA256：`8C41E052B838AC4F958075F0810B11EB16EF1B24AA4D7EBE90E94EB48426737B`
> 代码行数：231
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

测试模式开启期间，每个原版判定周期和切谱清理。

## 顶层及类型声明

- `AutoplayScoreGuard`（`class`，源码第 10 行）

## 方法/属性职责

- `BeginChartSession`（第 40 行）：在下一张 gameplay 谱面建立时清理上一局未结算的 Autoplay 成绩抑制状态，避免中途退谱污染下一局。
  - 签名：`internal static void BeginChartSession()`
- `EnsureRunGuard`（第 65 行）：确认本局 Autoplay 成绩抑制已建立；GameScene 尚未就绪时等待，detour/能力不可用时返回 false 由调用方关闭 Autoplay。
  - 签名：`internal static bool EnsureRunGuard()`
- `MarkUsed`（第 106 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void MarkUsed()`
- `CompleteResultsStart`（第 119 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void CompleteResultsStart()`
- `BeginResultsStart`（第 132 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void BeginResultsStart()`
- `OnSceneUnloaded`（第 138 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void OnSceneUnloaded()`
- `OnGUI`（第 144 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void OnGUI()`
- `MarkGuardUnavailable`（第 201 行）：记录 score guard 不可用并设置用户可见错误提示窗口。
  - 签名：`private static void MarkGuardUnavailable(string reason)`
- `LogError`（第 216 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void LogError(Exception exception)`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 未检测到直接 native 访问。

## 所有权与释放点

- 第 35 行：`/// Clears an unfinished previous run when a new gameplay chart starts.`

## 性能边界

- 未检测到直接帧级入口；仍应在修改后复核分配和调用频率。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
