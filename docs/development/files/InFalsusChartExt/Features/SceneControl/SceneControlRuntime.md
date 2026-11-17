# SceneControlRuntime.cs

> 源码：`InFalsusChartExt/Features/SceneControl/SceneControlRuntime.cs`
> SHA256：`1FDDC129557D66D508CE313C6B1D9351847D17432021EBCFB79058D85422A3B1`
> 代码行数：1185
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

hidegroup/trackdisplay/shadow

## 生命周期

谱面载入、每个游戏帧和场景切换；退出谱面时释放自有资源。

## 顶层及类型声明

- `SceneControlRuntime`（`class`，源码第 19 行）
- `TrackDisplayKeyframe`（`class`，源码第 46 行）

## 方法/属性职责

- `ResetForChartSwitch`（第 88 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal static void ResetForChartSwitch()`
- `DisposeForSceneUnload`（第 113 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal static void DisposeForSceneUnload()`
- `DestroyScreenShadowResources`（第 118 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void DestroyScreenShadowResources()`
- `PrepareForFrame`（第 156 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void PrepareForFrame(double nowMs)`
- `EnsureTimeline`（第 190 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void EnsureTimeline()`
- `IsTimingGroupHidden`（第 242 行）：判断当前对象/状态条件。
  - 签名：`internal static bool IsTimingGroupHidden(int group)`
- `IsNoteHidden`（第 248 行）：判断当前对象/状态条件。
  - 签名：`internal static bool IsNoteHidden(long id)`
- `FilterSpawnList`（第 256 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void FilterSpawnList(`
- `Tick`（第 328 行）：执行帧级状态或视觉更新；必须保持无异常扩散和稳定分配。
  - 签名：`internal static void Tick(Il2Cppifapp.Game.Scenes.Game.Track track)`
- `HasTrackDisplayEvents`（第 370 行）：判断当前对象/状态条件。
  - 签名：`private static bool HasTrackDisplayEvents()`
- `EvaluateTrackAlpha`（第 375 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static float EvaluateTrackAlpha(double nowMs)`
- `AdvanceAlpha`（第 395 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static float AdvanceAlpha(`
- `GatherTrackRenderers`（第 403 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void GatherTrackRenderers(Il2Cppifapp.Game.Scenes.Game.Track track)`
- `AddHierarchy`（第 453 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void AddHierarchy(`
- `AddProtectedRoot`（第 496 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void AddProtectedRoot(IntPtr fieldAddress)`
- `AddProtectedName`（第 512 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void AddProtectedName(Transform master, string name)`
- `AddPtr`（第 523 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void AddPtr(IntPtr address)`
- `AddRendererArray`（第 535 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void AddRendererArray(IntPtr arrayObject)`
- `ApplyTrackAlpha`（第 554 行）：把当前状态应用到目标对象；不得越界修改共享原版状态。
  - 签名：`private static void ApplyTrackAlpha(`
- `ApplyMeasureLines`（第 578 行）：把当前状态应用到目标对象；不得越界修改共享原版状态。
  - 签名：`private static void ApplyMeasureLines(float alpha)`
- `RebuildMeasureLineCache`（第 597 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void RebuildMeasureLineCache()`
- `ApplyRenderer`（第 636 行）：把当前状态应用到目标对象；不得越界修改共享原版状态。
  - 签名：`private static void ApplyRenderer(MeshRenderer renderer, float alpha)`
- `GetFadeMaterial`（第 673 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static Material GetFadeMaterial(Material source)`
- `ApplyMaterialAlpha`（第 729 行）：把当前状态应用到目标对象；不得越界修改共享原版状态。
  - 签名：`private static void ApplyMaterialAlpha(`
- `ApplyAlphaColor`（第 746 行）：把当前状态应用到目标对象；不得越界修改共享原版状态。
  - 签名：`private static void ApplyAlphaColor(`
- `ClearTrackTint`（第 757 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`private static void ClearTrackTint()`
- `UpdateScreenShadow`（第 793 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void UpdateScreenShadow(`
- `GetShadowTopY`（第 846 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static float GetShadowTopY(`
- `SampleTrackEndY`（第 872 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void SampleTrackEndY(`
- `FindGameplayCamera`（第 887 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static Camera FindGameplayCamera()`
- `ComputeTrackFarDepth`（第 923 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static float ComputeTrackFarDepth(Camera camera)`
- `EnsureScreenShadow`（第 953 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void EnsureScreenShadow(`
- `ResolveGameplayCamera`（第 1020 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static Camera ResolveGameplayCamera()`
- `CreateScreenShadowMaterial`（第 1042 行）：构造运行时对象或计划；失败路径不得遗留所有权。
  - 签名：`private static Material CreateScreenShadowMaterial(`
- `FindShadowSourceMaterial`（第 1102 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static Material FindShadowSourceMaterial(GameObject prefab)`
- `CreateScreenShadowTexture`（第 1132 行）：构造运行时对象或计划；失败路径不得遗留所有权。
  - 签名：`private static Texture2D CreateScreenShadowTexture(int size)`
- `SmoothStep`（第 1161 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static float SmoothStep(float a, float b, float x)`
- `DestroyFadeMaterials`（第 1167 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void DestroyFadeMaterials()`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 第 267 行：`for (int side = 0; side < NativeLayoutCatalog.SideCount; side++)`
- 第 274 行：`IntPtr arr = Marshal.ReadIntPtr(`
- 第 275 行：`lp + NativeLayoutCatalog.Runtime.ListItems);`
- 第 276 行：`int count = Marshal.ReadInt32(`
- 第 277 行：`lp + NativeLayoutCatalog.Runtime.ListCount);`
- 第 279 行：`int capacity = Marshal.ReadInt32(`
- 第 280 行：`arr + NativeLayoutCatalog.ArrayLengthOffset);`
- 第 288 行：`int pos = Marshal.ReadInt32(data + read * 4);`
- 第 291 行：`pos * NativeLayoutCatalog.NoteStride;`
- 第 292 行：`long id = Marshal.ReadInt64(`
- 第 293 行：`note + NativeLayoutCatalog.Note.Id);`
- 第 295 行：`int noteType = Marshal.ReadInt32(`
- 第 296 行：`note + NativeLayoutCatalog.Note.Type);`
- 第 312 行：`Marshal.WriteInt32(data + write * 4, pos);`
- 第 316 行：`Marshal.WriteInt32(`
- 第 317 行：`lp + NativeLayoutCatalog.Runtime.ListCount, write);`
- 第 353 行：`IntPtr notesPtr = Marshal.ReadIntPtr(`
- 第 354 行：`track.Pointer + NativeLayoutCatalog.Track.NotesContainer);`
- 第 408 行：`track.Pointer + NativeLayoutCatalog.Track.SkyCursor);`
- 第 410 行：`track.Pointer + NativeLayoutCatalog.Track.SkyGhostCursor);`
- 第 412 行：`track.Pointer + NativeLayoutCatalog.Track.CursorExtrasContainer);`
- 第 415 行：`IntPtr skyGlow = Marshal.ReadIntPtr(`
- 第 417 行：`NativeLayoutCatalog.Track.SkyJudgementLineGlowRenderer);`
- 第 422 行：`IntPtr masterPtr = Marshal.ReadIntPtr(`
- 其余原生访问点从源码搜索 `Marshal`、`IL2CPP`、`NativeLayoutCatalog`。

## 所有权与释放点

- 第 88 行：`internal static void ResetForChartSwitch()`
- 第 90 行：`ClearTrackTint();`
- 第 91 行：`DestroyScreenShadowResources();`
- 第 92 行：`TrackRenderers.Clear();`
- 第 93 行：`TrackRendererPtrs.Clear();`
- 第 94 行：`ExcludedRendererPtrs.Clear();`
- 第 95 行：`ProtectedRoots.Clear();`
- 第 96 行：`HiddenGroups.Clear();`
- 第 97 行：`HideGroupEvents.Clear();`
- 第 98 行：`TrackDisplayTimeline.Clear();`
- 第 99 行：`MeasureLineRenderers.Clear();`
- 第 100 行：`MeasureLineRendererPtrs.Clear();`
- 第 113 行：`internal static void DisposeForSceneUnload()`
- 第 115 行：`ResetForChartSwitch();`
- 第 118 行：`private static void DestroyScreenShadowResources()`
- 第 123 行：`UnityEngine.Object.Destroy(`
- 第 130 行：`UnityEngine.Object.Destroy(_screenShadowMaterial);`
- 第 136 行：`UnityEngine.Object.Destroy(_screenShadowMesh);`
- 第 142 行：`UnityEngine.Object.Destroy(_screenShadowTexture);`
- 第 170 行：`HiddenGroups.Clear();`
- 第 196 行：`HiddenGroups.Clear();`
- 第 197 行：`HideGroupEvents.Clear();`
- 第 198 行：`TrackDisplayTimeline.Clear();`
- 第 199 行：`MeasureLineRenderers.Clear();`

## 性能边界

- 包含帧级入口：`Tick`, `UpdateScreenShadow`。不得新增无界扫描、每帧对象创建或无界缓存。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
