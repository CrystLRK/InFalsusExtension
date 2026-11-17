# ChartIngress.cs

> 源码：`InFalsusChartExt/Core/ChartModel/ChartIngress.cs`
> SHA256：`FB273B4C88B5693AE64A14D4E7CDA9DD4AEA1B162BFA2C864BD4E6F9D8D46CD9`
> 代码行数：243
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

native/visual 分区与规则快照

## 生命周期

Mod 启动校验、谱面载入/编译及当前谱上下文生命周期。

## 顶层及类型声明

- `ChartIngressResult`（`class`，源码第 7 行）
- `ChartIngressPartition`（`class`，源码第 29 行）
- `ChartIngress`（`class`，源码第 50 行）

## 方法/属性职责

- `Dispose`（第 19 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal void Dispose()`
- `NativeOnly`（第 52 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static ChartIngressResult NativeOnly(`
- `FromCurrentState`（第 73 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static ChartIngressResult FromCurrentState(`
- `PartitionVisualNotes`（第 101 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static ChartIngressPartition PartitionVisualNotes(`
- `Contains`（第 183 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool Contains(`
- `NewResult`（第 195 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static ChartIngressResult NewResult(`
- `SnapshotNative`（第 213 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void SnapshotNative(`
- `SnapshotRules`（第 225 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void SnapshotRules(ChartRuntimeRules target)`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 未检测到直接 native 访问。

## 所有权与释放点

- 第 19 行：`internal void Dispose()`
- 第 21 行：`NativeChart?.Clear();`
- 第 22 行：`VisualChart?.Clear();`
- 第 23 行：`Rules?.Clear();`
- 第 24 行：`SkyBlocks?.Clear();`

## 性能边界

- 未检测到直接帧级入口；仍应在修改后复核分配和调用频率。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
