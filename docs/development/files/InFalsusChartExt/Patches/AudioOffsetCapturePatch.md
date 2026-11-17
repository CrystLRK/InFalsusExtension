# AudioOffsetCapturePatch.cs

> 源码：`InFalsusChartExt/Patches/AudioOffsetCapturePatch.cs`
> SHA256：`77560BDE4DB8EACA861D56EB2B29C861B3EE9D1A0C2CC5C5262AE39822032D79`
> 代码行数：28
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

Harmony 目标方法调用时；生命周期跟随原版调用方。

## 顶层及类型声明

- `AudioOffsetCapturePatch`（`class`，源码第 12 行）

## 方法/属性职责

- `Prepare`（第 15 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool Prepare() =>`
- `TargetMethod`（第 19 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static MethodBase TargetMethod() =>`
- `Postfix`（第 23 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Postfix(_Cg __result, bool __6)`

## 原版/Harmony/native 访问点

- 第 11 行：`[HarmonyPatch]`
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
