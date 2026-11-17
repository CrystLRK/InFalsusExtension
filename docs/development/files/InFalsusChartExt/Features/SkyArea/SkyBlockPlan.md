# SkyBlockPlan.cs

> 源码：`InFalsusChartExt/Features/SkyArea/SkyBlockPlan.cs`
> SHA256：`B0AD828CF9279DF30D827C24A350CD0DEE1FA5456B10A2321D28B833ABD52FD5`
> 代码行数：1080
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

block/runtime group/连接/重叠计划

## 生命周期

谱面载入建立计划，游戏帧内更新视觉/判定，切谱回收。

## 顶层及类型声明

- `SkyBlockMode`（`enum`，源码第 6 行）
- `SkyBandRange`（`struct`，源码第 11 行）
- `SkyBlockMember`（`class`，源码第 34 行）
- `SkyGroupState`（`class`，源码第 118 行）
- `SkyJudgmentLayer`（`class`，源码第 136 行）
- `SkyBlock`（`class`，源码第 147 行）
- `SkyBlockPlan`（`class`，源码第 176 行）
- `SkyBlockPolicy`（`class`，源码第 1074 行）

## 方法/属性职责

- `Contains`（第 22 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal bool Contains(float value, float tolerance)`
- `Overlaps`（第 25 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal bool Overlaps(SkyBandRange other, float tolerance)`
- `OverlapsWithArea`（第 29 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal bool OverlapsWithArea(SkyBandRange other)`
- `RangeAt`（第 76 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal SkyBandRange RangeAt(double timeMs)`
- `Ease`（第 99 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static double Ease(int code, double value)`
- `IsVisualMember`（第 201 行）：判断当前对象/状态条件。
  - 签名：`internal bool IsVisualMember(long id)`
- `IsJudgeMember`（第 204 行）：判断当前对象/状态条件。
  - 签名：`internal bool IsJudgeMember(long id)`
- `ApplyRuntimeGroups`（第 213 行）：把当前状态应用到目标对象；不得越界修改共享原版状态。
  - 签名：`internal int ApplyRuntimeGroups(`
- `Clear`（第 278 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal void Clear()`
- `TryBuild`（第 292 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`internal static SkyBlockPlan TryBuild(`
- `Build`（第 316 行）：构造运行时对象或计划；失败路径不得遗留所有权。
  - 签名：`private void Build(IReadOnlyList<Icp1Codec.Note> notes)`
- `BuildGroupStates`（第 542 行）：构造运行时对象或计划；失败路径不得遗留所有权。
  - 签名：`private static void BuildGroupStates(SkyBlock block)`
- `ValidateGroupStateOwnership`（第 565 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void ValidateGroupStateOwnership(`
- `BuildOverlapPeers`（第 587 行）：构造运行时对象或计划；失败路径不得遗留所有权。
  - 签名：`private void BuildOverlapPeers()`
- `MarkIndependentJudgeBlocks`（第 639 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private void MarkIndependentJudgeBlocks()`
- `BuildVisualAndJudgeLists`（第 682 行）：构造运行时对象或计划；失败路径不得遗留所有权。
  - 签名：`private void BuildVisualAndJudgeLists()`
- `BlocksOverlapAtTime`（第 744 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool BlocksOverlapAtTime(`
- `MembersOverlapAcross`（第 764 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool MembersOverlapAcross(`
- `MembersOverlapInterval`（第 776 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool MembersOverlapInterval(`
- `RangesOverlapAt`（第 817 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool RangesOverlapAt(`
- `BoundaryRate`（第 826 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static double BoundaryRate(`
- `BuildTimeline`（第 849 行）：构造运行时对象或计划；失败路径不得遗留所有权。
  - 签名：`private void BuildTimeline(SkyBlock block)`
- `AddOrMergeRange`（第 930 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void AddOrMergeRange(`
- `MergeRenderRanges`（第 965 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static SkyBandRange MergeRenderRanges(`
- `TryReadGeometry`（第 981 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`private static bool TryReadGeometry(`
- `CanConnect`（第 1012 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool CanConnect(`
- `ModeOf`（第 1045 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static SkyBlockMode ModeOf(Icp1Codec.Note note)`
- `DecodeEase`（第 1051 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static int DecodeEase(`
- `IsFinite`（第 1059 行）：判断当前对象/状态条件。
  - 签名：`private static bool IsFinite(double value)`
- `Find`（第 1063 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static int Find(int[] parent, int index)`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 未检测到直接 native 访问。

## 所有权与释放点

- 第 278 行：`internal void Clear()`
- 第 280 行：`MemberById.Clear();`
- 第 281 行：`Members.Clear();`
- 第 282 行：`VisualMembers.Clear();`
- 第 283 行：`JudgeMembers.Clear();`
- 第 284 行：`VisualMemberIds.Clear();`
- 第 285 行：`JudgeMemberIds.Clear();`
- 第 286 行：`Blocks.Clear();`
- 第 287 行：`SharedLayers.Clear();`
- 第 288 行：`JudgeLayers.Clear();`
- 第 311 行：`plan.Clear();`
- 第 544 行：`block.GroupStates.Clear();`

## 性能边界

- 未检测到直接帧级入口；仍应在修改后复核分配和调用频率。

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
