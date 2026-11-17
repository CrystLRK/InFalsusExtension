# VisualSpawnTiming.cs

> 源码：`InFalsusChartExt/Features/VisualNotes/VisualSpawnTiming.cs`
> SHA256：`72C9135EFDB9A525FB3B6AC56DF74F40DD74AAADFB5155B37BF4BC0DB776CD1E`
> 代码行数：50
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

视觉提前量计算

## 生命周期

谱面载入建立计划，游戏帧内更新视觉/判定，切谱回收。

## 顶层及类型声明

- `VisualSpawnTiming`（`class`，源码第 12 行）

## 方法/属性职责

- `IsFiniteTime`（第 16 行）：判断当前对象/状态条件。
  - 签名：`internal static bool IsFiniteTime(double value)`
- `MaxLeadMs`（第 20 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static double MaxLeadMs(`
- `MaxLeadMs`（第 37 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static double MaxLeadMs(IntPtr track)`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 第 42 行：`Marshal.ReadInt64(`
- 第 43 行：`track + NativeLayoutCatalog.Track.BaseSpeed));`
- 第 45 行：`Marshal.ReadInt64(`
- 第 46 行：`track + NativeLayoutCatalog.Track.DepthFactor));`

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
