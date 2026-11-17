# TimingGroupLiveTapPatch.cs

> 源码：`InFalsusChartExt/Patches/TimingGroupLiveTapPatch.cs`
> SHA256：`CC5415677C3EA7F6546B1C15CDAE4741BDF4D8C281C692F33C5177BC3629D057`
> 代码行数：26
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

由文件名、顶层类型和调用方确定。

## 生命周期

Harmony 目标方法调用时；生命周期跟随原版调用方。

## 顶层及类型声明

- `TimingGroupLiveTapPatch`（`class`，源码第 7 行）

## 方法/属性职责

- `Prepare`（第 10 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool Prepare() =>`
- `TargetMethod`（第 15 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static MethodBase TargetMethod() =>`
- `Postfix`（第 20 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Postfix(`

## 原版/Harmony/native 访问点

- 第 6 行：`[HarmonyPatch]`
- 未检测到直接 native 访问。

## 所有权与释放点

- 未检测到显式资源清理代码；修改时需确认调用方所有权。

## 性能边界

- 未检测到直接帧级入口；仍应在修改后复核分配和调用频率。

## 对应测试

- FrameScanHarness
- R3/R4 contract harness
- TimingGroupTransactionHarness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
