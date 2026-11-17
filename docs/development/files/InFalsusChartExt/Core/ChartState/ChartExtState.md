# ChartExtState.cs

> 源码：`InFalsusChartExt/Core/ChartState/ChartExtState.cs`
> SHA256：`1094152B9F241E1DFD097E794B544691692A0EAB8CE71892A487DED7D5D4ABC8`
> 代码行数：559
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

当前谱扩展全局状态

## 生命周期

Mod 启动校验、谱面载入/编译及当前谱上下文生命周期。

## 顶层及类型声明

- `ChartExtState`（`class`，源码第 7 行）
- `TrackMoveEntry`（`class`，源码第 32 行）
- `SideSlot`（`class`，源码第 49 行）
- `GroupSlot`（`class`，源码第 62 行）
- `ShowTextEntry`（`class`，源码第 76 行）
- `SceneControlEntry`（`class`，源码第 91 行）
- `SkyColorEntry`（`class`，源码第 106 行）
- `GroupSpeed`（`class`，源码第 134 行）
- `AutoplayDue`（`class`，源码第 145 行）
- `SpeedEvent`（`class`，源码第 159 行）
- `TransformSnapshot`（`class`，源码第 182 行）

## 方法/属性职责

- `TimingGroupOf`（第 129 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static int TimingGroupOf(long id)`
- `BeginTransform`（第 225 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static TransformSnapshot BeginTransform()`
- `CommitTransform`（第 235 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void CommitTransform(TransformSnapshot snapshot)`
- `RollbackTransform`（第 243 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void RollbackTransform(TransformSnapshot snapshot)`
- `PopTransform`（第 252 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void PopTransform(TransformSnapshot snapshot)`
- `CaptureTransformSnapshot`（第 276 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static TransformSnapshot CaptureTransformSnapshot()`
- `RestoreTransformSnapshot`（第 313 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void RestoreTransformSnapshot(`
- `CopyTrackMoves`（第 368 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void CopyTrackMoves(`
- `CopySideSlots`（第 389 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void CopySideSlots(`
- `CopyGroupSlots`（第 406 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void CopyGroupSlots(`
- `CopyShowTextEntries`（第 425 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void CopyShowTextEntries(`
- `CopySceneControls`（第 441 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void CopySceneControls(`
- `CopySkyColorEntries`（第 460 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void CopySkyColorEntries(`
- `CopyGroupSpeeds`（第 478 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void CopyGroupSpeeds(`
- `CopyAutoplayDues`（第 494 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void CopyAutoplayDues(`
- `CopySpeedEvents`（第 513 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void CopySpeedEvents(`
- `ResetChartState`（第 528 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal static void ResetChartState()`
- `ClearChartState`（第 536 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`private static void ClearChartState()`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 未检测到直接 native 访问。

## 所有权与释放点

- 第 231 行：`ClearChartState();`
- 第 316 行：`TrackMoves.Clear();`
- 第 318 行：`BombIds.Clear();`
- 第 320 行：`SkyAreaSlots.Clear();`
- 第 322 行：`GroupedSlots.Clear();`
- 第 324 行：`ShowTextEntries.Clear();`
- 第 329 行：`SceneControls.Clear();`
- 第 332 行：`HideGroupControlledTimingGroups.Clear();`
- 第 335 行：`SkyColorEvents.Clear();`
- 第 338 行：`SkyAreaGroupById.Clear();`
- 第 341 行：`NoInputTimingGroups.Clear();`
- 第 344 行：`NoClipTimingGroups.Clear();`
- 第 347 行：`NoteTimingGroup.Clear();`
- 第 350 行：`GroupSpeeds.Clear();`
- 第 352 行：`AutoplayDues.Clear();`
- 第 355 行：`SpeedEvents.Clear();`
- 第 357 行：`SpawnKeysNow.Clear();`
- 第 532 行：`ChartRuntimeLifecycle.Dispose();`
- 第 533 行：`ClearChartState();`
- 第 536 行：`private static void ClearChartState()`
- 第 538 行：`TrackMoves.Clear();`
- 第 539 行：`BombIds.Clear();`
- 第 540 行：`SpeedEvents.Clear();`
- 第 541 行：`SpawnKeysNow.Clear();`

## 性能边界

- 未检测到直接帧级入口；仍应在修改后复核分配和调用频率。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
