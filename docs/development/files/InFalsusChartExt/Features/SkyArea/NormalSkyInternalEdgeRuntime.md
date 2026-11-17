# NormalSkyInternalEdgeRuntime.cs

> 源码：`InFalsusChartExt/Features/SkyArea/NormalSkyInternalEdgeRuntime.cs`
> SHA256：`F2542D2619B760A5C38443BEF8BD7A33F313409A68D6AC69D100BCE42B0575D2`
> 代码行数：452
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

正常 SkyArea 连接端部分重合包络与 MissingRange

## 生命周期

谱面载入建立计划，游戏帧内更新视觉/判定，切谱回收。

## 顶层及类型声明

- `NormalSkyInternalEdgeRuntime`（`class`，源码第 19 行）
- `Entry`（`class`，源码第 25 行）
- `NoteInfo`（`class`，源码第 36 行）

## 方法/属性职责

- `Tick`（第 69 行）：执行帧级状态或视觉更新；必须保持无异常扩散和稳定分配。
  - 签名：`internal static void Tick(Il2Cppifapp.Game.Scenes.Game.Track track)`
- `EnsureChart`（第 113 行）：等待原生 SkyArea 槽位稳定后构建一次连接图；同 timing group、同 authored group 的时间相接段按端点交集生成部分缺失范围。
  - 签名：`private static bool EnsureChart(`
- `AddEnvelope`（第 319 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void AddEnvelope(`
- `CoversEndpoint`（第 335 行）：判断连接端点交集是否覆盖完整端面；完整覆盖时不创建多余的逐帧 `_MissingRange` 工作。
  - 签名：`private static bool CoversEndpoint(`
- `ResetForChartSwitch`（第 344 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal static void ResetForChartSwitch()`
- `SlotSignature`（第 354 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static ulong SlotSignature()`
- `MixHash`（第 374 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static ulong MixHash(ulong hash, long value)`
- `ReadFloat`（第 380 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static float ReadFloat(IntPtr address)`
- `ApplyPool`（第 386 行）：把当前状态应用到目标对象；不得越界修改共享原版状态。
  - 签名：`private static void ApplyPool(`
- `ApplyEntries`（第 396 行）：把当前状态应用到目标对象；不得越界修改共享原版状态。
  - 签名：`private static void ApplyEntries(`
- `ClearOwnedRenderers`（第 432 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`private static void ClearOwnedRenderers()`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 第 87 行：`entry.Position * NativeLayoutCatalog.NoteStride;`
- 第 88 行：`if (Marshal.ReadInt64(notePtr) != entry.Id) continue;`
- 第 141 行：`slot.Position * NativeLayoutCatalog.NoteStride;`
- 第 142 行：`if (Marshal.ReadInt64(notePtr) != slot.Id ||`
- 第 143 行：`Marshal.ReadInt32(`
- 第 144 行：`notePtr + NativeLayoutCatalog.Note.Type) != 5)`
- 第 170 行：`slot.Position * NativeLayoutCatalog.NoteStride;`
- 第 171 行：`long id = Marshal.ReadInt64(notePtr);`
- 第 175 行：`if (Marshal.ReadInt32(`
- 第 176 行：`notePtr + NativeLayoutCatalog.Note.Type) != 5)`
- 第 180 行：`notePtr + NativeLayoutCatalog.Note.StartCenter);`
- 第 182 行：`notePtr + NativeLayoutCatalog.Note.EndCenter);`
- 第 184 行：`notePtr + NativeLayoutCatalog.Note.StartWidth));`
- 第 186 行：`notePtr + NativeLayoutCatalog.Note.EndWidth));`
- 第 193 行：`Group = Marshal.ReadInt64(`
- 第 194 行：`notePtr + NativeLayoutCatalog.Note.Group),`
- 第 195 行：`StartMs = Marshal.ReadInt32(`
- 第 196 行：`notePtr + NativeLayoutCatalog.Note.StartMs),`
- 第 197 行：`EndMs = Marshal.ReadInt32(`
- 第 198 行：`notePtr + NativeLayoutCatalog.Note.EndMs),`
- 第 366 行：`slot.Position * NativeLayoutCatalog.NoteStride;`
- 第 367 行：`hash = MixHash(hash, Marshal.ReadInt64(notePtr));`
- 第 368 行：`hash = MixHash(hash, Marshal.ReadInt32(`
- 第 369 行：`notePtr + NativeLayoutCatalog.Note.Type));`
- 其余原生访问点从源码搜索 `Marshal`、`IL2CPP`、`NativeLayoutCatalog`。

## 所有权与释放点

- 第 56 行：`private static readonly MaterialPropertyBlock ClearBlock =`
- 第 78 行：`ClearOwnedRenderers();`
- 第 124 行：`Entries.Clear();`
- 第 156 行：`Entries.Clear();`
- 第 315 行：`RendererViewCache.Clear();`
- 第 344 行：`internal static void ResetForChartSwitch()`
- 第 346 行：`ClearOwnedRenderers();`
- 第 347 行：`Entries.Clear();`
- 第 424 行：`Block.Clear();`
- 第 432 行：`private static void ClearOwnedRenderers()`
- 第 445 行：`renderer.SetPropertyBlock(ClearBlock);`
- 第 449 行：`OwnedRenderers.Clear();`

## 性能边界

- 包含帧级入口：`Tick`。不得新增无界扫描、每帧对象创建或无界缓存。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
