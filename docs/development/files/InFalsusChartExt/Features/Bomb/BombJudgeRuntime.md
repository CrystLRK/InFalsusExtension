# BombJudgeRuntime.cs

> 源码：`InFalsusChartExt/Features/Bomb/BombJudgeRuntime.cs`
> SHA256：`DB91A6A6E7D78ECE41E0150B0F47E84F4D66A1828F92AB69CE74E61572ACB8A0`
> 代码行数：554
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

独立 Bomb record/`_PEA` 判定

## 生命周期

谱面载入建立计划，判定与渲染阶段消费，切谱回收。

## 顶层及类型声明

- `BombJudgeRuntime`（`class`，源码第 16 行）
- `BombState`（`class`，源码第 22 行）

## 方法/属性职责

- `OnChartCompiled`（第 44 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void OnChartCompiled(`
- `AfterNativeJudge`（第 104 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static unsafe void AfterNativeJudge(`
- `ResetForChartSwitch`（第 212 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal static void ResetForChartSwitch()`
- `ResetProgress`（第 225 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`private static void ResetProgress()`
- `FinalizeBomb`（第 233 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static unsafe void FinalizeBomb(`
- `TryGetNativeNote`（第 312 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`private static bool TryGetNativeNote(`
- `TryReadHeldState`（第 334 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`private static bool TryReadHeldState(`
- `IsNewPress`（第 371 行）：判断当前对象/状态条件。
  - 签名：`internal static bool IsNewPress(`
- `EnqueuePresentation`（第 377 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static unsafe bool EnqueuePresentation(`
- `WritePresentationTuple`（第 413 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void WritePresentationTuple(`
- `EnsurePresentationQueue`（第 446 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool EnsurePresentationQueue(`
- `FindPresentationEnqueue`（第 478 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static IntPtr FindPresentationEnqueue(`
- `RemoveStateAt`（第 526 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void RemoveStateAt(int index)`
- `CurrentPlan`（第 533 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static BombPlan CurrentPlan()`
- `LogError`（第 540 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void LogError(`
- `IsFinite`（第 550 行）：判断当前对象/状态条件。
  - 签名：`private static bool IsFinite(double value)`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 第 67 行：`side < NativeLayoutCatalog.SideCount;`
- 第 85 行：`long id = Marshal.ReadInt64(`
- 第 88 行：`NativeLayoutCatalog.NoteStride +`
- 第 89 行：`NativeLayoutCatalog.Note.Id);`
- 第 118 行：`Marshal.ReadInt64(`
- 第 120 行：`NativeLayoutCatalog.SkyJudge.ChartTime)) *`
- 第 219 行：`Marshal.FreeHGlobal(_presentationTupleBuffer);`
- 第 256 行：`int arrayLength = Marshal.ReadInt32(`
- 第 258 行：`NativeLayoutCatalog.ArrayLengthOffset);`
- 第 274 行：`int laneFirst = Marshal.ReadInt32(`
- 第 275 行：`note + NativeLayoutCatalog.Note.LaneFirst);`
- 第 276 行：`int laneLast = Marshal.ReadInt32(`
- 第 277 行：`note + NativeLayoutCatalog.Note.LaneLast);`
- 第 323 行：`NativeLayoutCatalog.NoteStride;`
- 第 324 行：`return Marshal.ReadInt64(`
- 第 325 行：`note + NativeLayoutCatalog.Note.Id) ==`
- 第 342 行：`IntPtr states = Marshal.ReadIntPtr(`
- 第 343 行：`judge + NativeLayoutCatalog.SkyJudge.InputStates);`
- 第 346 行：`int stateCount = Marshal.ReadInt32(`
- 第 347 行：`states + NativeLayoutCatalog.ArrayLengthOffset);`
- 第 348 行：`int start = Marshal.ReadInt32(`
- 第 349 行：`note + NativeLayoutCatalog.Note.LaneFirst);`
- 第 350 行：`int end = Marshal.ReadInt32(`
- 第 351 行：`note + NativeLayoutCatalog.Note.LaneLast);`
- 其余原生访问点从源码搜索 `Marshal`、`IL2CPP`、`NativeLayoutCatalog`。

## 所有权与释放点

- 第 49 行：`ResetForChartSwitch();`
- 第 212 行：`internal static void ResetForChartSwitch()`
- 第 227 行：`StateById.Clear();`
- 第 228 行：`Active.Clear();`

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
