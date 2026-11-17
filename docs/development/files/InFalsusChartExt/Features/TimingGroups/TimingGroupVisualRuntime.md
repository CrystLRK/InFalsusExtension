# TimingGroupVisualRuntime.cs

> 源码：`InFalsusChartExt/Features/TimingGroups/TimingGroupVisualRuntime.cs`
> SHA256：`AB289069828D6521D661DA1D07882667AD9541269E88EB71F62C7629F4D741DD`
> 代码行数：662
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

grouped Tap 独立视觉

## 生命周期

谱面编译建立时间线；Track.Update 和判定阶段读取，切谱释放。

## 顶层及类型声明

- `TimingGroupVisualRuntime`（`class`，源码第 18 行）
- `VisualEntry`（`class`，源码第 64 行）

## 方法/属性职责

- `RegisterTap`（第 76 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void RegisterTap(`
- `ApplyFlip`（第 120 行）：把当前状态应用到目标对象；不得越界修改共享原版状态。
  - 签名：`internal static void ApplyFlip(bool spawnPhase)`
- `RollbackFlipWrites`（第 188 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void RollbackFlipWrites(int count)`
- `LogFlipIdentityMismatch`（第 206 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void LogFlipIdentityMismatch(`
- `Tick`（第 216 行）：执行帧级状态或视觉更新；必须保持无异常扩散和稳定分配。
  - 签名：`internal static void Tick(Il2Cppifapp.Game.Scenes.Game.Track track)`
- `ResetForChartSwitch`（第 372 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal static void ResetForChartSwitch()`
- `CreateEntry`（第 381 行）：构造运行时对象或计划；失败路径不得遗留所有权。
  - 签名：`private static VisualEntry CreateEntry(int lane)`
- `CloneTemplate`（第 446 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static GameObject CloneTemplate(`
- `EnsureTapTemplate`（第 475 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void EnsureTapTemplate(`
- `ApplyTapGuideSetting`（第 538 行）：把当前状态应用到目标对象；不得越界修改共享原版状态。
  - 签名：`private static void ApplyTapGuideSetting(`
- `CloneMaterials`（第 559 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static List<Material> CloneMaterials(GameObject root)`
- `DestroyEntries`（第 598 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void DestroyEntries()`
- `DestroyTemplates`（第 610 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void DestroyTemplates()`
- `DestroyMaterials`（第 642 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void DestroyMaterials(`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 第 152 行：`note + NativeLayoutCatalog.Note.Type,`
- 第 163 行：`Marshal.WriteInt32(target.Address, target.TargetType);`
- 第 197 行：`if (Marshal.ReadInt32(target.Address) ==`
- 第 199 行：`Marshal.WriteInt32(`
- 第 260 行：`long id = Marshal.ReadInt64(`
- 第 261 行：`note + NativeLayoutCatalog.Note.Id);`
- 第 265 行：`int be = Marshal.ReadInt32(`
- 第 266 行：`note + NativeLayoutCatalog.Note.StartMs);`
- 第 267 行：`int lane = Marshal.ReadInt32(`
- 第 268 行：`note + NativeLayoutCatalog.Note.Lane);`
- 第 269 行：`int width = Marshal.ReadInt32(`
- 第 270 行：`note + NativeLayoutCatalog.Note.PackedStartWidth);`
- 第 274 行：`bool consumed = Marshal.ReadByte(`
- 第 275 行：`note + NativeLayoutCatalog.Note.Consumed) != 0;`
- 第 406 行：`var mr2 = Marshal.ReadIntPtr(`
- 第 408 行：`NativeLayoutCatalog.TapNote.MeshRenderer) != IntPtr.Zero`
- 第 409 行：`? new MeshRenderer(Marshal.ReadIntPtr(`
- 第 411 行：`NativeLayoutCatalog.TapNote.MeshRenderer))`
- 第 414 行：`Marshal.WriteIntPtr(`
- 第 416 行：`NativeLayoutCatalog.TapNote.RuntimeMaterial,`

## 所有权与释放点

- 第 107 行：`DestroyMaterials(materials);`
- 第 111 行：`UnityEngine.Object.Destroy(tpl);`
- 第 124 行：`FlipTargets.Clear();`
- 第 136 行：`FlipTargets.Clear();`
- 第 147 行：`FlipTargets.Clear();`
- 第 184 行：`FlipTargets.Clear();`
- 第 230 行：`DestroyEntries();`
- 第 231 行：`DestroyTemplates();`
- 第 233 行：`DiagnosticEntries.Clear();`
- 第 242 行：`DestroyEntries();`
- 第 297 行：`DiagnosticEntries.Clear();`
- 第 372 行：`internal static void ResetForChartSwitch()`
- 第 377 行：`DestroyEntries();`
- 第 378 行：`DestroyTemplates();`
- 第 432 行：`DestroyMaterials(materials);`
- 第 436 行：`UnityEngine.Object.Destroy(go);`
- 第 463 行：`DestroyMaterials(materials);`
- 第 468 行：`UnityEngine.Object.Destroy(tpl);`
- 第 585 行：`UnityEngine.Object.Destroy(clone);`
- 第 593 行：`DestroyMaterials(owned);`
- 第 598 行：`private static void DestroyEntries()`
- 第 602 行：`DestroyMaterials(e.OwnedMaterials);`
- 第 603 行：`e.OwnedMaterials.Clear();`
- 第 604 行：`try { if (e.Go != null) UnityEngine.Object.Destroy(e.Go); }`

## 性能边界

- 包含帧级入口：`Tick`。不得新增无界扫描、每帧对象创建或无界缓存。

## 对应测试

- FrameScanHarness
- R3/R4 contract harness
- TimingGroupTransactionHarness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
