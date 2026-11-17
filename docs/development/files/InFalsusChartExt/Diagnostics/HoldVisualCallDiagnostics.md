# HoldVisualCallDiagnostics.cs

> 源码：`InFalsusChartExt/Diagnostics/HoldVisualCallDiagnostics.cs`
> SHA256：`A835D7660302A6695B3B5E37D6FF6406CB690D0D71DB6CA35B309457050851A8`
> 代码行数：121
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

按文件入口和调用方确定；变更时必须补充。

## 顶层及类型声明

- `HoldVisualCallDiagnostics`（`class`，源码第 18 行）
- `Snapshot`（`struct`，源码第 25 行）

## 方法/属性职责

- `Prefix`（第 36 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Prefix(`
- `IsGameplay`（第 101 行）：判断当前对象/状态条件。
  - 签名：`private static bool IsGameplay()`
- `Reset`（第 113 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal static void Reset()`

## 原版/Harmony/native 访问点

- 第 17 行：`[HarmonyPatch(typeof(HoldNoteV2), "_cZ")]`
- 未检测到直接 native 访问。

## 所有权与释放点

- 第 117 行：`Snapshots.Clear();`

## 性能边界

- 未检测到直接帧级入口；仍应在修改后复核分配和调用频率。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
