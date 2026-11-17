# AutoplaySettingsPatches.cs

> 源码：`InFalsusChartExt/Patches/AutoplaySettingsPatches.cs`
> SHA256：`DF3BDD12BF25E2997823407CD0641C51C32D9A77D80514A96F787DA3B36714EB`
> 代码行数：128
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

Harmony 目标方法调用时；生命周期跟随原版调用方。

## 顶层及类型声明

- `AutoplaySettingsPatchResolver`（`class`，源码第 6 行）
- `AutoplaySettingsAwakePatch`（`class`，源码第 24 行）
- `AutoplaySettingsUpdatePatch`（`class`，源码第 56 行）
- `AutoplaySettingsLayerHidePatch`（`class`，源码第 88 行）
- `AutoplaySettingsLayerDestroyPatch`（`class`，源码第 109 行）

## 方法/属性职责

- `FindNamed`（第 8 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static MethodBase FindNamed(`
- `Prepare`（第 27 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool Prepare()`
- `TargetMethod`（第 40 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static MethodBase TargetMethod() =>`
- `Postfix`（第 47 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Postfix(`
- `Prepare`（第 59 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool Prepare()`
- `TargetMethod`（第 72 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static MethodBase TargetMethod() =>`
- `Postfix`（第 79 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Postfix(`
- `Prepare`（第 91 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool Prepare() =>`
- `TargetMethod`（第 95 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static MethodBase TargetMethod() =>`
- `Postfix`（第 102 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Postfix()`
- `Prepare`（第 112 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool Prepare() =>`
- `TargetMethod`（第 116 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static MethodBase TargetMethod() =>`
- `Postfix`（第 123 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Postfix()`

## 原版/Harmony/native 访问点

- 第 23 行：`[HarmonyPatch]`
- 第 55 行：`[HarmonyPatch]`
- 第 87 行：`[HarmonyPatch]`
- 第 108 行：`[HarmonyPatch]`
- 未检测到直接 native 访问。

## 所有权与释放点

- 第 109 行：`internal static class AutoplaySettingsLayerDestroyPatch`
- 第 120 行：`"OnDestroy");`

## 性能边界

- 未检测到直接帧级入口；仍应在修改后复核分配和调用频率。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
