# TimingNoInputSkyVisualRuntime.cs

> 源码：`InFalsusChartExt/Features/TimingGroups/TimingNoInputSkyVisualRuntime.cs`
> SHA256：`8CB93B76E31FF14E285A74A43426DF8DFF7FC9FC07A37D99723BCFCC00A5DF36`
> 代码行数：957
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

noinput/noclip SkyArea 独立视觉

## 生命周期

谱面编译建立时间线；Track.Update 和判定阶段读取，切谱释放。

## 顶层及类型声明

- `TimingNoInputSkyVisualRuntime`（`class`，源码第 16 行）
- `Entry`（`class`，源码第 22 行）
- `RangeInfo`（`class`，源码第 42 行）

## 方法/属性职责

- `Tick`（第 76 行）：执行帧级状态或视觉更新；必须保持无异常扩散和稳定分配。
  - 签名：`internal static unsafe void Tick(`
- `TryGetGroupMotion`（第 181 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`internal static bool TryGetGroupMotion(`
- `EnsureChart`（第 218 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static bool EnsureChart(`
- `ResetForChartSwitch`（第 237 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal static void ResetForChartSwitch()`
- `BuildRanges`（第 246 行）：构造运行时对象或计划；失败路径不得遗留所有权。
  - 签名：`private static void BuildRanges()`
- `MergeRenderRanges`（第 411 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static Vector2 MergeRenderRanges(`
- `AddRange`（第 426 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void AddRange(`
- `Find`（第 461 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static int Find(int[] parent, int index)`
- `BuildRenderNote`（第 471 行）：构造运行时对象或计划；失败路径不得遗留所有权。
  - 签名：`private static unsafe Il2Cpp_b._fA BuildRenderNote(`
- `WriteBand`（第 521 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void WriteBand(`
- `CreateEntry`（第 534 行）：构造运行时对象或计划；失败路径不得遗留所有权。
  - 签名：`private static Entry CreateEntry(`
- `CreateArea`（第 579 行）：构造运行时对象或计划；失败路径不得遗留所有权。
  - 签名：`private static GameObject CreateArea(`
- `UpdateEntry`（第 622 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void UpdateEntry(`
- `UpdateBrackets`（第 722 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void UpdateBrackets(`
- `IsBracketHeadInBounds`（第 772 行）：判断当前对象/状态条件。
  - 签名：`internal static bool IsBracketHeadInBounds(`
- `CreateBrackets`（第 778 行）：构造运行时对象或计划；失败路径不得遗留所有权。
  - 签名：`private static void CreateBrackets(`
- `CloneMaterials`（第 820 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static List<Material> CloneMaterials(GameObject root)`
- `SetBracketsActive`（第 859 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void SetBracketsActive(`
- `ReadVector3`（第 878 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static Vector3 ReadVector3(IntPtr address)`
- `ReadQuaternion`（第 889 行）：读取状态或原生数据，并校验对象边界。
  - 签名：`private static Quaternion ReadQuaternion(IntPtr address)`
- `Hide`（第 902 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void Hide(long id)`
- `DestroyAll`（第 910 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void DestroyAll()`
- `DestroyEntry`（第 917 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void DestroyEntry(Entry entry)`
- `DestroyMaterials`（第 928 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`private static void DestroyMaterials(`
- `TryDestroy`（第 945 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`private static void TryDestroy(GameObject go)`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 第 475 行：`NativeLayoutCatalog.NoteStride];`
- 第 477 行：`buffer, NativeLayoutCatalog.NoteStride).Clear();`
- 第 481 行：`Marshal.WriteInt64(`
- 第 482 行：`ptr + NativeLayoutCatalog.Note.Id, info.Id);`
- 第 483 行：`Marshal.WriteInt64(`
- 第 484 行：`ptr + NativeLayoutCatalog.Note.Group, info.Group);`
- 第 485 行：`Marshal.WriteInt32(`
- 第 486 行：`ptr + NativeLayoutCatalog.Note.Side, 4);`
- 第 487 行：`Marshal.WriteInt32(`
- 第 488 行：`ptr + NativeLayoutCatalog.Note.Type, 5);`
- 第 489 行：`Marshal.WriteInt32(`
- 第 490 行：`ptr + NativeLayoutCatalog.Note.StartMs, info.StartMs);`
- 第 491 行：`Marshal.WriteInt32(`
- 第 492 行：`ptr + NativeLayoutCatalog.Note.EndMs, info.EndMs);`
- 第 494 行：`ptr + NativeLayoutCatalog.Note.PackedStart,`
- 第 497 行：`ptr + NativeLayoutCatalog.Note.PackedEnd,`
- 第 500 行：`ptr + NativeLayoutCatalog.Note.PackedStartWidth,`
- 第 503 行：`ptr + NativeLayoutCatalog.Note.PackedEndWidth,`
- 第 505 行：`Marshal.WriteInt64(`
- 第 506 行：`ptr + NativeLayoutCatalog.Note.RuntimeLead,`
- 第 508 行：`Marshal.WriteByte(`
- 第 509 行：`ptr + NativeLayoutCatalog.Note.Consumed, 0);`
- 第 513 行：`Marshal.WriteInt32(`
- 第 514 行：`ptr + NativeLayoutCatalog.Note.Flags,`
- 其余原生访问点从源码搜索 `Marshal`、`IL2CPP`、`NativeLayoutCatalog`。

## 所有权与释放点

- 第 162 行：`DeadIds.Clear();`
- 第 169 行：`DestroyEntry(Entries[id]);`
- 第 229 行：`DestroyAll();`
- 第 230 行：`Ranges.Clear();`
- 第 231 行：`OrderedRanges.Clear();`
- 第 237 行：`internal static void ResetForChartSwitch()`
- 第 239 行：`DestroyAll();`
- 第 240 行：`Ranges.Clear();`
- 第 241 行：`OrderedRanges.Clear();`
- 第 242 行：`DeadIds.Clear();`
- 第 248 行：`Ranges.Clear();`
- 第 249 行：`OrderedRanges.Clear();`
- 第 477 行：`buffer, NativeLayoutCatalog.NoteStride).Clear();`
- 第 551 行：`DestroyEntry(entry);`
- 第 571 行：`DestroyEntry(entry);`
- 第 617 行：`TryDestroy(go);`
- 第 810 行：`TryDestroy(entry.LeftBracket);`
- 第 811 行：`TryDestroy(entry.RightBracket);`
- 第 846 行：`UnityEngine.Object.Destroy(clone);`
- 第 854 行：`DestroyMaterials(owned);`
- 第 910 行：`private static void DestroyAll()`
- 第 913 行：`DestroyEntry(entry);`
- 第 914 行：`Entries.Clear();`
- 第 917 行：`private static void DestroyEntry(Entry entry)`

## 性能边界

- 包含帧级入口：`Tick`, `UpdateBrackets`, `UpdateEntry`。不得新增无界扫描、每帧对象创建或无界缓存。

## 对应测试

- FrameScanHarness
- R3/R4 contract harness
- TimingGroupTransactionHarness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
