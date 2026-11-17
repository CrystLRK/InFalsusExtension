# SkyColorRuntime.cs

> 源码：`InFalsusChartExt/Features/SkyArea/SkyColorRuntime.cs`
> SHA256：`523F4556A4E4AA148763C2610D353BE067400350DAF80A35BDF68DAF588019DA`
> 代码行数：625
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

setcolor、bracket/cursor tint

## 生命周期

谱面载入建立计划，游戏帧内更新视觉/判定，切谱回收。

## 顶层及类型声明

- `SkyColorRuntime`（`class`，源码第 12 行）
- `ColorState`（`class`，源码第 42 行）
- `ColorZone`（`struct`，源码第 76 行）

## 方法/属性职责

- `ResetForChartSwitch`（第 87 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal static void ResetForChartSwitch()`
- `Tick`（第 140 行）：执行帧级状态或视觉更新；必须保持无异常扩散和稳定分配。
  - 签名：`internal static void Tick(Il2Cppifapp.Game.Scenes.Game.Track track)`
- `AddActiveZone`（第 214 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void AddActiveZone(`
- `GetZoneRenderer`（第 245 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static MeshRenderer GetZoneRenderer(`
- `ApplyBracketAndCursorTints`（第 262 行）：把当前状态应用到目标对象；不得越界修改共享原版状态。
  - 签名：`private static void ApplyBracketAndCursorTints(`
- `GetCursorTransform`（第 335 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static Transform GetCursorTransform(IntPtr pointer)`
- `GetCursorRenderer`（第 346 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static MeshRenderer GetCursorRenderer(IntPtr pointer)`
- `RebuildBracketCache`（第 357 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void RebuildBracketCache()`
- `TryZoneColorAt`（第 402 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`private static bool TryZoneColorAt(Vector3 position, out uint rgba)`
- `ApplyTint`（第 439 行）：把当前状态应用到目标对象；不得越界修改共享原版状态。
  - 签名：`private static void ApplyTint(MeshRenderer renderer, uint rgba)`
- `BuildCandidates`（第 457 行）：构造运行时对象或计划；失败路径不得遗留所有权。
  - 签名：`private static void BuildCandidates()`
- `TryGetActiveColor`（第 498 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`internal static bool TryGetActiveColor(`
- `GetClone`（第 535 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`internal static Il2Cppifapp.Game.Scenes.Game.SafeAreaColorDetails GetClone(`
- `FindSafeArea`（第 570 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static Il2Cppifapp.Game.Scenes.Game.SkySafeArea FindSafeArea(`
- `ApplyDefaultColors`（第 592 行）：把当前状态应用到目标对象；不得越界修改共享原版状态。
  - 签名：`private static void ApplyDefaultColors(`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 第 15 行：`NativeLayoutCatalog.SkySafeArea.ColorDetails;`
- 第 17 行：`NativeLayoutCatalog.SafeAreaColorDetails.MainDefault;`
- 第 19 行：`NativeLayoutCatalog.SafeAreaColorDetails.ColorStride;`
- 第 21 行：`NativeLayoutCatalog.SafeAreaColorDetails.EdgeGlowRed;`
- 第 23 行：`NativeLayoutCatalog.SafeAreaColorDetails.EdgeCenterRed;`
- 第 100 行：`Marshal.WriteIntPtr(area.Pointer + ColorDetailsOffset, source);`
- 第 187 行：`original = Marshal.ReadIntPtr(area.Pointer + ColorDetailsOffset);`
- 第 192 行：`Marshal.WriteIntPtr(area.Pointer + ColorDetailsOffset, clone.Pointer);`
- 第 249 行：`IntPtr rendererPtr = Marshal.ReadIntPtr(`
- 第 250 行：`area.Pointer + NativeLayoutCatalog.SkySafeArea.MeshRenderer);`
- 第 314 行：`IntPtr cursorTransformPtr = Marshal.ReadIntPtr(`
- 第 315 行：`track.Pointer + NativeLayoutCatalog.Track.SkyCursor);`
- 第 316 行：`IntPtr cursorRendererPtr = Marshal.ReadIntPtr(`
- 第 317 行：`track.Pointer + NativeLayoutCatalog.Track.SkyCursorRenderer);`
- 第 602 行：`Marshal.Copy(source + offset, SourceColor, 0, 4);`
- 第 606 行：`Marshal.Copy(OutputColor, 0, target + offset, 4);`
- 第 607 行：`Marshal.Copy(`
- 第 621 行：`Marshal.Copy(OutputColor, 0, target + offset, 4);`

## 所有权与释放点

- 第 87 行：`internal static void ResetForChartSwitch()`
- 第 110 行：`UnityEngine.Object.Destroy(clone);`
- 第 117 行：`Applied.Clear();`
- 第 118 行：`OriginalDetails.Clear();`
- 第 119 行：`AppliedRgba.Clear();`
- 第 120 行：`AppliedEdgeGlow.Clear();`
- 第 121 行：`Clones.Clear();`
- 第 122 行：`CandidateIdsByGroup.Clear();`
- 第 123 行：`ColorEventsByGroup.Clear();`
- 第 124 行：`ColorStates.Clear();`
- 第 125 行：`ActiveZones.Clear();`
- 第 126 行：`ZoneRenderers.Clear();`
- 第 127 行：`BracketRenderers.Clear();`
- 第 128 行：`BracketRendererPtrs.Clear();`
- 第 156 行：`ActiveZones.Clear();`
- 第 236 行：`BracketRenderers.Clear();`
- 第 237 行：`BracketRendererPtrs.Clear();`
- 第 359 行：`BracketRenderers.Clear();`
- 第 360 行：`BracketRendererPtrs.Clear();`
- 第 448 行：`_tintBlock.Clear();`
- 第 459 行：`CandidateIdsByGroup.Clear();`
- 第 460 行：`ColorEventsByGroup.Clear();`
- 第 461 行：`ColorStates.Clear();`
- 第 559 行：`UnityEngine.Object.Destroy(clone);`

## 性能边界

- 包含帧级入口：`Tick`。不得新增无界扫描、每帧对象创建或无界缓存。

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
