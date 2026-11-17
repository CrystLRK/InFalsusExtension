# SkyBlockRuntimeSlots.cs

> 源码：`InFalsusChartExt/Core/Native/SkyBlockRuntimeSlots.cs`
> SHA256：`0E968FFCAD7381D4549A372AC2A944CC64B1B02BFEBEC375E856BEE5610B450E`
> 代码行数：105
> 文档状态：基于 `E77A4112` 源码结构扫描生成的维护基线

## 职责

note id→native slot

## 生命周期

Mod 启动校验、谱面载入/编译及当前谱上下文生命周期。

## 顶层及类型声明

- `SkyBlockRuntimeSlots`（`class`，源码第 8 行）

## 方法/属性职责

- `Ensure`（第 16 行）：由函数名、签名和调用方确定职责；行为修改时同步更新。
  - 签名：`internal static bool Ensure(SkyBlockPlan plan)`
- `TryNote`（第 75 行）：受保护地尝试读取或执行；失败时应返回安全状态。
  - 签名：`internal static bool TryNote(`
- `Reset`（第 98 行）：清理谱面/场景状态或释放自有资源。
  - 签名：`internal static void Reset()`

## 原版/Harmony/native 访问点

- 未声明 Harmony Patch。
- 第 61 行：`NativeLayoutCatalog.NoteStride;`
- 第 62 行：`if (Marshal.ReadInt64(`
- 第 63 行：`note + NativeLayoutCatalog.Note.Id) !=`
- 第 65 行：`Marshal.ReadInt32(`
- 第 66 行：`note + NativeLayoutCatalog.Note.Type) != 5)`
- 第 87 行：`NativeLayoutCatalog.NoteStride;`
- 第 88 行：`if (Marshal.ReadInt64(`
- 第 89 行：`candidate + NativeLayoutCatalog.Note.Id) !=`
- 第 91 行：`Marshal.ReadInt32(`
- 第 92 行：`candidate + NativeLayoutCatalog.Note.Type) != 5)`

## 所有权与释放点

- 第 29 行：`SlotsById.Clear();`
- 第 100 行：`SlotsById.Clear();`

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
