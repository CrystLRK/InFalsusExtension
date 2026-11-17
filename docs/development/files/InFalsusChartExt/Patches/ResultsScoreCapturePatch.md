# ResultsScoreCapturePatch.cs

> 源码：`InFalsusChartExt/Patches/ResultsScoreCapturePatch.cs`
> SHA256：`8CD236EF2862574E69FE9DE575231067E761ECA979E0C9B2382C719FD9ABA88E`
> 代码行数：25
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

ResultsScene.Start 后置结果观察

## 生命周期

Harmony 目标方法调用时；生命周期跟随原版调用方。

## 顶层及类型声明

- `ResultsScoreCapturePatch`（`class`，源码第 11 行）

## 方法/属性职责

- `Prepare`（第 14 行）：仅在当前 profile 声明 CustomScores 能力时安装补丁。
  - 签名：`private static bool Prepare()`
- `TargetMethod`（第 18 行）：解析原版 ResultsScene.Start 托管方法。
  - 签名：`private static System.Reflection.MethodBase TargetMethod()`
- `Postfix`（第 22 行）：原版最终成绩更新后调用自制谱成绩保存编排。
  - 签名：`private static void Postfix()`

## 原版/Harmony/native 访问点

- 第 10 行：`[HarmonyPatch]`
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
