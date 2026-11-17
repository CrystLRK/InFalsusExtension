# NativeNoteSlotTable.cs

> 源码：`InFalsusChartExt/Core/Native/NativeNoteSlotTable.cs`
> SHA256：`D26A064310573A9F654D513E435ECBA36FB486F42D40C1280BB4A5119CC4B5FC`
> 代码行数：107
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

per-side note slot 视图

## 生命周期

Mod 启动校验、谱面载入/编译及当前谱上下文生命周期。

## 顶层及类型声明

- `NativeNoteSlotTable`（`class`，源码第 11 行）

## 方法/属性职责

- `SlotBase`（第 22 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static IntPtr SlotBase(int side)`
- `Refresh`（第 25 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void Refresh()`
- `ForceRefresh`（第 89 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static void ForceRefresh()`
- `Reset`（第 95 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal static void Reset()`
- `Clear`（第 101 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`private static void Clear()`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 第 14 行：`NativeLayoutCatalog.SideCount;`
- 第 67 行：`IntPtr array = Marshal.ReadIntPtr(`
- 第 68 行：`hap + NativeLayoutCatalog.CompiledSide.PositionArray);`
- 第 69 行：`IntPtr memoryObject = Marshal.ReadIntPtr(`
- 第 70 行：`hap + NativeLayoutCatalog.CompiledSide.MemoryObject);`
- 第 71 行：`int memoryIndex = Marshal.ReadInt32(`
- 第 72 行：`hap + NativeLayoutCatalog.CompiledSide.MemoryIndex);`
- 第 77 行：`: viewObject + NativeLayoutCatalog.ArrayDataOffset +`
- 第 78 行：`viewIndex * NativeLayoutCatalog.NoteStride;`

## 所有权与释放点

- 第 29 行：`Clear();`
- 第 40 行：`Clear();`
- 第 46 行：`Clear();`
- 第 83 行：`Clear();`
- 第 98 行：`Clear();`
- 第 101 行：`private static void Clear()`
- 第 103 行：`Array.Clear(SideSlotBase, 0, SideSlotBase.Length);`

## 性能边界

- 未检测到直接帧级入口；仍应在修改后复核分配和调用频率。

## 对应测试

- R3/R4 contract harness
- 构建：0 warning / 0 error

## 拆分约束

- 只能移动代码或拆分类型，不改变命名空间、类型全名、方法体、Harmony 目标和调用顺序。
- 行为修改必须单独提交，不能混入目录迁移。
- 文件职责或 API 变化时同步更新本文件、架构说明和覆盖检查结果。
