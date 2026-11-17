# TimingGroupRuntime.cs

> 源码：`InFalsusChartExt/Features/TimingGroups/TimingGroupRuntime.cs`
> SHA256：`C29E367E3674DB353B2B9FE5B684BF8EE265EC32B9CFB0A07D648153BF72DBA4`
> 代码行数：1257
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

speed curve、warp session、pause clock

## 生命周期

谱面编译建立时间线；Track.Update 和判定阶段读取，切谱释放。

## 顶层及类型声明

- `TimingGroupRuntime`（`class`，源码第 21 行）
- `WarpSession`（`class`，源码第 48 行）
- `SpeedCurve`（`class`，源码第 78 行）

## 方法/属性职责

- `Build`（第 86 行）：构造运行时对象或计划；失败路径不得遗留所有权。
  - 签名：`internal void Build(`
- `Evaluate`（第 117 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal double Evaluate(double t)`
- `Inverse`（第 126 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal double Inverse(double target)`
- `FirstEventAtOrAfter`（第 153 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private int FirstEventAtOrAfter(double t)`
- `FirstWindAtOrAbove`（第 166 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private int FirstWindAtOrAbove(double target)`
- `ApplyWarp`（第 182 行）：把当前状态应用到目标对象；不得越界修改共享原版状态。
  - 签名：`internal static void ApplyWarp()`
- `Restore`（第 371 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void Restore()`
- `BeginCompile`（第 485 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void BeginCompile()`
- `ResetForChartSwitch`（第 498 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal static void ResetForChartSwitch()`
- `ApplyVisualFields`（第 519 行）：把当前状态应用到目标对象；不得越界修改共享原版状态。
  - 签名：`internal static void ApplyVisualFields()`
- `AcquireSession`（第 575 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static WarpSession AcquireSession()`
- `ClearSessions`（第 582 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`private static void ClearSessions()`
- `TryResolveSlotNote`（第 587 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`private static bool TryResolveSlotNote(`
- `TryResolveGroupedSlot`（第 614 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`internal static bool TryResolveGroupedSlot(`
- `HasCurrentCompiledIdentity`（第 645 行）：判断当前对象/状态条件。
  - 签名：`internal static bool HasCurrentCompiledIdentity(`
- `TryResolveSessionNote`（第 656 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`private static bool TryResolveSessionNote(`
- `TryGetCompiledOwner`（第 673 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`private static bool TryGetCompiledOwner(out IntPtr owner)`
- `TryResolveWarpTable`（第 695 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`private static bool TryResolveWarpTable(`
- `RestoreInt32`（第 737 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void RestoreInt32(`
- `RestoreInt64`（第 758 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void RestoreInt64(`
- `IsSameSlotIdentity`（第 779 行）：判断当前对象/状态条件。
  - 签名：`private static bool IsSameSlotIdentity(`
- `IsSameOwner`（第 785 行）：判断当前对象/状态条件。
  - 签名：`private static bool IsSameOwner(`
- `ValueStillOwned`（第 795 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool ValueStillOwned(`
- `LogRestoreSkip`（第 804 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void LogRestoreSkip(string detail)`
- `NowMs`（第 812 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static double NowMs()`
- `ShouldHoldVisualClock`（第 839 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static bool ShouldHoldVisualClock(`
- `CurrentMs`（第 846 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static double CurrentMs() => NowMs();`
- `IsPauseLayerActive`（第 855 行）：判断当前对象/状态条件。
  - 签名：`internal static bool IsPauseLayerActive()`
- `IsResumeTransition`（第 876 行）：判断当前对象/状态条件。
  - 签名：`internal static bool IsResumeTransition()`
- `NoClipTailMs`（第 887 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static double NoClipTailMs()`
- `DebugWGroup`（第 904 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static double DebugWGroup(int group, double t) => WGroup(group, t);`
- `VisualTimeMs`（第 908 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static double VisualTimeMs(int group, double t)`
- `TryGetNativeVisualTiming`（第 920 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`internal static bool TryGetNativeVisualTiming(`
- `TryGetCompiledVisualTimes`（第 965 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`internal static bool TryGetCompiledVisualTimes(`
- `EnsureCompiledVisualTable`（第 1023 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool EnsureCompiledVisualTable()`
- `ResetVisualTableCache`（第 1057 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`private static void ResetVisualTableCache()`
- `IsFinite`（第 1065 行）：判断当前对象/状态条件。
  - 签名：`private static bool IsFinite(double value)`
- `EnsureCurves`（第 1071 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void EnsureCurves()`
- `WGlobal`（第 1123 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static double WGlobal(double t)`
- `WGroup`（第 1129 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static double WGroup(int group, double t)`
- `InvGlobal`（第 1151 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static double InvGlobal(double target, double nowMs)`
- `InvGlobalSlow`（第 1164 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static double InvGlobalSlow(double target, double nowMs)`
- `ConsiderInverseSegment`（第 1229 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void ConsiderInverseSegment(`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 第 222 行：`int be = Marshal.ReadInt32(`
- 第 223 行：`note + NativeLayoutCatalog.Note.StartMs);`
- 第 253 行：`int end = Marshal.ReadInt32(`
- 第 254 行：`note + NativeLayoutCatalog.Note.EndMs);`
- 第 277 行：`long id = Marshal.ReadInt64(`
- 第 278 行：`note + NativeLayoutCatalog.Note.Id);`
- 第 284 行：`NativeLayoutCatalog.Compiled.WarpEntryStride);`
- 第 285 行：`long origBits = Marshal.ReadInt64(entry);`
- 第 286 行：`long orig2Bits = Marshal.ReadInt64(new IntPtr(`
- 第 288 行：`NativeLayoutCatalog.Compiled.WarpEntryEnd));`
- 第 354 行：`Marshal.WriteInt64(`
- 第 356 行：`Marshal.WriteInt64(`
- 第 359 行：`NativeLayoutCatalog.Compiled.WarpEntryEnd),`
- 第 410 行：`note + NativeLayoutCatalog.Note.StartMs,`
- 第 417 行：`note + NativeLayoutCatalog.Note.EndMs,`
- 第 424 行：`note + NativeLayoutCatalog.Note.RuntimeLead,`
- 第 452 行：`NativeLayoutCatalog.Compiled.WarpEntryStride);`
- 第 463 行：`NativeLayoutCatalog.Compiled.WarpEntryEnd),`
- 第 542 行：`Marshal.ReadInt64(`
- 第 544 行：`NativeLayoutCatalog.Note.RuntimeLead);`
- 第 549 行：`Marshal.WriteInt32(`
- 第 550 行：`note + NativeLayoutCatalog.Note.StartMs,`
- 第 553 行：`Marshal.WriteInt32(`
- 第 554 行：`note + NativeLayoutCatalog.Note.EndMs,`
- 其余原生访问点从源码搜索 `Marshal`、`IL2CPP`、`NativeLayoutCatalog`。

## 所有权与释放点

- 第 89 行：`_times.Clear();`
- 第 90 行：`_windAt.Clear();`
- 第 91 行：`_multAfter.Clear();`
- 第 215 行：`ClearSessions();`
- 第 477 行：`ClearSessions();`
- 第 498 行：`internal static void ResetForChartSwitch()`
- 第 507 行：`WindowDiagnostic.Clear();`
- 第 582 行：`private static void ClearSessions()`
- 第 1095 行：`GroupCurves.Clear();`

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
