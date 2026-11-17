# BombPlan.cs

> 源码：`InFalsusChartExt/Features/Bomb/BombPlan.cs`
> SHA256：`5654AF71CB166B7A2415979EB9529BE81A1083C91EF549F8D7E68C950FCA5BA7`
> 代码行数：316
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

Bomb descriptor、native/visual ownership

## 生命周期

谱面载入建立计划，判定与渲染阶段消费，切谱回收。

## 顶层及类型声明

- `BombMode`（`enum`，源码第 6 行）
- `BombDescriptor`（`class`，源码第 18 行）
- `BombPlan`（`class`，源码第 47 行）

## 方法/属性职责

- `IsBomb`（第 67 行）：判断当前对象/状态条件。
  - 签名：`internal bool IsBomb(long nativeId)`
- `TryGetByNativeId`（第 70 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`internal bool TryGetByNativeId(long nativeId, out BombDescriptor descriptor)`
- `TryBuild`（第 73 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`internal static BombPlan TryBuild(`
- `BindAfterPartition`（第 150 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal bool BindAfterPartition(`
- `Validate`（第 223 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal bool Validate(out string error)`
- `TryReadGeometry`（第 267 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`private static bool TryReadGeometry(`
- `RoundNormalized`（第 294 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static int RoundNormalized(double value)`
- `Contains`（第 299 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool Contains(`
- `IsFinite`（第 313 行）：判断当前对象/状态条件。
  - 签名：`private static bool IsFinite(double value)`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 未检测到直接 native 访问。

## 所有权与释放点

- 第 157 行：`NativeMembers.Clear();`
- 第 158 行：`VisualMembers.Clear();`
- 第 159 行：`Due.Clear();`
- 第 160 行：`_byNativeId.Clear();`
- 第 161 行：`_byVisualId.Clear();`

## 性能边界

- 未检测到直接帧级入口；仍应在修改后复核分配和调用频率。

## 对应测试

- BombJudgeValidation
- BombPlanValidation
- PrerollValidation
- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
