# SkyBlockJudgeRuntime.cs

> 源码：`InFalsusChartExt/Features/SkyArea/SkyBlockJudgeRuntime.cs`
> SHA256：`D51734C8B6BA63D4F1F923AE0C0751C1B53E4D81BA69BC8CE829070EE0F844D3`
> 代码行数：1242
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

独立 SkyBlock 判定/展示

## 生命周期

谱面载入建立计划，游戏帧内更新视觉/判定，切谱回收。

## 顶层及类型声明

- `SkyBlockJudgeRuntime`（`class`，源码第 16 行）
- `PendingLayer`（`struct`，源码第 27 行）
- `DueRecord`（`struct`，源码第 33 行）

## 方法/属性职责

- `OnChartCompiled`（第 66 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void OnChartCompiled()`
- `IsManagedMember`（第 94 行）：判断当前对象/状态条件。
  - 签名：`internal static bool IsManagedMember(long id)`
- `AfterNativeJudge`（第 104 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void AfterNativeJudge(`
- `ResetForChartSwitch`（第 169 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal static void ResetForChartSwitch()`
- `EvaluateGroupStates`（第 202 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void EvaluateGroupStates(`
- `BeginGroupStateEvaluation`（第 255 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void BeginGroupStateEvaluation(`
- `GroupContains`（第 275 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool GroupContains(`
- `ConsumeDueRecords`（第 300 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void ConsumeDueRecords(`
- `ConsumeLayerRecords`（第 346 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static unsafe void ConsumeLayerRecords(`
- `AdvanceGroupStates`（第 524 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void AdvanceGroupStates(`
- `ExpandedOverlapContains`（第 547 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool ExpandedOverlapContains(`
- `HasExternalOverlap`（第 624 行）：判断当前对象/状态条件。
  - 签名：`private static bool HasExternalOverlap(`
- `FindTimelineLayer`（第 654 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static SkyJudgmentLayer FindTimelineLayer(`
- `CompareRanges`（第 682 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static int CompareRanges(`
- `MemberActiveAt`（第 689 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool MemberActiveAt(`
- `UpdateBlockVisualState`（第 694 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void UpdateBlockVisualState(`
- `TryGetMemberRedFactor`（第 714 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`internal static bool TryGetMemberRedFactor(`
- `ComputeRedFactor`（第 727 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static float ComputeRedFactor(`
- `IsMemberInNormalState`（第 741 行）：判断当前对象/状态条件。
  - 签名：`internal static bool IsMemberInNormalState(`
- `IsMemberGlowActive`（第 748 行）：判断当前对象/状态条件。
  - 签名：`internal static bool IsMemberGlowActive(`
- `IsMemberInState`（第 759 行）：判断当前对象/状态条件。
  - 签名：`private static bool IsMemberInState(`
- `IsGroupNormal`（第 769 行）：判断当前对象/状态条件。
  - 签名：`private static bool IsGroupNormal(`
- `IsGroupGlowActive`（第 779 行）：判断当前对象/状态条件。
  - 签名：`private static bool IsGroupGlowActive(`
- `AssignGlowOwner`（第 787 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void AssignGlowOwner(`
- `ClearGlowOwner`（第 844 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`private static void ClearGlowOwner(`
- `UpdateWarningState`（第 853 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void UpdateWarningState(`
- `UpdateSkyHoldFeedback`（第 918 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void UpdateSkyHoldFeedback(`
- `EnqueuePresentation`（第 977 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static unsafe bool EnqueuePresentation(`
- `EnsurePresentationQueue`（第 1031 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool EnsurePresentationQueue(`
- `FindPresentationEnqueue`（第 1062 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static IntPtr FindPresentationEnqueue(`
- `RefreshActiveLayers`（第 1110 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void RefreshActiveLayers(`
- `ResetBlockStates`（第 1174 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`private static void ResetBlockStates(`
- `CurrentPlan`（第 1210 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static SkyBlockPlan CurrentPlan()`
- `IsFinite`（第 1219 行）：判断当前对象/状态条件。
  - 签名：`private static bool IsFinite(double value)`
- `Clamp`（第 1223 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static double Clamp(`
- `LogError`（第 1229 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void LogError(`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 第 121 行：`Marshal.ReadInt64(`
- 第 123 行：`NativeLayoutCatalog.SkyJudge.ChartTime)) *`
- 第 126 行：`Marshal.ReadInt32(`
- 第 128 行：`NativeLayoutCatalog.SkyJudge.CursorX));`
- 第 184 行：`Marshal.FreeHGlobal(_presentationTupleBuffer);`
- 第 383 行：`int arrayLength = Marshal.ReadInt32(`
- 第 385 行：`NativeLayoutCatalog.ArrayLengthOffset);`
- 第 391 行：`Marshal.ReadInt64(`
- 第 393 行：`NativeLayoutCatalog.Note.RuntimeLead));`
- 第 402 行：`NativeLayoutCatalog.Record.Final) != 0 ||`
- 第 404 行：`NativeLayoutCatalog.Record.Judgment) != 0)`
- 第 409 行：`NativeLayoutCatalog.Record`
- 第 481 行：`NativeLayoutCatalog.Record.Judgment);`
- 第 484 行：`NativeLayoutCatalog.Record.Final);`
- 第 486 行：`NativeLayoutCatalog.Record.Judgment) = grade;`
- 第 488 行：`NativeLayoutCatalog.Record.Final) = 1;`
- 第 498 行：`NativeLayoutCatalog.Record.Judgment) =`
- 第 501 行：`NativeLayoutCatalog.Record.Final) =`
- 第 948 行：`Marshal.ReadInt64(`
- 第 950 行：`NativeLayoutCatalog.SkyJudge.WarningTime));`
- 第 953 行：`Marshal.ReadInt64(`
- 第 955 行：`NativeLayoutCatalog.SkyJudge.PenaltyTime));`
- 第 957 行：`Marshal.ReadByte(`
- 第 959 行：`NativeLayoutCatalog.SkyJudge.SpaceCovered) != 0;`
- 其余原生访问点从源码搜索 `Marshal`、`IL2CPP`、`NativeLayoutCatalog`。

## 所有权与释放点

- 第 77 行：`ResetForChartSwitch();`
- 第 86 行：`ActiveLayers.Clear();`
- 第 87 行：`PendingLayers.Clear();`
- 第 88 行：`DueRecords.Clear();`
- 第 169 行：`internal static void ResetForChartSwitch()`
- 第 178 行：`ActiveLayers.Clear();`
- 第 179 行：`PendingLayers.Clear();`
- 第 195 行：`DueRecords.Clear();`
- 第 196 行：`FrameGroupStates.Clear();`
- 第 197 行：`FrameBlocks.Clear();`
- 第 199 行：`SkyBlockFeedbackRuntime.ResetForChartSwitch();`
- 第 210 行：`FrameGroupStates.Clear();`
- 第 211 行：`FrameBlocks.Clear();`
- 第 360 行：`DueRecords.Clear();`
- 第 563 行：`CoverageScratch.Clear();`
- 第 844 行：`private static void ClearGlowOwner(`
- 第 905 行：`ClearGlowOwner(state);`
- 第 1118 行：`ActiveLayers.Clear();`
- 第 1119 行：`PendingLayers.Clear();`
- 第 1199 行：`ClearGlowOwner(state);`

## 性能边界

- 包含帧级入口：`UpdateBlockVisualState`, `UpdateSkyHoldFeedback`, `UpdateWarningState`。不得新增无界扫描、每帧对象创建或无界缓存。

## 对应测试

- R3/R4 contract harness
- SkyBlock alias scan
- SkyBlockOverlapHarness
- SkyBlockTestHarness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
