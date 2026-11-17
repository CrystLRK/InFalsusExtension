# AutoplayScoreGuardPatches.cs

> 源码：`InFalsusChartExt/Patches/AutoplayScoreGuardPatches.cs`
> SHA256：`5BCCB2A38810DA7B2C795149692E5937933DAEDA087AD13F622B562BDA0E03B8`
> 代码行数：56
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

Harmony 目标方法调用时；生命周期跟随原版调用方。

## 顶层及类型声明

- `AutoplayResultsCompletePatch`（`class`，源码第 6 行）
- `AutoplaySaveFreezePatch`（`class`，源码第 25 行）

## 方法/属性职责

- `Prepare`（第 9 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool Prepare()`
- `TargetMethod`（第 14 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static System.Reflection.MethodBase TargetMethod()`
- `Postfix`（第 20 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Postfix()`
- `Prepare`（第 28 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool Prepare()`
- `TargetMethod`（第 33 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static System.Reflection.MethodBase TargetMethod()`
- `Prefix`（第 38 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool Prefix(`

## 原版/Harmony/native 访问点

- 第 5 行：`[HarmonyPatch]`
- 第 24 行：`[HarmonyPatch]`
- 未检测到直接 native 访问。

## 所有权与释放点

- 未检测到显式资源清理代码；修改时需确认调用方所有权。

## 性能边界

- 未检测到直接帧级入口；仍应在修改后复核分配和调用频率。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
